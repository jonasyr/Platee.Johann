# UI-Automation für Platé.Johann — Design

Stand: 2026-09-24 · Basis: `release/v1.5.0` · Status: vom Nutzer abgenommen (Chat), Spec zur Prüfung

## Ziel

Der Nutzer hat den Verdacht, dass in v1.4.0–v1.5.0 nicht alles so ist, wie es sein soll, kann es
aber nicht belegen. Gebraucht werden zwei Dinge, in dieser Reihenfolge:

- **A — Audit (einmalig, jetzt):** Agenten bedienen die echte, laufende App durch alle Flows,
  vergleichen mit dem dokumentierten Soll und liefern einen belegten Befundbericht.
- **B — Dauerhafte Suite:** die deterministischen Flows aus A als FlaUI-End-to-End-Tests im Repo,
  kostenlos und wiederholbar, vor jedem Release und in CI.

Erfolg: (A) ein Bericht, in dem jeder Befund reproduziert, belegt und gegen eine Soll-Quelle
geprüft ist; (B) eine Suite, die die Kern-Flows gegen die echte EXE prüft, ohne API-Kosten und
ohne den Desktop des Nutzers außerhalb eines bewussten Laufs zu belegen.

## Gesagt vs. angenommen

Vom Nutzer entschieden: Ansatz C (erst A, dann B) · Audit mit echter API gegen Sandbox-Kopie ·
Isolation per `JOHANN_HOME` (A1) · Abdeckung wie in Abschnitt „Audit-Umfang“ · B mit
OpenAI-Stub-Server (B1), CI zunächst nicht blockierend.
Vom Nutzer genannt: 95 % seiner MP3s sind kurze Testaufnahmen fast ohne Text.

Angenommen (korrigierbar): Nutzer bedient den PC während eines Automationslaufs nicht ·
Outlook-Schritt nur beaufsichtigt · Befunde werden erst nach Freigabe zu Issues.

## Sicherheitsregeln (gelten für A und B)

1. Niemals in `Z:\12_Tools\Peano\Johann\prompts.json` schreiben. Die Sandbox enthält eine Kopie;
   `GlobalPromptFilePath` zeigt darauf.
2. Niemals `Documents\Johann` lesen-schreibend benutzen. Der Lauf kopiert daraus einmalig und
   startet Johann nur mit gesetztem `JOHANN_HOME`.
3. **Wächter vor jedem Start:** `settings.json` der Sandbox darf weder `Z:` noch
   `Documents\Johann` enthalten (Quell-, Ausgabe-, Archivpfad, Prompt-Datei). Sonst Abbruch.
4. Kein Outlook außer im beaufsichtigten Mail-Schritt des Audits.
5. Kein zweiter Johann-Prozess: vor dem Start prüfen, dass keiner läuft.

## Produktänderungen (klein, nur per Umgebungsvariable aktiv)

| Variable | Wirkung | Wo |
|---|---|---|
| `JOHANN_HOME` | Ersetzt `Documents\Johann` als Einstellungsordner (settings, prompts.personal, Prompt-Cache, `.env`) und als Basis der Standard-Ein-/Ausgabeordner | `App.xaml.cs` (settingsDir, Default-Roots), `ApiKeyProvider` (Schritt 2) |
| `JOHANN_OPENAI_ENDPOINT` | Basis-URL für alle OpenAI-Aufrufe | `OpenAiLlmProvider`, `WhisperTranscriber` (`OpenAIClientOptions.Endpoint`), `OpenAiModelAvailabilityProbe` (`baseAddress`) |
| `JOHANN_NO_UPDATE_CHECK` | Überspringt die Velopack-Prüfung | `App.CheckForUpdatesAsync` |

Ungesetzt verhält sich Johann exakt wie heute — durch Unit-Tests belegt. **Gesetzt, aber ungültig** (relativer Pfad, keine http(s)-URL) verweigert Johann den Start mit klarer Meldung, statt still auf den Standard zurückzufallen: ein Tippfehler in `JOHANN_HOME` landete sonst in den echten Daten, einer im Endpunkt bei der kostenpflichtigen API. Ist `JOHANN_HOME` gesetzt, sucht `ApiKeyProvider` keine `.env` mehr in Elternordnern der EXE. Das Auflösen der
Variablen liegt in einer kleinen statischen Klasse (`JohannEnvironment`, Infrastructure/Hosting), die UI und
Infrastructure gemeinsam nutzen.

`AutomationProperties.AutomationId` an allen bedienbaren Elementen von `MainWindow`,
`SettingsView`, Dialogen und Toasts (Phase B). Keine Verhaltens- oder Sichtänderung.

## Bausteine

