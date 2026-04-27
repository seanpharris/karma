# Player Model Generation Next 15

This is the next review-oriented task list for generating and curating player model candidates after the native `32x64` player-v2 pipeline landed.

## Current intent

Generate a few candidate player models now if possible, keep them out of runtime, and let Sean review them tomorrow before promotion. PixelLab is preferred when available, but local deterministic candidates are useful as fallback/reference attempts.

## Tasks

1. **Create a review-only candidate folder** — keep all generated attempts under `assets/art/sprites/player_v2/imported/review_YYYY-MM-DD/` so runtime does not accidentally load them.
2. **Generate local fallback candidates** — create 3-5 deterministic `32x64`, 8-direction, 4-row candidates from the current skeleton for immediate review when external generation is blocked.
3. **Record candidate metadata** — write an index with source sheet, contract, generation method, and review notes.
4. **Try PixelLab candidate generation when available** — use PixelLab for stronger original concepts, but never paste/commit PixelLab API tokens.
5. **Import PixelLab outputs offline** — normalize downloaded PNG/ZIP outputs through `tools/import_pixellab_character.py` into the review folder.
6. **Audit candidate dimensions** — verify `256x256`, `32x64` cells, 8 direction columns, 4 rows, transparent background.
7. **Review direction readability** — check front/front-right/right/back-right/back/back-left/left/front-left ordering and whether diagonals read as true diagonals.
8. **Review baseline and scale** — ensure feet stay on a consistent baseline and head/body proportions stay stable across frames.
9. **Review animation rows** — confirm idle and three walk rows have visible but not chaotic stepping.
10. **Review paper-doll separability** — decide whether the candidate can split cleanly into base body, skin, hair, outfit, backpack/tool/weapon overlays.
11. **Pick one candidate direction** — choose one visual target or combine the best traits from multiple candidates.
12. **Normalize selected candidate** — clean transparent pixels, trim artifacts, enforce contract, and save a promoted candidate stem.
13. **Split selected candidate into layers** — derive base/skin/hair/outfit/overlay layers and verify pixel-perfect recomposition.
14. **Preview in runtime only after review** — wire the selected candidate behind the existing manifest/compositor path once accepted.
15. **Commit/push verified candidate slice** — run Windows `dotnet build Karma.csproj` and Godot headless `res://scenes/TestHarness.tscn`, then commit/push the reviewed slice.

## Review checklist for tomorrow

- Which silhouette is closest to Karma's player fantasy?
- Which color/material language works best: engineer, settler, medic, scavenger, or another role?
- Are the characters too busy at runtime scale?
- Should the active base body stay tool-free/backpack-free, with overlays for loadout states?
- Should PixelLab replace these local placeholders or use them as references?
