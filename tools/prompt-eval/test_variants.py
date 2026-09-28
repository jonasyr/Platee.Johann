"""Tests fuer den Kandidatenbau in variants.py -- nur erfundene Texte, kein Team-Wortlaut."""

from __future__ import annotations

import pytest

from variants import (
    CACHE_THRESHOLD_TOKENS,
    SECTION_KEYS,
    CandidateError,
    Prices,
    build_candidate,
    cached_tokens,
    call_cost,
    catalog_prices,
    cost_report,
    stable_prefix_tokens,
    validate_candidate,
)

SYSTEM = (
    "Du bist ein Assistent für Notizen.\n\n"
    "**Schlechtes Beispiel:**\n„Also irgendwie war da was.“\n\n"
    "**Gutes Beispiel:**\n„Es wurde der Stand besprochen.“\n"
)


def _base() -> dict:
    prompts = {key: f"Anweisung für {key}.\n\nTranskript:\n{{transcript}}" for key in SECTION_KEYS}
    prompts["abstractPrompt"] = "Höchstens {word_limit} Wörter.\n\nTranskript:\n{transcript}"
    prompts["emailPrompt"] = "Schreibe eine Mail.\n\nZusammenfassung:\n{prose_summary}"
    return {"promptDefaultsRevision": 1, "systemMessage": SYSTEM, **prompts}


# ----------------------------------------------------------------------------- Invarianten


def test_a_faithful_candidate_passes_and_keeps_non_text_fields() -> None:
    merged = build_candidate(_base(), {"analogPrompt": "Kurz fassen.\n\nTranskript:\n{transcript}"})
    assert merged["promptDefaultsRevision"] == 1
    assert merged["analogPrompt"].startswith("Kurz fassen.")


def test_unknown_keys_are_rejected() -> None:
    with pytest.raises(CandidateError, match="Unbekannte"):
        build_candidate(_base(), {"analogPromt": "Tippfehler {transcript}"})


def test_a_lost_placeholder_is_reported() -> None:
    prompts = {**_base(), "abstractPrompt": "Kurz.\n\nTranskript:\n{transcript}"}
    assert any("word_limit" in p for p in validate_candidate(_base(), prompts))


def test_the_false_language_premise_is_reported() -> None:
    prompts = {
        **_base(),
        "analogPrompt": "Du erhältst ein Transkript auf Deutsch.\n\nTranskript:\n{transcript}",
    }
    assert any("B-01" in p for p in validate_candidate(_base(), prompts))


@pytest.mark.parametrize(
    ("system", "finding"),
    [
        (SYSTEM + "\n### CHAIN OF THOUGHTS ###\n1. Lies.", "S-02"),
        (SYSTEM + "\nSCHREIBE IMMER SACHLICH.", "S-01"),
        (SYSTEM.replace("Stand besprochen", "Stand erörtert"), "S-08"),
    ],
)
def test_system_message_findings_are_reported(system: str, finding: str) -> None:
    prompts = {**_base(), "systemMessage": system}
    assert any(finding in p for p in validate_candidate(_base(), prompts))


def test_abbreviations_are_not_mistaken_for_shouting() -> None:
    prompts = {**_base(), "systemMessage": SYSTEM + "\nAls PDF oder HTML nutzbar."}
    assert validate_candidate(_base(), prompts) == []


# ---------------------------------------------------------------------------------- Cache


@pytest.mark.parametrize(
    ("prefix", "cached"),
    [(1023, 0), (1024, 1024), (1151, 1024), (1152, 1152), (1300, 1280)],
)
def test_cached_tokens_follow_the_1024_plus_128_rule(prefix: int, cached: int) -> None:
    assert cached_tokens(prefix) == cached


def test_stable_prefix_ends_at_the_first_placeholder() -> None:
    # Beim Abstract wechselt {word_limit} je Diktat -- dahinter ist fuer den Cache anderer Text.
    short = stable_prefix_tokens(SYSTEM, "Höchstens {word_limit} Wörter. Viel mehr Text danach.")
    assert short == stable_prefix_tokens(SYSTEM, "Höchstens ")


def test_a_hit_is_never_more_expensive_and_below_threshold_changes_nothing() -> None:
    prices = Prices(input_per_million=0.20, cached_per_million=0.02)
    template = "Kurz.\n{transcript}"
    miss = call_cost(SYSTEM, template, 500, prices, hit=False)
    hit = call_cost(SYSTEM, template, 500, prices, hit=True)
    assert stable_prefix_tokens(SYSTEM, template) < CACHE_THRESHOLD_TOKENS
    assert hit == miss


def test_a_long_prefix_is_cheaper_on_a_hit() -> None:
    prices = Prices(input_per_million=0.20, cached_per_million=0.02)
    long_system = SYSTEM + ("Regel für sachliches Schreiben. " * 300)
    template = "Kurz.\n{transcript}"
    assert call_cost(long_system, template, 200, prices, hit=True) < call_cost(
        long_system, template, 200, prices, hit=False
    )


def test_cost_report_sums_the_six_automatic_calls() -> None:
    prices = Prices(input_per_million=1.0, cached_per_million=0.1)
    report = cost_report(_base(), [100, 300], prices)
    auto = report["per_dictation_auto"]["usd_miss"]
    expected = sum(
        report["calls"][c]["usd_miss"]
        for c in ("title", "abstractPrompt", "structuredPrompt", "prosePrompt",
                  "aufgabePrompt", "gespraechsnotizPrompt")
    )
    assert auto == pytest.approx(expected)
    assert set(report["calls"]) == {"title", *SECTION_KEYS}


def test_catalog_prices_read_the_first_model_and_apply_the_discount() -> None:
    source = """
        new(ModelNames.Summaries, "Luna", "…", Reasoning: 3, Speed: 4,
            PriceInPerMillion: 0.20, PriceOutPerMillion: 1.20),
        new("terra", "Terra", "…", PriceInPerMillion: 2.00, PriceOutPerMillion: 12.00),
    """
    prices = catalog_prices(source, cache_discount=0.9)
    assert prices.input_per_million == 0.20
    assert prices.cached_per_million == pytest.approx(0.02)


def test_catalog_prices_fail_loudly_when_the_source_changed() -> None:
    with pytest.raises(ValueError, match="nicht im Katalog"):
        catalog_prices("class Leer {}", cache_discount=0.9)
