# Was ist neu?

## Version 1.4.0

**Bessere und schnellere Transkripte.**

- Johann nutzt jetzt die aktuellen OpenAI-Modelle. Die Spracherkennung ist deutlich
  genauer und pro Minute sogar günstiger als bisher, die Zusammenfassungen laufen auf
  einem spürbar stärkeren Modell.
- Diktate in einer anderen Sprache werden korrekt erkannt und ergeben trotzdem einen
  deutschen Eintrag. Das Transkript bleibt in der gesprochenen Sprache, alles andere
  ist deutsch. Vorher war Johann fest auf Deutsch eingestellt.

**Aufgaben lesen sich jetzt wie Aufgaben.**

- Der Aufgaben-Abschnitt beginnt mit einer kurzen Zusammenfassung und listet darunter
  die Aufgaben — je eine Zeile, kurz und abhakbar statt als Textblock.

**Behobener Fehler: verschwundene Tage.**

- Wurden alle Einträge eines Tages abgehakt, verschwand der Tag aus der Liste links —
  bei mehreren Tagen sah es aus, als wären Einträge verloren. Der Filter blendet
  erledigte Tage weiterhin aus, aber der gerade geöffnete Tag bleibt immer sichtbar,
  und unter der Liste steht, wie viele Tage ausgeblendet sind.

**Aufgeräumte Oberfläche.**

- „+ Neues Element" ist entfallen, „🎙 Diktieren" nimmt jetzt die volle Breite ein.
- Was bisher „Kategorien" hieß, heißt jetzt durchgängig **Vorlagen**. „Typ" bezeichnet
  weiterhin die Art des Eintrags. Vorher meinten beide Wörter dasselbe und
  Verschiedenes zugleich.

**Eigene Vorlagen.**

- Neben den acht eingebauten Abschnitten lassen sich jetzt eigene Vorlagen anlegen —
  persönlich (nur für dich) oder global für das ganze Team.
- Jede Vorlage läuft entweder **automatisch** bei jedem Eintrag mit oder erst **auf
  Knopfdruck**. Standardmäßig sind nur vier der eingebauten Abschnitte automatisch,
  statt bisher acht — ein Eintrag ist dadurch spürbar schneller fertig.
- Beim ersten Start fragt Johann einmalig, ob die neue Aufteilung übernommen werden soll.

**Kein Admin-Passwort mehr.**

- Prompts werden nicht mehr per Passwort freigeschaltet. Stattdessen wählst du beim
  Speichern aus, ob die Änderung persönlich gilt oder für das ganze Team.
- Persönliche Prompt-Änderungen überleben jetzt den Neustart.

**Abschnitte ein- und ausblenden.**

- Die Liste links steuert jetzt auch eigene Vorlagen — getrennt nach eigenen,
  Team- und gelöschten Vorlagen. Was abgewählt ist, fehlt in der Ansicht, im PDF,
  im HTML und beim Kopieren.
- Text einer gelöschten Vorlage geht nicht verloren: er bleibt unter seinem
  ursprünglichen Namen sichtbar und lässt sich ausblenden.

---

## Version 1.3.2

**Defekte Einstellungsdateien werden nicht mehr überschrieben.**

- Eine beschädigte `settings.json` oder `prompts.json` führte dazu, dass alle Einstellungen auf Standardwerte zurückfielen und beim nächsten Speichern endgültig verloren waren. Johann legt jetzt vor dem Zurückfallen eine Sicherungskopie an (`….corrupt-<Zeitstempel>.json`) und meldet den Fehler beim Start.

**Fehlgeschlagener PDF-Export per Drag & Drop wird gemeldet.**

- Bisher passierte beim Ziehen eines Eintrags in einen Ordner im Fehlerfall einfach nichts.

---

## Version 1.3.1

**Automatische Updates funktionieren wieder**

- Johann hat seit Version 1.1.0 nie auf neue Versionen hingewiesen. Die Update-Prüfung wurde beim normalen Programmstart übersprungen und der Fehler dabei stillschweigend verschluckt.
- Ab dieser Version meldet sich Johann wieder automatisch, sobald eine neue Version im Netzlaufwerk bereitliegt.

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
