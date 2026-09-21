"""Baut die Lesevergleich-Seite #73 (Schritt 3): R gegen K1, 8 Vorlagen x 2 Diktate = 16 Paare.

Frage je Paar: Ist eine der beiden Fassungen schlechter? Die Seiten A/B sind je Paar zufaellig
verteilt; welche Fassung R und welche K1 ist, steht **nur** in der Zuordnungsdatei neben der Seite,
nie im HTML. Deshalb fragt die Seite symmetrisch ("A schlechter" / "B schlechter" /
"gleichwertig"): Mit zufaelligen Seiten wuerde "Ist die rechte schlechter?" in der Haelfte der
Paare nach R statt nach K1 fragen.

Keine Noten, keine Hinweise auf erwartete Ergebnisse (stand-73 §4, Ankereffekt).

Auswahl je Vorlage (Soll-Profil §6 Plan):

* Standard: ein kurzes oder mittleres Diktat und ein langes; winzige Testaufnahmen nie.
* Gespraechsnotiz: ein Diktat, auf dem K1 den Leerfall setzt, und eines mit echter Notiz.
* Stundenzettel: ein Diktat, das Zeiten nennt, und eines ohne.
* E-Mail: eines der zwei nachgebesserten Diktate plus ein langes.

Echte Diktate vor erfundenen vor synthetischen; bei Gleichstand das bisher seltener gewaehlte, damit
die 16 Paare moeglichst viele verschiedene Diktate abdecken.

Aufruf:
    uv run python build_pair_artifact.py --roh <eval>\\messlauf73b.json \\
        --korpus <eval>\\corpus.v3.json --out <eval>\\lesevergleich73.html

Schreibt daneben `<out-stem>.zuordnung.json` (docId -> Diktat, Vorlage, welche Fassung A/B ist).
"""

from __future__ import annotations

import argparse
import hashlib
import html
import json
import random
import re
from pathlib import Path

from format_checks import EMPTY_SENTENCES

SECTIONS: tuple[str, ...] = (
    "abstractPrompt",
    "structuredPrompt",
    "prosePrompt",
    "aufgabePrompt",
    "gespraechsnotizPrompt",
    "emailPrompt",
    "stundenzettelPrompt",
    "analogPrompt",
)

#: Anzeigename und Zweck -- neutral, ohne Formvorgaben, damit keine Fassung am Vertrag der
#: anderen gemessen wird.
LABELS: dict[str, tuple[str, str]] = {
    "abstractPrompt": ("Abstract", "Kurzübersicht unter dem Titel des Eintrags"),
    "structuredPrompt": ("Zusammenfassung", "Inhalt verdichtet und geordnet"),
    "prosePrompt": ("Ausführliche Zusammenfassung", "Das Gesagte in lesbarem Schriftdeutsch"),
    "aufgabePrompt": ("Aufgaben", "Was aus dem Diktat zu tun ist"),
    "gespraechsnotizPrompt": ("Gesprächsnotiz", "Notiz eines Gesprächs zur Weitergabe"),
    "emailPrompt": ("E-Mail", "Mail an den Empfänger; entsteht aus der ausführlichen Zusammenfassung derselben Fassung"),
    "stundenzettelPrompt": ("Stundenzettel", "Zeiten und Tätigkeiten zum Übertragen"),
    "analogPrompt": ("Analog", "Freitext-Abschnitt zum Eintrag"),
}

#: Ausgaben, die als reiner Text erscheinen (in der App kein Markdown).
PLAIN: frozenset[str] = frozenset({"abstractPrompt", "emailPrompt"})

#: Die zwei Diktate, an denen die E-Mail nachgebessert wurde (Duzen, Unklarheitsmarken).
EMAIL_FIXED: tuple[str, ...] = ("aufnahme:2026_02_05_12_44_15.mp3", "aufnahme:2026_02_05_14_55_10.mp3")

SHORT = frozenset({"kurz", "mittel"})
CANDIDATES = ("R", "K1")

_TIME = re.compile(
    r"\d+(?:[.,]\d+)?\s*(?:h|std|stunden?|min|minuten?|uhr)\b|\b(?:stunde|stunden|minute|minuten|uhr)\b",
    re.I,
)


