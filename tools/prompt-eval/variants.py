"""Erzeugt Prompt-Varianten aus einem Basissatz und einem Faktorvektor.

Hintergrund: `docs/prompting/faktoren-arm1-2026-09-13.md`. Sieben Faktoren, vier in der
Systemnachricht und drei in den Abschnitts-Prompts. Bei sieben binaeren Faktoren gibt es
128 Kombinationen; der Versuchsplan zieht daraus rund 40. Von Hand geschriebene Dateien
skalieren dabei nicht und driften auseinander, deshalb wird jede Variante aus einem Basissatz
plus Transformationen *erzeugt*.

Der Prompt-Wortlaut selbst steht nicht in dieser Datei. Basis und Ersatztexte liegen in der
Sandbox (`Documents\\Johann\\prompt-sandbox\\`), weil sie firmeninternes Team-Material sind.
Hier steht nur der Mechanismus.

Sicherheitsnetz: Jede Transformation prueft, dass sie tatsaechlich etwas veraendert hat, und
bricht sonst ab. Eine still fehlschlagende Textersetzung hat in diesem Projekt schon einmal
einen Befund vorgetaeuscht -- siehe `docs/pilot/` im Schwesterprojekt.
"""

from __future__ import annotations

import argparse
import functools
import json
import re
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Callable, Iterable

# --------------------------------------------------------------------------- Konstanten

SECTION_KEYS: tuple[str, ...] = (
    "abstractPrompt",
    "structuredPrompt",
    "prosePrompt",
    "emailPrompt",
    "aufgabePrompt",
    "gespraechsnotizPrompt",
    "stundenzettelPrompt",
    "analogPrompt",
)

#: Abschnitte, die die falsche Sprachpraemisse tragen (Befund B-01).
LANGUAGE_PREMISE_KEYS: tuple[str, ...] = (
    "abstractPrompt",
    "structuredPrompt",
    "gespraechsnotizPrompt",
    "stundenzettelPrompt",
    "analogPrompt",
)

#: Abschnitte mit je eigener, leicht abweichender Ueberschriften-Unterdrueckung (Befund B-04).
HEADING_RULE_KEYS: tuple[str, ...] = (
    "prosePrompt",
    "gespraechsnotizPrompt",
    "stundenzettelPrompt",
    "analogPrompt",
)

#: Untergrenze fuer Prompt-Caching. Faellt das Praefix darunter, entfaellt der Rabatt von
#: bis zu 90 % -- Kuerzen kann die Kosten dann *erhoehen*. Siehe Faktorenplan, Abschnitt 1.
CACHE_THRESHOLD_TOKENS = 1024

#: Gemessener Wert fuer deutsche Fliesstexte. Nur Rueckfallebene, wenn tiktoken fehlt.
CHARS_PER_TOKEN_FALLBACK = 4.3


@dataclass(frozen=True)
class Factors:
    """Ein Punkt im Faktorraum. Die Vorgabewerte sind der heutige Zustand (Referenz)."""

    s1_denkprozess: bool = True
    """Vorgeschriebener Denkprozess-Block in der Systemnachricht (S-02)."""

    s2_grossschreibung: bool = True
    """Durchgehende Grossschreibung (S-01). Kostet rund 37 % Token."""

    s3_verbote: bool = True
    """Regeln als Verbotsliste statt positiv formuliert (S-07)."""

    s4_zentralregel: bool = False
    """Markdown- und Ueberschriften-Regel zentral in der Systemnachricht (B-03, B-04)."""

    a1_sprachpraemisse: bool = True
    """Die falsche Praemisse "auf Deutsch" in fuenf Abschnitts-Prompts (B-01)."""

    a2_leerfall: bool = False
    """Regel fuer den Fall, dass das Transkript nichts Passendes enthaelt (B-05)."""

    a3_vollvertrag: bool = False
    """Voller Ausgabevertrag -- Laenge, Format und Leerfall -- statt Minimalvertrag."""

    @property
    def cell_id(self) -> str:
        """Kurzkennung fuer Dateinamen und Ergebniszeilen, z. B. 'S1-S2-A3'."""
        flags = [
            ("S1", self.s1_denkprozess),
            ("S2", self.s2_grossschreibung),
            ("S3", self.s3_verbote),
            ("S4", self.s4_zentralregel),
            ("A1", self.a1_sprachpraemisse),
            ("A2", self.a2_leerfall),
            ("A3", self.a3_vollvertrag),
        ]
        active = [name for name, on in flags if on]
        return "-".join(active) if active else "BASIS"


class TransformError(RuntimeError):
    """Eine Transformation hat nichts veraendert. Fast immer ein veralteter Anker."""


