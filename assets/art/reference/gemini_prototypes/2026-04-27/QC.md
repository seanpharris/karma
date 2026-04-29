# QC — Gemini Prototype Art References — 2026-04-27

Strict quality review after generation. These are not final production assets.

## Summary

- `karma_natural_props_ref.jpg` — **usable with cleanup**. Best sheet. Natural forms tolerate AI noise, silhouettes are readable, and several props could become prototype sprites after background removal, palette cleanup, and canvas normalization.
- `karma_static_props_ref.jpg` — **reference-only**. Strong vibe and palette, but mechanical props contain AI artifacts/melted details and inconsistent perspective. Use for art direction, not direct import.
- `karma_item_icons_ref_labeled.jpg` — **reference-only**. Good item vocabulary and readable silhouettes, but labels/gibberish text and inconsistent rendering make it a tracing/reference sheet only.
- `karma_terrain_materials_ref_labeled.jpg` — **reject for direct tile use**. Looks like terrain samples, not functional seamless game tiles/autotiles. Use only as loose material mood reference if needed.

## Quality bar

Gemini static output is a visual upgrade over simple placeholder shapes, but only natural props are close enough to clean into prototype art right now. For props/items, Gemini is useful for concepting; for terrain tiles, use hand-authored or tool-assisted tile/autotile workflows instead.

## Common issues

- JPG output rather than clean alpha PNG.
- Non-transparent backgrounds.
- Baked labels/text on item/terrain sheets.
- AI artifacts in mechanical details: melted tools, wobbly panel grids, nonsensical pipe joints.
- Inconsistent pixel scale and soft-brush artifacts.
- Perspective drift between objects.
- Terrain samples are not seamless and should not be imported as tiles.

## Recommended next steps

1. Start with `karma_natural_props_ref.jpg`: isolate 6-10 strongest props, remove background, normalize canvases/pivots, and review in-game.
2. Use `karma_static_props_ref.jpg` as a reference to regenerate or redraw cleaner individual sci-fi props.
3. Use `karma_item_icons_ref_labeled.jpg` only for item vocabulary/tracing, avoiding AI text artifacts.
4. Do not import `karma_terrain_materials_ref_labeled.jpg` as tiles; create real seamless/autotile terrain separately.
