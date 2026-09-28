# Prompts für GPT-5.6 schreiben — ein belegter Praxisleitfaden

**Für Wissensarbeit in Markdown und Obsidian-Vaults**
**Fassung 1.0 · 12.09.2026**

Dieser Leitfaden erklärt, wie man Prompts für die GPT-5.6-Familie (Luna, Terra, Sol) schreibt,
wenn das Material in Markdown-Dateien liegt — Besprechungsnotizen, Projektnotizen, Recherche,
ein persönlicher oder geteilter Vault. Er ist so geschrieben, dass ihn auch jemand befolgen kann,
der noch nie bewusst einen Prompt abgestimmt hat, und so, dass jemand, der das schon getan hat,
die Begründung nachprüfen kann.

Jede nicht offensichtliche Aussage trägt einen Beleg. Das Literaturverzeichnis folgt APA 7.

Drei Arten von Belegen kommen vor, bewusst auseinandergehalten:

| Kennzeichnung | Bedeutung |
|---|---|
| **[Hersteller]** | OpenAIs eigene Dokumentation. Maßgeblich fürs Modellverhalten, aber nicht unabhängig. |
| **[Begutachtet / Preprint]** | Wissenschaftliche Arbeiten. Unabhängig, aber meist an älteren Modellen als 5.6 geprüft. |
| **[Gemessen]** | Messungen gegen die Live-API am 11.09.2026 in einer deutschsprachigen produktiven Zusammenfassungs-Anwendung. Gilt für diese Last; als Richtwert zu lesen. |

Wo sich diese widersprechen, sagt der Leitfaden das — statt die glatteste Antwort zu wählen.

---

## 1. Die Kurzfassung in einem Absatz

Sag dem Modell, **wie ein gutes Ergebnis aussieht** und **wann es fertig ist** — und dann geh
aus dem Weg. Schreib ihm nicht die Schritte vor. Sag jede Regel **genau einmal**. Entferne alles,
was die Ausgabe nicht verändert. Widersprüche kosten mehr als Lücken, weil das Modell
abrechenbare Denk-Token verbraucht, um sie aufzulösen (OpenAI, 2025a).

Alles Weitere ist die Langfassung dieses Absatzes — samt der Stellen, an denen er nicht ganz
stimmt.

---

## 2. Was GPT-5.6 ist, und warum ältere Prompts danebengreifen

GPT-5.6 gibt es in drei Größen: **Luna** (am schnellsten und günstigsten), **Terra**
(ausgewogen) und **Sol** (am leistungsfähigsten) (OpenAI, 2026b). Alle drei sind
*Reasoning-Modelle*: Bevor sie eine sichtbare Antwort schreiben, erzeugen sie verborgene
Denk-Token, die als Ausgabe-Token abgerechnet werden, obwohl man sie nie sieht (OpenAI, 2026e).

Diese eine Tatsache entwertet einen großen Teil der Prompt-Folklore von vor 2025. Prompts für
GPT-4-Modelle tragen typischerweise ein Gerüst, dessen Aufgabe es war, ein nicht denkendes Modell
zum Denken zu bringen: „denke Schritt für Schritt", nummerierte Denkprozesse, durchgerechnete
Beispiele. GPT-5.6 tut das bereits intern. Das Gerüst erkauft kein Denken mehr — es konkurriert
damit.

OpenAIs Leitfaden zu 5.6 sagt ausdrücklich, dass die Korrektur im **Weglassen** besteht: Prompts
sollen „Ergebnisse, Randbedingungen, Belege und Abschlusskriterien definieren und das Modell dann
einen effizienten Weg wählen lassen"; der Leitfaden „betont das Entfernen von Gerüst statt das
Hinzufügen von Details" (OpenAI, 2026a). **[Hersteller]**

### 2.1 Was bringt das Ausdünnen?

In OpenAIs internen Evaluationen erzielten schlankere System-Prompts rund **10–15 % höhere
Bewertungen** bei **41–66 % weniger Token** und **33–67 % geringeren Kosten** (OpenAI, 2026a).
Dasselbe Dokument schränkt ein: *„Die Ergebnisse schwanken je nach Last, behandeln Sie diese
Spannen als Richtungsangabe."* **[Hersteller]**

Diese Zahlen sind, was sie sind: ein internes Herstellerergebnis an Coding-Agenten. Sie sind das
beste verfügbare Signal speziell zu 5.6 — aber kein kontrollierter öffentlicher Benchmark.

---

## 3. Die fünf Grundsätze

### 3.1 Beschreibe das Ziel, nicht den Weg

> „Beschreibe das Ziel, statt jeden Schritt vorzuschreiben." (OpenAI, 2026a)

OpenAIs Reasoning-Dokumentation formuliert dasselbe anders: Gib dem Modell „ein klares Ziel,
starke Randbedingungen und einen expliziten Ausgabe-Vertrag, ohne jeden Zwischenschritt
vorzuschreiben" — und behandle den Denkaufwand „als Stellschraube, nicht als das primäre Mittel,
um Qualität zurückzuholen" (OpenAI, 2026e). **[Hersteller]**

