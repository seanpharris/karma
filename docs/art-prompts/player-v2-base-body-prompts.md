# Karma Player v2 Base Body Art Prompts

Use these prompts to generate the **new professional player base body**. This is
not the old temporary `32x32` sheet. The goal is a clean reusable base for a
paper-doll/layered character system.

## Required Contract

- Original license-safe pixel art. Do not copy LPC, Stardew, Pokemon, Eastward,
  or any commercial/game asset.
- Transparent background.
- Orthographic top-down / 3/4 RPG perspective.
- Pixel art with crisp hard edges; no anti-aliased blur, no painterly shading.
- Frame size: **64x64** preferred. Use **48x48** only if generation struggles.
- True 8 directions in this order:
  1. front
  2. front-right
  3. right
  4. back-right
  5. back
  6. back-left
  7. left
  8. front-left
- Keep the body centered consistently. Feet should land on the same baseline.
- No clothing except neutral underwear/simple modest base layer.
- No weapons, props, backpack, text, labels, grid lines, shadows, watermarks, or
  UI chrome.

## Prompt 1 — Base Body Idle Sheet

```text
Create an original transparent PNG pixel-art spritesheet for a top-down 2D RPG
player base body, sci-fi frontier life-sim style, neutral human adult, reusable
paper-doll base layer, no clothing except simple neutral underlayer, no hair, no
weapons, no props.

Spritesheet contract:
- 8 columns, one direction per column.
- Direction order left to right: front, front-right, right, back-right, back,
  back-left, left, front-left.
- 4 rows for idle animation frames.
- Each frame is exactly 64x64 pixels.
- Total sheet size: 512x256 pixels.
- Transparent background.
- Feet aligned on a consistent baseline in every frame.
- Body centered consistently in every frame.

Animation:
- Idle breathing / weight shift only.
- Subtle motion: shoulders and chest move by 1 pixel, head/body very slightly
  shifts, no walking steps.
- Keep silhouette stable and readable.

Style:
- Crisp readable pixel art, limited palette, clean outline, readable hands/feet,
  no blur, no anti-aliasing, no gradients, no labels, no grid, no watermark.
``` 

## Prompt 2 — Base Body Walk Sheet

```text
Create an original transparent PNG pixel-art spritesheet for the same top-down
2D RPG player base body as the previous prompt. This is the walk animation layer
for a reusable paper-doll character system.

Spritesheet contract:
- 8 columns, one direction per column.
- Direction order left to right: front, front-right, right, back-right, back,
  back-left, left, front-left.
- 6 rows for walk animation frames.
- Each frame is exactly 64x64 pixels.
- Total sheet size: 512x384 pixels.
- Transparent background.
- Feet aligned to a consistent ground baseline.
- Body centered consistently; no camera or sheet drift.

Animation:
- Natural walk cycle with clear alternating legs and arms.
- True diagonal poses, not mirrored front/side placeholders.
- Front/back frames show readable shoulder/hip sway.
- Side frames show readable stride and arm counter-swing.
- Diagonal frames should look like real diagonal movement.

Style:
- Crisp pixel art, clean outline, limited palette, no blur, no labels, no grid,
  no watermark, no clothing except neutral base underlayer, no hair, no props.
``` 

## Prompt 3 — Base Body Sprint Sheet

```text
Create an original transparent PNG pixel-art spritesheet for the same top-down
2D RPG player base body as the previous prompts. This is the sprint/run animation
layer for a reusable paper-doll character system.

Spritesheet contract:
- 8 columns, one direction per column.
- Direction order left to right: front, front-right, right, back-right, back,
  back-left, left, front-left.
- 6 rows for sprint animation frames.
- Each frame is exactly 64x64 pixels.
- Total sheet size: 512x384 pixels.
- Transparent background.
- Feet aligned to a consistent baseline.

Animation:
- More energetic than walk: forward lean, longer stride, stronger arm swing.
- Keep movement readable without smear/blur.
- True 8-direction poses.
- Avoid exaggerated cartoon squash; this is a grounded sci-fi frontier RPG.

Style:
- Crisp pixel art, clean outline, limited palette, no anti-aliasing, no labels,
  no grid, no watermark, no hair, no clothing except neutral base underlayer,
  no weapons or props.
``` 

## Prompt 4 — Base Body Interaction Sheet

```text
Create an original transparent PNG pixel-art spritesheet for the same top-down
2D RPG player base body. This sheet contains generic interaction/reach frames for
opening, picking up, repairing, talking, or touching a nearby object.

Spritesheet contract:
- 8 columns, one direction per column.
- Direction order left to right: front, front-right, right, back-right, back,
  back-left, left, front-left.
- 4 rows for interaction animation frames.
- Each frame is exactly 64x64 pixels.
- Total sheet size: 512x256 pixels.
- Transparent background.
- Feet stay planted on the same baseline.

Animation:
- Frame 1: neutral ready pose.
- Frame 2: arm starts reaching toward facing direction.
- Frame 3: full reach/use pose.
- Frame 4: return pose.
- Should work for buttons, crates, terminals, repairs, and NPC interactions.

Style:
- Crisp readable pixel art, no blur, no labels, no grid, no watermark, no hair,
  no clothing except neutral base underlayer, no props.
``` 

## Validation Checklist

Before accepting a sheet:

- [ ] Transparent background.
- [ ] Exact expected dimensions.
- [ ] No labels/grid/text/watermark.
- [ ] Every column is a distinct true direction.
- [ ] Feet align consistently.
- [ ] No cropped hands, feet, or head.
- [ ] Frames do not drift within the cell.
- [ ] Looks readable at game zoom.
