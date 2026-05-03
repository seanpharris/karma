# Karma Player v2 Paper-Doll Layer Prompts

Generate these after the base body exists. Every layer must match the exact same
frame size, direction order, row count, and pose timing as the base body sheet it
belongs to.

## Universal Layer Rules

Add this to every prompt:

```text
This is a transparent overlay layer for a paper-doll pixel character system. It
must align perfectly over the matching Karma v2 base body animation sheet. Use
the same frame size, row count, column count, direction order, body pose timing,
feet baseline, and frame centering as the referenced base body sheet. Draw only
the requested layer pixels. Do not redraw the full body unless the layer covers
that body area. Transparent background. No labels, no grid, no watermark.
```

## Prompt 1 — Simple Engineer Outfit, Idle/Walk/Sprint/Interact

Use once per animation group, replacing `{ANIMATION_GROUP}` and sheet dimensions.

```text
Create an original transparent PNG pixel-art paper-doll overlay sheet for a
sci-fi frontier engineer outfit for Karma's player character.

Layer content:
- practical work shirt or jumpsuit torso
- utility belt
- rugged pants
- work boots
- small accent panels that read as sci-fi frontier, not superhero armor
- no helmet, no hair, no weapon, no backpack

Animation group: {ANIMATION_GROUP}
Sheet contract:
- Match the referenced Karma v2 base body {ANIMATION_GROUP} sheet exactly.
- 8 direction columns in order: front, front-right, right, back-right, back,
  back-left, left, front-left.
- Each frame is exactly 64x64 pixels.
- Transparent background.
- Draw only outfit/clothing pixels that overlay the body.

Style:
- Original crisp pixel art, clean silhouette, readable at small size.
- Limited palette: dusty blue-gray fabric, warm tan utility straps, dark boots,
  small orange/teal sci-fi accents.
- No blur, no anti-aliasing, no gradients, no labels, no grid, no watermark.
```

## Prompt 2 — Hair Layer

```text
Create an original transparent PNG pixel-art paper-doll hair overlay sheet for
Karma's v2 player character.

Layer content:
- short practical adventurer hair
- readable front, side, back, and diagonal silhouettes
- no face, no body, no clothes, no helmet

Sheet contract:
- Match the referenced Karma v2 base body animation sheet exactly.
- 8 direction columns in order: front, front-right, right, back-right, back,
  back-left, left, front-left.
- Each frame is exactly 64x64 pixels.
- Transparent background.
- Draw only hair pixels.

Style:
- Crisp pixel art, clean outline, limited palette, no blur, no labels, no grid,
  no watermark.
```

## Prompt 3 — Backpack / Satchel Layer

```text
Create an original transparent PNG pixel-art paper-doll backpack/satchel overlay
sheet for Karma's v2 player character.

Layer content:
- compact survival backpack or cross-body satchel
- visible mostly from back/side/diagonal directions
- subtle straps visible from front if appropriate
- no full body, no clothing, no weapon

Sheet contract:
- Match the referenced Karma v2 base body animation sheet exactly.
- 8 direction columns in order: front, front-right, right, back-right, back,
  back-left, left, front-left.
- Each frame is exactly 64x64 pixels.
- Transparent background.
- Draw only backpack/satchel and strap pixels.

Style:
- Sci-fi frontier utility look, rugged canvas/leather plus small tech clasp.
- Crisp pixel art, no blur, no labels, no grid, no watermark.
```

## Prompt 4 — Held Multi-Tool Overlay

```text
Create an original transparent PNG pixel-art held-item overlay sheet for Karma's
v2 player character holding a compact sci-fi multi-tool.

Layer content:
- one hand holding a compact multi-tool where visible
- small glow/accent at the tool tip during interaction frames if appropriate
- no full body, no clothing, no hair

Sheet contract:
- Match the referenced Karma v2 base body animation sheet exactly.
- 8 direction columns in order: front, front-right, right, back-right, back,
  back-left, left, front-left.
- Each frame is exactly 64x64 pixels.
- Transparent background.
- Draw only held-tool and necessary hand-overlap pixels.

Animation behavior:
- For idle/walk/sprint: tool is carried low or at side.
- For interact/tool_use: tool points toward the facing direction and reads as
  actively being used.

Style:
- Crisp readable pixel art, limited palette, small teal/yellow tech accent, no
  blur, no labels, no grid, no watermark.
```

## Prompt 5 — Stun Baton / Melee Overlay

```text
Create an original transparent PNG pixel-art held-item overlay sheet for Karma's
v2 player character using a compact stun baton.

Layer content:
- compact baton in the hand
- readable baton positions for all 8 directions
- optional tiny electric accent pixels during attack/use frames
- no full body, no clothing, no hair

Sheet contract:
- Match the referenced Karma v2 base body animation sheet exactly.
- 8 direction columns in order: front, front-right, right, back-right, back,
  back-left, left, front-left.
- Each frame is exactly 64x64 pixels.
- Transparent background.
- Draw only baton/weapon overlay and necessary hand-overlap pixels.

Style:
- Grounded sci-fi frontier tool-weapon, not fantasy sword.
- Crisp pixel art, no blur, no labels, no grid, no watermark.
```

## Recommended First Batch

Generate in this order:

1. Base body idle.
2. Base body walk.
3. Base body sprint.
4. Base body interact.
5. Engineer outfit matching idle/walk/sprint/interact.
6. Hair matching idle/walk/sprint/interact.
7. Multi-tool matching idle/walk/interact.

That is enough to make the player look dramatically better without generating a
huge costume/item library yet.
