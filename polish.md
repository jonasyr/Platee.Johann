

## 🔄 Verarbeitung & Statusanzeige

* Sichtbare **Status-/Fortschrittsanzeige**, wenn das Tool arbeitet
* Anzeige als **Progress-Element (z. B. Kreis oder Balken)** in der UI
* Integration in obere Leiste:

  * Icon zeigt aktuellen Zustand (idle / läuft / fertig)
  * Bei laufender Verarbeitung: **animierter Fortschrittskreis**
* Klick auf Status öffnet **Detailfenster**:

  * Laufende Prozesse (Detailansicht)
  * Abgeschlossene Aufgaben (Notifications)
  * Optionen:

    * Liste komplett leeren
    * Nur erledigte Einträge löschen

---

## 🧭 Navigation & Layout

* **Settings-Bereich überarbeiten**:

  * Aktuell unten → nach **oben verlagern**
  * Button ändern von:

    * `[Zahnrad]` → `[Zahnrad] Einstellungen`
  * Integration in obere Leiste (gemeinsam mit Statusanzeige sinnvoll)

---

## ✅ UX-Verbesserungen (Interaktionen & Buttons)

### Allgemein

* Alle Buttons mit **Tooltips versehen**

### „Als erledigt markieren“

* Aktuelle Position/UX unklar → verbessern:

  * Alternative Platzierung:

    * Bei anderen Aktionsbuttons im Detailbereich (rechte Seite)
    * Oder direkt im Detail-View statt Checkbox
* Checkbox entfernen:

  * Redundant, da Status bereits im UI sichtbar

### „Verarbeiten“-Button

* Umbenennen in etwas Klareres, z. B.:

  * „Sektionen neu generieren“
  * „Daten neu verarbeiten“
* Erweiterung:

  * **Rechtsklick-Menü**:

    * Liste aller Sektionen
    * Möglichkeit, gezielt einzelne Sektionen neu zu verarbeiten

---

## 🔍 Detailansicht & Skalierung

* Unten rechts im Detailbereich:

  * **Zoom-/Skalierungssteuerung**

    * Anzeige: `- 100% +`
    * Schrittweise Anpassung (z. B. 80% / 100% / 120%)
  * Ziel: bessere Lesbarkeit / flexible UI-Nutzung

---

## 🐛 Kritischer Bug (Nummerierung)

* Problem:

  * Bei hoher Interaktion während Verarbeitung:

    * Nummerierung bleibt stehen
    * gleiche Nummer wird mehrfach vergeben ❌
* Anforderungen:

  * **Nummerierung muss immer eindeutig und fortlaufend sein**
  * Lösungsideen:

    * Zentrale ID-Quelle (Single Source of Truth)
    * Atomare Vergabe (z. B. Counter + Lock / Queue)
    * Fallback:

      * **Persistente Speicherung** (z. B. Datei/DB), falls nötig
* Wichtig:

  * Muss auch unter Stressbedingungen stabil bleiben

---

## ⚙️ Parallelität & Stabilität

* Während Verarbeitung:

  * Nutzer soll weiterhin:

    * navigieren
    * filtern
    * Aktionen ausführen
* Anforderungen:

  * Keine Crashes / Inkonsistenzen
  * Keine komplizierten Deadlocks
* Fallback-Strategie:

  * Kritische Buttons:

    * **deaktivieren (grau)** während sensibler Prozesse
  * Fokus auf:

    * einfache, robuste Lösung statt komplexer Parallelisierung

---

## 💡 Gesamtziel

* Klarere UX
* Transparente Prozesse
* Stabilität auch unter Last
* Mehr Kontrolle für den Nutzer (gezielte Aktionen, Feedback, Skalierung)

---

## 🧪 Manueller Regressionstest: Settings-Tabs & Prompt-Felder

1. Einstellungen öffnen.
2. In einen Prompt-Tab wechseln (z. B. „Zusammenfassung“) und mehrere Zeilen eingeben.
3. Auf einen anderen Tab wechseln und danach wieder zurück.
4. Erwartung: Der aktive Tab springt nicht ungewollt auf einen anderen Tab.
5. „Speichern“ klicken, Fenster schließen und erneut öffnen.
6. Erwartung: Eingaben sind gespeichert; Tab-Wechsel bleibt stabil, auch bei erneutem Öffnen derselben Settings-Instanz.

