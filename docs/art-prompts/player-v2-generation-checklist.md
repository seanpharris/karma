# Karma Player v2 Art Generation Checklist

Use this as the short practical checklist while generating player sprite art.

## Files to Generate First

Generate these before any fancy costumes:

1. Base body idle — `512x256`, 8 columns x 4 rows, 64x64 frames.
2. Base body walk — `512x384`, 8 columns x 6 rows, 64x64 frames.
3. Base body sprint — `512x384`, 8 columns x 6 rows, 64x64 frames.
4. Base body interact — `512x256`, 8 columns x 4 rows, 64x64 frames.
5. Engineer outfit overlay matching the same four animation groups.
6. Hair overlay matching the same four animation groups.
7. Multi-tool overlay for idle/walk/interact.

## Direction Order

Every sheet uses this exact column order:

1. front
2. front-right
3. right
4. back-right
5. back
6. back-left
7. left
8. front-left

## Good Result Looks Like

- True diagonal poses, not cheap mirrored/cardinal copies.
- Feet stay on a consistent baseline.
- Character does not drift around inside each frame.
- Hands, feet, and head are never cropped.
- Transparent background only.
- No labels, grid, watermark, frame numbers, UI, or shadows baked into the sheet.
- Readable at game zoom.

## Bad Result — Regenerate or Fix

- Blurry/anti-aliased pixels.
- Direction labels or grid lines included in the image.
- Background color instead of transparency.
- Diagonal columns look identical to front/side columns.
- Frame sizes differ or sheet dimensions are wrong.
- Clothing layer redraws the entire character instead of only overlay pixels.
- Big pose mismatch between base body and outfit layer.

## Suggested Naming

Use temporary names like:

- `player_v2_base_idle_64_8dir.png`
- `player_v2_base_walk_64_8dir.png`
- `player_v2_base_sprint_64_8dir.png`
- `player_v2_base_interact_64_8dir.png`
- `player_v2_engineer_outfit_walk_64_8dir.png`
- `player_v2_hair_short_walk_64_8dir.png`
- `player_v2_multitool_interact_64_8dir.png`

Do not overwrite the current runtime `scifi_engineer_player_8dir.png` until the
new v2 sheets are validated and the runtime loader/compositor is ready.

## Where These Fit

Prompt files:

- `docs/art-prompts/player-v2-base-body-prompts.md`
- `docs/art-prompts/player-v2-paper-doll-layer-prompts.md`
- `docs/art-prompts/player-v2-downed-carry-prompts.md`

Longer design docs:

- `../../TASKS.md#professional-character-art-systems`
- `../../TASKS.md#character-art-generation-workflow`
- `../../TASKS.md#character-animation-pipeline`
