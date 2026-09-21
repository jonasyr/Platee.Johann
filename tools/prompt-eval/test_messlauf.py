"""Tests fuer messlauf.py -- ohne API, nur Aufgabenbau, Nachrichten und Auswertung."""

from __future__ import annotations

import pytest

from messlauf import (
    EMAIL,
    PHASE_ONE,
    call_cost,
    catalog_prices,
    email_tasks,
    parse_renew,
    phase_one_tasks,
    seed_from_basis,
    summarise,
    user_message,
)

PROMPTS = {
    "systemMessage": "System.",
    "abstractPrompt": "Höchstens {word_limit} Wörter.\n{transcript}",
    "emailPrompt": "Mail aus:\n{prose_summary}",
    "prosePrompt": "Aufbereiten:\n{transcript}",
}
ITEMS = [{"id": "a:1", "bucket": "kurz", "text": "Das Angebot kommt Freitag."}]


def _gen(prompt: int = 1000, cached: int = 0, output: int = 100) -> dict:
    return {
        "prompt_tokens": prompt,
        "cached_tokens": cached,
        "output_tokens": output,
        "reasoning_tokens": 40,
        "finish_reason": "stop",
        "well_formed_first_try": True,
        "error": "",
    }


def test_word_limit_is_filled_like_the_app() -> None:
    assert user_message(PROMPTS, "abstractPrompt", "wort " * 10).startswith("Höchstens 20 Wörter.")


def test_email_gets_the_prose_not_the_transcript() -> None:
    message = user_message(PROMPTS, EMAIL, "TRANSKRIPT", prose="PROSA")
    assert "PROSA" in message and "TRANSKRIPT" not in message


def test_phase_one_covers_every_section_except_email_per_candidate() -> None:
    tasks = phase_one_tasks(ITEMS, {"R": PROMPTS, "K1": PROMPTS})
    assert len(tasks) == 2 * len(PHASE_ONE)
    assert EMAIL not in {t["section"] for t in tasks}


def test_email_tasks_use_the_prose_of_the_same_candidate_and_skip_empty_ones() -> None:
    done = {
        "a:1|R|prosePrompt": {"output": "Prosa von R"},
        "a:1|K1|prosePrompt": {"output": ""},
    }
    tasks = email_tasks(ITEMS, {"R": PROMPTS, "K1": PROMPTS}, done)
    assert [(t["candidate"], t["prose"]) for t in tasks] == [("R", "Prosa von R")]


def test_cost_uses_the_reported_cached_share() -> None:
    prices = {"input": 1.0, "cached": 0.1, "output": 10.0}
    assert call_cost(_gen(prompt=1000, cached=0, output=100), prices) == pytest.approx(0.002)
    assert call_cost(_gen(prompt=1000, cached=1000, output=100), prices) == pytest.approx(0.0011)


def test_catalog_prices_read_input_and_output() -> None:
    prices = catalog_prices("PriceInPerMillion: 0.20, PriceOutPerMillion: 1.20", cache_discount=0.9)
    assert prices == {"input": 0.20, "cached": pytest.approx(0.02), "output": 1.20}


def _row(candidate: str, section: str, item: str = "a:1") -> dict:
    return {"key": f"{item}|{candidate}|{section}", "candidate": candidate, "section": section, "item_id": item}


def test_basis_keeps_everything_except_the_sections_to_renew() -> None:
    basis = [_row("R", EMAIL), _row("K1", EMAIL), _row("K1", "prosePrompt")]
    seeded = seed_from_basis(basis, {"R", "K1"}, {("K1", EMAIL)})
    assert set(seeded) == {"a:1|R|emailPrompt", "a:1|K1|prosePrompt"}


def test_renewing_the_prose_also_renews_the_email_that_is_built_from_it() -> None:
    basis = [_row("K1", EMAIL), _row("K1", "prosePrompt"), _row("K1", "title")]
    assert set(seed_from_basis(basis, {"K1"}, {("K1", "prosePrompt")})) == {"a:1|K1|title"}


def test_basis_rows_of_candidates_not_in_this_run_are_dropped() -> None:
    assert seed_from_basis([_row("K2", EMAIL)], {"R", "K1"}, set()) == {}


def test_later_basis_files_override_earlier_ones() -> None:
    older = {**_row("K1", EMAIL), "output": "alt"}
    newer = {**_row("K1", EMAIL), "output": "neu"}
    seeded = seed_from_basis([older, newer], {"K1"}, set())
    assert seeded["a:1|K1|emailPrompt"]["output"] == "neu"


def test_parse_renew_rejects_unknown_candidates_and_sections() -> None:
    assert parse_renew(["K1:emailPrompt"], {"R", "K1"}) == {("K1", EMAIL)}
    with pytest.raises(ValueError):
        parse_renew(["K9:emailPrompt"], {"R", "K1"})
    with pytest.raises(ValueError):
        parse_renew(["K1:mailPrompt"], {"R", "K1"})


def test_summarise_counts_violations_and_dictation_cost() -> None:
    prices = {"input": 1.0, "cached": 0.1, "output": 1.0}
    rows = [
        {"candidate": "R", "section": "title", "item_id": "a:1", "output": "Angebot", "gen": _gen()},
        {
            "candidate": "R",
            "section": "analogPrompt",
            "item_id": "a:1",
            "output": "- Das Angebot kommt am Freitag.",
            "gen": _gen(),
        },
    ]
    summary = summarise(rows, {"a:1": ITEMS[0]["text"]}, prices)
    analog = summary["je_abschnitt"]["R"]["analogPrompt"]
    assert analog["pruefungen"]["fliesstext"] == {"anwendbar": 1, "verletzt": 1, "beispiele": ["a:1"]}
    assert summary["je_diktat"]["R"]["auto_usd_mittel"] == pytest.approx(0.0011)