Die unabhängige Evidenz stimmt zu. Meincke, Mollick, Mollick und Shapiro (2025) prüften
Chain-of-Thought-Prompting über Modellklassen hinweg: Bei Reasoning-Modellen bringt es „oft nur
marginale, wenn überhaupt Zugewinne an Antwortgenauigkeit", erhöht aber „deutlich die Zeit und
die Token". Bei nicht denkenden Modellen half es im Mittel leicht — erhöhte aber die Streuung und
verdarb gelegentlich Fragen, die zuvor richtig waren. **[Preprint]**

Eine generationenübergreifende Studie kommt zum selben Schluss: Mit besseren GPT-Modellen sank
der Grenznutzen von außen aufgezwungener Denkstruktur — gemessen **−0,8 Prozentpunkte** für
GPT-4o und bis zu **−13,8 Punkte** für Mistral-Large, während dieselben Techniken der
Qwen-Familie weiterhin halfen (*Aging of prompt engineering techniques across LLM versions*,
2026). Der brauchbare Schluss: „Wirksame Prompt-Strategien müssen je Modellfamilie und Generation
angepasst und nicht unverändert übertragen werden." **[Preprint]**

**Praktisch.** Lösch Blöcke wie diesen:

```text
### DENKPROZESS ###
1. LIES die Notiz vollständig
2. IDENTIFIZIERE die Kernaussagen
3. GRUPPIERE sie nach Themen
4. SCHREIBE die Zusammenfassung
```

Ersetze sie durch das Ziel:

```text
Die Zusammenfassung ist fertig, wenn ein Kollege, der nicht dabei war, weiß, was entschieden
wurde, wer bis wann was tut und was offen ist.
```

### 3.2 Sag jede Regel genau einmal

> „Wiederholte Anweisungen wie ‚frag zuerst', ‚nicht verändern' oder ‚auf Freigabe warten' können
> unnötige Freigabeanfragen für sichere, erwartete Aktionen auslösen." (OpenAI, 2026a)

Wiederholung ist keine Betonung. Für das Modell sind dieselbe Regel in drei Formulierungen **drei
Regeln**, die alle zugleich erfüllt sein müssen — und Beinahe-Dubletten wirken wie weiche
Widersprüche. **[Hersteller]**

**Eine Falle speziell für Vaults:** Wer einen System-Prompt in einer Notiz und Aufgaben-Prompts in
anderen pflegt, hat dieselbe Regel schnell in beiden. Wer nur eine Datei liest, merkt es nicht.
In einem gemessenen Fall verbesserte das Entfernen eines doppelten Anweisungsblocks die Treue und
senkte zugleich die Eingabe-Token um 24 % — die verbliebene Kopie im Aufgaben-Prompt war die
ausführlichere **[Gemessen]**.

### 3.3 Widersprüche sind schlimmer als Lücken

> „Schlecht gebaute Prompts mit widersprüchlichen oder vagen Anweisungen können GPT-5 stärker
> schaden als anderen Modellen, weil es Denk-Token darauf verwendet, einen Weg zur Auflösung der
> Widersprüche zu suchen." (OpenAI, 2025a)

Für 5.6: „Modelle der GPT-5-Klasse folgen Prompt-Verträgen eng, deshalb können widersprüchliche
Regeln mehr Instabilität erzeugen als fehlende Details" (OpenAI, 2026a). **[Hersteller]**

OpenAIs Optimizer-Cookbook arbeitet einen echten Prompt durch, dessen Widersprüche unter anderem
„bevorzuge die Standardbibliothek" neben „nutze externe Pakete, wenn es einfacher wird" und
„halte Kommentare minimal" neben „füge kurze Erklärungen hinzu" enthielten. Deren Beseitigung hob
die gemessene Anweisungstreue von **4,40 auf 4,90 von 5** (OpenAI, 2025c). **[Hersteller]**

Typische Widersprüche in Notiz-Prompts:

| Widerspruch | Warum er schadet |
|---|---|
| „Sei vollständig" + „maximal 150 Wörter" | Das Modell muss raten, was gewinnt. |
| „Nutze die Worte des Autors" + „schreibe förmlich" | Notizen sind selten förmlich. |
| „Erfinde nie etwas" + „vervollständige angefangene Gedanken" | Das ist das Gegenteil. |
| „Fasse dich kurz" + lange Liste, was hineingehört | Die Liste widerlegt das Adjektiv. |
| „Erhalte alle Links" + „entferne Irrelevantes" | Was gilt, wenn ein Link in einer gestrichenen Passage steht? |

> ⚠ **Eine ehrliche Einschränkung.** In einem kontrollierten Test an einem
> Zusammenfassungs-Prompt brachte das Auflösen zweier echter Widersprüche **keine messbare
> Qualitätsänderung** (4 Elemente besser, 6 schlechter, p = 0,754) **[Gemessen]**. Widersprüche
> gehören trotzdem beseitigt — vor allem, weil sie einen Prompt für **Menschen** unwartbar machen
> — aber erwarte davon allein keinen Qualitätssprung.

### 3.4 Entscheidungsregeln statt Absolutheiten

> „Vermeide unnötige absolute Regeln (IMMER, NIE, muss, nur) bei Ermessensfragen; nutze
> stattdessen Entscheidungsregeln." (OpenAI, 2026a) **[Hersteller]**

Eine Absolutheit, die sich nicht immer erfüllen lässt, wird zum Widerspruch, sobald sie auf einen
Fall trifft, der nicht passt.

