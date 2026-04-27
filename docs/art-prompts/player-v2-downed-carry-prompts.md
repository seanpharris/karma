# Karma Player v2 Downed / Carry / Rescue Art Prompts

These prompts support the future Karma social loop where a downed player can be
helped, carried, abandoned, robbed, or executed. Generate these after the normal
idle/walk/sprint/interact base body is stable.

## Shared Contract

- Original transparent PNG pixel art.
- Same character proportions as Karma v2 base body.
- Frame size: 64x64.
- True 8 directions in order: front, front-right, right, back-right, back,
  back-left, left, front-left.
- No labels, grid, watermark, UI chrome, or background.
- Keep pose readable and grounded, not gory.

## Prompt 1 — Hurt to Downed Transition

```text
Create an original transparent PNG pixel-art spritesheet for Karma's v2 player
base body transitioning from hurt/staggered to downed.

Sheet contract:
- 8 columns, direction order: front, front-right, right, back-right, back,
  back-left, left, front-left.
- 4 rows for animation frames.
- Each frame is 64x64 pixels.
- Total sheet size: 512x256 pixels.
- Transparent background.

Animation:
- Frame 1: standing hurt/stagger pose.
- Frame 2: knees buckling or body losing balance.
- Frame 3: falling/lowering toward the ground.
- Frame 4: downed pose on the ground.
- Non-gory, readable as incapacitated.

Style:
- Crisp pixel art, clean outline, no blur, no labels, no grid, no watermark.
```

## Prompt 2 — Downed Idle

```text
Create an original transparent PNG pixel-art spritesheet for Karma's v2 player
base body in a downed idle state.

Sheet contract:
- 8 columns, direction order: front, front-right, right, back-right, back,
  back-left, left, front-left.
- 4 rows for subtle loop frames.
- Each frame is 64x64 pixels.
- Total sheet size: 512x256 pixels.
- Transparent background.

Animation:
- Player is on the ground, injured/incapacitated but alive.
- Subtle breathing or small arm movement only.
- Must read clearly from all directions.
- Non-gory.

Style:
- Crisp pixel art, clean outline, no blur, no labels, no grid, no watermark.
```

## Prompt 3 — Help-Up / Revive

```text
Create an original transparent PNG pixel-art spritesheet for Karma's v2 player
base body being helped up / revived.

Sheet contract:
- 8 columns, direction order: front, front-right, right, back-right, back,
  back-left, left, front-left.
- 6 rows for animation frames.
- Each frame is 64x64 pixels.
- Total sheet size: 512x384 pixels.
- Transparent background.

Animation:
- Frame 1: downed pose.
- Frame 2: reaching for help.
- Frame 3: being pulled upward / bracing on one knee.
- Frame 4: kneeling or crouched recovery.
- Frame 5: rising to feet.
- Frame 6: recovered standing pose.
- This sheet only draws the helped player, not the helper.

Style:
- Hopeful/rescue tone, non-gory, crisp pixel art, no labels, no grid, no
  watermark.
```

## Prompt 4 — Carry / Drag Pose for Carried Player

```text
Create an original transparent PNG pixel-art spritesheet for Karma's v2 player
base body in a carried/dragged incapacitated pose.

Sheet contract:
- 8 columns, direction order: front, front-right, right, back-right, back,
  back-left, left, front-left.
- 4 rows for subtle carried movement frames.
- Each frame is 64x64 pixels.
- Total sheet size: 512x256 pixels.
- Transparent background.

Animation:
- The player is limp or semi-conscious while being carried/dragged.
- Pose should align visually with a separate carrier animation.
- Non-gory, readable silhouette.

Style:
- Crisp pixel art, no blur, no labels, no grid, no watermark.
```

## Prompt 5 — Carrier Walk Overlay/Base

```text
Create an original transparent PNG pixel-art spritesheet for Karma's v2 player
base body carrying or dragging another downed player.

Sheet contract:
- 8 columns, direction order: front, front-right, right, back-right, back,
  back-left, left, front-left.
- 6 rows for carry-walk animation frames.
- Each frame is 64x64 pixels.
- Total sheet size: 512x384 pixels.
- Transparent background.

Animation:
- Heavier slower walk than normal.
- Arms/body posture indicate supporting another person.
- Should align with the separate carried-player pose sheet.
- True 8-direction poses.

Style:
- Grounded rescue/carry motion, crisp pixel art, no blur, no labels, no grid,
  no watermark.
```

## Note

For gameplay implementation, the first minimum viable set is:

1. `hurt_to_downed`
2. `downed_idle`
3. `help_up`

Carry/drag can come after revive/execute choices exist.
