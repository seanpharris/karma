# Prompt/Task: Skin Palette Variants

## Queue

- Status: still-needs-generated
- Asset type: recolor-task
- Target output filename: `skin_light`, `skin_medium`, `skin_deep` variant sheets
- Reference image: `assets/art/sprites/player_v2/imported/player_base_body_sheet_32x64_8dir_4row.png`

## Contract

- Tool: local recolor tooling preferred; PixelLab only if recolor isolation fails
- Background: preserve existing transparency
- Style: preserve existing pixel art exactly except skin ramp colors
- Frame layout: preserve source dimensions, alpha, silhouette, direction order, and row order

## Prompt

```text
This is a local art-processing task, not a generation prompt unless recoloring fails.

Create skin palette variants from the base-body sheet:
- light skin ramp
- medium skin ramp
- deep skin ramp
- optional cool/alien skin ramp later

Keep exact alpha, silhouette, frame layout, animation pixels, and transparency.
Recolor only skin ramp pixels.
Preserve highlight/mid/shadow relationships.
Do not regenerate unless the source palette is too messy to isolate reliably.
```

## Acceptance checks

- Exact same dimensions and alpha coverage as source
- Only skin colors changed
- Shading ramp remains readable
- No drift, no new pixels, no body-shape changes

## Import notes

- Implement with local Python/Godot recolor tooling if possible.
- After import/review succeeds, move this prompt to `docs/art-prompts/completed/`.
