# Johann – Benutzerhandbuch

**Johann** ist eine Desktop-Anwendung für Windows, die Sprachaufnahmen (MP3-Dateien) automatisch transkribiert, mit KI zusammenfasst und strukturiert ablegt. Die App ermöglicht es, alle gespeicherten Einträge zu durchsuchen, zu exportieren und weiterzuverarbeiten.

---

## Schnellstart

1. Die Datei `Johann.UI.exe` doppelklicken – keine Installation notwendig.
2. Beim ersten Start wird der Ordner `Dokumente\Johann\output` automatisch angelegt.
3. OpenAI API-Key einrichten, um KI-Funktionen zu nutzen.

---

## Benutzeroberfläche im Überblick

Die App ist in drei Bereiche aufgeteilt:

```txt
┌─────────┬──────────────────────┬──────────────────────────────────────────┐
│  Datum  │  Einträge            │  Detail                                  │
│         │                      │                                          │
│ 27.02.  │ 001_Johann_wir_...   │  [Aufgabe]  Johann                       │
│ 26.02.  │ 002_Iris_kurzes_...  │  Johann_wir_müssen_Änderungen_vornehmen  │
│ 25.02.  │ 003_Allg_projekt_... │  27.02.2026 · 0:45                       │
│         │                      │  ─────────────────────────────────────   │
│         │                      │  Abstract / Zusammenfassung / Aufgaben   │
│         │                      │  ...                                     │
│         │ [+ Neues Element]    │  [HTML] [PDF] [E-Mail] [Kopieren]        │
│         │ [🎙 MP3]             │  ☑ Transkript  [Verarbeiten]             │
└─────────┴──────────────────────┴──────────────────────────────────────────┘
```

### Linke Spalte – Datumsauswahl

- Listet alle Tage, für die Einträge vorhanden sind (neueste zuerst).
- Klick auf ein Datum → die Eintrags-Liste in der Mitte aktualisiert sich sofort.

### Mittlere Spalte – Eintrags-Liste

- Zeigt alle Einträge des ausgewählten Tages.
- Jeder Eintrag zeigt **NNN_Projektname_ErsteWörter** sowie den Typ (Aufgabe, Gesprächsnotiz, etc.) darunter.
- Klick auf einen Eintrag → der Detail-Bereich rechts öffnet sich.

**Schaltflächen unten:**

- **+ Neues Element** – manuellen Eintrag anlegen (Text eingeben, Typ und Projekt wählen).
- **🎙 MP3** – eine oder mehrere MP3-Dateien auswählen; die App transkribiert und fasst sie zusammen.

### Rechte Spalte – Detail-Ansicht

Zeigt den vollständigen Inhalt des ausgewählten Eintrags:

