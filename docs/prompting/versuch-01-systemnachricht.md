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

**V3 — B ist bei Treue und Klarheit mindestens so gut wie A.**
Begründung: Meincke et al. (2025), OpenAI (2026a, 2026e).

**V4 — B ist deutlich günstiger.**

### Gegenhypothese, die ernst genommen wurde

**G1 — B verliert bei der Vollständigkeit.** Thelwall (2024) fand, dass kürzere Anweisungen bei
komplexen Textbewertungen **schlechtere** Ergebnisse lieferten.

### Abbruchkriterium

Mehr objektive Vertragsverletzungen als die Referenz ⇒ Variante nicht übernehmbar.

---

# Ergebnis

540 Erzeugungen, 180 Bewertungen, Kosten rund 1,30 $. Berichte:
`prompt-sandbox/eval/rang1_systemnachricht.html` und `.md`.

## Zuerst: ein Fehler im Messinstrument

Der erste Auswertungsdurchlauf meldete **6 / 3 / 2** objektive Vertragsverletzungen und legte
damit nahe, B sei formal sauberer. **Das war ein Artefakt.**

Alle elf Treffer entfielen auf die Prüfung „Ausgabe wirkt nicht deutsch", und alle elf waren
Fehlalarme — kurze, völlig korrekte Sätze wie:

> „Es wurden keine inhaltlich relevanten Aussagen, Entscheidungen oder To-dos übermittelt."

Sie enthielten schlicht keines der 15 Stoppwörter, auf die geprüft wurde. Der Detektor prüfte
auf **Vorhandensein von Deutsch** statt auf **Fremdsprachigkeit** — bei kurzen Ausgaben
unbrauchbar.

Korrigiert (Prüfung auf fremdsprachige Marker im Übergewicht) und die gespeicherten Ausgaben
ohne neue API-Aufrufe neu bewertet: **0 Fehler bei allen drei Varianten.** Bei den objektiven
Prüfungen gibt es also **keinen Unterschied**.

## Bewertungen, paarweiser Vorzeichentest je Diktat

| Vergleich | Merkmal | besser | schlechter | gleich | p |
|---|---|---:|---:|---:|---:|
| A gegen Referenz | Treue | 4 | 6 | 50 | 0,754 |
| **B gegen Referenz** | **Treue** | **12** | **3** | 45 | **0,035** |
| **B gegen A** | **Treue** | **13** | **2** | 45 | **0,007** |
| A gegen Referenz | Vollständigkeit | 3 | 6 | 51 | 0,508 |
| B gegen Referenz | Vollständigkeit | 4 | 4 | 52 | 1,000 |
| B gegen A | Vollständigkeit | 5 | 2 | 53 | 0,453 |
| alle | Klarheit | ≤2 | ≤3 | ≥55 | 1,000 |

Verteilung der Treue-Noten (Anzahl Diktate):

| Variante | Note 3 | Note 4 | Note 5 |
|---|---:|---:|---:|
| Referenz | 1 | 16 | 43 |
| A | 1 | 18 | 41 |
| **B** | **0** | **8** | **52** |

B halbiert die Zahl der Diktate mit kleinen Treue-Abzügen.

## Token je Aufruf (Median)

| Variante | Eingabe | Ausgabe | davon Denken |
|---|---:|---:|---:|
| Referenz | 1.386 | 166 | 73 |
| A | 1.335 | 162 | 72 |
| **B** | **1.054 (−24 %)** | **152 (−8 %)** | 66 |

## Bewertung der Vorhersagen

| | Vorhersage | Ergebnis |
|---|---|---|
| **V1** | A verbessert die Treue | **nicht bestätigt** — 4 besser gegen 6 schlechter, p = 0,754 |
| **V2** | A erzeugt weniger Ausgabe-Token | **nicht bestätigt** — 4 Token Median, im Rauschen |
| **V3** | B mindestens so gut wie A | **bestätigt und übertroffen** |
| **V4** | B deutlich günstiger | **bestätigt** — −24 % Eingabe |
| **G1** | B verliert an Vollständigkeit | **nicht eingetreten** — 4 besser gegen 4 schlechter |

## Was daraus wirklich folgt

**Die beiden Widersprüche zu beseitigen hat für sich genommen nichts gebracht.** Das ist der
überraschende Teil. OpenAI (2025a) schreibt, Widersprüche kosteten Denk-Token — hier waren es
im Median 72 gegen 73, also nichts. Die Widersprüche sind real und gehören trotzdem beseitigt,
weil sie einen Prompt für **Menschen** unwartbar machen; messbar besser wird das Ergebnis davon
aber nicht.

**Der gesamte Gewinn stammt aus dem Streichen des Denkprozess-Blocks.** B ist gegenüber A bei
der Treue signifikant besser (p = 0,007), während A sich von der Referenz nicht unterscheidet.
Das passt zu Leitfaden §11: weniger vorgeschriebenes Denken, weniger kreatives Lückenfüllen.

**Thelwalls Gegenbefund trat nicht ein.** Der Block enthielt zwar Anteile, die wie
Bewertungsvorschrift aussehen („FILTERE irrelevante Inhalte", „PRIORISIERE nach Relevanz"),
aber deren Wegfall kostete keine Vollständigkeit. Erklärung: `structuredPrompt` trägt diese
Kriterien bereits selbst und ausführlicher. Die Substanz war **doppelt** vorhanden — im System
als Prozessschritt, im Abschnitt als Kriterium. Gestrichen wurde die schwächere Kopie.

Das schärft die Regel aus dem Leitfaden: Nicht „kürzen ist gut", sondern **Dubletten über
Prompt-Grenzen hinweg sind teuer**. Wer nur den System-Prompt liest, sieht sie nicht.

## Einschränkungen

1. **Mehrfachvergleich.** Neun Tests gerechnet. Bei Bonferroni-Korrektur läge die Schwelle bei
   0,0056; der beste Wert (p = 0,007) liegt knapp darüber. **Streng genommen ist damit kein
   Einzelergebnis gesichert.** Für die Entscheidung spricht, dass die Richtung in beiden
   B-Vergleichen gleich ist, kein Merkmal schlechter wird und die Kostenersparnis unabhängig
   von der Signifikanz real ist.
2. **Der Richter ist selbst ein Sprachmodell.** Terra bewertet Lunas Ausgabe. Ein anderes
   Modell zu nehmen mildert die Zirkularität, hebt sie nicht auf.
3. **Ein Abschnitt, eine Modellstufe.** Gemessen wurde `zusammenfassung` auf Luna. Ob der
   Befund für `ausfuehrlich` oder `aufgaben` ebenso gilt, ist offen.
4. **Das Archiv dominiert den Korpus.** 30 der 60 Elemente sind Archiv-Diktate mit Median
   21 Token. Bei so kurzen Eingaben unterscheiden sich Varianten naturgemäß wenig — der Effekt
   bei den 14 echten Aufnahmen dürfte deutlicher sein, als der Gesamtwert zeigt.

## Empfehlung

**Variante B übernehmen** — nicht wegen der Signifikanz, die den strengen Test nicht besteht,
sondern weil sie in jeder gemessenen Hinsicht mindestens gleichauf und in zwei Hinsichten
besser ist: konsistent bessere Treue und 24 % weniger Eingabe-Token, bei unveränderter
Vollständigkeit und Klarheit und null Vertragsverletzungen.

**Vor der Übernahme lesend gegenprüfen** (`rang1_systemnachricht.html`), weil die
Qualitätsentscheidung laut Leitfaden §10 beim Menschen liegt und nicht beim Richter-Modell.
