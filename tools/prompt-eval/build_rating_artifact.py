"""Baut die Blindbewertungsseite als Artefakt-HTML.

Unterschied zur lokalen Seite: ein Artefakt darf keine Datei herunterladen -- die
Sandbox unterbindet das. Die Noten wandern deshalb in den Datenspeicher der Seite
(`capabilities: {db: {}}`) und werden von dort ausgelesen.

Nebeneffekt, der hier wichtiger ist als die Umgehung: jede Note wird sofort gespeichert.
Dreissig Bewertungen sind eine dreiviertel Stunde Arbeit, und die soll man in mehreren
Sitzungen erledigen koennen, ohne am Ende alles zu verlieren.

Aufruf:
    uv run python build_rating_artifact.py --out <ziel.html>
"""

from __future__ import annotations

import argparse
import html
import json
import os
import random
from pathlib import Path

EVAL = Path(os.environ["USERPROFILE"]) / "Documents" / "Johann" / "prompt-sandbox" / "eval"

def load_template() -> str:
    """Laedt die Seitenvorlage aus `rating_template.html`.

    Sie steht bewusst in einer eigenen Datei: eingebettet in einen Python-String war sie
    unlesbar, und die Bedienoberflaeche ist inzwischen der Teil, an dem am haeufigsten
    gearbeitet wird -- die erste Fassung ging beim Wiederoeffnen verloren, weil sie nur eine
    Speicherebene hatte und ihre Statusanzeige auf dem iPhone unter der Home-Leiste lag.
    """
    return (Path(__file__).parent / "rating_template.html").read_text(encoding="utf-8")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--roh", type=Path, default=EVAL / "pilot_raw.json")
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--stichprobe", type=int, default=30)
    parser.add_argument("--seed", type=int, default=20260913)
    parser.add_argument(
        "--praefix",
        default="k",
        help="Praefix der Dokument-IDs. Eine neue Bewertungsrunde braucht ein neues "
        "Praefix, sonst holt die Seite die Noten der VORIGEN Runde auf die neuen Texte "
        "zurueck -- gleiche ID, anderer Inhalt.",
    )
    args = parser.parse_args(argv)

    payload = json.loads(args.roh.read_text(encoding="utf-8"))
    transcripts = payload["transkripte"]

    # Dieselbe Regel wie in der Auswertung: leere Transkripte fliegen raus, und die Auswahl ist
    # eine echte Zufallsstichprobe -- keine Auswahl nach Schwierigkeit. Waehlte man die harten
    # Faelle, waere die spaetere PPI-Korrektur ungueltig; sie setzt i.i.d. voraus.
    rows = [
        r
        for r in payload["zeilen"]
        if r.get("output") and len((transcripts.get(r["item_id"]) or "").strip()) >= 20
    ]
    rng = random.Random(args.seed)

    # ⚠ Hoechstens eine Ausgabe je Diktat.
    #
    # Die erste Fassung zog frei aus allen Ausgaben. Bei 17 Diktaten und sechs Ausgaben je
    # Diktat landeten dadurch drei Bewertungen auf DEMSELBEN Text -- der Mensch las dreimal
    # dasselbe und vergab dreimal dieselbe Note. Das ist doppelt schaedlich: es verschwendet
    # die knappste Ressource im Projekt, und es taeuscht Stichprobenumfang vor, den es nicht
    # gibt, weil drei Noten zum selben Diktat fast dieselbe Information tragen.
    #
    # Gezogen wird deshalb zweistufig: erst das Diktat, dann eine Ausgabe darin -- beides
    # gleichverteilt. Das bleibt eine echte Wahrscheinlichkeitsauswahl (jede Ausgabe hat eine
    # bekannte, positive Chance), taugt also weiter fuer die spaetere PPI-Korrektur, und es
    # deckt bei gleichem Leseaufwand die dreifache Zahl an Diktaten ab.
    by_item: dict[str, list[dict]] = {}
    for row in rows:
        by_item.setdefault(row["item_id"], []).append(row)

    items = sorted(by_item)
    rng.shuffle(items)
    picked = [rng.choice(by_item[item]) for item in items[: args.stichprobe]]
    rng.shuffle(picked)

    if len(picked) < args.stichprobe:
        print(
            f"⚠ nur {len(picked)} verschiedene Diktate verfuegbar, "
            f"{args.stichprobe} angefragt -- Stichprobe entsprechend kleiner"
        )

    daten = []
    for index, row in enumerate(picked):
        transcript = transcripts[row["item_id"]]
        words = len(transcript.split())
        daten.append(
            {
                # Dokument-IDs duerfen keine Trennzeichen wie "|" enthalten, deshalb eine
                # laufende Nummer; der echte Schluessel steht als Feld daneben.
                "docId": f"{args.praefix}{index:03d}",
                "schluessel": f"{row['item_id']}|{row['variant_id']}|{row['run_index']}",
                "woerter": words,
                "transkript": html.escape(transcript[:6000]),
                "ausgabe": html.escape(row["output"][:8000]),
            }
        )

    page = load_template().replace("__DATEN__", json.dumps(daten, ensure_ascii=False))
    page = page.replace("__N__", str(len(daten)))
    page = page.replace("__PRAEFIX__", args.praefix)
    page = page.replace("__SPEICHERSCHLUESSEL__", f"johann-noten-{args.praefix}")
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(page, encoding="utf-8")

    print(f"{len(daten)} Ausgaben, {len(page) // 1024} KB -> {args.out}")
    herkunft: dict[str, int] = {}
    for row in picked:
        key = row["item_id"].split(":")[0]
        herkunft[key] = herkunft.get(key, 0) + 1
    print("Herkunft: " + ", ".join(f"{k} {v}" for k, v in sorted(herkunft.items())))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