| Bereich | Inhalt |
| --- | --- |
| **Abstract** | Kurze Zusammenfassung (1–3 Sätze) |
| **Zusammenfassung** | Strukturierte Gliederung (Markdown) |
| **Ausführliche Zusammenfassung** | Fließtext-Zusammenfassung |
| **Aufgaben** | Aufgabenliste (nur bei Typ „Aufgabe") |
| **Gesprächsnotiz** | Protokoll (nur bei Typ „Gesprächsnotiz") |
| **Originaltranskript** | Vollständiges Transkript (ausklappbar) |

---

## Buttons in der Aktionsleiste (Detail-Ansicht)

| Button | Funktion |
| --- | --- |
| **HTML** | Erzeugt eine HTML-Datei für diesen Eintrag und öffnet sie im Browser. Aktualisiert gleichzeitig die Tages-Übersicht `_ItemÜbersicht.html`. |
| **PDF** | Erzeugt ein PDF mit Layout je nach Eintragstyp und öffnet es. |
| **E-Mail** | Generiert einen E-Mail-Text via KI (GPT) und kopiert ihn direkt in die Zwischenablage – kein Datei-Dialog, sofort einfügbar. |
| **Kopieren** | Kopiert **alles** (Titel, Abstract, Zusammenfassung, Ausführliche Zusammenfassung, Aufgaben, Gesprächsnotiz, und ggf. Transkript) mit Abschnittsüberschriften in die Zwischenablage. |
| **☑ Transkript** | Checkbox: Legt fest, ob das Originaltranskript in PDF, HTML und beim Kopieren enthalten sein soll. Standard: aktiviert. |
| **Verarbeiten** | Regeneriert alle KI-Zusammenfassungen für den aktuellen Eintrag neu (setzt einen konfigurierten API-Key voraus). |

---

## Eintragstypen

Johann unterscheidet fünf Typen, die automatisch aus dem Sprachbefehl erkannt werden:

| Typ | Erkennungswort | Besonderheiten |
| --- | --- | --- |
| **Projekt** | *(Standard, kein Keyword)* | Allgemeiner Eintrag |
| **Aufgabe** | „Aufgabe" | Aufgabenliste wird generiert |
| **Gesprächsnotiz** | „Gesprächsnotiz" | Protokoll wird generiert; im Dateinamen enthalten |
| **E-Mail** | „E-Mail" oder „Email" | E-Mail-Text wird generiert |
| **Stundenzettel** | „Stundenzettel" | Kompaktes PDF-Layout |

**Beispiel-Diktat:** *„Aufgabe Johann wir müssen noch einige Änderungen an der App vornehmen"*
→ Typ: Aufgabe, Projekt: Johann, Titel: „wir müssen noch einige Änderungen"

---

## Dateistruktur

Alle Daten liegen in `Dokumente\Johann\output\`:

```txt
Dokumente\Johann\output\
└── YYYY-MM-DD\
    ├── _ItemÜbersicht.html          ← Tages-Übersicht (auto-generiert)
    ├── YYMMDD_NNN_Projekt_Titel.pdf
    ├── YYMMDD_NNN_Projekt_Titel.html
    └── _raw\
        ├── YYMMDD_NNN_Projekt_Titel_status.json   ← Datenspeicher
        ├── YYMMDD_NNN_Projekt_Titel.mp3           ← Original-Audio (Kopie)
        └── YYMMDD_NNN_Projekt_Titel.txt           ← Transkript-Text
```

**Namensschema:** `YYMMDD_NNN[_Gesprächsnotiz]_Projektname_ErsteFünfWorteDesTitels`

- `YYMMDD` = Datum (z.B. 260227)
- `NNN` = laufende Nummer des Tages (001, 002, …)
- `_Gesprächsnotiz` = nur bei diesem Typ eingeschaltet
- Projektname und Titel: erste fünf Wörter, Sonderzeichen durch `_` ersetzt

---

## MP3-Dateien verarbeiten

1. **🎙 MP3**-Button klicken.
2. Eine oder mehrere MP3-Dateien auswählen (Mehrfachauswahl mit Strg/Shift möglich).
3. Die App:
   - Transkribiert jede Datei via OpenAI Whisper.
   - Erkennt Typ und Projekt aus dem gesprochenen Header.
   - Generiert Abstract, Zusammenfassung, Ausführliche Zusammenfassung parallel.
   - Speichert alles in `Dokumente\Johann\output\HEUTE\`.
   - Aktualisiert die Tages-Übersicht `_ItemÜbersicht.html`.
4. Fortschritt wird in der Statusleiste unten angezeigt.
5. Die Einträge erscheinen sofort in der Liste – kein Neustart nötig.

> **Hinweis:** Ohne API-Key können MP3s nicht verarbeitet werden. Der Button ist dann ausgegraut, und es erscheint eine Fehlermeldung in der Statusleiste.

---

## Manuelle Einträge

1. **+ Neues Element** klicken.
2. Im Dialog: Typ, Projekt und Text eingeben (Text wird als Transkript behandelt).
3. Mit **Speichern** bestätigen.
4. Falls ein API-Key vorhanden ist, wird sofort automatisch eine KI-Zusammenfassung generiert.

---

## Einstellungen

Das **⚙**-Symbol unten rechts öffnet die Einstellungen. Hier können alle GPT-Prompts bearbeitet werden:

| Tab | Prompt |
| --- | --- |
| System | Systemrolle für den KI-Assistenten |
| Abstract | Kurzfassung-Generierung |
| Zusammenfassung | Strukturierte Gliederung |
| Fließtext | Ausführliche Zusammenfassung |
| E-Mail | E-Mail-Generierung |

**Speichern** übernimmt die Änderungen sofort – laufende Verarbeitungen nutzen die neuen Prompts ab dem nächsten Aufruf.
**Zurücksetzen** stellt die Original-Prompts wieder her.

Die Einstellungen werden in `Dokumente\Johann\settings.json` gespeichert.

---

## API-Key einrichten

Die KI-Funktionen (Transkription, Zusammenfassungen, E-Mail-Generierung) erfordern einen OpenAI API-Key.

**Methode 1 – Umgebungsvariable (empfohlen):**

1. Windows-Suche → „Umgebungsvariablen bearbeiten" öffnen.
2. Neue Variable anlegen: Name `OPENAI_API_KEY`, Wert: `sk-proj-...`.
3. Johann neu starten.

**Methode 2 – .env-Datei:**

1. Datei `.env` im selben Ordner wie `Johann.UI.exe` anlegen.
2. Inhalt: `OPENAI_API_KEY=sk-proj-...`
3. Johann neu starten.

Wenn kein Key vorhanden ist, funktioniert die App weiterhin als reiner Viewer für bereits verarbeitete Einträge.

---

## Tages-Übersicht (_ItemÜbersicht.html)

In jedem Tages-Ordner (`Dokumente\Johann\output\YYYY-MM-DD\`) wird automatisch eine `_ItemÜbersicht.html` erzeugt oder aktualisiert, sobald:

- ein neuer Eintrag via MP3 oder manuell gespeichert wird,
- ein Eintrag neu verarbeitet wird, oder
- der HTML-Export-Button für einen Eintrag gedrückt wird.

Die Übersicht zeigt alle Einträge des Tages als Karten mit Typ-Badge, Projekt, Titel und Abstract. Sie kann direkt im Browser geöffnet werden und enthält Links zu den einzelnen HTML-Dateien.

---

## Fehlerprotokoll

Bei Abstürzen wird automatisch eine Datei `Johann_crash.txt` auf dem Desktop erstellt. Diese enthält Zeitstempel und den vollständigen Fehlertext und kann für die Fehlerbehebung verwendet werden.

---

## Noch nicht implementiert / Geplant

- **Suchfunktion** – Volltextsuche über alle Einträge
- **Eintragsbearbeitung** – nachträgliches Bearbeiten von Titel, Projekt oder Typ im Detail-Bereich
- **Stundenzettel-Auswertung** – automatische Summierung von Zeiten pro Projekt
- **Direktversand per E-Mail** – aktuell wird die E-Mail nur in die Zwischenablage kopiert
- **Mehrsprachigkeit** – aktuell nur Deutsch

---

## Technische Hinweise

- **Betriebssystem:** Windows 10 / 11 (64-Bit)
- **Ausgabeordner:** `Dokumente\Johann\output\` (kann per Kommandozeilenargument überschrieben werden: `Johann.UI.exe "C:\anderer\pfad"`)
- **Datenspeicher:** JSON-Dateien (kein Server, keine Datenbank)
- **Kompatibilität:** Liest auch Einträge, die vom Python-Vorgängersystem erstellt wurden
- **Keine Installation:** Einzelne EXE-Datei, läuft ohne .NET-Installation

---

*Johann – Entwickelt mit C# / WPF / .NET 8*
