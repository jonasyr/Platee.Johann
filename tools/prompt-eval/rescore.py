"""Bewertet gespeicherte Ausgaben mit einem neuen Messverfahren -- ohne neue Erzeugung.

Der Pilotlauf hat gezeigt, dass die erste Rubrik nicht trennt: Klarheit bekam in 306 von 306
Faellen eine 5, Vollstaendigkeit in 92,8 %, Treue in 78,8 %. Ein Lineal mit einer einzigen
Markierung.

Dieses Skript laesst zwei Nachfolger gegeneinander antreten, beide auf **denselben bereits
bezahlten** 102 Ausgaben:

    absolut    -- Rubrik v2 mit verschaerften Ankern, Klarheit ersetzt
    paarweise  -- Thurstone: welche der beiden Ausgaben ist treuer? Beide Reihenfolgen.

Die paarweise Form ist der eigentliche Kandidat. Vergleiche sind nachweislich zuverlaessiger
als Einzelnoten, und sie koennen keinen Deckeneffekt haben -- es gibt immer einen Gewinner oder
ein ausdrueckliches Unentschieden. Der Preis sind mehr Aufrufe je Aussage.

Aufruf:
    uv run python rescore.py --modus absolut
    uv run python rescore.py --modus paarweise
    uv run python rescore.py --vergleich          # wertet beide aus, ohne neue Aufrufe
"""

from __future__ import annotations

import argparse
import collections
import json
import random
import statistics
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

import pilot  # Aufrufmechanik, Protokollierung und Pfade wiederverwenden

DIMENSIONS_V2 = ("treue", "vollstaendigkeit", "nacharbeit")

JUDGE_SYSTEM = (
    "Du bewertest automatisch erzeugte Zusammenfassungen von Diktaten. Du bist streng: "
    "die Hoechstnote ist fuer fehlerfreie Arbeit reserviert, nicht fuer brauchbare. "
    "Du antwortest ausschliesslich mit einem JSON-Objekt."
)

#: Rubrik v2. Der Unterschied zur ersten Fassung ist nicht die Formulierung, sondern die Lage
#: der Latte: in v1 hiess 5 "jede Aussage ist belegt" -- das ist bei ordentlicher Arbeit der
#: Normalfall und keine Spitzenleistung. In v2 verlangt die 5 Genauigkeit bis in die Nuance.
#: "Klarheit" ist ersetzt: sie variierte nie. An ihre Stelle tritt die Frage, die im Alltag
#: wirklich zaehlt -- wie viel muss ich noch anfassen, bevor ich das verschicke?
ABSOLUTE_TEMPLATE = """Bewerte die Zusammenfassung gegen das Transkript. Drei Dimensionen, je 1 bis 5.

Die Hoechstnote ist fuer fehlerfreie Arbeit reserviert, nicht fuer brauchbare.
Wenn du schwankst, nimm die NIEDRIGERE Note.

TREUE — stimmt alles, bis in die Nuance?
  5 = keine Abweichung. Beispiel: "Frist ist Ende des Monats" -> "Frist: Ende des Monats"
  4 = eine Nuance verschoben, keine Tatsache falsch.
      Beispiel: "ich glaube das war so" -> "das war so" (die Unsicherheit faellt weg)
  3 = eine Tatsache ergaenzt, umgedeutet oder sinnentstellend weggelassen.
      Beispiel: "der Kollege wollte sich melden" -> "der Kollege meldet sich morgen"
  2 = mehrere solche Stellen, etwa ein erfundener Termin UND ein falscher Name
  1 = eine zentrale Aussage ist erfunden. Beispiel: "wir haben ueber das Projekt
      gesprochen" -> "Es wurde beschlossen, das Projekt zu beenden"

VOLLSTAENDIGKEIT — ist alles Handlungsrelevante da?
  5 = jede Entscheidung, Aufgabe, Zahl, Frist und namentliche Zuordnung kommt vor
  4 = eine Nebeninformation fehlt. Beispiel: "und bring bitte den Schluessel mit" fehlt
  3 = eine handlungsrelevante Information fehlt. Beispiel: "Abgabe ist Freitag" fehlt --
      ohne die Frist handelt jemand falsch
  2 = mehrere handlungsrelevante Informationen fehlen, etwa Frist UND Zustaendigkeit
  1 = der Kern des Diktats fehlt. Beispiel: Diktat ueber einen Wasserschaden, die
      Zusammenfassung erwaehnt nur ein Telefonat

NACHARBEIT — wie viel muesste ein Mensch anfassen, bevor er das so verschickt?
  5 = nichts, so abschicken
  4 = ein einzelnes Wort. Beispiel: "der Mieter" muss "die Mieterin" heissen
  3 = ein Satz muss umgeschrieben werden. Beispiel: "Bezueglich der besprochenen
      Thematik wurde vereinbart, dass..." -- so schreibt das niemand
  2 = mehrere solche Stellen, dazu eine unpassende Gliederung
  1 = schneller selbst geschrieben als korrigiert

Antworte genau so:
{{"treue": <1-5>, "vollstaendigkeit": <1-5>, "nacharbeit": <1-5>, "begruendung": "<ein Satz>"}}

TRANSKRIPT:
{transcript}

ZUSAMMENFASSUNG:
{output}"""

