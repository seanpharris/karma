# Prompt: Small Handheld Multi-Tool Layer

## Queue

- Status: priority
- Asset type: equipment-layer
- Target output filename: `tool_multitool_hand_layer.png`
- Reference image: `/mnt/c/Users/pharr/code/player-sprite/player_base_body_sheet.png`

## Contract

- Tool: PixelLab
- Background: transparent only
- Style: clean readable pixel art, cozy sci-fi life-sim RPG, crisp outline, no blur
- Frame layout: transparent 512x256 PNG sprite sheet, 8 columns x 4 rows, 64x64 frames
- Direction order: south, south-east, east, north-east, north, north-west, west, south-west
- Row order: idle, walk1, walk2, walk3

## Prompt

```text
Use PixelLab to create a small handheld multi-tool-only pixel art equipment layer for the attached humanoid base-body sprite sheet.

Use the attached base-body sheet as the exact alignment reference. Do not redraw the body. Draw only the multi-tool pixels on transparent pixels.

Layer:
Small sci-fi handheld multi-tool. Compact dark-gray tool with a tiny cyan or orange light accent. It should read as a practical repair/scanner tool, not a gun or weapon.

Output:
Transparent PNG sprite sheet. 512x256 total. 8 columns x 4 rows. Each frame is 64x64.

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

- Transparent 512x256 sheet
- Correct direction and row order
- Aligns to hand positions on the base-body reference in every frame
- Contains only multi-tool pixels
- No labels/text/grid/shadow/floor

## Import notes

- Save generated art to `/mnt/c/Users/pharr/code/player-sprite/` first unless importing directly.
- This hand-layer-only prompt overlaps with `item_multitool_dual_use.md`; keep whichever output is cleaner.
- After import/review succeeds, move this prompt to `docs/art-prompts/completed/`.
