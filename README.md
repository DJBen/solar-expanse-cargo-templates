# Solar Expanse Cargo Templates

Tired of clicking through *Add Resources* eight times to load the same Research
Laboratory cargo for the third mission in a row? This mod solves that pain:
define the load once as a template — or seed it straight from any building's
construction cost — and drop it into a mission with one click, multiplied by
however many you're building.

A BepInEx plugin for Solar Expanse that lets you define reusable resource cargo
templates and apply them to missions from the Plan Mission → Cargo step.

## What it does

### CARGO TEMPLATES window

A draggable **CARGO TEMPLATES** button appears near the top-left of the screen.
Click it to open the template editor:

- **+ NEW** creates a template; rename it inline (the focused field highlights
  so you can tell what you're editing).
- **+ ADD RESOURCE** opens a searchable picker of every cargo-able resource
  (icon + name); each added item has an editable tonnage.
- **+ FROM BUILDING COST** opens a searchable picker of every building you have
  unlocked (icon, name, and its cost as icon + tonnage). Choosing one merges
  the building's construction cost into the template, summing amounts for
  resources already present.
- **✕** removes an item or a whole template. Every change saves immediately.

### TEMPLATES dropdown (Plan Mission → Cargo)

A **TEMPLATES** button sits next to *Add Resources* / *Add Modules*. Its
dropdown lists your templates, one block per template:

- First line: template name, with a **× multiplier** input beside it
  (default 1) to apply the template N times over.
- Below: the resource list as icon + amount (`100t`, `15kt`, `1.5Mt`),
  wrapping onto as many lines as needed.
- Amounts the origin can't fully cover are shown in **red**, re-checked live
  as you type in the multiplier.
- Click the block to append those resources to the current cargo. Amounts are
  clamped to what the origin actually has and the craft's remaining capacity.

### Notes

- **Resources only** — modules are intentionally not part of templates.
- The dropdown does **not** appear in cyclical-mission planning, only the
  regular Plan Mission window.
- Templates are global (not per-save) and persist in
  `BepInEx/config/SolarExpanseCargoTemplates.json` (hand-editable JSON).

## Installation

1. Install **BepInEx 5.4** if you haven't already:
   https://docs.bepinex.dev/articles/user_guide/installation/index.html
2. Run Solar Expanse once after installing BepInEx to generate the
   `BepInEx/plugins/` folder.
3. Download the latest release zip.
4. Extract `SolarExpanseCargoTemplates.dll` from the zip into:
   ```
   Solar Expanse/BepInEx/plugins/
   ```
5. Launch the game. The **CARGO TEMPLATES** button will appear near the
   top-left; the **TEMPLATES** button appears on the Plan Mission Cargo step.

## Building from source

Requires the .NET SDK and a Solar Expanse install with BepInEx 5.4.

```sh
export SOLAR_EXPANSE_ROOT="/path/to/Solar Expanse"
dotnet build src/mod
```

The build validates the game path and copies the DLL straight into
`BepInEx/plugins/`.