#: Paarweiser Vergleich. Kein Deckeneffekt moeglich -- gefragt ist eine Rangaussage, keine Note.
#: Unentschieden ist ausdruecklich erlaubt, damit der Richter nicht zu einer Entscheidung
#: gezwungen wird, die er nicht hat; ein erzwungener Muenzwurf waere schlimmer als ein
#: ehrliches Remis.
PAIRWISE_TEMPLATE = """Zwei Zusammenfassungen desselben Diktats. Welche ist dem Transkript treuer?

Treu heisst: keine erfundene, umgedeutete oder sinnentstellend weggelassene Aussage; Zahlen,
Fristen, Namen und der Sicherheitsgrad einer Aussage stimmen.

Antworte genau so:
{{"besser": "A" | "B" | "unentschieden", "begruendung": "<ein Satz>"}}

Waehle "unentschieden" nur, wenn du wirklich keinen Unterschied in der Treue siehst.

TRANSKRIPT:
{transcript}

ZUSAMMENFASSUNG A:
{a}

ZUSAMMENFASSUNG B:
{b}"""


def parse_absolute(text: str) -> dict | None:
    start, end = text.find("{"), text.rfind("}")
    if start < 0 or end <= start:
        return None
    try:
        value = json.loads(text[start : end + 1])
    except json.JSONDecodeError:
        return None
    return value if all(k in value for k in DIMENSIONS_V2) else None


def parse_pairwise(text: str) -> str | None:
    start, end = text.find("{"), text.rfind("}")
    if start < 0 or end <= start:
        return None
    try:
        value = json.loads(text[start : end + 1])
    except json.JSONDecodeError:
        return None
    choice = str(value.get("besser", "")).strip().lower()
    return choice if choice in ("a", "b", "unentschieden") else None


def rate_absolute(job: tuple[dict, str, int]) -> dict:
    row, transcript, rep = job
    call = pilot.chat(
        pilot.JUDGE_MODEL,
        JUDGE_SYSTEM,
        ABSOLUTE_TEMPLATE.format(transcript=transcript[:12000], output=row["output"][:12000]),
        max_tokens=1200,
    )
    return {
        "key": pilot.gen_key(row),
        "rep": rep,
        "scores": parse_absolute(call.text),
        "usage": {"out": call.output_tokens, "think": call.reasoning_tokens},
    }


