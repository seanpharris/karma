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

The smaller-batch approach works better than asking Gemini for a full 8-direction sheet. The first extraction pass now lives in `assets/art/sprites/generated/`: it extracts the best front/right/back frames, mirrors right to left as a temporary prototype, and composites a 64x64-frame 8-direction candidate sheet. Remaining art work: hand/AI-clean true diagonals and style-match the back/front/right batches.