def _expect_change(before: str, after: str, what: str) -> str:
    """Bricht ab, wenn eine Ersetzung wirkungslos war.

    Der Grund fuer diese Huerde: `str.replace` schlaegt still fehl. Ein verschobenes
    Leerzeichen im Basistext liesse die Variante dann unveraendert durchlaufen und der
    gesamte Versuch verglichen die Referenz mit sich selbst.
    """
    if before == after:
        raise TransformError(
            f"{what}: Text unveraendert. Anker passt nicht mehr zum Basissatz -- "
            f"Transformation pruefen, nicht ueberspringen."
        )
    return after


# --------------------------------------------------------------- Konstante Korrekturen


def resolve_contradictions(prompts: dict[str, str], snippets: dict[str, str]) -> dict[str, str]:
    """Loest S-03 und S-04 auf. Konstant, kein Faktor.

    Der Vorversuch hat gemessen, dass diese Aufloesung fuer sich genommen nichts bringt
    (p = 0,754; im Median 72 gegen 73 Denk-Token). Sie kostet also nichts, macht den Prompt
    aber fuer Menschen wartbar. Als Faktor wuerde sie einen Plan-Slot fuer eine beantwortete
    Frage verbrennen.

    Aus jedem Widerspruchspaar bleibt die Haelfte stehen, die der *Treue* dient.
    """
    out = dict(prompts)
    system = out["systemMessage"]
    for anchor_key in ("s03_entfernen", "s04_entfernen"):
        anchor = snippets[anchor_key]
        system = _expect_change(system, system.replace(anchor, ""), f"Widerspruch {anchor_key}")
    out["systemMessage"] = _tidy(system)
    return out


def relocate_ambiguity_rule(prompts: dict[str, str], snippets: dict[str, str]) -> dict[str, str]:
    """Verschiebt "Unklarheiten neutral markieren" in die Instruktionen. Konstant.

    ⚠ Diese Funktion beseitigt eine Konfundierung, die den Versuch entwertet haette.

    Die Regel stand bisher als Schritt im Denkprozess-Block. Wird dieser Block von Faktor S1
    entfernt, verschwindet mit dem *Geruest* zugleich eine *inhaltliche* Regel -- und S1 wuerde
    zwei Dinge auf einmal messen: das Weglassen vorgeschriebener Denkschritte und das Weglassen
    einer Treue-Regel.

    Genau das ist im Vorversuch passiert: dessen Variante B entfernte beides zusammen. Indem
    die Regel hier unabhaengig von S1 in die Instruktionen wandert, misst S1 nur noch das
    Geruest. Faellt der Effekt dadurch weg, war nicht der Denkprozess der Wirkstoff, sondern
    die mitgeloeschte Regel -- was ein besserer Befund waere als der urspruengliche.
    """
    out = dict(prompts)
    system = out["systemMessage"]
    rule = snippets["unklarheiten_regel"]
    system = _expect_change(system, system.replace(rule, ""), "Unklarheiten-Regel entnehmen")
    system = _insert_into_instructions(system, snippets["unklarheiten_regel_positiv"])
    out["systemMessage"] = _tidy(system)
    return out


def scope_compression_rule(prompts: dict[str, str], snippets: dict[str, str]) -> dict[str, str]:
    """Gibt der Verdichtungsregel einen Geltungsbereich. Konstant. Loest P-01.

    Produktentscheidung vom 13.09.2026: "Ausfuehrlich" ist vollstaendige Aufbereitung, nicht
    Verdichtung. Die Systemnachricht verlangte Verdichtung bislang pauschal und widersprach
    damit dem Prosa-Abschnitt.
    """
    out = dict(prompts)
    system = out["systemMessage"]
    system = _expect_change(
        system,
        system.replace(snippets["verdichtung_alt"], snippets["verdichtung_neu"]),
        "Verdichtungsregel einschraenken",
    )
    out["systemMessage"] = _tidy(system)
    return out


# ------------------------------------------------------------------- Faktoren, System


def drop_thinking_block(prompts: dict[str, str]) -> dict[str, str]:
    """S1 = aus: entfernt den Denkprozess-Block (S-02), rund 300 Token je Aufruf.

    Entfernt wird von der Blockueberschrift bis zur naechsten `###`-Ueberschrift. Nur noch
    Geruest, seit `relocate_ambiguity_rule` die inhaltliche Regel herausgeloest hat.
    """
    out = dict(prompts)
    system = out["systemMessage"]
    pattern = re.compile(
        r"^###\s*CHAIN OF THOUGHTS.*?$.*?(?=^###)",
        re.MULTILINE | re.DOTALL,
    )
    out["systemMessage"] = _tidy(
        _expect_change(system, pattern.sub("", system), "Denkprozess-Block entfernen")
    )
    return out


