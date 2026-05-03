# Cleaned Gemini Natural Props — 2026-04-27

Extracted from `assets/art/reference/gemini_prototypes/2026-04-27/karma_natural_props_ref.jpg` using `tools/cleanup_gemini_reference_sheet.gd`.

## Output

- 16 individual transparent PNG sprites
- 128x128 normalized canvases
- `gemini_natural_props_cleaned_atlas.png` — 4x4 atlas, 128x128 cells
- `gemini_natural_props_cleaned_contact.png` — dark-background review sheet
- `manifest.json` — sprite names, source cells, canvas size

## Usability

Good enough for prototype use after automated cleanup. Best immediate candidates:

- `red_mineral_rock.png`
- `blue_crystal_shard.png`
- `cactus_succulent.png`
- `cracked_mud_clump.png`
- `berry_bush.png`
- `moss_patch.png`
- `mushroom_cluster.png`

Needs more manual cleanup before final/polished use:

- `fallen_branch.png`
- `dead_bush.png`
- `dry_grass_clump.png`
- `tiny_flowering_plant.png`
- `small_sapling.png`
- `puddle.png`

## Known issues

The source was a JPG with a flat gray background. Automated cleanup removed the background, but some sprites still have dark edge halos, jagged alpha, lost twig/stem detail, and JPG noise. These are acceptable for prototype placement, especially on darker/noisy terrain, but not final art.
