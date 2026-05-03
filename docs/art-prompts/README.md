# Art Prompt Queue

PixelLab/Claude art prompts for Karma player-v2 sprite work.


## Prompt file template

New prompt files should follow `PROMPT_TEMPLATE.md`. Required sections:

- `## Queue`
- `## Contract`
- `## Prompt`
- `## Acceptance checks`
- `## Import notes`

Use one markdown file per asset concept. For dual-use tools/weapons, keep the world-item prompt and wielded-layer prompt together in the same file.

## Priority queue

Prototype-critical prompts live in `priority/`. Generate these first.

Current recommended order:

1. `boots_utility_layer.md`
2. `outfit_separate_pants_layer.md`
3. `outfit_separate_shirt_layer.md`
4. `hair_short_practical_layer.md`
5. `hair_short_copper_layer.md`
6. `accessory_daypack_layer.md`
7. `tool_multitool_hand_layer.md`
8. `outfit_utility_jumpsuit_layer.md` as an all-in-one fallback/test outfit

9. `item_multitool_dual_use.md`
10. `item_welding_torch_dual_use.md`
11. `weapon_practice_baton_dual_use.md`

## Workflow

1. Prompts waiting for generation live in `still-needs-generated/`.
2. Generate the art manually with PixelLab/Claude using the referenced base sheet.
3. Save generated PNGs into `/mnt/c/Users/pharr/code/player-sprite/` or directly into the Karma art folder.
4. After a prompt's output is imported/reviewed, move that prompt to `completed/`.

## Current base reference

Use this base-body sheet as the alignment reference for all clothing/equipment layers:

- `/mnt/c/Users/pharr/code/player-sprite/player_base_body_sheet.png`
- Karma copy: `assets/art/sprites/player_v2/imported/player_base_body_sheet_64px_8dir_4row.png`

Base sheet contract:

- 512x256 PNG
- 8 columns x 4 rows
- 64x64 per frame
- Columns: south, south-east, east, north-east, north, north-west, west, south-west
- Rows: idle, walk1, walk2, walk3

## Dual-use item prompts

For tools/weapons that can appear both in-world and in-hand, keep both prompts in the same markdown file:

- **World / inventory item:** single transparent 64x64 sprite.
- **Wielded overlay:** transparent 512x256 layer aligned to the base-body sheet.

This lets us test one item through pickup, inventory, world rendering, and paper-doll equipment rendering.

## Gemini static prototype art

Use Gemini for static prototype references where strict alpha/layer alignment is not required. Current priority prompts live in:

- `priority/gemini-static-prototype/static_props_sheet.md`
- `priority/gemini-static-prototype/natural_props_sheet.md`
- `priority/gemini-static-prototype/item_icons_sheet.md`
- `priority/gemini-static-prototype/terrain_materials_sheet.md`

Save generated references under `assets/art/reference/gemini_prototypes/YYYY-MM-DD/`. Treat them as concept/reference until cleaned: Gemini often returns JPGs, non-transparent backgrounds, labels, inconsistent scale, and non-seamless terrain.
