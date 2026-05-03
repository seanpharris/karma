# Prompt: Multi-Tool — World Item + Wielded Layer

## Queue

- Status: priority
- Asset type: dual-use-item
- Target output filename: `multi-tool_world_64x64.png` and `multi-tool_wielded_layer.png`
- Reference image: `/mnt/c/Users/pharr/code/player-sprite/player_base_body_sheet.png` for wielded layer only

## Contract

- Tool: PixelLab
- Background: transparent only
- Style: clean readable pixel art, cozy sci-fi life-sim RPG, crisp outline, no blur
- World item frame layout: transparent single 64x64 PNG item sprite
- Wielded layer frame layout: transparent 512x256 PNG sprite sheet, 8 columns x 4 rows, 64x64 frames
- Direction order for wielded layer: south, south-east, east, north-east, north, north-west, west, south-west
- Row order for wielded layer: idle, walk1, walk2, walk3

## Prompt

## A) World / Inventory Item Prompt

```text
Use PixelLab to create a small sci-fi multi-tool item sprite for a cozy sci-fi life-sim RPG.

Item:
Compact handheld repair/scanner multi-tool. Dark gray body, small cyan screen or lens, tiny orange utility accent. It should read as a practical tool, not a weapon.

Output:
Transparent PNG.
Single item sprite centered in a 64x64 canvas.
Also include enough empty transparent margin so it can be used as a world pickup or inventory icon.

Style:
Clean readable pixel art, crisp outline, low top-down RPG item perspective, consistent with cozy sci-fi frontier equipment.

Restrictions:
No hand, no character body, no background, no shadow, no floor, no labels, no text, no UI frame, no weapon silhouette, no muzzle, no blade.
```

## B) Wielded / Hand Overlay Prompt

```text
Use PixelLab to create a multi-tool-only wielded equipment layer for the attached humanoid base-body sprite sheet.

Use the attached base-body sheet as the exact alignment reference. Do not redraw the body. Draw only the multi-tool pixels on transparent pixels.

Layer:
Small sci-fi handheld repair/scanner multi-tool. Dark-gray compact body, tiny cyan or orange light accent. It should read as a practical repair/scanner tool, not a gun or weapon.

Output:
Transparent PNG sprite sheet.
512x256 total.
8 columns x 4 rows.
Each frame is 64x64.

Columns left to right:
south, south-east, east, north-east, north, north-west, west, south-west.

Rows top to bottom:
idle, walk1, walk2, walk3.

Alignment:
Place the multi-tool in or near one hand from the attached base model in every frame. It should follow the hand position and arm swing during the walk cycle. Match the same pose, scale, center point, direction order, and row order.

Restrictions:
Only multi-tool pixels. No body. No skin. No clothing. No hair. No face. No backpack. No weapon shape. No muzzle, blade, projectile, or aggressive silhouette. No shadow. No floor. No labels. No text. Transparent background only.
```


## Acceptance checks

- World item is transparent 64x64 and readable as pickup/inventory art
- Wielded layer is transparent 512x256 with correct direction/row order
- Wielded layer aligns to hand positions on the base-body reference
- Contains only requested item pixels; no body/clothing/skin unless explicitly part of straps/grip overlap
- No labels/text/grid/shadow/floor/UI frame

## Import notes

- Save generated art to `/mnt/c/Users/pharr/code/player-sprite/` first unless importing directly.
- Import world item and wielded overlay separately.
- After import/review succeeds, move this prompt to `docs/art-prompts/completed/`.
