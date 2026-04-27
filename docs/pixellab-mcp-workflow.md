# PixelLab MCP Workflow

PixelLab MCP can be used as an external candidate generator for Karma pixel art, while Karma keeps the runtime contract and final curation local.

## Safety / token handling

- Do **not** commit PixelLab API tokens.
- Do **not** paste tokens into Discord or docs.
- The repo tooling here is offline-only: it imports downloaded PixelLab PNG/ZIP files and never calls the PixelLab API.
- Configure PixelLab MCP in the local AI client that supports MCP, then download generated assets into a local scratch folder.

PixelLab's MCP docs describe tools such as `create_character`, `animate_character`, `get_character`, top-down tilesets, and map objects. For Karma player art, the expected external flow is:

1. Generate an 8-direction humanoid character.
2. Queue walking/idle animation.
3. Download the resulting PNG/ZIP.
4. Normalize it into Karma's player-v2 format with `tools/import_pixellab_character.py`.
5. Review the normalized sheet before copying it into active runtime art.

## Karma target contract

The current player-v2 original-art target is:

- `32x64` cells.
- `8` columns.
- `4` rows.
- Sheet size: `256x256`.
- Direction order:
  1. front/down
  2. front-right
  3. right
  4. back-right
  5. back/up
  6. back-left
  7. left
  8. front-left
- Row contract:
  1. idle/facing
  2. walk-1
  3. walk-2
  4. walk-3

The current runtime renderer still previews this in square `64x64` cells, so the importer also creates a centered runtime sheet:

- `512x256`.
- `8` columns x `4` rows.
- `64x64` runtime cells with the `32x64` body centered horizontally.

## Import command

From the repo root:

```powershell
python tools/import_pixellab_character.py path\to\pixellab-download.png --output-dir assets\art\sprites\player_v2\imported --output-stem pixellab_engineer_v1
```

For a ZIP download:

```powershell
python tools/import_pixellab_character.py path\to\pixellab-download.zip --output-dir assets\art\sprites\player_v2\imported --output-stem pixellab_engineer_v1
```

If the source has a chroma-key background:

```powershell
python tools/import_pixellab_character.py path\to\sheet.png --chroma --output-dir assets\art\sprites\player_v2\imported --output-stem pixellab_engineer_v1
```

Outputs:

- `pixellab_engineer_v1_32x64_8dir_4row.png`
- `pixellab_engineer_v1_32x64_8dir_runtime.png`

## Prompt seed for PixelLab

Use wording like this when creating the character:

```text
Original top-down low-angle pixel art survivor engineer for a cozy sci-fi life-sim RPG. Compact readable 32x64-ish humanoid proportions, no weapons, no text, no labels, transparent background, clean black/dark outline, orange work jacket, small backpack, simple boots, readable head/torso rotation. Generate 8 directions: south, south-east, east, north-east, north, north-west, west, south-west. Keep identity, outfit, scale, and silhouette consistent across directions.
```

Then animate with a simple walk/idle request:

```text
Simple readable walking loop, arms swing opposite legs, no tools or weapons, same character and outfit, consistent scale and foot baseline.
```

## Curation notes

- Treat PixelLab output as a candidate, not source of truth.
- Prefer the local `player_model_32x64_8dir_4row.png` skeleton when checking direction order and baseline.
- Reject outputs with mixed identities, wrong direction order, weapons/tools in normal walk, cropped feet/head, or inconsistent scale.
- If a candidate is good, copy the normalized `*_runtime.png` into the active player-v2 path only after visual review and smoke tests.
