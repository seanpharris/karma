# Prompt: Separate Utility Shirt/Jacket Layer

## Queue

- Status: priority
- Asset type: clothing-layer
- Target output filename: `outfit_utility_shirt_jacket_layer.png`
- Reference image: `/mnt/c/Users/pharr/code/player-sprite/player_base_body_sheet.png`

## Contract

- Tool: PixelLab
- Background: transparent only
- Style: clean readable pixel art, cozy sci-fi life-sim RPG, crisp outline, no blur
- Frame layout: Transparent 512x256 PNG sprite sheet, 8 columns x 4 rows, 64x64 frames.

## Prompt

```text
Use PixelLab to create a shirt-and-light-jacket-only pixel art clothing layer for the attached humanoid base-body sprite sheet.

Use the attached base-body sheet as the exact alignment reference. Do not redraw the body. Draw only upper-body clothing pixels on transparent pixels.

Layer:
A simple cozy sci-fi utility shirt with a light work jacket. Soft blue-gray shirt, darker cropped utility jacket or vest panels, simple cuffs, small orange chest accent. Practical frontier colony-worker style, not armor, not bulky.

Output:
Transparent PNG sprite sheet. 512x256 total. 8 columns x 4 rows. Each frame is 64x64.

Columns left to right:
south, south-east, east, north-east, north, north-west, west, south-west.

Rows top to bottom:
idle, walk1, walk2, walk3.

Alignment:
The shirt/jacket must sit directly over the torso, shoulders, and arms from the attached base model in every frame. Match the same pose, scale, center point, shoulder position, arm positions, walk cycle, direction order, and row order. Sleeves should move exactly with each arm during the walk animation.

Restrictions:
Only shirt/jacket pixels. No body. No skin. No head. No hair. No face. No pants. No shoes. No tools. No weapons. No backpack. No shadow. No floor. No labels. No text. Transparent background only.
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
