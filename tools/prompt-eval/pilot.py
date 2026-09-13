"""Pilotlauf: misst das Messinstrument, nicht die Prompt-Qualitaet.

Der Hauptlauf braucht fuenf Zahlen, die man nicht raten kann. Dieser Lauf beschafft sie:

1. tau^2 / sigma^2 -- Streuung zwischen Diktaten gegen Rauschen innerhalb eines Diktats.
   Entscheidet, wie das Budget zwischen Items und Wiederholungen aufgeteilt wird. Es gibt
   dafuer nachweislich keine Faustregel (Hedges & Schauer, 2021).
2. Selbstkonsistenz des Richters -- gibt er demselben Text zweimal dieselbe Note? Die
   Literatur berichtet Werte zwischen 61 und 95 %, je nach Modell und Temperatur.
3. Rauschboden -- wie weit streut derselbe Prompt ueber Wiederholungen? Daraus wird die
   Aequivalenzmarge fuer jeden spaeteren Nullbefund abgeleitet.
4. Korrelation Richter zu Mensch -- Voraussetzung fuer Prediction-Powered Inference. Unterhalb
   von 1/sqrt(n-2) schadet das Verfahren mehr als es nuetzt (arXiv:2505.20178). Die
   menschlichen Noten kommen aus `pilot_analyse.py`, dieser Lauf legt nur die Stichprobe fest.
5. Batch gegen synchron -- ob der Rabatt von 50 % die Messung unberuehrt laesst. Die
   Gleichwertigkeit ist plausibel, aber nicht dokumentiert.

Aufruf:
    uv run python pilot.py --dry-run          # Umfang und Kosten, ohne einen Aufruf
    uv run python pilot.py                    # echter Lauf

Jede Rohantwort wird vollstaendig gespeichert. Das ist die einzige Reproduzierbarkeitsgarantie,
die bei einer gehosteten API bleibt: API-Modelle sind auch bei Temperatur 0 nicht reproduzierbar
(Levy, 2026), und `seed` ist in der Spezifikation als veraltet markiert. Eine korrigierte
Auswertung kostet damit null neue Aufrufe -- genau das hat im Vorversuch einen Fehlbefund
gerettet.
"""

from __future__ import annotations

import argparse
import io
import json
import os
import random
import time
import urllib.request
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass, asdict
from datetime import datetime, timezone
from pathlib import Path

BASE = Path(os.environ["USERPROFILE"]) / "Documents" / "Johann"
SANDBOX = BASE / "prompt-sandbox"
EVAL = SANDBOX / "eval"

#: Dateierte Snapshots statt Aliase. Ein Alias zeigt morgen auf andere Gewichte, und der Lauf
#: waere nicht mehr zuzuordnen.
GENERATOR_MODEL = "gpt-5.6-luna"
JUDGE_MODEL = "gpt-5.6-terra"

#: Der Abschnitt mit dem reichsten Ausgabevertrag -- dort ist am meisten zu messen.
SECTION = "structuredPrompt"

JUDGE_SYSTEM = (
    "Du bewertest die Qualitaet einer automatisch erzeugten Zusammenfassung eines Diktats. "
    "Du antwortest ausschliesslich mit einem JSON-Objekt."
)

#: Drei benannte Dimensionen statt einer holistischen Note. Die Ankertexte machen aus der Skala
#: eine verhaltensverankerte Skala: jede Stufe beschreibt beobachtbares Verhalten, nicht ein
#: Gefuehl. Das ist der Teil, der die Uebereinstimmung zwischen Bewertern traegt.
JUDGE_TEMPLATE = """Bewerte die Zusammenfassung gegen das Transkript auf drei Dimensionen, je 1 bis 5.

TREUE - steht in der Zusammenfassung nur, was im Transkript steht?
  5 = jede Aussage ist im Transkript belegt
  3 = eine Aussage geht ueber das Transkript hinaus oder deutet es um
  1 = mehrere erfundene oder verfaelschte Aussagen

VOLLSTAENDIGKEIT - ist alles Wesentliche enthalten?
  5 = jede Entscheidung, Aufgabe und Zahl aus dem Transkript kommt vor
  3 = ein wesentlicher Punkt fehlt
  1 = mehrere wesentliche Punkte fehlen

KLARHEIT - ist der Text ohne das Transkript verstaendlich?
  5 = durchgehend klar, keine Rueckfrage noetig
  3 = eine Stelle bleibt unklar
  1 = mehrere Stellen unverstaendlich

Antworte genau so:
{{"treue": <1-5>, "vollstaendigkeit": <1-5>, "klarheit": <1-5>, "begruendung": "<ein Satz>"}}

TRANSKRIPT:
{transcript}

ZUSAMMENFASSUNG:
{output}"""

