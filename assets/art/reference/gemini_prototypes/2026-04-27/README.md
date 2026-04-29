# Gemini Prototype Art References — 2026-04-27

AI-generated static art concept/reference sheets for replacing Karma placeholder prototype objects.

These are **not production-ready assets** yet. Use them as visual direction and as source material for cleanup/import passes.

## Files

- `karma_static_props_ref.jpg`
  - Best sci-fi colony prop reference sheet.
  - Good candidates for cleanup: cargo crate, repair kit case, utility junction box, hydroponics planter, oxygen rack, kiosk/terminal, airlock door, wall segment, solar panel, pipe cluster, landing beacon, lamp post, maintenance hatch, medical supply box, portable terminal, power cell.
- `karma_natural_props_ref.jpg`
  - Best natural prop reference sheet and closest to importable after cleanup.
  - Good candidates: dry grass, moss/rocks, mushrooms, mineral rocks, crystal shard, branch/log, dead bush, stone, succulent/cactus, glowing lichen, berry bush, sapling.
- `karma_item_icons_ref_labeled.jpg`
  - Inventory/world item vocabulary reference only.
  - Has baked labels/text; do not import directly.
- `karma_terrain_materials_ref_labeled.jpg`
  - Terrain/material direction only.
  - Has baked labels/text and is not seamless/autotile-ready.

## Known issues

- Gemini returned JPGs, not alpha PNGs.
- Backgrounds are not transparent.
- Some sheets contain baked labels/text.
- Scale, perspective, and pixel density vary.
- Terrain samples are not guaranteed seamless.
- Cleanup needed before import: slice, remove background, normalize canvas/pivots, repaint noisy edges, and build real atlases.

## Recommended next pass

1. Cleanup/import natural props first.
2. Cleanup/import static sci-fi props second.
3. Use item icons as reference for generating/painting clean 64x64 world/inventory sprites.
4. Treat terrain sheet as material reference, then author proper tiles/autotiles separately.
