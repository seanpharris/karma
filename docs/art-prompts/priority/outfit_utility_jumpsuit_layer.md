# Prompt: Utility Jumpsuit Layer

## Queue

- Status: priority
- Asset type: clothing-layer
- Target output filename: `outfit_utility_jumpsuit_layer.png`
- Reference image: `/mnt/c/Users/pharr/code/player-sprite/player_base_body_sheet.png`

## Contract

- Tool: PixelLab
- Background: transparent only
- Style: clean readable pixel art, cozy sci-fi life-sim RPG, crisp outline, no blur
- Frame layout: Transparent 512x256 PNG sprite sheet, 8 columns x 4 rows, 64x64 frames.

## Prompt

```text
Use PixelLab to create a clothing-only pixel art layer for the attached humanoid base-body sprite sheet.

Use the attached base-body sheet as the exact alignment reference. Do not redraw the body. Draw only the clothing layer on transparent pixels.

Layer:
A simple cozy sci-fi utility jumpsuit / mechanic outfit. Soft blue-gray fabric, darker belt, small knee pads, simple cuffs, subtle orange utility accents. Practical colony-worker style, readable but not bulky.

Output:
Transparent PNG sprite sheet. 512x256 total. 8 columns x 4 rows. Each frame is 64x64.

Columns left to right:
south, south-east, east, north-east, north, north-west, west, south-west.

Rows top to bottom:
idle, walk1, walk2, walk3.

Alignment:
The jumpsuit must sit directly over the attached base model in every frame. Match the same pose, scale, center point, torso, arms, legs, foot baseline, direction order, and row order. Sleeves and pant legs should follow the walk animation exactly.

Restrictions:
Only clothing pixels. No body. No skin. No head. No hair. No face. No shoes unless they are very simple integrated pant cuffs; boots should preferably be generated as a separate layer. No tools. No weapons. No backpack. No shadow. No floor. No labels. No text. Transparent background only.
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
