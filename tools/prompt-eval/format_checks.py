"""Automatische Formatpruefungen je Vorlage (#73, Schritt 2).

Abgeleitet aus `docs/prompting/soll-profil-73.md`. Geprueft wird nur, was sich ohne Urteil
feststellen laesst -- Form, Laenge, Leerfall, Sprache. Ob eine Zusammenfassung *gut* ist,
entscheidet der Lesevergleich in Schritt 3, nicht dieses Modul.

Jede Pruefung liefert `True` (erfuellt), `False` (verletzt) oder `None` (trifft hier nicht zu,
etwa die Wortgrenze bei einem Leerfall-Satz). Zahlen, die kein Urteil sind (Wortzahl,
Verhaeltnis), stehen daneben unter eigenem Namen.

Bewusst grob: Die Heuristiken sollen Ausreisser finden, die man dann liest -- nicht jede
Grenzfrage des Deutschen entscheiden.
"""

from __future__ import annotations

import re
from dataclasses import dataclass

#: Anzeigenamen, unter denen die Anwendung die Abschnitte bereits ueberschreibt.
DISPLAY_NAMES: dict[str, tuple[str, ...]] = {
    "abstractPrompt": ("abstract", "kurzfassung", "zusammenfassung"),
    "structuredPrompt": ("zusammenfassung", "strukturierte zusammenfassung"),
    "prosePrompt": ("ausführliche zusammenfassung", "ausführlich", "zusammenfassung"),
    "aufgabePrompt": ("aufgaben", "aufgabenliste", "zusammenfassung"),
    "gespraechsnotizPrompt": ("gesprächsnotiz", "gesprächsprotokoll"),
    "emailPrompt": ("e-mail", "email"),
    "stundenzettelPrompt": ("stundenzettel", "zeiterfassung"),
    "analogPrompt": ("analog", "zusammenfassung"),
}

#: Leerfall-Saetze aus K1. R kennt nur den der Aufgaben; die uebrigen treffen dort nie zu.
EMPTY_SENTENCES: dict[str, str] = {
    "abstractPrompt": "Kein zusammenfassbarer Inhalt.",
    "aufgabePrompt": "Keine Aufgaben genannt.",
    "gespraechsnotizPrompt": "Kein Gespräch dokumentiert.",
    "stundenzettelPrompt": "Keine Zeiten genannt.",
    "analogPrompt": "Kein Eintrag erkennbar.",
}

STRUCTURED_HEADINGS = frozenset({"Kontext", "Kernaussagen", "Entscheidungen", "Offene Punkte / ToDos"})

#: Gespraechsnotiz und E-Mail: 250 -> 400 nach dem Lesevergleich (21.09.2026) -- bei 426/452 Woertern
#: Diktat schnitt die Grenze genau das ab, was der Leser vermisste.
MAX_WORDS = {"gespraechsnotizPrompt": 400, "emailPrompt": 400, "analogPrompt": 120}
MAX_TASKS = 8
MAX_TASK_WORDS = 20

_GERMAN = frozenset(
    "der die das und ist nicht mit für ein eine einen zu von auf den dem des sich wird wurde "
    "werden im in bei als auch noch keine kein sie wir ich es sind hat haben dass oder über nach".split()
)
_ENGLISH = frozenset(
    "the and is of to with for a an on in be was were are this that it not or from by".split()
)

_BULLET = re.compile(r"^(\s*)([-*+]|\d+[.)])\s+")
_HEADING = re.compile(r"^\s{0,3}(#{1,6})\s*(.*?)\s*#*\s*$")
_PLACEHOLDER = re.compile(r"\bTBD\b|\[[^\]\n]{1,25}\]|\bXXX+\b|\?\?\?")
_PREAMBLE = re.compile(r"^(hier (ist|sind|folgt|die|das)|gerne|natürlich|selbstverständlich|sure|here)\b", re.I)
_FOLLOW_UP = re.compile(
    r"(\?\s*$)|(möchten sie|soll ich|wenn sie (möchten|wünschen)|lassen sie mich wissen|"
    r"sagen sie (mir )?bescheid)",
    re.I,
)
_GREETING_END = re.compile(
    r"(grüße|gruß|mit freundlichen|herzlich(e|en)? grüße?|viele grüße|beste grüße|lg\b)", re.I
)
_SALUTATION = re.compile(
    r"^(sehr geehrte[rn]?|liebe[rn]?|hallo|guten (tag|morgen|abend)|moin)\b", re.I
)
_INFORMAL = re.compile(r"\b(du|dich|dir|dein|deine|deinen|deinem|deiner|euch|euer|eure)\b", re.I)
#: „bitte prüfe …" -- Du-Imperativ ohne Pronomen, den `_INFORMAL` nicht sieht. Heuristik: Verb auf -e
#: nach „bitte"; Begleiter wie „eine" oder „diese" sind ausgenommen.
_DU_IMPERATIVE = re.compile(
    r"\bbitte\s+(?!(?:eine|keine|diese|jene|alle|meine|seine|ihre|unsere|die|kurze|gerne)\b)[a-zäöüß]+e\b(?!\s+Sie\b)",
    re.I,
)
#: Markierungen einer Transkriptionsluecke -- gehoeren in den Eintrag, nicht in eine Mail.
_UNCLEAR_MARK = re.compile(r"\[[^\]\n]*(?:unklar|unverständlich)[^\]\n]*\]|inhaltlich unklar|\bunverständlich\b", re.I)
_DURATION = re.compile(
    r"\d+(?:[.,]\d+)?\s*(?:h\b|std\b|stunden?\b|min\b|minuten?\b)|dauer nicht genannt", re.I
)
_NAME = re.compile(r"\b(?:Herr|Herrn|Frau)\s+([A-ZÄÖÜ][\wäöüß-]+)")


