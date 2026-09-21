"""Objektiver Messlauf #73, Schritt 2: Kandidaten ueber den Korpus, alle Abschnitte, K = 1.

Misst, was ohne Urteil messbar ist -- Token (Eingabe, gecacht, Ausgabe, Denken), Kosten mit dem
tatsaechlich gemeldeten Cache-Anteil, Laenge, Erstversuchsquote und die Formatpruefungen aus
`format_checks.py`. Qualitaet entscheidet der Lesevergleich in Schritt 3.

Wie in der Anwendung:

* Titel, Abstract und alle sieben Abschnitte laufen mit der Systemnachricht des Kandidaten.
* `{word_limit}` wird wie `WordLimitCalculator` ersetzt.
* Die **E-Mail** laeuft in einer zweiten Phase auf der Prosa-Ausgabe *desselben* Kandidaten
  (`SummaryGenerator.GenerateEmailTextAsync` bekommt die Prosa, nicht das Transkript).
* `max_completion_tokens` 20.000 fuer Abschnitte, 4.000 fuer die E-Mail -- wie `Options(...)`.
* Kein `reasoning_effort` (Faktorenplan §3), keine Korrekturliste.

Aufruf:
    uv run python messlauf.py --kandidat R=<pfad> --kandidat K1=<pfad> \\
        --korpus <eval>\\corpus.v3.json --out <eval>\\messlauf73.json [--dry-run]

Nur einzelne Abschnitte neu, alles andere aus einem frueheren Lauf:
    uv run python messlauf.py ... --out <eval>\\messlauf73b.json \\
        --basis <eval>\\messlauf73_gen.jsonl --neu K1:emailPrompt

Zwischenstand `<out-stem>_gen.jsonl` wird je Antwort geschrieben; ein Neustart bezahlt nichts
doppelt.
"""

from __future__ import annotations

import argparse
import json
import re
import statistics
import time
from concurrent.futures import ThreadPoolExecutor
from dataclasses import asdict
from datetime import datetime, timezone
from pathlib import Path

from format_checks import check, word_limit_for
from pilot import append_jsonl, chat, read_jsonl
from variants import TITLE_PROMPT, count_tokens

MODEL = "gpt-5.6-luna"

#: Phase 1 -- alles, was das Transkript bekommt.
PHASE_ONE: tuple[str, ...] = (
    "title",
    "abstractPrompt",
    "structuredPrompt",
    "prosePrompt",
    "aufgabePrompt",
    "gespraechsnotizPrompt",
    "stundenzettelPrompt",
    "analogPrompt",
)
EMAIL = "emailPrompt"
AUTO_CALLS = PHASE_ONE[:6]

MAX_TOKENS = {"title": 4000, EMAIL: 4000}
DEFAULT_MAX_TOKENS = 20000

#: Grobe Annahme nur fuer --dry-run: sichtbare plus Denk-Token je Aufruf.
DRY_RUN_OUTPUT_TOKENS = 700


def catalog_prices(source: str, cache_discount: float) -> dict[str, float]:
    """Ein- und Ausgabepreis des ersten Katalogeintrags (Luna) aus dem C#-Quelltext."""
    price_in = re.search(r"PriceInPerMillion:\s*([0-9.]+)", source)
    price_out = re.search(r"PriceOutPerMillion:\s*([0-9.]+)", source)
    if price_in is None or price_out is None:
        raise ValueError("Preise nicht im Katalog gefunden -- Quelltext geaendert?")
    value_in = float(price_in.group(1))
    return {
        "input": value_in,
        "cached": value_in * (1 - cache_discount),
        "output": float(price_out.group(1)),
    }


def user_message(prompts: dict, section: str, transcript: str, prose: str = "") -> str:
    if section == "title":
        return TITLE_PROMPT.replace("{transcript}", transcript)
    if section == EMAIL:
        return prompts[EMAIL].replace("{prose_summary}", prose)
    return (
        prompts[section]
        .replace("{word_limit}", str(word_limit_for(transcript)))
        .replace("{transcript}", transcript)
    )


