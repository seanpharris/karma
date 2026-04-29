# Prompt: Medic Outfit Layer

## Queue

- Status: still-needs-generated
- Asset type: clothing-layer
- Target output filename: `outfit_medic_layer.png`
- Reference image: `/mnt/c/Users/pharr/code/player-sprite/player_base_body_sheet.png`

## Contract

- Tool: PixelLab
- Background: transparent only
- Style: clean readable pixel art, cozy sci-fi life-sim RPG, crisp outline, no blur
- Frame layout: Transparent 512x256 PNG sprite sheet, 8 columns x 4 rows, 64x64 frames.

## Prompt

```text
Use PixelLab to create a medic outfit-only pixel art clothing layer for the attached humanoid base-body sprite sheet.

Use the attached base-body sheet as the exact alignment reference. Do not redraw the body. Draw only the medic outfit pixels on transparent pixels.

Layer:
Cozy sci-fi field medic outfit. Off-white or pale teal jacket panels, dark undersuit/pants, small red-orange medical cross-style accent patch, soft utility pouches. Practical and readable, not bulky armor.

Output:
Transparent PNG sprite sheet. 512x256 total. 8 columns x 4 rows. Each frame is 64x64.

Columns left to right:
south, south-east, east, north-east, north, north-west, west, south-west.

Rows top to bottom:
idle, walk1, walk2, walk3.

Alignment:
Match the attached base model's pose, scale, center point, torso, arms, legs, foot baseline, direction order, and row order exactly.

Restrictions:
Only clothing pixels. No body, skin, head, hair, face, tools, weapons, backpack, shadow, floor, labels, or text. Transparent background only.
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
