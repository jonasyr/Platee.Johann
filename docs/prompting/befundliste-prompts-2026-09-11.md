# Befundliste: alle neun Prompts, geprüft gegen den GPT-5.6-Leitfaden

**Stand 2026-09-11 · Issue #73 · Grundlage: `docs/prompting/gpt-5.6-prompting-leitfaden.de.md`**

Geprüft wurde die Team-Datei im Stand vom 10.09.2026, 16:35 Uhr
(`Z:\12_Tools\Peano\Johann\prompts.json`, 10.171 Bytes). Eine schreibgeschützte Referenzkopie
liegt unter `Documents\Johann\prompt-sandbox\prompts.TEAM-REFERENZ-2026-09-10.json`.

**Dies ist eine Befundliste, keine Überarbeitung.** Nichts wurde geändert. Jeder Befund trägt
eine Nummer, einen Schweregrad und einen Verweis auf den Abschnitt des Leitfadens, aus dem das
Kriterium stammt.

| Schweregrad | Bedeutung |
|---|---|
| **Hoch** | Widerspruch, falsche Prämisse oder messbare Kosten. Wirkt auf jedes Diktat. |
| **Mittel** | Fehlender Vertrag, Redundanz, unklare Vorgabe. Wirkt situativ. |
| **Niedrig** | Sprachfehler, Uneinheitlichkeit. Kostet Vertrauen, nicht Qualität. |

---

## 0. Mengengerüst

| Prompt | Token | läuft im Standard | Eingabequelle |
|---|---:|---|---|
| `systemMessage` | **1.008** | bei **jedem** Aufruf | — |
| `aufgabePrompt` | 445 | automatisch | Transkript |
| `structuredPrompt` | 326 | automatisch | Transkript |
| `emailPrompt` | 309 | auf Abruf | **Prosa-Zusammenfassung** |
| `prosePrompt` | 118 | automatisch | Transkript |
| `abstractPrompt` | 99 | immer (Intrinsic) | Transkript |
| `gespraechsnotizPrompt` | 69 | automatisch | Transkript |
| `stundenzettelPrompt` | 65 | auf Abruf | Transkript |
| `analogPrompt` | 53 | auf Abruf | Transkript |

Die System-Nachricht macht damit **rund 83 % der gesamten Eingabe je Diktat** aus
(6 × 1.008 = 6.048 Token). Jede Kürzung dort wirkt sechsfach — und jeder Fehler dort ebenso.

⚠ **Vor dem Kürzen §12.1 des Leitfadens lesen.** Die System-Nachricht liegt bei 1.008 Token,
die Cache-Schwelle bei 1.024. Das Präfix je Abschnitt (System + Vorlage) liegt aktuell zwischen
1.057 und 1.449 Token. Wer die System-Nachricht um mehr als ~33 Token kürzt, drückt den
kürzesten Abschnitt unter die Schwelle.

---

## 1. Übergreifende Befunde

### B-01 · Falsche Prämisse „auf Deutsch" in fünf Prompts — **Hoch**

`abstractPrompt`, `structuredPrompt`, `gespraechsnotizPrompt`, `stundenzettelPrompt` und
`analogPrompt` beginnen mit „Du erhältst ein Transkript … **auf Deutsch**".

