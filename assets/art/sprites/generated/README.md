# Generated Sprite Candidates

These PNGs are extracted/normalized from Gemini reference batches. They are
transparent, exact-size candidate sheets, but they are still **prototype
candidates**, not final v2 runtime art. The current runtime candidate comes from
a regenerated full 4x4 chroma sheet so the 64px preview can be judged in-game
without opaque background blocks.

## Files

- `player_v2_front_pose_extract.png`, `player_v2_right_pose_extract.png`, `player_v2_back_pose_extract.png`
  - Older 64x256 extraction candidates from separate direction batches.
- `player_v2_64px_front_full_sheet_extract.png`, `player_v2_64px_right_full_sheet_extract.png`, `player_v2_64px_back_full_sheet_extract.png`, `player_v2_64px_front_right_full_sheet_extract.png`
  - 256x64 direction-row extracts from the regenerated full-sheet chroma prompt.
- `player_v2_64px_right_walk_strict_extract.png`, `player_v2_64px_up_right_walk_strict_extract.png`, `player_v2_64px_up_left_walk_strict_extract.png`, `player_v2_64px_back_walk_strict_extract.png`
  - Strict no-tool walk-strip extracts merged over the weak movement directions.
- `player_v2_knight_8dir_4row_reference.png`
  - 512x256, 8 columns x 4 rows, 64x64 frames.
  - Current preferred prototype player sheet when present. Extracted from the user-provided `assets/art/2D Character Knight/` pack because its true 8-direction walk continuity is much better than the generated candidate sheets.
  - The source pack row order is north-east, east, south-east, south, south-west, west, north-west, north; `tools/extract_knight_preview.gd` remaps it into Karma runtime column order: front/down, front-right, right, back-right, back, back-left, left, front-left.
  - Prototype/reference only until the source/license is confirmed.
- `player_v2_engineer_8dir_4row_candidate.png`
  - 512x256, 8 columns x 4 rows, 64x64 frames.
  - Older generated temporary runtime preview. Built from the full-sheet extracts, then patched with strict right/up-right/back walk strips and mirrored temporary left-facing directions.

## Caveats

- Left-facing directions are temporary mirrors. Up-right/back-right and back movement now use stricter walk-strip art; up-left/back-left mirrors the up-right strip until bespoke left-facing art exists.
- The source images were JPEGs, so chroma/background extraction may still leave
  imperfect edge pixels around the character art.
- Rows are `idle`, `walk A`, `walk B`, and a third walk/contact row for patched directions, not the full v2 animation contract yet. Front/down movement still avoids the original tool-like fourth row.
- This is the current 64px visual target/preview, but not the final paper-doll layer architecture.

## Regenerate

Run from the repo root:

```powershell
& 'C:\Users\pharr\Downloads\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path 'C:\Users\pharr\code\karma' --script 'res://tools/extract_player_v2_64px_full_sheet.gd'
```
