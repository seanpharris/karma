# Prompt: Small Daypack / Backpack Layer

## Queue

- Status: priority
- Asset type: equipment-layer
- Target output filename: `accessory_daypack_layer.png`
- Reference image: `/mnt/c/Users/pharr/code/player-sprite/player_base_body_sheet.png`

## Contract

- Tool: PixelLab
- Background: transparent only
- Style: clean readable pixel art, cozy sci-fi life-sim RPG, crisp outline, no blur
- Frame layout: Transparent 512x256 PNG sprite sheet, 8 columns x 4 rows, 64x64 frames.

## Prompt

```text
Use PixelLab to create a small backpack-only pixel art equipment layer for the attached humanoid base-body sprite sheet.

Use the attached base-body sheet as the exact alignment reference. Do not redraw the body. Draw only backpack/strap pixels on transparent pixels.

Layer:
Small practical sci-fi daypack. Muted olive-gray fabric, compact rectangular body, simple shoulder straps visible where direction allows, tiny orange utility tag. Lightweight colony-worker pack, not oversized, not military heavy armor.

Output:
Transparent PNG sprite sheet. 512x256 total. 8 columns x 4 rows. Each frame is 64x64.

Columns left to right:
south, south-east, east, north-east, north, north-west, west, south-west.

Rows top to bottom:
idle, walk1, walk2, walk3.

Alignment:
The backpack must sit on the character's back relative to each direction from the attached base model. On front-facing frames, only small straps or side hints should be visible. On back-facing frames, the pack body should be visible and centered on the back. Match the same pose, scale, torso position, walk cycle, direction order, and row order.

Restrictions:
Only backpack/strap pixels. No body. No skin. No hair. No face. No shirt or pants except minimal strap overlap. No tools. No weapons. No shadow. No floor. No labels. No text. Transparent background only.
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
