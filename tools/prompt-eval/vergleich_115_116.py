"""Vergleich #115/#116: alter gegen neuen Titel- und E-Mail-Prompt, sonst alles gleich.

Haelt fest, was die Aenderung nicht betrifft: Beide Fassungen bekommen dieselben Transkripte
(Titel) und dieselben Prosa-Texte (E-Mail) -- die Prosa stammt aus einem frueheren Lauf der
gueltigen Fassung (`--basis`, Kandidat K1). Die synthetischen Test-Diktate (`--fixtures`, D1-D6)
kommen dazu; D4 ist der Fall „Herr Thomas“ (F26), D2 der Titel „unerquicklich“ (F03). Ihre Prosa
wird einmal mit der alten Fassung erzeugt und von beiden benutzt.

Titel laufen mehrfach (`--titel-laeufe`), weil eine erfundene Wertung nur gelegentlich auftritt.

Pruefungen, beide ohne Urteil:
* Anrede: nennt „Herr/Frau X“ einen Vornamen? X gilt als Vorname, wenn im Text „X Nachname“
  steht und nirgends „Herr/Frau X“.
* Titel: Woerter (ab 5 Buchstaben), deren Stamm (erste 5 Buchstaben) im Diktat nicht vorkommt --
  ein Hinweis auf Hinzugefuegtes, etwa eine Wertung. Die Liste wird gelesen, nicht nur gezaehlt.

Aufruf:
    uv run python vergleich_115_116.py --alt <sandbox>\\kandidat.K1.v7mitTitel.de.json \\
        --neu <sandbox>\\kandidat.K1.v8.de.json --korpus <eval>\\corpus.v3.json \\
        --basis <eval>\\messlauf73f_gen.jsonl --fixtures ..\\..\\tests\\fixtures\\dictations \\
        --out <eval>\\vergleich_115_116.jsonl
"""

from __future__ import annotations

import argparse
import json
import re
import threading
from concurrent.futures import ThreadPoolExecutor
from dataclasses import asdict
from pathlib import Path

from messlauf import EMAIL, MAX_TOKENS, MODEL, user_message
from pilot import append_jsonl, chat, read_jsonl

GREETING = re.compile(r"^(Guten Tag|Hallo|Sehr geehrte[rs]?|Liebe[rs]?)\b.*", re.MULTILINE)
PERSON = re.compile(r"\b(Herrn?|Frau)\s+([A-ZÄÖÜ][\wäöüß-]+)")
WORD = re.compile(r"[A-Za-zÄÖÜäöüß]{5,}")


def uses_first_name(greeting: str, text: str) -> bool:
    for _, name in PERSON.findall(greeting):
        called_formally = re.search(rf"\b(Herrn?|Frau)\s+{re.escape(name)}\b", text)
        followed_by_surname = re.search(rf"\b{re.escape(name)}\s+[A-ZÄÖÜ][a-zäöüß]+", text)
        if followed_by_surname and not called_formally:
            return True
    return False


def added_words(title: str, transcript: str) -> list[str]:
    stems = {w[:5].lower() for w in WORD.findall(transcript)}
    return [w for w in WORD.findall(title) if w[:5].lower() not in stems]