def mentions_time(text: str) -> bool:
    return bool(_TIME.search(text))


def origin_rank(item_id: str) -> int:
    """0 echt, 1 erfunden (#71), 2 synthetisch."""
    prefix = item_id.split(":", 1)[0]
    return 0 if prefix in ("aufnahme", "archiv") else 1 if prefix == "erfunden" else 2


def _outputs(rows: list[dict]) -> dict[tuple[str, str, str], str]:
    return {(r["item_id"], r["candidate"], r["section"]): r["output"] or "" for r in rows}


def _pick(pool: list[str], used: dict[str, int], rng: random.Random, exclude: set[str]) -> str:
    choices = [i for i in pool if i not in exclude]
    if not choices:
        raise ValueError("kein passendes Diktat fuer diese Schicht")
    tiebreak = {i: rng.random() for i in choices}
    best = min(choices, key=lambda i: (origin_rank(i), used.get(i, 0), tiebreak[i]))
    used[best] = used.get(best, 0) + 1
    return best


def select_pairs(items: list[dict], rows: list[dict], rng: random.Random) -> list[dict]:
    outputs = _outputs(rows)
    by_id = {i["id"]: i for i in items}
    used: dict[str, int] = {}

    def complete(section: str) -> list[str]:
        return [
            i["id"]
            for i in items
            if i["bucket"] != "winzig" and all(outputs.get((i["id"], c, section)) for c in CANDIDATES)
        ]

    pairs: list[dict] = []
    for section in SECTIONS:
        pool = complete(section)
        if section == "gespraechsnotizPrompt":
            empty = EMPTY_SENTENCES[section]
            strata = [
                [i for i in pool if outputs[(i, "K1", section)].strip() == empty],
                [i for i in pool if outputs[(i, "K1", section)].strip() != empty],
            ]
        elif section == "stundenzettelPrompt":
            strata = [
                [i for i in pool if mentions_time(by_id[i]["text"])],
                [i for i in pool if not mentions_time(by_id[i]["text"])],
            ]
        elif section == "emailPrompt":
            strata = [
                [i for i in pool if i in EMAIL_FIXED],
                [i for i in pool if by_id[i]["bucket"] == "lang" and i not in EMAIL_FIXED],
            ]
        else:
            strata = [
                [i for i in pool if by_id[i]["bucket"] in SHORT],
                [i for i in pool if by_id[i]["bucket"] == "lang"],
            ]
        chosen: set[str] = set()
        for stratum in strata:
            item_id = _pick(stratum, used, rng, chosen)
            chosen.add(item_id)
            pairs.append({"item_id": item_id, "section": section})
    return pairs


def pairs_from_mapping(mapping: dict[str, dict], sections: set[str]) -> list[dict]:
    """Folgerunde: dieselben Diktate der genannten Vorlagen wie in einer frueheren Runde."""
    unknown = sections - set(SECTIONS)
    if unknown:
        raise ValueError(f"unbekannte Vorlagen: {sorted(unknown)}")
    return [
        {"item_id": entry["item_id"], "section": entry["section"]}
        for _, entry in sorted(mapping.items())
        if entry["section"] in sections
    ]


def build_page_data(
    pairs: list[dict], items: dict[str, dict], rows: list[dict], prefix: str, rng: random.Random
) -> tuple[list[dict], dict[str, dict]]:
    """Seitendaten (ohne Kandidatennamen) und die geheime Zuordnung docId -> Fassungen."""
    outputs = _outputs(rows)
    order = list(range(len(pairs)))
    rng.shuffle(order)
    data: list[dict] = []
    mapping: dict[str, dict] = {}
    for position, pair_index in enumerate(order):
        pair = pairs[pair_index]
        doc_id = f"{prefix}{position:03d}"
        side_a, side_b = rng.sample(CANDIDATES, 2)
        item = items[pair["item_id"]]
        name, purpose = LABELS[pair["section"]]
        data.append(
            {
                "docId": doc_id,
                "vorlage": name,
                "zweck": purpose,
                "klartext": pair["section"] in PLAIN,
                "woerter": len(item["text"].split()),
                "transkript": html.escape(item["text"]),
                "a": html.escape(outputs[(pair["item_id"], side_a, pair["section"])]),
                "b": html.escape(outputs[(pair["item_id"], side_b, pair["section"])]),
            }
        )
        mapping[doc_id] = {"item_id": pair["item_id"], "section": pair["section"], "A": side_a, "B": side_b}
    return data, mapping


