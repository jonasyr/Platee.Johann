from __future__ import annotations

from vergleich_115_116 import added_words, greeting_of, uses_first_name

TEXT = "Mail an Thomas Berger von der Firma Holzbau Berger. Hallo Thomas, danke für das Angebot."


def test_first_name_in_a_formal_greeting_is_flagged() -> None:
    assert uses_first_name("Guten Tag, Herr Thomas,", TEXT)


def test_surname_in_a_formal_greeting_is_fine() -> None:
    assert not uses_first_name("Guten Tag Herr Berger,", TEXT)
    assert not uses_first_name("Guten Tag,", TEXT)


def test_a_name_the_text_itself_uses_formally_is_a_surname() -> None:
    assert not uses_first_name("Guten Tag Frau Hartmann,", "Telefonat mit Frau Hartmann Neele war dabei.")


def test_greeting_is_the_first_greeting_line_after_the_subject() -> None:
    assert greeting_of("Betreff: Angebot\n\nGuten Tag Herr Berger,\n\nText.") == "Guten Tag Herr Berger,"


def test_title_words_missing_from_the_dictation_are_listed() -> None:
    transcript = "Der Brandschutznachweis für die Baugenehmigung fehlt noch."
    assert added_words("Baugenehmigung: Brandschutznachweis unerquicklich", transcript) == ["unerquicklich"]
    assert added_words("Baugenehmigung: Brandschutznachweis fehlt", transcript) == []