Das ist seit **#58** falsch. Das Transkript bleibt in der gesprochenen Sprache; nur die
generierten Abschnitte sind deutsch. Die System-Nachricht sagt es korrekt („Das Diktat kann in
einer beliebigen Sprache vorliegen"), fünf Abschnitts-Prompts widersprechen ihr.

Damit ist das **kein Schönheitsfehler, sondern ein Widerspruch zwischen System- und
Abschnitts-Prompt** — und genau das, was laut Leitfaden §3.3 teurer ist als eine Lücke.

*Vorschlag:* Die Sprachangabe ersatzlos streichen. Die System-Nachricht regelt es bereits (§3.2:
jede Regel genau einmal).

### B-02 · Die Ausgabesprache steht zweimal in einem einzigen Satz — **Mittel**

In der System-Nachricht:

> „SCHREIBE ausnahmslos auf Deutsch, unabhängig von der Sprache des Diktats. Das Diktat kann in
> einer beliebigen Sprache vorliegen; sämtliche Ausgaben sind dennoch immer deutsch"

Beide Hälften sagen dasselbe. Leitfaden §3.2. *Vorschlag:* ein Satz.

### B-03 · Kein einziger Prompt verlangt Markdown, die App rendert aber Markdown — **Mittel**

`MarkdownFlowDocumentConverter` rendert jeden generierten Abschnitt. Verlangt wird Markdown
aber nirgends explizit; `structuredPrompt` nennt `###`, `aufgabePrompt` nennt `-`. Dass die
übrigen Abschnitte brauchbares Markdown liefern, ist **Zufall, nicht Vertrag**.

Das erklärt rückwirkend die drei Anzeigefehler aus v1.4.0 (verschachtelte Listen, rohes
Markdown in Prosa und Abstract): Die Ausgabeform war nie vereinbart.

Leitfaden §6: Markdown ist eine *lockere* Formatvorgabe und damit unbedenklich — anders als
erzwungenes JSON. *Vorschlag:* eine Markdown-Regel **einmal** in die System-Nachricht, nicht in
neun Prompts.

### B-04 · Überschriften-Unterdrückung viermal, leicht unterschiedlich formuliert — **Niedrig**

`prosePrompt`, `gespraechsnotizPrompt`, `stundenzettelPrompt`, `analogPrompt` tragen je eine
Variante von „Gib keine Überschrift für den Abschnitt aus — die Anwendung setzt sie bereits."
`abstractPrompt` trägt sie **nicht**, `aufgabePrompt` löst es anders.

Die Wiederholung je Prompt ist technisch unschädlich (getrennte Aufrufe), aber sie ist
negativ formuliert (§3.5) und sie fehlt ausgerechnet beim Abstract. *Vorschlag:* einmal positiv
in die System-Nachricht: „Beginne mit dem ersten inhaltlichen Satz; die Überschrift setzt die
Anwendung."

### B-05 · Kein Abschnitt außer „Aufgaben" kennt den Leerfall — **Mittel**

Nur `aufgabePrompt` sagt, was zu tun ist, wenn das Transkript nichts Passendes enthält
(„Keine Aufgaben genannt."). Die übrigen acht sagen nichts.

**Empirisch geprüft** (11.09.2026, `gpt-5.6-luna`, reines Zeiterfassungs-Diktat ohne Gespräch):
Das Modell **erfindet nichts**. Es schreibt „Teilnehmer: Nicht angegeben" und „Keine Beschlüsse
dokumentiert". Die Halluzinationssorge aus Leitfaden §11 bestätigt sich hier also **nicht** —
das gehört ausdrücklich festgehalten, statt eine Gefahr zu behaupten, die ich nicht messen
konnte.

Das tatsächliche Problem ist ein anderes, siehe B-06.

### B-06 · Drei Abschnitte geben auf demselben Diktat denselben Inhalt zurück — **Mittel**

Im selben Test lieferten `gespraechsnotizPrompt` und `analogPrompt` inhaltlich dasselbe wie die
Zusammenfassung — dreimal dieselben zwei Zeitblöcke, nur anders formatiert. Der Nutzer bezahlt
drei Aufrufe und liest dreimal dasselbe.

Das ist kein Prompt-Fehler im engeren Sinn, sondern eine Folge davon, dass Abschnitte
automatisch laufen, die zum Diktat nicht passen. *Vorschlag:* keine Prompt-Änderung, sondern
ein Hinweis in der Auswertung — und ggf. eine Frage an den Chef, ob „Analog" überhaupt noch
gebraucht wird (siehe P-09).

---

## 2. `systemMessage` — 1.008 Token, wirkt sechsfach

Der mit Abstand folgenreichste Prompt. Sieben Befunde.

### S-01 · Durchgehende Großschreibung kostet 37 % zusätzliche Token — **Hoch**

Die Persona und die Instruktionsblöcke sind vollständig in Versalien gesetzt.

**Gemessen:** Diese Nachricht tokenisiert mit **3,14 Zeichen je Token** gegen **4,3** bei
normaler deutscher Prosa — rund **37 % mehr Token für dieselben Wörter**. Bei sechs Aufrufen je
Diktat zahlt jede Aufnahme diesen Aufschlag sechsmal.

Für einen Nutzen gibt es keinen Beleg (Leitfaden §7.1). *Vorschlag:* normale Schreibweise.
Erwartete Ersparnis grob 200–250 Token je Aufruf, ohne inhaltliche Änderung.

⚠ Genau hier greift aber die Cache-Warnung: 1.008 − ~250 = ~760 Token, damit fielen **alle**
Abschnitte unter die 1.024er-Schwelle. Da Caching bei uns ohnehin nie greift (Leitfaden §12.1,
gemessen), ist das derzeit folgenlos — es muss aber bewusst entschieden und nicht übersehen
werden.

### S-02 · `### CHAIN OF THOUGHTS (DENKPROZESS) ###` — sieben vorgeschriebene Denkschritte — **Hoch**

Der Block schreibt VERSTEHEN → GRUNDLAGEN → ZERLEGUNG → ANALYSE → AUFBAU → EDGE CASES →
FINALISIERUNG vor. Er ist damit das Musterbeispiel des Anti-Musters aus Leitfaden §3.1.

OpenAI rät für 5.6 ausdrücklich, „das Ziel zu beschreiben, statt jeden Schritt vorzuschreiben"
(OpenAI, 2026a), und die Reasoning-Dokumentation rät explizit von vorgeschriebenen
Zwischenschritten ab (OpenAI, 2026e). Meincke et al. (2025) messen für Reasoning-Modelle „nur
marginale, wenn überhaupt" Gewinne bei deutlich höheren Token- und Zeitkosten.

Der Block umfasst **rund 300 Token** — bei sechs Aufrufen 1.800 Token je Diktat.

*Vorschlag:* ersatzlos streichen; was daraus erhalten bleiben soll, siehe S-03 und S-04.

### S-03 · Direkter Widerspruch: implizite Aussagen ausformulieren vs. nicht interpretieren — **Hoch**

Im Denkprozess, Schritt 4:

> „KLÄRE implizite Aussagen und formuliere sie explizit aus"

Im Verbotsblock:

> „NIEMALS EIGENE MEINUNGEN ODER INTERPRETATIONEN HINZUFÜGEN"

Implizites explizit zu machen **ist** Interpretation. Das Modell kann beides nicht zugleich
erfüllen und verbraucht laut OpenAI (2025a) Denk-Token darauf, den Konflikt aufzulösen.

Der Widerspruch ist zudem inhaltlich riskant: Leitfaden §11 zeigt, dass mehr internes Denken die
faktische Treue *senkt* (r = −0,685). Schritt 4 lädt genau dazu ein.

*Vorschlag:* Schritt 4 entfällt mit S-02. Die Treue-Regel positiv formulieren:
„Jede Aussage muss sich auf das Diktat zurückführen lassen."

### S-04 · Direkter Widerspruch: Unklarheiten markieren vs. keine unklaren Formulierungen — **Hoch**

Denkprozess, Schritt 6:

> „MARKIERE ggf. Unklarheiten neutral"

Verbotsblock:

> „NIEMALS UNKLARE FORMULIERUNGEN STEHEN LASSEN (z. B. ‚irgendwas wurde besprochen')"

Auch hier: zwei Regeln, die sich gegenseitig ausschließen, sobald das Diktat tatsächlich unklar
ist — und Diktate sind oft unklar.

*Vorschlag:* Eine Entscheidungsregel statt zweier Absolutheiten (§3.4):
„Bleibt etwas im Diktat unklar, benenne es als unklar, statt es auszuformulieren oder wegzulassen."

### S-05 · Widerspruch zur Längenvorgabe des Abstracts — **Mittel**

Verbotsblock: „NIEMALS WICHTIGE INFORMATIONEN WEGKÜRZEN ODER VERFÄLSCHEN."
`abstractPrompt`: „Maximal {word_limit} Wörter."

Bei einem langen Diktat schließen sich beide aus. Das System sagt „nie kürzen", der Abschnitt
sagt „höchstens N Wörter".

*Vorschlag:* Im System die Absolutheit auflösen; die Längenregel gehört ohnehin in den
jeweiligen Abschnitt, nicht ins System.

### S-06 · Strukturvorgabe des Systems widerspricht zwei Abschnitten — **Mittel**

System: „STRUKTURIERE die Inhalte logisch (z. B. Einleitung, Hauptpunkte, nächste Schritte)."

- `structuredPrompt` schreibt ein **anderes** festes Schema vor (Kontext / Kernaussagen /
  Entscheidungen / Offene Punkte).
- `aufgabePrompt` verlangt ausdrücklich **keine** Überschriften.

Die System-Vorgabe ist damit in zwei von sechs automatischen Abschnitten falsch.

*Vorschlag:* Streichen. Struktur ist Sache des Abschnitts, nicht des Systems.

### S-07 · Acht Verbote, wo positive Regeln klarer wären — **Mittel**

Der Block `### WHAT NOT TO DO ###` besteht aus acht `NIEMALS`-Sätzen. Leitfaden §3.5: Negation
wirkt schwächer, weil das Unterdrückte erst repräsentiert werden muss.

Drei davon sind zudem entbehrlich, weil sie Verhalten verbieten, das 5.6 ohnehin nicht zeigt
(„NIEMALS CHAOTISCHE ODER UNSTRUKTURIERTE TEXTE ERZEUGEN") — Leitfaden §4: Prozessanweisungen
für ohnehin zuverlässiges Verhalten entfernen.

*Behalten* würde ich die Substanz von zweien: keine Umgangssprache, kein wörtliches Transkript.
Beide positiv formuliert.

### S-08 · Das Beispielpaar am Ende ist **gut** und soll bleiben — **kein Mangel**

> **SCHLECHTES BEISPIEL:** „Also wir haben irgendwie über das Projekt geredet …"
> **GUTES BEISPIEL:** „Es wurde der aktuelle Stand des Projekts besprochen …"

Das ist kein Few-Shot-Aufgabenbeispiel, sondern ein **Formmuster** im Sinne von Leitfaden §8 —
es klärt eine Registerfrage, die keine Regel sauber ausdrückt. Genau die Sorte Beispiel, die
laut eurer eigenen Erfahrung („Beispiele schlagen Regeln", `CLAUDE.md`) trägt.

**Ausdrücklich nicht streichen.**

### S-09 · Englische Überschriften in einem deutschen Prompt — **Niedrig**

`### CHAIN OF THOUGHTS ###`, `### WHAT NOT TO DO ###`, `### INSTRUKTIONEN ###` — zwei englisch,
eins deutsch. Erledigt sich mit S-02 und S-07.

---

## 3. `abstractPrompt` — 99 Token

### A-01 · Falsche Sprachprämisse — **Hoch** → siehe B-01

### A-02 · Einzige Vorlage ohne Überschriften-Regel — **Mittel**

Alle anderen Abschnitte sagen „Gib keine Überschrift aus". Das Abstract nicht. Ob es in der
Praxis eine erzeugt, hängt allein vom Modell ab — also Zufall.

### A-03 · Kein Leerfall — **Niedrig**

Was, wenn das Diktat zwei Sätze lang ist? Derzeit ungeregelt. Nach B-05 empirisch unkritisch,
aber unnötig offen.

### A-04 · `{word_limit}` ist die einzige Zahl, die von außen kommt — **Hinweis**

Der Platzhalter wird von `SummaryGenerator` gefüllt. Beim Umbau darauf achten, dass er erhalten
bleibt — sonst steht „Maximal {word_limit} Wörter" wörtlich im Prompt.

---

## 4. `structuredPrompt` — 326 Token · **der beste der neun**

### T-01 · Falsche Sprachprämisse — **Hoch** → siehe B-01

### T-02 · Die Relevanzkriterien sind **Substanz, nicht Gerüst** — **nicht kürzen**

> „berücksichtige Inhalte als relevant, wenn sie mindestens eines der folgenden Kriterien
> erfüllen: enthalten eine Entscheidung … führen zu einer konkreten Handlung … betreffen das
> Hauptthema … werden mehrfach erwähnt oder besonders betont"

Das ist eine **Bewertungsvorschrift**, keine Prozessanweisung. Genau hier greift der Gegenbefund
aus Leitfaden §10: Thelwall (2024) fand, dass Kürzen *solcher* Anweisungen die Ergebnisse
verschlechtert. Der Prompt ist mit 326 Token der zweitlängste — und das zu Recht.

**Ausdrücklich nicht ausdünnen.**

### T-03 · Zwei Regeln sagen fast dasselbe — **Niedrig**

> „Bevorzuge Informationsdichte statt extreme Kürze"
> „prägnante Zusammenfassung, aber inhaltlich vollständig"

Beide regeln die Länge, keine nennt eine Zahl. Leitfaden §5.2: Zahlen statt Adjektive.

### T-04 · Satzzeichen fehlt, Kleinschreibung uneinheitlich — **Niedrig**

„…der folgenden Überschriften zu" (Punkt fehlt); „keine todos", „prägnante Zusammenfassung",
„verwende nur…" beginnen klein, andere Stichpunkte groß.

---

## 5. `prosePrompt` — 118 Token

### P-01 · Widerspruch zur System-Nachricht über die Aufgabe selbst — **Hoch**

`prosePrompt` verlangt: „dass **alle Inhalte** enthalten sind", also eine vollständige,
lesbare Wiedergabe.

System-Nachricht verbietet: „NIEMALS WÖRTLICHES TRANSKRIPT STATT ZUSAMMENFASSUNG LIEFERN" und
verlangt „ELIMINIERE Füllwörter, Wiederholungen und unnötige Abschweifungen" sowie „FASSE
Inhalte zusammen".

Der Abschnitt ist inhaltlich eine **Aufbereitung**, keine Zusammenfassung — die System-Nachricht
rahmt ihn aber als Zusammenfassung. Das Modell muss zwischen zwei Aufträgen wählen.

Das ist der **schwerwiegendste Einzelbefund**, weil er den Zweck des Abschnitts betrifft und
nicht nur dessen Form.

*Vorschlag:* Die System-Nachricht darf die Aufgabe nicht festlegen; sie sollte Register, Sprache
und Treue regeln, nicht den Verdichtungsgrad. Der gehört in den Abschnitt.

### P-02 · Drei Sprachfehler im Prompt selbst — **Niedrig**

- „Bereite **den** unten stehenden Transkript" → *das* Transkript
- „dass alle **Inhalten** enthalten sind" → *Inhalte*
- „**umgangsprachliche**" → *umgangssprachliche*

Kein Qualitätsproblem für das Modell, aber ein Vertrauensproblem: Ein Prompt, der um Korrektur
von Grammatikfehlern bittet, sollte keine enthalten.

### P-03 · Überschriften-Regel nur negativ — **Niedrig**

„Gib keine Überschrift … aus" ohne das positive Gegenstück („Beginne mit dem ersten
inhaltlichen Satz"), das andere Prompts haben.

---

## 6. `aufgabePrompt` — 445 Token · **strukturell der modernste**

Aus #66, und das merkt man: expliziter Ausgabevertrag, Formmuster, Leerfall geregelt. Die
Befunde sind entsprechend milder.

### G-01 · Vorbildlich: Leerfall, Formmuster, Zahlen — **kein Mangel**

- Leerfall: „schreibe statt der Liste genau diesen Satz … Keine Aufgaben genannt."
- Formmuster: „Beispiel: ‚PDF je Sprachnachricht erzeugen'" — genau das Beispiel, das laut
  `CLAUDE.md` das kaputte Infinitiv-Deutsch behoben hat (Leitfaden §8)
- Zahlen statt Adjektive: „höchstens 20 Wörter", „höchstens acht Aufgaben"

Dieser Prompt ist die Vorlage, an der sich die übrigen orientieren sollten.

### G-02 · Dieselbe Regel steht zweimal — **Mittel**

„Deine Antwort selbst enthält keine Überschriften." (Kopf)
„Keine Überschriften und keine Zwischenüberschriften …" (Block „Das darfst du nicht")

Und:

„Nutze ausschließlich Informationen, die explizit im Transkript stehen" (Regeln)
„Nichts ergänzen, was nicht im Transkript steht" (Das darfst du nicht)

Leitfaden §3.2: Eine dreifach formulierte Regel liest sich als drei Regeln. Hier doppelt.

### G-03 · Der Block „Das darfst du nicht" ist nach der Dublettenbereinigung fast leer — **Niedrig**

Bleiben würden „keine eigene Nummerierung" und „keine Einleitung/Rückfrage". Beides ließe sich
positiv in den Ausgabevertrag oben ziehen.

---

## 7. `gespraechsnotizPrompt` — 69 Token

### N-01 · Falsche Sprachprämisse — **Hoch** → siehe B-01

### N-02 · Kein Vertrag: keine Länge, kein Format, kein Leerfall — **Mittel**

Der gesamte Auftrag lautet: „Erstelle eine strukturierte, kundentaugliche Gesprächsnotiz
(Teilnehmer, Themen, Beschlüsse, weiteres Vorgehen)."

Verglichen mit `structuredPrompt` fehlt **jede** Bewertungsvorschrift. Nach Leitfaden §10 ist
„kurz" allein kein Gütesiegel — hier ist der Prompt nicht schlank, sondern **unvollständig**.

Die vier Klammerbegriffe sind faktisch eine Gliederung, werden aber nicht als solche benannt.
Im Test erzeugte das Modell daraus fettgedruckte Pseudo-Überschriften — plausibel, aber nicht
vereinbart.

### N-03 · Läuft automatisch auf Diktaten, die keine Gespräche sind — **Mittel** → siehe B-06

---

## 8. `stundenzettelPrompt` — 65 Token

### Z-01 · Falsche Sprachprämisse — **Hoch** → siehe B-01

### Z-02 · Keine Ausgabeform für strukturierte Daten — **Mittel**

„Extrahiere die Zeiten und Tätigkeiten, um sie in einen Stundenzettel einzutragen."

Zeiten und Tätigkeiten sind tabellarische Daten, aber es ist nicht gesagt, ob eine Tabelle, eine
Liste oder Fließtext erwartet wird. Wer die Ausgabe in einen Stundenzettel übertragen soll,
braucht eine verlässliche Form.

### Z-03 · Kein Leerfall — **Mittel**

Ein Diktat ohne Zeitangaben ist der Normalfall, denn der Abschnitt läuft auf Abruf, aber
`AllAuto`-Nutzer bekommen ihn bei jedem Diktat.

---

## 9. `analogPrompt` — 53 Token · **der schwächste**

### L-01 · Falsche Sprachprämisse — **Hoch** → siehe B-01

### L-02 · Der Auftrag ist inhaltsleer — **Mittel**

„Fasse den Eintrag treffend zusammen." Keine Länge, kein Format, kein Kriterium, was „treffend"
heißt. Leitfaden §5.2: nicht messbare Adjektive.

### L-03 · Überschneidet sich vollständig mit Abstract und Zusammenfassung — **Mittel**

Im Test war die Ausgabe inhaltlich nicht von einer Kurzfassung unterscheidbar. Vor jeder
Überarbeitung sollte geklärt werden, **wozu dieser Abschnitt existiert** — sonst optimiert man
etwas, das gestrichen gehört. Siehe P-09 unten.

---

## 10. `emailPrompt` — 309 Token · Sonderfall

### M-01 · Einzige Vorlage, die **nicht** das Transkript bekommt — **Hinweis**

Eingabe ist `{prose_summary}`, nicht `{transcript}`. Die E-Mail erbt damit jeden Fehler der
Prosa-Aufbereitung — insbesondere den Widerspruch aus **P-01**. Eine Verbesserung von
`prosePrompt` verbessert die E-Mail mit; eine Verschlechterung ebenso.

### M-02 · Klassischer Längen-Widerspruch — **Mittel**

„Länge: So kompakt wie möglich bei **vollständiger Information**" — Leitfaden §3.3, die
Musterform des Widerspruchs. Keine Zahl.

### M-03 · „Grußformel" wird in zwei verschiedenen Bedeutungen benutzt — **Mittel**

> „mit Grußformel beginnen, wenn möglich: Namen des Empfängers erkennen …"
> „keine Grußformel am Ende der Mail"

Im ersten Fall ist die **Anrede** gemeint, im zweiten der **Gruß**. Dasselbe Wort für zwei
Dinge, von denen eines verlangt und das andere verboten wird. Das ist für ein Modell schwer
aufzulösen und für einen Menschen beim Pflegen des Prompts auch.

*Vorschlag:* „Beginne mit einer Anrede …" / „Schließe ohne Grußformel; die Signatur kommt aus
Outlook."

### M-04 · `siezen` steht als einzelnes Wort am Listenende — **Niedrig**

Eine der wenigen harten, nicht verhandelbaren Vorgaben steht versteckt als letzter Stichpunkt
einer Aufzählung über Inhalt und Ton. Gehört nach oben zum Register.

### M-05 · „positive Sprache", „Professionelles Deutsch" — nicht überprüfbar — **Niedrig**

Leitfaden §5.2. Ohne Beispiel bleibt es Dekoration.

### M-06 · Markdown vs. E-Mail — **offene Frage, kein Befund**

Die E-Mail geht künftig über Outlook (#57) und wird dort als HTML gesetzt. Ob dieser Abschnitt
Markdown liefern soll, ist bislang nirgends entschieden. Vor B-03 („Markdown in die
System-Nachricht") muss geklärt sein, ob die E-Mail davon ausgenommen wird — sonst landen
`**Sternchen**` in einer Kundenmail.

**Das ist der Befund, der am ehesten zu einem sichtbaren Fehler beim Kunden führt.**

---

## 11. Priorisierung

| Rang | Befund | Warum zuerst |
|---|---|---|
| 1 | **S-03, S-04** Widersprüche im System | Wirken bei jedem Aufruf, kosten Denk-Token, Auflösung ist billig |
| 2 | **B-01** falsche Sprachprämisse (5×) | Widerspruch zur System-Nachricht, seit #58 schlicht falsch |
| 3 | **P-01** Prosa gegen System | Betrifft den Zweck eines Abschnitts, nicht nur die Form — und wirkt über M-01 auf die E-Mail |
| 4 | **S-02** Denkprozess-Block | ~300 Token × 6 Aufrufe, klar belegtes Anti-Muster |
| 5 | **S-01** Großschreibung | ~37 % Token, kein Beleg für Nutzen — aber Cache-Schwelle beachten |
| 6 | **M-06** Markdown in der E-Mail | Risiko eines sichtbaren Fehlers beim Kunden |
| 7 | **B-03** Markdown überhaupt vereinbaren | Erklärt die v1.4.0-Anzeigefehler an der Wurzel |
| 8 | **N-02, Z-02, Z-03, L-02** fehlende Verträge | Substanz ergänzen, nicht kürzen |
| 9 | **G-02, T-03, S-07** Dubletten und Negationen | Aufräumen, geringe Wirkung |
| 10 | **P-02, T-04, M-04, M-05, S-09** Sprach- und Stilfehler | Kosmetik |

---

## 12. Was ich ausdrücklich **nicht** vorschlage

- **`structuredPrompt` ausdünnen.** Seine Länge ist Bewertungsvorschrift, nicht Gerüst (T-02).
  Thelwall (2024) ist der Gegenbeleg zur „kürzer ist besser"-These.
- **Das Beispielpaar aus der System-Nachricht streichen** (S-08). Es ist ein Formmuster und
  wirkt nachweislich.
- **`reasoning_effort` in diesem Zug setzen.** Zwei Variablen gleichzeitig zu bewegen macht die
  Bewertung unmöglich (Leitfaden §10), und es entwertet die Katalog-Konstanten aus #71.
- **Alles in einem Durchgang umschreiben.** OpenAI (2026a) ausdrücklich: „Schreiben Sie einen
  funktionierenden Prompt-Stapel nicht auf einmal um."

---

## 13. Offene Fragen an den Chef

Diese kann ich nicht aus der Technik beantworten:

- **P-09 · Wird der Abschnitt „Analog" noch gebraucht?** Er überschneidet sich vollständig mit
  Kurzfassung und Zusammenfassung (L-03). Ihn zu streichen wäre billiger als ihn zu
  überarbeiten.
- **Soll die E-Mail Markdown enthalten?** (M-06) Hängt an #57.
- **Welcher Verdichtungsgrad ist bei „Ausführlich" gewollt** — vollständige Aufbereitung oder
  echte Zusammenfassung? (P-01) Der Prompt sagt das eine, die System-Nachricht das andere.

---

## 14. Vorgehen für die Überarbeitung

Nach Leitfaden §10 und OpenAI (2026a):

1. **Sandbox steht bereits.** `GlobalPromptFilePath` zeigt auf
   `Documents\Johann\prompt-sandbox\prompts.json`; die Team-Datei ist unberührt, eine
   schreibgeschützte Referenz liegt daneben.
2. Festen Satz von 10–15 Diktaten anlegen (die 16 erfundenen aus #71 sind bereits vorhanden und
   decken 20 s bis 6 min ab).
3. **Eine Änderung pro Durchgang**, beginnend mit Rang 1 der Priorisierung.
4. Alt und neu über den **ganzen** Satz laufen lassen, Ergebnisse nebeneinander lesen.
5. Nur übernehmen, was beim Lesen gewinnt.
6. Erst am Ende, nach ausdrücklicher Freigabe, in die Team-Datei **und** die
   `SummaryPrompts`-Konstanten übertragen — beide, sonst greift die Änderung nicht
   (`TeamPromptDriftTests` bewacht das).
