# Prompt: Gemini Natural Prototype Props Sheet

## Queue

- Status: priority
- Asset type: natural-prop reference sheet
- Target output filename: `karma_natural_props_ref.jpg`
- Reference image: none

## Contract

- Tool: Gemini image generation
- Background: plain neutral background acceptable for reference; transparent preferred but not expected
- Style: clean readable pixel art, cozy sci-fi life-sim RPG, crisp outline, minimal blur
- Frame layout: 4x4 concept/reference grid, not final atlas

## Prompt

```text
Create a clean pixel art prototype natural-object sheet for a cozy sci-fi frontier life-sim RPG called Karma.

Purpose: concept/reference art to replace placeholder natural objects after cleanup.

Output: a neat 4x4 grid of static natural sprites on a plain flat neutral background. No labels, no text, no UI, no characters. Leave consistent spacing between objects.

Objects: alien shrub, dry grass clump, moss patch, tiny flowering plant, mushroom cluster, red mineral rock, blue crystal shard, fallen branch, dead bush, smooth river stone, cactus-like succulent, glowing lichen patch, puddle, cracked mud clump, berry bush, small sapling.

Style: crisp readable pixel art, cozy but slightly strange alien frontier ecology, muted natural colors with small saturated accents, low top-down/three-quarter RPG perspective, strong silhouettes, minimal painterly blur.
```

## Acceptance checks

- No labels/text/UI/characters
- Strong readable silhouettes
- Natural/alien frontier style
- Good candidates for cleanup into individual sprites
- Plain background is acceptable for reference, but production import needs transparency cleanup

## Import notes

- Save generated reference to `assets/art/reference/gemini_prototypes/YYYY-MM-DD/`.
- Cleanup/import only selected props into production atlases.
- After cleanup succeeds, move this prompt to `docs/art-prompts/completed/`.
