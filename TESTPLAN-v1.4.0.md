# Testplan v1.4.0 — Kategorien & Auto/On-Demand

> Stand: 2026-09-07 · Branch `feat/51-category-model` (21 Commits, 422 Tests grün, nicht gepusht)
> Backup: `C:\Users\JW\Documents\Johann_Backup_v1.4.0_2026-09-07_150924`
> (Pfad steht auch in `Documents\Johann_Backup_LATEST.txt`)

## Vor dem Testen

Der Build muss neu sein — der Fix für die Kategorien-Persistenz ist erst seit Commit `397ed32` drin:

```powershell
dotnet build
dotnet run --project Platee.Johann.UI
```

⚠️ **Die Titelleiste zeigt noch „v1.3.2"**, auch im neuen Build. Die Version ist noch nicht
hochgezogen — nicht am Titel orientieren, um Builds zu unterscheiden.

Logs mitlaufen lassen: `C:\Peano\Platee.Johann\logs\johann-crash-*.log`

---

## Bereits erledigt (2026-09-07)

| Test | Ergebnis |
| --- | --- |
| **A1** Baseline v1.3.2 | 40 s (15:32:10 → 15:32:50) |
| **A2** Schema | `"schemaVersion": 3`, alle 8 Abschnitte gefüllt, kein `customSections` ✅ |
| **A3** Referenz-Specimen | im Backup gesichert, Hash geprüft ✅ |
| **B1–B4** First-Run-Dialog | erscheint einmal, nach Neustart weg ✅ |
| **B6** Empfohlenes Preset | 25 s (15:38:40 → 15:39:05) — **~38 % schneller** ✅ |
| **E7** Auto / Auf-Knopfdruck-Umschalter | funktioniert ✅ |
| **E1/E3** Persönliche Kategorie | funktioniert end-to-end ✅ |
| **F1** „Generieren" bei globaler Kategorie | funktioniert (war on-demand, kein Bug) ✅ |

**Gefundener Bug, behoben:** `JsonPromptSettingsRepository` mappt über ein handgeschriebenes
`PromptDto`, in dem `CustomCategories` fehlte — Kategorien wurden nie gespeichert und wären beim
nächsten Neustart weg gewesen. Behoben in `397ed32` mit drei Round-Trip-Tests.

**Konsequenz:** Die am 07.09. angelegten Testkategorien sind *nicht* gespeichert. Nach dem Rebuild
neu anlegen.

---

## 1 · Zuerst — den behobenen Bug verifizieren (5 Min)

- [ ] **1.1** Persönliche Kategorie anlegen und speichern
- [ ] **1.2** Datei prüfen:
      ```powershell
      (Get-Content "$env:USERPROFILE\Documents\Johann\prompts.personal.json" -Raw | ConvertFrom-Json).customCategories
      ```
      Muss die Kategorie auflisten. **Vor dem Fix war das leer — genau das war der Bug.**
- [ ] **1.3** **App neu starten** → Kategorie ist noch da
- [ ] **1.4** Auf „Automatisch" stellen, MP3 in den Eingang → Abschnitt wird erzeugt

> Wenn 1.2 oder 1.3 fehlschlägt: abbrechen und melden. Alles andere ist dann zweitrangig.

---

## 2 · Was nur manuell prüfbar ist (15 Min)

- [ ] **2.1** Kategorie **umbenennen** → bereits erzeugter Text bleibt sichtbar
      *(Ids werden einmal vergeben; verschwindet der Text, ist die Id-Logik kaputt)*
- [ ] **2.2** Kategorie **löschen** → alter Text erscheint als „Nicht mehr konfiguriert",
      **ohne** „Generieren"-Button
- [ ] **2.3** **„Generieren" schnell doppelt klicken** → nur eine Generierung.
      *Der Test, der sonst unbemerkt Geld kostet.*
- [ ] **2.4** **PDF und HTML** exportieren → eigener Abschnitt erscheint mit **Namen, nicht `custom.xyz`**
- [ ] **2.5** Eigenen Abschnitt links abwählen → fehlt im PDF/HTML
- [ ] **2.6** Heutige `_ItemÜbersicht.html` → Name statt Id
- [ ] **2.7** Kontextmenü → „Neu generieren" auf **Zusammenfassung** →
      regeneriert die *ausführliche* Zusammenfassung.
      *(Geerbte Namensvertauschung, die die Id-Migration exakt erhalten musste — hier
      würde sie auffallen.)*