def greeting_of(mail: str) -> str:
    match = GREETING.search(mail)
    return match.group(0).strip() if match else ""


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--alt", type=Path, required=True)
    parser.add_argument("--neu", type=Path, required=True)
    parser.add_argument("--korpus", type=Path, required=True)
    parser.add_argument("--basis", type=Path, required=True)
    parser.add_argument("--fixtures", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--titel-laeufe", type=int, default=3)
    parser.add_argument("--mail-laeufe", type=int, default=2)
    parser.add_argument("--workers", type=int, default=6)
    args = parser.parse_args(argv)

    variants = {
        "alt": json.loads(args.alt.read_text(encoding="utf-8")),
        "neu": json.loads(args.neu.read_text(encoding="utf-8")),
    }
    corpus = json.loads(args.korpus.read_text(encoding="utf-8"))
    items = [{"id": i["id"], "text": i["text"]} for i in (corpus["items"] if isinstance(corpus, dict) else corpus)]
    items += [{"id": f"fixture:{p.stem}", "text": p.read_text(encoding="utf-8").strip()}
              for p in sorted(args.fixtures.glob("D*.txt"))]
    prose = {r["item_id"]: r["output"] for r in read_jsonl(args.basis)
             if r.get("candidate") == "K1" and r.get("section") == "prosePrompt" and r.get("output")}

    done = {r["key"]: r for r in read_jsonl(args.out) if not r.get("error")}
    # Worker threads append concurrently; unguarded, two records interleaved into one broken
    # line, and read_jsonl stops at the first broken line -- a resume then re-paid the rest.
    write_lock = threading.Lock()

    def run(key: str, system: str, user: str, max_tokens: int, meta: dict) -> dict:
        if key in done:
            return done[key]
        call = chat(MODEL, system, user, max_tokens=max_tokens)
        row = dict(meta, key=key, output=call.text, error=call.error, usage=asdict(call))
        with write_lock:
            append_jsonl(args.out, row)
        return row

    alt = variants["alt"]
    missing = [i for i in items if i["id"] not in prose]
    with ThreadPoolExecutor(args.workers) as pool:
        for item, row in zip(missing, pool.map(lambda i: run(
                f"{i['id']}|prosa", alt["systemMessage"], user_message(alt, "prosePrompt", i["text"]), 20000,
                {"item_id": i["id"], "kind": "prosa"}), missing)):
            prose[item["id"]] = row["output"]

        tasks = []
        for name, prompts in variants.items():
            for item in items:
                for n in range(args.titel_laeufe):
                    tasks.append((f"{item['id']}|{name}|titel|{n}", prompts["systemMessage"],
                                  user_message(prompts, "title", item["text"]), MAX_TOKENS["title"],
                                  {"item_id": item["id"], "variant": name, "kind": "titel"}))
                for n in range(args.mail_laeufe):
                    tasks.append((f"{item['id']}|{name}|mail|{n}", prompts["systemMessage"],
                                  user_message(prompts, EMAIL, item["text"], prose=prose[item["id"]]), MAX_TOKENS[EMAIL],
                                  {"item_id": item["id"], "variant": name, "kind": "mail"}))
        rows = list(pool.map(lambda t: run(*t), tasks))

    text_of = {i["id"]: i["text"] for i in items}
    report: dict[str, dict] = {}
    for name in variants:
        titles = [r for r in rows if r.get("variant") == name and r["kind"] == "titel" and r["output"]]
        mails = [r for r in rows if r.get("variant") == name and r["kind"] == "mail" and r["output"]]
        flagged_titles = [(r["item_id"], r["output"], added_words(r["output"], text_of[r["item_id"]]))
                          for r in titles if added_words(r["output"], text_of[r["item_id"]])]
        wrong = [(r["item_id"], greeting_of(r["output"])) for r in mails
                 if uses_first_name(greeting_of(r["output"]), prose[r["item_id"]] + "\n" + text_of[r["item_id"]])]
        report[name] = {
            "titel": len(titles),
            "titel_mit_fremden_woertern": len(flagged_titles),
            "mails": len(mails),
            "anrede_mit_vorname": len(wrong),
            "fehler": sum(1 for r in rows if r.get("variant") == name and r.get("error")),
            "ausgabe_token": sum(r["usage"]["output_tokens"] for r in rows if r.get("variant") == name),
            "beispiele_titel": flagged_titles,
            "beispiele_anrede": wrong,
            "anreden": sorted({greeting_of(r["output"]) for r in mails}),
        }
    summary = args.out.with_suffix(".bericht.json")
    summary.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    for name, r in report.items():
        print(f"{name}: Titel {r['titel']}, davon mit fremden Woertern {r['titel_mit_fremden_woertern']} · "
              f"Mails {r['mails']}, Anrede mit Vorname {r['anrede_mit_vorname']} · Fehler {r['fehler']} · "
              f"Ausgabe-Token {r['ausgabe_token']}")
    print(f"Bericht: {summary}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
