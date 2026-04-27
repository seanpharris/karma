# Generated Sprite Candidates

These PNGs are extracted/normalized from Gemini reference batches. They are
transparent, exact-size candidate sheets, but they are still **prototype
candidates**, not final v2 runtime art. The extractor removes chroma and dark
edge-connected source backgrounds so the 64px runtime preview can be judged
in-game without opaque background blocks.

## Files

- `player_v2_front_pose_extract.png`
  - 64x256, one direction column x four rows.
- `player_v2_right_pose_extract.png`
  - 64x256, one direction column x four rows.
  - Strongest source batch so far.
- `player_v2_back_pose_extract.png`
  - 64x256, one direction column x four rows.
- `player_v2_engineer_8dir_4row_candidate.png`
  - 512x256, 8 columns x 4 rows, 64x64 frames.
  - Composite preview using front/right/back extracts, mirrored right for left,
    and placeholder duplicated frames for diagonals/back diagonals.

## Caveats

- Diagonals are temporary placeholders, not true diagonal art.
- The source images were JPEGs, so chroma/background extraction may still leave
  imperfect edge pixels around the character art.
- Rows are `idle`, `walk A`, `walk B`, `sprint/ready`, not the full v2 animation
  contract yet.
- This should not replace `assets/art/sprites/scifi_engineer_player_8dir.png`
  until the runtime supports the v2 64x64/4-row candidate layout deliberately.

## Regenerate

Run from the repo root:

```powershell
& 'C:\Users\pharr\Downloads\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path 'C:\Users\pharr\code\karma' --script 'res://tools/extract_gemini_pose_batches.gd'
```
