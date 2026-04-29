# Prompt: Gemini Prototype Item Icon Sheet

## Queue

- Status: priority
- Asset type: world-item / inventory-icon reference sheet
- Target output filename: `karma_item_icons_ref.jpg`
- Reference image: none

## Contract

- Tool: Gemini image generation
- Background: plain neutral background acceptable for reference; transparent preferred but not expected
- Style: clean readable pixel art, cozy sci-fi life-sim RPG, crisp outline, minimal blur
- Frame layout: 4x4 concept/reference grid, not final atlas

## Prompt

```text
Create a clean pixel art prototype item icon sheet for a cozy sci-fi frontier life-sim RPG called Karma.

Purpose: concept/reference art for world pickups and inventory icons after cleanup.

Output: a neat 4x4 grid of centered item sprites on a plain flat neutral background. No labels, no text, no UI, no characters. Leave consistent spacing between items.

Items: whoopie cushion, deflated balloon, repair kit, practice baton, work vest, ration pack, data chip, filter core, contraband package, apology flower, portable terminal, scrip coin/token, stun baton, multi-tool, welding torch, medi patch.

Style: crisp readable pixel art, cozy sci-fi frontier items, low top-down/three-quarter RPG perspective, strong silhouettes, bright enough to read at small size, muted palette with teal/orange accents, minimal painterly blur.
```

## Acceptance checks

- No labels/text/UI/characters
- Strong readable item silhouettes
- Items feel compatible with Karma's tone
- Good references for clean 64x64 world/inventory sprites

## Import notes

- Save generated reference to `assets/art/reference/gemini_prototypes/YYYY-MM-DD/`.
- If Gemini adds labels/text, mark output as reference-only and regenerate stricter or hand-clean.
- After cleanup succeeds, move this prompt to `docs/art-prompts/completed/`.
