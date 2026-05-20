# UI Kit — Platé.Johann Desktop App

A clickable HTML recreation of the WPF main window (`Platee.Johann.UI/MainWindow.xaml`), the Einstellungen dialog (`Views/SettingsView.xaml`), and the Neuer Eintrag dialog (`Views/NewEntryView.xaml`).

The kit is **cosmetic, not production code** — it reproduces the visual language and the click-through flow, not the real `.NET` data layer.

## What you can do

- Click a **date** in the left pane to filter entries to that day.
- Click an **entry** in the middle pane to load its detail on the right.
- Click **⚙ Einstellungen** in the top bar to open the modal Settings window with its sidebar navigation.
- Click **+ Neues Element** to open the New Entry dialog.
- Click **🔔** in the top bar to open the floating Status-Log popover.
- Toggle the **Als erledigt markieren** button on a detail to flip the green ✓ in the entry row.
- Use the **− 100% +** controls in the detail toolbar to zoom the right pane.

## Files

```
ui_kits/desktop-app/
├── README.md            ← this file
├── index.html           ← entry point — boots React + Babel
├── data.js              ← sample dictation entries
├── main.jsx             ← top-level App, state, layout
└── components/
    ├── TopBar.jsx
    ├── DatePane.jsx
    ├── EntryPane.jsx
    ├── DetailPane.jsx
    ├── StatusLogPopover.jsx
    ├── SettingsDialog.jsx
    ├── NewEntryDialog.jsx
    └── shared.jsx       ← TypeBadge, ProgressBar, helpers
```

## Source of truth

- Layout & colours: `MainWindow.xaml`
- Settings dialog: `Views/SettingsView.xaml`
- New entry dialog: `Views/NewEntryView.xaml`
- Tokens: `colors_and_type.css` at repo root

If you change a colour or radius, change it in `colors_and_type.css` and let the kit pick it up — the kit doesn't redefine tokens.