_KEY: str | None = None


def api_key() -> str:
    global _KEY
    if _KEY is None:
        for line in io.open(BASE / ".env", encoding="utf-8-sig"):
            if line.strip().startswith("OPENAI_API_KEY="):
                _KEY = line.split("=", 1)[1].strip().strip("\"'")
                break
        else:
            raise SystemExit("kein OPENAI_API_KEY in Documents/Johann/.env")
    return _KEY


@dataclass
class Call:
    """Eine API-Antwort mit allem, was zur Nachvollziehbarkeit gehoert."""

    text: str
    prompt_tokens: int = 0
    output_tokens: int = 0
    reasoning_tokens: int = 0
    cached_tokens: int = 0
    finish_reason: str = ""
    latency_s: float = 0.0
    well_formed_first_try: bool = True
    error: str = ""


def chat(model: str, system: str, user: str, max_tokens: int = 4000, retries: int = 2) -> Call:
    """Ein Aufruf mit vollstaendiger Protokollierung.

    `well_formed_first_try` wird als eigene Groesse gefuehrt und nicht stillschweigend
    wegrepariert. Wenn eine Bedingung mehr Fehlversuche produziert als eine andere und man die
    nachzieht, verschiebt sich ihre Stichprobenverteilung -- unsichtbar. Die Erstversuchsquote
    ist deshalb ein Messwert, kein Betriebsdetail.
    """
    payload = {
        "model": model,
        "max_completion_tokens": max_tokens,
        "messages": ([{"role": "system", "content": system}] if system else [])
        + [{"role": "user", "content": user}],
    }
    request = urllib.request.Request(
        "https://api.openai.com/v1/chat/completions",
        data=json.dumps(payload).encode(),
        headers={"Authorization": f"Bearer {api_key()}", "Content-Type": "application/json"},
    )
    first_try = True
    for attempt in range(retries + 1):
        started = time.time()
        try:
            data = json.load(urllib.request.urlopen(request, timeout=300))
            usage = data["usage"]
            details = usage.get("completion_tokens_details", {})
            prompt_details = usage.get("prompt_tokens_details", {})
            return Call(
                text=(data["choices"][0]["message"]["content"] or "").strip(),
                prompt_tokens=usage["prompt_tokens"],
                output_tokens=usage["completion_tokens"],
                reasoning_tokens=details.get("reasoning_tokens", 0),
                cached_tokens=prompt_details.get("cached_tokens", 0),
                finish_reason=data["choices"][0]["finish_reason"],
                latency_s=round(time.time() - started, 2),
                well_formed_first_try=first_try,
            )
        except Exception as exc:  # noqa: BLE001 - Netzwerkfehler sind heterogen
            first_try = False
            if attempt == retries:
                return Call(text="", error=str(exc)[:200], well_formed_first_try=False)
            time.sleep(2 * (attempt + 1))
    raise AssertionError("unerreichbar")


def parse_scores(text: str) -> dict | None:
    """Liest das JSON-Urteil. Gibt None zurueck, statt zu raten."""
    start, end = text.find("{"), text.rfind("}")
    if start < 0 or end <= start:
        return None
    try:
        value = json.loads(text[start : end + 1])
    except json.JSONDecodeError:
        return None
    if not all(k in value for k in ("treue", "vollstaendigkeit", "klarheit")):
        return None
    return value


