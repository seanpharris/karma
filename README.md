# Karma

Karma is a multiplayer 2D life-sim RPG prototype with cozy visuals, absurd objects,
PvP, procedural worlds, and a central Ascension/Descension score.

Players start a generated world, meet generated NPCs, help or harm people, prank,
trade, fight, betray, protect, and compete for the highest or lowest karma on the
server. Death causes a **Karma Break**: the player respawns, but their karma path
and perks reset.

## Direction

- Engine: Godot 4 with C#
- Initial player count: 4 per world, configurable toward larger worlds
- Authority: server-owned world state, client sends intent
- Karma is uncapped in both directions
- Positive path: Ascension
- Negative path: Descension
- High leaderboard title: Saint
- Low leaderboard title: Scourge

## First Prototype Goals

- Top-down 2D movement with stamina-limited left Shift sprinting
- A tiny generated town map
- NPC interaction choices that Ascend or Descend the player
- Weird interactible objects such as whoopie cushions and deflated balloons
- Karma tiers and Karma Break reset behavior
- Code structured so an authoritative server can own game state later

## Project Layout

- `docs/` design notes
- `scenes/` Godot scenes
- `scripts/Core/` shared game concepts and state
- `scripts/Data/` item, NPC, and karma models
- `scripts/World/` world generation and interactibles
- `scripts/Player/` player controller
- `scripts/Npc/` NPC interaction scripts
- `scripts/Net/` multiplayer/server boundary notes and stubs

## Fast Launch

- Main menu path: `powershell -ExecutionPolicy Bypass -File .\tools\run-main-menu.ps1`
- Direct gameplay path: `powershell -ExecutionPolicy Bypass -File .\tools\run-gameplay.ps1`

See [`docs/testing-launch-paths.md`](docs/testing-launch-paths.md) for direct Godot commands.
See [`docs/sprite-modeling-status.md`](docs/sprite-modeling-status.md) for why the current sprite-modeling difference is mostly pipeline/animation support rather than a dramatic visual upgrade.

## Building Sprite Pipeline

This repository now includes a Python sprite pipeline for converting raw building artwork into clean, consistent 96x96 transparent PNG sprites for a top-down pixel game.

The pipeline:
- removes a keyed background color from a configured flat background
- supports any strong color such as magenta, green, cyan, red, blue, or user-specified hex
- uses edge-connected masking so only border-connected background pixels are removed
- optionally detects the background color from the image border with `--auto-bg`
- optionally cleans up edge anti-aliasing with `--edge-cleanup`
- optionally decontaminates visible sprite edge pixels with `--decontaminate-bg`
- crops to the visible object
- resizes proportionally with nearest-neighbor scaling
- centers sprites on a transparent `96x96` canvas by default
- supports per-sprite canvas, fit, anchor, and footprint metadata from a manifest
- quantizes palette colors while preserving alpha
- optionally adds a 1px dark outline
- generates fixed-grid sprite sheets or variable-size atlases with JSON metadata

Best practice for generated building sprites:
- generate the building on a flat, high-contrast background color
- choose a background color that does not appear in the building
- keep `#ff00ff` as the default key color, or use `--bg "#00ff00"` for green, `#00ffff` for cyan, etc.
- if the background varies slightly, use `--auto-bg`
- if halos remain, use `--edge-cleanup`
- if the sprite has background contamination on edge pixels, use `--decontaminate-bg`
- if parts disappear, lower `--tolerance` and keep `--use-hsv-bg-detection` disabled
- if the background still remains, increase `--tolerance` slightly or enable `--auto-bg`
- for 96x96 output, simpler chunky buildings work best with strong silhouettes and fewer tiny details

Example commands:

```bash
python -m src.pipeline process --input input_raw --output output_sprites --size 96 --colors 32 --bg "#00ff00" --tolerance 25
python -m src.pipeline process --input input_raw --output output_sprites --size 96 --colors 32 --auto-bg --tolerance 30 --edge-cleanup
python -m src.pipeline all --input input_raw --sprites output_sprites --sheet output_sheets/buildings_sheet.png --metadata output_sheets/buildings_sheet.json --size 96 --colors 32 --cols 8 --auto-bg --edge-cleanup
```

If the art generator returns one 3x3 sheet or a screenshot of one, split it into named raw inputs first:

```bash
python -m src.pipeline split-grid --input input_raw/western_screenshot.png --output input_raw/western_buildings --rows 3 --cols 3 --names saloon,sheriff_office,general_store,doctor_office,mine_entrance,ore_storage,farmhouse,barn,animal_pen --bg "#ff00ff" --manifest-output western_buildings_manifest.json --canvas 96x96 --fit 92x92 --footprint-tiles 6x6 --category building
```

If the generated art drifts across the implied grid boundary, add `--component-split`. This detects each foreground sprite on the keyed background and assigns it to a grid slot by center point instead of blindly cutting equal rectangles:

```bash
python -m src.pipeline split-grid --input input_raw/western_screenshot.png --output input_raw/western_buildings --rows 3 --cols 3 --names saloon,sheriff_office,general_store,doctor_office,mine_entrance,ore_storage,farmhouse,barn,animal_pen --bg "#ff00ff" --manifest-output western_buildings_manifest.json --canvas 96x96 --fit 92x92 --footprint-tiles 6x6 --category building --component-split
```

Then process and build the final atlas:

```bash
python -m src.pipeline all --input input_raw/western_buildings --sprites output_sprites/western_buildings --sheet output_sheets/western_buildings_atlas.png --metadata output_sheets/western_buildings_atlas.json --manifest western_buildings_manifest.json --size 96 --colors 32 --cols 3 --auto-bg --edge-cleanup --variable-atlas
```

For mixed-size sprites, create a manifest keyed by raw file name without extension:

```json
{
  "main_hall": {
    "canvas": [96, 96],
    "fit": [92, 92],
    "anchor": [48, 88],
    "footprint_tiles": [6, 6],
    "category": "building"
  },
  "fountain": {
    "canvas": [48, 48],
    "fit": [40, 40],
    "anchor": [24, 42],
    "footprint_tiles": [2, 2],
    "category": "prop"
  },
  "notice_board": {
    "canvas": [32, 32],
    "fit": [28, 28],
    "anchor": [16, 30],
    "footprint_tiles": [1, 1],
    "category": "prop"
  }
}
```

Then run the full mixed-size pipeline:

```bash
python -m src.pipeline all --input input_raw --sprites output_sprites --sheet output_sheets/boarding_school_atlas.png --metadata output_sheets/boarding_school_atlas.json --manifest sprite_manifest.json --size 96 --colors 32 --cols 8 --auto-bg --edge-cleanup --variable-atlas
```

`canvas` controls the output PNG size for that sprite. `fit` controls how large the cropped artwork is allowed to become inside the canvas. `anchor` is the pixel point the game can use for placement, usually near the bottom center of the sprite. `footprint_tiles` records how many `16px` world tiles the object occupies.

The new folders are:
- `input_raw/`
- `output_sprites/`
- `output_sheets/`

Generated files in those folders are ignored by git.
