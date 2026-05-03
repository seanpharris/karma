# `assets/` — Karma Runtime Assets

Single source of art, audio, and theme data the running game loads at
runtime via `res://assets/...`. Everything not under this tree is either
source for build pipelines (`tools/`), tests, or scripts.

If you're adding files, consult the matching subsection below for the
expected shape + path. Code that consumes assets via `Image.Load`,
`ResourceLoader`, or `FileAccess.FileExists` should match the layout
documented here.

```
assets/
├── art/
│   ├── generated/
│   │   ├── gemini_natural_props_2026_04_27/
│   │   ├── gemini_static_props_2026_04_27/
│   │   ├── priority_static_atlases/
│   │   ├── sliced/
│   │   └── static_event_atlases/
│   ├── sprites/
│   │   ├── docs/                       (LPC upstream README + LICENSE + CREDITS)
│   │   ├── palette_definitions/        (LPC palette JSONs — gitignored)
│   │   ├── sheet_definitions/          (LPC layer JSONs — gitignored)
│   │   ├── spritesheets/               (LPC raw source PNGs — 1.2 GB, gitignored)
│   │   ├── themes/
│   │   │   └── medieval/               (composed LPC bundles for medieval NPCs)
│   │   └── README.md
│   ├── themes/
│   │   └── medieval/
│   │       ├── theme.json              (canonical theme data + asset manifest)
│   │       ├── banners/                (faction banners)
│   │       ├── buildings/              (smithy, tavern, chapel, etc.)
│   │       ├── decals/                 (blood, footprints, hay, scorch)
│   │       ├── environment/            (trees, hay, campfire, fence, stump)
│   │       ├── hud_chrome/             (parchment panel, button, scroll, divider)
│   │       ├── items/                  (per-item-id 32×32 inventory icons)
│   │       ├── map_icons/              (markers + currency + vital icons)
│   │       ├── mounts/                 (horse, donkey, ox cart, mule)
│   │       ├── npc_portraits/          (head crops from LPC bundles)
│   │       ├── quest_glyphs/           (quest type icons)
│   │       ├── status_icons/           (Hungry, Burning, Wraith, etc.)
│   │       ├── structures/             (chest, barrel, anvil, gate, etc.)
│   │       ├── tiles/                  (ground tile variants)
│   │       └── boarding_school|space|western/   ⚠ misplaced; should sit at art/themes/<name>/
│   └── third_party/
│       ├── cainos_pixel_art_top_down_basic_v1_2_3/
│       ├── kenney_fantasy_ui_borders/
│       ├── mixel_top_down_rpg_32x32_v1_7/
│       └── nature_free_noncommercial/
└── audio/
    ├── music/
    │   └── themes/
    │       └── medieval/               (themed music tracks: lute, choir, drum)
    └── sfx/                            (flat list of cue WAVs)
```

## Subsystems

### `assets/art/themes/<theme>/` — themed game art

The runtime is keyed off **theme id** (`medieval` is the default; see
`scripts/World/ThemeRegistry.cs` and `WorldConfig.CreatePrototype`).
Each theme owns the same category subfolders so the runtime can swap
between themes per-match without changing render code.

Categories that resolve through `scripts/Art/ThemedArtRegistry.cs`:

| Folder | Used by | Sizing |
|---|---|---|
| `banners/` | faction reputation HUD | 64×96 |
| `buildings/` | `WorldRoot.RenderServerStructures` (server stations) | 64–144 px |
| `decals/` | combat events, paths | 32×32 / 48×48 |
| `environment/` | world decoration scatter | 32–96 px |
| `hud_chrome/` | dialogue / Escape menu / quest log panels | varies |
| `items/` | inventory / vendor / hotbar (`ItemArtRegistry`) | 32×32 |
| `map_icons/` | minimap markers, currency, karma + vital glyphs | 32×32 |
| `mounts/` | mount entity render | 64×64 (ox_cart 96×64) |
| `npc_portraits/` | dialogue panel + tooltip avatar | 32×32 |
| `quest_glyphs/` | quest log row prefix | 32×32 |
| `status_icons/` | HUD status strip (`status_<id>` matches `PlayerStatus`) | 32×32 |
| `structures/` | secondary structure props (chests, barrels, signs) | 64×64 |
| `tiles/` | ground tile variants | 32×32 |

**Variants:** files named `<kind>.png`, `<kind>_a.png`, `<kind>_b.png`,
… are treated as variants of the same `kind`. Runtime picks via
deterministic `(worldId, entityId)` hash so the same world always picks
the same variant. See `ThemedArtRegistry.GetVariant` and the
`variant_index` block in `theme.json`.

**Manifest:** `theme.json → assets.categories[<name>].files[]` is the
authoritative list. Re-run `tools/update_medieval_theme_assets.js`
after adding files to refresh it.

### `assets/art/sprites/` — LPC sprite generator stash