---

## 3 · Team-Datei — gefahrlos testen

**Nicht** gegen die Live-Datei auf `Z:` testen. Kopie anlegen und darauf umstellen:

```powershell
Copy-Item 'Z:\12_Tools\Peano\Johann\prompts.json' "$env:USERPROFILE\Documents\prompts_TEST.json"
```
Dann diesen Pfad in den Einstellungen als Team-Prompt-Datei eintragen.

- [ ] **3.1** **Globale** Kategorie anlegen → `prompts_TEST.json` bekommt `customCategories`
- [ ] **3.2** Datei mit v1.3.2 öffnen → lädt normal, keine Fehlermeldung,
      **kein** `prompts.corrupt-*.json` daneben
- [ ] **3.3** In v1.3.2 als Admin einen Prompt speichern → erneut prüfen:
      `customCategories` ist **weg**

**3.3 ist eine bekannte, nicht behebbare Einschränkung**, keine Regression: v1.3.2 kennt den
Schlüssel nicht und schreibt die Datei ohne ihn zurück. v1.3.2 ist bereits ausgeliefert und kann
das nicht nachträglich lernen.

→ **Keine globalen Kategorien anlegen, solange nicht das ganze Team auf 1.4.0 ist.**
Persönliche Kategorien sind immer sicher (`prompts.personal.json`, wird von 1.3.2 nie angefasst).

Danach wieder auf `Z:` zurückstellen.

---

## 4 · Einträge über Versionen hinweg (Rollback-Fall)

Einträge liegen benutzerlokal, daher nur beim Downgrade relevant.

- [ ] **4.1** In 1.4.0 einen eigenen Abschnitt erzeugen → JSON zeigt `"schemaVersion": 4`
      und einen `"customSections"`-Block
- [ ] **4.2** Denselben Eintrag in 1.3.2 öffnen → alle 8 eingebauten Abschnitte rendern, kein Absturz
- [ ] **4.3** In 1.3.2 „Als erledigt markieren" (erzwingt ein Speichern) → JSON erneut prüfen:
      **`customSections` weg, die 8 eingebauten intakt**

Das ist der dokumentierte, akzeptierte Restverlust. Prüfen, dass es genau so und nicht schlimmer ist.

---

## 5 · Regressionen

- [ ] **5.1** Watch-Ordner verarbeitet MP3s weiterhin unbeaufsichtigt
- [ ] **5.2** 🎙 Diktieren
- [ ] **5.3** Transkript bearbeiten + neu generieren
- [ ] **5.4** Drag & Drop PDF-Export in den Explorer
- [ ] **5.5** `Strg +` / `Strg -` / `Strg 0` / `Strg+Mausrad`
- [ ] **5.6** Update-Benachrichtigung (#42)
- [ ] **5.7** Korrekturliste greift auch bei eigenen Kategorien („Piano" → „Peano")
- [ ] **5.8** Keine neuen Einträge in `C:\Peano\Platee.Johann\logs\`

---

## 6 · Rollback

- [ ] **6.1** v1.3.2 über den neuen Build installieren → startet, Einträge öffnen,
      kein Corrupt-Settings-Dialog
- [ ] **6.2** Wieder 1.4.0 installieren → `sectionModes` kann von 1.3.2 verworfen worden sein,
      der First-Run-Dialog kann erneut kommen. Akzeptabel — prüfen, dass nichts Schlimmeres passiert.

---

## Priorität bei wenig Zeit

**1.3, 2.3, 2.7** — in dieser Reihenfolge.

- **1.3** ist der gerade behobene Bug; ein grüner Test ersetzt keinen echten Neustart.
- **2.3** fällt sonst niemandem auf, kostet aber bei jedem Fehlklick echtes Geld.
- **2.7** ist die einzige Stelle, an der die Id-Migration zwei Abschnitte stillschweigend
  vertauscht haben könnte.

## Vor dem Release noch offen

- [ ] Versionsnummer auf 1.4.0 ziehen (Titelleiste zeigt noch 1.3.2)
- [ ] `RELEASE_NOTES.md`: Moduswechsel (8 → 4 automatisch), eigene Kategorien, Passwort entfällt
- [ ] `HANDBUCH.html` + `README.md`: Abschnitt „Eigene Kategorien"
- [ ] `.\build-installer.ps1 -Version 1.4.0`
- [ ] Auto-Update gegen installierte 1.3.2 verifizieren
