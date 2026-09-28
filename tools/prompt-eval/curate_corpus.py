"""Bereinigt den Korpus und misst, wie echt die synthetischen Diktate klingen.

Zwei Aufgaben, die zusammengehoeren.

BEREINIGEN. Der gewachsene Korpus enthielt 60 Elemente, von denen ein Drittel nichts misst:
Testaufnahmen ("Test, Test, Funktioniert das hier?"), ein Diktat mit leerem Transkript und --
am schlimmsten -- sechs *textgleiche* Kopien desselben "Hallo, hallo"-Schnipsels. Bei der
ersten menschlichen Blindbewertung entfielen drei von acht Noten auf ein und denselben Text.
Das ist keine Kleinigkeit: die knappste Ressource im Projekt ist die Lesezeit des Menschen,
und sie wurde an Material verschwendet, bei dem eine Zusammenfassung gar nicht falsch sein
kann.

MESSEN. Die selbst erfundenen Diktate sind zu sauber. Gemessen an den echten:

    Merkmal                 echt    erfunden
    Fuellwoerter            1,9 %      0,0 %
    Neuansaetze/1000 W      1,1        0,0
    Zahlen                  0,8 %      1,7 %

Kein einziges Fuellwort, kein einziger abgebrochener Satz -- und mehr Zahlen, als echte
Sprecher nennen. Ein Befund, der auf glattem Text haelt, muss auf echtem Gestammel nicht
halten. Dieses Modul stellt die Messung bereit, damit neue Diktate dagegen geprueft werden
koennen statt nach Gefuehl geschrieben zu werden.

Aufruf:
    uv run python curate_corpus.py --bericht          # nur zeigen, nichts schreiben
    uv run python curate_corpus.py --schreiben        # corpus.curated.json erzeugen
"""

from __future__ import annotations

import argparse
import collections
import hashlib
import json
import os
import re
import statistics
from dataclasses import dataclass
from pathlib import Path

EVAL = Path(os.environ["USERPROFILE"]) / "Documents" / "Johann" / "prompt-sandbox" / "eval"

#: Unter dieser Wortzahl misst ein Diktat nichts ueber Prompts.
MIN_WORDS = 25

#: Ab diesem Anteil eines einzigen Wortes ist der Text eine Tonprobe, kein Diktat.
MAX_WORD_SHARE = 0.20

#: Wendungen, die eine Aufnahmeprobe verraten.
PROBE_MARKERS = ("test, test", "hallo, hallo", "funktioniert das", "das, das, das")

FILLER = re.compile(
    r"\b(also|halt|irgendwie|sozusagen|quasi|ähm|äh|genau|eben|nh|ne|oder so|und so)\b", re.I
)
NUMBERS = re.compile(
    r"\b\d+([.,]\d+)?\b|\b(ein|zwei|drei|vier|fünf|sechs|sieben|acht|neun|zehn|zwölf|"
    r"zwanzig|dreißig|hundert)\b",
    re.I,
)


@dataclass(frozen=True)
class Profile:
    """Wie gesprochen klingt ein Text?"""

    words: int
    filler_pct: float
    restarts_per_1000: float
    sentence_len: float
    number_pct: float


def restarts(text: str) -> int:
    """Zaehlt neu angesetzte Saetze ueber wiederholte Dreiwortfolgen in kurzem Abstand.

    Echte Sprecher setzen Saetze neu an: "der soll eigentlich nur anzeigen, der soll nur
    anzeigen". Eine Wiederholung derselben drei Woerter innerhalb von zwoelf Woertern faengt
    dieses Muster, ohne auf normale Wiederholung anzuspringen.
    """
    words = [w.lower().strip(".,") for w in text.split()]
    trigrams = [" ".join(words[i : i + 3]) for i in range(len(words) - 2)]
    positions: dict[str, list[int]] = collections.defaultdict(list)
    for index, gram in enumerate(trigrams):
        positions[gram].append(index)
    return sum(
        1
        for spots in positions.values()
        if len(spots) > 1 and any(spots[i + 1] - spots[i] <= 12 for i in range(len(spots) - 1))
    )


def profile(text: str) -> Profile:
    words = text.split()
    count = max(len(words), 1)
    sentences = [s for s in re.split(r"[.!?]+", text) if s.strip()]
    return Profile(
        words=len(words),
        filler_pct=round(100 * len(FILLER.findall(text)) / count, 2),
        restarts_per_1000=round(1000 * restarts(text) / count, 2),
        sentence_len=round(
            statistics.fmean(len(s.split()) for s in sentences) if sentences else 0.0, 1
        ),
        number_pct=round(100 * len(NUMBERS.findall(text)) / count, 2),
    )


