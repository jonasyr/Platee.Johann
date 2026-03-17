# Platé.Johann – Single Source of Truth

> **Gesamtdokumentation** aller Feature-Ideen, Änderungswünsche, Bugfixes und offenen Aufgaben.
> Zusammengetragen aus sämtlichen Feedback-Runden des Auftraggebers.

---

## 1. Produktname & Branding

| Thema | Details |
|---|---|
| **Offizieller Name** | **Platé.Johann** (alternativ nur „Johann") |
| **Verbotene Namen** | „Johann C-S" – ausdrücklich abgelehnt |
| **Einordnung** | Passt in das Gesamtkonstrukt der Platé-Produktfamilie |
| **Langfristiges Ziel** | Nach einigen Wochen Einspielzeit soll das Tool den Mitarbeitern präsentiert werden – u. a. als Hilfe zum Diktieren von Berichten |

---

## 2. Konfiguration & Einstellungen

### 2.1 Externe Config-Datei

> **Status:** Eine Config-Datei existiert bereits, wird aber vom Programm **nicht korrekt eingelesen**. Änderungen an der Config werden nach einem Neustart **nicht übernommen**.

- [ ] Config-Datei beim Programmstart **vollständig einlesen und anwenden**
- [ ] Änderungen an der Config müssen nach **Neustart** wirksam werden
- [ ] Ziel: Einstellungen bleiben bei Updates erhalten
- [ ] Config muss ohne Programmänderung editierbar sein

### 2.2 Pflichtfelder in der Config

| Feld | Beschreibung |
|---|---|
| **Name** | Benutzername, wird ins PDF gedruckt |
| **Firma** | Firmenname, wird ins PDF gedruckt |
| **Quellverzeichnis (MP3)** | Standardpfad, aus dem MP3-Dateien gelesen werden |
| **Prompts pro Typ** | Für jeden Typ (E-Mail, Aufgabe, Gesprächsnotiz, Stundenzettel, Analog etc.) ein individueller Prompt |

> **Hinweis:** Die `.env`-Datei mit dem OpenAI API-Key soll im **Publish-Ordner** liegen (siehe Abschnitt 11).

---

## 3. Titel-Erkennung aus Transkript (WICHTIG)

> **Status:** Der Titel wird aktuell **nicht erkannt/ausgelesen**. Dies ist ein kritisches Feature.

### 3.1 Parsing-Regel für den Transkript-Header

Der Beginn jedes Transkripts folgt dem Schema: `[Typ] [Projekt] Titel [Titeltext] Ende`

**Regeln:**

1. Nach Typ und Projekt kann **optional** ein Titel definiert werden
2. Wenn das dritte Wort **„Titel"** oder **„Betreff"** ist, bilden die **nächsten maximal 15 Worte** den Titel – bis das Wort **„Ende"** gesagt wird
3. Wird „Ende" vergessen, endet der Titel **automatisch nach 15 Worten**
4. Wird kein Titel angegeben oder das Wort „Titel" nicht verwendet, bestimmt **ChatGPT den Titel selbstständig**
5. Wenn „Ende" erst nach 30–35 Worten auftaucht (also weit über dem 15-Wort-Limit), wird der Titel ebenfalls **automatisch von ChatGPT generiert**

### 3.2 Beispiele

| Transkript-Beginn | Typ | Projekt | Titel |
|---|---|---|---|
| „Aufgabe Johann Titel wir müssen noch einige Änderungen in Johann vornehmen Ende" | Aufgabe | Johann | „wir müssen noch einige Änderungen in Johann vornehmen" |
| „Projekt Iris Titel Offene Punkte Ende" | Projekt | Iris | „Offene Punkte" |
| Eine normale kurze Nachricht über ein Gespräch | Projekt | allgemein | *automatisch generiert von ChatGPT* |
| „E-Mail Johann Titel …" (sehr lang, „Ende" erst nach 30+ Worten) | E-Mail | Johann | *automatisch generiert von ChatGPT* |

### 3.3 Speicherung & Anzeige

- [ ] Erkannter Titel wird im **JSON** gespeichert
- [ ] Titel wird im **PDF** angezeigt
- [ ] Titel wird in der **GUI** angezeigt
- [ ] Funktionsweise analog zur bestehenden Projekt-Erkennung

---

## 4. MP3-Verarbeitung & Automatisierung

### 4.1 Quellverzeichnis & Auto-Start