def key_of(item_id: str, candidate: str, section: str) -> str:
    return f"{item_id}|{candidate}|{section}"


def phase_one_tasks(items: list[dict], candidates: dict[str, dict]) -> list[dict]:
    return [
        {"item": item, "candidate": name, "section": section}
        for item in items
        for name in candidates
        for section in PHASE_ONE
    ]


def email_tasks(items: list[dict], candidates: dict[str, dict], done: dict[str, dict]) -> list[dict]:
    """E-Mail-Aufgaben fuer jede vorhandene, nicht leere Prosa desselben Kandidaten."""
    tasks = []
    for item in items:
        for name in candidates:
            prose = done.get(key_of(item["id"], name, "prosePrompt"))
            if prose and prose["output"]:
                tasks.append({"item": item, "candidate": name, "section": EMAIL, "prose": prose["output"]})
    return tasks


def parse_renew(specs: list[str], candidates: set[str]) -> set[tuple[str, str]]:
    """`NAME:ABSCHNITT` -> (Kandidat, Abschnitt); unbekannte Namen sind ein Tippfehler, kein Leerlauf."""
    renew = set()
    for spec in specs:
        name, _, section = spec.partition(":")
        if name not in candidates:
            raise ValueError(f"--neu: Kandidat '{name}' ist nicht per --kandidat angegeben")
        if section not in (*PHASE_ONE, EMAIL):
            raise ValueError(f"--neu: unbekannter Abschnitt '{section}'")
        renew.add((name, section))
    return renew


def _completed(row: dict) -> bool:
    """Eine Antwort gilt nur als erledigt, wenn der Aufruf nicht gescheitert ist."""
    return not (row.get("gen") or {}).get("error")


def completed_rows(rows: list[dict]) -> dict[str, dict]:
    """Zwischenstand ohne gescheiterte Aufrufe -- die werden beim Fortsetzen wiederholt."""
    return {row["key"]: row for row in rows if _completed(row)}


def seed_from_basis(basis: list[dict], candidates: set[str], renew: set[tuple[str, str]]) -> dict[str, dict]:
    """Antworten eines frueheren Laufs uebernehmen, ausser denen, die neu erzeugt werden sollen.

    Die E-Mail entsteht aus der Prosa: Wird die Prosa eines Kandidaten erneuert, gilt seine E-Mail
    mit als erneuert, sonst stuende eine Mail neben einer Prosa, aus der sie nicht stammt.
    """
    dropped = set(renew) | {(name, EMAIL) for name, section in renew if section == "prosePrompt"}
    return {
        row["key"]: row
        for row in basis
        if row["candidate"] in candidates
        and (row["candidate"], row["section"]) not in dropped
        and _completed(row)
    }


def call_cost(gen: dict, prices: dict[str, float]) -> float:
    uncached = gen["prompt_tokens"] - gen["cached_tokens"]
    return (
        uncached * prices["input"]
        + gen["cached_tokens"] * prices["cached"]
        + gen["output_tokens"] * prices["output"]
    ) / 1e6


def run_task(task: dict, candidates: dict[str, dict]) -> dict:
    prompts = candidates[task["candidate"]]
    item = task["item"]
    call = chat(
        MODEL,
        prompts["systemMessage"],
        user_message(prompts, task["section"], item["text"], task.get("prose", "")),
        max_tokens=MAX_TOKENS.get(task["section"], DEFAULT_MAX_TOKENS),
    )
    return {
        "key": key_of(item["id"], task["candidate"], task["section"]),
        "item_id": item["id"],
        "bucket": item.get("bucket", "?"),
        "candidate": task["candidate"],
        "section": task["section"],
        "model": MODEL,
        "output": call.text,
        "gen": asdict(call),
        "time": datetime.now(timezone.utc).isoformat(timespec="seconds"),
    }


def _mean(values: list[float]) -> float:
    return round(statistics.fmean(values), 4) if values else 0.0


