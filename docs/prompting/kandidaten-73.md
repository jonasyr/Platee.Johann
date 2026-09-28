# Kandidaten #73 — Schritt 1 (15.09.2026)

Schritt 1 aus `stand-73-2026-09-15.md` §6. Grundlage: `soll-profil-73.md`. Der Wortlaut liegt nur
in der Sandbox (`prompt-sandbox\kandidat.K1.de.json`, erzeugt von `kandidat_k1_schreiben.py`;
Ausgabe `prompt-sandbox\kandidaten-73\`).

## Korpus v3

`build_corpus_v3.py` (11 Tests) erzeugt `corpus.v3.json` aus `corpus.v2.json`, **46 Diktate**:

- **−3 Dubletten:** `archiv:260317_001` und `archiv:260910_003` sind dieselbe Aufnahme wie
  `aufnahme:2026_02_05_12_37_24.mp3`; `archiv:260317_009` ist dasselbe Diktat, erneut
  aufgenommen, wie `archiv:260317_005` (Jaccard 0,62, von `near_duplicates` gefunden).
  Alle übrigen Paare liegen unter 0,35.
- **+3 lange synthetische Diktate** (`dictations_long.py`): 806 / 1.053 / 1.285 Wörter —
  Wochenzeiten, Übergabe mit mehr als acht Aufgaben, Baubesprechung mit sechs Beteiligten.
  Sprechprofil gemessen und im Modul dokumentiert.
- Eimer: winzig 9 · kurz 6 · mittel 20 · lang 11.

Nebenbefund: Diktiert wird mit 60–90 Wörtern je Minute (Testaufnahmen 05.02.2026, gemessene
Dauer). Das längste echte Diktat (474 Wörter) dauert 6:02 Minuten.

## Was K1 umsetzt

Systemnachricht: normale Schreibung, Denkprozess-Block raus, positive Regeln, Sprache in einem
Satz, Unklarheits-Regel als Entscheidungsregel, Verdichtung mit Geltungsbereich, keine
Strukturvorgabe, Beispielpaar wörtlich erhalten. Abschnitte je Vorlage nach `soll-profil-73.md`
§3. Formulierungen, auf die `SummaryPromptsTests` eine gemachte Lehre festnageln (z. B. „keine
Überschrift für den Abschnitt", „nach dem Bindestrich", „höchstens acht Aufgaben"), sind
wörtlich übernommen.

`variants.py --kandidat` prüft jede Fassung beim Einsetzen: Platzhalter vollständig, keine
Sprachprämisse, kein Denkprozess-Block, keine Versalien, Beispielsätze unverändert
(`test_variants.py`, 18 Tests).

## Token und Eingabekosten

Luna, 0,20 $ je Mio. Eingabetoken (aus `SummaryModelCatalog`), gecachte Token mit 90 % Rabatt
(Parameter, nicht im Katalog). Mittel über `corpus.v3`. **Nur Eingabe** — die Ausgabe misst
Schritt 2.

| | Systemnachricht | stabiler Präfix je Aufruf | cachefähig | Eingabe je Diktat, 6 Aufrufe, ohne Treffer | mit Treffer |
|---|---:|---:|---:|---:|---:|
| R | 1.008 Token | 1.044–1.449 | 9 / 9 | 0,186 ¢ | 0,064 ¢ |
| K1 | 408 Token | 453–814 | 0 / 9 | 0,120 ¢ | 0,120 ¢ |

**Lesart:**

- K1 verliert die Cache-Fähigkeit vollständig — und ist trotzdem billiger, solange weniger als
  **54 %** der Aufrufe einen Treffer landen. Der Cache lebt Minuten; ein Treffer braucht, dass
  *derselbe Abschnitt* kurz vorher lief. Innerhalb eines Diktats trifft nichts: Der Titel läuft
  zuerst, teilt mit den Abschnitten aber nur die Systemnachricht, und bei R liegt die knapp unter
  1.024. Die 34/51 Treffer aus dem Pilot stammen aus einem Stapellauf, nicht aus dem Alltag.
- Der Unterschied beträgt **0,07 ¢ je Diktat**. Bei ~0,30 ¢ Gesamtkosten je einminütigem Diktat
  (#71) entscheidet die Eingabe hier nichts; die Ausgabe- und Denk-Token aus Schritt 2 wiegen
  schwerer.
- **K2 entfällt.** K1 liegt schon deutlich unter der Schwelle; weiteres Kürzen spart höchstens
  Bruchteile eines Hundertstel-Cents und riskiert Vertrag.

---

# Schritt 2 — Messlauf (15.09.2026)

`messlauf.py` (7 Tests) + `format_checks.py` (37 Tests). `corpus.v3` × {R, K1} × Titel + acht
Abschnitte, K = 1, `gpt-5.6-luna`, E-Mail auf der Prosa desselben Kandidaten. **828 Aufrufe,
0 Fehler, 685 s, 0,59 $.** Rohdaten: `prompt-sandbox\eval\messlauf73.json` (+ `_gen.jsonl`).

## Kosten und Token (Mittel je Aufruf)

| Abschnitt | Eingabe R / K1 | Ausgabe R / K1 | davon Denken R / K1 | Wörter R / K1 |
|---|---:|---:|---:|---:|
| Titel | 1.426 / 826 | 45 / 42 | 28 / 26 | – |
| Abstract | 1.474 / 930 | 209 / 232 | 147 / 172 | 28 / 28 |
| Zusammenfassung | 1.703 / 1.126 | 572 / 565 | 138 / 145 | 252 / 250 |
| Ausführlich | 1.495 / 973 | 475 / 498 | 90 / 114 | 246 / 252 |
| Aufgaben | 1.822 / 1.187 | 690 / 663 | 532 / 502 | 84 / 88 |
| Gesprächsnotiz | 1.446 / 1.010 | 536 / 354 | 115 / 182 | 233 / 100 |
| E-Mail | 1.698 / 1.087 | 470 / 362 | 116 / 98 | 224 / 168 |
| Stundenzettel | 1.442 / 1.013 | 294 / 358 | 136 / 240 | 81 / 62 |
| Analog | 1.430 / 879 | 306 / 225 | 50 / 77 | 151 / 87 |

**Je Diktat (6 automatische Aufrufe): R 0,490 ¢ · K1 0,400 ¢ (−18 %).** Die Ausgabe ist bei den
drei Abschnitten, die ohnehin laufen (Zusammenfassung, Ausführlich, Aufgaben), praktisch gleich
lang; Katalog-Konstanten (#71) müssen dafür nicht neu gemessen werden. Kürzer wird es bei
Gesprächsnotiz, E-Mail und Analog — dort, wo K1 eine Obergrenze oder den Leerfall setzt.

**Cache: 0 von 828 Aufrufen mit gecachten Token — auch bei R.** Im Pilot waren es 50/102 (ein
Abschnitt, dicht hintereinander), in der Kalibrierung 3/92. Der Cache-Rabatt, für den R seine
Länge behalten müsste, tritt also schon im Stapellauf kaum ein, im Alltag erst recht nicht.

Erstversuch wohlgeformt (`finish_reason = stop`, nicht leer): 100 % in allen Zellen.

## Formatprüfungen (verletzt / anwendbar)

⚠ R hatte für Gesprächsnotiz, E-Mail, Stundenzettel und Analog keinen Längen- oder Formvertrag.
Eine „Verletzung" dort misst R an K1s Vertrag, nicht an seinem eigenen.

| Abschnitt | R | K1 |
|---|---|---|
| Abstract | Länge 3, zwei Absätze 2, Markdown 1 | Länge 2 (53 statt 50, 22 statt 20 Wörter) |
| Zusammenfassung | – | – · verschachtelte Listen in 52 % (R 63 %) → #83 |
| Ausführlich | Listenform 4 | Listenform 1 · Platzhalter 1 (s. u.) · Wortverhältnis 1,03 bei beiden |
| Aufgaben | > 8 Aufgaben 2, > 20 Wörter 2 | > 8 Aufgaben 2, > 20 Wörter 4 · Leerfall 2 (R 4) |
| Gesprächsnotiz | > 250 Wörter 16, erfundene/unbelegte Namen 2, Platzhalter 2 | > 250 Wörter 1 · **Leerfall 15/46, alle berechtigt** |
| E-Mail | > 250 Wörter 14, Markdown 3 | > 250 Wörter 2 · **Duzen 1** · **Platzhalter 1** |
| Stundenzettel | ohne Liste 17, keine Zeile mit Dauer 29/29 | **33/46 nur „Dauer nicht genannt"** · Tageszeilen als Aufzählung |
| Analog | Aufzählung 23, > 120 Wörter 24 | – |

**Gelesen, nicht nur gezählt:**

- **Gesprächsnotiz-Leerfall stimmt:** Die 15 Diktate sind Aufgabenstellungen, Materialbestellung,
  Tagesabschluss, Schadensaufnahme, Übergabe, Testaufnahmen und Zeiterfassung. Echte Gespräche
  („Telefonat Angebot", „Besprechung Lüftungsanlage") bekommen weiter eine Notiz. Genau das
  beseitigt die Dreifachausgabe aus B-06.
- **Stundenzettel K1 — echter Mangel:** Auf Diktaten ohne Zeiterfassung macht K1 aus *geplanten*
  Aufgaben Stundenzettel-Zeilen („Dauer nicht genannt – Messung der Trocknungswerte" aus einer
  Übergabe, „1 Tag – Stichprobe" aus einer Planung). Der Leerfall greift nur, wenn weder Zeiten
  noch *Tätigkeiten* vorkommen — und Tätigkeiten kommen fast immer vor. Außerdem setzt das Modell
  die Tageszeilen als Aufzählungspunkt.
- **E-Mail K1 — zwei Einzelfälle:** Ein Diktat, das den Kollegen duzt, ergab eine duzende Mail
  (R: gesiezt). Und bei dem Diktat mit Transkriptionslücken („Projekt … soll … und …")
  übernahm die Mail die Markierung „[inhaltlich unklar]" aus der Prosa — in einer Kundenmail
  darf das nicht stehen.
- **Rückfrage in der Prosa:** Fehlalarm — die Frage steht so im Diktat.
- **Aufgaben über 20 Wörter:** Grenzfälle (21–26 Wörter) mit Frist und Person in Klammern, bei R
  genauso.

## Nachbesserung E-Mail (15.09.2026)

Entscheidungen zum Soll-Profil: Stundenzettel bleibt wie K1 · Aufgaben bleiben `- ` · die E-Mail
siezt **immer** · als unklar markierte Stellen entfallen in der Mail. Nur `emailPrompt` von K1
geändert (Register als eigener Punkt mit Formmuster „Bitte prüfen Sie …“, statt „Lieber/Liebe …“
als Anrede-Beispiel; Regel für Unklarheiten mit Begründung). Die von `SummaryPromptsTests`
festgenagelten Wendungen stehen weiter wörtlich drin. Alte Fassung: `kandidat.K1.v1.de.json`.

`format_checks.py` prüft jetzt zusätzlich **Du-Imperativ** („bitte prüfe …“, von `sie_form` nicht
erkannt) und **Unklarheitsmarken**; „unklar“ als Wort wird nur gezählt, weil es echter Inhalt sein
kann. `messlauf.py --basis <gen.jsonl> --neu K1:emailPrompt` übernimmt den alten Lauf und erzeugt
nur die genannten Antworten neu: **46 Aufrufe, 0 Fehler, 44 s.** Rohdaten `eval\messlauf73b.json`.

| E-Mail | Wörter | Ausgabe / Denken | Kosten je Mail | Verletzungen |
|---|---:|---:|---:|---|
| R | 217 | 470 / 116 | 0,090 ¢ | > 250 Wörter 14 · Markdown 3 · **Du-Imperativ 2** |
| K1 alt | 162 | 362 / 98 | 0,065 ¢ | > 250 Wörter 2 · Duzen 1 · Du-Imperativ 1 · Unklarheitsmarke 1 |
| K1 neu | 153 | 388 / 136 | 0,070 ¢ | > 250 Wörter 2 (255 / 265, beide synthetisch > 1.000 Wörter) |

Gelesen: Die Mail an einen Mitarbeiter, den das Diktat beim Vornamen nennt und duzt, siezt jetzt
durchgehend („Guten Tag ‹Vorname›, bitte beschäftigen Sie sich …“). Die Mail zum Diktat mit
Transkriptionslücken nennt nur noch Start und Rückmeldeweg — die unklaren Stellen fehlen, wie
entschieden. Das eine verbleibende „unklar“ ist Inhalt („dadurch ist unklar, wo welche Dokumente zu
finden sind“). Die neue Prüfung zeigt, dass **R** in zwei Mails Sie und Du mischt.

---

# Schritt 3 — Lesevergleich (gelesen bis 21.09.2026)

16 Paare R gegen K1 (K1 mit nachgebesserter E-Mail), 8 Vorlagen × 2 echte Diktate, Seiten
zufällig, Frage symmetrisch („gleichwertig / A schlechter / B schlechter, weil …“), weil zufällige
Seiten eine einseitige Frage zur Hälfte auf R gerichtet hätten. Seite `build_pair_artifact.py` +
`pair_template.html` (12 Tests), Urteile im Artefakt-Speicher `vergleich/p000…p015`, Zuordnung und
Urteile nur in der Sandbox (`eval\lesevergleich73.zuordnung.json`, `eval\lesevergleich73_urteile\`).

| Vorlage | Paar 1 | Paar 2 | Übernahmeregel |
|---|---|---|---|
| Abstract | R schlechter („auf den Punkt, wichtige Infos“) | **K1 schlechter** („A bringt es besser auf den Punkt“; 16 gegen 15 Wörter) | K1 fällt durch |
| Zusammenfassung | R schlechter („nur minimal“, Dopplung Kontext/Offene Punkte) | **K1 schlechter** („ohne Wiederholungen auf den Punkt“) | K1 fällt durch |
| Ausführlich | R schlechter („mehr Infos, besser lesbar“) | **K1 schlechter** („nur minimal … roter Faden“) | K1 fällt durch |
| Aufgaben | R schlechter („bringt es besser auf den Punkt“) | ⚠ Wahl „K1 schlechter“, Begründung beschreibt die Dopplung in **R** | offen: Verklicker? |
| Gesprächsnotiz | R schlechter („ist kein Gespräch“, Leerfall) | **K1 schlechter** („viel mehr wichtige Infos“; K1 239, R 401 Wörter) | K1 fällt durch |
| E-Mail | R schlechter („natürlicher, mehr Infos“) | **K1 schlechter** („genauere Infos“; K1 224, R 325 Wörter) | K1 fällt durch |
| Stundenzettel | R schlechter („zu viel Info“) | R schlechter („gehört nicht in den Stundenzettel“) | **K1 gewinnt** |
| Analog | R schlechter („kein Freitext“) | gleichwertig | **K1 gewinnt** |

Summe: R 9-mal schlechter (10 mit p011 als Verklicker), K1 6-mal (5), einmal gleichwertig.

**Befund:** Die zwei klarsten K1-Niederlagen (Gesprächsnotiz, E-Mail) liegen auf den längsten
Diktaten (426 und 452 Wörter) und stoßen an die in K1 gesetzte Obergrenze von 250 Wörtern — der
Leser vermisst genau das, was die Grenze abschneidet. Die übrigen drei geteilten Vorlagen sind 1:1
mit „minimal“-Begründungen; bei zwei Paaren je Vorlage ist das vom Zufall nicht zu trennen.

**Entscheidungen (User, 21.09.2026):** p011 war ein Verklicker (R schlechter) → Aufgaben: K1.
Systemnachricht: K1. Abstract, Zusammenfassung, Ausführlich: 1:1 als Unentschieden gewertet → K1
(besser bei Format und Kosten). Gesprächsnotiz und E-Mail: einmal nachbessern.

## Nachbesserung Gesprächsnotiz und E-Mail (21.09.2026)

- **K1 v3:** Obergrenze beider Vorlagen 250 → 400 Wörter (`messlauf73c`, 92 Aufrufe). E-Mail
  wirkt: Mitarbeiter-Diktat 224 → 338 Wörter (R 325), sonst nur zwei Mails über 400. Gesprächsnotiz
  **nicht**: p010 239 → 244 Wörter. Die Grenze war nicht die Ursache.
- **Ursache Gesprächsnotiz:** Die Gliederung Teilnehmer/Themen/Beschlüsse/Vorgehen machte „Themen“
  zur Stichwortliste; Sachverhalte, die weder Beschluss noch Aufgabe sind, fielen weg (R schreibt
  „Themen und Ergebnisse“ mit Einzelheiten).
- **K1 v4** (nur Gesprächsnotiz, `messlauf73d`, 46 Aufrufe): Teil „Themen und Ergebnisse“ mit allen
  genannten Einzelheiten, weitere Beteiligte als eigener Punkt, Leerfall als Entscheidungsregel
  („Austausch mit mindestens einer anderen Person … Aufgabenbeschreibungen, Notizen an sich selbst,
  Bestellungen und Zeiterfassungen sind kein Gespräch“). p010 jetzt 318 Wörter mit den vermissten
  Angaben.
- **Leerfall gegen eine Referenz geprüft**, nicht gegen v1: Die 46 Anfänge gelesen, 12 Diktate
  schildern einen Austausch (Gespräch, Telefonat, Besprechung, Begehung mit Mieterin). v4 schreibt
  für **alle 12** eine Notiz und setzt sonst den Leerfall — bis auf zwei Grenzfälle
  (`syn2:softwarefehler`, `syn2:rechnungsklaerung`: nur ein früheres Telefonat bzw. „der Kunde
  sagt“ erwähnt). v1 schrieb für rund 17 Nicht-Gespräche eine Notiz (Aufgabenstellungen,
  Montagsmeeting-Konzept, Begehungen ohne Gesprächspartner). Der Anstieg 15 → 32 Leerfälle ist
  also die Korrektur, nicht der Fehler.

Kosten je Diktat K1 v4: 0,390 ¢ (R 0,490 ¢).

## Folgerunde q (4 Paare) und Endfassung K1 v5 (21.09.2026)

| Paar | Vorlage | Urteil |
|---|---|---|
| q001 | Gesprächsnotiz, Projekt Johann | R schlechter („ist kein Gespräch“) |
| q002 | Gesprächsnotiz, Gespräch mit externem Dienstleister | K1 v4 schlechter („genauer, alle Infos“) |
| q000 | E-Mail, Anweisung an einen Mitarbeiter | K1 v3 schlechter („besserer Lesefluss“) |
| q003 | E-Mail, Montagsmeeting | K1 v3 schlechter („so gut wie gleichwertig“) |

Bei Gesprächsnotiz und E-Mail zog der Leser in beiden Runden die ausführlichere R-Fassung vor.
Nach der Übernahmeregel bleibt dort R. **Entscheidung (User, 21.09.2026):** R-Wortlaut als
Grundlage, ergänzt nur um die entschiedenen Regeln — kein erneutes Lesen, nur Formatprüfung.

- **Gesprächsnotiz v5:** R-Wortlaut + Leerfall-Regel aus v4; „Transkript eines *Gesprächs* auf
  Deutsch“ → „das Transkript eines Sprachdiktats“ (setzte ein Gespräch voraus, B-01).
- **E-Mail v5:** R-Wortlaut + Sie-Form mit Formmuster oben + keine Unklarheitsmarken + kein
  Markdown; „siezen“ als letzter Punkt entfällt (M-04).

`messlauf73e` (92 Aufrufe, 0 Fehler) gegen R:

| | Gesprächsnotiz R | v5 | E-Mail R | v5 |
|---|---:|---:|---:|---:|
| Wörter (ohne Leerfall) | 233 | 250 | 217 | 213 |
| Kosten je Aufruf | 0,093 ¢ | 0,047 ¢ | 0,090 ¢ | 0,081 ¢ |
| Gespräche erkannt (Referenz 12) | 12 | 12 | – | – |
| Notiz ohne Gespräch | **34** | **4** | – | – |
| Markdown / Du-Imperativ | 1 / – | – / – | **3 / 2** | – / – |
| Platzhalter | 2 | 2 („[eigene Person]“) | – | – |

Die vier gelesenen Diktate: Dienstleister-Notiz 372 Wörter (R 401); Mails 313–357 Wörter (R 268–361).
Die „Rückfrage“ in einer v5-Mail ist die verlangte Einladung zu Rückfragen (Fehlalarm).
Referenz der 12 Gespräche: `eval\gespraech_referenz.json`.

**Damit sind alle acht Vorlagen entschieden — Endfassung K1 v5** (`kandidat.K1.de.json`,
`kandidaten-73e\prompts.K1.json`): Systemnachricht und sechs Abschnitte aus K1, Gesprächsnotiz und
E-Mail auf R-Basis. Kosten je Diktat (6 automatische Aufrufe): **0,390 ¢ gegen R 0,490 ¢ (−20 %)**.

---

# Schritt 5 und 6 — Übernahme (21.09.2026, freigegeben)

- **`SummaryPrompts`** maschinell aus `kandidat.K1.de.json` erzeugt (`summary_prompts_schreiben.py`
  in der Sandbox), damit Konstanten und Team-Datei zeichengleich sind.
- **Team-Datei** `Z:\12_Tools\Peano\Johann\prompts.json` überschrieben. Vorher geprüft: seit
  10.09.2026 unverändert (byteidentisch mit der Referenzkopie). Sicherungen:
  `Z:\12_Tools\Peano\Johann\prompts.vor-73-2026-09-21.json` und
  `prompt-sandbox\prompts.TEAM-vor-73-2026-09-21.json`. `promptDefaultsRevision` und
  Schlüsselreihenfolge unverändert, keine `customCategories` in der Datei.
- **`SummaryPromptsTests`:** Die zwei Tests, die „HOCHSPEZIALISIERTER EXPERTE“ und
  „### WHAT NOT TO DO ###“ festnagelten, ersetzt durch Tests auf das neue Verhalten
  (kein Denkprozess, normale Schreibung, eine Unklarheits-Regel, Verdichtung beim Abschnitt,
  Beispielpaar, keine Sprachprämisse, Leerfälle, Sie-Form mit Formmuster, Unklarheitsmarken,
  kein Markdown in der Mail).
- **`dotnet test`:** 578 von 579 grün, `TeamPromptDriftTests` 9/9 grün gegen die neue Team-Datei.
  Rot war einmal `SummaryModelLiveAvailabilityTests` für Terra (HTTP 400, 16 Token fürs Denken
  aufgebraucht) — hängt nicht an den Prompts, danach dreimal grün. Flackert gelegentlich.
- **Schritt 6 (Analog auf Abruf):** schon umgesetzt und getestet
  (`SectionModeFilteringTests.Recommended_MarksStundenzettelAnalogAndEmailOnDemand`).
- **Katalog-Konstanten (#71):** bleiben. Luna-Gerade 62 + 1,05·T gegen 230 gemessene
  Aufrufe der automatischen Abschnitte: Endfassung +1 % über die Summe (Fit 125 + 0,87·T), R lag
  −11 %. Terra/Sol nicht neu gemessen.
- **Offen:** S4 (zentrale Markdown-Regel) erst nach #57. E-Mail und Gesprächsnotiz tragen den
  R-Wortlaut und damit M-02/M-03 (Längenadjektiv, „Grußformel“ doppeldeutig) — bewusst, weil der
  Leser diese Fassung vorzog.

## Nachprüfung nach Codex-Befunden (21.09.2026, #88)

`format_checks.py` zählte einen Leerfall schon, wenn der Satz nur *vorkam*; `messlauf.py` hätte
gescheiterte Aufrufe beim Fortsetzen nicht wiederholt. Beides ist behoben. Aus den Rohdaten
neu gerechnet: **0 Abweichungen** in `messlauf73`, `73b`, `73c`, `73d` und `73e` – jeder gezählte
Leerfall stand allein, und kein Lauf hatte einen gescheiterten Aufruf. Die Ergebnisse oben gelten
unverändert.

# Zentrale Markdown-Regel (S4, 21.09.2026, nach #57)

Voraussetzung aus dem Faktorenplan §4.2 erfüllt: #57 wandelt Markdown in der Mail nach HTML
(klassisches und neues Outlook). **K1 v6:** Block „Form“ in der Systemnachricht („Formatiere die
Antwort in Markdown, wo es Bedeutung trägt … Gibt der Abschnitt reinen Text oder eine andere Form
vor, gilt seine Vorgabe.“), die Klartext-Zeile der E-Mail entfällt. Systemnachricht 408 → 492 Token.

`messlauf73f` (alle K1-Abschnitte, 414 Aufrufe, 0 Fehler) gegen v5:

- **E-Mail:** In 32 von 46 Mails setzte das Modell die **Betreffzeile fett** (`**Betreff: …**`) –
  Johann hätte den Betreff nicht gefunden und eine fette Zeile oben in die Mail gesetzt.
  → **K1 v7:** „die Betreffzeile steht als reiner Text ohne Markdown in der ersten Zeile“, und
  `MailDraftBuilder` erkennt die Zeile auch in Markdown-Hülle. `messlauf73g` (46 Aufrufe):
  Betreffzeile 46/46 rein, Anrede 46/46, Fettdruck im Text sparsam (4 Mails).
- Zusammenfassung und Ausführlich nutzen mehr Fettdruck (105 → 231 bzw. 29 → 47 Stellen);
  Abstract, Aufgaben, Stundenzettel und Analog bleiben praktisch ohne Markdown.
- Gesprächsnotiz: 12/12 Gespräche erkannt, Notizen ohne Gespräch 5 (v5: 4) – Streuung eines Laufs.
- Kosten je Diktat unverändert (0,389 ¢ gegen 0,388 ¢).

**Ausgabewege geprüft:** Detailansicht und HTML wandelten schon; das **PDF** druckte Aufgaben,
Gesprächsnotiz, Stundenzettel, Analog und E-Mail als Rohtext und kannte keinen Fettdruck – jetzt
einheitlicher Markdown-Pfad mit Fett/kursiv (`InlineMarkdown`), mit PyMuPDF geprüft: keine
Sternchen. **Kopieren** der E-Mail und die `.txt`-Mail geben Klartext aus. Live im klassischen
Outlook über den echten Composer: Betreff sauber, `<strong>`/`<em>`, keine Sternchen.

Übernommen in Team-Datei und `SummaryPrompts` (Sicherung
`Z:\12_Tools\Peano\Johann\prompts.vor-markdown-2026-09-21.json`), `TeamPromptDriftTests` grün.
