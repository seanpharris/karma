# Player v2 imported candidate review

This folder is for normalized candidate sheets from PixelLab or other local/downloaded generators.

Rules:

- Importers may write here, but runtime code should not load these files directly.
- Normalize candidates to Karma's active native player contract: `32x64` cells, 8 direction columns, 4 animation rows.
- Use descriptive stems such as `pixellab_engineer_v1_32x64_8dir_4row.png`.
- Promote a candidate into active runtime only after review for direction order, baseline, scale, readable silhouette, consistent identity, and clean transparent background.
- Do not store PixelLab API tokens or raw service credentials here.

Suggested import command:

```bash
python tools/import_pixellab_character.py path/to/pixellab-download.zip --output-dir assets/art/sprites/player_v2/imported --output-stem pixellab_engineer_v1
```