def reject_reason(text: str) -> str | None:
    """Warum ein Element nicht in den Korpus gehoert -- oder None, wenn es taugt."""
    stripped = text.strip()
    if not stripped:
        return "leeres Transkript"
    words = [w.lower().strip(".,!?") for w in stripped.split()]
    if len(words) < MIN_WORDS:
        return f"zu kurz ({len(words)} Wörter)"
    share = collections.Counter(words).most_common(1)[0][1] / len(words)
    if share > MAX_WORD_SHARE:
        return f"Wortwiederholung {share:.0%}"
    lowered = stripped.lower()
    if any(marker in lowered for marker in PROBE_MARKERS):
        return "Aufnahmeprobe"
    return None


def curate(items: list[dict]) -> tuple[list[dict], list[tuple[str, str]]]:
    """Filtert und dedupliziert. Gibt die behaltenen Elemente und die Begruendungen zurueck."""
    kept: list[dict] = []
    dropped: list[tuple[str, str]] = []
    seen: dict[str, str] = {}
    for item in items:
        text = item.get("text", "")
        reason = reject_reason(text)
        if reason:
            dropped.append((item["id"], reason))
            continue
        digest = hashlib.md5(text.strip().lower().encode()).hexdigest()
        if digest in seen:
            dropped.append((item["id"], f"textgleich mit {seen[digest]}"))
            continue
        seen[digest] = item["id"]
        kept.append(item)
    return kept, dropped


def source_of(item_id: str) -> str:
    return item_id.split(":")[0]


def report(items: list[dict], kept: list[dict], dropped: list[tuple[str, str]]) -> None:
    print(f"Korpus: {len(items)} Elemente  ->  {len(kept)} behalten, {len(dropped)} verworfen\n")

    by_reason = collections.Counter(reason.split(" ")[0] for _, reason in dropped)
    print("Verworfen nach Grund:")
    for reason, count in by_reason.most_common():
        print(f"   {reason:<22}{count:>4}")

    print("\nBestand nach Herkunft:")
    before = collections.Counter(source_of(i["id"]) for i in items)
    after = collections.Counter(source_of(i["id"]) for i in kept)
    for source in sorted(before):
        print(f"   {source:<12}{before[source]:>4}  ->{after.get(source, 0):>4}")

    real = [i for i in kept if source_of(i["id"]) != "erfunden"]
    invented = [i for i in kept if source_of(i["id"]) == "erfunden"]
    if not (real and invented):
        return

    print("\nWie gesprochen klingen die Texte?")
    print(f"   {'Merkmal':<24}{'echt':>10}{'erfunden':>12}{'Ziel':>10}")
    fields = [
        ("words", "Wörter (Median)", "{:.0f}"),
        ("filler_pct", "Füllwörter %", "{:.2f}"),
        ("restarts_per_1000", "Neuansätze je 1000 W", "{:.2f}"),
        ("sentence_len", "Wörter je Satz", "{:.1f}"),
        ("number_pct", "Zahlen %", "{:.2f}"),
    ]
    for key, label, fmt in fields:
        a = statistics.median(getattr(profile(i["text"]), key) for i in real)
        b = statistics.median(getattr(profile(i["text"]), key) for i in invented)
        print(f"   {label:<24}{fmt.format(a):>10}{fmt.format(b):>12}{fmt.format(a):>10}")
    print("\n   Die Zielspalte ist der echte Wert -- neue Diktate werden dagegen geprüft.")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--korpus", type=Path, default=EVAL / "corpus.json")
    parser.add_argument("--out", type=Path, default=EVAL / "corpus.curated.json")
    parser.add_argument("--schreiben", action="store_true")
    parser.add_argument("--details", action="store_true", help="jedes verworfene Element nennen")
    args = parser.parse_args(argv)

    raw = json.loads(args.korpus.read_text(encoding="utf-8"))
    items = raw["items"] if isinstance(raw, dict) else raw
    kept, dropped = curate(items)
    report(items, kept, dropped)

    if args.details:
        print("\nIm Einzelnen verworfen:")
        for item_id, reason in dropped:
            print(f"   {item_id:<40}{reason}")

    if args.schreiben:
        payload = {"items": kept, "verworfen": [{"id": i, "grund": r} for i, r in dropped]}
        args.out.write_text(
            json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
        )
        print(f"\ngeschrieben: {args.out}")
    else:
        print("\n(Probelauf -- nichts geschrieben. Mit --schreiben festhalten.)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
