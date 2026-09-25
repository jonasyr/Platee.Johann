# 📄 Johann Backlog  
  
> Kurzübersicht der geplanten/offenen Änderungen
> Details → [[Code Audit Report]]  
  
---  
  
## 📊 Status Legend  
  
- ☐ Offen  
- 🟡 In Arbeit  
- ✅ Fertig  
- 🔁 Verschoben  


  
---  
## 🐛 Bugfixes  
  
| Status | Titel                                                             | Beschreibung                                    | Link                                                                                                           |
| ------ | ----------------------------------------------------------------- | ----------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| ✅      | Sequence Number Race Condition                                    | Doppelte Nummern / mögliche Datenüberschreibung | [[Code Audit Report#Finding 1 Sequence Number Race Condition Under Concurrent Processing]]                     |
| ✅      | Kein Feedback bei Input-Ordner Fehler                             | Fehler wird nicht an User propagiert            | [[Code Audit Report#Finding 2 Fire-and-Forget `Task.Run` in AudioWatcherService Swallows Exceptions Silently]] |
| ✅      | UI Deadlock beim Start                                            | Sync-Blocking von Async Code                    | [[Code Audit Report#Finding 4 Synchronous Blocking on Async Code at Startup]]                                  |
| ☐      | FileSystemWatcher verliert Events                                 | Dateien werden teilweise nicht verarbeitet      | [[Code Audit Report#Finding 7 `FileSystemWatcher` Misses Events Under Load and Has No Retry Logic]]            |
| ✅      | Analog Typ wird nicht erkannt                                     | Falsche Klassifikation von Entries              | [[Code Audit Report#Finding 12 `TypeExtractor` Missing "Analog" Keyword]]                                      |
| ✅      | Validation Bypass im Dialog                                       | DialogResult wird falsch gesetzt                | [[Code Audit Report#Finding 13 `NewEntryView` Dialog Result Logic Is Fragile]]                                 |
| ✅      | Inkonsistente Generierung bei Prompt-Änderung                     | Settings werden während Verarbeitung geändert   | [[Code Audit Report#Finding 16 `SettingsView` Is Non-Modal but Settings Changes Have No Undo]]                 |
| ✅      | Path Bug wenn neue Settings.json von anderem Rechner genutzt wird | Failed silently wenn Pfad nicht existiert       |                                                                                                                |
| ✅      | „Erledigt"-Button verdeckt & scrollt weg                          | Button von oben rechts nach oben links verschieben und fix (nicht mitscrollend) über der Detailansicht verankern, damit er beim Einlesen neuer Einträge nicht von Toasts verdeckt wird und immer erreichbar bleibt | [#37](https://github.com/jonasyr/Platee.Johann/issues/37) |
| ✅      | Renderer-Dispatch überspringt Renderer stillschweigend | Groß-/Kleinschreibung beim Abgleich von „PDF"/„HTML" — ein als „pdf" registrierter Renderer wurde ohne Fehler und ohne Log übersprungen | [#40](https://github.com/jonasyr/Platee.Johann/issues/40) |
| ✅      | Auto-Update seit v1.1.0 komplett kaputt | `VelopackApp.Run()` war hinter Argument-Prüfung versteckt und lief beim normalen Start nie — der Locator wurde nie initialisiert, es gab nie eine Update-Meldung | [#42](https://github.com/jonasyr/Platee.Johann/issues/42) |
| ✅      | Stille Fehler im gesamten Programm | Audit über 40 catch-Stellen: unerreichbare Team-Prompts, defekte settings.json/prompts.json, stumme Drag-&-Drop- und Migrationsfehler — 9 Befunde behoben, 7 bewusst unverändert | [#45](https://github.com/jonasyr/Platee.Johann/issues/45) |
  
---  
## 🛠 Improvements  
  
| Status | Titel                                      | Beschreibung                                                                                                                                                                       | Link                                                                                                             |
| ------ | ------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| ✅      | Logging Pfad ändern                        | Logs nicht mehr auf Desktop sondern unter C:\Users\[User]\Peano\Johann\...                                                                                                         | —                                                                                                                |
| ✅      | Settings UX verbessern                     | Tabs springen nicht mehr                                                                                                                                                           | —                                                                                                                |
| ✅      | XSS Fix im HTML Renderer                   | Markdown → HTML absichern                                                                                                                                                          | [[Code Audit Report#Finding 5 XSS Vulnerability in HTML Renderers via Markdown Content]]                         |
| ☐      | API Key Sicherheit                         | Klartext → Verschlüsselung                                                                                                                                                         | [[Code Audit Report#Finding 6 API Key Stored in Plaintext `.env` File Without Encryption]]                       |
| ✅      | Dispose Handling verbessern                | Watcher sauber freigeben                                                                                                                                                           | [[Code Audit Report#Finding 8 `Dispose` Pattern Incomplete — `AudioWatcherService` Not Disposed on Crash Paths]] |
| ✅      | Exception Handling verbessern              | Keine leeren catch Blöcke                                                                                                                                                          | [[Code Audit Report#Finding 9 Swallowed Exceptions in Processing Pipeline Mask Root Causes]]                     |
| ✅      | HtmlEncode ersetzen                        | Built-in Encoding nutzen                                                                                                                                                           | [[Code Audit Report#Finding 10 `HtmlEncode` Is Manual and Incomplete]]                                           |
| ✅      | JSON Lookup optimieren                     | O(N) → effizienter Zugriff                                                                                                                                                         | [[Code Audit Report#Finding 11 JSON Repository Reads All Files Sequentially for `GetByJobIdAsync`]]              |
| ✅      | Duration Formatter zentralisieren          | Duplicate Code entfernen                                                                                                                                                           | [[Code Audit Report#Finding 14 Duplicated Duration Formatting Logic Across 4 Files]]                             |
| ✅      | Test Coverage erweitern                    | Integration Tests hinzufügen                                                                                                                                                       | [[Code Audit Report#Finding 15 No Test Coverage for `EntryProcessingService` Integration]]                       |
| ✅      | Prompts verbessern                         | Neue und bessere Global Defaults für die Prompts                                                                                                                                   |                                                                                                                  |
| ✅      | Anzahl unerledigter Einträge visualisieren | Neben dem<br>Datum, in Klammern, die Anzahl der Einträge, die noch nicht erledigt sind. Dann würde man sehen, ob<br>man da noch was vergessen hat.                                 |                                                                                                                  |
| ✅      | Optionsdateien trennen                     | Zwei Optionsdateien vorsehen: eine individuelle Datei für persönliche Einstellungen wie Allgemein und Verzeichnisse, sowie eine globale Prompt-Datei unter Z:\... für alle Nutzer. | —                                                                                                                |
| ✅      | Korrekturliste für Prompts                 | Eigene Prompt-Option „Korrekturliste“ ergänzen, damit häufige Korrekturen zentral gepflegt werden können, z. B. „Peano“ nicht als „Piano“ und „Neele“ nie als „Nele“.              | —                                                                                                                |
  
---  
  
## ✨ Features  
  
| Status | Titel                                    | Beschreibung                                                                                                                                                                                                                                                                                                                                                                                    | Link                                                                                                           |
| ------ | ---------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| ☐      | Parallel Processing                      | Mehrere Audios gleichzeitig verarbeiten                                                                                                                                                                                                                                                                                                                                                         | [[Code Audit Report#Finding 3 `SemaphoreSlim` in AudioWatcherService Serializes All Processing Unnecessarily]] |
| ✅      | Zoomen Shortcut                          | Zoomen mit Strg + und Strg - bzw auch Strg Mausrad ermöglichen sodass man nicht immer unten rechts + und - drücken muss.                                                                                                                                                                                                                                                                        |                                                                                                                |
| ✅      | Transkriptionstext bearbeiten            | Möglichkeit ergänzen, den Transkriptionstext nachträglich über einen Bearbeiten-/Bleistift-Button zu ändern und den Eintrag anschließend erneut generieren zu lassen. Hilfreich, um abgebrochene oder zusammengehörige Nachrichten zu verbinden, Inhalte zu entfernen oder Korrekturen am Transkript vorzunehmen.                                                                               |                                                                                                                |
| ✅      | Neue Einträge direkt in Johann diktieren | Möglichkeit ergänzen, direkt in Johann neue Einträge per Diktat zu erstellen. Dafür soll es ein neues Eingabefeld bzw. einen Bereich mit großem Startbutton geben. Nach dem Diktieren wird der Text transkribiert, anschließend stehen sowohl der Transkriptionstext als auch die ausführliche Zusammenfassung zur Verfügung. Weitere Zusammenfassungen sollen optional erstellt werden können. |                                                                                                                |
| ☐      | Frei definierbare Kategorien (allgemein + eigen) | Die acht fest verdrahteten Prompts (`PromptSettings`, `SummaryGenerator`, `Entry`, XAML) durch benutzerdefinierbare Kategorien ersetzen: globale (Team) und persönliche Kategorien, frei anleg-, umbenenn-, sortier- und löschbar — z. B. eine eigene Kategorie „Programmierung". Das Admin-Passwort für die Prompt-Bearbeitung entfällt ersatzlos. | [#34](https://github.com/jonasyr/Platee.Johann/issues/34) |
| ☐      | Auto- vs. Knopfdruck-Generierung pro Kategorie | Heute werden pro Eintrag alle acht Abschnitte generiert (8 GPT-Aufrufe) — beim Warten vor dem Rechner zu langsam. Pro Kategorie wählbar: automatisch (ca. 4–5) oder erst auf Knopfdruck. Nicht generierte Abschnitte zeigen einen „Generieren"-Button. | [#35](https://github.com/jonasyr/Platee.Johann/issues/35) |
| ☐      | Live-Diktat-Modus mit Kategorie-Auswahl | Nach dem Diktat zuerst nur Transkript, Titel und Abstract erzeugen und anzeigen; danach per Checkboxen wählen, welche Kategorien daraus generiert werden. Transkript vor der Generierung korrigierbar. Watch-Ordner bleibt unbeaufsichtigt wie bisher. | [#36](https://github.com/jonasyr/Platee.Johann/issues/36) |
| ☐      | Markdown durchgängig (Erzeugung, Anzeige, PDF) | Prompts sollen verbindlich Markdown liefern; alle Abschnitte in der Detailansicht gerendert darstellen (heute nur `LongSummary`/`TaskList`/`ConversationNote`); `PdfRenderer.RenderMarkdown` durch echtes Markdig-Parsing ersetzen (heute nur `###` + einfache Bullets). | [#38](https://github.com/jonasyr/Platee.Johann/issues/38) |

---

## 📌 Aktueller Stand (2026-09-25)

**v1.5.0 ist entwickelt, aber noch nicht veröffentlicht.** Nutzer laufen auf **v1.4.0**
(ausgeliefert 2026-09-10).

**Neu seit 2026-09-24: UI-Automation und Audit ([#111](https://github.com/jonasyr/Platee.Johann/issues/111), PR #113).**
- Die laufende App wurde in einer Sandbox mit Kopie der echten Daten durchgeprüft. Bericht mit
  30 Befunden: `docs/audit/2026-09-24-v1.5.0.md`.
- Dazu kommt eine FlaUI-Testsuite gegen die echte EXE: 23 Tests für Liste, Löschen, Detail,
  Kopieren, PDF, Einstellungen, Fehler und Tastatur. Sie läuft lokal mit
  `scripts/run-ui-tests.ps1` (belegt den Desktop) und als nicht blockierender CI-Job.
- Gefunden und behoben: F28, der erste Eintrag eines neuen Tages erschien nicht in der Liste.

**Vor dem Release noch offen (Meilenstein v1.5.0):**
- [#114](https://github.com/jonasyr/Platee.Johann/issues/114) „Global“ speichert persönliche Vorlagen in die Team-Datei. Seit v1.4.0
  ausgeliefert; die echte Team-Datei ist noch sauber.
- [#115](https://github.com/jonasyr/Platee.Johann/issues/115) E-Mail-Anrede „Herr Thomas“ statt „Herr Berger“.
- [#116](https://github.com/jonasyr/Platee.Johann/issues/116) Der Titel erfindet eine Wertung.
- [#112](https://github.com/jonasyr/Platee.Johann/issues/112) Transkript mit Zeilenumbruch nach jedem Satzende.

Danach folgt der Release: `build-installer.ps1 -Version 1.5.0` mit geschlossenem Johann,
Auto-Update gegen v1.4.0 prüfen, dann `release/v1.5.0` → `main` + Tag.

Inhalt v1.5.0: Mail-Knöpfe für klassisches und neues Outlook, Einträge löschen mit
Johann-Papierkorb, Kopiersymbol je Abschnitt, überarbeitete Prompts (−20 % Kosten),
PDF mit Listen und Fettdruck, ruhigere Eintragsliste (kein Neuladen, Auswahl bleibt),
Doppelklick auf die Trennlinien passt die Spalte an, Knopf „Neuigkeiten“, einheitliche
Knöpfe mit geprüften Kontrasten, Aufnahmen über 25 MB verständlich abgelehnt, gescheiterte
Diktate werden gesichert statt gelöscht. 816 Tests grün. Details: `RELEASE_NOTES.md`.

Nebenbei behoben: ein Deadlock in den WPF-Tests, der die CI hängen ließ, und die CI
meldete fehlschlagende Tests bis dahin gar nicht (PR #101).

> [!IMPORTANT]
> **Source of Truth sind die [GitHub Issues](https://github.com/jonasyr/Platee.Johann/issues).**
> Diese Datei ist ein gepflegter Spiegel für die Abstimmung mit dem Chef.
> Bei Abweichung gilt GitHub. Die veraltete Zweitkopie `docs/Johann Backlog.md`
> wurde am 2026-09-10 gelöscht — sie stammte von vor der GitHub-Migration und
> zeigte 13 längst erledigte Punkte fälschlich als offen.

### Neu aufgenommen (Sprachnachrichten 260909_004 und 260909_007)

Nach dem Meeting vom 2026-09-09 sind sechs Issues dazugekommen (#63–#68) und
sechs bestehende wurden umgeschrieben. Die Einzelheiten stehen unten unter
„Priorisierung & Reihenfolge".

**Kopplung aufgelöst:** #62 (Textentry entfernen) war angeblich an #58
(Übersetzung) gekoppelt. Das ist **falsch** — #58 arbeitet mit Diktaten in der
jeweiligen Sprache bzw. übersetzt bestehende Einträge und braucht keine
Tastatureingabe. #62 ist frei.

**Weiterhin gültig:** keine globalen Kategorien auf `Z:`, solange nicht das
ganze Team auf 1.3.3+ läuft — ein v1.3.2-Client entfernt `customCategories`
beim Speichern aus der Team-Datei.

---

## 🔗 GitHub-Issue-Zuordnung

Abgleich Backlog ↔ [GitHub Issues](https://github.com/jonasyr/Platee.Johann/issues) (Stand: 2026-09-10 — für alles danach gilt der Release-Plan unten bzw. GitHub).

| Issue | Titel | GH-Status | Backlog-Eintrag |
| ----- | ----- | --------- | --------------- |
| [#6](https://github.com/jonasyr/Platee.Johann/issues/6) | Sequence Number Race Condition in JsonRepository | geschlossen (v1.3.0) | ✅ Sequence Number Race Condition |
| [#7](https://github.com/jonasyr/Platee.Johann/issues/7) | Synchronous blocking on async code at startup | geschlossen | ✅ UI Deadlock beim Start |
| [#8](https://github.com/jonasyr/Platee.Johann/issues/8) | FileSystemWatcher drops events under load | **offen** | ☐ FileSystemWatcher verliert Events |
| [#9](https://github.com/jonasyr/Platee.Johann/issues/9) | Settings changes during active processing | geschlossen | ✅ Inkonsistente Generierung bei Prompt-Änderung |
| [#10](https://github.com/jonasyr/Platee.Johann/issues/10) | Encrypt OpenAI API key at rest (DPAPI) | **offen** | ☐ API Key Sicherheit |
| [#11](https://github.com/jonasyr/Platee.Johann/issues/11) | Complete IDisposable implementation | geschlossen (v1.3.0) | ✅ Dispose Handling verbessern |
| [#12](https://github.com/jonasyr/Platee.Johann/issues/12) | Replace swallowed empty catch blocks | geschlossen (PR #33) | ✅ Exception Handling verbessern |
| [#13](https://github.com/jonasyr/Platee.Johann/issues/13) | Replace O(N) scan in GetByJobIdAsync | geschlossen | ✅ JSON Lookup optimieren |
| [#14](https://github.com/jonasyr/Platee.Johann/issues/14) | Centralize FormatDuration helper | geschlossen | ✅ Duration Formatter zentralisieren |
| [#15](https://github.com/jonasyr/Platee.Johann/issues/15) | Integration tests for EntryProcessingService | geschlossen (v1.3.0) | ✅ Test Coverage erweitern |
| [#16](https://github.com/jonasyr/Platee.Johann/issues/16) | Separate personal settings from global prompts | geschlossen | ✅ Optionsdateien trennen |
| [#17](https://github.com/jonasyr/Platee.Johann/issues/17) | Correction list for Whisper fixes | geschlossen | ✅ Korrekturliste für Prompts |
| [#18](https://github.com/jonasyr/Platee.Johann/issues/18) | Parallel audio processing | **offen** | ☐ Parallel Processing |
| [#19](https://github.com/jonasyr/Platee.Johann/issues/19) | Zoom keyboard shortcuts | geschlossen | ✅ Zoomen Shortcut |
| [#20](https://github.com/jonasyr/Platee.Johann/issues/20) | Edit transcript and re-generate | geschlossen | ✅ Transkriptionstext bearbeiten |
| [#21](https://github.com/jonasyr/Platee.Johann/issues/21) | In-app dictation | geschlossen | ✅ Neue Einträge direkt in Johann diktieren |
| [#29](https://github.com/jonasyr/Platee.Johann/issues/29) | SettingsHolder.Snapshot() not atomic | geschlossen | ✅ (Teil von „Inkonsistente Generierung") |
| [#34](https://github.com/jonasyr/Platee.Johann/issues/34) | User-definable categories, no password gate | geschlossen | → zerlegt in #50–#53 |
| [#35](https://github.com/jonasyr/Platee.Johann/issues/35) | Auto vs. on-demand generation per category | geschlossen | → #52 |
| [#36](https://github.com/jonasyr/Platee.Johann/issues/36) | Diktier-Popup (2026-09-10 neu gefasst, 2026-09-11 Halte-Knopf ergänzt) | **offen** | ☐ Diktier-Popup |
| [#37](https://github.com/jonasyr/Platee.Johann/issues/37) | Move „Erledigt" button top-left and pin it | geschlossen (v1.3.0) | ✅ „Erledigt"-Button verdeckt & scrollt weg |
| [#38](https://github.com/jonasyr/Platee.Johann/issues/38) | End-to-end Markdown | **offen** | ☐ Markdown durchgängig |
| [#39](https://github.com/jonasyr/Platee.Johann/issues/39) | Epic: Live dictation & user-definable categories | **offen** | (Klammer um #34/#35/#36) |
| [#50](https://github.com/jonasyr/Platee.Johann/issues/50) | Admin-Passwort entfernen | erledigt (v1.3.3) | ✅ in `main` |
| [#51](https://github.com/jonasyr/Platee.Johann/issues/51) | Kategorien-Modell + Persistenz (Schema v4) | erledigt (v1.3.3) | ✅ in `main` |
| [#52](https://github.com/jonasyr/Platee.Johann/issues/52) | Auto vs. Knopfdruck + id-basiertes Dispatch | erledigt (v1.3.3) | ✅ in `main` (ersetzt #35) |
| [#53](https://github.com/jonasyr/Platee.Johann/issues/53) | Kategorien-Einstellungen + On-Demand-Zeilen | erledigt (v1.3.3) | ✅ in `main` |
| [#40](https://github.com/jonasyr/Platee.Johann/issues/40) | Renderer dispatch is case-sensitive, skips silently | geschlossen (v1.3.0) | ✅ Renderer-Dispatch überspringt Renderer |
| [#42](https://github.com/jonasyr/Platee.Johann/issues/42) | Auto-update broken since v1.1.0 | geschlossen (v1.3.1) | ✅ Auto-Update seit v1.1.0 kaputt |
| [#45](https://github.com/jonasyr/Platee.Johann/issues/45) | Audit: silent-failure sweep, 9 findings | geschlossen (v1.3.2) | ✅ Stille Fehler im gesamten Programm |

**Ohne GitHub-Issue** (bereits erledigt, nur im Backlog dokumentiert): Kein Feedback bei Input-Ordner Fehler · Analog Typ wird nicht erkannt · Validation Bypass im Dialog · Path Bug bei fremder settings.json · Logging Pfad ändern · Settings UX verbessern · XSS Fix im HTML Renderer · HtmlEncode ersetzen · Prompts verbessern · Anzahl unerledigter Einträge visualisieren

---

## 📥 Anforderungen Chef (Sprachnachricht 2026-09)

Abgeglichen am 2026-09-07 gegen den v1.4.0-Branch und die offenen Issues.
Quelle: Original-Transkript + ChatGPT-Zusammenfassung (JOH-01…27).

### Bereits erledigt — steckt in v1.4.0 (Branch `feat/51-category-model`, noch nicht gemergt)

| JOH | Anforderung | Umgesetzt in |
| --- | ----------- | ------------ |
| JOH-01 | Globale Kategorien editierbar | #50 (Passwort weg) + #53 |
| JOH-02 | Globale Kategorien ergänzbar | #51 + #53 |
| JOH-03 | Persönliche Kategorien pro Benutzer | #51 + #53 |
| JOH-04 | Persönliche Kategorien verwalten | #53 |
| JOH-15 | Weitere Ausgaben erst bei Auswahl generieren | #52 (`GenerateSectionAsync` + „Generieren"-Button) |

**Der inhaltliche Hauptwunsch des Chefs ist damit fertig und wartet nur auf den Merge.**

### Reine Konfiguration — kein Code nötig

| JOH | Anforderung | Hinweis |
| --- | ----------- | ------- |
| JOH-06 | Kategorie „E-Mail formlos" | als globale Vorlage anlegen |
| JOH-07 | Kategorie „E-Mail förmlich" | als globale Vorlage anlegen |
| JOH-10/12 | Automatische Spracherkennung | übernimmt das Modell, muss nur im Prompt stehen (#58) |

⚠️ Globale Vorlagen erst anlegen, wenn das **ganze Team** auf v1.4.0 ist —
ein v1.3.2-Client löscht `customCategories` beim Speichern wieder
(siehe `TESTPLAN-v1.4.0.md` §3.3).

### Neue Issues

| JOH | Anforderung | Issue |
| --- | ----------- | ----- |
| JOH-13/14/16 | Diktat vs. Import getrennt behandeln | [#61](https://github.com/jonasyr/Platee.Johann/issues/61) |
| JOH-17/18 | Einträge löschen (Papierkorb + Rechtsklick) | [#55](https://github.com/jonasyr/Platee.Johann/issues/55) |
| JOH-19/20 | E-Mail als Dokument öffnen | [#57](https://github.com/jonasyr/Platee.Johann/issues/57) |
| JOH-21/22/23 | Kopiersymbol je Abschnitt | [#56](https://github.com/jonasyr/Platee.Johann/issues/56) |
| JOH-24…27 | Einträge zusammenführen (A → B) | [#54](https://github.com/jonasyr/Platee.Johann/issues/54) |
| JOH-09/11 | Übersetzung Arabisch / Ukrainisch | [#58](https://github.com/jonasyr/Platee.Johann/issues/58) |
| JOH-08 | Ariadne-Integration „KI" | [#60](https://github.com/jonasyr/Platee.Johann/issues/60) |
| — | „+ Neues Element“-Button entfernen (Chef) | [#62](https://github.com/jonasyr/Platee.Johann/issues/62) |
| — | „Vorlagen" statt „Kategorien" (Jonas) | [#59](https://github.com/jonasyr/Platee.Johann/issues/59) |

### Geklärt am 2026-09-07

1. ✅ **JOH-27 — Merge-Richtung.** A auf B ziehen → an B anhängen, **B** neu berechnen,
   **A** löschen. Das wörtliche „B soll gelöscht werden“ war ein Versprecher. → #54
2. ✅ **JOH-19 — E-Mail-Ausgabe.** Weder PDF noch HTML: die Mail öffnet sich beim Klick **direkt
   in Outlook**. `OpenInOutlookCommand` existiert bereits, hängt aber im Rechtsklick-Menü. → #57
3. ✅ **JOH-14 — Diktat-Automatik.** Abstract + Zusammenfassung. Damit kostet ein Diktat
   **zwei statt acht** GPT-Aufrufe. → #61
4. ✅ **Texteingabe.** „+ Neues Element“ wird **vollständig entfernt** (#62).
   Folge: #58 muss eine eigene Texteingabe mitbringen und wächst von `size: S` auf `size: M`.
   **Beide Tickets gehören in dieselbe Version** — sonst ist die Texteingabe weg, bevor der
   Ersatz existiert.
5. ✅ **JOH-05 — „diese Testdinger“ ist bereits erledigt.** Gemeint war, dass persönliche
   Prompt-Änderungen nur bis zum nächsten Neustart galten („gelten nur bis zum nächsten
   Neustart“) — also faktisch nur zum Testen taugten. Genau das ist in v1.4.0 behoben:
   #50 gibt persönlichen Prompts ein eigenes Ziel (`prompts.personal.json`), Commit `397ed32`
   schließt die Lücke, dass `CustomCategories` gar nicht serialisiert wurde, und
   `PromptStartupResolver` lädt sie beim Start wieder ein.
   **Kein eigenes Ticket nötig.**

### Offene Rückfragen an den Chef

1. **#36 vs. #61** — der Auswahldialog aus #36 ist laut Chef-Aussage vermutlich überflüssig,
   sobald #61 umgesetzt ist. Vor der Umsetzung von #36 kurz bestätigen lassen.

### Nicht übernommen

- — (alle Anforderungen zugeordnet)

---

## 🚀 Releases

| Version | Datum | Inhalt |
| ------- | ----- | ------ |
| v1.2.1 | 18.06.2026 | Letzter Release vor dem Rückstand |
| v1.3.0 | 03.09.2026 | #6, #11, #15, #37, #40 — plus ~50 unveröffentlichte Commits |
| v1.3.1 | 03.09.2026 | #42 Auto-Update repariert. **Einmalig manuell `Setup.exe` ausführen** — die alte, installierte Version kann sich nicht selbst aktualisieren. |
| v1.3.2 | 04.09.2026 | #45 Stille Fehler behoben (PRs #46, #47 inkl. zwei Codex-Review-Runden). Am selben Tag mit gekürzten Release Notes neu geschnitten (PR #48). |

---

## 🔢 Versionierung

**Entschieden 2026-09-10.**

| Stelle | Bedeutung |
| ------ | --------- |
| `x.X.x` **Minor** | Alles, was beim Nutzer ankommt. Jeder Release an den Chef und das Team ist ein Minor. |
| `x.x.X` **Patch** | Nur Entwickler-Zwischenstände, nichts davon wird ausgeliefert. |

Deshalb wurde v1.3.3 zu **v1.4.0**: der Kategorien-Umbau war nie beim Nutzer und
bekommt keinen eigenen Release, sondern geht in v1.4.0 auf.

---

## 🎯 Release-Plan

Stand 2026-09-23 (v1.5.0), sonst 2026-09-10.

### v1.4.0 — ausgeliefert · fertig

Enthält **rückwirkend den nie ausgelieferten Kategorien-Umbau** (#34, #35, #50–#53).

| # | Titel | Status |
| - | ----- | ------ |
| [#63](https://github.com/jonasyr/Platee.Johann/issues/63) | Tage verschwanden bei „alles erledigt" | ✅ |
| [#62](https://github.com/jonasyr/Platee.Johann/issues/62) | „+ Neues Element" entfernt, Diktieren volle Breite | ✅ |
| [#66](https://github.com/jonasyr/Platee.Johann/issues/66) | Aufgaben-Prompt: Zusammenfassung + abhakbare Aufgaben | ✅ |
| [#59](https://github.com/jonasyr/Platee.Johann/issues/59) | Abschnitte heißen „Vorlagen" | ✅ |
| [#67](https://github.com/jonasyr/Platee.Johann/issues/67) | `gpt-transcribe` + `gpt-5.6-luna` | ✅ |
| [#58](https://github.com/jonasyr/Platee.Johann/issues/58) | Fremdsprachige Diktate ergeben deutsche Einträge | ✅ |

**Auf Wunsch des Chefs gestrafft:** #56 und #55 wurden nach v1.5.0 verschoben, damit die
neuen Modelle schnell beim Team sind. #55 (Löschen) bewusst nicht unter Zeitdruck — es ist
ein unwiderruflicher Pfad, bei dem wiederverwendete Sequenznummern Einträge überschreiben
könnten.

### v1.5.0 — E-Mail, Modellwahl, Feinschliff

~~[#57](https://github.com/jonasyr/Platee.Johann/issues/57) Outlook-Knöpfe~~ **erledigt** (PR #87, #90, #91) ·
~~[#56](https://github.com/jonasyr/Platee.Johann/issues/56) Kopiersymbol je Abschnitt~~ **erledigt** (PR #95) ·
~~[#55](https://github.com/jonasyr/Platee.Johann/issues/55) Einträge löschen~~ **erledigt** (PR #98) ·
~~[#97](https://github.com/jonasyr/Platee.Johann/issues/97) Knöpfe ohne WPF-Hellblau, Kontraste~~ **erledigt** (PR #99) ·
~~[#100](https://github.com/jonasyr/Platee.Johann/issues/100) Liste abgleichen statt neu laden~~ **erledigt** (PR #101, inkl. WPF-Test-Deadlock + CI-Härtung) ·
~~[#96](https://github.com/jonasyr/Platee.Johann/issues/96) Eintragsliste abgeschnitten~~ **erledigt** (PR #102, inkl. Doppelklick auf die Trennlinien) ·
~~[#71](https://github.com/jonasyr/Platee.Johann/issues/71) Modell in den Einstellungen wählbar~~ **erledigt** ·
~~[#73](https://github.com/jonasyr/Platee.Johann/issues/73) Prompts für GPT-5.6 überarbeiten~~ **erledigt** (PR #85, #91) ·
~~[#83](https://github.com/jonasyr/Platee.Johann/issues/83) PDF: verschachtelte Listen~~ **erledigt** (PR #86) ·
~~[#88](https://github.com/jonasyr/Platee.Johann/issues/88) Codex-Befunde~~ **erledigt** (PR #89) ·
~~[#84](https://github.com/jonasyr/Platee.Johann/issues/84) Live-Test der Katalogmodelle flackert~~ **erledigt** (PR #93) ·
~~[#77](https://github.com/jonasyr/Platee.Johann/issues/77) Aufnahmen über 25 MB abfangen~~ **erledigt** (PR #105) ·
~~[#106](https://github.com/jonasyr/Platee.Johann/issues/106) Gescheitertes Diktat sichern statt löschen~~ **erledigt** (PR #108) ·
~~[#78](https://github.com/jonasyr/Platee.Johann/issues/78) Release-Notes-Knopf~~ **erledigt** (PR #104) ·
~~[#79](https://github.com/jonasyr/Platee.Johann/issues/79) Layout Vorlagen-Einstellungen~~ **erledigt** (PR #94) ·
[#111](https://github.com/jonasyr/Platee.Johann/issues/111) UI-Automation + Audit — PR #113

**Noch offen:** [#114](https://github.com/jonasyr/Platee.Johann/issues/114) „Global“ speichert persönliche Vorlagen in die Team-Datei ·
[#115](https://github.com/jonasyr/Platee.Johann/issues/115) E-Mail-Anrede mit Vornamen · [#116](https://github.com/jonasyr/Platee.Johann/issues/116) Titel erfindet Wertung ·
[#112](https://github.com/jonasyr/Platee.Johann/issues/112) Transkript: neue Zeile nach jedem Satzende

### v1.6.0 — Diktieren

**Aus dem Audit v1.5.0** (`docs/audit/2026-09-24-v1.5.0.md`):
- Inhalt: [#117](https://github.com/jonasyr/Platee.Johann/issues/117) Korrekturliste unzuverlässig · [#118](https://github.com/jonasyr/Platee.Johann/issues/118) erfundene Aufgaben und
  Platzhalter.
- Oberfläche:
  - [#119](https://github.com/jonasyr/Platee.Johann/issues/119) Barrierefreiheit (Screenreader-Namen, Kontrast)
  - [#120](https://github.com/jonasyr/Platee.Johann/issues/120) leere Aufnahme ohne Titel und Hinweis
  - [#121](https://github.com/jonasyr/Platee.Johann/issues/121) PDF-Reihenfolge
  - [#122](https://github.com/jonasyr/Platee.Johann/issues/122) Scrollposition
  - [#123](https://github.com/jonasyr/Platee.Johann/issues/123) neue Vorlage erst nach Eintragswechsel
  - [#124](https://github.com/jonasyr/Platee.Johann/issues/124) Viewer-Modus sperrt lokale Aktionen
  - [#125](https://github.com/jonasyr/Platee.Johann/issues/125) Tab-Reihenfolge
  - [#126](https://github.com/jonasyr/Platee.Johann/issues/126) englische Fehlermeldung
- Sammel-Issues: [#127](https://github.com/jonasyr/Platee.Johann/issues/127) Kosmetik · [#128](https://github.com/jonasyr/Platee.Johann/issues/128) Vorschläge.

Unabhängig davon: [#103](https://github.com/jonasyr/Platee.Johann/issues/103) `gpt-transcribe`
mit Kontext (Korrekturliste als `keywords`, `languages`, `prompt`) — aus #77 abgespalten, nur mit
Messlauf. [#107](https://github.com/jonasyr/Platee.Johann/issues/107) Große Aufnahmen verarbeiten
— erst Sprach-Bitrate senken (Messlauf), sonst an Pausen teilen; gesicherte Diktate erneut
verarbeiten (passt zu #36).

Reihenfolge zwingend: [#64](https://github.com/jonasyr/Platee.Johann/issues/64) Schema v5
→ [#36](https://github.com/jonasyr/Platee.Johann/issues/36) Diktier-Popup
(+ [#61](https://github.com/jonasyr/Platee.Johann/issues/61))
→ [#65](https://github.com/jonasyr/Platee.Johann/issues/65) „nicht umgesetzt"
→ [#18](https://github.com/jonasyr/Platee.Johann/issues/18) paralleles Diktieren.

⚠ **Startet erst, wenn der Chef #36 in der neuen Form bestätigt hat.**

**#36 enthält seit 2026-09-11 den Halte-Knopf** neben Stopp: Pause während des Diktierens,
um kurz nachzuschlagen. Recorder-seitig nur ein Flag im `DataAvailable`-Handler — `WasapiCapture`
läuft weiter, ein einziger `WaveFileWriter` über die ganze Aufnahme. Der `RecordingDuration`-Timer
muss mitpausieren, sonst laufen angezeigte und gemessene Dauer auseinander.

### v1.7.0 — Zusammenführen

[#54](https://github.com/jonasyr/Platee.Johann/issues/54) Einträge zusammenführen.

### v1.8.0 — Härtung

[#8](https://github.com/jonasyr/Platee.Johann/issues/8) FileSystemWatcher ·
[#10](https://github.com/jonasyr/Platee.Johann/issues/10) API-Key per DPAPI ·
[#38](https://github.com/jonasyr/Platee.Johann/issues/38) Markdown durchgängig.

### Ohne Termin

[#60](https://github.com/jonasyr/Platee.Johann/issues/60) Ariadne-Integration (Epic).

---

## 🧠 Prompts: die Team-Datei ist die Wahrheit

`Z:\12_Tools\Peano\Johann\prompts.json` besitzt den Wortlaut aller neun Prompts und
gewinnt zur Laufzeit immer. Die `SummaryPrompts`-Konstanten sind nur Startwert für
Neuinstallationen und Rückfall ohne Share.

➡ **Jede Prompt-Änderung geht in beide.** `TeamPromptDriftTests` bewacht das und
überspringt sich still, wenn der Share fehlt.

⚠ Kein Client darf die Team-Datei automatisch umschreiben. `PromptDefaultsMigration` war
genau das und wurde in v1.4.0 gelöscht.

---

## ❓ Offene Fragen an den Chef

1. **Ist #36 richtig verstanden?** Vorlagen-Auswahl im Aufnahme-Fenster statt nachträglich
   neben dem Transkript. *(blockiert v1.6.0)*

## ✅ Entschieden (2026-09-10)

- **Option A** für die Abschnitts-Auswahl: am Eintrag speichern (#64)
- **`EntryType` bei Diktaten immer `Projekt`**, Typ-Autoerkennung nur für den Watch-Folder
- **PDF nur an der internen Aufgaben-Mail**, nicht an der externen
- **Kostenanzeige verworfen** — Issue gelöscht
- **Transkript bleibt in der gesprochenen Sprache**, alle generierten Abschnitte deutsch
- **Zentraler „Kopieren"-Knopf bleibt**, Abschnitts-Symbole kommen daneben

## 🚦 Vor jedem Release

- Installer bauen: `.\build-installer.ps1 -Version 1.x.0`
- Auto-Update gegen die zuletzt ausgelieferte Version prüfen
- Sichtbare Verhaltensänderungen gehören in die Release Notes
- UI-Suite grün: `pwsh scripts/run-ui-tests.ps1` (belegt den Desktop, nur wenn niemand tippt).
  Der CI-Job `ui-tests` blockiert nicht und ersetzt den lokalen Lauf nicht.
