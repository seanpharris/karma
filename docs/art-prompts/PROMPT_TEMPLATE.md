# Prompt: <Asset Name>

## Queue

- Status: still-needs-generated | priority | completed
- Asset type: base-body | clothing-layer | hair-layer | equipment-layer | world-item | dual-use-item | recolor-task
- Target output filename: `<suggested_output_name>.png`
- Reference image: `<path or none>`

## Contract

- Tool: PixelLab
- Background: transparent only
- Style: clean readable pixel art, cozy sci-fi life-sim RPG, crisp outline, no blur
- Frame layout: describe exact canvas/sheet size
- Direction order, when sheet-based: south, south-east, east, north-east, north, north-west, west, south-west
- Row order, when sheet-based: idle, walk1, walk2, walk3

## Prompt

```text
Use PixelLab to ...
```

## Acceptance checks

- Transparent background
- Correct dimensions
- Correct direction/row order if sheet-based
- No labels/text/grid/shadow/floor
- Contains only the requested layer/item pixels
- Aligns to the base-body reference if layer-based

## Import notes

- Save generated art to `/mnt/c/Users/pharr/code/player-sprite/` first unless importing directly.
- After import/review succeeds, move this prompt to `docs/art-prompts/completed/`.