```
tools/ui-driver/                 Konsolenwerkzeug für Agenten (Phase A)
  Program.cs                     Befehle: start, tree, click, type, key, screenshot,
                                 wait-for, clipboard, close
Platee.Johann.UiDriver/          Bibliothek: FlaUI.UIA3-Kern (Start, Finden, Bedienen,
                                 Screenshot, Baum als JSON) — von Tool und Tests genutzt
Platee.Johann.UiTests/           Phase B: xUnit, startet die echte EXE
  Stub/OpenAiStubServer.cs       HttpListener: transcriptions, chat/completions, models/{id};
                                 Fehlerarten 500, Timeout, 404
  Fixtures/                      Antworten aus dem Audit (D1–D6), Test-MP3s
  Sandbox/                       Legt je Test JOHANN_HOME + output + eingang an
tests/fixtures/dictations/       Diktattexte D1–D8 (Markdown) + erzeugte MP3s
scripts/new-audit-sandbox.ps1    Sandbox bauen + Wächter
scripts/run-ui-tests.ps1         Build, Prozessprüfung, UiTests
docs/audit/                      Befundberichte + Screenshots
```

`Platee.Johann.UiDriver` hängt an keinem Produktprojekt; Johann wird nur als Prozess gestartet.

## Fixture-Diktate

Echte Aufnahmen des Nutzers taugen fast nur als Leer-Randfall. Deshalb eigene Texte, als MP3
per OpenAI-TTS erzeugt (Rückfall: Windows SAPI mit deutscher Stimme), je 30–90 s:

