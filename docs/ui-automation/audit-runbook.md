# Audit-Runbook v1.5.0 (#111)

Ablauf des einmaligen Audits der laufenden App. Jeder Schritt nennt das **Soll** und seine
**Quelle**; eine Abweichung ohne Quelle ist ein *Vorschlag*, kein Befund.

## Sicherheitsregeln

- Johann läuft nur in einer Sandbox (`ui-driver sandbox new`), nie gegen `Documents\Johann`.
  Der Wächter prüft vor jedem Start; ungültige `JOHANN_*`-Werte verweigern den Start.
- Die Team-Datei auf `Z:` wird nur **gelesen** (kopiert). „Global“ speichert in die Sandbox-Kopie.
- Outlook nur in Flow 7 und nur unter Aufsicht des Nutzers; Entwürfe danach verwerfen.
- Vor jedem `start`: kein anderer Johann läuft. Nach dem Lauf: echte Dateien per Prüfsumme
  gegen den Stand vorher vergleichen.
- Tastatur-Schritte brauchen Johann im Vordergrund — der Nutzer tippt währenddessen nicht.

## Werkzeug

`dotnet run --project tools/ui-driver --no-build -- <befehl>` — `sandbox new|check`, `start`,
`windows`, `tree`, `click [--mouse]`, `rightclick`, `doubleclick`, `type`, `key`,
`screenshot [--of]` (PrintWindow, auch verdeckt), `wait-for`, `clipboard`, `close`, `silence`.

## Quellen (Soll)

`RELEASE_NOTES.md` (RN) · `HANDBUCH.html`/`README.md` (HB) · `CLAUDE.md` (CM) ·
Issue-Texte (#nr) · `docs/ui-automation/design.md` (Spec).

## Flows

| # | Flow | Schritte | Soll (Quelle) |
|---|---|---|---|
| 1 | Start | Start mit Kopie der echten Daten; Titel, Dialoge, Neuigkeiten beim ersten Start einer neuen Version; zweiter kurzer Lauf ohne `.env` | Titel trägt die ausgelieferte Version; Neuigkeiten erscheinen einmal je Version (CM „Release notes window“); ohne `.env` Einrichtungsfrage bzw. Viewer-Modus (CM „No-Op stubs“) |
| 2 | Verarbeitung | D1–D8 in `eingang\`; Diktier-Knopf ohne Mikrofon | Eintrag je Diktat, deutsche Abschnitte, Transkript in gesprochener Sprache (#58); D6 korrigiert (Korrekturliste, CM); D7 erfindet nichts (RN 1.5.0 „Besser“); D8 verständliche 25-MB-Meldung vor dem Upload (#77, RN) |
| 3 | Liste | Sortieren, „Nur unerledigte“, erledigt, lange Titel, Doppelklick Trennlinie, Löschen (Entf/Rechtsklick/Knopf), Papierkorb | Auswahl bleibt, kein Flackern (#100); „…“ + Tooltip, Haken sichtbar (#96); Rückfrage mit „Nein“ vorausgewählt, `output\_Papierkorb`, Tag verschwindet (#55, RN) |
| 4 | Detail | alle Abschnitte, ein/aus, auf Knopfdruck, Kopiersymbol, „Kopieren“, Transkript bearbeiten → neu generieren, Zoom | Kopieren = sichtbare Abschnitte in Ansichtsreihenfolge, kein `**` (#56, RN); Markdown gerendert, Unterpunkte eingerückt (CM); Zoom 50–200 % (CM) |
| 5 | Exporte | PDF, HTML-Übersicht | Fettdruck, eingerückte Unterpunkte, kein `**` in allen Abschnitten (#83, RN) |
| 6 | Einstellungen | alle Sektionen; KI-Modell + Kostenkarte; Vorlagen anlegen/umbenennen/löschen, Speicherziel; Korrekturliste; Layout | Modellwahl gilt nur für mich, Kosten je Diktat (#71, RN); Kästchen nicht an der Leiste (#79); Farben/Kontrast wie #97 |
| 7 | Mail (beaufsichtigt) | „Aufgaben“ an D1, „E-Mail“ an D4 | intern mit Begleittext + PDF + Signatur, extern förmlich ohne Anhang (#57, RN) |
| 8 | Neuigkeiten, Handbuch | Knopf, Puls, Darstellung | Rubriken Neu/Besser/Behoben; Handbuch öffnet (CM) |
| 9 | Tastatur, Kontrast | Tab-Reihenfolge, Fokus, Kontrast aus Screenshots | jeder Knopf erreichbar, Fokus sichtbar, WCAG AA (#97) |

## Befund-Format

Flow · Soll (Quelle) · Ist · Screenshot · Schwere (**Bug / UX / Kosmetik / Doku-Abweichung /
Vorschlag**) · Reproduktion. Jeder Befund wird vor dem Bericht ein zweites Mal reproduziert.