- ✗ `Erzeuge IMMER genau fünf Stichpunkte.`
- ✓ `Ein Stichpunkt je eigenständigem Thema. Eine kurze Notiz braucht vielleicht nur einen.`

### 3.5 Sag, was zu tun ist — nicht, was zu lassen

Negative Anweisungen sind schwächer als positive: Etwas zu unterdrücken setzt voraus, es erst zu
repräsentieren. Sprachmodelle missdeuten Verneinungen zudem vergleichsweise oft und reagieren
empfindlich auf die Rahmung (Dwivedi et al., 2023). **[Gemischte Evidenz]** Formuliere das
gewünschte Verhalten; heb Verbote für harte Grenzen auf, für die es keine positive Formulierung
gibt.

- ✗ `Wiederhole den Titel der Notiz nicht.`
- ✓ `Beginne mit dem ersten inhaltlichen Satz. Der Titel steht bereits in der Datei.`

---

## 4. Was raus soll, was bleiben muss

OpenAIs 5.6-Leitfaden gibt dazu eine ausdrückliche Zweiteilung (OpenAI, 2026a). **[Hersteller]**

**Entfernen:**
- mehrfache Formulierungen derselben Regel
- Stil- oder Prozessanweisungen, die das Verhalten nicht ändern
- Beispiele, die das Ergebnis nicht ändern
- Prozessanweisungen für Verhalten, das das Modell ohnehin zuverlässig zeigt
- Beschreibungen von Werkzeugen oder Dateien, die die Aufgabe nicht nutzt

**Behalten:**
- das sichtbare Ergebnis
- Erfolgskriterien und Abbruchbedingungen
- Sicherheits-, Geschäfts-, Beleg- und Berechtigungsgrenzen
- Weichen, die vom Kontext abhängen
- die verlangte Ausgabeform und Prüfanforderungen

Ein brauchbarer Test für jede Zeile: **Lösch sie, lass dieselben Eingaben laufen, sieh nach, ob
sich die Ausgabe ändert.** Ändert sie sich nicht, war die Zeile Zierrat — und Zierrat ist nicht
kostenlos, weil er bei **jedem** Aufruf in die Eingabe geht.

---

## 5. Denkaufwand und Ausführlichkeit

GPT-5.6 kennt `reasoning_effort` mit den Stufen `none`, `low`, `medium`, `high`, `xhigh` und
`max` (OpenAI, 2026e). OpenAIs Migrationsrat: bisherige Stufe als Ausgangspunkt behalten, dann
**eine Stufe darunter** testen, denn „GPT-5.6 ist token-effizienter als frühere Generationen,
niedrigere Einstellungen halten die Qualität oft" (OpenRouter, 2026).
**[Hersteller / Dritte]**

Entscheidend: **Bevor** du den Aufwand erhöhst, prüfe, ob dem Prompt Erfolgskriterien, Weichen
oder Prüfschritte fehlen (OpenAI, 2026a). Denkaufwand ersetzt keinen klaren Vertrag.

### 5.1 Gemessene Wirkung an einer Zusammenfassungs-Aufgabe

Je ein Aufruf pro Stufe gegen `gpt-5.6-luna`, langer deutscher Ausgangstext **[Gemessen]**:

| Stufe | Ausgabe-Token | davon Denken | sichtbarer Text |
|---|---|---|---|
| Standard | 888 | 213 | 2.873 Zeichen |
| `low` | **644** (−27 %) | 0 | 2.797 Zeichen |
| `medium` | 808 | 162 | 2.742 Zeichen |
| `high` | 1.335 (+50 %) | 653 | 2.871 Zeichen |

`minimal` lehnen diese Modelle mit HTTP 400 ab.

Zwei Lesarten:

1. **`high` kostete 50 % mehr und lieferte keinen zusätzlichen Inhalt.** Fürs Zusammenfassen und
   Umschreiben ist das Verschwendung. Wer die Stufe als „bessere Qualität" vorschlägt, bekommt
   hier die Gegenzahl.
2. **`low` sparte rund ein Viertel bei praktisch gleicher Textlänge.** Ob es genauso *gut*
   arbeitet, kann keine Token-Zählung beantworten — das verlangt einen lesenden Vergleich (§10).

Einschränkung: ein Aufruf je Stufe, ein Prompt, ein Dokument. Die Reihenfolge ist belastbar, die
Prozentwerte sind Richtwerte.

### 5.2 Ausführlichkeit

GPT-5.6 „ist von Haus aus knapper als GPT-5.5" (OpenAI, 2026a). Nutze `text.verbosity` als
globalen Standard und stell aufgabenspezifische Längenregeln in den Prompt. Gib konkrete Grenzen
an — „3–6 Sätze oder höchstens 5 Stichpunkte" — statt des Adjektivs „knapp" (OpenAI, 2026c).
**[Hersteller]**

---

## 6. Ausgabeform: Markdown ist die sichere Wahl

Zwei Befunde ziehen in verschiedene Richtungen, und die Auflösung ist für Vault-Arbeit wichtig.

**Befund eins:** Formatzwänge verschlechtern das Denken. Tam et al. (2024) fanden „einen
deutlichen Rückgang der Denkfähigkeit unter Formatbeschränkungen" — je strenger der Zwang, desto
größer der Verlust, mit erzwungenem JSON-Decoding als schlimmstem Fall. **[Begutachtet]**