def rate_pairwise(job: tuple[str, str, dict, dict, int, bool]) -> dict:
    """Ein Vergleich. `swapped` dreht die Reihenfolge -- beide Richtungen werden gefragt."""
    item_id, transcript, first, second, rep, swapped = job
    call = pilot.chat(
        pilot.JUDGE_MODEL,
        JUDGE_SYSTEM,
        PAIRWISE_TEMPLATE.format(
            transcript=transcript[:12000],
            a=first["output"][:12000],
            b=second["output"][:12000],
        ),
        max_tokens=800,
    )
    choice = parse_pairwise(call.text)
    # Zurueckuebersetzen auf die Varianten, unabhaengig davon, wie herum gefragt wurde.
    winner: str | None = None
    if choice == "unentschieden":
        winner = "unentschieden"
    elif choice == "a":
        winner = first["variant_id"]
    elif choice == "b":
        winner = second["variant_id"]
    return {
        "item_id": item_id,
        "run_index": rep,
        "swapped": swapped,
        "gefragt_als_A": first["variant_id"],
        "sieger": winner,
        "usage": {"out": call.output_tokens, "think": call.reasoning_tokens},
    }


def report_absolute(records: list[dict]) -> None:
    valid = [r for r in records if r.get("scores")]
    print(f"\nRUBRIK v2 — {len(valid)} von {len(records)} Urteile lesbar")
    print(f"   {'Dimension':<18}{'1':>6}{'2':>6}{'3':>6}{'4':>6}{'5':>6}{'Decke':>9}")
    for dim in DIMENSIONS_V2:
        counts = collections.Counter(int(r["scores"][dim]) for r in valid)
        total = sum(counts.values()) or 1
        top = 100 * counts.get(5, 0) / total
        row = f"   {dim:<18}"
        for value in (1, 2, 3, 4, 5):
            row += f"{counts.get(value, 0):>6}"
        print(row + f"{top:>8.1f}%")
    print("   Zum Vergleich Rubrik v1: Treue 78,8 % · Vollstaendigkeit 92,8 % · Klarheit 100 %")


