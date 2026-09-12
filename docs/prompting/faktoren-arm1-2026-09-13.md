# Faktorenplan für die Prompt-Überarbeitung (#73)

**Stand 2026-09-13.** Baut auf `befundliste-prompts-2026-09-11.md` (38 Befunde) und dem
Versuchsaufbau aus dem Schwesterprojekt PromptLab (`docs/method/2026-09-12-experimental-design.de.md`,
Arm 1). Dieses Dokument legt fest, **was gemessen wird, was konstant bleibt und was vorher
entschieden werden musste**.

Ziel von Arm 1 ist kein Erkenntnisgewinn, sondern eine Lieferung: die bestmögliche Fassung der
Systemnachricht und aller acht Abschnitts-Prompts, lesend gegengeprüft, übertragbar in die
Team-Datei und die `SummaryPrompts`-Konstanten.

---

## 1. Nebenbedingung, die alles überlagert: die Cache-Schwelle

| Größe | Wert |
|---|---:|
| Systemnachricht heute | 1 008 Token |
| Cache-Schwelle | **1 024 Token** |
| Präfix je Abschnitt heute (System + Vorlage) | 1 057 – 1 449 Token |
| Rabatt auf gecachte Eingabetoken | bis **90 %** |

**Wer die Systemnachricht kürzt, kann die Kosten erhöhen statt senken.** Fällt das Präfix unter
1 024 Token, entfällt die Cache-Fähigkeit, und die verbleibenden Token kosten wieder den vollen
Preis statt ein Zehntel. Variante B aus dem Vorlauf kürzt um 332 Token — ihr gemessener Gewinn
von −24 % Eingabe wurde **ohne** Caching gerechnet.

Entschärft wird es dadurch, dass das Transkript im Präfix mitzählt. Bei den kurzen
Archiv-Diktaten (Median 21 Token) greift das jedoch nicht.

**Verbindlich für den Versuch:**

1. Jede Faktorstufe protokolliert ihre **Präfixlänge je Abschnitt**.
2. Kosten werden **mit** Cache gerechnet, nie in rohen Token.
3. Fällt eine Kombination unter die Schwelle, ist das ein Befund und kein Fehler — er gehört in
   den Bericht.
4. Mögliche Gegenmaßnahme, falls sie gebraucht wird: stabile Inhalte (Korrekturliste,
   Beispielpaar) wandern in den Präfix und heben ihn wieder über die Schwelle, statt die Regel
   zu opfern.

---

## 2. Die sieben Faktoren

### 2.1 Whole-Plot — Systemnachricht

Sie geht in **jeden** Abschnittsaufruf ein. Eine Änderung wirkt auf alle Abschnitte gleichzeitig
und wirft den Präfix-Cache weg. Deshalb ist sie die teure, „schwer zu ändernde" Ebene.

| | Faktor | Stufen | Befund | Warum im Plan |
|---|---|---|---|---|
| **S1** | Denkprozess-Block | drin / raus | S-02 (Rang 4) | Der einzige Faktor mit gemessenem Effekt — aber nur auf **einem** Abschnitt. Ob er für die übrigen sieben gilt, ist die Kernfrage von Arm 1. |
| **S2** | Schreibweise | durchgehende Großschreibung / normale Schreibung | S-01 (Rang 5) | 37 % Token ohne Nutzenbeleg. Trifft unmittelbar die Cache-Schwelle aus §1. |
| **S3** | Regelform | acht Verbote / positive Regeln | S-07 | Klar benanntes Anti-Muster, bislang unbelegt. |
| **S4** | Markdown- und Überschriften-Regel | zentral in der Systemnachricht / verstreut in den Abschnitten | B-03 + B-04 (Rang 7) | Beide Befunde sind dieselbe Bewegung: eine Regel **einmal** zentral statt vier abweichender Varianten. Erklärt die drei Anzeigefehler aus v1.4.0 an der Wurzel. |

