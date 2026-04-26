# Character Art Generation Workflow

This is the practical side-pipeline for generating Karma character art manually
with ChatGPT/image tools, Python, Aseprite, Krita, or similar.

## Target Runtime Contract

The game code expects a runtime-ready PNG with this exact shape:

- File type: PNG with RGBA transparency.
- Size: `256 x 288 px`.
- Grid: `8 columns x 9 rows`.
- Frame size: `32 x 32 px`.
- No labels, text, grid lines, guide boxes, metadata panels, watermarks, or decorative borders.
- Character feet should be bottom-centered in every frame.
- Transparent background. If a generator cannot do transparency, use flat bright green chroma key and remove it before committing.

Direction columns:

```text
0 front
1 front-right
2 right
3 back-right
4 back
5 back-left
6 left
7 front-left
```

Rows:

```text
0 idle
1 walk frame 1
2 walk frame 2
3 walk frame 3
4 walk frame 4
5 run/action-ready
6 tool/use
7 melee/impact
8 interact/reach
```

## Recommended Manual Pipeline

1. Generate a **reference sheet** with ChatGPT or another image model.
2. If the output contains labels/guides/text, treat it as reference only.
3. Create/export a clean runtime PNG at exactly `256 x 288`.
4. Run the validator:

```bash
python3 tools/prepare_character_sheet.py validate path/to/sheet.png
```

5. If the image has green chroma background, normalize it:

```bash
python3 tools/prepare_character_sheet.py normalize path/to/input.png assets/art/sprites/scifi_engineer_player_8dir.png --chroma
```

6. Validate the final asset again:

```bash
python3 tools/prepare_character_sheet.py validate assets/art/sprites/scifi_engineer_player_8dir.png
```

7. Run the game verification:

```bash
powershell -ExecutionPolicy Bypass -File .\tools\test.ps1; if ($LASTEXITCODE -eq 0) { powershell -ExecutionPolicy Bypass -File .\tools\snapshot.ps1 }; if ($LASTEXITCODE -eq 0) { powershell -ExecutionPolicy Bypass -File .\tools\check.ps1 } else { exit $LASTEXITCODE }
```

From WSL without `powershell.exe`, use the Windows executables directly as done
in existing agent sessions.

## Prompt: Full Runtime Sheet

Use this when asking ChatGPT/image tools for a complete character sheet.

```text
Create a clean 2D pixel-art top-down RPG character runtime sprite sheet for the game Karma.

Subject: <describe character/outfit/personality>. Sci-fi frontier colony style. Compact readable pixel art.

STRICT RUNTIME FORMAT:
- Output must be a clean sprite grid only.
- Canvas must be exactly 256x288 px if possible.
- 8 columns x 9 rows.
- Each frame is exactly 32x32 px.
- Direction columns left-to-right: front, front-right, right, back-right, back, back-left, left, front-left.
- Row 0: idle pose, one per direction.
- Rows 1-4: four-frame walking cycle for every direction.
- Row 5: run/action-ready pose for every direction.
- Row 6: tool/use pose for every direction.
- Row 7: melee/impact pose for every direction.
- Row 8: interact/reach pose for every direction.

TRUE 8-DIRECTION REQUIREMENT:
- Do not duplicate side-facing sprites for diagonal directions.
- Front-right and front-left must show a real three-quarter/front diagonal body silhouette.
- Back-right and back-left must show a real three-quarter/back diagonal body silhouette.
- Diagonal poses should have distinct head, torso, shoulder, arm, leg, and foot angles.
- The character should visibly rotate through all eight directions like a true 8-way RPG sprite.
- Mirroring left/right is acceptable, but front/back/diagonal views must not be the same pose.

VISUAL RULES:
- Transparent background preferred.
- If transparency is impossible, use perfectly flat #00ff00 chroma-key background.
- No shadows baked into the background.
- No labels, no text, no metadata, no guide boxes, no decorative border, no watermark.
- Feet bottom-centered in every 32x32 frame.
- Character proportions consistent across all frames.
- Nearest-neighbor crisp pixel art, no painterly blur, no antialiased soft edges.
- Keep outline pixels intact; do not use green inside the character.
```

## Prompt: True 8-Direction Template Like the Reference Image

Use this if the model keeps making cardinal-only or copied diagonal frames.

```text
Create a true 8-direction pixel-art character animation sheet similar to a professional RPG sprite template, but output ONLY the clean runtime grid.

Character: <engineer / rancher / medic / soldier / farmer>, readable 32x32 pixel-art proportions.

Output contract:
- PNG sprite sheet only.
- 256x288 px canvas.
- 8 columns x 9 rows.
- 32x32 px per frame.
- Transparent background, or flat #00ff00 chroma if transparency is impossible.
- No labels, no text, no grid lines, no side panels, no metadata, no preview boxes, no watermark.

Columns, left to right:
1 front
2 front-right three-quarter view
3 right side view
4 back-right three-quarter view
5 back view
6 back-left three-quarter view
7 left side view
8 front-left three-quarter view

Rows:
1 idle
2 walk frame 1
3 walk frame 2
4 walk frame 3
5 walk frame 4
6 run/action-ready
7 tool/use or shoot-ready
8 melee/impact
9 interact/reach

Important:
- Every diagonal column must be a real diagonal pose, not a copy of right/left/front/back.
- Front-right/front-left should show the face and chest partly turned.
- Back-right/back-left should show the back and shoulder partly turned.
- Side views should be profile only.
- Back view should clearly show the rear of the head/body.
- Keep feet aligned to bottom center in every 32x32 frame.
- Keep silhouette size consistent so the sprite does not pop or jitter while walking.
```

## Prompt: Blank Base Body

Use this first if building a reusable base + skin system.

```text
Create a clean 2D pixel-art top-down RPG blank humanoid base-body runtime sprite sheet for Karma.

Subject: neutral blank humanoid body, no clothing except minimal neutral underlayer, sci-fi frontier RPG proportions.

Format: exact 256x288 px runtime grid, 8 columns x 9 rows, 32x32 px frames.
Columns: front, front-right, right, back-right, back, back-left, left, front-left.
Rows: idle, walk1, walk2, walk3, walk4, run/action-ready, tool/use, melee/impact, interact/reach.

Transparent background, feet bottom-centered, consistent proportions, no labels, no guides, no text, no borders, no watermark.
```

## Prompt: Skin/Outfit Layer

Use this after the blank body exists. The output should align to the exact same
runtime grid.

```text
Create a transparent clothing/skin layer for the Karma 8-direction character runtime sheet.

Layer subject: <engineer suit / medic coat / raider armor / helmet / hair / backpack>.
This layer must align perfectly over the existing blank humanoid base-body sheet.

Exact format: 256x288 px, 8 columns x 9 rows, 32x32 px frames.
Keep all body-visible pixels transparent unless covered by the clothing/gear layer.
No labels, no guide boxes, no text, no border, no watermark.
```

## Validator Notes

`tools/prepare_character_sheet.py` checks:

- exact dimensions (`256 x 288`),
- transparent pixel presence,
- chroma-green leakage,
- empty frames,
- frames touching cell edges, which usually means cropping/alignment problems.

Warnings are not always fatal, but errors should be fixed before wiring the art
into the prototype.

## Current Bridge Asset

`tools/export_engineer_8dir.py` creates the current bridge player sheet from the
existing four-direction engineer art. It is useful for testing code and layout,
but it is not final art: diagonal/action frames are approximations. Replace
`assets/art/sprites/scifi_engineer_player_8dir.png` with a true generated or
hand-authored runtime sheet when ready.