def report_pairwise(records: list[dict]) -> None:
    print(f"\nPAARWEISER VERGLEICH — {len(records)} Urteile")
    by_pair: dict[tuple[str, int], list[dict]] = collections.defaultdict(list)
    for record in records:
        by_pair[(record["item_id"], record["run_index"])].append(record)
    both = [v for v in by_pair.values() if len(v) == 2]

    # ⚠ Rohe Siege zu zaehlen waere falsch. Jedes Paar wird zweimal gefragt, einmal in jeder
    # Reihenfolge -- wer die Einzelurteile addiert, zaehlt widerspruechliche Paare doppelt und
    # verwechselt Positionsneigung mit Qualitaet. Gewertet wird nur, wenn beide Richtungen
    # dasselbe sagen; Widersprueche sind Remis.
    consistent: collections.Counter[str] = collections.Counter()
    contradictions = 0
    mutual_ties = 0
    for pair in both:
        first, second = pair[0]["sieger"], pair[1]["sieger"]
        if first != second:
            contradictions += 1
        elif first == "unentschieden":
            mutual_ties += 1
        elif first:
            consistent[first] += 1

    print(f"   Paare mit beiden Richtungen: {len(both)}")
    print(f"   beidseitig entschieden:      {sum(consistent.values())}")
    print(f"   beidseitig unentschieden:    {mutual_ties}")
    print(f"   widerspruechlich -> Remis:   {contradictions}")
    for variant, count in consistent.most_common():
        print(f"      {variant:<34}{count:>4} Siege")

    if both:
        agree = len(both) - contradictions
        print(
            f"\n   Reihenfolge getauscht: gleiches Urteil in {agree}/{len(both)} "
            f"({100 * agree / len(both):.0f} %)"
        )
        print("   Zum Vergleich: fuer GPT-4 sind 65 % dokumentiert -- ein Drittel der Urteile")
        print("   kippt allein durch die Reihenfolge.")

    decided = [r for r in records if r["sieger"] and r["sieger"] != "unentschieden"]
    first_pos = sum(1 for r in decided if r["sieger"] == r["gefragt_als_A"])
    if decided:
        print(
            f"   Sieger stand an erster Stelle: {first_pos}/{len(decided)} "
            f"({100 * first_pos / len(decided):.0f} %) — 50 % waere neutral"
        )


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--modus", choices=("absolut", "paarweise"), help="was gemessen wird")
    parser.add_argument("--vergleich", action="store_true", help="nur auswerten, keine Aufrufe")
    parser.add_argument("--reps", type=int, default=2, help="Wiederholungen je Urteil")
    parser.add_argument("--workers", type=int, default=3)
    parser.add_argument("--roh", type=Path, default=pilot.EVAL / "pilot_raw.json")
    parser.add_argument(
        "--rubrik",
        default="v2b",
        help="Kennung des Rubrikstands; landet im Dateinamen. v2b = v2 mit Minibeispielen.",
    )
    args = parser.parse_args(argv)

    payload = json.loads(args.roh.read_text(encoding="utf-8"))
    rows = [r for r in payload["zeilen"] if r.get("output")]
    transcripts = payload["transkripte"]

    # Ausgabedateien haengen an Rohdaten UND Rubrikstand. Feste Namen haben schon einmal zwei
    # Laeufe in einer Datei vermischt; hier kaeme hinzu, dass bereits vorhandene Urteile als
    # "erledigt" uebersprungen wuerden -- eine geaenderte Rubrik liefe dann gar nicht neu.
    stem = args.roh.stem
    abs_path = args.roh.with_name(f"{stem}_absolut_{args.rubrik}.jsonl")
    pair_path = args.roh.with_name(f"{stem}_paarweise.jsonl")
    if stem == "pilot_raw" and args.rubrik == "v2":
        abs_path = pilot.EVAL / "rescore_absolut.jsonl"  # Altbestand des ersten Pilotlaufs
        pair_path = pilot.EVAL / "rescore_paarweise.jsonl"

    if args.vergleich:
        absolute = pilot.read_jsonl(abs_path)
        pairwise = pilot.read_jsonl(pair_path)
        if absolute:
            report_absolute(absolute)
        if pairwise:
            report_pairwise(pairwise)
        if not absolute and not pairwise:
            print("Noch nichts bewertet. Zuerst --modus absolut bzw. --modus paarweise.")
        return 0

    if args.modus == "absolut":
        done = {f"{r['key']}|{r['rep']}" for r in pilot.read_jsonl(abs_path)}
        jobs = [
            (row, transcripts[row["item_id"]], rep)
            for row in rows
            for rep in range(args.reps)
            if f"{pilot.gen_key(row)}|{rep}" not in done
        ]
        print(f"Rubrik v2: {len(jobs)} Urteile (bereits vorhanden: {len(done)})")
        with ThreadPoolExecutor(max_workers=args.workers) as pool:
            for record in pool.map(rate_absolute, jobs):
                pilot.append_jsonl(abs_path, record)
        report_absolute(pilot.read_jsonl(abs_path))
        return 0

    if args.modus == "paarweise":
        by_item: dict[tuple[str, int], dict[str, dict]] = collections.defaultdict(dict)
        for row in rows:
            by_item[(row["item_id"], row["run_index"])][row["variant_id"]] = row

        rng = random.Random(20260913)
        done = {
            f"{r['item_id']}|{r['run_index']}|{r['swapped']}"
            for r in pilot.read_jsonl(pair_path)
        }
        jobs = []
        for (item_id, run_index), variants in sorted(by_item.items()):
            if len(variants) != 2:
                continue
            left, right = sorted(variants)
            for swapped in (False, True):
                if f"{item_id}|{run_index}|{swapped}" in done:
                    continue
                first, second = (
                    (variants[right], variants[left])
                    if swapped
                    else (variants[left], variants[right])
                )
                jobs.append(
                    (item_id, transcripts[item_id], first, second, run_index, swapped)
                )
        rng.shuffle(jobs)
        print(f"Paarweise: {len(jobs)} Vergleiche (bereits vorhanden: {len(done)})")
        with ThreadPoolExecutor(max_workers=args.workers) as pool:
            for record in pool.map(rate_pairwise, jobs):
                pilot.append_jsonl(pair_path, record)
        report_pairwise(pilot.read_jsonl(pair_path))
        return 0

    parser.error("--modus oder --vergleich angeben")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