**Befund zwei:** Dieselbe Literatur findet, dass *lockere* Formatvorgaben die Leistung in der
Regel verbessern und die Streuung senken.

**Auflösung:** **Markdown** zu verlangen ist eine lockere Vorgabe für die Gestalt von Fließtext,
kein Decodier-Gitter. Das ist unbedenklich und meist hilfreich — praktischerweise ist Markdown
ohnehin das, was ein Vault speichert. Striktes JSON um denselben Inhalt wäre es nicht. Wenn du
maschinenlesbare Ausgabe brauchst (für ein Skript, das Notizen ablegt), nimm **Structured
Outputs** statt nachdrücklicher Prompt-Formulierungen: Nur Structured Outputs garantiert die
Schema-Treue tatsächlich und erspart „nachdrücklich formulierte Prompts, um beständige
Formatierung zu erreichen" (OpenAI, 2026f). **[Hersteller]**

Eine Betriebswarnung: Die Befolgung von Markdown-Anweisungen im System-Prompt „kann über den
Verlauf eines langen Gesprächs nachlassen" (OpenAI, 2025a). Für Einzelverarbeitung gilt das
nicht; in einem langen Chat die Formatregel alle paar Züge wiederholen.

**Formulierung, die trägt:**

```text
Formatiere die Antwort in Markdown. Nutze `##` für Überschriften, `-` für Listen und
**Fettung** für Begriffe, die hervorstechen sollen. Setze Markdown nur, wo es Bedeutung trägt.
```

---

## 7. Arbeiten mit Vault-Dateien

Zwei begutachtete Befunde tragen diesen Abschnitt, und beide betreffen genau die Lage, die ein
Vault erzeugt: lange Eingaben, zusammengesetzt aus mehreren Dateien.

**Die Position zählt mehr, als man denkt.** Liu et al. (2023) untersuchten
Mehrdokumenten-Fragebeantwortung und Schlüssel-Wert-Abruf und fanden, dass „die Leistung oft dann
am höchsten ist, wenn die relevante Information am **Anfang oder Ende** des Eingabekontexts
steht, und deutlich abfällt, wenn das Modell auf Information in der **Mitte** langer Kontexte
zugreifen muss — **selbst bei ausdrücklich langkontextfähigen Modellen**". Die Leistung sinke
zudem „erheblich, je länger der Eingabekontext wird". **[Begutachtet, TACL]**

**Dem Modell zu sagen, es solle nur aus dem gelieferten Material antworten, wirkt — und zwar
messbar.** Addlesee (2024) baute ein Frage-Antwort-Korpus mit Material, das die Modelle im
Vortraining nicht gesehen haben konnten, und zeigte, dass ein auf Erdung ausgerichteter Prompt
die Antwortgenauigkeit um **bis zu 28 Prozentpunkte (im Mittel 12)** verbesserte — quer über
Medizin und Finanzen, indem er den Konflikt zwischen Prompt-Wissen und statischem Vortrainings-
wissen verringerte. **[Begutachtet]**

Der Rest dieses Abschnitts ist die praktische Folge dieser beiden Ergebnisse.

### 7.1 Frontmatter und Links ausdrücklich schützen

Ein Modell, das eine Notiz umschreibt, formatiert bereitwillig das YAML-Frontmatter um,
nummeriert Listen neu oder „glättet" Wikilinks zu reinem Text. Geht die Datei zurück in den
Vault, zerbricht das Abfragen, Dataview-Tabellen und den Graphen.

Formuliere es als Ergebnis, nicht als Verbot:

```text
Das YAML-Frontmatter zwischen den `---`-Zeilen wird unverändert zurückgegeben, Zeichen für
Zeichen. Wikilinks bleiben in ihrer Originalform: [[Notizname]] und [[Notizname|Alias]].
Tags behalten ihr `#`-Präfix.
```

⚠ **Lass dir niemals Links erfinden.** Das Modell erzeugt `[[Notizen, die es nicht gibt]]`, die
plausibel aussehen und verwaiste Einträge im Graphen anlegen. Wenn Verlinkung gewünscht ist, gib
die Liste der vorhandenen Notiztitel mit und sag: *„Verlinke nur auf Titel aus dieser Liste;
passt keiner, schreib reinen Text."*

### 7.2 Die Notiz ans Ende

Zwei unabhängige Gründe, den veränderlichen Inhalt ans **Ende** zu stellen:

1. OpenAIs Prompt-Dokumentation empfiehlt, Kontext eher ans Ende als an den Anfang zu setzen —
   wegen Kosten und Antwortzeit (OpenAI, 2026h). **[Hersteller]**
2. Der Positionseffekt bei Liu et al. (2023) zeigt in dieselbe Richtung: Das Ende ist eine der
   beiden **starken** Positionen. **[Begutachtet]**
3. Prompt-Caching nutzt ein *Präfix*. Stabile Anweisungen zuerst, wechselnder Inhalt zuletzt ist
   die Struktur, die überhaupt je profitieren kann (OpenAI, 2026d) **[Hersteller]** — was
   Caching tatsächlich liefert, steht in §9.2.

```text
[stabile Anweisungen]
[stabiler Ausgabe-Vertrag]

