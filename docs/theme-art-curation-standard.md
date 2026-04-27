# Theme Art Curation Standard

Karma should be able to support many visual themes — sci-fi frontier, western,
farm, WW2, medieval, cyberpunk, etc. — without rewriting gameplay code or
hand-mapping every image differently. This document defines how we curate art so
new themes can plug into the same catalogs, validators, and runtime conventions.

## Goals

- Keep theme art swappable by gameplay role, not by one-off file names.
- Separate raw/reference art from runtime-ready atlases.
- Make generated art auditable before it enters a code catalog.
- Preserve a consistent scale, camera angle, and interaction footprint across themes.

## Folder Model

Use `assets/art/` as the art library root.

```text
assets/art/
  reference/              # raw prompts, mood boards, labeled/generated sheets
  sprites/                # runtime character, NPC, item, weapon, tool sheets
  tilesets/               # runtime terrain/floor/wall/door/zone atlases
  props/                  # runtime furniture, pickups, interactibles, oddities
  structures/             # runtime buildings, rooms, greenhouses, modules
  ui/                     # icons, portraits, HUD art later
```

Reference art can be messy. Runtime art should be clean, transparent/chroma-free
where appropriate, and mapped only when it matches a documented contract.

## Naming Convention

Use lowercase snake-case with this shape:

```text
<theme>_<domain>_<subject>[_variant][_size].png
```

Examples:

```text
scifi_sprites_engineer_player_8dir.png
western_sprites_rancher_player_8dir.png
farm_tilesets_homestead_16px.png
ww2_props_field_radio_32px.png
western_structures_sheriff_office_atlas.png
```

Current prototype files predate the full convention, so do not rename them
casually. For new curated packs, use the convention.

## Theme IDs

Theme IDs should be short, stable, and lowercase:

- `scifi`
- `western`
- `farm`
- `ww2`
- `medieval`
- `cyberpunk`

A theme pack should fill the same gameplay roles whenever possible. Example:

| Gameplay role | Sci-fi | Western | Farm | WW2 |
| --- | --- | --- | --- | --- |
| clinic / safe hub | frontier clinic | town doctor | farmhouse clinic | field hospital |
| repair tool | multi-tool | wrench | farm tool kit | engineer kit |
| ranged weapon | electro pistol | revolver | varmint rifle | service rifle |
| oddity | alien relic | cursed idol | scarecrow charm | encrypted orders |
| currency | scrip | dollars | farm credits | ration stamps |

## Runtime Contracts

### Character Sheets

Use the existing character contract:

- `256 x 288` PNG.
- `8 columns x 9 rows`.
- `32 x 32` frames.
- Transparent background.
- Feet bottom-centered.
- Direction columns and action rows from `docs/character-art-generation-workflow.md`.

Validate with:

```bash
python3 tools/prepare_character_sheet.py validate assets/art/sprites/<sheet>.png
```

### Item / Prop / Weapon Atlases

Preferred constraints:

- transparent PNG,
- clear silhouettes,
- consistent top-down or three-quarter top-down angle,
- each object fits an intentional footprint: `16x16`, `24x24`, `32x32`, or `64x64`,
- no labels or grid lines in runtime atlases,
- no baked drop shadows unless the whole theme uses them consistently.

Generated item sheets are allowed, but they should be converted into explicit
catalog rectangles before game code depends on them.

### Tilesets

Preferred constraints:

- tile size should be documented in the filename or catalog mapping,
- no perspective that breaks grid readability,
- floor/wall/door variants should be visually distinct at game scale,
- use repeatable patterns for floor/terrain tiles.

### Structures

Preferred constraints:

- use transparent PNGs,
- keep a predictable footprint and anchor point,
- split huge presentation renders into reusable runtime pieces when possible:
  base, door, wall/cap, damaged overlay, powered/off variants, etc.

## Theme Pack Manifest

For each curated theme, keep a small manifest alongside the art or in docs. Use
this shape:

```yaml
theme: western
status: reference | runtime-ready | cataloged
source: chatgpt | hand-drawn | purchased | mixed
scale: 32px characters, 16px tiles
camera: top-down / 3-quarter top-down
palette_notes: dusty frontier, warm browns, muted reds
runtime_assets:
  characters:
    player: assets/art/sprites/western_sprites_rancher_player_8dir.png
  tilesets:
    town: assets/art/tilesets/western_tilesets_town_16px.png
  props:
    general: assets/art/props/western_props_town_32px.png
missing_roles:
  - clinic equivalent
  - faction banners
  - currency icon
notes:
  - Generated sheet had labels removed manually.
  - Needs catalog rectangles before runtime use.
```

## Prompt Template: New Theme Pack

```text
Create a cohesive 2D pixel-art runtime art pack for the game Karma.

Theme: <western / farm / WW2 / etc.>
Camera/style: top-down or slight three-quarter top-down, readable at small scale.
Visual constraints: crisp pixel art, consistent palette, no text, no labels, no guide boxes, no watermark.
Runtime constraints: transparent PNG preferred; if transparency is impossible, use flat #00ff00 chroma key.

Create assets that correspond to these gameplay roles:
- player character concept
- friendly NPC concept
- hostile/rough NPC concept
- safe hub/clinic structure
- common floor/terrain tile
- wall/fence/barrier tile
- door/gate tile
- small pickup/currency item
- repair/healing/support tool
- melee weapon
- ranged weapon
- oddity/quest object
- furniture/cover prop

Keep all objects at a consistent scale so they can be split into runtime atlases later.
Do not make a poster, mockup, UI screen, or labeled presentation sheet.
```

## Prompt Template: Runtime Atlas

```text
Create a clean runtime PNG atlas for Karma.

Theme: <theme id>.
Domain: <props / weapons / tools / tiles / structures>.
Subjects: <list of exact objects>.
Style: cohesive pixel art, top-down or slight three-quarter top-down.
Background: transparent PNG, or flat #00ff00 chroma key if transparency is impossible.
Constraints: no labels, no text, no guide boxes, no watermark, no decorative border.
Spacing: leave at least 2 px of transparent padding around each object.
Scale: small objects fit 16x16 or 32x32; structures can use larger cells but must remain reusable.
```

## Curation Checklist

Before cataloging an image in code:

- [ ] It has a clear theme ID.
- [ ] It lives in the correct domain folder.
- [ ] Filename follows the new convention, unless preserving a legacy file.
- [ ] Runtime image has no labels/guides/watermarks.
- [ ] Background is transparent or normalized from chroma key.
- [ ] Scale matches the existing prototype.
- [ ] Source/reference image is preserved separately when useful.
- [ ] Catalog rectangles are mapped intentionally.
- [ ] Smoke tests/build pass after wiring it into code.

## Audit Tool

Run this to get a quick library report:

```bash
python3 tools/audit_art_library.py
```

The audit does not replace visual review, but it catches naming, folder, PNG
size, alpha, and chroma-key issues early.
