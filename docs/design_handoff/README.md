# Handoff: UX-Audit · Platé.Johann v1.0.7 → v1.1.0

> Zwölf priorisierte UX-Verbesserungen für das WPF-Hauptfenster und den
> Einstellungs-Dialog von **Platé.Johann**, fertig zur Umsetzung im
> bestehenden .NET-8/WPF-Repo `jonasyr/Platee.Johann`.

---

## Overview

Das Audit identifiziert **12 spürbare UX-Verbesserungen**, die ohne
Strukturänderung umgesetzt werden können. Die Brand-Identität
(Peano-Rot, Segoe UI / Consolas, 3-Pane-Layout) bleibt unberührt. Alle
12 Befunde sind in einem klickbaren **React-HTML-Prototyp** als
v1.1.0-Stand implementiert; dieser Prototyp ist die maßgebliche visuelle
Referenz für die Umsetzung in XAML.

Die Empfehlungen sind in drei Sprints / Patch-Releases gruppiert:

- **Sprint 1 → v1.0.8** — Footer & Header-Hygiene (Befunde 01, 02, 09, 10)
- **Sprint 2 → v1.0.9** — Orientierung & Filter (03, 05, 06, 07)
- **Sprint 3 → v1.1.0** — Empty-/Error-States & Politur (04, 08, 11, 12)

Die volle, navigierbare Audit-Doku liegt als
[`UX-Audit.html`](./UX-Audit.html) bei.

---

## About the design files

> **Diese Dateien sind Design-Referenzen, kein Production-Code.**

Im Bundle:

```
design_handoff_ux_audit/
├── README.md                       ← du bist hier
├── UX-Audit.html                   ← die ausführliche Audit-Doku mit Before/After
├── assets/                         ← Tokens-CSS, Johann-Icons (für die Audit-Doku)
└── prototype/
    ├── app/index.html              ← klickbarer v1.1.0-Prototyp (React + HTML)
    ├── app/styles.css              ← Basis-Stylesheet (v1.0.7-Look)
    ├── app/styles-v2.css           ← Audit-Patches (jede Klasse `.fNN-on` gated)
    ├── app/main.jsx                ← App-State + Findings-Schaltung + Tweaks-Panel
    ├── app/components/*.jsx        ← Pro Pane eine Komponente
    └── prototype/assets/           ← Johann-Icons + Token-CSS
```

Der Prototyp wurde in **React/HTML/CSS** gebaut, damit jede Änderung
toggle- und vergleichbar ist. Die Aufgabe ist **nicht**, den
React-Code zu portieren, sondern jede Änderung im **WPF-XAML** des
echten Repos nachzubauen. Der Prototyp dient als visuelle Wahrheit
für Maße, Farben, Verhalten — der CSS-Wert pro Token ist dabei der
Soll-Wert für die XAML-Resource.

> Beim Öffnen des Prototyps oben rechts den **„Tweaks"**-Toggle in der
> Toolbar aktivieren. Damit lässt sich jede der 12 Findings einzeln
> ein-/ausschalten und mit dem v1.0.7-Stand vergleichen. Es gibt
> zusätzlich Demo-Schalter für die Empty-States (kein API-Key,
> fehlender Eingangs-Ordner, kein Eintrag ausgewählt) und Test-Buttons
> für die Toast-Varianten.

## Fidelity

**High-fidelity.** Alle Farben, Schriften, Padding-Werte, Border-Radien
und State-Übergänge sind aus dem Design-System (`colors_and_type.css`,
HANDBUCH.html, MainWindow.xaml) abgeleitet und referenzieren existierende
WPF-Resourcen — nicht aus dem Nichts erfunden. Wo neue Resourcen nötig
sind, ist dies unten explizit dokumentiert.

---

## Repo-Orientierung

Die Findings betreffen folgende XAML-Dateien im echten Repo
`jonasyr/Platee.Johann`:

| Datei | Verantwortung | Betroffene Findings |
|---|---|---|
| `Platee.Johann.UI/MainWindow.xaml` | 3-Pane-Shell, TopBar, Toast, Status-Log-Popup | 01, 02, 03, 04, 05, 06, 08, 09, 10, 11, 12 |
| `Platee.Johann.UI/Views/SettingsView.xaml` | Einstellungs-Dialog, Bereich-Nav, Tabs | 07 |
| `Platee.Johann.UI/Views/NewEntryView.xaml` | „Neuer Eintrag"-Modal | (keine Änderung) |
| `Platee.Johann.UI/App.xaml` | App-weite Resourcen (Brushes, Styles, Geometrien) | 01, 02, 04, 05, 08, 09, 10, 12 (neue Resourcen) |

Vermutete ViewModel-Dateien (falls vorhanden, sonst Logik in MainWindow.xaml.cs):

