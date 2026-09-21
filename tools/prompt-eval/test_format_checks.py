"""Tests fuer format_checks.py -- je Pruefung ein Gut- und ein Schlechtfall, erfundene Texte."""

from __future__ import annotations

import pytest

from format_checks import (
    check,
    has_nesting,
    is_german,
    markdown_well_formed,
    names_not_in_transcript,
    word_limit_for,
)

TRANSCRIPT = (
    "Gespräch mit Herrn Vogel von der Firma Brenner. Das Angebot kommt bis Ende der Woche, "
    "und Neele rechnet die Summe gegen. " * 3
)


def v(section: str, output: str, transcript: str = TRANSCRIPT) -> list[str]:
    return check(section, output, transcript).violations


# ------------------------------------------------------------------------ allgemein


def test_a_clean_german_answer_has_no_general_violations() -> None:
    assert v("analogPrompt", "Herr Vogel erhält das Angebot bis Ende der Woche. Die Summe wird noch geprüft.") == []


@pytest.mark.parametrize(
    ("output", "violation"),
    [
        ("", "nicht_leer"),
        ("Hier ist die Zusammenfassung: Das Angebot kommt bis Ende der Woche.", "keine_vorrede"),
        ("Das Angebot kommt bis Ende der Woche.\n\nMöchten Sie eine Kurzfassung?", "keine_rueckfrage"),
        ("Datum: TBD. Das Angebot kommt bis Ende der Woche.", "keine_platzhalter"),
        ("Guten Tag [Name], das Angebot kommt bis Ende der Woche.", "keine_platzhalter"),
        ("Analog\n\nDas Angebot kommt bis Ende der Woche.", "keine_selbstgenannte_ueberschrift"),
        ("The offer will be sent by the end of the week and it is on track.", "deutsch"),
    ],
)
def test_general_violations_are_found(output: str, violation: str) -> None:
    assert violation in v("analogPrompt", output)


def test_is_german_does_not_judge_fragments() -> None:
    assert is_german("OK") is None


@pytest.mark.parametrize(
    ("text", "ok"),
    [
        ("- Punkt\n  - Unterpunkt\n- Punkt", True),
        ("  - Unterpunkt ohne Eltern", False),
        ("**fett ohne Ende", False),
        ("###\nText", False),
        ("### Kontext\nText", True),
    ],
)
def test_markdown_well_formed(text: str, ok: bool) -> None:
    assert markdown_well_formed(text) is ok


def test_nesting_is_reported_as_a_metric() -> None:
    assert has_nesting("- a\n  - b")
    assert not has_nesting("- a\n- b")


# --------------------------------------------------------------------------- Abstract


def test_word_limit_matches_word_limit_calculator() -> None:
    assert [word_limit_for("w " * n) for n in (10, 299, 300, 999, 1000)] == [20, 20, 50, 50, 150]


def test_abstract_over_the_limit_or_with_markdown_is_flagged() -> None:
    long = " ".join(["Wort"] * 25)
    assert "laenge_ok" in v("abstractPrompt", long)
    assert "kein_markdown" in v("abstractPrompt", "**Angebot** kommt diese Woche.")


# --------------------------------------------------------------------- Zusammenfassung


def test_structured_accepts_the_schema_and_rejects_foreign_or_empty_headings() -> None:
    good = "### Kontext\n- Angebot Brenner\n\n### Entscheidungen\n- Bis Freitag"
    assert v("structuredPrompt", good) == []
    assert "nur_schema_ueberschriften" in v("structuredPrompt", "### Hintergrund\n- Angebot")
    assert "keine_leere_ueberschrift" in v("structuredPrompt", "### Kontext\n### Entscheidungen\n- x")


# -------------------------------------------------------------------------- Aufgaben


def test_tasks_good_case() -> None:
    out = "Es geht um das Angebot für Herrn Vogel.\n\n- Summe mit Neele gegenrechnen\n- Angebot senden (Ende der Woche)"
    assert v("aufgabePrompt", out) == []


def test_tasks_empty_case_is_valid_without_a_list() -> None:
    out = "Es geht um eine Rückmeldung ohne Handlungsbedarf.\n\nKeine Aufgaben genannt."
    result = check("aufgabePrompt", out, TRANSCRIPT)
    assert result.violations == []
    assert result.metrics["leerfall"] == 1


@pytest.mark.parametrize(
    ("out", "violation"),
    [
        ("- Angebot senden", "absatz_vor_liste"),
        ("Absatz.\n\n" + "\n".join(f"- Aufgabe Nummer {i} erledigen" for i in range(9)), "hoechstens_acht"),
        ("Absatz.\n\n- " + " ".join(["Wort"] * 21), "hoechstens_20_woerter"),
        ("Absatz.\n\n* Angebot senden", "bindestrich"),
        ("Absatz.\n\n- [ ] Angebot senden", "bindestrich"),
        ("Absatz.\n\n- Aufgabe 1: Angebot senden", "keine_nummerierung"),
    ],
)
def test_task_violations(out: str, violation: str) -> None:
    assert violation in v("aufgabePrompt", out)