NOTIZ:
{{content}}
```

### 7.3 Mehrere Notizen als Kontext: beschriften und ordnen

Wenn du mehrere Notizen einfügst, kann das Modell nicht erkennen, wo eine endet und die nächste
beginnt, solange du es nicht sagst. Nutze ausdrückliche Trenner und sag, was damit zu tun ist:

```text
Unten stehen mehrere Notizen, jede eingeleitet durch eine Zeile `### DATEI: <Pfad>`.
Antworte ausschließlich aus diesen Notizen. Wenn du eine Aussage verwendest, nenne die Datei,
aus der sie stammt.
Enthalten die Notizen die Antwort nicht, sag das, statt sie zu erschließen.
```

Der letzte Satz ist der wichtige. Ohne ausdrückliche Ausstiegsmöglichkeit erzeugt ein Modell,
dessen Kontext die Frage nicht beantwortet, trotzdem eine plausible Antwort — und genau solche
Erdungsanweisungen hoben die Genauigkeit um bis zu 28 Prozentpunkte (Addlesee, 2024).
**[Begutachtet]**

⚠ **Ordne die Dateien bewusst.** Weil die Genauigkeit am Anfang und Ende eines langen Kontexts
am höchsten und in der Mitte am niedrigsten ist (Liu et al., 2023), sollte die Notiz, die die
Antwort am wahrscheinlichsten enthält, nicht zwischen zwanzig anderen in der Mitte stehen. Wenn
du sie nicht ordnen kannst, nimm lieber **weniger** Dateien: Die Genauigkeit sinkt „erheblich, je
länger der Eingabekontext wird" — den ganzen Vault einzufügen ist schlechter, als die fünf
relevanten Notizen einzufügen. **[Begutachtet]**

### 7.4 Vorlagen: ein Vertrag je Aufgabe

Eine Obsidian-Vorlage, die ein Modell aufruft, sollte **eine** Aufgabe tragen. Eine Vorlage, die
zusammenfasst *und* Aufgaben extrahiert *und* Tags vorschlägt, sind drei Verträge in einem
Prompt, und ihre Längenregeln widersprechen sich (§3.3). Drei kleine Vorlagen schlagen eine
große — und jede lässt sich einzeln messen (§10).

---

## 8. Rollen, Großschreibung und Beispiele

### 8.1 Personas leisten weniger, als man denkt

Zheng, Pei, Logeswaran, Lee und Jurgens (2024) prüften **162 Rollen** über **4 Modellfamilien**
und **2.410 Faktenfragen** und fanden, dass „das Hinzufügen von Personas im System-Prompt die
Modellleistung nicht verbessert" — verglichen mit der Kontrollbedingung ohne Persona. Die Wirkung
einer einzelnen Persona sei „weitgehend zufällig", und das automatische Finden einer guten
schneide „nicht besser als Zufallsauswahl" ab. **[Begutachtet, EMNLP Findings]**

Eine spätere Arbeit verfeinert das: Experten-Personas **helfen** bei ausrichtungsnahen Aufgaben
(Schreiben, Ton- und Formattreue — +0,40 bis +0,65 auf MT-Bench) und **schaden** bei Wissensabruf
(MMLU 68,0 % gegenüber 71,6 %). Kürzere Personas richteten den geringsten Schaden an (Hu, Rostami
& Thomason, 2026). **[Preprint]**

**Was folgt.** Notizen umzuschreiben und zusammenzufassen liegt näher am „Schreiben" als am
„Wissensabruf", eine kurze Rollenzeile ist also nicht schädlich — sie leistet nur nicht das, was
man ihr zuschreibt. Kurz und funktional halten, und niemals die eigentlichen Anforderungen darin
verstecken.

- ✗ `DU BIST EIN HOCHSPEZIALISIERTER EXPERTE MIT 20 JAHREN ERFAHRUNG IN …`
- ✓ `Du schreibst Besprechungsnotizen für Kollegen um, die nicht dabei waren.`

### 8.2 Zum Thema GROSSSCHREIBUNG

Es gibt keinen Beleg, dass Versalien die Anweisungstreue verbessern — sie haben aber messbare
Kosten: Durchgehend große Schrift tokenisiert deutlich schlechter. In einem gemessenen deutschen
Prompt tokenisierte großgeschriebener Text mit **3,14 Zeichen je Token** gegenüber **4,3** bei
normaler deutscher Prosa — rund **37 % mehr Token für dieselben Wörter** **[Gemessen]**. Steht
dieser Text in einem System-Prompt, der bei jedem Aufruf mitgeht, ist das eine wiederkehrende
Rechnung für eine Schreibgewohnheit.

### 8.3 Beispiele: welche Sorte, und wann

Die allgemeine Empfehlung für Reasoning-Modelle lautet **Zero-Shot** als Standard: Sie „brauchen
oft keine Few-Shot-Beispiele für gute Ergebnisse", und mehrere Auswertungen berichten, dass
Few-Shot-Prompting die Leistung von o1-Modellen *senkte*. Die generationenübergreifende Studie maß
Few-Shot mit **−7,4 Prozentpunkten** für das GPT-Paar, während dieselbe Technik bei Qwen **+7,9
Punkte** brachte (*Aging of prompt engineering techniques*, 2026). **[Preprint]**

Es werden aber zwei verschiedene Dinge „Beispiele" genannt, und nur von einem wird abgeraten:

| | Aufgabenbeispiele (Few-Shot) | Formmuster |
|---|---|---|
| Zeigen | wie man das Problem löst | wie die Oberfläche aussehen soll |
| Wirkung auf Reasoning-Modelle | neutral bis negativ | positiv, wo eine Regel schwer zu fassen ist |
| Einsatz für | selten nötig | Listenstil, Überschriftenstil, eine heikle grammatische Form |

Eine einzelne Zeile, die zeigt, wie **ein** Ausgabeelement aussehen soll, ist ein **Formmuster**.
Es ist billig und klärt Fragen, die keine Regel sauber ausdrückt. Solche behalten; durchgerechnete
Denkbeispiele streichen.

**Praxisfall:** Die Regel „beginne jede Aufgabe mit einem Verb im Infinitiv" erzeugte kaputtes
Deutsch; **ein** Beispiel — `PDF je Sprachnachricht erzeugen` — löste es sofort, auch auf einem
schwächeren Modell **[Gemessen]**.

---

## 9. Kosten: was die Rechnung wirklich treibt

### 9.1 Pro Token ist nicht pro Aufgabe

Denk-Token werden als Ausgabe abgerechnet (OpenAI, 2026e). Damit taugt der Token-Preis schlecht
als Maßstab für die Kosten einer Aufgabe.

Eine Messung über 160 Aufrufe und 16 Dokumente ergab, dass das nominell **billigste** Modell eines
Katalogs **2.496 Denk-Token verbrannte, um 514 sichtbare zu erzeugen**, und dadurch **je Aufgabe
teurer** war als ein neueres, nominell teureres Modell — bei schlechterer Qualität **[Gemessen]**.

**Regel: Modelle nach gemessenen Kosten je Aufgabe einordnen, nie nach dem Token-Preis.**

### 9.2 Prompt-Caching gibt es; du bekommst es vielleicht nicht

GPT-5.6 cacht wiederverwendbare Präfixe: mindestens **1.024 sichtbare Eingabe-Token**,
**30 Minuten** Lebensdauer, gecachte Token zu **0,1×** des normalen Eingabepreises,
Schreibvorgänge zu **1,25×** (OpenAI, 2026d). **[Hersteller]** Bei einem langen, stabilen
System-Prompt sieht das nach großer Ersparnis aus.

Es wurde geprüft **[Gemessen]**:

| Fall | Prompt-Token | gecacht |
|---|---|---|
| Identischer Prompt, zweiter Aufruf | 2.031 | **2.028** |
| Gleiche Anweisungen, anderes Dokument | 1.911 → 1.881 | **0** |
| Identischer Prompt plus vier Token am Ende | 2.035 | **0** |

Nur ein **byte-identischer** Prompt traf den Cache. Vier angehängte Token zerstörten den Treffer
trotz rund 2.031 identischer Präfix-Token.

**Folgen für Vault-Arbeit:**

- Plane keine Ersparnis über Caching ein, ohne sie am eigenen Aufrufmuster zu messen.
- Halte stabile Anweisungen zuerst und die Notiz zuletzt trotzdem ein (§7.2): Es kostet nichts und
  ist die Voraussetzung, je davon zu profitieren.
- Wenn du **dieselbe** Notiz wiederholt verarbeitest — etwa beim Feilen an einem Prompt — hilft
  Caching sehr wohl, weil der Prompt dann tatsächlich identisch ist.

---

## 10. Woran man erkennt, ob eine Änderung geholfen hat

Dieser Abschnitt ist wichtiger als jede Einzelregel, denn Prompt-Arbeit scheitert am häufigsten
beim Auswerten.

**Ausgaben sind nicht reproduzierbar.** API-Modelle sind ausdrücklich nicht deterministisch, und
Reproduzierbarkeit „im großen Maßstab ist nahezu unmöglich" (Levy, 2026). **[Begutachtet]**

Daraus folgt: **Ein einzelner Lauf beweist nichts.** Thelwall (2024) sagt es unverblümt:
„Nicht-systematische Experimente mit Variationen der Eingaben oder Anweisungen sind sinnlos, wenn
das Ziel bessere Ergebnisse sind", weil die natürliche Streuung die Wirkung einer Einzeländerung
aus einem Test heraus unmessbar macht. Das Mittel ist Wiederholung und Mittelung — in den
zitierten Studien **bis zu 30 Wiederholungen**. **[Begutachtet]**

Derselbe Autor berichtet ein Ergebnis, das der „schlanker Prompt"-Erzählung entgegensteht: Im
einzigen systematischen Vergleich von System-Prompts für eine komplexe Textbewertung lieferten
**kürzere Anweisungen schlechtere Ergebnisse** (Thelwall, 2024). **[Begutachtet]**

**Wie beides zusammenpasst.** OpenAIs 5.6-Evidenz betrifft agentische Prompts voller
Prozessgerüst; Thelwalls Befund betrifft bewertende Prompts, deren Anweisungen die eigentliche
Bewertungsvorschrift tragen. **Gerüst zu kürzen ist gut belegt. Substanz zu kürzen nicht.** Der
Test ist, ob eine Zeile die Ausgabe verändert — nicht, ob sie lang ist.

### Ein praktikables Vorgehen

1. Leg einen festen Satz von **10–15 repräsentativen Notizen** an — kurz und lang, ordentlich und
   chaotisch. Verändere sie nicht; sie sind dein Maßstab.
2. Ändere **eine** Sache auf einmal.
3. Lass alten und neuen Prompt über den **ganzen** Satz laufen, **je dreimal**.
4. Prüfe zuerst automatisch das objektiv Entscheidbare: Frontmatter erhalten? Längengrenze
   eingehalten? Keine erfundenen Links? Richtige Sprache?
5. Lies die Überlebenden nebeneinander. Bei Schreibaufgaben ist menschliches Lesen der Maßstab.
6. Behalte die Änderung nur, wenn sie beim Lesen gewinnt — und halte fest, was du geändert hast
   und warum.

> **Eine Warnung aus der Praxis.** In einer Auswertung meldete eine automatische Sprachprüfung,
> eine Variante habe dreimal weniger Verstöße als eine andere. **Alle elf gemeldeten Fälle waren
> Fehlalarme** — kurze, völlig korrekte Sätze, die zufällig keines der Stoppwörter des Prüfers
> enthielten. Der vermeintliche Befund verschwand, sobald der Prüfer korrigiert war
> **[Gemessen]**. **Prüfe dein Messinstrument, bevor du seinem Urteil traust.**

---

## 11. Durchgearbeitetes Beispiel: vorher und nachher

Ein Prompt, der eine Besprechungsnotiz im Vault zusammenfasst.

### Vorher

```text
### ROLLE ###
DU BIST EIN HOCHSPEZIALISIERTER EXPERTE FÜR DIE ANALYSE UND STRUKTURIERUNG VON
BESPRECHUNGSNOTIZEN MIT LANGJÄHRIGER ERFAHRUNG.

