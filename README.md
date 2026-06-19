# Platé.Johann – Benutzerhandbuch

**Johann** ist ein KI-gestütztes Diktat-Werkzeug für Windows. Du sprichst ein Diktat auf dein Smartphone, legst die MP3-Datei in den Eingangsordner – und Johann erledigt den Rest: Transkription via OpenAI Whisper, automatische Zusammenfassung, strukturierte Ablage als HTML und PDF.

---

## Schnellstart

1. Setup-Datei starten: `Z:\12_Tools\Peano\Johann\Platee.Johann-win-Setup.exe` und Installation abschließen.
2. Beim ersten Start erscheint ein Dialog: **.env-Datei einrichten** → **Ja** klicken. Damit wird der API-Schlüssel automatisch hinterlegt.
3. MP3-Datei in `Dokumente\Johann\Eingang` legen → Johann verarbeitet sie sofort im Hintergrund.
4. Fertiger Eintrag erscheint automatisch in der Liste – kein Neustart nötig.

> Ohne API-Schlüssel funktioniert Johann als reiner Viewer für bereits vorhandene Einträge.

---

## Die Oberfläche

```
┌──────────┬─────────────────────┬──────────────────────────────────────────┐
│  Datum   │  Einträge           │  Detail                                  │
│          │                     │                                          │
│ 17.03.   │ 001 intern          │  [Aufgabe]  Johann                       │
│ 16.03.   │   Citroën abholen…  │  Johann – wir müssen Änderungen vornehm. │
│ 15.03.   │ 002 Iris            │  17.03.2026 · 1:12                       │
│          │   ich weiß gar…     │  ────────────────────────────────────    │
│          │                     │  Kurzfassung / Zusammenfassung / …       │
│          │ [+ Neues Element]   │  [HTML] [PDF] [E-Mail] [Kopieren]        │
└──────────┴─────────────────────┴──────────────────────────────────────────┘
```

**Links – Datumsleiste**
Alle Tage mit Einträgen, neueste zuerst. Klick auf ein Datum filtert die Liste.

**Mitte – Einträge des Tages**
Nummer, Projektname und erste Wörter des Titels. Badge darunter zeigt den Typ.

**Rechts – Detailansicht**
Vollständiger Inhalt: alle KI-Abschnitte, Exportbuttons und das aufklappbare Transkript.

---

## Workflow

### MP3 einlesen (Eingangsordner)

MP3 in `Dokumente\Johann\Eingang` legen – Johann erkennt die Datei automatisch und verarbeitet sie sofort. Kein Klick nötig. Ideal für automatische Synchronisation über OneDrive vom Smartphone.

### Manuellen Eintrag anlegen

**+ Neues Element** öffnet einen Dialog: Typ und Projekt wählen, Text eingeben. Mit API-Schlüssel werden KI-Abschnitte sofort generiert.

### Fortschritt verfolgen

Das **🔔 Status-Log** (oben rechts) zeigt alle laufenden und abgeschlossenen Verarbeitungen. Bei Fehlern erscheint der Hinweis direkt dort.

---

## Diktat-Schema

Johann erkennt Typ, Projekt und Titel automatisch aus dem gesprochenen Anfang:

```
[Typ]  [Projektname]  Titel [Titel] Ende  [Inhalt]
```

- **Typ** *(optional, erstes Wort)* – steuert Eintragstyp und KI-Bausteine
- **Projektname** – zweites Wort nach dem Typ (oder erstes, wenn kein Typ)
- **Titel** *(optional)* – nach dem Schlüsselwort `Titel`, beendet mit `Ende`
- **Inhalt** – alles nach `Ende` (oder der gesamte Rest, wenn kein Titel angegeben)

**Beispiele:**

| Gesprochener Anfang | Typ | Projekt | Titel |
|---|---|---|---|
| `Aufgabe Johann Titel App anpassen Ende wir müssen…` | Aufgabe | Johann | „App anpassen" |
| `Gesprächsnotiz Iris Titel Meeting Ende Iris möchte…` | Gesprächsnotiz | Iris | „Meeting" |
| `E-Mail Müller Titel Angebot März Ende guten Tag…` | E-Mail | Müller | „Angebot März" |
| `intern Citroën abholen…` | Projekt | intern | *(GPT generiert)* |

Kein `Titel … Ende`? Dann generiert GPT den Titel automatisch aus dem Inhalt.

### Eintragstypen

