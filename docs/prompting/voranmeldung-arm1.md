# Vorab-Registrierung — Arm 1, Prompt-Überarbeitung (#73)

> **Status: Entwurf.** Die mit `‹…›` markierten Stellen füllt der Pilotlauf. Registriert wird
> erst, wenn alle Lücken geschlossen sind — und **bevor** ein einziger Lauf des Hauptversuchs
> startet. Danach ist dieses Dokument eingefroren; Abweichungen werden im Bericht als
> Abweichungen gekennzeichnet, nicht stillschweigend eingearbeitet.

**Angelegt:** 2026-09-13 · **Registriert:** ‹Datum› · **Eingefroren bei Commit:** ‹Hash›

---

## 1. Warum es diese Datei gibt

Hypothesen nach dem Ergebnis aufzuschreiben ist wertlos und lässt sich nicht rückwirkend
heilen. Weil die Daten hier erst erzeugt werden, folgt die Registrierung dem Muster der
Simulationsstudie: festgelegt wird **der datenerzeugende Prozess und die Auswertung als Code**,
nicht als Absichtserklärung.

Wo eine Entscheidung wirklich erst an den Daten fällt, steht hier die **bedingte Regel** — und
nicht die Entscheidung offen.

Der Vorversuch ist der Grund für diese Strenge: dort wurden neun Tests gerechnet, und nach
Korrektur für multiples Testen hielt kein Einzelergebnis stand. Das war korrekt berichtet,
aber vermeidbar.

## 2. Fragestellung

Welche Fassung der Systemnachricht und der acht Abschnitts-Prompts liefert die treuesten
Zusammenfassungen — bei vertretbaren Kosten?

**Kein Erkenntnisziel.** Arm 1 ist Produktarbeit. Die übertragbare Frage stellt Arm 2, auf
anderem Korpus und in zwei Sprachen.

## 3. Faktoren

Sieben, vollständig beschrieben in `faktoren-arm1-2026-09-13.md`, technisch umgesetzt in
`tools/prompt-eval/variants.py`.

| Ebene | Faktor | Stufen |
|---|---|---|
| Whole Plot | S1 Denkprozess-Block | drin / raus |
| Whole Plot | S2 Schreibweise | durchgehend groß / normal |
| Whole Plot | S3 Regelform | Verbote / positive Regeln |
| Whole Plot | S4 Format- und Überschriftenregel | verstreut / zentral |
| Subplot | A1 Sprachprämisse | drin / raus |
| Subplot | A2 Leerfall-Regel | raus / drin |
| Subplot | A3 Ausgabevertrag | minimal / voll |

**Konstant gehalten** (Begründung im Faktorenplan §3): Widersprüche S-03/S-04 aufgelöst,
Beispielpaar erhalten, Relevanzkriterien des `structuredPrompt` unangetastet,
`reasoning_effort` nicht gesetzt, Modellstufe fest.

## 4. Versuchsplan

D-optimaler Split-Plot, erzeugt mit `tools/prompt-eval/design.py`:

- **12 Whole Plots × 4 Läufe = 48 Zellen**
- 20 Parameter im Modell (Achsenabschnitt, 7 Haupteffekte, 12 Wechselwirkungen
  Whole Plot × Subplot), 28 Freiheitsgrade übrig
- Faktorkorrelation 0,000, Ungleichgewicht 0,000
- Varianzverhältnis der Planerzeugung: ‹aus dem Pilotlauf›; der Plan wird mit diesem Wert
  **einmal neu erzeugt**, falls er deutlich von 1,0 abweicht

Der Plan liegt als `design.arm1.json` fest, die 48 Prompt-Fassungen als
`varianten-arm1/prompts.NNN.*.json`. Beide werden mit dem Registrierungs-Commit eingefroren.

**Korpus:** 60 Elemente, geschichtet nach Diktatlänge.
**Wiederholungen:** K = ‹aus dem Pilotlauf› Erzeugungen je Zelle und Diktat.
**Blockung:** Jede Behandlung wird über die Zeitblöcke balanciert, Reihenfolge innerhalb eines
Blocks randomisiert.

## 5. Primärer Endpunkt — genau einer

> **Treue**, gemittelt über die drei automatisch laufenden Abschnitte mit dem reichsten
> Ausgabevertrag, bewertet auf der fünfstufigen verhaltensverankerten Skala.

Getestet bei vollem α = 0,05, ohne Korrektur. Alles andere ist nachgelagert.

**Warum Treue:** In einem Diktat-Archiv ärgert eine unvollständige Notiz — eine erfundene
richtet Schaden an.

## 6. Nachgelagerte Endpunkte (Gatekeeping)

Werden **nur** ausgewertet, wenn der primäre Endpunkt hält, und in dieser Reihenfolge:

1. Kosten je Diktat **mit** Cache-Rabatt
2. Vollständigkeit
3. Klarheit
4. Treue in den übrigen fünf Abschnitten
5. Erstversuchsquote

Das gibt volle Kontrolle des Familienfehlers **ohne** Powerverlust beim primären Endpunkt.
Alles jenseits dieser Liste ist explorativ, wird als solches gekennzeichnet und mit
Benjamini-Hochberg korrigiert — nicht mit Bonferroni.

## 7. Auswertungsmodell

Festgelegt vorab, umgesetzt in Code:

- **Ordinales gemischtes Modell** (cumulative link mixed model) mit zufälligem Achsenabschnitt
  je Diktat. Kein t-Test, keine Varianzanalyse, kein Mittelwert über 1–5-Noten.
- **Zwei Fehlerebenen.** Whole-Plot-Faktoren gegen den Whole-Plot-Fehler, Subplot-Faktoren
  gegen den Subplot-Fehler. Den Plan als vollständig randomisiert auszuwerten wäre ein Fehler
  und bläht den Fehler erster Art beim Modellvergleich auf.
- **Geclusterte Standardfehler** über `item_id`, darunter `section_id`.
- **Gepaarte Differenzen** als Standardvergleich.
- **Bedingte Regel:** Der Brant-Test prüft die Proportional-Odds-Annahme. Fällt sie
  (p < 0,05), wird auf partielle proportionale Odds ausgewichen — nicht auf ein metrisches
  Modell.
- **Bedingte Regel:** Liegen bei einer Dimension über 90 % der Noten auf einer Stufe, wird für
  diese Dimension zusätzlich der Anteil „Note 5" logistisch ausgewertet und beides berichtet.

## 8. Äquivalenzmarge — für jeden Nullbefund

Ein großes p ist **Abwesenheit von Beleg, nicht Beleg von Abwesenheit**. Soll irgendwo „kein
Unterschied" stehen, kommt das aus einem TOST-Äquivalenztest.

> **Marge: ‹Rauschboden aus dem Pilotlauf› Notenpunkte.**

Sie ist am gemessenen Rauschboden verankert — der Streuung desselben Prompts über
Wiederholungen. Ein Unterschied, der kleiner ist als der zwischen zwei Läufen **desselben**
Prompts, ist per Konstruktion nicht der Rede wert. Die Marge wird hier eingetragen, bevor
Ergebnisse vorliegen, und danach nicht mehr angefasst.

## 9. Richter und Kalibrierung

- Panel aus drei Modellen **verschiedener Familien**, Median der Urteile
- Jedes Paarurteil zweimal mit vertauschter Reihenfolge; Uneinigkeit gilt als Unentschieden
- Längenkontrolle per Regression im Nachgang
- Fester Modell-Snapshot für den gesamten Versuch
- **Bedingte Regel:** Liegt die Selbstkonsistenz unter 0,80 gleichem Urteil, werden ‹n›
  Richteraufrufe je Ausgabe gemittelt statt einem vertraut. Wert aus dem Pilotlauf: ‹…›
- **Bedingte Regel:** Prediction-Powered Inference wird eingesetzt, **wenn** die untere Grenze
  des 95-%-Konfidenzintervalls der Richter-Mensch-Korrelation über 1/√(n−2) liegt. Liegt nur
  der Punktschätzer darüber, gilt das als unsicher, und es werden weitere Noten vergeben statt
  das Verfahren zu starten. Ergebnis des Pilotlaufs: ‹…›
- Übereinstimmungsmaß: **Gwets AC2**, Krippendorffs α als Robustheitsprüfung, gewichtetes
  Kappa zusätzlich mit ausdrücklichem Hinweis auf das Kappa-Paradox

## 10. Abbruch- und Fortsetzungsregeln

- Der Lauf läuft in Blöcken. Abgebrochen wird, wenn der primäre Endpunkt eindeutig ist —
  **auch eindeutig null**.
- **Objektive Ausschlussregel:** Läufe mit leerer Ausgabe oder unlesbarem Urteil werden bis zu
  zweimal wiederholt, mit identischem Limit für **alle** Bedingungen. Die Erstversuchsquote
  wird je Bedingung als eigene Größe berichtet, nicht wegrepariert.
- Kein Nachlegen von Diktaten nach Sichtung der Ergebnisse.

## 11. Bestätigungslauf

Die aus dem Modell vorhergesagte beste Kombination ist eine Vorhersage, keine gemessene Zelle —
der Plan deckt 48 von 128 Kombinationen ab.

Sie wird deshalb erzeugt und **gepaart gegen die heutige Referenz über den ganzen Korpus**
gestellt. Fällt sie schlechter aus als vorhergesagt, war das Modell zu einfach. Das ist ein
Befund und wird als solcher berichtet.

## 12. Was ausdrücklich explorativ ist

- Wechselwirkungen innerhalb der Systemnachricht (im Plan nicht schätzbar)
- Unterschiede nach Diktatlänge
- Alles, was beim Lesen der Ausgaben auffällt
- Zusammenhänge zwischen Reasoning-Token und Qualität

Explorative Befunde werden im Bericht so gekennzeichnet und begründen keine Prompt-Änderung
ohne eigenen Versuch.

## 13. Entscheidung über die Übernahme

Kein Automatismus. Übernommen wird eine Fassung nur, wenn **alle drei** zutreffen:

1. Der primäre Endpunkt spricht für sie, oder sie ist äquivalent und deutlich günstiger.
2. Kein nachgelagerter Endpunkt verschlechtert sich bedeutsam.
3. Die Ausgaben sind gegen die heutige Fassung **gelesen** worden.

Übertragen wird danach in **beide** Orte — Team-Datei und `SummaryPrompts`-Konstanten, sonst
greift die Änderung nicht (`TeamPromptDriftTests` bewacht das).

⚠ Die zentrale Markdown-Regel (S4) wird erst übertragen, wenn die Wandlung nach HTML in #57
nachweislich greift. Bis dahin läuft sie nur im Versuch mit.