def words(text: str) -> list[str]:
    return re.findall(r"[\wäöüÄÖÜß-]+", text)


def word_limit_for(transcript: str) -> int:
    """Wie `WordLimitCalculator.Calculate` (Abstract-Grenze): 20 / 50 / 150."""
    count = len(transcript.split())
    return 20 if count < 300 else 50 if count < 1000 else 150


def _lines(text: str) -> list[str]:
    return [line for line in text.splitlines() if line.strip()]


def _strip_markup(line: str) -> str:
    return re.sub(r"^[#>*\-\s]+|[*:#\s]+$", "", line).strip().lower()


def is_german(text: str) -> bool | None:
    tokens = [w.lower() for w in words(text)]
    if len(tokens) < 3:
        return None
    de = sum(t in _GERMAN for t in tokens)
    en = sum(t in _ENGLISH for t in tokens)
    if de == 0 and en == 0:
        return bool(re.search(r"[äöüß]", text.lower())) or None
    return de >= 2 * en


def markdown_well_formed(text: str) -> bool:
    """Gerade Anzahl `**`, keine leere Ueberschrift, Einrueckung ohne Waisen."""
    if text.count("**") % 2:
        return False
    previous_indent: int | None = None
    for line in text.splitlines():
        heading = _HEADING.match(line)
        if heading and line.lstrip().startswith("#") and not heading.group(2):
            return False
        bullet = _BULLET.match(line)
        if bullet:
            indent = len(bullet.group(1).expandtabs(4))
            if indent > 0 and previous_indent is None:
                return False  # Unterpunkt ohne Elternpunkt
            previous_indent = indent
        elif line.strip():
            previous_indent = None
    return True


def has_nesting(text: str) -> bool:
    return any((m := _BULLET.match(line)) and m.group(1) for line in text.splitlines())


def names_not_in_transcript(output: str, transcript: str) -> list[str]:
    """Nachnamen nach Herr/Frau, die im Transkript nicht vorkommen -- Kandidaten fuer Erfindung."""
    known = transcript.lower()
    return sorted({name for name in _NAME.findall(output) if name.lower() not in known})


@dataclass(frozen=True)
class Result:
    section: str
    checks: dict[str, bool | None]
    metrics: dict[str, float | int | str]

    @property
    def violations(self) -> list[str]:
        return sorted(name for name, ok in self.checks.items() if ok is False)


def check(section: str, output: str, transcript: str) -> Result:
    text = output.strip()
    lines = _lines(text)
    n_words = len(words(text))
    empty_sentence = EMPTY_SENTENCES.get(section)
    is_empty_case = bool(empty_sentence) and empty_sentence in text
    first = lines[0] if lines else ""

    checks: dict[str, bool | None] = {
        "nicht_leer": bool(text),
        "deutsch": is_german(text) if text else None,
        "keine_vorrede": not _PREAMBLE.match(first) if first else None,
        "keine_rueckfrage": not _FOLLOW_UP.search(lines[-1]) if lines else None,
        "keine_platzhalter": not _PLACEHOLDER.search(text),
        "keine_selbstgenannte_ueberschrift": _strip_markup(first) not in DISPLAY_NAMES[section]
        if first
        else None,
        "markdown_wohlgeformt": markdown_well_formed(text),
    }
    metrics: dict[str, float | int | str] = {
        "woerter": n_words,
        "leerfall": int(is_empty_case),
        "verschachtelt": int(has_nesting(text)),
    }

    specific = _SPECIFIC.get(section)
    if specific:
        specific(text, lines, transcript, is_empty_case, checks, metrics)
    return Result(section=section, checks=checks, metrics=metrics)


# ----------------------------------------------------------------------- je Vorlage


def _no_markdown(text: str) -> bool:
    return not re.search(r"\*\*|^\s*#|^\s*[-*+]\s", text, re.MULTILINE)


def _abstract(text, lines, transcript, empty, checks, metrics) -> None:
    limit = word_limit_for(transcript)
    metrics["wortgrenze"] = limit
    checks["laenge_ok"] = None if empty else metrics["woerter"] <= limit
    checks["ein_absatz"] = "\n\n" not in text
    checks["kein_markdown"] = _no_markdown(text)


