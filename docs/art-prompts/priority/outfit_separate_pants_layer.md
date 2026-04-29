# Prompt: Separate Utility Pants Layer

## Queue

- Status: priority
- Asset type: clothing-layer
- Target output filename: `outfit_utility_pants_layer.png`
- Reference image: `/mnt/c/Users/pharr/code/player-sprite/player_base_body_sheet.png`

## Contract

- Tool: PixelLab
- Background: transparent only
- Style: clean readable pixel art, cozy sci-fi life-sim RPG, crisp outline, no blur
- Frame layout: Transparent 512x256 PNG sprite sheet, 8 columns x 4 rows, 64x64 frames.

## Prompt

```text
Use PixelLab to create a pants-only pixel art clothing layer for the attached humanoid base-body sprite sheet.

Use the attached base-body sheet as the exact alignment reference. Do not redraw the body. Draw only pants pixels on transparent pixels.

Layer:
Simple sci-fi utility work pants. Muted slate-blue or charcoal fabric, small knee patches, subtle seam lines, tiny orange utility accent near one pocket. Practical colony-worker style, readable but not bulky.

Output:
Transparent PNG sprite sheet. 512x256 total. 8 columns x 4 rows. Each frame is 64x64.

Columns left to right:
south, south-east, east, north-east, north, north-west, west, south-west.

Rows top to bottom:
idle, walk1, walk2, walk3.

Alignment:
The pants must sit directly over the legs and hips from the attached base model in every frame. Match the same pose, scale, center point, hip position, leg positions, walk cycle, foot baseline, direction order, and row order. Pant legs should move exactly with each leg during the walk animation.

Restrictions:
Only pants pixels. No body. No skin. No torso shirt. No shoes or boots. No hair. No face. No tools. No weapons. No backpack. No shadow. No floor. No labels. No text. Transparent background only.
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