def normalise_case(prompts: dict[str, str], snippets: dict) -> dict[str, str]:
    """S2 = aus: wandelt durchgehende Grossschreibung in normale Schreibung (S-01).

    Blockueberschriften (`### ... ###`) bleiben unberuehrt, weil sie Struktur und nicht Betonung
    sind, ebenso Platzhalter wie `{word_limit}`.

    ⚠ Warum es hier eine Korrekturliste gibt: Die Heuristik kann deutsche Substantive nicht
    erkennen. Aus "NIEMALS UMGANGSSPRACHE VERWENDEN" wuerde sonst "Niemals umgangssprache
    verwenden" -- und dann unterschiede sich die Faktorstufe nicht mehr nur in der Schreibweise,
    sondern auch in der Korrektheit des Deutschen. Der Faktor wuerde zwei Dinge gleichzeitig
    messen. `grossschreibung_korrekturen` in der Sandbox haelt die Ausnahmen; die Liste ist
    kurz, weil sie nur die Woerter enthaelt, die in diesem Prompt tatsaechlich vorkommen.
    """
    out = dict(prompts)
    system = out["systemMessage"]

    mapping: dict[str, str] = snippets["grossschreibung"]
    protected: list[str] = []

    def stash(match: re.Match[str]) -> str:
        protected.append(match.group(0))
        return f"\x00{len(protected) - 1}\x00"

    guarded = re.sub(r"^###.*?###\s*$|\{[a-z_]+\}", stash, system, flags=re.MULTILINE)

    unknown: set[str] = set()

    def lookup(match: re.Match[str]) -> str:
        token = match.group(0)
        if token not in mapping:
            unknown.add(token)
            return token
        return mapping[token]

    converted = re.sub(r"[A-ZÄÖÜ][A-ZÄÖÜß]+", lookup, guarded)
    if unknown:
        raise TransformError(
            "Grossschreibung: keine Zuordnung fuer "
            + ", ".join(sorted(unknown))
            + ". In 'grossschreibung' ergaenzen -- eine Heuristik wuerde hier falsches Deutsch "
            "erzeugen und den Faktor mit der Sprachrichtigkeit vermengen."
        )

    # Satzanfaenge wieder gross: Zeilen- und Aufzaehlungsanfang sowie nach Satzzeichen.
    converted = re.sub(
        r"(?m)^(\s*(?:[-*]\s+|\d+\.\s+)?)([a-zäöü])",
        lambda m: m.group(1) + m.group(2).upper(),
        converted,
    )
    converted = re.sub(
        r"([.!?]\s+)([a-zäöü])", lambda m: m.group(1) + m.group(2).upper(), converted
    )
    restored = re.sub(r"\x00(\d+)\x00", lambda m: protected[int(m.group(1))], converted)

    out["systemMessage"] = _expect_change(system, restored, "Grossschreibung normalisieren")
    return out


def positive_rules(prompts: dict[str, str], snippets: dict[str, str]) -> dict[str, str]:
    """S3 = aus: ersetzt den Verbotsblock durch positiv formulierte Regeln (S-07).

    Der Ersatztext liegt in der Sandbox. Er bildet dieselben acht Regeln ab, nur als
    Handlungsanweisung statt als Verbot -- und laesst die Regel aus, die
    `resolve_contradictions` ohnehin entfernt hat, damit beide Stufen denselben Vertrag tragen.
    """
    out = dict(prompts)
    system = out["systemMessage"]
    pattern = re.compile(r"^###\s*WHAT NOT TO DO\s*###.*?(?=^\*\*SCHLECHTES)", re.MULTILINE | re.DOTALL)
    replaced = pattern.sub(snippets["positive_regeln"] + "\n\n", system)
    out["systemMessage"] = _tidy(_expect_change(system, replaced, "Verbote positiv umschreiben"))
    return out


def central_format_rule(prompts: dict[str, str], snippets: dict[str, str]) -> dict[str, str]:
    """S4 = ein: eine Markdown- und Ueberschriften-Regel zentral (B-03, B-04).

    Die Regel wandert *einmal* in die Instruktionen, und die vier abweichenden
    Ueberschriften-Saetze in den Abschnitten entfallen. Bislang verlangt kein einziger Prompt
    Markdown, obwohl die Anwendung jeden Abschnitt als Markdown rendert -- brauchbare Ausgabe
    war Zufall, nicht Vertrag.
    """
    out = dict(prompts)
    out["systemMessage"] = _tidy(
        _insert_into_instructions(out["systemMessage"], snippets["zentrale_formatregel"])
    )
    pattern = re.compile(snippets["ueberschriften_regel_muster"])
    for key in HEADING_RULE_KEYS:
        text = out[key]
        cleaned = pattern.sub("", text, count=1)
        out[key] = _tidy(_expect_change(text, cleaned, f"Ueberschriften-Regel in {key}"))
    return out