### 2.2 Subplot — Abschnitts-Prompts

Billig zu wechseln, deshalb die Ebene, auf der Effekte am präzisesten schätzbar sind.

| | Faktor | Stufen | Befund | Warum im Plan |
|---|---|---|---|---|
| **A1** | Sprachprämisse „auf Deutsch" | drin / raus | B-01 (Rang 2) | Widerspruch zur Systemnachricht in fünf Prompts, seit #58 sachlich falsch. |
| **A2** | Leerfall-Regel | drin / raus | B-05 | Acht von neun Prompts haben keine. Dass das Modell nichts erfindet, ist gemessen — aber nur an einem Diktat. |
| **A3** | Ausgabevertrag | voll (Länge + Format + Leerfall) / minimal | N-02, Z-02, Z-03, L-02 (Rang 8) | Testet die eigentliche Streitfrage: hilft mehr Vertrag, oder schadet er? Thelwall (2024) und die „kürzer ist besser"-These stehen hier gegeneinander. |

---

## 3. Was konstant bleibt — und warum das ein Gewinn ist

Jeder Faktor kostet Plan-Kapazität. Was gemessen *ist* oder gut belegt ist, wird festgesetzt
statt mitgeschleppt.

| Festsetzung | Begründung |
|---|---|
| **Widersprüche S-03 und S-04: immer aufgelöst** | Der Vorlauf hat gemessen, dass ihre Beseitigung nichts bringt — p = 0,754, im Median 72 gegen 73 Denk-Token. Sie kosten also nichts, machen den Prompt aber für Menschen unwartbar. Als Faktor würden sie einen Slot für eine beantwortete Frage verbrennen. |
| **Beispielpaar bleibt** | S-08: ausdrücklich als wirksames Formmuster bewertet. |
| **Relevanzkriterien im `structuredPrompt` bleiben unangetastet** | T-02: ihre Länge ist Bewertungsvorschrift, nicht Gerüst. Thelwall (2024) ist der Gegenbeleg zur Kürzungsthese. |
| **`reasoning_effort` wird nicht gesetzt** | Zwei Variablen gleichzeitig zu bewegen macht die Bewertung unmöglich, und es entwertet die gemessenen Katalogkonstanten aus #71 (`OutputBase`, `OutputSlope`). |
| **Modellstufe konstant** | Arm 1 misst Prompts, nicht Modelle. Die Modellwahl ist über #71 bereits eine Nutzereinstellung. |

---

## 4. Produktentscheidungen vom 13.09.2026

Die drei offenen Fragen aus §13 der Befundliste sind beantwortet. Sie legen den Vertrag fest,
**gegen den** gemessen wird — ohne sie wäre die Bewertung bodenlos.

### 4.1 „Ausführlich" = vollständige Aufbereitung

> Alles Gesagte wird in lesbares Fließdeutsch überführt. Nichts wird weggelassen; geglättet und
> geordnet, aber nicht verdichtet.

**Folge — P-01 ist damit auflösbar** (Rang 3 der Priorisierung). Die Systemnachricht verlangt
bislang pauschal Verdichtung und widerspricht damit diesem Abschnitt. Die Verdichtungsregel
bekommt einen **Geltungsbereich**: sie gilt für Kurzfassung und Zusammenfassung, nicht für die
vollständige Aufbereitung. Das ist eine Festsetzung, kein Faktor.

⚠ **Folge für die Messung.** „Vollständigkeit" wird für diesen Abschnitt nahezu tautologisch und
„Treue" zum dominanten Kriterium. Die Bewertungsrubrik muss das abbilden, sonst misst sie hier
nichts. Für diesen Abschnitt gilt zusätzlich das **Verhältnis von Ausgabe- zu Eingabelänge** als
objektive Kennzahl.

### 4.2 Die E-Mail bekommt Markdown wie alle anderen Abschnitte

