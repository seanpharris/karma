# Prompt: Feminine Body-Type Base Sheet

## Queue

- Status: still-needs-generated
- Asset type: base-body
- Target output filename: `body_feminine_base_sheet_64px_8dir_4row.png`
- Reference image: `/mnt/c/Users/pharr/code/player-sprite/player_base_body_sheet.png` optional visual/scale reference

## Contract

- Tool: PixelLab
- Background: transparent only
- Style: clean readable pixel art, cozy sci-fi life-sim RPG, crisp outline, no blur
- Frame layout: transparent 512x256 PNG sprite sheet, 8 columns x 4 rows, 64x64 frames
- Direction order: south, south-east, east, north-east, north, north-west, west, south-west
- Row order: idle, walk1, walk2, walk3

## Prompt

```text
Use PixelLab to create a reusable feminine body-type player base sprite sheet for a cozy sci-fi life-sim RPG.

Style:
Production-quality pixel art, low top-down RPG view, clean dark outline, transparent background, no text, no labels, no shadow, no floor.

Character:
Neutral humanoid feminine paper-doll base model. Bald head, no hair, no clothing, no armor, no backpack, no tools, no weapons, no accessories. Minimal neutral underwear or simple skin-tone mannequin coverage only, suitable for layering clothes, hair, armor, boots, and equipment on top later.

Proportions:
Compact readable 32x64-ish humanoid proportions inside a 64x64 canvas. Slightly narrower shoulders, subtly wider hips, feminine torso silhouette, but keep it practical and non-exaggerated. Character should occupy about 48-56 pixels of height, centered horizontally, with feet/pivot consistently near the bottom center of each frame.

Output:
Transparent PNG sprite sheet. 512x256 total. 8 columns x 4 rows. Each frame is 64x64.

Columns left to right:
south, south-east, east, north-east, north, north-west, west, south-west.

Rows top to bottom:
idle, walk1, walk2, walk3.

Animation:
Idle plus 3 walk frames per direction. Simple readable walking loop. Arms swing opposite legs. Feet step clearly but stay on a consistent baseline.

Requirements:
Same body, same proportions, same scale, same silhouette, same foot baseline across every direction. Clear head, torso, arms, legs, hands, and feet. Keep internal body detail minimal so clothing layers can cover it cleanly.

Restrictions:
No baked-in shirt, pants, boots, gloves, belt, hair, facial hair, helmet, backpack, gear, tools, weapons, shadow, floor, labels, or text.
```

## Acceptance checks

- Transparent 512x256 sheet
- Correct direction and row order
- No labels/text/grid/shadow/floor
- Bald neutral base, no baked-in clothes/hair/accessories
- Feet share a consistent baseline and body stays centered
- Usable as a paper-doll base

## Import notes

- Save generated art to `/mnt/c/Users/pharr/code/player-sprite/` first unless importing directly.
- Normalize into Karma's 32x64 and runtime player-v2 formats before wiring.
- After import/review succeeds, move this prompt to `docs/art-prompts/completed/`.