### DENKPROZESS ###
1. LIES die Notiz vollständig und erfasse den Kontext
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
- Ändere die Formatierung nicht
- Schreibe auf Deutsch

Fasse die folgende Notiz zusammen: {{content}}
```

Was daran falsch ist: eine Persona ohne Wirkung (§8.1) in Versalien, die rund 37 % zusätzliche
Token kostet (§8.2); ein Denkprozess-Block, den das Modell nicht braucht (§3.1); „präzise und
vollständig" gegen „kurz" und „knapp" — ein Widerspruch, dreimal gesagt (§3.2, §3.3); „auf
Deutsch" doppelt (§3.2); „Ändere die Formatierung nicht" ist zu vage, um Frontmatter zu schützen
(§7.1); und **keine Aussage darüber, was eine fertige Zusammenfassung enthält**.

### Nachher

```text
Du fasst Besprechungsnotizen für Kollegen zusammen, die nicht dabei waren.

Eine fertige Zusammenfassung lässt den Leser wissen: was entschieden wurde, wer bis wann was
tut und was offen ist.

- Länge: 3–6 Sätze, oder bis zu 5 Stichpunkte, wenn die Notiz mehrere Themen berührt.
- Jede Aussage muss sich auf die Notiz zurückführen lassen. Was nicht darin steht, bleibt weg.
- Übernimm die Begriffe des Autors für Namen, Projekte und Zahlen.
- Gib das YAML-Frontmatter zwischen den `---`-Zeilen unverändert zurück, Zeichen für Zeichen.
- Wikilinks bleiben in ihrer Originalform: [[Notizname]] und [[Notizname|Alias]].
- Formatiere in Markdown; Listen nur für tatsächlich gleichrangige Punkte.
- Beginne mit dem ersten inhaltlichen Satz; die Notiz hat bereits einen Titel.

