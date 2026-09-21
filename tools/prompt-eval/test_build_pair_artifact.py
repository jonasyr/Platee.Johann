"""Tests fuer build_pair_artifact.py -- Auswahl, Seitenzufall, Seitendaten. Ohne echte Diktate."""

from __future__ import annotations

import json
import random

import pytest

from build_pair_artifact import (
    EMAIL_FIXED,
    SECTIONS,
    build_page_data,
    mentions_time,
    origin_rank,
    pairs_from_mapping,
    select_pairs,
)
from format_checks import EMPTY_SENTENCES


def _items() -> list[dict]:
    items = []
    for n in range(4):
        items.append({"id": f"aufnahme:kurz{n}", "bucket": "mittel", "text": f"Kurzes echtes Diktat {n}."})
        items.append({"id": f"aufnahme:lang{n}", "bucket": "lang", "text": f"Langes echtes Diktat {n}, drei Stunden Arbeit."})
        items.append({"id": f"syn2:mittel{n}", "bucket": "mittel", "text": f"Synthetisch {n}, 2 h Fliesen."})
    items.append({"id": "archiv:winzig", "bucket": "winzig", "text": "Hallo."})
    items.extend({"id": fixed, "bucket": "lang", "text": "Mail-Diktat."} for fixed in EMAIL_FIXED)
    return items


def _rows(items: list[dict]) -> list[dict]:
    rows = []
    for item in items:
        for candidate in ("R", "K1"):
            for section in SECTIONS:
                text = f"{candidate} {section} {item['id']}"
                if candidate == "K1" and section == "gespraechsnotizPrompt" and "kurz" in item["id"]:
                    text = EMPTY_SENTENCES["gespraechsnotizPrompt"]
                rows.append({"item_id": item["id"], "candidate": candidate, "section": section, "output": text})
    return rows


def test_every_template_gets_exactly_two_pairs() -> None:
    items = _items()
    pairs = select_pairs(items, _rows(items), random.Random(1))
    assert len(pairs) == 2 * len(SECTIONS)
    assert all(sum(p["section"] == s for p in pairs) == 2 for s in SECTIONS)


def test_default_strata_are_one_short_or_medium_and_one_long_never_tiny() -> None:
    items = _items()
    buckets = {i["id"]: i["bucket"] for i in items}
    pairs = select_pairs(items, _rows(items), random.Random(2))
    for section in ("abstractPrompt", "structuredPrompt", "prosePrompt", "aufgabePrompt", "analogPrompt"):
        chosen = sorted(buckets[p["item_id"]] for p in pairs if p["section"] == section)
        assert chosen[0] == "lang" and chosen[1] in ("kurz", "mittel"), (section, chosen)
    assert "archiv:winzig" not in {p["item_id"] for p in pairs}


def test_real_dictations_are_preferred_over_synthetic_ones() -> None:
    items = _items()
    pairs = select_pairs(items, _rows(items), random.Random(3))
    assert all(origin_rank(p["item_id"]) == 0 for p in pairs)


def test_conversation_note_pairs_one_empty_case_with_one_real_note() -> None:
    items = _items()
    rows = _rows(items)
    pairs = [p for p in select_pairs(items, rows, random.Random(4)) if p["section"] == "gespraechsnotizPrompt"]
    k1 = {(r["item_id"]): r["output"] for r in rows if r["candidate"] == "K1" and r["section"] == "gespraechsnotizPrompt"}
    empty = [k1[p["item_id"]] == EMPTY_SENTENCES["gespraechsnotizPrompt"] for p in pairs]
    assert sorted(empty) == [False, True]


def test_timesheet_pairs_one_dictation_with_times_and_one_without() -> None:
    items = _items()
    text = {i["id"]: i["text"] for i in items}
    pairs = [p for p in select_pairs(items, _rows(items), random.Random(5)) if p["section"] == "stundenzettelPrompt"]
    assert sorted(mentions_time(text[p["item_id"]]) for p in pairs) == [False, True]


def test_email_always_contains_one_of_the_repaired_dictations() -> None:
    items = _items()
    for seed in range(5):
        pairs = [p for p in select_pairs(items, _rows(items), random.Random(seed)) if p["section"] == "emailPrompt"]
        assert len({p["item_id"] for p in pairs} & set(EMAIL_FIXED)) == 1


@pytest.mark.parametrize(
    ("text", "expected"),
    [("zwei Stunden Estrich", True), ("2,5 h Montage", True), ("von 8 Uhr bis Mittag", True), ("Material bestellen", False)],
)
def test_mentions_time(text: str, expected: bool) -> None:
    assert mentions_time(text) is expected


def test_sides_are_random_but_reproducible_and_stay_out_of_the_page() -> None:
    items = _items()
    rows = _rows(items)
    first = select_pairs(items, rows, random.Random(7))
    data, mapping = build_page_data(first, {i["id"]: i for i in items}, rows, "p", random.Random(7))
    again, mapping_again = build_page_data(first, {i["id"]: i for i in items}, rows, "p", random.Random(7))
    assert mapping == mapping_again
    assert {m["A"] for m in mapping.values()} == {"R", "K1"}
    assert [d["docId"] for d in data] == [f"p{n:03d}" for n in range(len(first))]
    page_json = json.dumps(data, ensure_ascii=False)
    assert '"R"' not in page_json and '"K1"' not in page_json and "candidate" not in page_json
    for entry in data:
        side_a = mapping[entry["docId"]]["A"]
        assert entry["a"].startswith(side_a + " ")


def test_a_follow_up_round_rereads_exactly_the_same_dictations_of_the_named_templates() -> None:
    mapping = {
        "p001": {"item_id": "x:1", "section": "emailPrompt", "A": "R", "B": "K1"},
        "p000": {"item_id": "x:2", "section": "emailPrompt", "A": "K1", "B": "R"},
        "p002": {"item_id": "x:3", "section": "abstractPrompt", "A": "R", "B": "K1"},
    }
    assert pairs_from_mapping(mapping, {"emailPrompt"}) == [
        {"item_id": "x:2", "section": "emailPrompt"},
        {"item_id": "x:1", "section": "emailPrompt"},
    ]
    with pytest.raises(ValueError):
        pairs_from_mapping(mapping, {"mailPrompt"})


def test_page_texts_are_html_escaped() -> None:
    items = [{"id": "aufnahme:x", "bucket": "mittel", "text": "<script>alert(1)</script>"}]
    rows = [
        {"item_id": "aufnahme:x", "candidate": c, "section": "abstractPrompt", "output": "<b>fett</b>"} for c in ("R", "K1")
    ]
    pairs = [{"item_id": "aufnahme:x", "section": "abstractPrompt"}]
    data, _ = build_page_data(pairs, {"aufnahme:x": items[0]}, rows, "p", random.Random(0))
    assert "<script>" not in data[0]["transkript"] and "<b>" not in data[0]["a"]
