# Versuch 01 — Widersprüche und Denkprozess in der System-Nachricht

**Angelegt 2026-09-11, Vorhersagen vor Kenntnis der Ergebnisse notiert.**

Betrifft Rang 1 und 4 der Priorisierung aus `befundliste-prompts-2026-09-11.md`.

## Varianten

| Datei | Änderung | System-Nachricht |
|---|---|---|
| `prompts.json` | keine (Referenz) | 1.008 Token |
| `variante_A_widersprueche.json` | S-03 und S-04 aufgelöst | 957 Token (−51) |
| `variante_B_ohne_cot.json` | A **plus** `### CHAIN OF THOUGHTS ###` gestrichen | 676 Token (−332, −33 %) |

### Was genau geändert wurde

**A — zwei Widerspruchspaare, je eine Hälfte entfernt.** In beiden Paaren bleibt die Hälfte
stehen, die der **Treue** dient, weil Treue laut Leitfaden §11 der eigentliche Maßstab für ein
Diktat-Archiv ist:

1. Entfernt: `KLÄRE implizite Aussagen und formuliere sie explizit aus` (Denkprozess, Schritt 4).
   Beibehalten: `NIEMALS EIGENE MEINUNGEN ODER INTERPRETATIONEN HINZUFÜGEN`.
2. Entfernt: `NIEMALS UNKLARE FORMULIERUNGEN STEHEN LASSEN (z. B. „irgendwas wurde besprochen")`.
   Beibehalten: `MARKIERE ggf. Unklarheiten neutral` (Denkprozess, Schritt 6).

**B — zusätzlich der gesamte Denkprozess-Block.** Sieben vorgeschriebene Schritte, rund 300
Token, die bei jedem der sechs Aufrufe je Diktat mitgehen.

## Aufbau

- Abschnitt: `zusammenfassung` (`structuredPrompt`) — der aussagekräftigste, weil er den
  reichsten Vertrag hat
- Korpus: alle 60 Elemente (14 echte Aufnahmen, 16 erfundene, 30 aus dem Archiv)
- 3 Wiederholungen je Diktat und Variante, weil Ausgaben nicht deterministisch sind
  (Levy, 2026; Thelwall, 2024)
- Erzeugung durch `gpt-5.6-luna`, Bewertung durch `gpt-5.6-terra`

## Vorhersagen

**V1 — A schlägt die Referenz bei der Treue, oder ist mindestens gleichauf.**
Begründung: Beide entfernten Zeilen luden zum Ausschmücken ein. Laut OpenAI (2025a) verbraucht
ein Widerspruch Denk-Token; laut Leitfaden §11 senkt mehr internes Denken die Treue.

**V2 — A erzeugt leicht weniger Ausgabe-Token als die Referenz.**
Begründung: Wegfallende Widerspruchsauflösung. Erwartete Größenordnung: wenige Prozent, also
womöglich im Rauschen.

**V3 — B ist bei Treue und Klarheit mindestens so gut wie A.**
Begründung: Meincke et al. (2025), OpenAI (2026a, 2026e) — vorgeschriebene Zwischenschritte
bringen Reasoning-Modellen nichts.

**V4 — B ist deutlich günstiger.** −332 Token Eingabe je Aufruf, also rund −18 % der
Gesamteingabe je Aufruf bei diesem Abschnitt.

### Gegenhypothese, die ernst genommen wird

**G1 — B verliert bei der Vollständigkeit.**
Thelwall (2024) fand, dass kürzere Anweisungen bei komplexen Textbewertungen **schlechtere**
Ergebnisse lieferten. Der Denkprozess-Block enthält mit „FILTERE irrelevante oder redundante
Inhalte" und „PRIORISIERE Informationen nach Relevanz" Anteile, die eher Bewertungsvorschrift
als Gerüst sind. Wenn G1 eintritt, ist das ein belastbarer Beleg **gegen** das pauschale
Ausdünnen — und der Denkprozess-Block müsste teilweise erhalten bleiben.

### Abbruchkriterium

Steigt bei einer Variante die Zahl der objektiven Vertragsverletzungen gegenüber der Referenz,
gilt sie unabhängig von den Bewertungsnoten als **nicht übernehmbar**.

## Ergebnis

*(wird nach dem Lauf ergänzt — Berichte:
`Documents\Johann\prompt-sandbox\eval\rang1_systemnachricht.html` und `.md`)*
