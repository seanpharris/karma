# Prompt: Gemini Static Prototype Props Sheet

## Queue

- Status: priority
- Asset type: world-item / environment-prop reference sheet
- Target output filename: `karma_static_props_ref.jpg`
- Reference image: none

## Contract

- Tool: Gemini image generation
- Background: plain neutral background acceptable for reference; transparent preferred but not expected
- Style: clean readable pixel art, cozy sci-fi life-sim RPG, crisp outline, minimal blur
- Frame layout: 4x4 concept/reference grid, not final atlas

## Prompt

```text
Create a clean pixel art prototype prop sheet for a cozy sci-fi frontier life-sim RPG called Karma.

Purpose: concept/reference art to replace placeholder prototype objects after cleanup.

Output: a neat 4x4 grid of static object sprites on a plain flat neutral background. No labels, no text, no UI, no characters. Leave consistent spacing between objects.

Objects: cargo crate, repair kit case, utility junction box, hydroponics planter, oxygen tank rack, compact kiosk terminal, airlock door front, station wall segment, solar panel, pipe cluster, landing beacon, exterior lamp post, maintenance hatch, small medical supply box, portable terminal, power cell canister.

Style: crisp readable pixel art, cozy sci-fi colony/frontier, muted grays and tans with small teal/orange accents, low top-down/three-quarter RPG perspective, strong silhouettes, minimal painterly blur.
```

## Acceptance checks

- No labels/text/UI/characters
- Strong readable silhouettes
- Coherent sci-fi colony style
- Good candidates for cleanup into individual sprites
- Plain background is acceptable for reference, but production import needs transparency cleanup

## Import notes

- Save generated reference to `assets/art/reference/gemini_prototypes/YYYY-MM-DD/`.
- Cleanup/import only selected props into production atlases.
- After cleanup succeeds, move this prompt to `docs/art-prompts/completed/`.