| Typ | Schlüsselwort | KI-Besonderheit |
|---|---|---|
| **Projekt** | *(keines – Standard)* | Kurzfassung, Zusammenfassung, Ausführlich |
| **Aufgabe** | „Aufgabe" | + Aufgabenliste |
| **Gesprächsnotiz** | „Gesprächsnotiz" | + Gesprächsprotokoll; Name im Dateinamen |
| **E-Mail** | „E-Mail" / „Email" | + E-Mail-Text direkt in Zwischenablage |
| **Stundenzettel** | „Stundenzettel" | Kompaktes PDF-Layout |
| **Analog** | „Analog" | Freitext-Abschnitt statt Zusammenfassung |

---

## Detailansicht – Abschnitte und Export

### Abschnitte

Über die Checkboxen links werden Abschnitte ein-/ausgeblendet. Ausgeblendete Abschnitte sind auch bei PDF, HTML und Kopieren nicht enthalten.

**Alle Abschnitte werden für jeden Eintrag generiert.** Die Checkbox-Vorauswahl richtet sich nach dem Typ – sie kann jederzeit manuell angepasst werden.

| Abschnitt | Inhalt | Standard aktiv bei |
|---|---|---|
| **Ausführlich** | Fließtext-Zusammenfassung | Alle Typen |
| **Zusammenfassung** | Strukturierte Gliederung (Markdown) | Alle Typen |
| **Kurzfassung** | Ein-Satz-Zusammenfassung | Alle Typen |
| **Aufgaben** | Aufgabenliste | Typ „Aufgabe" |
| **Gesprächsnotiz** | Gesprächsprotokoll | Typ „Gesprächsnotiz" |
| **E-Mail** | Fertiger E-Mail-Text | Typ „E-Mail" |
| **Stundenzettel** | Zeiterfassung | Typ „Stundenzettel" |
| **Analog** | Freitext-Abschnitt | Typ „Analog" |
| **Transkript** | Vollständiger Originaltext (ausklappbar) | Alle Typen |

### Aktions-Buttons

| Button | Klick | Rechtsklick |
|---|---|---|
| **HTML** | HTML-Datei erstellen und im Browser öffnen; aktualisiert Tages-Übersicht | HTML-Inhalt in Zwischenablage kopieren |
| **PDF** | PDF erstellen und öffnen | PDF-Datei in Zwischenablage kopieren (als Datei, direkt einfügbar) |
| **E-Mail** | E-Mail-Text in Zwischenablage kopieren | In Outlook öffnen (mailto-Link mit Betreff und Text) |
| **Kopieren** | Alle sichtbaren Abschnitte mit Überschriften in Zwischenablage | — |
| **↻ Neu generieren** | Alle KI-Abschnitte neu generieren | Einzelne Abschnitte wählen (Ausführlich, Zusammenfassung, Aufgaben, …) |
| **Als erledigt markieren** | Eintrag abschließen – grünes Häkchen in der Liste | — |

### Transkript bearbeiten

Das Transkript kann direkt in der Detailansicht korrigiert werden:

1. Stift-Symbol (✏) neben „Transkript" klicken → der Text wird editierbar.
2. Fehler korrigieren (z. B. falsch erkannte Namen, fehlende Satzzeichen, versehentlich aufgenommene Passagen entfernen).
3. **Neu generieren** klicken → alle KI-Abschnitte werden aus dem korrigierten Text neu erstellt.

Der korrigierte Text wird sofort angezeigt und bleibt auch nach einem Neustart erhalten. Das Original-Transkript von Whisper wird intern als Referenz aufbewahrt. Bearbeitete Transkripte sind mit „(bearbeitet)" gekennzeichnet. PDF, HTML und Kopieren verwenden automatisch den korrigierten Text.

Mit **Abbrechen** wird die Bearbeitung verworfen und der zuletzt gespeicherte Text wiederhergestellt.

### Drag & Drop

Einen Eintrag in der Liste **anklicken und ziehen** erzeugt automatisch die PDF-Datei und startet einen Datei-Drag. Die PDF lässt sich so direkt in Explorer-Fenster, E-Mail-Programme oder andere Anwendungen ziehen.

---

## Tages-Übersicht

In jedem Tages-Ordner liegt eine `_ItemÜbersicht.html` – Karten-Ansicht aller Einträge des Tages mit Typ, Projekt, Titel und Kurzfassung. Wird bei jeder Änderung automatisch aktualisiert und kann als Browser-Lesezeichen gespeichert werden.

---

## Einstellungen

Das **⚙ Einstellungen**-Symbol öffnet den Einstellungs-Dialog.

### Allgemein – Name und Firma

**Beim ersten Start unbedingt ausfüllen.** Name und Firma werden in Kopf- und Fußzeile jedes PDFs eingedruckt.

| Feld | Zweck |
|---|---|
| **Name** | Erscheint im PDF-Kopf |
| **Firma** | Erscheint im PDF-Kopf neben dem Namen |