def summarise(rows: list[dict], transcripts: dict[str, str], prices: dict[str, float]) -> dict:
    """Kennzahlen je Kandidat und Abschnitt plus Kosten je Diktat."""
    groups: dict[tuple[str, str], list[dict]] = {}
    for row in rows:
        groups.setdefault((row["candidate"], row["section"]), []).append(row)

    per_section: dict[str, dict[str, dict]] = {}
    for (candidate, section), group in sorted(groups.items()):
        entry: dict = {
            "n": len(group),
            "eingabe": _mean([r["gen"]["prompt_tokens"] for r in group]),
            "gecacht": _mean([r["gen"]["cached_tokens"] for r in group]),
            "ausgabe": _mean([r["gen"]["output_tokens"] for r in group]),
            "denken": _mean([r["gen"]["reasoning_tokens"] for r in group]),
            "usd": _mean([call_cost(r["gen"], prices) for r in group]),
            "erstversuch_ok": _mean(
                [
                    float(r["gen"]["well_formed_first_try"] and r["gen"]["finish_reason"] == "stop" and bool(r["output"]))
                    for r in group
                ]
            ),
        }
        if section != "title":
            results = [check(section, r["output"], transcripts[r["item_id"]]) for r in group]
            names = sorted({name for res in results for name in res.checks})
            entry["pruefungen"] = {}
            for name in names:
                applicable = [(res, r) for res, r in zip(results, group) if res.checks.get(name) is not None]
                failed = [r["item_id"] for res, r in applicable if res.checks[name] is False]
                entry["pruefungen"][name] = {
                    "anwendbar": len(applicable),
                    "verletzt": len(failed),
                    "beispiele": failed[:5],
                }
            for metric in ("woerter", "leerfall", "verschachtelt", "verhaeltnis", "aufgaben", "woerter_textkoerper", "unklar_erwaehnt"):
                values = [res.metrics[metric] for res in results if isinstance(res.metrics.get(metric), (int, float))]
                if values:
                    entry[metric] = _mean(values)
            if section in ("stundenzettelPrompt", "gespraechsnotizPrompt", "aufgabePrompt"):
                entry["leerfall_diktate"] = [r["item_id"] for res, r in zip(results, group) if res.metrics["leerfall"]]
        per_section.setdefault(candidate, {})[section] = entry

    per_dictation: dict[str, dict] = {}
    for candidate in per_section:
        by_item: dict[str, float] = {}
        for row in rows:
            if row["candidate"] == candidate and row["section"] in AUTO_CALLS:
                by_item[row["item_id"]] = by_item.get(row["item_id"], 0.0) + call_cost(row["gen"], prices)
        per_dictation[candidate] = {"auto_usd_mittel": _mean(list(by_item.values())), "diktate": len(by_item)}
    return {"je_abschnitt": per_section, "je_diktat": per_dictation}


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--kandidat", action="append", required=True, metavar="NAME=PFAD")
    parser.add_argument("--korpus", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--workers", type=int, default=6)
    parser.add_argument(
        "--katalog",
        type=Path,
        default=Path(__file__).resolve().parents[2] / "Platee.Johann.Application" / "Processing" / "SummaryModelCatalog.cs",
    )
    parser.add_argument("--cache-rabatt", type=float, default=0.9)
    parser.add_argument(
        "--basis",
        type=Path,
        action="append",
        default=[],
        help="_gen.jsonl frueherer Laeufe (mehrfach; spaetere ueberschreiben fruehere); ihre Antworten "
        "werden uebernommen statt neu bezahlt",
    )
    parser.add_argument(
        "--neu",
        action="append",
        default=[],
        metavar="NAME:ABSCHNITT",
        help="nur mit --basis: diese Antworten trotzdem neu erzeugen (z. B. K1:emailPrompt)",
    )
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args(argv)
    if args.neu and not args.basis:
        parser.error("--neu braucht --basis")

    candidates: dict[str, dict] = {}
    for spec in args.kandidat:
        name, _, path = spec.partition("=")
        if not name or not path:
            parser.error(f"--kandidat erwartet NAME=PFAD: {spec}")
        candidates[name] = json.loads(Path(path).read_text(encoding="utf-8"))
    corpus = json.loads(args.korpus.read_text(encoding="utf-8"))
    items = corpus["items"] if isinstance(corpus, dict) else corpus
    transcripts = {item["id"]: item["text"] for item in items}
    prices = catalog_prices(args.katalog.read_text(encoding="utf-8"), args.cache_rabatt)

    gen_path = args.out.with_name(f"{args.out.stem}_gen.jsonl")
    if any(path.resolve() == gen_path.resolve() for path in args.basis):
        parser.error("--basis darf nicht der Zwischenstand von --out sein")
    try:
        renew = parse_renew(args.neu, set(candidates))
    except ValueError as error:
        parser.error(str(error))
    basis_rows = [row for path in args.basis for row in read_jsonl(path)]
    done = seed_from_basis(basis_rows, set(candidates), renew)
    if done:
        print(f"aus --basis uebernommen: {len(done)} Antworten")
    resumed = completed_rows(read_jsonl(gen_path))
    if resumed:
        print(f"Zwischenstand vorhanden: {len(resumed)} Antworten -- werden uebersprungen")
    done.update(resumed)

    first = phase_one_tasks(items, candidates)
    if args.dry_run:
        open_first = [t for t in first if key_of(t["item"]["id"], t["candidate"], t["section"]) not in done]
        prose_pending = {(t["item"]["id"], t["candidate"]) for t in open_first if t["section"] == "prosePrompt"}
        open_email = [
            {**t, "section": EMAIL}
            for t in first
            if t["section"] == "prosePrompt"
            and (
                (t["item"]["id"], t["candidate"]) in prose_pending
                or key_of(t["item"]["id"], t["candidate"], EMAIL) not in done
            )
        ]
        todo = open_first + open_email
        tokens_in = sum(
            count_tokens(p["systemMessage"]) + count_tokens(user_message(p, t["section"], t["item"]["text"], t["item"]["text"]))
            for t in todo
            for p in [candidates[t["candidate"]]]
        )
        usd = (tokens_in * prices["input"] + len(todo) * DRY_RUN_OUTPUT_TOKENS * prices["output"]) / 1e6
        print(f"Diktate {len(items)} · Kandidaten {len(candidates)} · offene Aufrufe {len(todo)}")
        print(f"Eingabe ~{tokens_in:,} Token · geschaetzt ~{usd:.2f} $ (Ausgabe {DRY_RUN_OUTPUT_TOKENS} Token/Aufruf angenommen)")
        print("Probelauf -- kein Aufruf abgesetzt.")
        return 0

    started = time.time()
    for phase, tasks in (("Phase 1", first), ("Phase 2 E-Mail", None)):
        if tasks is None:
            tasks = email_tasks(items, candidates, done)
        open_tasks = [t for t in tasks if key_of(t["item"]["id"], t["candidate"], t["section"]) not in done]
        print(f"{phase}: {len(open_tasks)} offen von {len(tasks)}")
        with ThreadPoolExecutor(max_workers=args.workers) as pool:
            for index, row in enumerate(pool.map(lambda t: run_task(t, candidates), open_tasks), 1):
                append_jsonl(gen_path, row)
                done[row["key"]] = row
                if index % 50 == 0:
                    print(f"   {index}/{len(open_tasks)}  ({time.time() - started:.0f} s)")

    rows = list(done.values())
    summary = summarise(rows, transcripts, prices)
    payload = {
        "erzeugt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "modell": MODEL,
        "preise": prices,
        "cache_rabatt": args.cache_rabatt,
        "korpus": str(args.korpus),
        "kandidaten": {name: spec.partition("=")[2] for name, spec in zip(candidates, args.kandidat)},
        "basis": [str(path) for path in args.basis],
        "neu": sorted(f"{name}:{section}" for name, section in renew),
        "zusammenfassung": summary,
        "zeilen": rows,
    }
    args.out.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    errors = sum(1 for row in rows if row["gen"]["error"])
    print(f"\n{len(rows)} Antworten, {errors} Fehler, {time.time() - started:.0f} s -> {args.out}")
    for candidate, figures in summary["je_diktat"].items():
        print(f"   {candidate}: {figures['auto_usd_mittel'] * 100:.3f} ¢ je Diktat (6 automatische Aufrufe)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
