# Player model review candidates — 2026-04-26

These are local deterministic review attempts generated from Karma's current native `32x64` player skeleton because PixelLab/MCP generation is not directly available in this OpenClaw session.

They are **not runtime-loaded** and should not be promoted until reviewed.

## Candidates

- `local_engineer_a_32x64_8dir_4row.png` — closer to the current frontier engineer, with clearer tech/visor accents.
- `local_settler_cloak_a_32x64_8dir_4row.png` — warmer frontier settler/worker silhouette with cloak/back cues.
- `local_medic_a_32x64_8dir_4row.png` — station medic/rescue worker direction, useful if the downed/rescue loop becomes central.
- `local_scavenger_a_32x64_8dir_4row.png` — rougher salvage runner silhouette with darker pack/tool cues.

## Contract

- `256x256` sheet
- `8` direction columns
- `4` animation rows
- `32x64` cells
- Direction order: front, front-right, right, back-right, back, back-left, left, front-left
- Rows: idle, walk A, walk B, walk C

## Review questions

- Which silhouette/color language best matches Karma?
- Which candidate is readable at gameplay zoom?
- Should the base model stay plain while these become outfit overlays?
- Should PixelLab use one of these as reference or replace them entirely?
