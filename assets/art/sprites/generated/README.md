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
  - Current 256x64 direction-row extracts from the regenerated full-sheet chroma prompt.
- `player_v2_engineer_8dir_4row_candidate.png`
  - 512x256, 8 columns x 4 rows, 64x64 frames.
  - Current temporary runtime preview. Built from the full-sheet extracts, with mirrored temporary left-facing directions.

## Caveats

- Left-facing directions and back diagonals are temporary mirrored/placeholder art. Runtime movement deliberately reuses back-facing frames for up-left/up-right until true back-diagonal poses exist.
- The source images were JPEGs, so chroma/background extraction may still leave
  imperfect edge pixels around the character art.
- Rows are `idle`, `walk A`, `walk B`, `tool-ready`, not the full v2 animation
  contract yet. Normal movement samples only `idle`, `walk A`, and `walk B` so the tool row does not pop during walking.
- This is the current 64px visual target/preview, but not the final paper-doll layer architecture.

## Regenerate

Run from the repo root:

```powershell
& 'C:\Users\pharr\Downloads\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path 'C:\Users\pharr\code\karma' --script 'res://tools/extract_player_v2_64px_full_sheet.gd'
```