# --------------------------------------------------------------- Faktoren, Abschnitte


def drop_language_premise(prompts: dict[str, str], snippets: dict) -> dict[str, str]:
    """A1 = aus: streicht die falsche Praemisse "auf Deutsch" (B-01).

    Seit #58 bleibt das Transkript in der gesprochenen Sprache; nur die erzeugten Abschnitte
    sind deutsch. Die Systemnachricht sagt das korrekt, fuenf Abschnitts-Prompts widersprechen
    ihr -- und ein Widerspruch ist teurer als eine Luecke.
    """
    out = dict(prompts)
    for key in LANGUAGE_PREMISE_KEYS:
        text = out[key]
        cleaned = re.sub(snippets["sprachpraemisse_muster"], "", text, count=1)
        out[key] = _expect_change(text, cleaned, f"Sprachpraemisse in {key}")
    return out


def add_empty_case(prompts: dict[str, str], snippets: dict[str, str]) -> dict[str, str]:
    """A2 = ein: ergaenzt eine Leerfall-Regel (B-05).

    Nur `aufgabePrompt` sagt bisher, was bei einem Transkript ohne passenden Inhalt zu tun ist.
    Dass das Modell in diesem Fall nichts erfindet, wurde einmal gemessen -- an einem einzigen
    Diktat. Ob das traegt, ist offen, und genau deshalb ist es ein Faktor.
    """
    out = dict(prompts)
    for key in SECTION_KEYS:
        if key == "aufgabePrompt":
            continue  # hat die Regel bereits
        rule = snippets["leerfall"].get(key) if isinstance(snippets["leerfall"], dict) else None
        if rule is None:
            rule = snippets["leerfall_standard"]
        out[key] = _add_clause(out[key], rule)
    return out


def full_contract(prompts: dict[str, str], snippets: dict[str, str]) -> dict[str, str]:
    """A3 = ein: ergaenzt Laengen- und Formatvertrag (N-02, Z-02, Z-03, L-02).

    Testet die eigentliche Streitfrage der Befundliste: hilft mehr Vertrag, oder schadet er?
    Thelwall (2024) fand, dass kuerzere Anweisungen bei komplexen Textbewertungen *schlechtere*
    Ergebnisse lieferten -- der Gegenbeleg zur verbreiteten Kuerzungsthese.
    """
    out = dict(prompts)
    contracts: dict[str, str] = snippets["ausgabevertrag"]
    for key, clause in contracts.items():
        out[key] = _add_clause(out[key], clause)
    return out


# ------------------------------------------------------------------------- Hilfsmittel


def _insert_into_instructions(system: str, text: str) -> str:
    """Haengt eine Regel an das Ende des Instruktionsblocks an."""
    pattern = re.compile(r"(^###\s*INSTRUKTIONEN\s*###.*?)(?=^###)", re.MULTILINE | re.DOTALL)
    match = pattern.search(system)
    if match is None:
        raise TransformError("Instruktionsblock nicht gefunden -- Basissatz geaendert?")
    block = match.group(1).rstrip()
    # Vor den abschliessenden Trenner einsetzen, nicht dahinter -- sonst steht die Regel
    # sichtbar ausserhalb des Blocks, zu dem sie gehoert.
    if block.endswith("---"):
        block = block[: -len("---")].rstrip() + "\n" + text + "\n\n---"
    else:
        block = block + "\n" + text
    return system[: match.start(1)] + block + "\n\n" + system[match.end(1) :]


def _add_clause(text: str, clause: str) -> str:
    """Setzt eine Klausel vor die Eingabe, nicht dahinter.

    Die Abschnitts-Vorlagen tragen den Platzhalter `{transcript}` in der Mitte. Wird eine Regel
    einfach angehaengt, steht sie hinter dem Diktat -- die Anweisung waere dann durch den
    gesamten Eingabetext von ihrem Kontext getrennt, und bei langen Diktaten waere unklar, ob
    sie ueberhaupt noch als Anweisung gelesen wird. Sie gehoert zu den uebrigen Anweisungen.
    """
    marker = re.search(r"(?m)^\s*(Transkript|Zusammenfassung)\s*:\s*$", text)
    if marker is None:
        return _tidy(text.rstrip() + "\n\n" + clause)
    head = text[: marker.start()].rstrip()
    tail = text[marker.start() :]
    return _tidy(head + "\n\n" + clause + "\n\n" + tail)


