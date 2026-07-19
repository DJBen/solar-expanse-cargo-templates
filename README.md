# Solar Expanse Cargo Templates

A BepInEx plugin for Solar Expanse that lets you define resource cargo templates
once and apply them to missions from the Plan Mission → Cargo step.

## What it does

**CARGO TEMPLATES window (top bar)** — a button next to the notification /
other mod buttons opens the template editor:

- **+ NEW** creates a template; edit its name inline.
- **+ ADD RESOURCE** opens a picker of every cargo-able resource; each item has
  an editable tonnage.
- **✕** removes an item or a whole template.
- Everything saves immediately.

**TEMPLATES ▾ (Plan Mission → Cargo)** — next to *Add Resources* / *Add
Modules*. The dropdown only lists templates:

- Click a template to append its resources to the current cargo. Amounts are
  clamped to what the origin actually has and the craft's remaining capacity.
- Each row has a **× multiplier** input (default 1) to apply the template N
  times over.
- Resources the origin can't fully cover at the chosen multiplier are shown in
  **red**.

Notes:

- **Resources only.** Modules are intentionally not part of templates.
- The dropdown does **not** appear in cyclical-mission planning — only the
  regular Plan Mission window.
- Templates are global (not per-save) and persist in
  `BepInEx/config/SolarExpanseCargoTemplates.json`.

## Installation

1. Install **BepInEx 5.4** and run the game once.
2. Download the latest release zip and extract `SolarExpanseCargoTemplates.dll`
   into `Solar Expanse/BepInEx/plugins/`.
3. Launch the game.

## Building from source

Requires the .NET SDK and a Solar Expanse install with BepInEx 5.4.

```sh
export SOLAR_EXPANSE_ROOT="/path/to/Solar Expanse"
dotnet build src/mod
```

The build validates the game path and copies the DLL straight into
`BepInEx/plugins/`.
