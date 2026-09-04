# Was ist neu?

## Version 1.3.2

**Fehler, die bisher niemand gesehen hat**

Diese Version behebt keine sichtbaren Funktionen, sondern eine Reihe von Fällen, in
denen Johann bisher stillschweigend etwas anderes getan hat als erwartet – ohne
Meldung, ohne Protokolleintrag.

- **Zentrale Prompts bleiben erhalten, wenn das Netzlaufwerk fehlt.** War `Z:` nicht
  erreichbar (z. B. ohne VPN), hat Johann kommentarlos auf die eingebauten
  Standard-Prompts zurückgegriffen – die Ergebnisse wichen dann von denen der
  Kolleginnen und Kollegen ab, ohne dass es jemand bemerken konnte. Johann legt jetzt
  bei jedem erfolgreichen Start eine lokale Kopie der Team-Prompts an und verwendet
  diese, wenn das Laufwerk gerade nicht erreichbar ist. Ein Hinweis beim Start sagt,
  was verwendet wurde und warum.
- **Defekte Einstellungsdateien werden nicht mehr überschrieben.** Eine beschädigte
  `settings.json` oder `prompts.json` führte dazu, dass alle Einstellungen auf
  Standardwerte zurückfielen und beim nächsten Speichern endgültig verloren waren.
  Johann legt jetzt vor dem Zurückfallen eine Sicherungskopie an
  (`…​.corrupt-<Zeitstempel>.json`) und meldet den Fehler beim Start.
- **Fehlgeschlagener PDF-Export per Drag & Drop wird gemeldet.** Bisher passierte
  beim Ziehen eines Eintrags in einen Ordner im Fehlerfall einfach nichts.
- **Das Handbuch nennt den echten Grund**, wenn es sich nicht öffnen lässt, statt
  immer „Dokumentation nicht gefunden" anzuzeigen.
- **Übersprungene Einträge bei der einmaligen JobId-Umstellung** landen jetzt im
  Fehlerprotokoll, statt unbemerkt zurückzubleiben.

---

## Version 1.3.1

**Automatische Updates funktionieren wieder**

- Johann hat seit Version 1.1.0 nie auf neue Versionen hingewiesen. Die Update-Prüfung wurde beim normalen Programmstart übersprungen und der Fehler dabei stillschweigend verschluckt.
- Ab dieser Version meldet sich Johann wieder automatisch, sobald eine neue Version im Netzlaufwerk bereitliegt.
- **Einmalig nötig:** Da alle bisher installierten Versionen den Fehler enthalten, können sie sich nicht selbst aktualisieren. Bitte einmal `Z:\12_Tools\Peano\Johann\Setup.exe` von Hand ausführen. Danach laufen alle weiteren Updates wieder automatisch.
- Fehler bei der Update-Prüfung werden ab sofort im Fehlerprotokoll festgehalten, statt unbemerkt zu verschwinden.

---

## Version 1.3.0

**In-App-Diktat per Mikrofon**

- Neue Schaltfläche „🎙 Diktieren" in der Eintrags-Liste: Aufnahme direkt aus der App starten, ohne vorher eine MP3-Datei auf dem Smartphone aufzunehmen.
- Während der Aufnahme wird ein roter Puls-Indikator mit laufendem Timer angezeigt. „■ Stop" beendet die Aufnahme und startet automatisch die Transkription und KI-Zusammenfassung.

**Transkript bearbeiten und neu generieren**

- Das Transkript kann jetzt direkt in der Detailansicht bearbeitet werden: Stift-Symbol (✏) neben „Transkript" klicken, Text korrigieren und „Neu generieren" klicken.
- Alle KI-Abschnitte werden aus dem korrigierten Text neu erstellt. Bei Fehlern bleibt die Bearbeitung erhalten.
- PDF, HTML und Kopieren verwenden automatisch den korrigierten Text, wenn vorhanden.
- Bearbeitete Transkripte sind mit „(bearbeitet)" gekennzeichnet.

**Korrekturliste für Whisper-Fehler**

- Neue Korrekturliste in den Einstellungen: Häufig falsch erkannte Wörter können als Korrekturpaare hinterlegt werden (z. B. Piano → Peano). Die Korrekturen werden automatisch bei der KI-Zusammenfassung berücksichtigt.
- Vier Standardkorrekturen sind bereits voreingestellt und können beliebig ergänzt oder entfernt werden.

**Zoom-Tastenkürzel**

- Die Detailansicht kann jetzt per Tastenkürzel gezoomt werden: `Strg++` / `Strg+-` zum Vergrößern/Verkleinern, `Strg+0` zum Zurücksetzen auf 100 %.
- `Strg+Mausrad` zoomt ebenfalls in der Detailansicht.
- Tooltips an den Zoom-Buttons zeigen die Tastenkürzel an.

**Diverse kleine Fehlerbehebungen und Verbesserungen**

---

## Version 1.2.1

**Prompts werden jetzt zentral verwaltet**

- Alle Mitarbeiter nutzen ab sofort die gleichen Prompt-Vorlagen. Diese werden beim Start automatisch von `Z:\12_Tools\Peano\Johann\prompts.json` geladen.

- Wenn Sie einen Prompt testweise anpassen möchten, können Sie das weiterhin in den Einstellungen tun. Die Änderung gilt dann nur für Sie persönnlich und bis zum nächsten App-Neustart.

**Neuer Admin-Modus**

- In den Einstellungen gibt es unten links einen passwortgeschützten "Admin"-Button. Damit können berechtigte Personen die Prompt-Vorlagen, sowie ihren Speicherort dauerhaft für alle Mitarbeiter ändern.

**Dokumentation aktualisiert**

- Das Handbuch ("?" Button oben rechts) wurde auf den aktuellen Stand gebracht.

**Verbesserungen und Fehlerbehebungen**

- Der Button „Erledigt" sitzt jetzt oben links und bleibt beim Scrollen immer sichtbar. Er wird nicht mehr von Meldungen überdeckt, während ein neuer Eintrag eingelesen wird.
- Doppelte Eintragsnummern können nicht mehr auftreten, wenn Johann auf mehreren Rechnern gleichzeitig in dasselbe Verzeichnis schreibt.
- Ressourcen werden jetzt auch dann sauber freigegeben, wenn das Programm unerwartet beendet wird.

---

## Version 1.1.0

**UI-Redesign**

- Die gesamte UI sowie UX wurde angepasst/verbessert.