def _structured(text, lines, transcript, empty, checks, metrics) -> None:
    headings = [m.group(2) for line in lines if (m := _HEADING.match(line)) and line.lstrip().startswith("#")]
    checks["hat_ueberschriften"] = bool(headings)
    checks["nur_schema_ueberschriften"] = all(h in STRUCTURED_HEADINGS for h in headings) if headings else None
    empty_heading = False
    for index, line in enumerate(lines):
        if line.lstrip().startswith("#") and (index + 1 == len(lines) or lines[index + 1].lstrip().startswith("#")):
            empty_heading = True
    checks["keine_leere_ueberschrift"] = not empty_heading


def _prose(text, lines, transcript, empty, checks, metrics) -> None:
    source = max(len(words(transcript)), 1)
    metrics["verhaeltnis"] = round(metrics["woerter"] / source, 2)
    bullets = sum(bool(_BULLET.match(line)) for line in lines)
    checks["fliesstext"] = bullets <= len(lines) / 2 if lines else None


def _tasks(text, lines, transcript, empty, checks, metrics) -> None:
    bullets = [line for line in lines if _BULLET.match(line)]
    metrics["aufgaben"] = len(bullets)
    checks["absatz_vor_liste"] = not _BULLET.match(lines[0]) if lines else None
    checks["liste_oder_leerfall"] = bool(bullets) != empty if lines else None
    checks["hoechstens_acht"] = len(bullets) <= MAX_TASKS
    checks["hoechstens_20_woerter"] = all(len(words(b)) <= MAX_TASK_WORDS for b in bullets) if bullets else None
    checks["bindestrich"] = all(re.match(r"^- (?!\[)", b.lstrip()) for b in bullets) if bullets else None
    checks["keine_nummerierung"] = not re.search(r"Aufgabe\s+\d+\s*:", text)
    checks["keine_ueberschriften"] = not any(line.lstrip().startswith("#") for line in lines)


def _note(text, lines, transcript, empty, checks, metrics) -> None:
    invented = names_not_in_transcript(text, transcript)
    metrics["namen_ohne_beleg"] = ", ".join(invented)
    checks["namen_belegt"] = not invented
    checks["laenge_ok"] = None if empty else metrics["woerter"] <= MAX_WORDS["gespraechsnotizPrompt"]
    checks["keine_folgemail"] = not re.search(r"^\s*Betreff:", text, re.MULTILINE)
    checks["leerfall_allein"] = text == EMPTY_SENTENCES["gespraechsnotizPrompt"] if empty else None


def _email(text, lines, transcript, empty, checks, metrics) -> None:
    has_subject = bool(lines) and lines[0].startswith("Betreff:")
    body = lines[1:] if has_subject else lines
    metrics["woerter_textkoerper"] = len(words("\n".join(body)))
    checks["betreff_erste_zeile"] = has_subject
    checks["anrede"] = bool(body) and bool(_SALUTATION.match(body[0]))
    checks["kein_gruss_am_ende"] = not _GREETING_END.search(body[-1]) if body else None
    checks["kein_markdown"] = _no_markdown(text)
    checks["sie_form"] = not _INFORMAL.search(text)
    checks["kein_du_imperativ"] = not _DU_IMPERATIVE.search(text)
    checks["keine_unklarheitsmarke"] = not _UNCLEAR_MARK.search(text)
    # „ist noch unklar" kann echter Inhalt sein -- nur zaehlen und lesen, nicht verwerfen.
    metrics["unklar_erwaehnt"] = int(bool(re.search(r"\bunklar\b", text, re.I)))
    checks["laenge_ok"] = metrics["woerter_textkoerper"] <= MAX_WORDS["emailPrompt"]


def _timesheet(text, lines, transcript, empty, checks, metrics) -> None:
    bullets = [line for line in lines if _BULLET.match(line)]
    metrics["zeilen"] = len(bullets)
    checks["liste_oder_leerfall"] = bool(bullets) != empty if lines else None
    checks["jede_zeile_mit_dauer"] = all(_DURATION.search(b) for b in bullets) if bullets else None
    checks["keine_ueberschriften"] = not any(line.lstrip().startswith("#") for line in lines)


def _analog(text, lines, transcript, empty, checks, metrics) -> None:
    checks["laenge_ok"] = None if empty else metrics["woerter"] <= MAX_WORDS["analogPrompt"]
    bullets = sum(bool(_BULLET.match(line)) for line in lines)
    checks["fliesstext"] = bullets == 0


_SPECIFIC = {
    "abstractPrompt": _abstract,
    "structuredPrompt": _structured,
    "prosePrompt": _prose,
    "aufgabePrompt": _tasks,
    "gespraechsnotizPrompt": _note,
    "emailPrompt": _email,
    "stundenzettelPrompt": _timesheet,
    "analogPrompt": _analog,
}
