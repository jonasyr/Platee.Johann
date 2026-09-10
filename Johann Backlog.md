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

## 📌 Aktueller Stand (2026-09-10)

**v1.3.3 liegt in `main`, getaggt als `v1.3.3-dev`, ist aber NICHT veröffentlicht.**
Kein Installer gebaut, kein GitHub-Release — Nutzer laufen weiter auf v1.3.2.

Fertig und gemergt: #50, #51, #52, #53 — eigene Kategorien (persönlich + global),
Auto/Auf-Knopfdruck pro Abschnitt, Admin-Passwort entfernt, Abschnitts-Sichtbarkeit
mit Gruppierung, Namensgrabsteine für gelöschte Kategorien. 453 Tests grün.

Der Testplan (`TESTPLAN-v1.3.3.md`) ist bis auf die Rollback-Abschnitte durch;
zwölf dabei gefundene Fehler wurden behoben.

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

Abgleich Backlog ↔ [GitHub Issues](https://github.com/jonasyr/Platee.Johann/issues) (Stand: 2026-09-10).

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
| [#36](https://github.com/jonasyr/Platee.Johann/issues/36) | Diktier-Popup (2026-09-10 neu gefasst) | **offen** | ☐ Diktier-Popup |
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

## 🎯 Priorisierung & Reihenfolge

Stand 2026-09-10, nach dem Meeting mit dem Chef. Alle Issues tragen auf GitHub
`priority:`, `size:` und `area:`-Labels.

### Block A — Diktieren (der große Brocken)

Die Reihenfolge ist **zwingend**, nicht beliebig: ohne das Datenmodell lässt
sich „nicht umgesetzt" nicht von „ausgeblendet" unterscheiden.

| # | Titel | Prio | Größe | Abhängig von |
| - | ----- | ---- | ----- | ------------ |
| [#64](https://github.com/jonasyr/Platee.Johann/issues/64) | Abschnitts-Auswahl am Eintrag speichern (Schema v5) | P1 | M | — |
| [#36](https://github.com/jonasyr/Platee.Johann/issues/36) | Diktier-Popup, Aufnahme startet sofort | P1 | M | #64 |
| [#61](https://github.com/jonasyr/Platee.Johann/issues/61) | Profil Diktat vs. Datei → Vorauswahl | P1 | S | #64, Teil von #36 |
| [#62](https://github.com/jonasyr/Platee.Johann/issues/62) | „+ Neues Element" entfernen | P1 | S | Teil von #36 |
| [#59](https://github.com/jonasyr/Platee.Johann/issues/59) | Abschnitte „Vorlagen" nennen | P3 | S | fällt bei #36 ab |
| [#65](https://github.com/jonasyr/Platee.Johann/issues/65) | „nicht umgesetzt", selektiv neu generieren | P1 | M | #64 |

Bei #65 sollte **#56** (Kopiersymbol je Abschnitt) opportunistisch mitlaufen —
das Abschnitts-Rendering wird dort ohnehin angefasst.

### Block B — E-Mail

| # | Titel | Prio | Größe | Abhängig von |
| - | ----- | ---- | ----- | ------------ |
| [#66](https://github.com/jonasyr/Platee.Johann/issues/66) | Aufgaben-Prompt: Zusammenfassung + Aufgaben | P1 | S | — |
| [#57](https://github.com/jonasyr/Platee.Johann/issues/57) | Aufgaben- und E-Mail-Knopf öffnen Outlook direkt | P1 | M | #66 |

#57 ist von S auf M gewachsen: `mailto:` kann weder Anhang noch Signatur, das
wird ein Mechanismuswechsel auf Outlook-COM.

### Block C — Modell & Kosten

| # | Titel | Prio | Größe | Abhängig von |
| - | ----- | ---- | ----- | ------------ |
| [#67](https://github.com/jonasyr/Platee.Johann/issues/67) | Aktuelle OpenAI-Modelle (`gpt-transcribe`, modernes Textmodell) | P1 | M | Spike: Audiodauer |
| [#68](https://github.com/jonasyr/Platee.Johann/issues/68) | Kostentelemetrie | — | — | **Entwurf, nicht freigegeben** |

### Block D — Fehler

| # | Titel | Prio | Größe |
| - | ----- | ---- | ----- |
| [#63](https://github.com/jonasyr/Platee.Johann/issues/63) | Tage verschwinden, wenn alles erledigt ist | P1 | S |
| [#18](https://github.com/jonasyr/Platee.Johann/issues/18) | Paralleles Verarbeiten (vom Chef erneut gemeldet) | P1 | M |
| [#8](https://github.com/jonasyr/Platee.Johann/issues/8) | FileSystemWatcher verliert Events | P2 | M |

### Nicht in diesem Zyklus

[#55](https://github.com/jonasyr/Platee.Johann/issues/55) Löschen ·
[#54](https://github.com/jonasyr/Platee.Johann/issues/54) Zusammenführen ·
[#58](https://github.com/jonasyr/Platee.Johann/issues/58) Übersetzung ·
[#38](https://github.com/jonasyr/Platee.Johann/issues/38) Markdown ·
[#10](https://github.com/jonasyr/Platee.Johann/issues/10) DPAPI ·
[#60](https://github.com/jonasyr/Platee.Johann/issues/60) Ariadne-Epic

### Offene Fragen an den Chef

1. **PDF beim externen E-Mail-Knopf** ja oder nein. Bis zur Klärung ohne bauen,
   aber so, dass das Nachrüsten kein großer Eingriff ist.
2. **Kostenanzeige** (#68) — dauerhaft in der App oder reicht das OpenAI-Dashboard?
   Der Verbrauch der 100 € ist nur dort einsehbar, nicht aus dem Repo.
3. **Ist #36 in der neuen Form richtig verstanden?** Auswahl im Aufnahme-Fenster
   statt nachträglich neben dem Transkript.

### Bereits entschieden (2026-09-10)

- **Option A** für die Abschnitts-Auswahl: am Eintrag speichern, drei
  Checkbox-Ebenen auf eine reduzieren (#64).
- **`EntryType` bei Diktaten ist immer `Projekt`.** Typ-Autoerkennung nur noch
  für den Watch-Folder.
- **Rollout-Entscheidung vertagt** — 1.3.3 sofort ausliefern oder alles in ein
  1.3.4 bündeln, wird nach der Backlog-Klärung entschieden.

### Vor jedem Release

- Installer bauen: `.uild-installer.ps1 -Version 1.3.x`
- Auto-Update gegen ein installiertes v1.3.2 prüfen
- Sichtbare Verhaltensänderungen gehören in die Release Notes
