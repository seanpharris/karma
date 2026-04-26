# Current Sprite Modeling Status

The current prototype sprite work is mostly a **runtime/animation pipeline change**,
not a final visual-art upgrade.

## What changed in-game

- The player sprite now loads from `assets/art/sprites/scifi_engineer_player_8dir.png`
  when that file exists.
- The runtime supports named 8-direction idle/walk animations.
- Movement can select diagonal animation names instead of only cardinal directions.
- The art loader can read the PNG alpha directly so stale imported texture data does
  not leave green/chroma artifacts.

## Why the visual difference may be subtle

The active sheet is still a prototype/extracted engineer sheet. It is useful for
wiring the runtime contract, but it is **not** a polished new character model.

Important limitations:

- The source art did not provide fully unique, professional diagonal/body poses.
- Some diagonal directions are still close to side/cardinal poses, so the rotation
  difference is easy to miss during play.
- The current frame size is only `32x32`, which limits visible outfit/body detail.
- The bigger professional plan is a v2 paper-doll/layer system, likely `48x48` or
  `64x64`, with base body + clothing/hair/item layers and cleaner animation groups.

## Practical expectation

For now, the current player sheet should be judged as:

- good enough to test movement animation wiring;
- good enough to validate transparent runtime loading;
- not good enough to represent final character quality or final customization.

If the goal is a visible art improvement, the next art slice should create or source
a real v2 base body and one outfit layer rather than keep polishing the temporary
`32x32` prototype engineer sheet.