| Datei | Verantwortung | Betroffene Findings |
|---|---|---|
| `MainViewModel` | Auswahl, Sortierung, Filter, Datums-Liste | 03 (Monatsgruppen), 05 (Sortier-Richtung), 11 (Liste-Untertitel) |
| `EntryDetailViewModel` | Sektionsauswahl, Done-State | 02, 06 |
| `SettingsViewModel` | Pfade, API-Key-Validierung | 04, 07 |

---

## Visual tokens — was in `App.xaml` ergänzt werden muss

Diese Resourcen müssen einmalig in der App-weiten Resource-Dictionary
angelegt werden (Werte sind aus dem Design-System exakt übernommen):

```xml
<!-- Status -->
<SolidColorBrush x:Key="WarningBrush"   Color="#C67C00" />
<SolidColorBrush x:Key="WarningBgBrush" Color="#FFF8EC" />
<SolidColorBrush x:Key="WarningLineBrush" Color="#F1DBA7" />

<!-- Toast / Erfolg -->
<SolidColorBrush x:Key="SuccessBrush"   Color="#27AE60" />
<SolidColorBrush x:Key="SuccessBgBrush" Color="#F4FBF7" />

<!-- Akzente bereits vorhanden? Sonst angleichen: -->
<SolidColorBrush x:Key="AccentBrush"      Color="#E63123" />
<SolidColorBrush x:Key="AccentDarkBrush"  Color="#C0392B" />
<SolidColorBrush x:Key="AccentDeepBrush"  Color="#C92D21" />
<SolidColorBrush x:Key="AccentSoftBrush"  Color="#FFF5F4" />
<SolidColorBrush x:Key="AccentLineBrush"  Color="#FFDBD8" />
<SolidColorBrush x:Key="AccentHoverBrush" Color="#FDF1F0" />
<SolidColorBrush x:Key="AccentBadgeBrush" Color="#FDE8E7" />

<!-- Typographie (existierend, hier zur Vollständigkeit) -->
<FontFamily x:Key="SansFamily">Segoe UI</FontFamily>
<FontFamily x:Key="MonoFamily">Consolas</FontFamily>

<!-- Shadow für Toasts (DropShadowEffect, neu) -->
<DropShadowEffect x:Key="ToastShadow"
                  ShadowDepth="2" BlurRadius="10"
                  Opacity="0.20" Color="Black" />
```

---

## Findings & XAML-Umsetzung

Jeder Befund unten: **Zieldatei → was zu ändern ist → exakte Werte → warum**.
Reihenfolge entspricht der Audit-Priorität.

---

### 01 · Aktions-Cluster im Detail-Footer auflösen *(Hoch, Sprint 1)*

**Ziel:** `MainWindow.xaml` — `Grid` am Boden der Detail-Pane mit den 5
Action-Buttons + Zoom-Steuerung.

**Status quo:** 5 Buttons (`HTML`, `PDF`, `E-Mail`, `Kopieren`,
`↻ Neu generieren`) und die Zoom-Buttons (`−`, `100%`, `+`) liegen
gleichwertig nebeneinander.

**Änderung:**

1. Die 4 Export-Buttons (`HTML`, `PDF`, `E-Mail`, `Kopieren`) in eine
   `StackPanel Orientation="Horizontal"` als Gruppe ohne Abstand —
   jeder Button hat `BorderThickness="1,1,0,1"`, der letzte (`Kopieren`)
   bekommt `BorderThickness="1"` für sauberen Abschluss. So entsteht der
   verbundene Cluster-Look.
2. Ein `Border Width="12"` (Spalt-Trenner) zwischen Cluster und
   `Neu generieren`.
3. `↻ Neu generieren` als eigener Button mit `BorderBrush="{StaticResource AccentBrush}"`,
   `Foreground="{StaticResource AccentBrush}"`, `FontWeight="SemiBold"`,
   `Background="Transparent"`. Hover-Trigger: `Background="{StaticResource AccentHoverBrush}"`,
   `Foreground="{StaticResource AccentDarkBrush}"`.
4. Zoom-Buttons aus dem Detail-Footer **entfernen** (siehe Befund 10).

**Begründung:** Schreibende KI-Aktion (`Neu generieren`) darf nicht wie
ein Export aussehen. Trenner kommuniziert „diese Aktion ist anders" ohne
neue Farbe, ohne Confirm-Modal.

**Referenz im Prototyp:**
`prototype/app/styles-v2.css` → `.f01-on` Block,
`prototype/app/components/DetailPane.jsx` → `<DetailToolbar>`.

---

### 02 · „Als erledigt markieren" — Umbruch & Platzierung *(Hoch, Sprint 1)*

**Ziel:** `MainWindow.xaml` — Detail-Header-Region.