def load_corpus(limit: int, seed: int) -> list[dict]:
    """Zieht eine nach Laenge geschichtete Stichprobe.

    Geschichtet, weil sich Prompt-Varianten bei kurzen und langen Diktaten unterschiedlich
    verhalten koennen. Eine unstratifizierte Stichprobe aus einem Korpus, den kurze
    Archiv-Diktate dominieren, wuerde das Verhalten bei langen Diktaten kaum abbilden -- genau
    diese Schwaeche hatte der Vorversuch.
    """
    corpus = json.loads((EVAL / "corpus.json").read_text(encoding="utf-8"))
    items = corpus["items"] if isinstance(corpus, dict) else corpus
    buckets: dict[str, list[dict]] = {}
    for item in items:
        buckets.setdefault(item.get("bucket", "?"), []).append(item)

    rng = random.Random(seed)
    per_bucket = max(1, limit // max(1, len(buckets)))
    picked: list[dict] = []
    for name in sorted(buckets):
        pool = buckets[name]
        picked.extend(rng.sample(pool, min(per_bucket, len(pool))))
    rng.shuffle(picked)
    return picked[:limit]


def build_tasks(
    items: list[dict], variants: dict[str, dict], repeats: int
) -> list[dict]:
    """Kreuzt Diktate, Varianten und Wiederholungen zu einer flachen Aufgabenliste."""
    tasks: list[dict] = []
    for item in items:
        for variant_id, prompts in variants.items():
            template = prompts[SECTION]
            for run_index in range(repeats):
                tasks.append(
                    {
                        "item_id": item["id"],
                        "bucket": item.get("bucket", "?"),
                        "transcript": item["text"],
                        "variant_id": variant_id,
                        "section_id": SECTION,
                        "run_index": run_index,
                        "system": prompts["systemMessage"],
                        "user": template.replace("{transcript}", item["text"]),
                    }
                )
    return tasks


def generate(task: dict) -> dict:
    call = chat(GENERATOR_MODEL, task["system"], task["user"])
    row = {k: v for k, v in task.items() if k not in ("system", "user", "transcript")}
    row.update(
        {
            "generator_model": GENERATOR_MODEL,
            "output": call.text,
            "gen": asdict(call),
            "time_block": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H"),
        }
    )
    return row


def rate(row: dict, transcripts: dict[str, str], judge_rep: int) -> dict:
    """Eine Bewertung. Mehrere Wiederholungen ergeben die Selbstkonsistenz."""
    if not row["output"]:
        return {"judge_rep": judge_rep, "error": "leere Ausgabe"}
    call = chat(
        JUDGE_MODEL,
        JUDGE_SYSTEM,
        JUDGE_TEMPLATE.format(
            transcript=transcripts[row["item_id"]][:12000], output=row["output"][:12000]
        ),
        max_tokens=1200,
    )
    scores = parse_scores(call.text)
    return {
        "judge_rep": judge_rep,
        "judge_model": JUDGE_MODEL,
        "scores": scores,
        "raw": call.text if scores is None else "",
        "usage": asdict(call),
    }


def append_jsonl(path: Path, record: dict) -> None:
    """Haengt einen Datensatz an und schreibt ihn sofort auf die Platte.

    ⚠ Warum nicht am Ende alles auf einmal: Der erste Anlauf dieses Pilotlaufs wurde vom System
    wegen Arbeitsspeichermangel abgebrochen -- und weil erst zum Schluss geschrieben wurde, war
    jede bis dahin bezahlte Antwort verloren. Bei einem Lauf, der Geld kostet, ist das nicht
    hinnehmbar. Jeder Datensatz geht deshalb einzeln raus, und ein Neustart setzt dort an, wo
    der Abbruch war.
    """
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8") as handle:
        handle.write(json.dumps(record, ensure_ascii=False) + "\n")
        handle.flush()
        os.fsync(handle.fileno())


def read_jsonl(path: Path) -> list[dict]:
    """Liest zurueck, was schon da ist. Unvollstaendige letzte Zeile wird verworfen."""
    if not path.exists():
        return []
    records: list[dict] = []
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line:
            continue
        try:
            records.append(json.loads(line))
        except json.JSONDecodeError:
            break  # abgeschnittene Zeile am Ende eines Abbruchs
    return records


def gen_key(task: dict) -> str:
    return f"{task['item_id']}|{task['variant_id']}|{task['run_index']}"


def estimate_cost(n_generations: int, n_ratings: int) -> str:
    """Grobe Schaetzung aus den Messwerten des Vorversuchs. Groessenordnung, keine Zusage."""
    gen = n_generations * 0.0005
    judge = n_ratings * 0.005
    return f"~{gen:.2f} $ Erzeugung + ~{judge:.2f} $ Bewertung = ~{gen + judge:.2f} $"


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--items", type=int, default=20, help="Diktate in der Stichprobe")
    parser.add_argument("--repeats", type=int, default=3, help="Erzeugungen je Zelle (K)")
    parser.add_argument("--judge-reps", type=int, default=3, help="Bewertungen je Ausgabe (R)")
    parser.add_argument(
        "--workers",
        type=int,
        default=3,
        help="Parallele Aufrufe. Bewusst niedrig: der erste Anlauf wurde vom System wegen "
        "Arbeitsspeichermangel abgebrochen.",
    )
    parser.add_argument("--seed", type=int, default=20260913)
    parser.add_argument(
        "--variants",
        type=Path,
        nargs=2,
        default=[
            SANDBOX / "varianten-arm1" / "prompts.000.S3-S4.json",
            SANDBOX / "varianten-arm1" / "prompts.025.S1-S2-A1-A2-A3.json",
        ],
        help="Zwei Varianten aus dem Plan. Die Vorgabe ist das Paar mit der groessten "
        "Distanz im Faktorraum -- alle sieben Faktoren unterscheiden sich. Fuer die "
        "Varianzkomponenten ist das die guenstigste Wahl, weil es die Spannweite "
        "ausschoepft, die der Hauptlauf spaeter abdeckt.",
    )
    parser.add_argument("--out", type=Path, default=EVAL / "pilot_raw.json")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args(argv)

    items = load_corpus(args.items, args.seed)
    variants: dict[str, dict] = {}
    for path in args.variants:
        if not path.exists():
            raise SystemExit(f"Variante fehlt: {path}\nZuerst variants.py laufen lassen.")
        variants[path.stem] = json.loads(path.read_text(encoding="utf-8"))

    tasks = build_tasks(items, variants, args.repeats)
    n_ratings = len(tasks) * args.judge_reps

    print(f"Diktate:      {len(items)} (geschichtet nach Laenge)")
    print(f"Varianten:    {len(variants)}")
    print(f"Erzeugungen:  {len(tasks)}  ({args.repeats} Wiederholungen je Zelle)")
    print(f"Bewertungen:  {n_ratings}  ({args.judge_reps} je Ausgabe)")
    print(f"Kosten:       {estimate_cost(len(tasks), n_ratings)}")
    if args.dry_run:
        print("\nProbelauf - kein Aufruf abgesetzt.")
        return 0

    transcripts = {item["id"]: item["text"] for item in items}
    gen_path = args.out.with_name("pilot_gen.jsonl")
    rate_path = args.out.with_name("pilot_rate.jsonl")

    # Was schon bezahlt wurde, wird nicht noch einmal bezahlt.
    done_gen = {gen_key(r): r for r in read_jsonl(gen_path)}
    open_tasks = [t for t in tasks if gen_key(t) not in done_gen]
    if done_gen:
        print(f"\nbereits vorhanden: {len(done_gen)} Erzeugungen -- werden uebersprungen")

    started = time.time()
    with ThreadPoolExecutor(max_workers=args.workers) as pool:
        for row in pool.map(generate, open_tasks):
            append_jsonl(gen_path, row)
            done_gen[gen_key(row)] = row
    if open_tasks:
        print(f"Erzeugung fertig in {time.time() - started:.0f} s")

    rows = [done_gen[gen_key(t)] for t in tasks if gen_key(t) in done_gen]

    done_rate = {
        f"{r['key']}|{r['judge_rep']}": r for r in read_jsonl(rate_path) if "key" in r
    }
    jobs = [
        (row, rep)
        for row in rows
        for rep in range(args.judge_reps)
        if f"{gen_key(row)}|{rep}" not in done_rate
    ]
    if done_rate:
        print(f"bereits vorhanden: {len(done_rate)} Bewertungen -- werden uebersprungen")

    started = time.time()
    with ThreadPoolExecutor(max_workers=args.workers) as pool:
        for record in pool.map(
            lambda pair: {**rate(pair[0], transcripts, pair[1]), "key": gen_key(pair[0])}, jobs
        ):
            append_jsonl(rate_path, record)
            done_rate[f"{record['key']}|{record['judge_rep']}"] = record
    if jobs:
        print(f"Bewertung fertig in {time.time() - started:.0f} s")

    for row in rows:
        key = gen_key(row)
        row["ratings"] = [
            done_rate[f"{key}|{rep}"]
            for rep in range(args.judge_reps)
            if f"{key}|{rep}" in done_rate
        ]

    payload = {
        "erzeugt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "konfiguration": {
            "items": args.items,
            "repeats": args.repeats,
            "judge_reps": args.judge_reps,
            "seed": args.seed,
            "section": SECTION,
            "generator_model": GENERATOR_MODEL,
            "judge_model": JUDGE_MODEL,
            "varianten": [p.name for p in args.variants],
        },
        "transkripte": transcripts,
        "zeilen": rows,
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )

    failed = sum(1 for row in rows if not row["output"])
    unparsed = sum(
        1 for row in rows for r in row["ratings"] if r.get("scores") is None
    )
    print(f"\nleere Ausgaben:       {failed} von {len(rows)}")
    print(f"unlesbare Urteile:    {unparsed} von {n_ratings}")
    print(f"Rohdaten: {args.out}")
    print("\nWeiter mit: uv run python pilot_analyse.py")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
