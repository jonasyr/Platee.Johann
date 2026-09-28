"""Erzeugt corpus.v3: Dublette aufgeloest, drei lange Diktate ergaenzt (#73).

Zwei Maengel von corpus.v2, beide am 15.09.2026 gefunden:

1. **Dieselbe Aufnahme dreifach.** `aufnahme:2026_02_05_12_37_24.mp3` wurde spaeter noch
   zweimal ueber das Archiv transkribiert (`260317_001`, `260910_003`; gleiche MP3, 4.967 KB).
   `curate_corpus.py` dedupliziert nur *textgleiche* Elemente -- zwei Laeufe der
   Spracherkennung weichen aber in Kleinigkeiten ab. Der Eimer "lang" hatte dadurch acht
   verschiedene Diktate statt zehn, und eines davon zaehlte dreifach. Behalten wird die
   urspruengliche Aufnahme.
2. **Kein oberes Ende.** Das laengste Transkript hat 474 Woerter. Siehe `dictations_long.py`.

Die Dubletten stehen als ausdrueckliche Liste im Code statt als Automatik, weil eine falsch
erkannte Dublette ein echtes Diktat still loeschen wuerde. `near_duplicates` meldet dafuer
jeden weiteren Verdacht, damit die Liste nicht wieder veraltet.

Aufruf:
    uv run python build_corpus_v3.py --korpus <eval>\\corpus.v2.json --out <eval>\\corpus.v3.json
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from curate_corpus import EVAL
from dictations_long import DICTATIONS_LONG

_SAME_FILE = "dieselbe Aufnahme wie {keep}"
_REDICTATED = "dasselbe Diktat erneut aufgenommen wie {keep}"

#: (behalten, verwerfen, Grund). Der zweite Fall wurde erst von `near_duplicates` gefunden
#: (Jaccard 0,62): zwei verschiedene MP3s (479 und 404 KB), aber fast wortgleich diktiert.
SAME_RECORDING: tuple[tuple[str, str, str], ...] = (
    ("aufnahme:2026_02_05_12_37_24.mp3", "archiv:260317_001_f9b84030", _SAME_FILE),
    ("aufnahme:2026_02_05_12_37_24.mp3", "archiv:260910_003_c34b8a3e", _SAME_FILE),
    ("archiv:260317_005_552afa90", "archiv:260317_009_f7843aa6", _REDICTATED),
)

#: Ab diesem Jaccard-Wert auf Wort-Dreiergruppen gelten zwei Texte als Verdacht auf dieselbe
#: Aufnahme. Unabhaengige Diktate aus derselben erfundenen Welt liegen weit darunter.
NEAR_DUPLICATE_THRESHOLD = 0.35

#: Wie 02_build_corpus.py -- die Eimer muessen ueber Korpusversionen vergleichbar bleiben.
CHARS_PER_TOKEN = 4.3


def bucket_of(text: str) -> str:
    tokens = round(len(text.strip()) / CHARS_PER_TOKEN)
    if tokens < 40:
        return "winzig"
    if tokens < 150:
        return "kurz"
    if tokens < 400:
        return "mittel"
    return "lang"


def _shingles(text: str) -> set[str]:
    words = re.findall(r"\w+", text.lower())
    return {" ".join(words[i : i + 3]) for i in range(len(words) - 2)}


def near_duplicates(
    items: list[dict], threshold: float = NEAR_DUPLICATE_THRESHOLD
) -> list[tuple[str, str, float]]:
    """Paare mit auffaellig vielen gemeinsamen Wort-Dreiergruppen, staerkstes zuerst."""
    shingles = [(item["id"], _shingles(item["text"])) for item in items]
    pairs: list[tuple[str, str, float]] = []
    for index, (id_a, set_a) in enumerate(shingles):
        for id_b, set_b in shingles[index + 1 :]:
            union = set_a | set_b
            if not union:
                continue
            score = len(set_a & set_b) / len(union)
            if score >= threshold:
                pairs.append((id_a, id_b, round(score, 2)))
    return sorted(pairs, key=lambda pair: -pair[2])


def build(
    corpus: dict,
    extra: list[dict],
    duplicates: tuple[tuple[str, str, str], ...] = SAME_RECORDING,
) -> dict:
    """Gibt einen neuen Korpus zurueck; die Eingabe bleibt unveraendert."""
    items: list[dict] = corpus["items"]
    present = {item["id"] for item in items}

    listed = {ident for keep, drop, _ in duplicates for ident in (keep, drop)}
    missing = listed - present
    if missing:
        raise ValueError(
            f"Dublettenliste passt nicht mehr, nicht im Korpus: {', '.join(sorted(set(missing)))}"
        )

    dropped = {drop: reason.format(keep=keep) for keep, drop, reason in duplicates}
    kept = [item for item in items if item["id"] not in dropped]
    verworfen = list(corpus.get("verworfen", [])) + [
        {"id": drop, "grund": reason} for drop, reason in dropped.items()
    ]

    kept_ids = {item["id"] for item in kept}
    added: list[dict] = []
    for dictation in extra:
        if dictation["id"] in kept_ids:
            raise ValueError(f"Id doppelt: {dictation['id']}")
        kept_ids.add(dictation["id"])
        added.append(
            {
                "id": dictation["id"],
                "source": dictation["id"].split(":")[0],
                "bucket": bucket_of(dictation["text"]),
                "titel": dictation["titel"],
                "text": dictation["text"],
            }
        )

    return {"items": kept + added, "verworfen": verworfen}


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--korpus", type=Path, default=EVAL / "corpus.v2.json")
    parser.add_argument("--out", type=Path, required=True)
    args = parser.parse_args(argv)

    if args.out.resolve() == args.korpus.resolve():
        parser.error("--out darf nicht die Eingabe ueberschreiben")

    corpus = json.loads(args.korpus.read_text(encoding="utf-8"))
    result = build(corpus, DICTATIONS_LONG)

    suspects = near_duplicates(result["items"])
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    buckets: dict[str, int] = {}
    for item in result["items"]:
        buckets[item["bucket"]] = buckets.get(item["bucket"], 0) + 1
    print(f"{len(corpus['items'])} -> {len(result['items'])} Diktate  ->  {args.out}")
    print("Eimer: " + ", ".join(f"{k} {v}" for k, v in sorted(buckets.items())))
    if suspects:
        print("\n⚠ Weitere Verdachtsfaelle auf dieselbe Aufnahme (von Hand pruefen):")
        for id_a, id_b, score in suspects:
            print(f"   {score:.2f}  {id_a}  ~  {id_b}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