Keine Ausnahme von der zentralen Markdown-Regel. Das vereinfacht S4: eine Regel, neun Prompts,
kein Sonderfall.

> ⚠ **Vorbedingung, die vor der Übernahme geprüft sein muss.** Diese Entscheidung ist nur
> sicher, wenn die Outlook-Anbindung aus **#57** Markdown tatsächlich nach HTML wandelt. Tut sie
> es nicht, landen `**Sternchen**` in einer Kundenmail — laut Befundliste (M-06) der Befund, der
> am ehesten beim Kunden sichtbar wird.
>
> **Regel:** Die zentrale Markdown-Regel wird erst dann in die Team-Datei übertragen, wenn die
> Wandlung in #57 nachweislich funktioniert. Im Versuch läuft sie mit; in der Auslieferung
> wartet sie auf #57.

### 4.3 „Analog" bleibt, läuft aber nur auf Abruf

Keine Prompt-Änderung, sondern eine Änderung an `SectionModeDefaults`: `analog` wechselt von
`Auto` auf `OnDemand`.

**Das löst B-06** — im Test lieferten Gesprächsnotiz, Analog und Zusammenfassung dreimal
denselben Inhalt, und der Nutzer bezahlte drei Aufrufe, um dreimal dasselbe zu lesen. Ohne den
automatischen Lauf verschwindet die Dreifachausgabe und ein Aufruf je Diktat.

Im Versuch bleibt `analog` als **sekundärer** Abschnitt mit, weil der Prompt weiter existiert und
auf Abruf funktionieren muss.

---

## 5. Zwei Besonderheiten, die ins Auswertungsmodell gehören

### 5.1 Die E-Mail ist verkettet

`emailPrompt` erhält **nicht** das Transkript, sondern die **Prosa-Zusammenfassung**. Jede
Änderung an S1–S4 wirkt damit zweifach auf die E-Mail: einmal direkt über die Systemnachricht,
einmal indirekt über die veränderte Prosa.

Ohne Gegenmaßnahme wäre ein Prosa-Effekt nicht von einem E-Mail-Effekt zu unterscheiden.
**Maßnahme:** Die E-Mail läuft zusätzlich einmal gegen eine **eingefrorene** Prosa-Fassung. Die
Differenz zwischen beiden Läufen ist der reine Weitergabe-Effekt.

### 5.2 Der Abschnitt ist eine Messwiederholung

Ein Diktat erzeugt bis zu acht Abschnitte, die denselben Inhalt teilen. `item_id` ist der
Clusterschlüssel, `section_id` die Ebene darunter.

Das ist die gute Nachricht: ein Befund, der „für alle Abschnitte gilt", braucht **deutlich
weniger Diktate** als acht getrennte Versuche. Und es ist zugleich der Grund, warum
ungeclusterte Fehlerbalken hier besonders irreführend wären — die Standardfehler wären bis zu
dreimal zu klein.

**Primärer Endpunkt:** Treue, über die drei automatisch laufenden Abschnitte mit dem reichsten
Vertrag. Alle übrigen Abschnitte laufen als sekundäre Endpunkte über Gatekeeping mit — sie
werden erst ausgewertet, wenn der primäre Endpunkt hält.

---

## 6. Was als Nächstes ansteht

1. Die sieben Faktoren in konkrete Prompt-Varianten übersetzen, je Stufe eine Datei in der
   Sandbox (`Documents\Johann\prompt-sandbox\`).
2. D-optimalen Split-Plan für die Faktorstruktur aus §2 erzeugen.
3. Pilotlauf nach §7 des Versuchsaufbaus — fünf Kennzahlen, darunter die Varianzkomponenten.
4. Vorab-Registrierung schreiben, **dann** messen.
5. Ergebnisse lesend gegenprüfen, erst danach in Team-Datei **und** `SummaryPrompts` übertragen
   — beide, sonst greift die Änderung nicht (`TeamPromptDriftTests` bewacht das).
