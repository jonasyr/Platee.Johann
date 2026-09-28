# Soll-Profil je Vorlage (#73, Arm 1)

**Stand 2026-09-15.** Schritt 1a aus `stand-73-2026-09-15.md` §6. Legt für jede der acht Vorlagen
fest, **wogegen** gemessen wird: Zweck, Leser, Format, Länge, Leerfall. Jede Formatprüfung
(`format_checks.py`) und jede Lesefrage leitet sich aus dieser Datei ab. Kein Prompt-Wortlaut —
der bleibt in der Sandbox.

Quellen: Befundliste 2026-09-11, Faktorenplan §4 (Produktentscheidungen), Code-Stand des Branches.

---

## 0. Festsetzungen für K1 (User, 15.09.2026)

| Frage | Entscheidung |
|---|---|
| Denkprozess-Block (S-02) | **raus**; die Unklarheits-Regel wird vorher herausgelöst und bleibt |
| Aufgabenliste | Aufzählung mit `- `, **nicht** `- [ ]` — Detailansicht und PDF kennen keine Checkbox-Syntax |
| Cache-Rechnung | beide Szenarien (mit/ohne Treffer), Preis gecachter Token als Parameter; läuft automatisch |
| Markdown-Zentralregel (S4) | nicht in K1, erst nach #57 |
| E-Mail | kein Markdown bis #57 |
| E-Mail: Register | **immer siezen**, auch wenn das Diktat den Empfänger duzt; die Anrede darf den Vornamen tragen, Verben und Pronomen stehen durchgehend in der Sie-Form |
| E-Mail: Unklarheiten | als unklar markierte Stellen der Prosa **entfallen** in der Mail; sie stehen nur im Eintrag. Die Mail erwähnt keine Unklarheit |
| Stundenzettel | bleibt wie K1 (keine Nachschärfung trotz „Dauer nicht genannt" bei geplanten Tätigkeiten) |

## 1. Wie die Vorlagen im Code verdrahtet sind

Geprüft im Quelltext (`SummaryGenerator`, `BuiltInSections`, `SectionModeDefaults.Recommended`,
`WordLimitCalculator`):

| Vorlage | Abschnitt in der App | Anzeigename | Modus (Empfehlung) | Eingabe | Längen-Platzhalter |
|---|---|---|---|---|---|
| `abstractPrompt` | Intrinsic (unter dem Titel) | — | immer | Transkript | `{word_limit}` = 20 / 50 / 150 Wörter bei < 300 / < 1000 / ≥ 1000 Wörtern Transkript |
| `structuredPrompt` | `builtin.longSummary` | Zusammenfassung | Auto | Transkript | wird mit 50 / 150 / 300 befüllt, **Vorlage enthält den Platzhalter aber nicht** |
| `prosePrompt` | `builtin.proseSummary` | Ausführliche Zusammenfassung | Auto | Transkript | — |
| `aufgabePrompt` | `builtin.taskList` | Aufgaben | Auto | Transkript | — |
| `gespraechsnotizPrompt` | `builtin.conversationNote` | Gesprächsnotiz | Auto | Transkript | — |
| `emailPrompt` | `builtin.emailText` | E-Mail | Abruf | **Prosa-Ausgabe** | — |
| `stundenzettelPrompt` | `builtin.stundenzettel` | Stundenzettel | Abruf | Transkript | — |
| `analogPrompt` | `builtin.analog` | Analog | Abruf | Transkript | — |

**Drei Befunde aus dem Code, die die Befundliste nicht kennt:**

- **C-01 · `{word_limit}` für die Zusammenfassung ist toter Code.** `SummaryGenerator` berechnet
  50/150/300 und ersetzt den Platzhalter, `structuredPrompt` trägt ihn nicht. Die Zusammenfassung
  hat damit keine Längengrenze. Nach T-02 ist das so gewollt (Informationsdichte vor Kürze) — K1
  lässt es dabei; nur festhalten, damit niemand einen Längenfehler sucht.
- **C-02 · „Analog auf Abruf" ist in `SectionModeDefaults.Recommended` bereits umgesetzt.**
  `stand-73` §2 nennt es offen. Es fehlt nur für Nutzer mit `AllAuto` bzw. mit vor der Migration
  gespeicherten `SectionModes` — Schritt 5 des Plans schrumpft damit auf eine Prüfung plus Test,
  ob der Wert greift.
- **C-03 · Die Korrekturliste hängt einen Block in Großschreibung an die Systemnachricht.**
  `BuildSystemPrompt` ergänzt `### KORREKTURLISTE ###` samt Einleitung aus dem Code, nicht aus der
  Team-Datei. Das ist stabiler Präfix-Inhalt (gut für den Cache, Faktorenplan §1 Nr. 4) und
  bleibt in K1 unberührt; die Schreibweise dort ist eine Code-Änderung und nicht Teil von #73.

Außerdem, als eigenes Issue und nicht für #73: **`PdfRenderer.RenderMarkdown` erkennt
Aufzählungen nur ohne Einrückung** — verschachtelte Unterpunkte erscheinen im PDF als Absatz mit
Bindestrich. Die Detailansicht ist seit v1.4.0 korrekt. → **#83** (v1.5.0).

**C-04 · Der Eintragstyp steuert die Abschnitte nicht mehr.** Ursprünglich (README, Handbuch) hing
ein Abschnitt am Schlüsselwort am Diktatanfang: „Gesprächsnotiz …" ergab den Typ Gesprächsnotiz
und damit das Gesprächsprotokoll, „Analog …" einen Freitext-Abschnitt statt der Zusammenfassung.
Seit dem Kategorien-Umbau (v1.3.3/v1.4.0) entscheidet allein `SectionModes`; der Typ färbt nur noch
Kopf und Mail-Betreff. Die Gesprächsnotiz läuft deshalb **auf jedem Diktat** — auch auf
Materialbestellungen und Zeiterfassungen.

## 1a. Längen: an Handfassungen geprüft (15.09.2026)

Zehn Abschnitte von Hand nach diesem Profil geschrieben, über sechs Diktate aus `corpus.v2`
(27–432 Wörter; Korpus: Median 193, Maximum 474). Fassungen in der Sandbox,
`eval/handfassungen-73.json`.

| Abschnitt | Transkript → Ausgabe (Wörter) | Verhältnis |
|---|---|---|
| Gesprächsnotiz | 79 → 57 · 194 → 92 · 432 → 189 | 0,72 · 0,47 · 0,44 |
| E-Mail (Textkörper) | 27 → 34 · 79 → 60 · 333 → 183 | 1,44 · 0,85 · 0,58 |
| Stundenzettel | 27 → Leerfall · 182 → 33 | — |
| Analog | 79 → 52 · 432 → 105 | 0,66 · 0,24 |

**Folgerung: nur Obergrenzen, keine Mindestlängen.** Eine Untergrenze von 80 (Gesprächsnotiz)
bzw. 120 Wörtern (E-Mail) hätte bei jedem kurzen Diktat Füllstoff erzwungen. Das Archiv zeigt, wohin
das ohne Leerfall-Regel führt: Aus Testdiktaten mit 3–9 Wörtern entstanden Gesprächsnotizen mit
119–313 Wörtern, mit eigener Überschrift, erfundenem Teilnehmer, angehängter Folge-Mail und
Rückfrage. ⚠ **Diese Archiv-Ausgaben stammen fast alle aus Versionen vor Luna** (User,
15.09.2026; der Eintrag trägt kein Modellfeld). Sie belegen das Risiko eines schwächeren Modells,
**nicht** das Verhalten von Luna — dafür gilt weiter der Einzelbefund B-05 („erfindet nichts") und
die Messung in Schritt 2.

### Lange Diktate: der Korpus ist dünner, als er aussieht

Gezielt gesucht (15.09.2026) in `Documents\Johann` (Archiv, Eingang, Sandbox), `Documents\Johann
Test Aufnahmen`, `Documents\Johann_Backup_v1.4.0_…`, Downloads/Desktop/Musik/Videos/OneDrive und
`Z:\12_Tools\Peano\Johann` (nur Programmdateien). **Es gibt kein längeres Transkript als die in
`corpus.v2`.** Alle 16 Diktate ab 230 Wörtern sind schon drin; das längste hat 474 Wörter
(≈ 3½ Minuten). Die großen Dateien im Archiv (11 MB, 3,8 MB) sind WAV-Rohaufnahmen von 10–31 s
Testsprache.

⚠ **Dublette im Korpus:** `aufnahme:2026_02_05_12_37_24.mp3`, `archiv:260317_001_f9b84030` und
`archiv:260910_003_c34b8a3e` sind **dieselbe Aufnahme**, dreimal transkribiert (gleiche MP3,
4.967 KB; im Backup liegt eine vierte, `260903_001`). `curate_corpus.py` hat sie nicht erkannt, weil
die Transkripte nicht wortgleich sind. Der Eimer „lang" hat damit **acht verschiedene** Diktate
statt zehn, und eines davon zählt dreifach.

Folge für #73: Die Obergrenzen (≤ 250) sind an Diktaten bis ~450 Wörtern geprüft, **nicht** an
den 5–6-Minuten-Aufnahmen, die #71 als realistisch annimmt (~700–900 Wörter). Echte lange Diktate
des Chefs kommen frühestens nach seinem Urlaub.

Festgesetzt: **Gesprächsnotiz ≤ 250 · E-Mail ≤ 250 (Textkörper) · Analog ≤ 120 Wörter.** Beim
dichtesten Diktat des Korpus lag die vollständige Gesprächsnotiz bei 189 Wörtern; 250 lässt Luft
für längere Aufnahmen, ohne kurze aufzublähen.

**Geändert 21.09.2026 nach dem Lesevergleich: Gesprächsnotiz und E-Mail ≤ 400 Wörter.** Auf den
zwei längsten gelesenen Diktaten (426 und 452 Wörter) lag K1 mit 239 bzw. 224 Wörtern an der
Grenze, und der Leser vermisste genau die abgeschnittenen Inhalte („viel mehr wichtige Infos“,
„genauere Infos“). „Kürzer, wenn das Diktat wenig enthält“ bleibt; eine Untergrenze gibt es weiter
nicht.

**Stundenzettel:** Diktate nennen nicht jede Dauer („Fotos sortiert, Bericht angefangen" ohne
Zeit) und oft nur ungefähre („gut drei Stunden"). Die Zeile trägt deshalb „ca." bzw. „Dauer nicht
genannt" statt einer erfundenen Zahl; Tätigkeiten, die das Diktat ausdrücklich nicht abrechnet,
entfallen.

---

## 2. Übersicht

| Vorlage | Zweck | Leser | Format | Länge | Leerfall |
|---|---|---|---|---|---|
| Abstract | auf einen Blick erkennen, worum es geht | Nutzer in der Eintragsliste, PDF-Kopf | Fließtext, 1 Absatz, **kein** Markdown, keine Überschrift | ≤ `{word_limit}` Wörter | kurzes Diktat → ein Satz, nichts auffüllen |
| Zusammenfassung | Inhalt verdichtet und geordnet | Nutzer, Kollegen, später nachlesend | feste `###`-Überschriften aus dem Schema, Aufzählungen, nur relevante Abschnitte | keine Zahl (T-02, C-01) | leere Abschnitte **weglassen**, nie leer ausgeben |
| Ausführlich | vollständige Aufbereitung: alles Gesagte in Schriftdeutsch | Nutzer als Protokoll des Gesagten | ganze Sätze, Absätze; Zwischenüberschriften nur bei echter Gliederung | keine Obergrenze; Verhältnis Ausgabe/Eingabe als Kennzahl | vorhandenen Text geglättet wiedergeben, nichts ausschmücken |
| Aufgaben | abarbeitbare To-dos | Nutzer | 1 Absatz (2–4 Sätze), Leerzeile, Aufzählung `- `; keine Überschriften, keine Nummerierung | ≤ 8 Aufgaben, ≤ 20 Wörter je Zeile | genau „Keine Aufgaben genannt." |
| Gesprächsnotiz | kundentaugliche Notiz eines Gesprächs | Kunde / Gesprächspartner | Aufzählung, ein Punkt je Sachverhalt; Gliederung Teilnehmer · Themen · Beschlüsse · weiteres Vorgehen (nur belegte Teile) | ≤ 400 Wörter, keine Untergrenze | kein Gespräch → genau ein Satz, der das feststellt |
| E-Mail | Inhalt an einen Empfänger senden | Empfänger (Kunde/Kollege) | `Betreff: …`-Zeile, Anrede, Fließtext; **kein Markdown** bis #57; kein Gruß am Ende (Signatur kommt aus Outlook); siezen; Ich-Perspektive | ≤ 400 Wörter Textkörper, keine Untergrenze | nicht nötig: leere Prosa erzeugt keinen Aufruf |
| Stundenzettel | Zeiten übertragbar machen | Nutzer beim Eintragen | eine Zeile je Tätigkeit, `Dauer – Tätigkeit`; Dauer als Zahl, „ca." bei ungefähren Angaben, „Dauer nicht genannt" statt Schätzung | so viele Zeilen wie Tätigkeiten | genau „Keine Zeiten genannt." |
| Analog | Freitext-Abschnitt (Zweck offen, §3.8) | | Fließtext ohne Aufzählung | ≤ 120 Wörter | ein Satz |

---

## 3. Je Vorlage

### 3.1 Abstract

Die eine Zeile, an der man einen Eintrag wiedererkennt. Erscheint unter dem Titel und im PDF-Kopf,
darum ohne Markdown und ohne Überschrift (A-02) — Sternchen oder `###` wären dort Rohtext. K1:
Sprachprämisse raus (B-01), Überschrift positiv ausschließen, Leerfall ergänzen (A-03),
`{word_limit}` bleibt zwingend erhalten (A-04).
**Prüfungen:** Wortzahl ≤ Limit · keine Markdown-Zeichen · keine Überschrift · ein Absatz · deutsch.

### 3.2 Zusammenfassung (`structuredPrompt`)

Der beste der neun Prompts (T-02). Die Relevanzkriterien sind Bewertungsvorschrift und bleiben
wörtlich. K1 räumt nur auf: Sprachprämisse raus, Dublette T-03 zu einer Regel, Satzzeichen und
Großschreibung T-04. Der Leerfall ist schon da („nur relevante Abschnitte") und wird ausdrücklich.
**Prüfungen:** nur Überschriften aus dem Schema · keine leere Überschrift · keine Überschrift
„Zusammenfassung" · Markdown wohlgeformt (Aufzählungen, Einrückung konsistent) · deutsch.

### 3.3 Ausführliche Zusammenfassung (`prosePrompt`)

Produktentscheidung 4.1: vollständige Aufbereitung, geglättet und geordnet, nicht verdichtet. Die
Systemnachricht bekommt dafür die Scope-Regel (P-01). K1: Sprachfehler P-02, Überschriften-Regel
positiv (P-03). Wirkt über die E-Mail doppelt (M-01).
**Prüfungen:** Ausgabe/Eingabe-Wortverhältnis (Kennzahl, kein harter Grenzwert) · keine
Abschnittsüberschrift am Anfang · keine Aufzählung als Hauptform · deutsch.

### 3.4 Aufgaben (`aufgabePrompt`)

Strukturell die Vorlage für alle anderen (G-01). K1 entfernt nur Dubletten (G-02) und zieht den
Rest des Verbotsblocks positiv in den Ausgabevertrag (G-03). Beispiel „PDF je Sprachnachricht
erzeugen" bleibt.
**Prüfungen:** Absatz vor der Liste · jede Aufgabe beginnt mit `- ` · ≤ 8 Aufgaben · ≤ 20 Wörter ·
keine Überschrift, keine „Aufgabe 1:"-Nummerierung · Leerfall exakt der vereinbarte Satz ·
deutsch.

### 3.5 Gesprächsnotiz

Heute ohne Vertrag (N-02) und automatisch auf jedem Diktat — auch auf solchen, die kein Gespräch
sind (N-03, B-06: dreimal derselbe Inhalt). Der Leerfall-Satz ist deshalb hier der wirksamste
Eingriff: ein Nicht-Gespräch soll **nicht** zu einer dritten Zusammenfassung werden.
**Prüfungen:** Aufzählung · keine Abschnittsüberschrift „Gesprächsnotiz" · Wortzahl ≤ 400 ·
keine Folge-Mail, keine Rückfrage, keine Platzhalter („TBD", „[Name]") · bei Diktaten ohne Gespräch: Leerfall-Satz statt Inhalt · keine erfundenen
Teilnehmer (Namen in der Ausgabe müssen im Transkript vorkommen) · deutsch.

### 3.6 E-Mail

Einzige Vorlage auf der Prosa-Ausgabe. K1 löst M-02 (Länge als Zahl), M-03 („Anrede" vs.
„Gruß"), M-04 (Siezen nach oben zum Register) und M-05 (nicht prüfbare Adjektive raus). Kein
Markdown, bis #57 HTML wandelt (4.2).

Nach dem Messlauf geschärft (User, 15.09.2026): Die Mail siezt **immer** — auch wenn das Diktat
duzt. Ursache des Duzens war das Anrede-Beispiel „Lieber/Liebe …", das den Du-Ton mitzieht; R
mischt dort ebenfalls („bitte bearbeite …" neben „prüfen Sie …"). Stellen, die die Prosa als unklar
markiert, übernimmt die Mail nicht, auch nicht umschrieben („ist derzeit noch unklar") — eine
Kundenmail ist kein Ort für Transkriptionslücken. Das ist eine abschnittsbezogene Ausnahme von der
Unklarheits-Regel der Systemnachricht, mit Begründung im Prompt, kein Widerspruch.
**Prüfungen:** erste Zeile `Betreff: ` · Anrede in der ersten Textzeile · keine Grußformel am
Ende · keine Markdown-Zeichen (`**`, `#`, `- ` am Zeilenanfang) · Sie-Form (kein „du/dich/dir/
euch") · keine gemischte Anrede (Sie-Form **und** Du-Imperativ wie „bitte prüfe") · keine
Unklarheitsmarke („[inhaltlich unklar]", „unverständlich") · ≤ 400 Wörter · deutsch.

### 3.7 Stundenzettel

Tabellarische Daten ohne Form (Z-02) und ohne Leerfall (Z-03). K1 legt eine Zeile je Tätigkeit
fest, weil das in jede Zeiterfassung übertragbar ist, ohne eine Markdown-Tabelle zu verlangen, die
das PDF nicht darstellen kann.
**Prüfungen:** jede Zeile enthält eine Dauer als Zahl oder „Dauer nicht genannt" · keine Überschrift · Leerfall exakt
der vereinbarte Satz, wenn das Transkript keine Zeiten nennt · keine erfundenen Zeiten (jede Zahl
steht im Transkript) · deutsch.

### 3.8 Analog — Zweck offen

L-02/L-03: „treffend zusammenfassen" ist vom Abstract und der Zusammenfassung nicht zu
unterscheiden. Produktentscheidung 4.3 sagt nur, **dass** der Abschnitt bleibt (auf Abruf), nicht
**wofür**. Ohne Zweck kann K1 hier nichts besser machen als R, und der Lesevergleich hat keine
Frage. Handbuch und README nennen nur „Freitext-Abschnitt statt Standard-Zusammenfassung" für Diktate,
die mit „Analog" beginnen (C-04); den Zweck kennt auch der User nicht (15.09.2026).
**Festsetzung: K1 räumt Analog nur auf** — Sprachprämisse, Überschrift, Leerfall, Fließtext
≤ 120 Wörter. Keine inhaltliche Neufassung, im Lesevergleich nur die Frage „schlechter?".

---

## 4. Querschnitt (Systemnachricht, gilt für alle)

Normale Schreibung (S-01) · Denkprozess-Block raus (S-02) · S-03/S-04 aufgelöst ·
Unklarheits-Regel als eine Entscheidungsregel · Sprache in einem Satz (B-02) · keine absolute
„nie kürzen"-Regel (S-05) · keine Strukturvorgabe im System (S-06) · positive Regeln (S-07) ·
Beispielpaar bleibt (S-08) · Verdichtung mit Geltungsbereich (P-01).

**Prüfungen für alle Abschnitte:** Ausgabe deutsch · erste Antwort nicht leer und ohne Vorrede
(„Hier ist …") · keine Überschrift, die den Abschnittsnamen wiederholt · keine Rückfrage am Ende.
