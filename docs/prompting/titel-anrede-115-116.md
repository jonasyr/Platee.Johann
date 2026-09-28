# Titel ohne Wertung, Anrede mit Nachnamen: #115, #116 (28.09.2026)

Zwei Befunde aus dem Audit v1.5.0 (`docs/audit/2026-09-24-v1.5.0.md`):

- **F26:** Die externe Mail sprach „Thomas Berger“ als „Herr Thomas“ an.
- **F03:** Ein Titel erfand eine Wertung: „Brandschutznachweis **unerquicklich**“.

Der Wortlaut liegt wie bei #73 in der Sandbox:
- `prompt-sandbox\kandidat_v8_115_116.py` schreibt v8.
- `kandidat.K1.de.json` ist jetzt v8.
- `kandidat.K1.v7.de.json` ist der Stand davor.

## Was geändert wurde

| | Wo | Änderung |
|---|---|---|
| #115 E-Mail | Team-Datei **und** `SummaryPrompts.Email` (generiert) | Neue Anrede-Zeile: Weil gesiezt wird, steht der **Nachname** in der Anrede („Guten Tag Herr Berger,“, nie „Herr Thomas“). Bei unklarem Geschlecht der volle Name, bei nur bekanntem Vornamen neutral „Guten Tag,“. |
| #116 Titel | `SummaryGenerator.TitleInstruction` (kein Team-Prompt) | Ein Satz „nennt nur, worum es geht; bewertet nicht“ und ein Beispielpaar gut/schlecht. Der `DictationCostEstimator` zählt jetzt diesen Text statt einer eigenen Kopie. |

Die Team-Datei auf Z: ist gesichert als `prompts.vor-115-2026-09-28.json`. Nur `emailPrompt` wurde geändert; alle übrigen Schlüssel sind bytegleich.

## Messung

Werkzeug: `tools/prompt-eval/vergleich_115_116.py`.

- **Einsatz:** alte gegen neue Fassung, sonst alles gleich. Titel laufen auf denselben Transkripten, E-Mails auf denselben Prosa-Texten (aus `messlauf73f`, gültige Fassung).
- **Stichprobe:** 46 Korpus-Diktate (`corpus.v3`) und die sechs synthetischen Test-Diktate D1–D6. D4 ist der F26-Fall, D2 der F03-Fall.
- **Wiederholungen:** Titel 3×, E-Mail 2×.
- **Modell:** `gpt-5.6-luna`, ohne Korrekturliste.
- **Kosten:** 0,37 $ für diesen Lauf und den Titel-Nachlauf v8/v9 zusammen.

### E-Mail-Anrede (104 Mails je Fassung)

| Anrede | alt | neu |
|---|---:|---:|
| neutral („Guten Tag,“) | 88 | 93 |
| förmlich mit Nachnamen | 8 | 11 |
| **mit Vornamen / informell** („Guten Tag Thomas,“, „Lieber Thomas,“, „Hallo …,“) | **5** | **0** |
| **keine Anrede** | **3** | **0** |

- **D4 („Mail an Thomas Berger … Hallo Thomas“):** alt „Guten Tag Thomas,“ und „Lieber Thomas,“, neu zweimal „Guten Tag,“.
- Genau „Herr Thomas“ trat in diesem Lauf in keiner Fassung auf. Der Fehler dahinter trat aber auf: Die alte Fassung nahm den Vornamen, obwohl sie siezen soll.
- Dass D4 neutral statt „Herr Berger“ anredet, ist richtig. Die Mail bekommt nur die Prosa, und dort steht der volle Name nicht immer.

### Titel (156 Titel je Fassung)

Eine automatische Prüfung listet Titelwörter, deren Stamm im Diktat nicht vorkommt. Die Liste wurde gelesen, nicht nur gezählt.

| | alt | neu (v8) |
|---|---:|---:|
| Titel, die auf ein fremdes kleingeschriebenes Wort enden (Wertung, Füllwort) | **25** | **10** |
| D2 „…Brandschutznachweis unerquicklich“ | 2 von 3 | 0 von 3 |

- **Wertungen in der alten Fassung:** „effizienter“, „optimieren“, „verbessern“, „erleichtern“, „vereinfachen“, „erfolgreich“, „unklar“, „unerquicklich“.
- **In der neuen Fassung:** fast nur sachliche Verben („prüfen“, „nachreichen“, „zurückrufen“).

### Was bleibt

Selten hängt das Modell ein **sinnloses Adjektiv**, das nirgends im Diktat steht, an das Ende des Titels („… unerquicklich“, „… geschniegelt“). Das kam in der alten und der neuen Fassung etwa gleich oft vor, rund 0,5–1 % der Titel.

Ein zusätzlicher Satz „Verwende keine Adjektive, die nicht im Text stehen“ (v9, je 260 Titel) änderte daran nichts: 2 statt 1 Treffer. Deshalb wurde er nicht übernommen. Das ist eine Eigenheit des Modells und keine Frage des Wortlauts. Wer es sieht, korrigiert den Titel von Hand; „Neu generieren“ erneuert den Titel bewusst nicht (Entscheidung zu #116).

## Nebenbefund Werkzeug

`vergleich_115_116.py` schrieb aus mehreren Threads in dieselbe `.jsonl`. Dabei verschränkten sich einmal zwei Datensätze zu einer kaputten Zeile. `read_jsonl` hört an der ersten kaputten Zeile auf, und ein Neustart hätte alles danach neu bezahlt. Jetzt schützt eine Sperre die Schreibstelle. `messlauf.py` schreibt aus dem Hauptthread und war nicht betroffen.