def round_id(roh: str, seed: int, prefix: str, mapping: dict[str, dict]) -> str:
    """Kennung einer Leserunde: dieselbe Id bei gleichem Präfix meint sonst andere Texte."""
    fingerprint = json.dumps([roh, seed, prefix, mapping], sort_keys=True, ensure_ascii=False)
    return hashlib.sha256(fingerprint.encode("utf-8")).hexdigest()[:12]


def load_template() -> str:
    return (Path(__file__).parent / "pair_template.html").read_text(encoding="utf-8")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--roh", type=Path, required=True, help="messlauf-JSON mit R und K1")
    parser.add_argument("--korpus", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True, help="Ziel-HTML (in der Sandbox, nie im Repo)")
    parser.add_argument("--seed", type=int, default=20260915)
    parser.add_argument(
        "--praefix",
        default="p",
        help="Praefix der Dokument-IDs; k und n sind von frueheren Runden belegt",
    )
    parser.add_argument(
        "--paare-aus",
        type=Path,
        help="Zuordnung einer frueheren Runde: dieselben Diktate erneut vorlegen (mit --abschnitte)",
    )
    parser.add_argument("--abschnitte", default="", help="Komma-Liste der Vorlagen fuer --paare-aus")
    args = parser.parse_args(argv)
    if args.praefix in ("k", "n"):
        parser.error("Praefix k und n sind belegt -- alte Noten wuerden an neuen Texten haengen")
    if args.paare_aus is not None:
        earlier = json.loads(args.paare_aus.read_text(encoding="utf-8"))
        if earlier.get("praefix") == args.praefix:
            parser.error("Folgerunde braucht ein neues --praefix")
        if not args.abschnitte:
            parser.error("--paare-aus braucht --abschnitte")

    run = json.loads(args.roh.read_text(encoding="utf-8"))
    corpus = json.loads(args.korpus.read_text(encoding="utf-8"))
    items = corpus["items"] if isinstance(corpus, dict) else corpus
    rows = [r for r in run["zeilen"] if r["candidate"] in CANDIDATES and r["section"] in SECTIONS]

    rng = random.Random(args.seed)
    if args.paare_aus is not None:
        try:
            pairs = pairs_from_mapping(earlier["paare"], {s.strip() for s in args.abschnitte.split(",") if s.strip()})
        except ValueError as error:
            parser.error(str(error))
    else:
        pairs = select_pairs(items, rows, rng)
    data, mapping = build_page_data(pairs, {i["id"]: i for i in items}, rows, args.praefix, rng)
    runde = round_id(str(args.roh), args.seed, args.praefix, mapping)

    page = (
        load_template()
        .replace("__DATEN__", json.dumps(data, ensure_ascii=False))
        .replace("__N__", str(len(data)))
        .replace("__PRAEFIX__", args.praefix)
        .replace("__SPEICHERSCHLUESSEL__", f"johann-vergleich-{args.praefix}-{runde}")
        .replace("__RUNDE__", runde)
    )
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(page, encoding="utf-8")
    mapping_path = args.out.with_name(f"{args.out.stem}.zuordnung.json")
    mapping_path.write_text(
        json.dumps(
            {"roh": str(args.roh), "seed": args.seed, "praefix": args.praefix, "runde": runde, "paare": mapping},
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )

    print(f"{len(data)} Paare, {len(page) // 1024} KB -> {args.out}")
    print(f"Zuordnung -> {mapping_path}")
    print(f"verschiedene Diktate: {len({p['item_id'] for p in pairs})}")
    for doc_id, entry in mapping.items():
        print(f"   {doc_id}  {LABELS[entry['section']][0]:<30} {entry['item_id']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