- [ ] MP3-Quellverzeichnis wird aus der Config gelesen (kein manueller Button-Klick mehr nötig)
- [ ] Beim **Programmstart** automatisch alle MP3s im Quellverzeichnis verarbeiten
- [ ] **Verarbeiten** bedeutet: Transkription + Titel-Erkennung + **alle** Typ-Prompts ausführen + ins JSON schreiben
- [ ] Verarbeitungsdauer von mehreren Minuten ist akzeptabel

### 4.2 Verzeichnisüberwachung (FileSystemWatcher)

- [ ] Bei **geöffnetem Programm** das Quellverzeichnis dauerhaft überwachen
- [ ] Neue MP3-Dateien automatisch verarbeiten, sobald sie im Verzeichnis erscheinen

### 4.3 MP3-Dateien nach Verarbeitung entfernen

- [ ] Verarbeitete MP3s müssen aus dem **Quellverzeichnis verschwinden** (verschieben, nicht löschen)
- [ ] Andernfalls Gefahr der Doppelverarbeitung und Organisation wird schwierig

---

## 5. PDF-Erzeugung & -Inhalte

### 5.1 Automatische PDF-Erstellung

- [ ] PDFs werden **immer automatisch** bei der Verarbeitung erzeugt – nicht nur auf Knopfdruck
- [ ] Alle PDFs müssen fertig sein, sobald auch die Übersichts-HTML erstellt ist
- [ ] PDFs sollen ins **Stammverzeichnis** kopiert werden

### 5.2 PDF-Inhalte & Branding