# ------------------------------------------------------------------- Gesprächsnotiz


def test_note_flags_invented_names_and_follow_up_mail() -> None:
    out = "Teilnehmer:\n- Herr Vogel\n- Frau Mia\n\nBetreff: Nachfrage"
    violations = v("gespraechsnotizPrompt", out)
    assert "namen_belegt" in violations
    assert "keine_folgemail" in violations
    assert names_not_in_transcript(out, TRANSCRIPT) == ["Mia"]


def test_note_empty_case_must_stand_alone() -> None:
    assert v("gespraechsnotizPrompt", "Kein Gespräch dokumentiert.") == []
    assert "leerfall_allein" in v("gespraechsnotizPrompt", "Kein Gespräch dokumentiert.\n\n- Angebot Brenner")


# ----------------------------------------------------------------------------- E-Mail


GOOD_MAIL = (
    "Betreff: Angebot Sanierung Erdgeschoss\n\nSehr geehrter Herr Vogel,\n\n"
    "Sie erhalten das Angebot bis Ende der Woche.\n\nBei Rückfragen melden Sie sich gern."
)


def test_email_good_case() -> None:
    assert v("emailPrompt", GOOD_MAIL) == []


@pytest.mark.parametrize(
    ("mail", "violation"),
    [
        (GOOD_MAIL.replace("Betreff: ", "Thema: "), "betreff_erste_zeile"),
        (GOOD_MAIL.replace("Sehr geehrter Herr Vogel,", "Das Angebot:"), "anrede"),
        (GOOD_MAIL + "\n\nMit freundlichen Grüßen", "kein_gruss_am_ende"),
        (GOOD_MAIL.replace("Sie erhalten", "Du erhältst"), "sie_form"),
        (GOOD_MAIL.replace("das Angebot", "das **Angebot**"), "kein_markdown"),
        (GOOD_MAIL + "\n\n" + " ".join(["Wort"] * 410), "laenge_ok"),
        (GOOD_MAIL.replace("Sie erhalten", "Bitte prüfe das Angebot. Sie erhalten"), "kein_du_imperativ"),
        (GOOD_MAIL.replace("Angebot bis", "Angebot für [inhaltlich unklar] bis"), "keine_unklarheitsmarke"),
        (GOOD_MAIL + " Der Rest war akustisch unverständlich.", "keine_unklarheitsmarke"),
    ],
)
def test_email_violations(mail: str, violation: str) -> None:
    assert violation in v("emailPrompt", mail)


@pytest.mark.parametrize(
    "sentence",
    ["Bitte prüfen Sie das Angebot.", "Bitte eine kurze Rückmeldung bis Freitag.", "Bitte beachten Sie die Frist."],
)
def test_email_formal_requests_are_no_du_imperative(sentence: str) -> None:
    assert "kein_du_imperativ" not in v("emailPrompt", GOOD_MAIL.replace("Sie erhalten", sentence + " Sie erhalten"))


def test_email_counts_a_mention_of_unclear_content_for_reading_only() -> None:
    result = check("emailPrompt", GOOD_MAIL + " Der Termin ist noch unklar.", "")
    assert result.metrics["unklar_erwaehnt"] == 1
    assert "keine_unklarheitsmarke" not in result.violations


# ---------------------------------------------------------------------- Stundenzettel


@pytest.mark.parametrize(
    ("section", "output"),
    [
        ("stundenzettelPrompt", "Keine Zeiten genannt. Montag waren es aber 3 h."),
        ("abstractPrompt", "Kein zusammenfassbarer Inhalt. Es ging um das Angebot."),
        ("gespraechsnotizPrompt", "Kein Gespräch dokumentiert.\n\n- Teilnehmer: Herr Vogel"),
        ("analogPrompt", "Kein Eintrag erkennbar, außer dem Termin am Freitag."),
    ],
)
def test_the_empty_sentence_only_counts_when_it_stands_alone(section: str, output: str) -> None:
    # Codex, PR #85: „enthält den Satz" reichte, und die übrigen Prüfungen entfielen.
    assert check(section, output, TRANSCRIPT).metrics["leerfall"] == 0


def test_the_task_empty_case_may_follow_the_summary_paragraph() -> None:
    output = "Es ging um die Ablage.\n\nKeine Aufgaben genannt."
    assert check("aufgabePrompt", output, TRANSCRIPT).metrics["leerfall"] == 1


def test_timesheet_lines_need_a_duration_or_the_explicit_gap() -> None:
    good = "- ca. 3 h – Ahornweg\n- Dauer nicht genannt – Fotos sortiert\n- 30 min – Telefonat"
    assert v("stundenzettelPrompt", good) == []
    assert "jede_zeile_mit_dauer" in v("stundenzettelPrompt", "- Ahornweg besichtigt")
    assert v("stundenzettelPrompt", "Keine Zeiten genannt.") == []


# ---------------------------------------------------------------------------- Analog


def test_analog_must_be_prose_within_120_words() -> None:
    assert "fliesstext" in v("analogPrompt", "- Angebot Brenner kommt")
    assert "laenge_ok" in v("analogPrompt", " ".join(["Angebot"] * 121))
