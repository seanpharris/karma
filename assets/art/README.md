# Karma Art Curation README

This folder is the staging area for all Karma art: generated, hand-drawn,
purchased, edited, and runtime-ready. The goal is to build a reusable art
library that can support many themes — sci-fi frontier, western, farm, WW2,
medieval, cyberpunk, etc. — without turning the project into a pile of one-off
images.

## Curation Philosophy

Curate art by **gameplay role** and **runtime contract**, not just by vibe.

Good curated art answers these questions:

1. What theme is it for? `scifi`, `western`, `farm`, `ww2`, etc.
2. What domain is it? character, item, prop, tile, structure, UI, reference.
3. What gameplay role does it serve? player, medic NPC, clinic, wall, weapon,
   repair tool, currency, quest oddity, cover prop, etc.
4. Is it reference art or runtime art?
5. Does it match the scale, transparency, grid, and naming conventions our code
   expects?

Reference art can be messy. Runtime art should be boringly predictable.

## Folder Layout

Use these folders consistently:

- `reference/` — raw prompts, mood boards, presentation sheets, labeled sheets,
  screenshots, generated concepts, and anything useful but not runtime-ready.
- `sprites/` — runtime player, NPC, outfit, armor, weapon, item, and animation
  sheets.
- `tilesets/` — terrain, floors, walls, doors, roads, field rows, trenches,
  station panels, and other grid/tile art.
- `props/` — furniture, cover, pickups, terminals, barrels, crates, oddities,
  decorations, and interactibles.
- `structures/` — large buildings and reusable structure atlases: clinics,
  farmhouses, barns, bunkers, domes, greenhouses, shops, etc.
- `ui/` — icons, portraits, HUD art, badges, cards, and menus later.

If a folder does not exist yet, create it when adding art for that domain.

## Naming Convention

New curated theme assets should use lowercase snake-case:

```text
<theme>_<domain>_<subject>[_variant][_size].png
```

Examples:

```text
western_props_field_radio_32px.png
farm_tilesets_homestead_16px.png
ww2_sprites_medic_player_8dir.png
scifi_structures_greenhouse_atlas.png
western_sprites_rancher_player_8dir.png
```

Current prototype files may keep legacy names until we intentionally migrate
them. Do not casually rename wired assets without updating catalogs/tests.

## Reference vs Runtime

### Reference art

Put it in `reference/` when it has any of these:

- labels or text,
- grid guides,
- prompt metadata,
- presentation panels,
- decorative backgrounds,
- inconsistent scale,
- mixed objects that have not been sliced,
- useful style inspiration but unclear runtime rectangles.

### Runtime art

Put it in `sprites/`, `tilesets/`, `props/`, `structures/`, or `ui/` only when:

- it has no labels, guides, watermarks, or prompt text,
- it has transparent background or a documented chroma source,
- scale and camera angle match the rest of the game,
- objects/frames can be mapped cleanly in code,
- it has a stable filename that describes theme/domain/subject.

## Character Runtime Contract

Preferred character sheets use the standard 8-direction format:

- PNG with RGBA transparency.
- `256 x 288 px` total.
- `8 columns x 9 rows`.
- `32 x 32 px` frames.
- Feet bottom-centered in every frame.
- No labels, text, guide boxes, shadows baked into the background, or borders.

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
0 idle
1 walk frame 1
2 walk frame 2
3 walk frame 3
4 walk frame 4
5 run/action-ready
6 tool/use
7 melee/impact
8 interact/reach
```

Validate character sheets with:

```bash
python3 tools/prepare_character_sheet.py validate assets/art/sprites/<sheet>.png
```

If the generator gives a green-screen sheet:

```bash
python3 tools/prepare_character_sheet.py normalize input.png assets/art/sprites/<sheet>.png --chroma
```

## Theme Pack Checklist

When adding a new theme, try to fill the same gameplay roles across themes:

- player character,
- friendly NPC,
- rough/hostile NPC,
- safe hub / clinic equivalent,
- common floor/terrain tile,
- wall/fence/barrier tile,
- door/gate tile,
- small pickup/currency,
- repair/healing/support tool,
- melee weapon,
- ranged weapon,
- quest oddity,
- furniture/cover prop,
- one large structure.

Example role mapping:

| Role | Sci-fi | Western | Farm | WW2 |
| --- | --- | --- | --- | --- |
| safe hub | frontier clinic | town doctor | farmhouse clinic | field hospital |
| repair tool | multi-tool | wrench | farm toolkit | engineer kit |
| ranged weapon | electro pistol | revolver | varmint rifle | service rifle |
| currency | scrip | dollars | farm credits | ration stamps |
| oddity | alien relic | cursed idol | scarecrow charm | encrypted orders |

## Prompting Workflow

For new art, start broad, then make runtime atlases.

1. Generate a cohesive theme/reference sheet.
2. Move messy/reference outputs into `reference/`.
3. Generate or crop clean runtime sheets/atlases from that reference.
4. Normalize transparency/chroma.
5. Validate with the tools below.
6. Wire only clean runtime assets into code catalogs.

Useful docs:

- [TASKS.md#theme-art-curation-standard](../../TASKS.md#theme-art-curation-standard) — broad multi-theme standard and prompts.
- [TASKS.md#character-art-generation-workflow](../../TASKS.md#character-art-generation-workflow) — character prompt and validator workflow.
- [TASKS.md#prototype-model-art-prompts](../../TASKS.md#prototype-model-art-prompts) — paste-ready prompts for current prototype NPC, item, weapon, tool, and station models.
- [TASKS.md#character-animation-pipeline](../../TASKS.md#character-animation-pipeline) — base-body plus skin/outfit layering plan.

## Audit Commands

Run a quick library audit:

```bash
python3 tools/audit_art_library.py
```

Validate a character sheet:

```bash
python3 tools/prepare_character_sheet.py validate assets/art/sprites/scifi_engineer_player_8dir.png
```

The audit may warn about current legacy prototype assets. That is fine for now;
warnings become a cleanup list, not an automatic blocker.

## Current Wired Prototype Assets

These are already referenced by code or tests, so handle with care:

- `tilesets/scifi_station_atlas.png`
- `character.png`
- `sprites/scifi_engineer_player_8dir.png`
- `sprites/scifi_engineer_player_sheet.png`
- `sprites/scifi_engineer_player_sheet_chroma.png`
- `sprites/scifi_item_atlas.png`
- `sprites/scifi_utility_item_atlas.png`
- `sprites/scifi_weapon_atlas.png`
- `sprites/scifi_tool_atlas.png`
- `structures/scifi_greenhouse_atlas.png`

`sprites/scifi_engineer_player_8dir.png` is the preferred local-player runtime
sheet. The current version is a bridge export from `tools/export_engineer_8dir.py`;
it proves the runtime layout but does not contain true authored diagonal poses.
Replace it with true 8-direction art when ready, keeping the same `256 x 288`,
`8 x 9`, `32 x 32` runtime contract.

## Code Mapping Notes

- Catalog rendering is opt-in: only set atlas regions after exact rectangles are
  known.
- Keep placeholder/procedural fallbacks for unmapped future ids so the prototype
  remains readable.
- `ArtAssetManifest` discovers mapped atlas paths from catalogs; if a catalog
  references a file, smoke tests expect it to exist.
- New sprite/prop/structure mappings should use the shared atlas helpers before
  introducing new rendering code.