Raw upstream content from
[Universal-LPC-Spritesheet-Character-Generator](https://github.com/liberatedpixelcup/Universal-LPC-Spritesheet-Character-Generator).
Used as input to the C# / GDScript composers in `tools/`.

| Subdir | Status | Purpose |
|---|---|---|
| `spritesheets/` | **gitignored** (1.2 GB) | Raw character/item layer PNGs. Composer reads, never written by runtime. |
| `sheet_definitions/` | gitignored | LPC JSON describing each sheet's layer + animation. |
| `palette_definitions/` | gitignored | Palette swap JSONs. |
| `themes/medieval/` | **committed** | Per-NPC bundle JSONs the composer materialises from. |
| `docs/` | committed | LPC upstream README, LICENSE, CREDITS.csv. |
| `README.md` | committed | LPC layout + license obligations. |

`spritesheets/`, `sheet_definitions/`, and `palette_definitions/` are
**not vendored** because of their size. To work locally, clone the
upstream LPC repo into those paths or generate them from upstream. The
runtime never reads from `spritesheets/` directly — only the composer
tools touch those.

### `assets/art/generated/` — composer outputs

| Subdir | Source | Purpose |
|---|---|---|
| `gemini_natural_props_2026_04_27/` | Gemini API | Original natural-prop PNGs. |
| `gemini_static_props_2026_04_27/` | Gemini API | Original static-prop PNGs. |
| `priority_static_atlases/` | manual | High-priority sci-fi atlases (clinic, location exteriors, etc.). |
| `sliced/` | `tools/slice_*` | Atlas slices keyed by event/zone (clinic_rescue_revive, containers_loot, …). |
| `static_event_atlases/` | manual | Per-event sci-fi atlases (modular_walls_doors, evidence_clues, etc.). |

These belong to the original sci-fi prototype assets. The medieval
theme set under `assets/art/themes/medieval/` is the active visual.

### `assets/art/third_party/` — vendored asset packs

Each subdir has a `README_KARMA_IMPORT.md` (or similar) describing
license + import notes.

| Pack | Purpose |
|---|---|
| `cainos_pixel_art_top_down_basic_v1_2_3/` | Tile/structure base — CC0 |
| `kenney_fantasy_ui_borders/` | UI frame textures — CC0 |
| `mixel_top_down_rpg_32x32_v1_7/` | RPG tilesets — license per pack |
| `nature_free_noncommercial/` | Nature props — non-commercial only (don't ship in paid build) |

### `assets/audio/`

| Subdir | Format | Notes |
|---|---|---|
| `music/themes/<theme>/` | mp3 (preferred) / wav | Themed music. `main_menu_theme_placeholder.wav` is the boot stub. Cue ids resolve through `scripts/Audio/PrototypeMusicPlayer.cs`. |
| `sfx/` | wav | Flat list keyed off `AudioEventCatalog`. File stem matches event id (e.g. `door_open.wav` for the `door_opened` event). |

## Adding new assets

### Themed art (medieval, future themes)

1. Drop the PNG under `assets/art/themes/<theme>/<category>/<id>.png`.
2. For variants, suffix with `_a`, `_b`, etc.
3. Run `node tools/update_medieval_theme_assets.js` to refresh
   `theme.json → assets`.
4. Runtime picks it up automatically via `ThemedArtRegistry`. No code
   change needed unless you're introducing a new category.

### Themed audio

1. Drop the file under `assets/audio/music/themes/<theme>/<id>.mp3` or
   `assets/audio/sfx/<event_id>.wav`.
2. For SFX: file stem must match the `AudioEventCatalog` event id.
3. For music: register the path in `PrototypeMusicPlayer` if it's a new
   slot.

### Third-party packs

Add a sibling subdir under `assets/art/third_party/`. Required:
- A `README_KARMA_IMPORT.md` documenting license, source URL, version,
  and the path inside the pack that's actively used.
- A line in `THIRD_PARTY_NOTICES.md` (root) crediting the pack.
- For non-CC0 licenses: also a `CREDITS.md` snippet listing each
  individual file's license + author.

## Known cleanups

- `assets/art/themes/medieval/{boarding_school,space,western}/` —
  these are atlas data for *other* themes that landed inside `medieval/`
  during the reorg. They should move to `assets/art/themes/<theme>/`
  (siblings of `medieval/`).
- `assets/art/generated/sliced/` and `priority_static_atlases/` /
  `static_event_atlases/` — sci-fi prototype atlases. Keep as
  reference/fallback for now; will be retired once the medieval set
  covers all rendering surfaces.
- `assets/art/character.png` (referenced by
  `scripts/Art/PrototypeSpriteModels.cs`) was removed in the reorg —
  callers should swap to a per-theme equivalent or be removed.
- `assets/art/generated/lpc_npcs/` (referenced by
  `LpcPlayerAppearanceRegistry`, `PrototypeCharacterSprite`, several
  composer tools) was removed in the reorg — composed NPC bundles now
  live under `assets/art/themes/medieval/npc_portraits/`. Tools that
  emit to `lpc_npcs/` need to redirect.
- `assets/art/reference/` (referenced by some `tools/extract_*` and
  `tools/cleanup_*` scripts) was removed — those tools target old
  Gemini reference jpgs that no longer exist.