def _tidy(text: str) -> str:
    """Normalisiert Leerzeilen, damit Transformationen keine Luecken hinterlassen."""
    return re.sub(r"\n{3,}", "\n\n", text).strip() + "\n"


@functools.lru_cache(maxsize=4096)
def count_tokens(text: str) -> int:
    """Zaehlt Token mit tiktoken.

    ⚠ Ohne tiktoken wird abgebrochen statt geschaetzt. Die Zeichen-Naeherung von 4,3 gilt fuer
    normale deutsche Prosa; durchgehend grossgeschriebener Text tokenisiert bei rund 3,14
    Zeichen je Token und wird dadurch um etwa 30 % zu niedrig geschaetzt. Da die gesamte
    Cache-Schwellen-Rechnung an diesen Zahlen haengt, waere eine stille Schaetzung hier
    gefaehrlicher als ein Abbruch: ein erster Probelauf meldete deswegen 733 statt rund 1.008
    Token und haette jede Variante faelschlich als nicht cachefaehig ausgewiesen.
    """
    try:
        import tiktoken
    except ImportError as exc:  # pragma: no cover - Umgebungsproblem, kein Logikpfad
        raise RuntimeError(
            "tiktoken fehlt. Die Zeichen-Naeherung unterschaetzt grossgeschriebenen Text um "
            "rund 30 %, und die Cache-Schwelle haengt daran. Installieren mit: "
            "pip install tiktoken"
        ) from exc

    return len(tiktoken.get_encoding("o200k_base").encode(text))


def build(base: dict[str, str], snippets: dict, factors: Factors) -> dict[str, str]:
    """Erzeugt einen vollstaendigen Prompt-Satz fuer einen Punkt im Faktorraum."""
    prompts = {k: v for k, v in base.items() if isinstance(v, str)}

    # Konstanten zuerst -- sie gelten in jeder Zelle und duerfen nicht mit Faktoren wandern.
    prompts = resolve_contradictions(prompts, snippets)
    prompts = relocate_ambiguity_rule(prompts, snippets)
    prompts = scope_compression_rule(prompts, snippets)

    # Reihenfolge ist bedeutsam: erst strukturelle Eingriffe, zuletzt die Schreibweise.
    # `normalise_case` veraendert den Text so weit, dass die Anker der uebrigen
    # Transformationen danach nicht mehr greifen -- der Probelauf ist genau daran gescheitert.
    if not factors.s1_denkprozess:
        prompts = drop_thinking_block(prompts)
    if not factors.s3_verbote:
        prompts = positive_rules(prompts, snippets)
    if factors.s4_zentralregel:
        prompts = central_format_rule(prompts, snippets)
    if not factors.a1_sprachpraemisse:
        prompts = drop_language_premise(prompts, snippets)
    if factors.a2_leerfall:
        prompts = add_empty_case(prompts, snippets)
    if factors.a3_vollvertrag:
        prompts = full_contract(prompts, snippets)
    if not factors.s2_grossschreibung:
        prompts = normalise_case(prompts, snippets)

    return prompts


def prefix_report(prompts: dict[str, str]) -> dict[str, dict[str, int | bool]]:
    """Praefixlaenge je Abschnitt und Cache-Faehigkeit.

    Das Praefix ist Systemnachricht plus Abschnitts-Vorlage. Liegt es unter der Schwelle,
    entfaellt der Cache-Rabatt -- und Kuerzen wird teurer statt billiger. Das Transkript zaehlt
    im echten Aufruf mit, faengt bei kurzen Archiv-Diktaten (Median 21 Token) aber nichts auf.
    """
    system_tokens = count_tokens(prompts["systemMessage"])
    report: dict[str, dict[str, int | bool]] = {
        "systemMessage": {"tokens": system_tokens, "cacheable": False}
    }
    for key in SECTION_KEYS:
        if key not in prompts:
            continue
        total = system_tokens + count_tokens(prompts[key])
        report[key] = {
            "tokens": total,
            "cacheable": total >= CACHE_THRESHOLD_TOKENS,
            "margin": total - CACHE_THRESHOLD_TOKENS,
        }
    return report


# ------------------------------------------------------------ Kandidaten (#73, schlank)
#
# Seit dem 15.09.2026 entscheidet Arm 1 nicht mehr ueber 48 Faktorzellen, sondern ueber
# hoechstens drei Kandidaten (`docs/prompting/stand-73-2026-09-15.md` §6). K1 ist *je Vorlage*
# zugeschnitten (`soll-profil-73.md`) und laesst sich deshalb nicht als Kette pauschaler
# Transformationen ausdruecken. Die Fassungen stehen vollstaendig in der Sandbox; dieses Modul
# setzt sie ein und prueft, was beim Umschreiben von Hand verloren gehen kann.

