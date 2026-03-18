# Platé.Johann – Benutzerhandbuch

**Johann** ist ein KI-gestütztes Diktat-Werkzeug für Windows. Du sprichst ein Diktat auf dein Smartphone, legst die MP3-Datei in einen Eingangsordner – und Johann erledigt den Rest: Transkription via OpenAI Whisper, automatische Zusammenfassung, strukturierte Ablage als HTML und PDF.

---

## Schnellstart

1. `Johann.UI.exe` doppelklicken – keine Installation nötig.
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
│          │ [+ Neu]  [🎙 MP3]   │  [HTML] [PDF] [E-Mail] [Kopieren]        │
└──────────┴─────────────────────┴──────────────────────────────────────────┘
```

**Links – Datumsleiste**
Alle Tage mit Einträgen, neueste zuerst. Klick auf ein Datum filtert die Liste.

**Mitte – Einträge des Tages**
Nummer, Projektname und erste Wörter des Titels. Darunter der Eintragstyp als Badge.

**Rechts – Detailansicht**
Der vollständige Inhalt: Kurzfassung, Zusammenfassung, Ausführliche Zusammenfassung, Aufgaben-/Gesprächsnotiz-Block und das aufklappbare Originaltranskript.

---

## Workflow

### Diktat aufnehmen

Sprich dein Diktat nach diesem Schema:

```
[Typ]  [Projektname]  [Inhalt]
```

**Beispiele:**

| Gesprochener Einstieg | Erkannter Typ | Projekt |
|---|---|---|
| *(nichts / normaler Einstieg)* | Projekt | erste(s) Wort(e) |
| „Aufgabe Johann …" | Aufgabe | Johann |
| „Gesprächsnotiz Iris …" | Gesprächsnotiz | Iris |
| „E-Mail Müller …" | E-Mail | Müller |
| „Stundenzettel intern …" | Stundenzettel | intern |
| „Analog …" | Analog | … |

Der Typ steuert, welche KI-Bausteine generiert werden und wie das PDF aussieht.

### MP3 einlesen

**Weg A – Automatisch (empfohlen):**
MP3 in `Dokumente\Johann\Eingang` legen. Johann erkennt die Datei sofort und startet die Verarbeitung – kein Klick nötig.

**Weg B – Manuell:**
Unten in der Eintrags-Liste auf **🎙 MP3** klicken und Datei(en) auswählen. Mehrfachauswahl mit Strg/Shift möglich.

Der Fortschritt läuft im Status-Log (Glocken-Symbol oben rechts). Nach Abschluss erscheint der Eintrag sofort in der Liste.

### Manuellen Eintrag anlegen

**+ Neu** öffnet einen Dialog: Typ und Projekt wählen, Text eingeben. Der Text wird wie ein Transkript behandelt – mit API-Schlüssel werden Zusammenfassungen sofort generiert.

---

## Detailansicht – Abschnitte und Export

### Abschnitte

Über die Checkboxen links kann jeder Abschnitt ein- oder ausgeblendet werden:

| Abschnitt | Inhalt |
|---|---|
| **Kurzfassung** | 2–4 Stichpunkte, die den Kern erfassen |
| **Zusammenfassung** | Strukturierte Gliederung als Markdown |
| **Ausführlich** | Fließtext-Zusammenfassung |
| **Aufgaben** | Aufgabenliste (nur Typ „Aufgabe") |
| **Gesprächsnotiz** | Protokoll (nur Typ „Gesprächsnotiz") |
| **Transkript** | Vollständiger Originaltext (ausklappbar) |

### Aktions-Buttons

| Button | Was passiert |
|---|---|
| **HTML** | Erzeugt HTML-Datei, öffnet sie im Browser und aktualisiert die Tages-Übersicht |
| **PDF** | Erzeugt formatiertes PDF (Layout je nach Typ) und öffnet es |
| **E-Mail** | Generiert E-Mail-Text per KI und kopiert ihn direkt in die Zwischenablage |
| **Kopieren** | Kopiert alle sichtbaren Abschnitte mit Überschriften in die Zwischenablage |
| **↻ Neu generieren** | Generiert alle KI-Abschnitte neu (erfordert API-Schlüssel) |
| **Als erledigt markieren** | Markiert den Eintrag als abgeschlossen |

---

## Tages-Übersicht

In jedem Tages-Ordner (`Dokumente\Johann\output\YYYY-MM-DD\`) liegt eine `_ItemÜbersicht.html` – eine Karten-Ansicht aller Einträge des Tages mit Typ, Projekt, Titel und Kurzfassung. Sie wird bei jeder Änderung automatisch aktualisiert und kann direkt im Browser geöffnet werden.

---

## Einstellungen

Das **⚙ Einstellungen**-Symbol oben rechts öffnet den Einstellungs-Dialog.

### Verzeichnisse
Eingangs-, Ausgabe- und Archiv-Ordner können frei gewählt werden. Standard:
- Eingang: `Dokumente\Johann\Eingang`
- Ausgabe: `Dokumente\Johann\output`

### Prompts anpassen
Jeder KI-Abschnitt hat einen eigenen Prompt-Tab (Kurzfassung, Zusammenfassung, Ausführlich, E-Mail, Aufgaben, Gesprächsnotiz, Stundenzettel, Analog). Prompts können frei bearbeitet werden – **Zurücksetzen** stellt die Originalversion wieder her.

Änderungen wirken sich auf alle zukünftigen Verarbeitungen aus. Bereits vorhandene Einträge bleiben unverändert und können per **↻ Neu generieren** aktualisiert werden.

---

## Dateiablage

Alle Daten liegen lokal in `Dokumente\Johann\output\`:

```
Dokumente\Johann\output\
└── 2026-03-17\
    ├── _ItemÜbersicht.html
    ├── 260317_001_Johann_wir_müssen_Änder….pdf
    ├── 260317_001_Johann_wir_müssen_Änder….html
    └── _raw\
        ├── …_status.json     ← Datenspeicher
        ├── …_original.mp3    ← Audio-Kopie
        └── …_transcript.txt  ← Transkript