NOTIZ:
{{content}}
```

Jede Regel steht einmal; die Längenvorgabe ist eine Zahl statt eines Adjektivs; die Treue steht
als Ergebnis statt als Verbot; Frontmatter und Links sind ausdrücklich geschützt statt durch ein
vages „Formatierung nicht ändern"; es gibt kein Prozessskript; und die Notiz bleibt zuletzt
(§7.2).

**Diese Überarbeitung ist eine Veranschaulichung, keine belegte Verbesserung.** Nach §10 müsste
sie über 10–15 Notizen gemessen werden, bevor sie irgendetwas ersetzt.

---

## 12. Prüfliste

- [ ] Sagt der Prompt, was ein fertiges Ergebnis enthält?
- [ ] Sagt er, wann das Modell fertig ist?
- [ ] Steht jede Regel genau einmal — auch über getrennte Prompt-Dateien hinweg?
- [ ] Gibt es zwei Regeln, die nicht beide erfüllbar sind?
- [ ] Werden Absolutheiten (IMMER/NIE) nur für harte Grenzen benutzt?
- [ ] Ist jedes Verbot, das eine positive Anweisung sein könnte, auch eine?
- [ ] Steht ein Prozessskript drin, das das Modell nicht braucht?
- [ ] Sind Längenvorgaben Zahlen statt Adjektive?
- [ ] Sind Frontmatter, Wikilinks und Tags ausdrücklich geschützt?
- [ ] Gibt es eine Ausstiegsmöglichkeit für „steht nicht im Material"?
- [ ] Steht der veränderliche Inhalt zuletzt?
- [ ] Sind bei mehreren Dateien die wichtigsten nicht in der Mitte?
- [ ] Würde das Löschen jeder verbleibenden Zeile die Ausgabe verändern?
- [ ] Wurde die Änderung über einen festen Satz Notizen und mehrfach gemessen?
- [ ] Wurde das Messinstrument selbst geprüft?

---

## 13. Katalog der Anti-Muster

| Anti-Muster | Warum | Ersetzen durch |
|---|---|---|
| `### DENKPROZESS ###` | Konkurriert mit dem internen Denken | Beschreibung des fertigen Ergebnisses |
| GROSSBUCHSTABEN | Kein Nutzen, ~37 % mehr Token | Normale Schreibweise |
| „präzise und vollständig" + „kurz" | Widerspruch | Eine explizite Längenregel |
| Lange Experten-Persona | Bestenfalls zufällig wirksam | Eine kurze Zeile zu Rolle und Zielgruppe |
| „Tue X nicht" | Verneinung wirkt schwächer | „Tue stattdessen Y" |
| Dieselbe Regel in mehreren Prompt-Dateien | Liest sich als mehrere Regeln | Einmal sagen, an einer Stelle |
| „knapp", „ausführlich", „gründlich" | Nicht messbar | Satz- oder Stichpunktzahlen |
| „Ändere die Formatierung nicht" | Zu vage für YAML | Frontmatter und Links ausdrücklich benennen |
| Links verlangen ohne Titelliste | Erfindet nicht vorhandene Notizen | Vorhandene Titel mitgeben, sonst reiner Text |
| Den ganzen Vault einfügen | Genauigkeit sinkt mit der Länge | Die fünf relevanten Notizen |
| Wichtiges in die Mitte langer Kontexte | Schwächste Position | An Anfang oder Ende |
| `reasoning_effort` erhöhen für Qualität | Kostet mehr, kein Mehrinhalt | Erst den Vertrag klären |
| Durchgerechnete Denkbeispiele | Neutral bis schädlich | Nur Formmuster |
| Eine Änderung an einem Lauf beurteilen | Ausgaben sind nicht deterministisch | Fester Satz, wiederholte Läufe |
| Dem automatischen Prüfer vertrauen | Er kann falsch liegen | Erst den Prüfer prüfen |