#: Platzhalter, die `SummaryGenerator` ersetzt. Fehlt einer, steht die Anweisung ohne Eingabe da.
PLACEHOLDER = re.compile(r"\{[a-z_]+\}")

#: OpenAI cached den laengsten gemeinsamen Praefix ab 1.024 Token in Schritten von 128.
CACHE_BLOCK_TOKENS = 128

#: Titel-Prompt aus `SummaryGenerator.GenerateTitleAsync`. Steht im Code, nicht in der
#: Team-Datei, schickt aber die Systemnachricht mit und zaehlt deshalb zu jedem Diktat.
TITLE_PROMPT = (
    "Bitte formuliere einen sehr kurzen, prägnanten Titel (maximal 3-7 Worte) für den "
    "folgenden Text. Antworte NUR mit dem Titel, ohne Anführungszeichen oder Erklärungen:"
    "\n\n{transcript}"
)

#: Aufrufe je Diktat mit `SectionModeDefaults.Recommended`: Titel, Abstract (immer) und die
#: vier automatischen Abschnitte. E-Mail, Stundenzettel und Analog laufen auf Abruf.
AUTO_CALLS: tuple[str, ...] = (
    "title",
    "abstractPrompt",
    "structuredPrompt",
    "prosePrompt",
    "aufgabePrompt",
    "gespraechsnotizPrompt",
)

#: Erlaubte Versalwoerter in einer normal geschriebenen Systemnachricht (Abkuerzungen).
_ALLOWED_CAPS = frozenset({"PDF", "HTML", "ASCII"})


class CandidateError(ValueError):
    """Ein Kandidat verletzt eine Invariante, die jede Fassung erfuellen muss."""


@dataclass(frozen=True)
class Prices:
    """Preise je Million Token."""

    input_per_million: float
    cached_per_million: float


def build_candidate(base: dict, overrides: dict[str, str]) -> dict:
    """Setzt die Fassungen eines Kandidaten in den Basissatz ein und prueft ihn.

    Nicht-Text-Felder des Basissatzes (z. B. `promptDefaultsRevision`) bleiben erhalten, damit
    die Datei als Team-Datei gueltig bleibt.
    """
    unknown = sorted(set(overrides) - set(base))
    if unknown:
        raise CandidateError(f"Unbekannte Schluessel: {', '.join(unknown)}")
    merged = {**base, **overrides}
    problems = validate_candidate(base, merged)
    if problems:
        raise CandidateError("Kandidat ungueltig:\n  - " + "\n  - ".join(problems))
    return merged


def _example_lines(system: str) -> list[str]:
    """Die zitierten Beispielsaetze unter den Beispiel-Ueberschriften (Befund S-08)."""
    return re.findall(r"BEISPIEL:\*\*\s*\n(\S[^\n]*)", system, flags=re.IGNORECASE)


def validate_candidate(base: dict, prompts: dict) -> list[str]:
    """Alle Verstoesse eines Kandidaten gegen die Festsetzungen -- leer, wenn er taugt."""
    problems: list[str] = []
    for key in ("systemMessage", *SECTION_KEYS):
        if not isinstance(prompts.get(key), str) or not prompts[key].strip():
            problems.append(f"{key}: fehlt oder leer")
            continue
        expected = sorted(set(PLACEHOLDER.findall(base[key])))
        actual = sorted(set(PLACEHOLDER.findall(prompts[key])))
        if expected != actual:
            problems.append(f"{key}: Platzhalter {actual} statt {expected}")

    for key in SECTION_KEYS:
        if re.search(r"^[^\n]*Transkript[^\n]*auf Deutsch", prompts.get(key, ""), re.MULTILINE):
            problems.append(f"{key}: Sprachpraemisse 'auf Deutsch' (B-01)")

    system = prompts.get("systemMessage", "")
    if re.search(r"CHAIN OF THOUGHT|DENKPROZESS", system, re.IGNORECASE):
        problems.append("systemMessage: Denkprozess-Block ist noch enthalten (S-02)")
    shouting = sorted(
        {w for w in re.findall(r"\b[A-ZÄÖÜ]{4,}\b", system) if w not in _ALLOWED_CAPS}
    )
    if shouting:
        problems.append(f"systemMessage: Grossschreibung {shouting[:5]} (S-01)")
    for line in _example_lines(base.get("systemMessage", "")):
        if line not in system:
            problems.append(f"systemMessage: Beispielsatz veraendert oder entfernt (S-08): {line[:40]}…")
    return problems


def cached_tokens(prefix_tokens: int) -> int:
    """Wie viele Token eines stabilen Praefixes der Cache hoechstens traegt."""
    if prefix_tokens < CACHE_THRESHOLD_TOKENS:
        return 0
    extra = (prefix_tokens - CACHE_THRESHOLD_TOKENS) // CACHE_BLOCK_TOKENS
    return CACHE_THRESHOLD_TOKENS + extra * CACHE_BLOCK_TOKENS


