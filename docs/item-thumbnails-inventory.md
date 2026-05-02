# Item Thumbnails / Icons Inventory

User-flagged 2026-05-02: vendors / inventory / loot caches / ground drops
should all surface a thumbnail per item. Audit done same day —
**most of the art already exists**; the gap is wiring + a few missing
enum entries + future medieval re-skin.

## Current state

**Items.** 42 items in [`StarterItems`](../scripts/Data/StarterItems.cs)
across 9 `ItemCategory` values (Weapon ×11, Tool ×12, Armor ×2,
Cosmetic ×3, Oddity ×3, Consumable ×3, Ammo ×2, InteractibleObject ×3,
Misc ×3 — some items appear in multiple buckets).

**Sprites already on disk.**
- `assets/art/sprites/scifi_item_atlas.png`
- `assets/art/sprites/scifi_utility_item_atlas.png`
- `assets/art/sprites/scifi_weapon_atlas.png`
- `assets/art/sprites/scifi_tool_atlas.png`
- A handful of polished one-off PNGs under
  `assets/art/sprites/generated/gemini_static_props_2026_04_27/polished/`
  (`power_cell_canister.png`, `portable_terminal.png`,
  `repair_kit_case.png`).

**Registry.**
[`PrototypeSpriteCatalog`](../scripts/Art/PrototypeSpriteModels.cs)
already maps 36/42 items to atlas regions via `GetKindForItem(itemId)`
→ `PrototypeSpriteKind` enum → `PrototypeSpriteDefinition` with full
`AtlasPath` + `SourceRegion` coordinates.

**What renders today:**
- **World drops** (ground tile sprites) — already render the catalog
  art via `PrototypeAtlasSprite` in
  [`WorldRoot.RenderServerItems`](../scripts/World/WorldRoot.cs).
- **Vendor rows** — text-only buttons (price + name).
- **Inventory rows** — text-only `InventoryDragRow` buttons.
- **Hotbar** — text + key digit, no icon.
- **Loot cache / supply-drop** — same as vendor (text-only).

## Gap

1. **Six items have no enum entry / no atlas region:**
   `BackpackBrown`, `BallisticRound`, `EnergyCell`, `StimSpike`,
   `DownerHaze`, `TremorTab`. They render a placeholder square.
2. **Vendor + inventory + hotbar** show text only; need a 32×32
   `TextureRect` child built from the catalog atlas region.
3. **Medieval re-skin** — the existing atlases are sci-fi flavored.
   For the medieval theme we eventually want medieval-flavored
   thumbnails (sword for "stun_baton", arrow for "ballistic_round",
   torch for "flashlight", etc.) keyed by the same item ids so the
   catalog lookup still works.

## Plan (two tasks)

The wiring + missing-enum work is a small agent-1 task and can land
immediately on the existing sci-fi atlases. The medieval re-skin is a
larger curation/generation task in the same shape as
[`docs/medieval-audio-inventory.md`](medieval-audio-inventory.md).

### Wiring (small)

- Add the 6 missing entries to `PrototypeSpriteKind` + sprite
  definitions for them. Use placeholder atlas regions until the art
  agent generates real cells.
- Refactor vendor row / inventory row / hotbar slot render in
  [`HudController.cs`](../scripts/UI/HudController.cs) to add a
  `TextureRect` child showing the atlas region from
  `PrototypeSpriteCatalog.Get(kind)`.

### Medieval re-skin (large — separate task)

- Decide source: AI generation (PixelLab when balance returns) vs
  asset-pack (LPC `weapon/`, `head/`, `accessory/` sheets — dozens of
  free items already), vs OpenGameArt CC0 icon packs.
- For each of the 42 item ids, pick a medieval visual.
- Output: `assets/art/themes/medieval/items/<item_id>.png` (32×32
  each), wired into a per-theme `ItemArtRegistry` keyed by
  `(themeId, itemId)`.
- Add `ItemArtRegistry.Get(themeId, itemId)` that falls back to the
  existing sci-fi catalog when the theme doesn't have a re-skin —
  same module+registry shape as `ThemeArtRegistry`.

## Sources for medieval re-skin

- **LPC weapon/armor/accessory sheets** — already vendored under
  `assets/art/sprites/lpc/`. Hundreds of medieval weapons (swords,
  bows, staves, daggers), shields, hats, pouches. Per-frame; need a
  GDScript composer like `lpc_compose_theme.gd` to crop a single
  inventory cell out of each sheet.
- **OpenGameArt** — search "icons CC0 RPG" for full RPG icon packs.
  Game-Icons.net (CC-BY 3.0) has 4000+ vector icons, also useful.
- **Pixabay** — secondary source; less likely to have game-shaped
  pixel art.
- **PixelLab** — best fidelity match to the existing player sprites
  but blocked on $0 balance (see `docs/pixellab-medieval-buildings-queue.md`).

## License rules (same as audio)

- CC0, CC-BY, OGA-BY, Pixabay Content License, MIT-style audio = OK.
- CC-BY-NC, CC-BY-ND, "personal use only" = NOT OK.
- Every shipped icon goes into `assets/art/CREDITS.md` (new) with
  source URL + author + license id.