### Verzeichnisse
- **Eingang:** `Dokumente\Johann\Eingang` – MP3s hier ablegen → automatische Verarbeitung
- **Ausgabe:** `Dokumente\Johann\output` – alle erzeugten Dateien
- **Archiv:** Unterordner von Eingang – verarbeitete MP3s werden hierhin verschoben

### Korrekturliste

In den Einstellungen unter **Korrekturliste** können häufig falsch erkannte Wörter als Korrekturpaare hinterlegt werden (z. B. „Piano" → „Peano"). Die Korrekturen werden automatisch bei der KI-Zusammenfassung berücksichtigt. Vier Standardkorrekturen sind bereits voreingestellt.

### Team-Prompts

Prompts werden zentral vom Netzlaufwerk geladen (`Z:\12_Tools\Peano\Johann\prompts.json`). Lokale Änderungen an Prompts gelten nur temporär bis zum nächsten App-Neustart – beim Start werden immer die aktuellen Team-Prompts vom Netzlaufwerk übernommen.

Der Bereich **Team-Prompts** in den Einstellungen zeigt den Pfad zur globalen Prompt-Datei.

**Admin-Modus:** Unten links in den Einstellungen gibt es einen **Admin**-Button (passwortgeschützt). Berechtigte Personen können darüber die Prompt-Vorlagen dauerhaft für alle Mitarbeiter ändern.

Bereits vorhandene Einträge können per **↻ Neu generieren** mit den aktuellen Prompts aktualisiert werden.

- Persönliche Einstellungen (Name, Firma, Verzeichnisse) werden in `Dokumente\Johann\settings.json` gespeichert.
- Prompt-Vorlagen werden zentral von `Z:\12_Tools\Peano\Johann\prompts.json` geladen.

---

## Neuigkeiten nach Updates

Nach jedem Update erscheint beim ersten Start ein Fenster mit den Neuerungen der Version. Es wird nur einmal pro Version angezeigt.

---

## Dateiablage

```
Dokumente\Johann\output\
└── 2026-03-17\
    ├── _ItemÜbersicht.html
    ├── 260317_001_Johann_App_anpassen.pdf
    ├── 260317_001_Johann_App_anpassen.html
    └── _raw\
        ├── …_status.json      ← Datenspeicher
        ├── …_original.mp3     ← Audio-Kopie
        └── …_transcript.txt   ← Transkript
```

Kein Server, keine Datenbank – normale Dateien, die sich kopieren, archivieren oder auf OneDrive synchronisieren lassen.

---

## Best Practices

**Diktat-Einstieg klar sprechen** – Typ und Projektname direkt am Anfang, dann kurze Pause. `Titel … Ende` für einen kontrollierten Titel verwenden, sonst übernimmt GPT.

**Projektnamen konsistent halten** – Johann speichert exakt wie erkannt. Immer denselben Begriff verwenden.

**Eingangsordner für den Alltag** – MP3s per OneDrive-Synchronisation vom Smartphone direkt in `Dokumente\Johann\Eingang`. Johann erledigt den Rest ohne weiteren Eingriff.

**Tages-Übersicht als Browser-Lesezeichen** – `_ItemÜbersicht.html` ablegen und nach einem Arbeitstag alle Einträge auf einen Blick sehen, ohne die App zu öffnen.

**Prompts anpassen** – Wenn Zusammenfassungen nicht passen: Prompt-Tab öffnen, Stil/Länge/Format ergänzen, dann **↻ Neu generieren**.

---

## Problembehandlung

| Problem | Lösung |
|---|---|
| Buttons ausgegraut, keine Verarbeitung | API-Schlüssel fehlt → Startdialog nutzen oder `.env` in `Dokumente\Johann` ablegen |
| MP3 im Eingangsordner wird nicht erkannt | Dateiformat prüfen (`.mp3`), Eingangsordner in Einstellungen kontrollieren |
| Eintrag hat falschen Typ | Diktat-Einstieg anpassen, dann **↻ Neu generieren** |
| KI-Abschnitte leer | API-Schlüssel und Internetverbindung prüfen, Status-Log prüfen |
| Absturz | Crash-Logs unter `%LOCALAPPDATA%\Platee\Johann\logs\crash-*.log` mit Zeitstempel und Fehlertext |
| Prompts nicht aktuell | App neu starten – Prompts werden beim Start vom Netzlaufwerk geladen |
| Korrekturen nicht aktiv | Einstellungen → Korrekturliste prüfen, Speichern klicken |
| Transkript-Bearbeitung verloren | Bei API-Fehler bleibt der korrigierte Text erhalten → erneut „↻ Neu generieren" klicken |

---

*Platé.Johann v1.2.1 · Windows 10/11 · Daten bleiben lokal*