def _template(prompts: dict, call: str) -> str:
    return TITLE_PROMPT if call == "title" else prompts[call]


def stable_prefix_tokens(system: str, template: str) -> int:
    """Systemnachricht plus Vorlage bis zum ersten Platzhalter.

    Beim Abstract endet der stabile Teil schon bei `{word_limit}`, weil die Zahl je nach
    Diktatlaenge wechselt -- alles dahinter ist fuer den Cache ein anderer Text.
    """
    head = PLACEHOLDER.split(template, maxsplit=1)[0]
    return count_tokens(system) + count_tokens(head)


def call_cost(
    system: str, template: str, transcript_tokens: int, prices: Prices, *, hit: bool
) -> float:
    """Eingabekosten eines Aufrufs in Dollar. Ausgabe wird in Schritt 2 gemessen, nicht geschaetzt."""
    total = count_tokens(system) + count_tokens(PLACEHOLDER.sub("", template)) + transcript_tokens
    cached = min(cached_tokens(stable_prefix_tokens(system, template)), total) if hit else 0
    return (cached * prices.cached_per_million + (total - cached) * prices.input_per_million) / 1e6


def cost_report(prompts: dict, transcript_tokens: list[int], prices: Prices) -> dict:
    """Praefix, Cache-Faehigkeit und mittlere Eingabekosten je Aufruf und je Diktat.

    Zwei Szenarien, weil keines allein stimmt: *ohne Treffer* ist der Alltag mit wenigen, weit
    auseinanderliegenden Diktaten (der Cache lebt Minuten); *mit Treffer* ist die Obergrenze,
    wenn derselbe Abschnitt kurz zuvor schon lief. ⚠ Die E-Mail bekommt die Prosa statt des
    Transkripts; deren Laenge wird hier durch die Transkriptlaenge angenaehert.
    """
    system = prompts["systemMessage"]
    calls = ("title", *SECTION_KEYS)
    n = max(len(transcript_tokens), 1)
    per_call: dict[str, dict] = {}
    for call in calls:
        template = _template(prompts, call)
        prefix = stable_prefix_tokens(system, template)
        miss = sum(call_cost(system, template, t, prices, hit=False) for t in transcript_tokens)
        hit = sum(call_cost(system, template, t, prices, hit=True) for t in transcript_tokens)
        per_call[call] = {
            "stable_prefix": prefix,
            "cached_tokens": cached_tokens(prefix),
            "cacheable": prefix >= CACHE_THRESHOLD_TOKENS,
            "usd_miss": miss / n,
            "usd_hit": hit / n,
        }
    return {
        "system_tokens": count_tokens(system),
        "calls": per_call,
        "per_dictation_auto": {
            "usd_miss": sum(per_call[c]["usd_miss"] for c in AUTO_CALLS),
            "usd_hit": sum(per_call[c]["usd_hit"] for c in AUTO_CALLS),
        },
    }


def catalog_prices(catalog_source: str, cache_discount: float) -> Prices:
    """Liest den Eingabepreis des Standardmodells (erster Katalogeintrag) aus dem C#-Quelltext.

    Der Katalog kennt keinen Preis fuer gecachte Token; der Rabatt kommt deshalb als Parameter
    und steht im Manifest, statt still angenommen zu werden.
    """
    match = re.search(r"PriceInPerMillion:\s*([0-9.]+)", catalog_source)
    if match is None:
        raise ValueError("PriceInPerMillion nicht im Katalog gefunden -- Quelltext geaendert?")
    price = float(match.group(1))
    return Prices(input_per_million=price, cached_per_million=price * (1 - cache_discount))


