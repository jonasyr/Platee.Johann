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
| ☐      | Sequence Number Race Condition                                    | Doppelte Nummern / mögliche Datenüberschreibung | [[Code Audit Report#Finding 1 Sequence Number Race Condition Under Concurrent Processing]]                     |
| ✅      | Kein Feedback bei Input-Ordner Fehler                             | Fehler wird nicht an User propagiert            | [[Code Audit Report#Finding 2 Fire-and-Forget `Task.Run` in AudioWatcherService Swallows Exceptions Silently]] |
| ☐      | UI Deadlock beim Start                                            | Sync-Blocking von Async Code                    | [[Code Audit Report#Finding 4 Synchronous Blocking on Async Code at Startup]]                                  |
| ☐      | FileSystemWatcher verliert Events                                 | Dateien werden teilweise nicht verarbeitet      | [[Code Audit Report#Finding 7 `FileSystemWatcher` Misses Events Under Load and Has No Retry Logic]]            |
| ✅      | Analog Typ wird nicht erkannt                                     | Falsche Klassifikation von Entries              | [[Code Audit Report#Finding 12 `TypeExtractor` Missing "Analog" Keyword]]                                      |
| ✅      | Validation Bypass im Dialog                                       | DialogResult wird falsch gesetzt                | [[Code Audit Report#Finding 13 `NewEntryView` Dialog Result Logic Is Fragile]]                                 |
| ☐      | Inkonsistente Generierung bei Prompt-Änderung                     | Settings werden während Verarbeitung geändert   | [[Code Audit Report#Finding 16 `SettingsView` Is Non-Modal but Settings Changes Have No Undo]]                 |
| ✅      | Path Bug wenn neue Settings.json von anderem Rechner genutzt wird | Failed silently wenn Pfad nicht existiert       |                                                                                                                |
  
---  
## 🛠 Improvements  
  
| Status | Titel                                      | Beschreibung                                                                                                                                                                       | Link                                                                                                             |
| ------ | ------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| ✅      | Logging Pfad ändern                        | Logs nicht mehr auf Desktop sondern unter C:\Users\[User]\Peano\Johann\...                                                                                                         | —                                                                                                                |
| ✅      | Settings UX verbessern                     | Tabs springen nicht mehr                                                                                                                                                           | —                                                                                                                |
| ✅      | XSS Fix im HTML Renderer                   | Markdown → HTML absichern                                                                                                                                                          | [[Code Audit Report#Finding 5 XSS Vulnerability in HTML Renderers via Markdown Content]]                         |
| ☐      | API Key Sicherheit                         | Klartext → Verschlüsselung                                                                                                                                                         | [[Code Audit Report#Finding 6 API Key Stored in Plaintext `.env` File Without Encryption]]                       |
| ☐      | Dispose Handling verbessern                | Watcher sauber freigeben                                                                                                                                                           | [[Code Audit Report#Finding 8 `Dispose` Pattern Incomplete — `AudioWatcherService` Not Disposed on Crash Paths]] |
| ☐      | Exception Handling verbessern              | Keine leeren catch Blöcke                                                                                                                                                          | [[Code Audit Report#Finding 9 Swallowed Exceptions in Processing Pipeline Mask Root Causes]]                     |
| ✅      | HtmlEncode ersetzen                        | Built-in Encoding nutzen                                                                                                                                                           | [[Code Audit Report#Finding 10 `HtmlEncode` Is Manual and Incomplete]]                                           |
| ☐      | JSON Lookup optimieren                     | O(N) → effizienter Zugriff                                                                                                                                                         | [[Code Audit Report#Finding 11 JSON Repository Reads All Files Sequentially for `GetByJobIdAsync`]]              |
| ☐      | Duration Formatter zentralisieren          | Duplicate Code entfernen                                                                                                                                                           | [[Code Audit Report#Finding 14 Duplicated Duration Formatting Logic Across 4 Files]]                             |
| ☐      | Test Coverage erweitern                    | Integration Tests hinzufügen                                                                                                                                                       | [[Code Audit Report#Finding 15 No Test Coverage for `EntryProcessingService` Integration]]                       |
| ✅      | Prompts verbessern                         | Neue und bessere Global Defaults für die Prompts                                                                                                                                   |                                                                                                                  |
| ✅      | Anzahl unerledigter Einträge visualisieren | Neben dem<br>Datum, in Klammern, die Anzahl der Einträge, die noch nicht erledigt sind. Dann würde man sehen, ob<br>man da noch was vergessen hat.                                 |                                                                                                                  |
| ☐      | Optionsdateien trennen                     | Zwei Optionsdateien vorsehen: eine individuelle Datei für persönliche Einstellungen wie Allgemein und Verzeichnisse, sowie eine globale Prompt-Datei unter Z:\... für alle Nutzer. | —                                                                                                                |
| ☐      | Korrekturliste für Prompts                 | Eigene Prompt-Option „Korrekturliste“ ergänzen, damit häufige Korrekturen zentral gepflegt werden können, z. B. „Peano“ nicht als „Piano“ und „Neele“ nie als „Nele“.              | —                                                                                                                |
  
---  
  
## ✨ Features  
  
| Status | Titel                                    | Beschreibung                                                                                                                                                                                                                                                                                                                                                                                    | Link                                                                                                           |
| ------ | ---------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| ☐      | Parallel Processing                      | Mehrere Audios gleichzeitig verarbeiten                                                                                                                                                                                                                                                                                                                                                         | [[Code Audit Report#Finding 3 `SemaphoreSlim` in AudioWatcherService Serializes All Processing Unnecessarily]] |
| ☐      | Zoomen Shortcut                          | Zoomen mit Strg + und Strg - bzw auch Strg Mausrad ermöglichen sodass man nicht immer unten rechts + und - drücken muss.                                                                                                                                                                                                                                                                        |                                                                                                                |
| ☐      | Transkriptionstext bearbeiten            | Möglichkeit ergänzen, den Transkriptionstext nachträglich über einen Bearbeiten-/Bleistift-Button zu ändern und den Eintrag anschließend erneut generieren zu lassen. Hilfreich, um abgebrochene oder zusammengehörige Nachrichten zu verbinden, Inhalte zu entfernen oder Korrekturen am Transkript vorzunehmen.                                                                               |                                                                                                                |
| ☐      | Neue Einträge direkt in Johann diktieren | Möglichkeit ergänzen, direkt in Johann neue Einträge per Diktat zu erstellen. Dafür soll es ein neues Eingabefeld bzw. einen Bereich mit großem Startbutton geben. Nach dem Diktieren wird der Text transkribiert, anschließend stehen sowohl der Transkriptionstext als auch die ausführliche Zusammenfassung zur Verfügung. Weitere Zusammenfassungen sollen optional erstellt werden können. |                                                                                                                |