---

## Literaturverzeichnis

Addlesee, A. (2024). Grounding LLMs to in-prompt instructions: Reducing hallucinations caused by
static pre-training knowledge. In *Proceedings of Safety4ConvAI: The Third Workshop on Safety for
Conversational AI @ LREC-COLING 2024*. https://aclanthology.org/2024.safety4convai-1.1/

*Aging of prompt engineering techniques across LLM versions* (2026). arXiv:2608.24641.
https://arxiv.org/html/2608.24641

Dwivedi, Y. K., et al. (2023). *Challenging the appearance of machine intelligence: Cognitive
bias in LLMs and best practices for adoption*. arXiv:2304.01358.
https://arxiv.org/pdf/2304.01358

Hu, Z., Rostami, M., & Thomason, J. (2026). *Expert personas improve LLM alignment but damage
accuracy: Bootstrapping intent-based persona routing with PRISM*. arXiv:2603.18507.
https://arxiv.org/html/2603.18507v1

Levy, B. (2026). Caution ahead: Numerical reasoning and look-ahead bias in AI models. *Journal of
Accounting Research, 64*(3), 1139–1188. https://doi.org/10.1111/1475-679x.70058

Liu, N. F., Lin, K., Hewitt, J., Paranjape, A., Bevilacqua, M., Petroni, F., & Liang, P. (2023).
Lost in the middle: How language models use long contexts. *Transactions of the Association for
Computational Linguistics*. https://arxiv.org/abs/2307.03172

Meincke, L., Mollick, E. R., Mollick, L., & Shapiro, D. (2025). *Prompting science report 2: The
decreasing value of chain of thought in prompting*. The Wharton School, University of
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

OpenAI. (2026h). *Prompt engineering*. OpenAI API documentation.
https://developers.openai.com/api/docs/guides/prompt-engineering

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

Zheng, M., Pei, J., Logeswaran, L., Lee, M., & Jurgens, D. (2024). When "a helpful assistant" is
not really helpful: Personas in system prompts do not improve performances of large language
models. In *Findings of the Association for Computational Linguistics: EMNLP 2024*.
https://aclanthology.org/2024.findings-emnlp.888/

### Hinweis zu den Messungen

Die mit **[Gemessen]** gekennzeichneten Angaben stammen aus Messungen gegen die Live-API von
OpenAI am 11.09.2026 in einer deutschsprachigen produktiven Zusammenfassungs-Anwendung: 160
Aufrufe über 16 Dokumente für das Kostenmodell, dazu getrennte Läufe zu Denkaufwand,
Prompt-Caching und ein kontrollierter Dreiervergleich von Prompt-Varianten (540 Erzeugungen, 180
automatisierte Bewertungen). Token wurden lokal mit `tiktoken` (`o200k_base`) gezählt. Sie stehen
hier, weil sie die einzigen Zahlen dieses Leitfadens sind, die an 5.6 selbst erhoben wurden — sie
stammen aber aus **einer** Last und **einer** Sprache und sind als Richtwerte zu lesen, nicht als
Benchmark.