**Status quo:** Der Button steht als 120-px-breite Box zwischen
Detail-Header und Abstract, bricht zweizeilig um („Als erledigt" /
„markieren") mit gemittelter Ausrichtung.

**Änderung:**

1. Button in den Detail-Header verschieben. Layout im Header:
   ```xml
   <Grid>
     <Grid.ColumnDefinitions>
       <ColumnDefinition Width="*" />
       <ColumnDefinition Width="Auto" />
     </Grid.ColumnDefinitions>
     <StackPanel Grid.Column="0">  <!-- TypeBadge + Project + Title + Meta --> </StackPanel>
     <Button Grid.Column="1"
             Style="{StaticResource DoneHeaderButtonStyle}"
             VerticalAlignment="Top" />
   </Grid>
   ```
2. `DoneHeaderButtonStyle`:
   - `MinWidth="200"`, `Padding="14,7"`, `FontSize="13"`
   - `WhiteSpace`-Äquivalent: `TextWrapping`/`TextBlock` mit
     `TextTrimming="None"`, einzeiliger Content
   - Default-State (nicht erledigt): `Background="{StaticResource AccentBrush}"`,
     `Foreground="White"`, `BorderBrush="{StaticResource AccentDeepBrush}"`,
     `FontWeight="SemiBold"`, Content: `"✓ Als erledigt markieren"`
   - Hover: `Background="{StaticResource AccentDarkBrush}"`
3. `Done-State` (Toggle): über `DataTrigger` auf `IsDone`:
   - `Background="{StaticResource BgBrush}"` (#F0F0F0)
   - `Foreground="{StaticResource TextBrush}"` (#333)
   - `BorderBrush="{StaticResource BorderMidBrush}"` (#CCC)
   - Content: `"✓ Erledigt — rückgängig"` (Check in `--pj-success` SemiBold)
4. Die alte In-Body-Done-Button-Box (`pj-detail-done-row`) entfernen.

**Begründung:** Button-Wrap-in-the-middle ist eines der härtesten
„unfinished UI"-Signale. Im Header rechts vom Titel folgt der Button
zusätzlich der Lesereihenfolge: Was ist das? → Was kann ich tun?

**Referenz im Prototyp:** `.f02-on` in `styles-v2.css`, `findings.f02`-Pfad
in `DetailPane.jsx`.

---

### 03 · Datums-Pane: Monats-Header + Live-Pending-Counts *(Hoch, Sprint 2)*

**Ziel:** `MainWindow.xaml` — `ListBox` der Datums-Pane (links).
ViewModel der Datums-Liste.

**Status quo:** Liste der Datums-Strings `17.03.`, `16.03.`, … ohne
Monatskontext; Count-Anzeige (sofern vorhanden) zeigt Gesamt-Anzahl,
ändert sich nicht beim Erledigen.

**Änderung (View):**

1. `CollectionViewSource` mit Gruppierung nach
   `PropertyGroupDescription("MonthYearKey")`.
   ```xml
   <CollectionViewSource x:Key="DatesGrouped" Source="{Binding Dates}">
     <CollectionViewSource.GroupDescriptions>
       <PropertyGroupDescription PropertyName="MonthYearKey" />
     </CollectionViewSource.GroupDescriptions>
   </CollectionViewSource>
   ```
2. `ListBox.GroupStyle` mit einem `Style` für `GroupItem`:
   ```xml
   <GroupStyle>
     <GroupStyle.HeaderTemplate>
       <DataTemplate>
         <TextBlock Text="{Binding Name}"
                    FontSize="10"
                    FontWeight="Bold"
                    Foreground="{StaticResource TextQuietBrush}"
                    Padding="14,10,14,4"
                    Background="{StaticResource BgLightBrush}"
                    TextTransform.. (use VisualBrush or convert in VM to UPPER)>
           "MÄRZ 2026" etc.
         </TextBlock>
       </DataTemplate>
     </GroupStyle.HeaderTemplate>
   </GroupStyle>
   ```
3. Datums-ItemTemplate: Format `17.03.` (kein Jahr — Jahr kommt aus dem
   Gruppen-Header). Daneben ein Pending-Count-Chip:
   - Wenn `PendingCount > 0`: `· 3` als 11-px-Caption in `TextFaintBrush`
   - Wenn `PendingCount == 0`: grüner `✓` (16-px) in `SuccessBrush`
4. Selected-State: links Border 3 px in `AccentBrush`, Hintergrund
   `AccentHoverBrush`, Text `AccentDarkBrush`, FontWeight SemiBold.
   (Heute nutzt der WPF-Code Windows-Native `#CCE8FF` — bitte auf das
   Peano-Rot-Pattern umstellen, konsistent mit Nav-Hover im Settings.)

**Änderung (ViewModel):**

1. `DateGroupItem`-Klasse:
   ```cs
   public class DateGroupItem {
     public string Key { get; set; }         // "2026-03-17"
     public string Display { get; set; }     // "17.03."
     public string MonthYearKey { get; set; } // "März 2026"
     public int Total { get; set; }
     public int Pending { get; set; }        // → property changed!
     public bool AllDone => Pending == 0;
   }
   ```
2. Wenn ein Entry seinen `IsDone`-Wert ändert, alle betroffenen
   `DateGroupItem.Pending` neu berechnen und `INotifyPropertyChanged`
   feuern.
3. `MONTHS_DE = ['Januar','Februar','März', …]`, `MonthYearKey` als
   `$"{months[m-1]} {y}"`.
4. `Nur unerledigte`-Filter: schließt zusätzlich Tage mit
   `Pending == 0` aus der Liste aus.

**Begründung:** Nutzer:innen verlassen sich bei Diktat-Wiederfinden
massiv aufs Datum. Monatskopf nimmt 22 px alle ~7 Tage und liefert den
Kontext, der heute komplett fehlt. Live-Pending-Counts machen Erledigung
sichtbar, ohne dass der Status-Log geöffnet werden muss.

**Referenz:** `.f03-on` Klasse, `prototype/app/main.jsx` →
`useMemo(dates, …)` mit `pending/total/allDone`, `DatePane.jsx`
Render-Pfad.

---

### 04 · Empty-State, fehlender API-Key, fehlender Eingangs-Ordner *(Hoch, Sprint 3)*

**Ziel:** Drei separate Patches in `MainWindow.xaml`.

**Status quo:** Wenn kein Eintrag ausgewählt ist, ist die Detail-Pane
leer (weiß). `polish.md` formuliert: „Buttons ausgegraut, keine
Verarbeitung", aber es gibt keinen Tooltip oder Hinweis warum.

**Änderung A — Leere Detail-Pane:**

1. `DataTrigger` auf `Binding SelectedEntry` `{x:Null}`: alternative
   Content-Template anzeigen.
2. Empty-Content:
   ```xml
   <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center"
               Margin="32">
     <Image Source="/Assets/Johann_256.png"
            Width="64" Height="64"
            Opacity="0.22" />
     <TextBlock Text="Wähle links ein Datum und einen Eintrag aus, um Abstract, Zusammenfassung und Aktionen zu sehen."
                Foreground="{StaticResource TextMutedBrush}"
                FontSize="13"
                TextWrapping="Wrap"
                TextAlignment="Center"
                MaxWidth="260"
                Margin="0,14,0,0" />
   </StackPanel>
   ```

**Änderung B — Disabled-Buttons Tooltip:**

`HTML`, `PDF`, `E-Mail`, `Kopieren`, `↻ Neu generieren` bekommen einen
`ToolTip`:
```xml
<Button IsEnabled="{Binding HasApiKey}">
  <Button.Style>
    <Style TargetType="Button" BasedOn="{StaticResource ActionButtonStyle}">
      <Style.Triggers>
        <Trigger Property="IsEnabled" Value="False">
          <Setter Property="ToolTip" Value="Kein API-Key in Einstellungen → KI hinterlegt." />
        </Trigger>
      </Style.Triggers>
    </Style>
  </Button.Style>
</Button>
```

**Änderung C — Banner bei fehlendem Ordner:**

Direkt unter der TopBar (zwischen TopBar und `pj-three-col`-Grid)
ein `Border` einfügen, sichtbar wenn `Binding PathStatus = "Missing"`:

```xml
<Border Visibility="{Binding InputPathMissing, Converter={StaticResource BoolToVis}}">
  <Border.Style>
    <Style TargetType="Border">
      <Setter Property="Padding" Value="14,8" />
      <Setter Property="Background" Value="{StaticResource AccentSoftBrush}" />
      <Setter Property="BorderBrush" Value="{StaticResource AccentLineBrush}" />
      <Setter Property="BorderThickness" Value="0,0,0,1" />
    </Style>
  </Border.Style>
  <StackPanel Orientation="Horizontal">
    <TextBlock Text="⚠" FontSize="14" Margin="0,0,10,0"
               Foreground="{StaticResource AccentDarkBrush}" />
    <TextBlock>
      <Run Text="Eingangs-Ordner nicht gefunden: " />
      <Run Text="{Binding InputPath}" FontFamily="{StaticResource MonoFamily}" />
    </TextBlock>
    <Hyperlink Command="{Binding OpenSettingsCommand}">Pfad ändern</Hyperlink>
  </StackPanel>
</Border>
```

Analog für `ApiKeyMissing` mit Warn-Tönen (`WarningBrush` / `#FFF8EC`).

**Begründung:** Der Erstkontakt mit der App ist exakt der Moment, an dem
nichts ausgewählt und alles ausgegraut ist. Ein Tooltip plus zwei klare
Banner ersparen dem Support-Postfach jede Woche dieselbe Frage und
respektieren die Tonalität (operational, kein Hype).

**Referenz:** `.f04-on` + `<DetailPane>`-Empty-Path, Banner in `main.jsx`.

---

### 05 · Sortier-Header eindeutig machen *(Mittel, Sprint 2)*

**Ziel:** `MainWindow.xaml` — Sort/Filter-Rail über der Entry-List.

**Status quo:** `↕ ID Projekt ☐ Nur unerledigte` in einer Zeile. Drei
Funktionen (Sort-Toggle, Sort-Spalte, Filter) gemischt. Aktive Spalte
nutzt Windows-Native-Blau (`#DCEBFB`) statt Peano-Rot.

**Änderung:**

1. Rail wird zweizeilig (`StackPanel` mit zwei `StackPanel`-Rows):
   - Reihe 1: `[SORTIEREN]` 10-px-Caps + `Nr`-Button + `Projekt`-Button
   - Reihe 2: `[FILTER]` 10-px-Caps + `☐ Nur unerledigte`
2. Aktive Sort-Spalte: Rotes Pill-Style — `Background="{StaticResource AccentBrush}"`,
   `Foreground="White"`, `BorderBrush="{StaticResource AccentDeepBrush}"`,
   `FontWeight="SemiBold"`. Inaktive: `Background="{StaticResource SurfaceBrush}"`,
   `BorderBrush="{StaticResource BorderMidBrush}"`, hover wie Nav.
3. Sort-Richtung als Pfeil-Glyph **direkt vor dem Spaltennamen** im
   aktiven Button: `↑` aufsteigend, `↓` absteigend. Default: `↑`. Klick
   auf bereits aktive Spalte toggelt die Richtung.

**ViewModel-Änderung:**

```cs
public string SortBy { get; set; }        // "id" | "project"
public string SortDir { get; set; }       // "asc" | "desc"
public ICommand SortByCommand { get; }    // toggles dir if same column
```

**Begründung:** Mehrdeutigkeit „aktiv vs. anklickbar" verschwindet,
sobald die aktive Spalte rot ist. Pfeil im aktiven Button löst zusätzlich
die Klickbarkeit der Sortier-Richtung mit, ohne separaten Toggle-Knopf.
Trennung von Sort/Filter ist Mental-Model-Hygiene.

**Referenz:** `.f05-on`, `EntryPane.jsx` → `findings.f05`-Pfad.

---

### 06 · Abschnitte-Filter neu beschriften und um Reset ergänzen *(Mittel, Sprint 2)*

**Ziel:** `MainWindow.xaml` — Footer der Datums-Pane (Abschnitt-Checkboxen).

**Status quo:** Caption „Abschnitte" + 8 Checkboxen. Aus dem Kontext
nicht erkennbar, dass diese das Detail-Rendering rechts steuern.

**Änderung:**

1. Caption: `Im Eintrag anzeigen` (links) + `Standard: nach Typ`
   (rechts, 10-px in `TextFaintBrush`, regular weight).
2. Checkbox-Liste bleibt.
3. **Neu:** Link-Button unter der Liste:
   ```xml
   <Button Style="{StaticResource LinkButtonStyle}"
           Command="{Binding ResetSectionsCommand}"
           Content="↻ Standard wiederherstellen"
           Foreground="{StaticResource AccentBrush}"
           HorizontalAlignment="Left" />
   ```
   `LinkButtonStyle`: Background `Transparent`, BorderThickness `0`,
   Padding `4,0,0,0`, FontSize `11`. Hover: `Foreground="{StaticResource AccentDarkBrush}"`,
   `TextDecorations="Underline"`.

**ViewModel-Änderung:** `ResetSectionsCommand` ruft die per-Typ-Defaults
zurück (gleiche Logik wie initialer Default).

**Referenz:** `.f06-on`, `DatePane.jsx` → `findings.f06`-Pfad.

---

### 07 · Einstellungen-Dialog: Scrollbar entfernen + Gruppen-Caps tracked *(Mittel, Sprint 2)*

**Ziel:** `Platee.Johann.UI/Views/SettingsView.xaml`.

**Status quo:** Rechte Content-Spalte zeigt sichtbaren horizontalen
Scrollbalken, obwohl Inhalt passt. Gruppen-Captions „Bereiche",
„Allgemein", „KI" sehen aus wie weitere klickbare Nav-Einträge; der
Wortlaut „Allgemein" kollidiert mit dem ausgewählten Eintrag „Allgemein".

**Änderung:**

1. Auf `ScrollViewer` der Content-Spalte:
   ```xml
   <ScrollViewer HorizontalScrollBarVisibility="Disabled"
                 VerticalScrollBarVisibility="Auto" />
   ```
   Plus alle Form-Felder `HorizontalAlignment="Stretch"` setzen, damit
   sie sich der Spaltenbreite anpassen.
2. Den `TextBlock "Bereiche"` an der Spitze der Nav-Spalte entweder
   ersatzlos entfernen (Dialog-Titel reicht) oder in `Kategorien`
   umbenennen — der Begriff darf nicht mit einem Nav-Eintrag kollidieren.
3. Gruppen-Captions umstellen:
   - `FontSize="10"`, `FontWeight="Bold"`, `Foreground="{StaticResource TextQuietBrush}"`
   - `TextElement.CharacterSpacing` oder `Typography.LetterSpacing` für 1.2 px Tracking
   - In den Strings selbst: Großbuchstaben (`ALLGEMEIN`, `KI`, `PROMPTS`) — WPF kennt kein `text-transform`, daher in der DataSource oder per `StringConverter` upper-casen
   - `Padding="10,14,10,4"`

**Begründung:** Ein Scrollbalken, der nichts scrollt, signalisiert
„hier ist etwas, was du nicht siehst". Getrackte Caps sind der einzige
Mechanismus in Segoe UI, der „Gruppen-Label" eindeutig macht.

**Referenz:** `.f07-on`, `SettingsDialog.jsx`.

---

### 08 · Sektions-Rhythmus im Detail entlasten *(Mittel, Sprint 3)*

**Ziel:** `MainWindow.xaml` — `SectionHeaderStyle` und Detail-Body.

**Status quo:** Heute rote H2-Header („Abstract", „Zusammenfassung"…)
mit 2-px-Block-Unterstrich; alle gleich gewichtet, treten oft direkt
hintereinander auf.

**Änderung:**

1. `SectionHeaderStyle` (rot 14-px Bold) bleibt — aber:
   - Untere Border auf 1 px reduzieren: `BorderThickness="0,0,0,1"`,
     `BorderBrush="{StaticResource AccentLineBrush}"`
   - `HorizontalAlignment="Left"`, `Padding="0,0,24,4"` —
     Underline endet kurz nach dem Text statt durch die ganze Breite
2. Spacing zwischen Sektionen: `Margin="0,14,0,22"`
   (war 18 px → 22 px).
3. Inner-Bold-Lines (durch Markdown-Renderer als `**Ziele**` o. ä.):
   `Foreground="{StaticResource TextBrush}"` (nicht rot!),
   `FontSize="13.5"`, `FontWeight="SemiBold"`, `Margin="0,12,0,4"`.

**Begründung:** Wenn alle Überschriften rot sind, ist keine rot.
Underline-Verkürzung + dezentere Farbe lassen Hierarchie atmen.

**Referenz:** `.f08-on`, kein JSX-Pfad (rein visuell).

---

### 09 · Tooltips, aria-label, Bell-Badge-Dot *(Mittel, Sprint 1)*

**Ziel:** `MainWindow.xaml` — TopBar (`🔔`, `⚙ Einstellungen`, `?`).

**Status quo:** Nur `Einstellungen` hat ein Label. `🔔` und `?` sind
ohne Tooltip, ohne `AutomationProperties.Name`.

**Änderung:**

1. `🔔`-Button:
   - `ToolTip="Status-Log öffnen (F2)"`
   - `AutomationProperties.Name="Status-Log"`
   - Wenn `Binding HasRunningJobs` true: kleiner 7-px-`Ellipse`-Badge
     oben rechts:
     ```xml
     <Ellipse Width="7" Height="7"
              Fill="{StaticResource AccentBrush}"
              Stroke="{StaticResource BgBrush}"
              StrokeThickness="1.5"
              HorizontalAlignment="Right"
              VerticalAlignment="Top"
              Margin="0,2,2,0"
              Visibility="{Binding HasRunningJobs, Converter={StaticResource BoolToVis}}" />
     ```
2. `⚙ Einstellungen`-Button: `ToolTip="Einstellungen öffnen (Strg+,)"`.
3. `?`-Button: `ToolTip="Handbuch öffnen (F1)"`,
   `AutomationProperties.Name="Handbuch"`. Plus `InputBindings`:
   `<KeyBinding Key="F1" Command="{Binding OpenHandbookCommand}" />`.
4. `InputBindings` in MainWindow auch für F2 (Log) und Strg+, (Settings).

**Referenz:** `.f09-on`, `TopBar.jsx`.

---

### 10 · Zoom in eigene Status-Zeile verschieben *(Niedrig, Sprint 1)*

**Ziel:** `MainWindow.xaml` — neue Statuszeile am unteren Fensterrand.

**Status quo:** `−`, `100%`, `+` sitzen im Detail-Footer neben den
Export-Buttons.

**Änderung:**

1. Zoom-Steuerung aus `pj-detail-toolbar-right` entfernen.
2. Neue Row im äußersten `Grid` (`Height="22"`):
   ```xml
   <Border Grid.Row="3"
           Height="22"
           Background="{StaticResource BgBrush}"
           BorderBrush="{StaticResource BorderBrush}"
           BorderThickness="0,1,0,0">
     <Grid>
       <Grid.ColumnDefinitions>
         <ColumnDefinition Width="*" />
         <ColumnDefinition Width="Auto" />
       </Grid.ColumnDefinitions>
       <StackPanel Grid.Column="0" Orientation="Horizontal" Margin="10,0">
         <TextBlock Text="{Binding StatusText, FallbackValue=Bereit}" />
         <TextBlock Text=" · " Foreground="{StaticResource TextFaintBrush}" Margin="10,0" />
         <TextBlock>
           <Run Text="Ausgabe in " />
           <Run Text="{Binding OutputPath}" FontFamily="{StaticResource MonoFamily}" />
         </TextBlock>
         <TextBlock Text=" · " Foreground="{StaticResource TextFaintBrush}" Margin="10,0" />
         <TextBlock Text="{Binding WhisperVersion}" />
       </StackPanel>
       <StackPanel Grid.Column="1" Orientation="Horizontal" Margin="0,0,10,0">
         <Button Content="−" Width="18" Height="18" Command="{Binding ZoomOutCommand}" />
         <TextBlock Text="{Binding ZoomDisplay}" MinWidth="38" TextAlignment="Center" />
         <Button Content="+" Width="18" Height="18" Command="{Binding ZoomInCommand}" />
       </StackPanel>
     </Grid>
   </Border>
   ```
3. Zoom-Buttons: `Background="Transparent"`, `BorderBrush="Transparent"`,
   on hover `Background="{StaticResource BgPaneBrush}"`,
   `BorderBrush="{StaticResource BorderMidBrush}"`.

**Begründung:** Status-Zeile ist Windows-nativ und passt zur Tonalität.
Trennung der Konzepte reduziert kognitive Last für den Footer.

**Referenz:** `.f10-on`, `<StatusBar>` in `main.jsx`.

---

### 11 · Typ + Dauer in Listeneintrag kombinieren *(Niedrig, Sprint 3)*

**Ziel:** `MainWindow.xaml` — `DataTemplate` der Entry-Liste (Mitte-Pane).

**Status quo:** Listen-Item zeigt unter der ersten Zeile nur den Typ
(`Aufgabe`); die Dauer steht ausschließlich im Detail-Header.

**Änderung:** Untertitel umbauen auf
```xml
<TextBlock FontSize="11" Foreground="{StaticResource TextMutedBrush}">
  <Run Text="{Binding Type}" />
  <Run Text=" · " />
  <Run Text="{Binding Duration}" />
</TextBlock>
```

**Begründung:** In gemischter Liste sucht der Nutzer Projekt + Typ. Den
Typ-Badge-im-Detail-Header gibt es zusätzlich — ein Untertitel mit
zweiter Info (Dauer) macht die Liste informationsreicher, ohne sie zu
verlängern.

**Referenz:** `.f11-on`, `EntryPane.jsx` `findings.f11`-Zweig.

---

### 12 · Toast-Grammatik um Warn/Fehler erweitern *(Niedrig, Sprint 3)*

**Ziel:** Neuer `UserControl` `Toast.xaml`. Anzeige als overlay in
`MainWindow.xaml` an Position `(top: 70, right: 18)`, `MaxWidth="340"`.

**Status quo:** Heute existiert ein „gespeichert"-Toast in Settings (im
Footer als grüner Text). Keine konsistente Warn-/Fehler-Variante.

**Änderung:**

1. `Toast`-Modell:
   ```cs
   public enum ToastTone { Ok, Warn, Error }
   public class ToastItem {
     public Guid Id { get; init; }
     public ToastTone Tone { get; init; }
     public string Title { get; init; }
     public string Message { get; init; }
     public Action OnDetails { get; init; }  // nur bei Error
   }
   ```
2. `ToastsViewModel` mit `ObservableCollection<ToastItem> Toasts`,
   Auto-Dismiss nach 5.2 s (`DispatcherTimer`). Hover stoppt den Timer
   (`MouseEnter`/`MouseLeave`-Handler).
3. `Toast.xaml` Layout:
   - `Border BorderThickness="1,1,1,1"`, BorderBrush `BorderMidBrush`,
     `Background="{StaticResource SurfaceBrush}"`,
     `Padding="10,12,12,14"`, `CornerRadius="6"`,
     `Effect="{StaticResource ToastShadow}"`.
   - **Linker Akzent-Strich** über `BorderThickness="4,1,1,1"`,
     je nach `Tone` eine `BorderBrush` per `Style` `DataTrigger`:
     - `Ok` → `SuccessBrush`
     - `Warn` → `WarningBrush`
     - `Error` → `AccentBrush`
   - Icon `✓` / `⚠` / `!` in passender Farbe (14 px), Titel SemiBold,
     Message muted, Close-`✕` rechts.
   - Bei `Error`: Hyperlink-Button „Details im Status-Log" → öffnet das
     `StatusLogPopover`.
4. `MainWindow.xaml` overlay:
   ```xml
   <ItemsControl ItemsSource="{Binding Toasts.Items}"
                 Panel.ZIndex="300"
                 HorizontalAlignment="Right"
                 VerticalAlignment="Top"
                 Margin="0,70,18,0">
     <ItemsControl.ItemsPanel>
       <ItemsPanelTemplate><StackPanel /></ItemsPanelTemplate>
     </ItemsControl.ItemsPanel>
     <ItemsControl.ItemTemplate>
       <DataTemplate><local:ToastControl /></DataTemplate>
     </ItemsControl.ItemTemplate>
   </ItemsControl>
   ```
5. Animation: kleine Slide-in-Animation 8 px → 0 px, Opacity 0 → 1,
   180 ms, Easing `ease-out`. WPF `Storyboard` mit `DoubleAnimation` auf
   `TranslateTransform.X` + `Opacity`.

**Begründung:** Heute gibt es nur den Status-Log als Fehler-Kanal — der
muss aktiv geöffnet werden. Konsistente Toast-Tonalität ist der erste
Schritt zu vertrauenswürdiger Background-Verarbeitung.

**Referenz:** `.pj-toast-tray` + `<ToastTray>` in `main.jsx`,
`pushToast()`-Helper.

---

## Behavior changes — Übersicht für ViewModel

| Was | Wo (Finding) | Implementierung |
|---|---|---|
| Sort-Richtung toggelbar | 05 | `SortDir`-Property + Logik im `SortByCommand` |
| Pending-Counts pro Datum | 03 | `DateGroupItem.Pending` + `PropertyChanged` bei `IsDone`-Wechsel |
| Tage mit 0 Pending ausblenden bei Filter | 03 | `CollectionViewSource` Filter ergänzen |
| Reset-Sections Command | 06 | Setzt Sections zurück auf per-Typ-Defaults |
| Toast-Queue | 12 | `ObservableCollection<ToastItem>` + `DispatcherTimer` |
| Fehlerzustands-Banner | 04 | `InputPathMissing`, `ApiKeyMissing` Boolean-Properties |
| Bell-Dot bei laufenden Jobs | 09 | `HasRunningJobs` derived from log items |
| F1, F2, Strg+, Shortcuts | 09 | `InputBindings` auf `MainWindow` |

---

## Assets

- Alle Icons als **Unicode-Glyphen** oder **Emoji** — siehe Design-System
  Iconography-Sektion. Keine neuen Bildassets nötig.
- Johann-Mark für Empty-State: existierende `Johann_256.png` (Opacity
  `0.22` setzen).
- Falls Lucide-Fallbacks nötig werden (sehr selten — die Audit-Findings
  brauchen keine), gilt die System-Regel: 16 × 16, `stroke-width="1.75"`,
  `currentColor` — und immer dem Nutzer flaggen.

---

## Bundled files

```
design_handoff_ux_audit/
├── README.md                                ← du bist hier
├── UX-Audit.html                            ← Audit-Dokument mit Before/After
├── assets/                                  ← Design-System-Token-CSS + Icons
└── prototype/
    ├── app/index.html                       ← Klickbarer v1.1.0-Prototyp
    ├── app/main.jsx                         ← App-Logik, Tweaks-Schaltung
    ├── app/styles.css                       ← v1.0.7-Basis-Styles
    ├── app/styles-v2.css                    ← Audit-Patches (`.fNN-on`)
    ├── app/tweaks-panel.jsx                 ← Tweaks-UI-Framework (für die Toggles)
    ├── app/data.js                          ← Demo-Daten (kann verworfen werden)
    └── app/components/
        ├── DatePane.jsx                     ← Findings 03, 06
        ├── EntryPane.jsx                    ← Findings 05, 11
        ├── DetailPane.jsx                   ← Findings 01, 02, 04, 08, 10
        ├── TopBar.jsx                       ← Finding 09
        ├── SettingsDialog.jsx               ← Finding 07
        ├── NewEntryDialog.jsx               ← (unverändert)
        ├── StatusLogPopover.jsx             ← (unverändert, von 12 referenziert)
        └── shared.jsx                       ← TypeBadge, IconBtn, Select
```

Den **Prototyp lokal starten:**
```bash
cd prototype/app
python3 -m http.server 8000     # oder beliebiger Static-Server
# Browser: http://localhost:8000/
```

(Funktioniert nur als HTTP-served, nicht per `file://` — Babel-Babel
verträgt's nicht.)

---

## Workflow mit Claude Code

Vorgeschlagener Ablauf:

```bash
# Im Repo-Root:
mkdir -p docs/design_handoff
# Diesen Handoff-Ordner dort entpacken.

# Dann Claude Code mit gezielten Tasks füttern, einer pro Finding:
claude "Implementiere Finding 01 aus docs/design_handoff/README.md
        — Aktions-Cluster im Detail-Footer in MainWindow.xaml.
        Erzeuge keine neuen Branches; arbeite in
        feature/ux-audit-sprint-1."
```

Tipp: ein PR pro Sprint (drei PRs insgesamt) hält die Changes
überschaubar und reviewbar. Pro PR sind das je 3–4 Findings,
gemeinsam getestet.

---

*Platé.Johann · UX-Audit · ein interner Bericht von Peano · 20.05.2026*
