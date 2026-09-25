# Was ist neu?

## Version 1.5.0

### Neu

- **Mails aus Outlook** – „**Aufgaben**“ öffnet die interne Mail an die Kollegen mit
  Begleittext, Aufgabenliste, PDF im Anhang und Ihrer Signatur. „**E-Mail**“ öffnet die
  förmliche Mail an Externe, ohne Anhang. Klappt im klassischen und im neuen Outlook.
  *Begleittext: Einstellungen → Allgemein.*
- **Einträge löschen** – per Rechtsklick, Taste **Entf** oder Knopf „Löschen“. Gelöschtes
  liegt 30 Tage im Ordner `output\_Papierkorb` und lässt sich von dort zurückholen.
- **Einzelne Abschnitte kopieren** – über das Kopiersymbol neben jeder Überschrift.
- **KI-Modell wählen** – unter Einstellungen → KI-Modell, mit Kosten je Diktat.
  Voreingestellt ist das günstige Standardmodell; die Wahl gilt nur für Sie.
- **„Neuigkeiten“** oben rechts zeigt diese Übersicht jederzeit wieder an.
- **Spaltenbreite per Doppelklick** – Doppelklick auf eine Trennlinie passt die Spalte an
  ihren Inhalt an, wie in Excel.

### Besser

- **Bessere Texte, rund ein Fünftel günstiger** – alle Vorlagen wurden neu gefasst. Die
  Mail siezt immer; die Gesprächsnotiz entsteht nur, wenn wirklich ein Gespräch
  stattfand; fehlt einem Abschnitt der Stoff, steht das da, statt dass etwas erfunden wird.
- **Sauberes PDF** – Fettdruck und eingerückte Unterpunkte in allen Abschnitten.
- **Ruhigere Liste** – Sortieren, Abhaken und Filtern flackern nicht mehr, der Eintrag
  bleibt ausgewählt. Lange Titel enden mit „…“ statt die Liste zu verbreitern.
- **„Kopieren“** nimmt jetzt genau das mit, was sichtbar ist – in derselben Reihenfolge.
- **Knöpfe** sind kontrastreicher und zeigen den Tastaturfokus deutlich.

### Behoben

- **Kein Diktat geht mehr verloren** – scheitert die Verarbeitung (z. B. ohne Internet),
  landet die Aufnahme in `output\_Diktate (nicht verarbeitet)`, statt gelöscht zu werden.
- **Sehr lange Aufnahmen** (über 25 MB, etwa 25 Minuten) meldet Johann vor dem Hochladen
  verständlich, statt mit einer technischen Fehlermeldung abzubrechen.
- **Eigene Vorlagen bleiben privat** – wer mit Ziel „Global“ speicherte, schrieb bisher
  auch seine persönlichen Vorlagen in die Team-Datei. Jetzt gehen nur globale Vorlagen dorthin.
- **Neuer Tag erscheint sofort** – der erste Eintrag eines neuen Tages war erst nach dem
  Wechsel auf einen anderen Tag in der Liste zu sehen.

## Version 1.4.0

### Neu

- **Eigene Vorlagen** – neben den acht eingebauten Abschnitten lassen sich eigene anlegen,
  persönlich oder für das ganze Team. Jede läuft **automatisch** oder erst **auf Knopfdruck**.
- **Schneller fertig** – standardmäßig laufen nur noch vier Abschnitte automatisch statt
  acht. Beim ersten Start fragt Johann einmal, ob Sie diese Aufteilung übernehmen.
- **Abschnitte ein- und ausblenden** gilt jetzt auch für eigene Vorlagen – in Ansicht,
  PDF, HTML und beim Kopieren. Text gelöschter Vorlagen bleibt lesbar.
- **Fremdsprachige Diktate** werden erkannt und ergeben trotzdem einen deutschen Eintrag;
  nur das Transkript bleibt in der gesprochenen Sprache.

### Besser

- **Neue OpenAI-Modelle** – genauere Transkripte, günstiger pro Minute und stärkere
  Zusammenfassungen.
- **Aufgaben** beginnen mit einer kurzen Zusammenfassung, darunter je Aufgabe eine
  abhakbare Zeile.
- **Kein Admin-Passwort mehr** – beim Speichern wählen Sie, ob eine Prompt-Änderung nur für
  Sie oder für das Team gilt. Persönliche Änderungen überleben den Neustart.
- **Aufgeräumt** – „+ Neues Element“ ist entfallen, „🎙 Diktieren“ hat die volle Breite.
  Aus „Kategorien“ wurden durchgängig **Vorlagen**.

### Behoben

- **Verschwundene Tage** – ein Tag mit lauter erledigten Einträgen verschwand aus der
  Liste. Der geöffnete Tag bleibt jetzt sichtbar, und unter der Liste steht, wie viele
  Tage ausgeblendet sind.
- **Darstellung** – Unterpunkte bleiben eingerückt, Zeichen wie `**` und `##` erscheinen
  nicht mehr im Text, und kein Abschnitt wiederholt seine eigene Überschrift.

## Version 1.3.2

### Behoben

- **Defekte Einstellungsdateien** werden nicht mehr überschrieben. Johann legt eine
  Sicherungskopie an (`….corrupt-<Zeitstempel>.json`) und meldet den Fehler beim Start.
- **PDF per Drag & Drop** – scheitert der Export, sagt Johann das jetzt, statt nichts zu tun.

## Version 1.3.1

### Behoben

- **Automatische Updates** – seit 1.1.0 wies Johann nie auf neue Versionen hin. Jetzt
  meldet er sich wieder, sobald eine neue Version im Netzlaufwerk liegt.

## Version 1.3.0

### Neu

- **Diktieren per Mikrofon** – „🎙 Diktieren“ nimmt direkt in Johann auf, ohne Umweg über
  das Smartphone. „■ Stop“ beendet die Aufnahme und startet die Verarbeitung.
- **Transkript bearbeiten** – Stift (✏) neben „Transkript“, Text korrigieren, „Neu
  generieren“. Alle Abschnitte, PDF und HTML nutzen dann den korrigierten Text.
- **Korrekturliste** – oft falsch erkannte Wörter als Paar hinterlegen (z. B. Piano →
  Peano). *Einstellungen → Grunddaten.*
- **Zoom per Tastatur** – `Strg++`, `Strg+-`, `Strg+0` und `Strg+Mausrad` in der
  Detailansicht.

## Version 1.2.1

### Neu

- **Zentrale Prompts** – alle nutzen dieselben Vorlagen aus
  `Z:\12_Tools\Peano\Johann\prompts.json`.
- **Admin-Modus** zum dauerhaften Ändern der Team-Prompts *(seit 1.4.0 ersetzt)*.

### Behoben

- **„Erledigt“** sitzt oben links und wird nicht mehr von Meldungen verdeckt.
- **Doppelte Eintragsnummern** bei mehreren Rechnern im selben Verzeichnis treten nicht
  mehr auf.

## Version 1.1.0

### Neu

- **Neue Oberfläche** – Aussehen und Bedienung wurden überarbeitet.
