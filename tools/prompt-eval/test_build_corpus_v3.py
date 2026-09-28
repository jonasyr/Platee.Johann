"""Tests fuer build_corpus_v3.py -- ohne Sandbox-Daten, nur mit erfundenen Texten."""

from __future__ import annotations

import pytest

from build_corpus_v3 import (
    SAME_RECORDING,
    bucket_of,
    build,
    near_duplicates,
)
from dictations_long import DICTATIONS_LONG


def _item(ident: str, text: str) -> dict:
    return {"id": ident, "source": ident.split(":")[0], "bucket": bucket_of(text), "text": text}


BASE_TEXT = (
    "Projekt Nordring. Wir haben folgende Aufgabenstellung. Die Kabelkanäle im zweiten Stock "
    "sitzen nicht da, wo sie laut Plan sitzen sollen, und müssen versetzt werden. Herr Okafor "
    "reicht dafür einen Nachtrag ein, und Frau Lenz prüft den Planstand auf der Baustelle."
)


def test_near_duplicates_finds_two_transcriptions_of_one_recording() -> None:
    # Zwei Laeufe der Spracherkennung weichen in Kleinigkeiten ab -- genau das hat die
    # textgleiche Pruefung in curate_corpus.py uebersehen.
    variant = BASE_TEXT.replace("Wir haben folgende", "Wir haben die folgende").replace(
        "sitzen sollen", "sein sollen"
    )
    items = [_item("aufnahme:a", BASE_TEXT), _item("archiv:b", variant)]

    pairs = near_duplicates(items)

    assert [(a, b) for a, b, _ in pairs] == [("aufnahme:a", "archiv:b")]


def test_near_duplicates_ignores_texts_that_only_share_the_setting() -> None:
    other = (
        "Begehung Ahornweg heute. Im Aufgang zwei laufen die Trocknungsgeräte noch eine Woche, "
        "danach holt Trockenbau Hess sie ab. Die Mieter wissen Bescheid."
    )
    assert near_duplicates([_item("a:1", BASE_TEXT), _item("a:2", other)]) == []


def test_build_drops_the_listed_duplicates_and_records_why() -> None:
    keep, drop, _ = SAME_RECORDING[0]
    items = [_item(keep, BASE_TEXT), _item(drop, BASE_TEXT + " Nachtrag.")]

    corpus = build({"items": items, "verworfen": []}, extra=[], duplicates=SAME_RECORDING[:1])

    assert [i["id"] for i in corpus["items"]] == [keep]
    assert {"id": drop, "grund": f"dieselbe Aufnahme wie {keep}"} in corpus["verworfen"]


def test_build_refuses_a_duplicate_list_that_no_longer_matches() -> None:
    # Ein veralteter Eintrag darf nicht still durchlaufen -- sonst glaubt man, die Dublette
    # sei entfernt, und liest dieselbe Aufnahme weiter dreifach.
    with pytest.raises(ValueError, match="nicht im Korpus"):
        build({"items": [_item("aufnahme:x", BASE_TEXT)], "verworfen": []}, extra=[])


def test_build_appends_long_dictations_with_bucket_and_title() -> None:
    keep, drop, _ = SAME_RECORDING[0]
    items = [_item(keep, BASE_TEXT), _item(drop, BASE_TEXT)]
    extra = [{"id": "syn3:probe", "titel": "Probe", "text": "Wort " * 900}]

    corpus = build({"items": items, "verworfen": []}, extra=extra, duplicates=SAME_RECORDING[:1])

    added = corpus["items"][-1]
    assert added == {
        "id": "syn3:probe",
        "source": "syn3",
        "bucket": "lang",
        "titel": "Probe",
        "text": "Wort " * 900,
    }


def test_build_rejects_an_id_that_already_exists() -> None:
    keep, drop, _ = SAME_RECORDING[0]
    items = [_item(keep, BASE_TEXT), _item(drop, BASE_TEXT)]
    with pytest.raises(ValueError, match="doppelt"):
        build(
            {"items": items, "verworfen": []},
            extra=[{"id": keep, "titel": "x", "text": BASE_TEXT}],
            duplicates=SAME_RECORDING[:1],
        )


@pytest.mark.parametrize(
    ("words", "expected"), [(5, "winzig"), (40, "kurz"), (150, "mittel"), (400, "lang")]
)
def test_bucket_of_uses_the_thresholds_of_02_build_corpus(words: int, expected: str) -> None:
    # 02_build_corpus.py: Token = Zeichen / 4,3; <40 winzig, <150 kurz, <400 mittel.
    assert bucket_of("Wort " * words) == expected


def test_long_dictations_cover_the_missing_upper_end() -> None:
    lengths = sorted(len(d["text"].split()) for d in DICTATIONS_LONG)
    assert lengths[0] >= 750, "kuerzer als die laengsten vorhandenen Diktate bringt nichts"
    assert lengths[-1] >= 1200
    assert len({d["id"] for d in DICTATIONS_LONG}) == len(DICTATIONS_LONG)
    assert all(d["id"].startswith("syn3:") for d in DICTATIONS_LONG)
