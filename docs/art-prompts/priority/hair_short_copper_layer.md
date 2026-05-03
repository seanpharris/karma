# Prompt: Short Copper Hair Layer

## Queue

- Status: priority
- Asset type: hair-layer
- Target output filename: `hair_short_copper_layer.png`
- Reference image: `/mnt/c/Users/pharr/code/player-sprite/player_base_body_sheet.png`

## Contract

- Tool: PixelLab
- Background: transparent only
- Style: clean readable pixel art, cozy sci-fi life-sim RPG, crisp outline, no blur
- Frame layout: Transparent 512x256 PNG sprite sheet, 8 columns x 4 rows, 64x64 frames.

## Prompt

```text
Use PixelLab to create a hair-only pixel art layer for the attached humanoid base-body sprite sheet.

Use the attached base-body sheet as the exact alignment reference. Do not redraw the body or head. Draw only hair pixels on transparent pixels.

Layer:
Short practical copper/red hair, slightly tousled, clean silhouette, readable from all 8 directions. Suitable for a cozy sci-fi life-sim RPG character. Hair should sit naturally on the bald head without changing the body pose.

Output:
Transparent PNG sprite sheet. 512x256 total. 8 columns x 4 rows. Each frame is 64x64.

Columns left to right:
south, south-east, east, north-east, north, north-west, west, south-west.

Rows top to bottom:
idle, walk1, walk2, walk3.

Alignment:
The hair must sit directly over the head from the attached base model in every frame. Match the same head position, pose, scale, center point, direction order, and row order. Hair should move only as needed to match the walk frames; do not introduce extra animation or drift.

Restrictions:
Only hair pixels. No body. No skin. No face. No clothing. No hats. No tools. No weapons. No shadow. No floor. No labels. No text. Transparent background only.
```

## Acceptance checks

- Transparent background
- Correct dimensions
- No labels/text/grid/shadow/floor
- Contains only the requested layer pixels
- Aligns to the base-body reference in every frame
- Preserves direction and row order

## Import notes

- Save generated art to `/mnt/c/Users/pharr/code/player-sprite/` first unless importing directly.
- After import/review succeeds, move this prompt to `docs/art-prompts/completed/`.