def _run_candidates(args: argparse.Namespace, base: dict) -> int:
    corpus = json.loads(args.korpus.read_text(encoding="utf-8"))
    items = corpus["items"] if isinstance(corpus, dict) else corpus
    tokens = [count_tokens(item["text"]) for item in items]
    prices = catalog_prices(args.katalog.read_text(encoding="utf-8"), args.cache_rabatt)

    candidates: dict[str, dict] = {"R": base}
    for spec in args.kandidat:
        name, _, path = spec.partition("=")
        if not name or not path or name == "R":
            raise SystemExit(f"--kandidat erwartet NAME=PFAD (nicht 'R'): {spec}")
        candidates[name] = build_candidate(base, json.loads(Path(path).read_text(encoding="utf-8")))

    args.out.mkdir(parents=True, exist_ok=True)
    manifest: dict = {
        "korpus": str(args.korpus),
        "diktate": len(items),
        "preise": asdict(prices),
        "cache_rabatt": args.cache_rabatt,
        "kandidaten": {},
    }
    print(f"{'Kandidat':<9}{'System':>8}{'je Diktat ohne Cache':>22}{'mit Cache':>12}  nicht cachefaehig")
    for name, prompts in candidates.items():
        target = args.out / f"prompts.{name}.json"
        target.write_text(json.dumps(prompts, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        report = cost_report(prompts, tokens, prices)
        manifest["kandidaten"][name] = {"datei": target.name, **report}
        blocked = [c for c, v in report["calls"].items() if not v["cacheable"]]
        auto = report["per_dictation_auto"]
        print(
            f"{name:<9}{report['system_tokens']:>8}"
            f"{auto['usd_miss'] * 100:>20.4f} ¢{auto['usd_hit'] * 100:>10.4f} ¢  "
            f"{len(blocked)}/{len(report['calls'])}"
        )

    manifest_path = args.out / "manifest.json"
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"\n{len(candidates)} Kandidaten in {args.out}  (Details: {manifest_path.name})")
    return 0


def main(argv: Iterable[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", type=Path, required=True, help="Basis-prompts.json")
    parser.add_argument("--snippets", type=Path, help="Ersatztexte (JSON), nur fuer --design")
    parser.add_argument("--out", type=Path, required=True, help="Zielverzeichnis")
    parser.add_argument(
        "--design",
        type=Path,
        help="JSON-Liste von Faktorvektoren. Fehlt sie, wird nur die Referenz erzeugt.",
    )
    parser.add_argument(
        "--kandidat",
        action="append",
        default=[],
        metavar="NAME=PFAD",
        help="Kandidat aus vollstaendigen Fassungen (JSON). R = Basis wird immer mit erzeugt.",
    )
    parser.add_argument("--korpus", type=Path, help="Korpus fuer die Kostenrechnung (--kandidat)")
    parser.add_argument(
        "--katalog",
        type=Path,
        default=Path(__file__).resolve().parents[2]
        / "Platee.Johann.Application"
        / "Processing"
        / "SummaryModelCatalog.cs",
        help="SummaryModelCatalog.cs -- Quelle des Eingabepreises",
    )
    parser.add_argument(
        "--cache-rabatt", type=float, default=0.9, help="Rabatt auf gecachte Token (0..1)"
    )
    args = parser.parse_args(list(argv) if argv is not None else None)

    base = json.loads(args.base.read_text(encoding="utf-8"))
    if args.kandidat:
        if args.korpus is None:
            parser.error("--kandidat braucht --korpus fuer die Kostenrechnung")
        return _run_candidates(args, base)
    if args.snippets is None:
        parser.error("--snippets ist ohne --kandidat erforderlich")
    snippets = json.loads(args.snippets.read_text(encoding="utf-8"))
    # Felder mit fuehrendem Unterstrich sind Planmetadaten (z. B. die Whole-Plot-Nummer) und
    # gehoeren nicht in den Faktorvektor. Sie wandern unveraendert in das Manifest, weil die
    # Auswertung die Whole-Plot-Zugehoerigkeit als Clusterschluessel braucht.
    design_rows: list[dict] = (
        json.loads(args.design.read_text(encoding="utf-8")) if args.design else [{}]
    )
    cells = [Factors(**{k: v for k, v in row.items() if not k.startswith("_")}) for row in design_rows]

    args.out.mkdir(parents=True, exist_ok=True)
    manifest: list[dict] = []
    for index, (factors, row) in enumerate(zip(cells, design_rows)):
        prompts = build(base, snippets, factors)
        merged = {**base, **prompts}
        # Die Laufnummer gehoert in den Dateinamen: ein Plan darf dieselbe Faktorkombination
        # mehrfach enthalten (Wiederholungen sind erwuenscht), und ohne Nummer wuerde die
        # zweite die erste ueberschreiben.
        target = args.out / f"prompts.{index:03d}.{factors.cell_id}.json"
        target.write_text(
            json.dumps(merged, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
        )
        report = prefix_report(prompts)
        manifest.append(
            {
                "run": index,
                "cell_id": factors.cell_id,
                "factors": asdict(factors),
                "plan": {k: v for k, v in row.items() if k.startswith("_")},
                "prefix": report,
            }
        )
        blocked = [k for k, v in report.items() if k != "systemMessage" and not v["cacheable"]]
        flag = f"  ⚠ unter der Cache-Schwelle: {', '.join(blocked)}" if blocked else ""
        print(f"{target.name}  System {report['systemMessage']['tokens']} Token{flag}")

    (args.out / "manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(f"\n{len(cells)} Varianten in {args.out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
