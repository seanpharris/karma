# Prompt: Repair Kit World / Inventory Item

## Queue

- Status: still-needs-generated
- Asset type: world-item
- Target output filename: `item_repair_kit_world.png`
- Reference image: `none`

## Contract

- Tool: PixelLab
- Background: transparent only
- Style: clean readable pixel art, cozy sci-fi life-sim RPG, crisp outline, no blur
- Frame layout: Transparent single 64x64 PNG item sprite.

## Prompt

```text
Use PixelLab to create a small repair kit item sprite for a cozy sci-fi life-sim RPG.

Item:
Compact field repair kit. Small rugged case, muted blue-gray shell, orange latch, tiny wrench/bolt icon-like detail painted on the case. Practical colony utility gear.

Output:
Transparent PNG. Single item sprite centered in a 64x64 canvas. Usable as a world pickup and inventory icon.

Style:
Clean readable pixel art, crisp outline, low top-down RPG item perspective, cozy sci-fi frontier equipment.

Restrictions:
No character body, no hand, no background, no shadow, no floor, no labels, no readable text, no UI frame.
```

## Acceptance checks

- Transparent background
- Correct dimensions
- No labels/text/grid/shadow/floor
- Single centered item with enough transparent margin
- Readable as world pickup and inventory icon

## Import notes

- Save generated art to `/mnt/c/Users/pharr/code/player-sprite/` first unless importing directly.
- After import/review succeeds, move this prompt to `docs/art-prompts/completed/`.
