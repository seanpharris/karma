# Generated Art References

These files are generated reference candidates, not validated runtime sheets.

## 2026-04-26 Gemini player v2 attempts

- `karma_player_v2_engineer_checker_reference.jpg`
  - First Gemini pass for a v2 sci-fi frontier engineer player sheet.
  - Useful visual direction, but checkerboard transparency appears baked into the image.
- `karma_player_v2_engineer_chroma_reference.jpg`
  - Better extraction candidate with bright green chroma background.
  - Still not runtime-ready; visible guide/grid artifacts and inconsistent columns need cleanup.
- `karma_player_v2_engineer_8x4_chroma_candidate.jpg`
  - Tighter 8x4 prompt attempt.
  - Still drifted toward extra columns, so treat as reference only.

Do not wire these directly into runtime. Next step is either generate a stricter sheet,
manually crop/paint a true 8-column x 4-row sheet from the best poses, or use them as
style references for a human/AI cleanup pass.

## 2026-04-26 Gemini smaller direction batches

- `karma_player_v2_front_pose_batch.jpg`
  - Front-facing only, green chroma background. Useful for idle/front reference, but walk progression is subtle.
- `karma_player_v2_right_pose_batch.jpg`
  - Right-facing only, green chroma background. Strongest extraction/reference candidate so far; side walk/run silhouettes are readable and consistent.
- `karma_player_v2_back_pose_batch.jpg`
  - Back-facing only, green chroma background. Useful rear-view supplement, but color/outfit consistency may need matching to the other batches.

The smaller-batch approach worked better than the first broad full-sheet prompts, but separate direction generations drifted in style.

## 2026-04-26 Gemini 64px full-sheet chroma regeneration

- `karma_player_v2_64px_full_sheet_chroma_regen.jpg`
  - Full 4x4 prompt with one consistent sci-fi frontier engineer on a flat #00FF00 chroma background.
  - Rows requested: front, right, back, front-right.
  - Columns requested: idle, walk A, walk B, tool-ready.
  - This became the current temporary 64px runtime candidate after chroma extraction and normalization.

The current extraction pass lives in `assets/art/sprites/generated/` and is produced by `tools/extract_player_v2_64px_full_sheet.gd`. It keys the chroma background, normalizes each cell into 64x64 frames, transposes direction rows into runtime animation rows, mirrors temporary left-facing directions, and writes `player_v2_engineer_8dir_4row_candidate.png`.

## 2026-04-26 Gemini strict walk-strip movement patch

- `karma_player_v2_64px_right_walk_strict.jpg`
- `karma_player_v2_64px_up_right_walk_strict.jpg`
- `karma_player_v2_64px_back_walk_strict.jpg`
  - Strict one-row chroma prompts for visible no-tool walking steps.
  - Merged into the runtime candidate with `tools/merge_player_v2_64px_walk_strips.gd`.
  - Runtime columns patched: right, back-right/up-right, back/up, mirrored back-left/up-left, and mirrored left.

Remaining art work: stronger front/down stepping, final per-frame alignment/artifact cleanup, true bespoke left-facing frames instead of mirrors, and eventually rebuilding the paper-doll layer stack at this 64px style/scale.
