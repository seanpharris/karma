# Prompt: Gemini Terrain / Material Reference Sheet

## Queue

- Status: priority
- Asset type: terrain/material reference sheet
- Target output filename: `karma_terrain_materials_ref.jpg`
- Reference image: none

## Contract

- Tool: Gemini image generation
- Background: plain neutral background acceptable for reference
- Style: clean readable pixel art, cozy sci-fi life-sim RPG, crisp outline, minimal blur
- Frame layout: 4x4 material concept grid, not final seamless/autotile atlas

## Prompt

```text
Create a clean pixel art prototype terrain/material reference sheet for a cozy sci-fi frontier life-sim RPG called Karma.

Purpose: concept/reference art for future tile cleanup. These do not need to be final seamless tiles yet.

Output: a neat 4x4 grid of square terrain/material samples on a plain flat neutral background. No labels, no text, no UI, no characters. Each sample should fill its square.

Materials: metal station floor, worn plating, clinic floor, hydroponics floor, dirt path, compacted gravel, dry grass, mossy ground, cracked concrete, hazard-striped maintenance floor, dark interior floor, light interior floor, wet mud, sand, rocky ground, landing pad surface.

Style: crisp readable pixel art, top-down tile perspective, cozy sci-fi frontier, muted palette, lower contrast than characters/props, minimal painterly blur, avoid text or symbols except abstract hazard stripes.
```

## Acceptance checks

- No labels/text/UI/characters
- Useful material vocabulary
- Lower contrast than characters/props
- Treat as reference only; do not assume seamless tiling

## Import notes

- Save generated reference to `assets/art/reference/gemini_prototypes/YYYY-MM-DD/`.
- Author real tile/autotile assets separately from these references.
- After tile cleanup succeeds, move this prompt to `docs/art-prompts/completed/`.
