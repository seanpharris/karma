# PixelLab Buildings Queue — Medieval Theme

Ready-to-run PixelLab MCP commands for the medieval village buildings
atlas. **Blocked on credit balance** — `pixellab-mcp get_balance`
returned `$0 USD` last time we checked. Top up at
[pixellab.ai](https://pixellab.ai), then run these.

## Sizing + style contract

- **View**: `low top-down` (matches the LPC characters and the Cainos
  tiles we use for ground).
- **Output size**: 128×128 per building for one-tile huts, 256×256 for
  larger buildings (chapel, gatehouse, mill). PixelLab's `width` /
  `height` parameters take pixel values; `64` / `128` / `256` are
  recommended.
- **No background**: pass `no_background=true` so the building drops on
  the existing ground tiles without a colour swatch.
- **Style** locked to: "low-top-down medieval village pixel art,
  thatched roofs where appropriate, stone walls, wooden shutters,
  cobblestone foundations".
- **Outline**: `selective outline` (most LPC art uses selective).
- **Shading**: `medium shading` to match Cainos.
- **Detail**: `medium detail`.

## Saved-to paths

Buildings land in
`assets/art/structures/medieval/<id>.png`. Create the folder before
running the prompts:

```bash
mkdir -p assets/art/structures/medieval
```

Each prompt's `save_to_file` should be the absolute path, e.g.:

```
"save_to_file": "C:/Users/pharr/code/karma/assets/art/structures/medieval/smithy.png"
```

## The roster (priority-ordered)

The 13 location archetypes from
[`assets/themes/medieval/theme.json`](../assets/themes/medieval/theme.json),
plus a few generic dressing pieces.

### Tier 1 — core buildings (commission first)

```jsonc
{
  "tool": "generate_image_pixflux",
  "arguments": {
    "description": "low top-down view medieval village blacksmith forge, stone-walled, slate roof, wooden shutters open, anvil + bellows visible at front, smoke from chimney",
    "negative_description": "modern, sci-fi, blurry, photo, text",
    "width": 128, "height": 128,
    "no_background": true,
    "outline": "selective outline",
    "shading": "medium shading",
    "detail": "medium detail",
    "save_to_file": "C:/Users/pharr/code/karma/assets/art/structures/medieval/smithy.png"
  }
}
```

```jsonc
// tavern — The Cracked Tankard
{
  "description": "low top-down view medieval village tavern, two-story, thatched roof, wooden front door with a hanging tankard sign, warm window glow",
  "save_to_file": "...tavern.png"
}
```

```jsonc
// chapel — Chapel of the Pale Star
{
  "description": "low top-down view stone chapel, small bell tower, narrow stained-glass window over the entrance, stone steps, slate roof",
  "width": 256, "height": 256,
  "save_to_file": "...chapel.png"
}
```

```jsonc
// market stall (single)
{
  "description": "low top-down view medieval market stall, striped canvas awning red and cream, wooden counter, baskets of produce, cobblestone footing",
  "width": 96, "height": 96,
  "save_to_file": "...market_stall.png"
}
```

```jsonc
// gatehouse / barracks
{
  "description": "low top-down view medieval gatehouse with a portcullis, two stone towers flanking, banners on the towers, drawbridge approach",
  "width": 256, "height": 256,
  "save_to_file": "...gatehouse.png"
}
```

```jsonc
// almshouse
{
  "description": "low top-down view modest stone almshouse, single story, thatched roof, wooden door with a charity bowl beside it",
  "save_to_file": "...almshouse.png"
}
```

```jsonc
// mill — Greywater Mill
{
  "description": "low top-down view medieval water mill, stone base, thatched roof, large wooden water wheel on one side, mill stream running along the building",
  "width": 256, "height": 256,
  "save_to_file": "...mill.png"
}
```

```jsonc
// stables
{
  "description": "low top-down view wooden stables, multiple stall doors visible, thatched roof, hay bales and a wooden fence at the entrance",
  "save_to_file": "...stables.png"
}
```

### Tier 2 — secondary buildings

```jsonc
// mason yard
{
  "description": "low top-down view stonecutter's yard, stacks of cut stone, partially-finished column, wooden cart with chisels, low workshop hut at back",
  "save_to_file": "...mason_yard.png"
}
```

```jsonc
// duel ring / skirmish pit
{
  "description": "low top-down view circular fighting pit dug into the earth, wooden palisade fence around it, rough-cut benches, training dummies at one end",
  "save_to_file": "...skirmish_pit.png"
}
```

```jsonc
// graveyard
{
  "description": "low top-down view small medieval graveyard, weathered stone gravemarkers in rows, wrought-iron gate, single bare tree, tufts of long grass",
  "save_to_file": "...graveyard.png"
}
```

```jsonc
// hidden den / shadowed guild front
{
  "description": "low top-down view of a narrow alley building with a heavy iron-bound wooden door, small barred window, no signage, half-buried into a stone wall",
  "save_to_file": "...shadow_den.png"
}
```

```jsonc
// hermitage
{
  "description": "low top-down view of a hermit's hut at a wood's edge, daub-and-wattle walls, thatched roof, single shuttered window, smoke from a small stone chimney, herb garden plot beside it",
  "save_to_file": "...hermitage.png"
}
```

### Tier 3 — dressing / props (low priority)

```jsonc
// well
{
  "description": "low top-down view medieval village well, circular stone wellhead, wooden roof on four posts, bucket on a chain, cobblestone surround",
  "width": 96, "height": 96,
  "save_to_file": "...well.png"
}
```

```jsonc
// village sign
{
  "description": "low top-down view medieval wooden village sign post, two cross-arms with carved place names, planted in a small stone cairn",
  "width": 64, "height": 64,
  "save_to_file": "...village_sign.png"
}
```

```jsonc
// trough
{
  "description": "low top-down view rectangular wooden water trough, stone foundation, slight overflow puddle beside it",
  "width": 96, "height": 64,
  "save_to_file": "...trough.png"
}
```

```jsonc
// haystacks (3 sizes)
{
  "description": "low top-down view three medieval hay stacks of varying size on a packed-earth floor",
  "width": 128, "height": 96,
  "save_to_file": "...haystacks.png"
}
```

## After buildings land

Once the PNGs are on disk, the wiring tasks are:

1. **Add a `MedievalBuildings` atlas registry entry.** Sibling to the
   tile registry at
   [`scripts/World/ThemeArtRegistry.cs`](../scripts/World/ThemeArtRegistry.cs).
   Probably easiest to add a separate `BuildingArtRegistry` keyed by a
   `BuildingId` enum (Smithy, Tavern, Chapel, Market, …).
2. **Wire the world generator** — locations from the
   `location_archetypes` array in
   [`assets/themes/medieval/theme.json`](../assets/themes/medieval/theme.json)
   pick a building id; the renderer looks it up in the registry.
3. **Smoke test** — assert that for each `location_archetypes` entry
   in `theme.json`, the registry returns a populated atlas region.

## If we want to skip PixelLab

Alternative: composite Mixel `Topdown RPG 32x32 - Ruins.PNG` slices
plus Cainos `TX Struct.png` into a hand-built atlas. Lower fidelity
but no API spend.
