# Prompts für GPT-5.6 schreiben — ein belegter Praxisleitfaden

**Fassung 1.0 · 11.09.2026 · für Platé.Johann (Issue #73)**

Dieser Leitfaden erklärt, wie man Prompts für die GPT-5.6-Familie (Luna, Terra, Sol) schreibt.
Er ist so geschrieben, dass ihn auch jemand befolgen kann, der noch nie einen Prompt
abgestimmt hat — und so, dass jemand, der das schon getan hat, die Begründung nachprüfen kann.
Jede nicht offensichtliche Aussage trägt einen Beleg; das Literaturverzeichnis am Ende folgt
APA 7.

Drei Arten von Belegen kommen vor, und sie werden bewusst auseinandergehalten:

| Kennzeichnung | Bedeutung |
|---|---|
| **[Hersteller]** | OpenAIs eigene Dokumentation. Maßgeblich für das Modellverhalten, aber nicht unabhängig. |
| **[Begutachtet / Preprint]** | Wissenschaftliche Arbeiten. Unabhängig, aber meist an älteren Modellen als 5.6 geprüft. |
| **[Selbst gemessen]** | Eigene Messungen gegen die Live-API am 11.09.2026. Gilt für unsere Last, nicht allgemein. |

Wo diese drei sich widersprechen, sagt dieser Leitfaden das — statt die glatteste Antwort zu
wählen.

---

## 1. Die Kurzfassung in einem Absatz

Sag dem Modell, **wie ein gutes Ergebnis aussieht** und **wann es fertig ist** — und dann geh
aus dem Weg. Schreib ihm nicht die Schritte vor, auf denen es dorthin kommen soll. Sag jede
Regel **genau einmal**. Entferne alles, was die Ausgabe nicht verändert. Widersprüche kosten
mehr als Lücken, weil das Modell abrechenbare Denk-Token verbraucht, um sie aufzulösen
(OpenAI, 2025a).

Alles Weitere ist die Langfassung dieses Absatzes — samt der Stellen, an denen er nicht ganz
stimmt.

---

## 2. Was GPT-5.6 ist, und warum alte Prompts daneben greifen

GPT-5.6 gibt es in drei Größen: **Luna** (am schnellsten und günstigsten), **Terra**
(ausgewogen) und **Sol** (am leistungsfähigsten) (OpenAI, 2026b). Alle drei sind
*Reasoning-Modelle*: Bevor sie eine sichtbare Antwort schreiben, erzeugen sie verborgene
Denk-Token, die als Ausgabe-Token abgerechnet werden, obwohl man sie nie zu sehen bekommt
(OpenAI, 2026e).

Diese eine Tatsache entwertet einen großen Teil der Prompt-Folklore von vor 2025. Prompts, die
für GPT-4-Modelle geschrieben wurden, tragen typischerweise ein Gerüst, dessen Aufgabe es war,
ein nicht denkendes Modell zum Denken zu bringen: „denke Schritt für Schritt", „analysiere
zuerst, fasse dann zusammen", nummerierte Denkprozesse, durchgerechnete Beispiele. GPT-5.6 tut
das bereits intern. Das Gerüst erkauft also kein Denken mehr — es konkurriert nur noch damit.

OpenAIs Leitfaden zu 5.6 sagt ausdrücklich, dass die Korrektur im **Weglassen** besteht, nicht
im Hinzufügen: Prompts sollen „Ergebnisse, Randbedingungen, Belege und Abschlusskriterien
definieren und das Modell dann einen effizienten Weg wählen lassen"; der Leitfaden „betont das
Entfernen von Gerüst statt das Hinzufügen von Details" (OpenAI, 2026a). **[Hersteller]**

### 2.1 Was bringt das Ausdünnen tatsächlich?

In OpenAIs internen Evaluationen mit Coding-Agenten erzielten schlankere System-Prompts rund
**10–15 % höhere Bewertungen** bei **41–66 % weniger Token** und **33–67 % geringeren Kosten**
(OpenAI, 2026a). Dasselbe Dokument schränkt ein: *„Die Ergebnisse schwanken je nach Last,
behandeln Sie diese Spannen als Richtungsangabe."* **[Hersteller]**

Diese Zahlen sind, was sie sind: ein internes Herstellerergebnis an Coding-Agenten. Sie sind
das beste verfügbare Signal speziell zu 5.6 — aber sie sollten nicht zitiert werden, als wären
sie ein kontrollierter öffentlicher Benchmark.

---

## 3. Die fünf Grundsätze

### 3.1 Beschreibe das Ziel, nicht den Weg

> „Beschreibe das Ziel, statt jeden Schritt vorzuschreiben." (OpenAI, 2026a)

OpenAIs Reasoning-Dokumentation formuliert dieselbe Regel anders: Gib dem Modell „ein klares
Ziel, starke Randbedingungen und einen expliziten Ausgabe-Vertrag, ohne jeden Zwischenschritt
vorzuschreiben" — und behandle den Denkaufwand „als Stellschraube, nicht als das primäre
Mittel, um Qualität zurückzuholen" (OpenAI, 2026e). **[Hersteller]**

Die unabhängige Evidenz zeigt in dieselbe Richtung. Meincke, Mollick, Mollick und Shapiro
(2025) prüften Chain-of-Thought-Prompting über Modellklassen hinweg und fanden: Bei
Reasoning-Modellen bringt CoT-Prompting „oft nur marginale, wenn überhaupt Zugewinne an
Antwortgenauigkeit", erhöht aber „deutlich die Zeit und die Token, die für eine Antwort nötig
sind". Bei nicht denkenden Modellen half CoT im Mittel leicht — erhöhte aber auch die Streuung
der Antworten und verdarb gelegentlich Fragen, die das Modell zuvor richtig hatte.
**[Preprint]**

Eine generationenübergreifende Studie kommt von anderer Seite zum selben Schluss: Mit besseren
GPT-Modellen sank der Grenznutzen von außen aufgezwungener Denkstruktur — gemessen **−0,8
Prozentpunkte** für GPT-4o auf einem Code-Benchmark und bis zu **−13,8 Punkte** für
Mistral-Large, während dieselben Techniken der Qwen-Familie weiterhin halfen (*Aging of prompt
engineering techniques across LLM versions*, 2026). Der brauchbare Schluss der Autoren:
„Wirksame Prompt-Strategien müssen je Modellfamilie und Generation angepasst und nicht
unverändert übertragen werden." **[Preprint]**

**Was das praktisch heißt.** Lösch Blöcke, die so aussehen:

```text
### CHAIN OF THOUGHTS (DENKPROZESS) ###
1. VERSTEHE den Text
2. IDENTIFIZIERE die Kernaussagen
3. STRUKTURIERE sie nach Themen
4. FORMULIERE die Zusammenfassung
```

Ersetze sie durch das Ziel:

```text
Die Zusammenfassung ist fertig, wenn jemand, der die Aufnahme nicht gehört hat, weiß,
was entschieden wurde, wer bis wann was tut und was offen ist.
```

### 3.2 Sag jede Regel genau einmal

> „Wiederholte Anweisungen wie ‚frag zuerst', ‚nicht verändern' oder ‚auf Freigabe warten'
> können unnötige Freigabeanfragen für sichere, erwartete Aktionen auslösen." (OpenAI, 2026a)

Wiederholung ist keine Betonung. Für das Modell sind dieselbe Regel in drei Formulierungen
**drei Regeln**, die alle zugleich erfüllt sein müssen — und Beinahe-Dubletten wirken wie
weiche Widersprüche. **[Hersteller]**

### 3.3 Widersprüche sind schlimmer als Lücken

> „Schlecht gebaute Prompts mit widersprüchlichen oder vagen Anweisungen können GPT-5 stärker
> schaden als anderen Modellen, weil es Denk-Token darauf verwendet, einen Weg zur Auflösung
> der Widersprüche zu suchen." (OpenAI, 2025a)

Für 5.6 ausdrücklich: „Modelle der GPT-5-Klasse folgen Prompt-Verträgen eng, deshalb können
widersprüchliche Regeln mehr Instabilität erzeugen als fehlende Details" (OpenAI, 2026a).
**[Hersteller]**

Das ist nicht abstrakt. OpenAIs Optimizer-Cookbook arbeitet einen echten Prompt durch, dessen
Widersprüche unter anderem „bevorzuge die Standardbibliothek" neben „nutze externe Pakete, wenn
es einfacher wird" enthielten — und „halte Kommentare minimal" neben „füge kurze Erklärungen
hinzu". Deren Beseitigung hob die gemessene Anweisungstreue von **4,40 auf 4,90 von 5** und
senkte den Spitzenspeicher von 3.626 KB auf 577,5 KB (OpenAI, 2025c). **[Hersteller]**

Typische Widersprüche in einem Zusammenfassungs-Prompt:

| Widerspruch | Warum er schadet |
|---|---|
| „Sei vollständig" + „maximal 150 Wörter" | Das Modell muss raten, was gewinnt. |
| „Nutze die Worte des Sprechers" + „schreibe förmlich" | Diktate sind selten förmlich. |
| „Erfinde nie etwas" + „vervollständige angefangene Gedanken" | Das ist das Gegenteil. |
| „Fasse dich kurz" + 400 Token Liste, was alles hinein soll | Die Liste widerlegt das Adjektiv. |

### 3.4 Entscheidungsregeln statt Absolutheiten

> „Vermeide unnötige absolute Regeln (IMMER, NIE, muss, nur) bei Ermessensfragen; nutze
> stattdessen Entscheidungsregeln." (OpenAI, 2026a) **[Hersteller]**

Eine Absolutheit, die sich nicht immer erfüllen lässt, wird in dem Moment zum Widerspruch, in
dem sie auf einen Fall trifft, der nicht passt. Eine Entscheidungsregel übersteht den Randfall:

- ✗ `Erzeuge IMMER genau fünf Stichpunkte.`
- ✓ `Ein Stichpunkt je eigenständigem Thema. Bei kurzen Aufnahmen genügt einer.`

### 3.5 Sag, was zu tun ist — nicht, was zu lassen

Negative Anweisungen sind schwächer als positive, weil das Unterdrücken eines Begriffs
voraussetzt, ihn erst einmal zu repräsentieren („Denk nicht an einen rosa Elefanten").
Sprachmodelle missdeuten Verneinungen zudem vergleichsweise oft und reagieren empfindlich auf
die Rahmung (Dwivedi et al., 2023). **[Gemischte Evidenz]** Die praktische Empfehlung ist über
die Quellen hinweg einheitlich: Formuliere das gewünschte Verhalten, nicht das verbotene, und
heb dir Verbote für harte Grenzen auf, für die es keine positive Formulierung gibt.

- ✗ `Wiederhole die Abschnitts-Überschrift nicht.`
- ✓ `Beginne mit dem ersten inhaltlichen Satz. Die Überschrift setzt die Anwendung.`

Dieses Beispiel stammt aus unserem eigenen Bestand: Prompts, die ihren eigenen Abschnitt
benannten, erzeugten doppelte Überschriften in Detailansicht, PDF und Mail (siehe `CLAUDE.md`).
Behoben hat es die positive Formulierung.

---

## 4. Was raus soll, was bleiben muss

OpenAIs 5.6-Leitfaden gibt dazu eine ausdrückliche Zweiteilung (OpenAI, 2026a). **[Hersteller]**

**Entfernen:**
- mehrfache Formulierungen derselben Regel
- Stil- oder Prozessanweisungen, die das Verhalten nicht ändern
- Beispiele, die das Ergebnis nicht ändern
- Prozessanweisungen für Verhalten, das das Modell ohnehin zuverlässig zeigt
- Beschreibungen von Werkzeugen, die die Aufgabe gar nicht nutzt

**Behalten:**
- das für den Nutzer sichtbare Ergebnis
- Erfolgskriterien und Abbruchbedingungen
- Sicherheits-, Geschäfts-, Beleg- und Berechtigungsgrenzen
- Weichen, die vom Kontext abhängen
- die verlangte Ausgabeform und Prüfanforderungen

Ein brauchbarer Test für jede Zeile: **Lösch sie, lass dieselben Eingaben laufen und sieh nach,
ob sich die Ausgabe ändert.** Ändert sie sich nicht, war die Zeile Zierrat — und bei einem
Reasoning-Modell ist Zierrat nicht kostenlos, weil er bei **jedem** Aufruf in die Eingabe geht.

---

## 5. Denkaufwand und Ausführlichkeit

GPT-5.6 kennt `reasoning_effort` mit den Stufen `none`, `low`, `medium`, `high`, `xhigh` und
`max` (OpenAI, 2026e). OpenAIs Migrationsrat: Behalte deine bisherige Stufe als Ausgangspunkt
und teste dann **eine Stufe darunter**, denn „GPT-5.6 ist token-effizienter als frühere
Generationen, niedrigere Einstellungen halten die Qualität oft" (OpenRouter, 2026).
**[Hersteller / Dritte]**

Entscheidend: **Bevor** du den Aufwand erhöhst, prüfe, ob dem Prompt Erfolgskriterien, Weichen
oder Prüfschritte fehlen (OpenAI, 2026a). Denkaufwand ersetzt keinen klaren Vertrag.

### 5.1 Was wir gemessen haben

An einem langen deutschen Diktat, mit Johanns echter System-Nachricht und echtem
Zusammenfassungs-Prompt, je ein Aufruf pro Stufe gegen `gpt-5.6-luna` **[Selbst gemessen]**:

| Stufe | Ausgabe-Token | davon Denken | sichtbarer Text |
|---|---|---|---|
| Standard | 888 | 213 | 2.873 Zeichen |
| `low` | **644** (−27 %) | 0 | 2.797 Zeichen |
| `medium` | 808 | 162 | 2.742 Zeichen |
| `high` | 1.335 (+50 %) | 653 | 2.871 Zeichen |

`minimal` lehnen diese Modelle mit HTTP 400 ab.

Zwei Lesarten, beide wichtig:

1. **`high` kostete 50 % mehr und lieferte keinen zusätzlichen Inhalt.** Für Zusammenfassungen
   ist das Verschwendung. Wer die Stufe als „bessere Qualität" vorschlägt, bekommt hier die
   Gegenzahl.
2. **`low` sparte rund ein Viertel der Ausgabe-Token bei praktisch gleicher Textlänge.** Ob es
   genauso *gut* zusammenfasst, kann keine Token-Zählung beantworten — das verlangt einen
   lesenden Vergleich (siehe §10).

Einschränkung: ein Aufruf je Stufe, ein Prompt, ein Diktat. Die Denk-Token schwanken von Lauf
zu Lauf. Die Reihenfolge ist belastbar, die genauen Prozentwerte sind Richtwerte.

### 5.2 Ausführlichkeit

GPT-5.6 „ist von Haus aus knapper als GPT-5.5" (OpenAI, 2026a). Nutze den Parameter
`text.verbosity` als globalen Standard und stell aufgabenspezifische Längenregeln in den Prompt.
Gib konkrete Grenzen an — „3–6 Sätze oder höchstens 5 Stichpunkte" — statt des Adjektivs
„knapp" (OpenAI, 2026c). **[Hersteller]**

---

## 6. Ausgabeform und Markdown

Hier ziehen zwei Befunde in verschiedene Richtungen, und die Auflösung ist wichtig.

**Befund eins:** Formatzwänge verschlechtern das Denken. Tam et al. (2024) fanden „einen
deutlichen Rückgang der Denkfähigkeit unter Formatbeschränkungen" — und je strenger der Zwang,
desto größer der Verlust, mit erzwungenem JSON-Decoding als schlimmstem Fall.
**[Begutachtet]**

**Befund zwei:** Dieselbe Literatur findet, dass *lockere* Formatvorgaben die Leistung in der
Regel verbessern und die Streuung senken — und dass sich die Leistung erholt, wenn freies
Denken der strukturierten Ausgabe vorausgehen darf.

**Auflösung für eine Zusammenfassungs-App:** **Markdown** zu verlangen ist eine *lockere*
Vorgabe für die Gestalt von Fließtext, kein erzwungenes Decodier-Gitter. Das ist unbedenklich
und meist hilfreich. Striktes JSON um denselben Inhalt wäre es nicht. Wenn je maschinenlesbare
Ausgabe gebraucht wird, nimm **Structured Outputs** statt drohender Prompt-Formulierungen: Nur
Structured Outputs garantiert die Schema-Treue tatsächlich und erspart „nachdrücklich
formulierte Prompts, um beständige Formatierung zu erreichen" (OpenAI, 2026f). **[Hersteller]**

Eine Betriebswarnung aus dem GPT-5-Leitfaden: Die Befolgung von Markdown-Anweisungen im
System-Prompt „kann über den Verlauf eines langen Gesprächs nachlassen" (OpenAI, 2025a). Für
unsere Einzelaufrufe gilt das nicht; für chat-förmige Produkte schon.

**Formulierung, die trägt:**

```text
Formatiere die Antwort in Markdown. Nutze `##` für Überschriften, `-` für Listen und
**Fettung** für Begriffe, die hervorstechen sollen. Setze Markdown nur, wo es Bedeutung trägt.
```

---

## 7. Rollen und Personas

Viele ältere Prompts beginnen mit einer Rolle: „Du bist ein hochspezialisierter Experte für …".
Die Belege dafür sind dünner, als die Verbreitung vermuten lässt.

Zheng, Pei, Logeswaran, Lee und Jurgens (2024) prüften **162 Rollen** über **4 Modellfamilien**
und **2.410 Faktenfragen** und fanden, dass „das Hinzufügen von Personas im System-Prompt die
Modellleistung über eine Bandbreite von Fragen hinweg nicht verbessert" — verglichen mit der
Kontrollbedingung ohne Persona. Sie ergänzen, die Wirkung einer einzelnen Persona sei „weitgehend
zufällig", und das automatische Finden einer guten Persona schneide „nicht besser als
Zufallsauswahl" ab. **[Begutachtet, EMNLP Findings]**

Eine spätere Arbeit verfeinert das, statt es umzustoßen: Experten-Personas **helfen** bei
ausrichtungsnahen Aufgaben (Schreiben, Rollenspiel, Ton- und Formattreue, Sicherheitsverweige-
rungen — Zugewinne von +0,40 bis +0,65 auf MT-Bench) und **schaden** bei Wissensabruf
(MMLU 68,0 % gegenüber 71,6 % Ausgangswert; Programmieren −0,65). Kürzere Personas richteten den
geringsten Schaden an (Hu, Rostami & Thomason, 2026). **[Preprint]**

**Was daraus folgt.** Zusammenfassen liegt näher am „Schreiben" als am „Wissensabruf", eine
Persona ist hier also nicht eindeutig schädlich — aber sie leistet auch nicht das, was man ihr
zuschreibt. Wenn eine Rollenzeile bleibt, sollte sie **kurz und funktional** sein, und sie darf
nicht der Ort sein, an dem sich die eigentlichen Anforderungen verstecken.

- ✗ `DU BIST EIN HOCHSPEZIALISIERTER EXPERTE MIT 20 JAHREN ERFAHRUNG IN …`
- ✓ `Du schreibst deutsche Zusammenfassungen diktierter Notizen für Büromitarbeiter.`

Die zweite Fassung ist kürzer, setzt Register und Zielgruppe und behauptet nichts, was das
Modell nicht einlösen kann.

### 7.1 Zum Thema GROSSBUCHSTABEN

Es gibt keinen Beleg, dass Großschreibung die Anweisungstreue verbessert — sie hat aber messbare
Kosten: Durchgehend große Schrift tokenisiert deutlich schlechter. In unserer eigenen
Prompt-Datei tokenisiert die großgeschriebene System-Nachricht mit **3,14 Zeichen je Token**,
gegenüber **4,3** bei normaler deutscher Prosa — rund **37 % mehr Token für dieselben Wörter**
**[Selbst gemessen]**. Da die System-Nachricht bei jedem Aufruf mitgeht, ist das eine
wiederkehrende Rechnung für eine Schreibgewohnheit.

---

## 8. Few-Shot-Beispiele: das ehrliche Bild

Die allgemeine Empfehlung für Reasoning-Modelle lautet: standardmäßig **Zero-Shot**. Sie
„brauchen oft keine Few-Shot-Beispiele für gute Ergebnisse", und mehrere Auswertungen berichten,
dass Few-Shot-Prompting die Leistung von o1-Modellen *senkte*. Die
generationenübergreifende Studie maß Few-Shot mit **−7,4 Prozentpunkten** für das GPT-Paar,
während dieselbe Technik bei Qwen **+7,9 Punkte** brachte (*Aging of prompt engineering
techniques*, 2026). **[Preprint]**

**Unser eigenes Projekt widerspricht dem aber in einem bestimmten Punkt** — und der Widerspruch
ist lehrreich. Aus `CLAUDE.md`:

> „Beispiele schlagen Regeln. ‚Beginne mit einem Verb im Infinitiv' erzeugte kaputtes Deutsch
> (‚verschriftlichen Diktate speichern'); ein Beispiel im Prompt löste es."

Der Widerspruch löst sich auf, sobald man zwei verschiedene Dinge trennt:

| | Aufgabenbeispiele (Few-Shot) | Formmuster |
|---|---|---|
| Zeigen | wie man das Problem löst | wie die Oberfläche aussehen soll |
| Wirkung auf Reasoning-Modelle | neutral bis negativ | positiv, wo eine Regel schwer zu formulieren ist |
| Einsatz für | selten nötig | grammatische Gestalt, Listenstil, Überschriftenstil |

Eine einzelne Zeile, die zeigt, wie ein Aufgabeneintrag aussehen soll, ist ein **Formmuster**.
Es ist billig, es klärt eine sprachliche Frage, die keine Regel sauber ausdrückt, und es schränkt
das Denken des Modells nicht ein. Solche Muster behalten. Durchgerechnete Denkbeispiele
streichen.

---

## 9. Prompts auf Deutsch schreiben

Unsere Prompts sind deutsch und erzeugen Deutsch. Dazu zwei Überlegungen:

1. Englische Prompt-Vorlagen schneiden oft leicht besser ab als übersetzte, weil Englisch das
   Vortraining dominiert; Deutsch gehört jedoch zu den stärkeren nicht-englischen Sprachen, und
   in mindestens einer Auswertung war der Unterschied zwischen vollständig englischen und
   vollständig deutschen Prompts nicht signifikant (mehrsprachige Prompt-Auswertungen, 2024).
   **[Preprint / gemischt]**
2. Sprachen **innerhalb** eines Prompts zu mischen ist die riskantere Wahl — und ein deutscher
   Prompt hat den praktischen Vorteil, dass die Leute, die ihn pflegen, ihn lesen können. Für
   Johann wiegt das Wartungsargument schwerer als eine kleine, unbelegte Qualitätsmarge.

**Die Prompts bleiben deutsch.** Sollte ein bestimmter Abschnitt je messbar schlechter
abschneiden, ist eine Variante mit englischer Anweisung und deutscher Ausgabe ein legitimes
Experiment — aber es muss gemessen und darf nicht angenommen werden.

---

## 10. Woran man erkennt, ob eine Prompt-Änderung geholfen hat

Dieser Abschnitt ist wichtiger als jede Einzelregel, denn Prompt-Arbeit scheitert am häufigsten
beim Auswerten.

**Ausgaben von Sprachmodellen sind nicht reproduzierbar.** API-Modelle sind ausdrücklich nicht
deterministisch; derselbe Prompt kann zweimal Verschiedenes liefern, und Reproduzierbarkeit „im
großen Maßstab ist nahezu unmöglich" (Levy, 2026). **[Begutachtet]**

Daraus folgt: **Ein einzelner Lauf beweist nichts.** Thelwall (2024) sagt es für komplexe
Textaufgaben unverblümt: „Nicht-systematische Experimente mit Variationen der Eingaben oder
Anweisungen sind sinnlos, wenn das Ziel bessere Ergebnisse sind", weil die natürliche Streuung
die Wirkung einer Einzeländerung aus einem Test heraus unmessbar macht. Das empfohlene Mittel
ist Wiederholung und Mittelung — in den zitierten Studien **bis zu 30 Wiederholungen**.
**[Begutachtet]**

Derselbe Autor berichtet ein Ergebnis, das der „schlanker Prompt"-Erzählung direkt
entgegensteht: Im einzigen systematischen Vergleich von System-Prompts für eine komplexe
Textbewertung lieferten **kürzere Anweisungen schlechtere Ergebnisse** (Thelwall, 2024).
**[Begutachtet]**

**Wie beides zusammenpasst.** OpenAIs 5.6-Evidenz betrifft agentische Coding-Prompts voller
Prozessgerüst; Thelwalls Befund betrifft bewertende Prompts, deren Anweisungen die eigentliche
Bewertungsvorschrift tragen. **Gerüst zu kürzen ist gut belegt. Substanz zu kürzen nicht.** Der
Test ist, ob eine Zeile die Ausgabe verändert — nicht, ob sie lang ist.

**Ein praktikables Vorgehen für dieses Projekt:**

1. Leg einen festen Satz von 10–15 repräsentativen Diktaten an, kurze und lange.
2. Ändere **eine** Sache auf einmal.
3. Lass alten und neuen Prompt über den **ganzen** Satz laufen.
4. Lies die Ergebnisse nebeneinander. Für ein Zusammenfassungsprodukt ist das menschliche Lesen
   der maßgebliche Maßstab; ROUGE-artige Kennzahlen belohnen Oberflächenüberlappung und können
   sich **gegenläufig** zur Treue bewegen (siehe §11).
5. Behalte die Änderung nur, wenn sie beim Lesen gewinnt — und halte fest, was du geändert hast
   und warum.

Überspring Schritt 3 nicht, weil das erste Ergebnis gut aussah.

---

## 11. Speziell fürs Zusammenfassen: die Treue-Falle

Ein Befund verdient einen eigenen Abschnitt, weil er der Intuition widerspricht und genau das
betrifft, was diese Anwendung tut.

Über acht Denkstrategien und acht Datensätze hinweg verbesserten explizite Denkstrategien
Flüssigkeit und referenzbasierte Kennzahlen (ROUGE, BERTScore) — **auf Kosten der Treue**. Die
Korrelation zwischen beidem war negativ: **r = −0,685, p = 0,014**. Schlimmer noch: „Die
faktische Treue nimmt beständig ab, je mehr Denkfähigkeit eingesetzt wird" — je mehr internes
Denken, desto mehr füllt das Modell Lücken kreativ. Die Empfehlung der Autoren lautet,
„treue Verdichtung zu priorisieren und die Halluzinationsrisiken kreativen Überdenkens zu
vermeiden" (*Understanding LLM reasoning for abstractive summarization*, 2025). **[Preprint]**

Für ein Diktat-Archiv ist das der Kern der Sache. Eine Zusammenfassung, die schön liest, aber
eine Frist erfindet, ist schlechter als eine schlichte, die das nicht tut. Zwei Konsequenzen:

- Greif **nicht** zu höherem Denkaufwand in der Hoffnung auf bessere Zusammenfassungen. Es kann
  sie unzuverlässiger machen — und wir haben gemessen, dass es zugleich 50 % mehr kostet (§5.1).
- Schreib Treue als **Ergebnis** in den Prompt, nicht als Verbot:
  `Jede Aussage muss sich auf die Aufnahme zurückführen lassen. Was nicht gesagt wurde, bleibt weg.`

---

## 12. Kostenstruktur: was die Rechnung wirklich treibt

**[Selbst gemessen]**, sofern nicht anders belegt.

Denk-Token werden als Ausgabe abgerechnet (OpenAI, 2026e). Damit taugt der Token-Preis schlecht
als Maßstab für die Kosten je Aufgabe. Unsere Messung von 160 Aufrufen über 16 Diktate ergab,
dass `gpt-5-nano` — nominell das billigste Modell — **2.496 Denk-Token verbrannte, um 514
sichtbare zu erzeugen**, und dadurch **je Diktat teurer war als Luna**, bei schlechterer
Qualität.

**Regel: Modelle nach gemessenen Kosten je Aufgabe einordnen, nie nach dem Token-Preis.**

### 12.1 Prompt-Caching — vorhanden, und wir bekommen es nicht

GPT-5.6 cacht wiederverwendbare Prompt-Präfixe: Mindestens **1.024 sichtbare Eingabe-Token**,
**30 Minuten** Lebensdauer, gecachte Token zu **0,1×** des normalen Eingabepreises,
Cache-Schreibvorgänge zu **1,25×** (OpenAI, 2026d). **[Hersteller]** Da unsere System-Nachricht
bei allen sechs Aufrufen je Diktat mitgeht, sieht das nach einer großen, abholbereiten Ersparnis
aus.

Wir haben es geprüft. **[Selbst gemessen]**

| Fall | Prompt-Token | gecacht |
|---|---|---|
| Identischer Prompt, zweiter Aufruf | 2.031 | **2.028** |
| Gleiche System-Nachricht + gleiche Vorlage, anderes Transkript | 1.911 → 1.881 | **0** |
| Gleiche System-Nachricht, andere Vorlage | 1.673 / 2.000 | **0** |
| Identischer Prompt plus vier zusätzliche Token am Ende | 2.035 | **0** |

Nur ein **byte-identischer** Prompt traf den Cache. Vier angehängte Token zerstörten den Treffer,
obwohl rund 2.031 Präfix-Token identisch waren. Der 90-%-Rabatt existiert also — unsere Last
erreicht ihn nur nie, weil jeder Aufruf ein anderes Transkript trägt.

Zwei praktische Folgen:

- Plane keine Einsparungen auf Basis von Caching ein, ohne sie am **eigenen** Aufrufmuster zu
  messen.
- Das Transkript steht bereits am **Ende** jeder Vorlage — genau die richtige Struktur fürs
  Caching (OpenAI, 2026d empfiehlt stabilen Inhalt zuerst, veränderlichen zuletzt). Lass das so:
  Es kostet nichts und ist die Voraussetzung dafür, je davon zu profitieren.

⚠ **Eine Falle beim Ausdünnen.** Unsere System-Nachricht hat **1.008 Token** — allein 16 unter
der Cache-Schwelle von 1.024. Das Präfix je Abschnitt (System + Vorlage) liegt derzeit zwischen
1.057 und 1.449 Token, also darüber. Wer die System-Nachricht kräftig kürzt, drückt die kürzeren
Abschnitte **unter** die Schwelle und nimmt ihnen die Cache-Fähigkeit ganz. Kürze der Klarheit
wegen — und rechne danach nach.

---

## 13. Durchgearbeitetes Beispiel: vorher und nachher

Eine realistische Überarbeitung eines Zusammenfassungs-Prompts in dem Stil, für den dieser
Leitfaden argumentiert.

### Vorher

```text
### ROLLE ###
DU BIST EIN HOCHSPEZIALISIERTER EXPERTE FÜR DIE ANALYSE UND STRUKTURIERUNG GESPROCHENER
TEXTE MIT LANGJÄHRIGER ERFAHRUNG.

### CHAIN OF THOUGHTS ###
1. LIES das Transkript vollständig und erfasse den Kontext
2. IDENTIFIZIERE alle Kernaussagen
3. STRUKTURIERE die Kernaussagen nach Themen
4. FORMULIERE die Zusammenfassung
5. PRÜFE deine Zusammenfassung auf Vollständigkeit

### REGELN ###
- Sei präzise und vollständig
- Fasse dich kurz
- Erfinde NIEMALS etwas
- Schreibe immer auf Deutsch
- Lasse keine wichtigen Informationen weg
- Formuliere knapp
- Verwende eine förmliche Sprache
- Schreibe auf Deutsch

Fasse den folgenden Text zusammen: {transcript}
```

Was daran falsch ist: eine Persona ohne Wirkung (§7) in Großbuchstaben, die 37 % zusätzliche
Token kostet (§7.1); ein Denkprozess-Block, den das Modell nicht braucht und gegen den es
womöglich arbeitet (§3.1); „präzise und vollständig" gegen „kurz" und „knapp" — derselbe
Widerspruch dreimal gesagt (§3.2, §3.3); „immer auf Deutsch" doppelt (§3.2); zwei Verbote, wo
positive Formulierungen klarer wären (§3.5); und **keine Aussage darüber, was eine fertige
Zusammenfassung enthält**.

### Nachher

```text
Du schreibst deutsche Zusammenfassungen diktierter Notizen für Büromitarbeiter.

Eine fertige Zusammenfassung lässt jemanden, der die Aufnahme nicht gehört hat, wissen:
was entschieden wurde, wer bis wann was tut und was offen ist.

- Länge: 3–6 Sätze, oder bis zu 5 Stichpunkte, wenn die Aufnahme mehrere Themen berührt.
- Jede Aussage muss sich auf die Aufnahme zurückführen lassen. Was nicht gesagt wurde, bleibt weg.
- Übernimm die Begriffe des Sprechers für Namen, Projekte und Zahlen.
- Beginne mit dem ersten inhaltlichen Satz; die Überschrift setzt die Anwendung.
- Formatiere in Markdown; Listen nur für tatsächlich gleichrangige Punkte.

Transkript:
{transcript}
```

Jede Regel steht einmal; die Längenvorgabe ist eine Zahl statt eines Adjektivs; die Treue steht
als Ergebnis statt als Verbot; es gibt kein Prozessskript; und das Transkript bleibt zuletzt,
was das cachefähige Präfix maximal hält (§12.1).

**Diese Überarbeitung ist eine Veranschaulichung, keine belegte Verbesserung.** Nach §10 müsste
sie über 10–15 Diktate gemessen werden, bevor sie irgendetwas ersetzt.

---

## 14. Prüfliste

Vor jeder Prompt-Änderung:

- [ ] Sagt der Prompt, was ein fertiges Ergebnis enthält?
- [ ] Sagt er, wann das Modell fertig ist?
- [ ] Steht jede Regel genau einmal?
- [ ] Gibt es zwei Regeln, die nicht beide erfüllbar sind?
- [ ] Werden Absolutheiten (IMMER/NIE) nur für harte Grenzen benutzt?
- [ ] Ist jedes Verbot, das eine positive Anweisung sein könnte, auch eine?
- [ ] Steht ein Prozessskript drin, das das Modell nicht braucht?
- [ ] Sind Längenvorgaben Zahlen statt Adjektive?
- [ ] Steht der veränderliche Teil (Transkript) zuletzt?
- [ ] Würde das Löschen jeder verbleibenden Zeile die Ausgabe verändern?
- [ ] Wurde die Änderung über einen festen Eingabesatz und mehrfach gemessen?

---

## 15. Katalog der Anti-Muster

| Anti-Muster | Warum | Ersetzen durch |
|---|---|---|
| `### CHAIN OF THOUGHTS ###` | Konkurriert mit dem internen Denken | Beschreibung des fertigen Ergebnisses |
| GROSSBUCHSTABEN | Kein Nutzen, ~37 % mehr Token | Normale Schreibweise |
| „präzise und vollständig" + „kurz" | Widerspruch | Eine explizite Längenregel |
| Lange Experten-Persona | Bestenfalls zufällig wirksam | Eine kurze Zeile zu Register und Zielgruppe |
| „Tue X nicht" | Verneinung wirkt schwächer | „Tue stattdessen Y" |
| Dieselbe Regel in drei Formulierungen | Liest sich als drei Regeln | Einmal sagen |
| „knapp", „ausführlich", „gründlich" | Nicht messbar | Satz- oder Stichpunktzahlen |
| `reasoning_effort` erhöhen für Qualität | Kostet mehr, kann Treue senken | Erst den Vertrag klären |
| Durchgerechnete Denkbeispiele | Neutral bis schädlich bei Reasoning-Modellen | Nur Formmuster |
| Eine Änderung an einem Lauf beurteilen | Ausgaben sind nicht deterministisch | Fester Satz, wiederholte Läufe |

---

## Literaturverzeichnis

*Aging of prompt engineering techniques across LLM versions* (2026). arXiv:2608.24641.
https://arxiv.org/html/2608.24641

Dwivedi, Y. K., et al. (2023). *Challenging the appearance of machine intelligence: Cognitive
bias in LLMs and best practices for adoption*. arXiv:2304.01358.
https://arxiv.org/pdf/2304.01358

Hu, Z., Rostami, M., & Thomason, J. (2026). *Expert personas improve LLM alignment but damage
accuracy: Bootstrapping intent-based persona routing with PRISM*. arXiv:2603.18507.
https://arxiv.org/html/2603.18507v1

Levy, B. (2026). Caution ahead: Numerical reasoning and look-ahead bias in AI models. *Journal
of Accounting Research, 64*(3), 1139–1188. https://doi.org/10.1111/1475-679x.70058

Meincke, L., Mollick, E. R., Mollick, L., & Shapiro, D. (2025). *Prompting science report 2:
The decreasing value of chain of thought in prompting*. The Wharton School, University of
Pennsylvania. arXiv:2506.07142. https://arxiv.org/abs/2506.07142

OpenAI. (2025a). *GPT-5 prompting guide*. OpenAI Cookbook.
https://developers.openai.com/cookbook/examples/gpt-5/gpt-5_prompting_guide

OpenAI. (2025b). *GPT-5 troubleshooting guide*. OpenAI Cookbook.
https://developers.openai.com/cookbook/examples/gpt-5/gpt-5_troubleshooting_guide

OpenAI. (2025c). *GPT-5 prompt migration and improvement using the new optimizer*. OpenAI
Cookbook. https://developers.openai.com/cookbook/examples/gpt-5/prompt-optimization-cookbook

OpenAI. (2026a). *Prompt guidance for GPT-5.6*. OpenAI API documentation.
https://developers.openai.com/api/docs/guides/prompt-guidance-gpt-5p6

OpenAI. (2026b). *GPT-5.6: Frontier intelligence that scales with your ambition*.
https://openai.com/index/gpt-5-6/

OpenAI. (2026c). *GPT-5.2 prompting guide*. OpenAI Cookbook.
https://developers.openai.com/cookbook/examples/gpt-5/gpt-5-2_prompting_guide

OpenAI. (2026d). *Prompt caching*. OpenAI API documentation.
https://developers.openai.com/api/docs/guides/prompt-caching

OpenAI. (2026e). *Reasoning*. OpenAI API documentation.
https://developers.openai.com/api/docs/guides/reasoning

OpenAI. (2026f). *Structured outputs*. OpenAI API documentation.
https://developers.openai.com/api/docs/guides/structured-outputs

OpenAI. (2026g). *GPT-5.1 prompting guide*. OpenAI Cookbook.
https://developers.openai.com/cookbook/examples/gpt-5/gpt-5-1_prompting_guide

OpenRouter. (2026). *GPT-5.6 migration guide*.
https://openrouter.ai/docs/cookbook/evaluate-and-optimize/model-migrations/gpt-5-6

Schulhoff, S., Ilie, M., Balepur, N., Kahadze, K., Liu, A., Si, C., … Resnik, P. (2024). *The
prompt report: A systematic survey of prompt engineering techniques*. arXiv:2406.06608.
https://arxiv.org/abs/2406.06608

Tam, Z. R., Wu, C.-K., Tsai, Y.-L., Lin, C.-Y., Lee, H., & Chen, Y.-N. (2024). *Let me speak
freely? A study on the impact of format restrictions on performance of large language models*.
arXiv:2408.02442. https://arxiv.org/abs/2408.02442

Thelwall, M. (2024). ChatGPT for complex text evaluation tasks. *Journal of the Association for
Information Science and Technology, 76*(4), 645–648. https://doi.org/10.1002/asi.24966

*Understanding LLM reasoning for abstractive summarization* (2025). arXiv:2512.03503.
https://arxiv.org/html/2512.03503v1

Zheng, M., Pei, J., Logeswaran, L., Lee, M., & Jurgens, D. (2024). When "a helpful assistant"
is not really helpful: Personas in system prompts do not improve performances of large language
models. In *Findings of the Association for Computational Linguistics: EMNLP 2024*.
https://aclanthology.org/2024.findings-emnlp.888/

### Eigene Messungen

Alle mit **[Selbst gemessen]** gekennzeichneten Angaben stammen aus Messungen gegen die
Live-API von OpenAI am 11.09.2026, mit Platé.Johanns echter Prompt-Datei und 16 erfundenen
Diktaten (20 s bis 6 min Sprechzeit). 160 Aufrufe für das Kostenmodell, dazu getrennte Läufe
für Denkaufwand und Prompt-Caching. Token lokal mit `tiktoken` (`o200k_base`) gezählt. Es
wurden keine echten Diktatinhalte an die API gesendet. Die Rohwerte sind in Issue #71
festgehalten.