| # | Szenario | Soll |
|---|---|---|
| D1 | Aufgabenliste mit verschachtelten Unterpunkten | Aufgaben-Abschnitt, Verschachtelung in Ansicht und PDF, Fettdruck |
| D2 | Telefonat mit zwei Personen | Gesprächsnotiz entsteht |
| D3 | Zeiterfassung, drei Tätigkeiten mit Dauer | Stundenzettel je Tätigkeit eine Zeile; „Kein Gespräch dokumentiert.“ |
| D4 | Diktierte E-Mail, Empfänger geduzt | E-Mail siezt, Betreffzeile, förmlich |
| D5 | Englisches Diktat | Transkript englisch, Rest deutsch (#58) |
| D6 | Wörter aus der Korrekturliste („Piano“, „JGPT“) | korrigiert |
| D7 | Fast stille echte Aufnahme des Nutzers | nichts erfunden, kein Absturz |
| D8 | Datei > 25 MB (erzeugt, nie hochgeladen) | verständliche Ablehnung vor dem Upload (#77) |

## Phase A — Audit

### Ablauf

1. `new-audit-sandbox.ps1` kopiert `settings.json`, `prompts.personal.json`, `.env`, `output\`
   und die Team-Datei nach `%TEMP%\johann-audit\<lauf>\`, biegt alle Pfade in die Sandbox um,
   legt `eingang\` mit D1–D8 an, prüft den Wächter.
2. Flows laufen **nacheinander** (ein Desktop, ein Fokus). Je Schritt Screenshot vorher/nachher
   und UI-Baum. Parallel laufen nur Auswertungen ohne Desktop (Screenshots gegen Soll,
   Doku-gegen-Code).
3. Jede Beobachtung wird gegen eine **Soll-Quelle** geprüft: CLAUDE.md, RELEASE_NOTES.md,
   HANDBUCH.html/README.md, Issue-Text. Geschmack ohne Quelle ist „Vorschlag“, nie „Bug“.
4. **Zweiter Durchgang:** jeder Befund wird in der laufenden App erneut reproduziert, bevor er
   in den Bericht kommt.

### Audit-Umfang

1. **Start:** Erststart-Dialoge (Abschnittsmodus-Migration, Neuigkeiten), Pfadwarnungen,
   Offline-Modus ohne `.env` (eigener kurzer Lauf).
2. **Verarbeitung:** Watch-Folder mit D1–D8; In-App-Diktat ohne Mikrofon (Knopfzustand, Meldung).
3. **Eintragsliste:** Tage, Sortierung, „Nur unerledigte“, erledigt, Auswahl bleibt (#100), lange
   Titel/„…“, Doppelklick Trennlinie (#96), Löschen per `Entf`/Rechtsklick/Knopf + Rückfrage +
   Papierkorb + Tag verschwindet (#55).
4. **Detailansicht:** alle Abschnitte, ein-/ausblenden, Abschnitte auf Knopfdruck,
   Kopiersymbol je Abschnitt + „Kopieren“ (Zwischenablage auslesen und vergleichen, #56),
   Transkript bearbeiten → neu generieren, Zoom (Tasten, Strg+Mausrad), Fehler-Toasts.
5. **Exporte:** PDF (Text auslesen + Screenshot, keine `**`), HTML-Übersicht, Drag & Drop falls machbar.
6. **Einstellungen:** jede Sektion; KI-Modell + Kostenkarte (#71); Vorlagen anlegen/umbenennen/
   löschen, automatisch/auf Knopfdruck, Speicherziel Persönlich/Global (Sandbox-Kopie);
   Korrekturliste; Pfade; Layout mit/ohne Bildlaufleiste (#79).
7. **Mail (beaufsichtigt):** „Aufgaben“ und „E-Mail“ im tatsächlich genutzten Outlook.
8. **Neuigkeiten + Handbuch:** Knopf, Puls, Darstellung der neu gegliederten Release Notes.
9. **Tastatur & Barrierefreiheit:** Tab-Reihenfolge, sichtbarer Fokus, jeder Knopf erreichbar,
   Kontrast aus Screenshots gemessen (WCAG AA).

### Befundbericht

`docs/audit/2026-09-<tag>-v1.5.0.md` + `docs/audit/<lauf>/*.png`. Je Befund: Flow · Soll (mit
Quelle) · Ist · Screenshot · Schwere (**Bug / UX / Kosmetik / Doku-Abweichung / Vorschlag**) ·
Reproduktion. Nach Durchsicht durch den Nutzer werden bestätigte Befunde auf Zuruf zu Issues.

Die echten Modellantworten auf D1–D6 werden als Fixtures für Phase B gesichert.

## Phase B — Dauerhafte Suite

### Test-Sandbox je Test

Eigener `JOHANN_HOME`, `output\`, `eingang\`, Stub-Server auf freiem Port,
`JOHANN_OPENAI_ENDPOINT` darauf, `JOHANN_NO_UPDATE_CHECK=1`, Dummy-Schlüssel in `.env`. Johann
wird mit diesem Environment als Prozess gestartet und am Testende beendet. Fehlschlag hinterlässt
Screenshot + UI-Baum in `TestResults`.

### Stub-Server

Antwortet aus Fixtures: Transkription je MP3-Name, Chat-Antwort je Abschnitt (erkannt am
Prompt-Inhalt), `models/{id}` 200. Pro Test umschaltbar auf 500, Timeout, 404 für ein Modell.
Er protokolliert jede Anfrage, damit Tests auch prüfen können, was **nicht** gesendet wurde
(z. B. keine Anfrage bei > 25 MB).

### Dauerhafte Tests

- Start inkl. Erststart-Dialoge · Watch-Folder → Eintrag erscheint an Sortierposition
- Liste: Sortierung, Filter, erledigt, Auswahl bleibt, Löschen inkl. Papierkorb + Tag verschwindet
- Detail: Abschnitte ein/aus, Abschnitt auf Knopfdruck, Kopiersymbol + „Kopieren“
  (Zwischenablage), Transkript bearbeiten → neu generieren, Zoom
- PDF-Export: Datei existiert, Text enthält Abschnitte, keine `**`
- Einstellungen: Vorlage anlegen/löschen, Speicherziel Persönlich/Global (Sandbox), Modellkarte,
  Korrekturliste
- Fehler: Stub 500 → Fehler-Toast; > 25 MB → Ablehnung ohne Anfrage
- Neuigkeiten-Knopf · Tastatur: jeder Hauptknopf per Tab erreichbar, Fokus sichtbar

**Nicht dauerhaft:** Outlook (Unit-Tests decken ab, öffnet echte Entwürfe), Mikrofon,
Drag & Drop in den Explorer, optische Beurteilung (bleibt Audit + Sichtprüfung).

### Ausführung

- Lokal: `scripts/run-ui-tests.ps1` (baut, prüft laufenden Johann, startet Suite).
- Nicht in `dotnet test` der Lösung und nicht im Pre-Push-Hook (langsam, belegt den Desktop).
- CI: eigener Job auf `windows-latest`, `continue-on-error: true`, bis er über mehrere Läufe
  stabil ist; danach blockierend. Timeout wie beim bestehenden Job.
- „Vor jedem Release“ (Serena-Memory `backlog`) bekommt den Punkt „UI-Suite grün“.

## Fehlerbehandlung

- Driver: jede Suche mit Timeout und klarer Meldung „Element X nicht gefunden“ + Baum-Dump.
- Unerwartete MessageBox (z. B. Pfadwarnung) wird erkannt, fotografiert und als Befund/
  Testfehler gemeldet, nicht blind weggeklickt.
- Johann-Prozess wird am Ende immer beendet (auch bei Fehlschlag); Sandbox bleibt bei
  Fehlschlag zur Analyse liegen, sonst wird sie gelöscht.

## Tests der Produktänderungen

`JohannEnvironment` mit Unit-Tests: ungesetzt oder leer → heutige Pfade/Endpoint; gesetzt → Override;
ungültiger Wert → Ausnahme mit Variablenname. Ein Test belegt, dass `OpenAiLlmProvider` und
`WhisperTranscriber` den Endpoint übernehmen (gegen den Stub).

## Offene Punkte / Risiken

- WPF `WebBrowser` (Neuigkeiten, Handbuch) ist für UIA teilweise undurchsichtig — dort nur
  Screenshot-Beurteilung.
- GitHub-`windows-latest` hat eine Desktop-Sitzung, UIA-Tests laufen dort erfahrungsgemäß,
  Stabilität muss sich zeigen — deshalb zunächst nicht blockierend.
- Kontrastmessung aus Screenshots ist ClearType-bedingt ungenau; maßgeblich bleiben die
  XAML-basierten `ControlContrastTests`, der Audit meldet nur Kandidaten.