```

Kein Server, keine Datenbank – alles sind normale Dateien, die sich kopieren, archivieren oder auf OneDrive synchronisieren lassen.

---

## Best Practices

**Einheitlich sprechen** – Das erste Wort des Diktats bestimmt den Typ. Sprich es klar und direkt am Anfang: *„Aufgabe Johann…"*

**Projektname konsistent halten** – Johann speichert den Projektnamen genau so, wie er erkannt wird. Immer denselben Begriff verwenden (z.B. immer „Johann" statt mal „Johann" mal „App").

**Eingangsordner nutzen** – Leg MP3s direkt aus der Aufnahme-App in `Dokumente\Johann\Eingang` (z.B. per OneDrive oder automatischer Synchronisation). Johann erledigt den Rest ohne weiteren Eingriff.

**Tages-Übersicht im Browser** – Die `_ItemÜbersicht.html` lässt sich als Browser-Bookmark ablegen. Nach einem Arbeitstag alle Einträge auf einen Blick.

**Prompts anpassen** – Wenn die Zusammenfassungen nicht dem gewünschten Stil entsprechen, den passenden Prompt-Tab in den Einstellungen öffnen und konkrete Anweisungen ergänzen (z.B. gewünschte Länge, Tonalität, Formatvorgaben).

---

## Problembehandlung

| Problem | Lösung |
|---|---|
| Buttons ausgegraut, keine Verarbeitung | API-Schlüssel fehlt → Einstellungen prüfen oder `.env`-Datei in `Dokumente\Johann` ablegen |
| MP3 wird nicht erkannt | Datei-Format prüfen (`.mp3`), Eingangsordner in Einstellungen kontrollieren |
| Eintrag hat falschen Typ | Diktat-Einstieg anpassen; manuell neu verarbeiten mit **↻ Neu generieren** |
| Absturz | `Johann_crash.txt` auf dem Desktop enthält den Fehlertext |

---

*Platé.Johann · Windows 10/11 · Keine Installation · Daten bleiben lokal*