- [ ] **Name + Firma** aus Config im PDF-Header/Footer eindrucken
- [ ] **Copyright-Hinweis** im PDF (z. B. „Generiert mit KI-Unterstützung" / Disclaimer)
- [ ] **Peano-Logo** im PDF integrieren (wenn mit wenig Aufwand machbar)

### 5.3 PDF-Inhalt abhängig von Typ-Auswahl

- [ ] Der Inhalt des PDFs richtet sich nach den **in der GUI angehakten Typen** (siehe Abschnitt 7)
- [ ] Beispiel Gesprächsnotiz: PDF enthält nur Header (Datum, Projekt, Abstract) + den Gesprächsnotiz-Prompt – **ohne** Transkript, Fließtext, Strukturiert
- [ ] Ziel: PDF direkt per E-Mail an Kunden/intern verschickbar

---

## 6. HTML-Erzeugung

### 6.1 Automatische HTML-Erstellung

- [ ] HTMLs werden ebenfalls automatisch bei der Verarbeitung erzeugt
- [ ] HTMLs sollen ins **RAW-Verzeichnis** kopiert werden

### 6.2 Individuelles HTML

- [ ] Auch das individuelle HTML pro Eintrag soll die **Typ-Auswahl berücksichtigen** (analog zum PDF)

### 6.3 Übersichts-HTML

- [ ] Das übergeordnete Übersichts-HTML benötigt **keine Typ-Filterung** – dient nur als Gesamtübersicht

---

## 7. Typen-System (Kernfeature)

### 7.1 Konzept

Jeder Eintrag hat einen erkannten **Typ** (z. B. Projekt, E-Mail, Aufgabe, Gesprächsnotiz, Stundenzettel, Analog). Pro Typ existiert ein **individueller Prompt** in der Config, der bei der Verarbeitung ausgeführt wird.

> **Vereinfachung:** Es werden **immer alle Typ-Prompts** für jeden Eintrag generiert – ausnahmslos. Kein Flag nötig. Das vereinfacht die Verarbeitung erheblich.

| Typ | Prompt | Besonderheit |
|---|---|---|
| **Projekt** | Standard-Prompts: Fließtext + Strukturiert | Kein eigener Typ-Prompt |
| **E-Mail** | Individueller E-Mail-Prompt | Aus Config |
| **Aufgabe** | Individueller Aufgaben-Prompt | Aus Config |
| **Gesprächsnotiz** | Individueller Gesprächsnotiz-Prompt | Aus Config, kundentauglich |
| **Stundenzettel** | Individueller Stundenzettel-Prompt | Aus Config |
| **Analog** | Individueller Analog-Prompt | Aus Config |
| *(weitere Typen)* | Jeweils eigener Prompt | Erweiterbar |

### 7.2 Verarbeitung

- [ ] Bei der MP3-Verarbeitung werden neben **Titel, Abstract, Strukturiert, Fließtext** immer **alle individuellen Typ-Prompts** berechnet
- [ ] Ergebnisse aller Typ-Prompts werden ins **JSON** geschrieben
- [ ] Kein Flag nötig – es werden stets alle Typen generiert

### 7.3 Checkbox-Leiste in der GUI (links) – Anzeige & Filterung

- [ ] Unterhalb der Datumsfelder (scrollbar) eine **Checkbox-Leiste** anzeigen
- [ ] Checkboxen für: Fließtext, Strukturiert, E-Mail, Aufgabe, Gesprächsnotiz, Stundenzettel, Analog, … (**alle Typen außer Projekt**)
- [ ] **Standard-Selektion**: Fließtext + Strukturiert + der erkannte individuelle Typ
- [ ] Da alle Typen bereits berechnet sind, dient die Checkbox **nur zur Steuerung der Anzeige** (kein on-demand-Berechnen nötig)
- [ ] Die angehakten Typen bestimmen, was in **GUI, HTML und PDF** angezeigt wird
- [ ] Titel + Abstract werden **immer** angezeigt (unabhängig von Checkboxen)
- [ ] Alle Typ-Inhalte sollen in der **Inhaltsansicht** sichtbar und per Checkbox ein-/ausblendbar sein

### 7.4 Anwendungsbeispiel

> Ich erstelle eine Gesprächsnotiz. Standardmäßig sind Fließtext, Strukturiert und Gesprächsnotiz angehakt.
> Ich nehme Fließtext und Strukturiert **raus** → GUI zeigt nur noch Titel, Abstract + Gesprächsnotiz-Prompt.
> Ich klicke auf PDF → PDF enthält nur Header + Gesprächsnotiz-Text → direkt per E-Mail verschickbar.
> Alternativ: Ich hake zusätzlich E-Mail an → PDF enthält dann Gesprächsnotiz + E-Mail-Prompt-Ergebnis.

---

## 8. GUI-Buttons: HTML & PDF (Klick-Verhalten)

### 8.1 Linksklick

- [ ] **Erstellen und Anzeigen** (bisheriges Verhalten)

### 8.2 Rechtsklick (oder alternative Belegung)

- [ ] **Erstellen und in die Zwischenablage kopieren**
- [ ] Danach: **MessageBox** anzeigen (z. B. „In die Zwischenablage kopiert")
- [ ] Zweck: Inhalt direkt in E-Mail, ELO oder andere Programme einfügen

---

## 9. Erledigt-Funktion

- [ ] Button/Checkbox pro Eintrag: **„Erledigt"** setzen
- [ ] Erledigt-Status wird im **JSON** mit abgespeichert
- [ ] **Filteroption**: „Alle anzeigen" oder „Nur unerledigte anzeigen"
- [ ] Hintergrund: Bearbeitung erfolgt nicht linear von oben nach unten, sondern springend

---

## 10. Sortierung

- [ ] Einträge sortierbar machen über einen **Button in der Leiste**
- [ ] Sortieroptionen:
  - Nach **ID**
  - Nach **Projekt**, dann nach **ID**

---

## 11. Drag & Drop / Dateiweiterverarbeitung

### 11.1 Idealzustand

- [ ] Auf einen Eintrag klicken und das zugehörige **PDF per Drag & Drop** in andere Programme ziehen:
  - **ELO** (Dokumentenmanagement)
  - **Outlook** (als Anhang)
  - **Windows Explorer** (Dateiablage)

### 11.2 Alternative (falls Drag & Drop zu aufwendig)

- [ ] Bei Klick auf einen Eintrag öffnet sich ein **Explorer-Fenster** mit der PDF-Datei
- [ ] Von dort kann der Benutzer die Datei manuell verschieben/ziehen
- [ ] **Kernziel**: Mit möglichst wenig Aufwand PDFs in ELO, Outlook und Explorer verfügbar machen

---

## 12. Deployment & Umgebung

- [x] ~~Deployment/Migration & API-Verbindungsprobleme~~ → **Behoben** (war nur eine Kleinigkeit)
- [ ] Die **`.env`-Datei** (enthält den OpenAI API-Key) soll im **Publish-Ordner** liegen, sodass nur dieser Ordner weitergegeben werden muss

---

## 13. Priorisierte Aufgabenliste (Zusammenfassung)

### Priorität 1 – Grundfunktionen & kritische Features

| # | Aufgabe | Status |
|---|---|---|
| ~~1~~ | ~~Deployment/Migration fixen~~ | ✅ Behoben |
| ~~2~~ | ~~API-Verbindung (JGPT-Schnittstelle) sicherstellen~~ | ✅ Behoben |
| ~~3~~ | ~~Config-Datei korrekt einlesen + Änderungen nach Neustart übernehmen~~ | ✅ Behoben (Laden aller 14 Felder ✅; Speichern der 5 UI-Felder ohne die übrigen zu überschreiben ✅; Felder ohne UI noch nicht editierbar → gehört zu Prio 2) |
| ~~4~~ | ~~**Titel-Erkennung** aus Transkript implementieren (Parsing „Titel … Ende")~~ | ✅ Behoben (alle 5 Regeln, GPT-Fallback, JSON/PDF/HTML/GUI) |
| ~~5~~ | ~~Quellverzeichnis aus Config lesen + Auto-Start-Verarbeitung~~ | ✅ Behoben |
| ~~6~~ | ~~Verzeichnisüberwachung (FileSystemWatcher) für neue MP3s~~ | ✅ Behoben |
| ~~7~~ | ~~MP3s nach Verarbeitung aus Quellverzeichnis entfernen~~ | ✅ Behoben (verschoben nach Archivverzeichnis) |
| ~~8~~ | ~~Automatische PDF- und HTML-Erzeugung bei Verarbeitung~~ | ✅ Behoben |
| ~~9~~ | ~~`.env`-Datei im Publish-Ordner unterstützen~~ | ✅ Behoben |

### Priorität 2 – Wichtige Features

| # | Aufgabe | Status |
|---|---|---|
| ~~10~~ | ~~Name + Firma in Config + PDF-Ausgabe~~ | ✅ Erledigt |
| ~~11~~ | ~~Copyright-Hinweis / KI-Disclaimer im PDF~~ | ✅ Erledigt |
| ~~12~~ | ~~Peano-Logo im PDF~~ | ✅ Erledigt |
| ~~13~~ | ~~Rechtsklick: Erstellen + Zwischenablage + MessageBox~~ | ✅ Erledigt |
| ~~14~~ | ~~Erledigt-Funktion (Status im JSON, Filteransicht)~~ | ✅ Erledigt |
| ~~15~~ | ~~Sortierung nach ID / Projekt+ID~~ | ✅ Erledigt |

### Priorität 3 – Typen-System (Highlight-Feature)

| # | Aufgabe | Status |
|---|---|---|
| ~~16~~ | ~~Pro Typ einen individuellen Prompt in Config~~ | ✅ Erledigt |
| ~~17~~ | ~~Bei Verarbeitung **immer alle** Typ-Prompts generieren~~ | ✅ Erledigt |
| ~~18~~ | ~~Checkbox-Leiste in GUI zur Anzeige-Filterung~~ | ✅ Erledigt |
| ~~19~~ | ~~GUI-Anzeige abhängig von Checkbox-Auswahl~~ | ✅ Erledigt |
| ~~20~~ | ~~PDF-Inhalt abhängig von Checkbox-Auswahl~~ | ✅ Erledigt |
| ~~21~~ | ~~Individuelles HTML abhängig von Checkbox-Auswahl~~ | ✅ Erledigt |

### Priorität 4 – Komfort & Weiterverarbeitung

| # | Aufgabe | Status |
|---|---|---|
| ~~22~~ | ~~Drag & Drop von PDFs in ELO / Outlook / Explorer~~ | ✅ Erledigt |
| ~~23~~ | ~~Alternativ: Explorer-Fenster mit PDF öffnen~~ | ✅ Drag and Drop geht |
| 24 | Produktname „Platé.Johann" im UI umsetzen | ☐ |

---

## 14. Offene Fragen / Klärungsbedarf

- **Logo-Datei**: Peano-Logo als Bilddatei bereitstellen (Format? Auflösung?)
- ~~**Config-Format**~~: Bereits vorhanden – muss nur korrekt eingelesen werden
- **Drag & Drop Machbarkeit**: Technische Prüfung für ELO-Integration nötig
- **Typ-Liste**: Ist die Liste der Typen final oder kommen weitere hinzu?
- **Prompt-Vorlagen**: Sollen Default-Prompts pro Typ mitgeliefert werden?
- **PDF-Ablageort**: Stammverzeichnis = Arbeitsverzeichnis des Programms oder konfigurierbar?
- **HTML-Ablageort**: RAW-Verzeichnis = Unterordner „raw" im Arbeitsverzeichnis?
- **Titel-Erkennung**: Soll der Titel auch in der Übersichts-HTML erscheinen oder nur im Einzel-PDF/GUI?

---

*Letzte Aktualisierung: 17.03.2026 – Priorität 4 gestartet: #22 Drag & Drop abgeschlossen*