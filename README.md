# Solar Expanse Cargo Templates

A BepInEx plugin for Solar Expanse that adds an **Add from template** button to the
Plan Mission → Cargo step, so you can save a combination of resources once and
re-apply it to future missions with one click.

## What it does

A **TEMPLATES ▾** button appears next to *Add Resources* / *Add Modules* on the
Cargo step of the Plan Mission window. Click it to open a dropdown:

- **Apply a template** — each saved template is listed with its contents
  (e.g. `Mars Base Kit — Metal 200t · Water 50t`). Clicking it appends those
  resources to the current cargo. Amounts are clamped to what's actually
  available at the origin and to the spacecraft's remaining capacity.
- **✕** — delete a template.
- **+ Save current cargo as template** — snapshots the resources currently in
  the cargo list (fuel and modules are excluded) as a new template.

Notes:

- **Resources only.** Modules are intentionally not part of templates.
- Templates do **not** appear in cyclical-mission planning — only the regular
  Plan Mission window.
- Templates are global (not per-save) and persist in
  `BepInEx/config/SolarExpanseCargoTemplates.json`. You can rename a template
  by editing the `name` field in that file.

## Installation

1. Install **BepInEx 5.4** and run the game once.
2. Download the latest release zip and extract `SolarExpanseCargoTemplates.dll`
   into `Solar Expanse/BepInEx/plugins/`.
3. Launch the game and open Plan Mission → Cargo.

## Building from source

Requires the .NET SDK and a Solar Expanse install with BepInEx 5.4.

```sh
export SOLAR_EXPANSE_ROOT="/path/to/Solar Expanse"
dotnet build src/mod
```

The build validates the game path and copies the DLL straight into
`BepInEx/plugins/`.
