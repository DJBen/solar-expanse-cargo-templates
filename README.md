

# Solar Expanse Cargo Templates

Tired of clicking through *Add Resources* eight times to load the same Research
Laboratory cargo for the third mission in a row? This mod solves that pain:
define the load once as a template — or seed it straight from any building's
construction cost — and drop it into a mission with one click, multiplied by
however many you're building.

A BepInEx plugin for Solar Expanse that lets you define reusable resource cargo
templates and apply them to missions from the Plan Mission → Cargo step.

| Define Cargo Template | Add Resources from Templates with Multiples |
| --- | --- |
| <img width="1112" height="950" alt="Screenshot 2026-07-18 230925" src="https://github.com/user-attachments/assets/3f4a2646-8c90-43c4-991e-f06097f9adc0" /> | <img width="987" height="610" alt="Screenshot 2026-07-18 231117" src="https://github.com/user-attachments/assets/ecb0667b-9c0a-44e3-b950-5ccfc8a0e152" /> |

## What it does

### CARGO TEMPLATES window

A draggable **CARGO TEMPLATES** button appears near the top-left of the screen.
Click it to open the template editor:

- **+ NEW** creates a template; rename it inline (the focused field highlights
  so you can tell what you're editing). The chevron folds a template down to
  its header row.
- **+ RESOURCE** opens a searchable picker of every cargo-able resource
  (icon + name); each added item has an editable tonnage.
- **+ MODULE** opens a searchable picker of space module types (crew capacity
  noted); each added item has an editable count.
- **+ BUILDING** opens a searchable picker of every building you have unlocked
  (icon, name, and its cost as icon + tonnage). Choosing one merges the
  building's construction cost into the template, summing amounts for
  resources already present.
- **+ SC & LV** does the same for the construction cost of any unlocked
  spacecraft or launch vehicle.
- **OPTIONS → Show Unresearched** additionally lists research-locked modules,
  buildings and craft (marked *(unresearched)*), for planning ahead.
- **✕** removes an item or a whole template. Every change saves immediately.

### TEMPLATES dropdown (Plan Mission → Cargo)

A **TEMPLATES** button sits next to *Add Resources* / *Add Modules*. Its
dropdown lists your templates, one block per template:

- First line: template name, with a **× multiplier** input beside it
  (default 1) to apply the template N times over.
- Below: the contents as icon + amount (`100t`, `15kt`, `1.5Mt`) and module
  counts (`4× Crew Compartment`), wrapping onto as many lines as needed.
- Anything the origin can't fully cover is shown in **red**, re-checked live
  as you type in the multiplier.
- Click the block to append the template to the current cargo:
  - Resource amounts are clamped to what the origin actually has and the
    craft's remaining capacity; resources already in the cargo are **merged**
    (summed), never duplicated or overwritten.
  - Modules are pulled from the origin's actual stock, up to what's available.
  - **Crew modules** load a full crew by default; if the origin doesn't have
    enough people left, the last module takes a partial crew instead.

### Notes

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
