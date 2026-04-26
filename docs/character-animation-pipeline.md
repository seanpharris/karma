# Character Animation Pipeline

Karma should support two practical character-art paths:

1. **Short-term runtime sheets**: clean transparent PNG frame grids that Godot can slice directly.
2. **Long-term base + skin pipeline**: a consistent blank/body rig with outfit, hair, gear, and accessory layers composited into the same runtime grid.

## Current Asset Status

- `assets/art/sprites/scifi_engineer_player_8dir.png` is the preferred active runtime player sheet when present.
  - It is a clean `256 x 288` bridge sheet exported by `tools/export_engineer_8dir.py`.
  - Its true source art is still four-direction, so diagonal/action columns are temporary approximations.
  - It will not look like a true authored eight-direction template until new diagonal frames are generated or drawn.
- `assets/art/sprites/scifi_engineer_player_sheet.png` remains the source/fallback player sheet.
  - It is mapped as a four-direction sheet: front, back, left, right.
  - The runtime code understands eight-direction animation names and falls back to cardinal directions when a sheet has no diagonal frames.
- `assets/art/ChatGPT Image Apr 26, 2026, 08_19_08 AM.png` is a useful eight-direction reference/template.
  - It is not runtime-ready because it contains labels, guide boxes, metadata, and presentation text.
  - Use it as a layout reference, not as an in-game atlas.
- `assets/art/character.png` is a concept/source atlas with many character looks and poses.
  - Use it as design reference or source material for skins.
  - Do not wire it as an eight-direction player runtime sheet unless a clean grid has been exported.

## Runtime Sheet Standard

Preferred eight-direction runtime sheet:

- Frame size: `32 x 32 px`.
- Sheet size: `256 x 288 px`.
- Columns: 8 directions.
- Rows: 9 animation/action rows.
- Background: transparent PNG.
- Pivot: bottom center / feet aligned.
- No labels, text, guide boxes, metadata, shadows, or presentation panels.

Direction columns:

```text
0 front
1 front-right
2 right
3 back-right
4 back
5 back-left
6 left
7 front-left
```

Rows:

```text
0 idle, one frame per direction
1-4 walk cycle frames 1-4
5 run pose/cycle placeholder
6 shoot pose/cycle placeholder
7 melee pose/cycle placeholder
8 interact pose/cycle placeholder
```

The code maps this with `CharacterSheetLayout.EightDirectionTemplate(origin)`.

## Recommended Next Step

Do **not** keep hand-mapping large generated presentation images. Instead:

1. Generate or draw a clean **blank/base body** eight-direction sheet using the runtime standard.
2. Generate matching **skin layers** using the exact same grid:
   - outfit/suit
   - hair/helmet
   - backpack/toolbelt
   - held weapon/tool overlays later
3. Composite layers offline into final runtime PNGs, e.g.:
   - `scifi_engineer_player_8dir.png`
   - `scifi_medic_player_8dir.png`
   - `scifi_raider_player_8dir.png`
4. Drop the first true engineer export at `assets/art/sprites/scifi_engineer_player_8dir.png`.
   The player catalog already prefers that file when it exists and falls back to
   `scifi_engineer_player_sheet.png` while it is missing. The current file at
   that path is a bridge export and can be replaced by better art without code
   changes if it preserves the runtime layout.
5. Keep source/reference images in `assets/art/` or a future `assets/art/reference/` folder.
6. Put only clean runtime atlases under `assets/art/sprites/` once they are ready to catalog.

This gives us reusable animation consistency without regenerating every NPC/player from scratch.

For the hands-on prompt + Python validation workflow, see
`docs/character-art-generation-workflow.md`.

## Generation Prompt Skeleton

```text
Create a clean 2D pixel-art top-down RPG character runtime sprite sheet for Karma.
Subject: blank neutral humanoid base body, sci-fi frontier colony proportions.
Frame size: every frame is exactly 32x32 px, nearest-neighbor pixel art.
Canvas: 8 columns x 9 rows, total 256x288 px.
Directions by column: front, front-right, right, back-right, back, back-left, left, front-left.
Rows: row 0 idle; rows 1-4 are a four-frame walk cycle; row 5 run; row 6 shoot; row 7 melee; row 8 interact.
Background: transparent PNG, or perfectly flat #00ff00 chroma key if transparency is impossible.
Alignment: feet bottom-centered in every frame, consistent body proportions, no camera angle changes.
Constraints: runtime grid only, no labels, no text, no guide boxes, no metadata, no watermark, no shadows baked into background.
```

For skins, replace the subject with the layer while preserving the exact grid/alignment:

```text
Subject: engineer jumpsuit/clothing layer only for the same blank humanoid base, transparent where body is visible.
```
