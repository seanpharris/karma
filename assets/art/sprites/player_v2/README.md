# Player v2 layered prototype

This folder is the first runtime-visible step toward the intended character art
architecture: build one canonical base body, then add skin, hair, outfit, and
held-tool layers instead of generating bespoke art for every character variant.

## Active preview

- `player_v2_layered_preview_8dir.png`
  - 256x288, 8 columns x 9 rows, 32x32 frames.
  - This is a generated composite of the layer files below and is currently the
    preferred player runtime sheet when present.
  - It deliberately matches the existing prototype sheet contract so it can be
    viewed in-game immediately without a larger renderer rewrite.

## Layers

Layer files live in `layers/` and share the exact same 8-direction/9-row grid:

- `base_body_8dir.png` — neutral mannequin/body-guide silhouette.
- `skin_medium_8dir.png` — replaceable skin layer.
- `hair_short_dark_8dir.png` — replaceable hair layer.
- `outfit_engineer_8dir.png` — replaceable clothing/equipment layer.
- `tool_multitool_8dir.png` — held-tool overlay for tool/interact rows.

The layer order is:

1. base body
2. skin
3. hair
4. outfit
5. held tool

## Regenerate

Run from the repo root:

```powershell
& 'C:\Users\pharr\Downloads\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path 'C:\Users\pharr\code\karma' --script 'res://tools/generate_layered_player_v2.gd'
```

## Next steps

- Replace the generated mannequin pixels with polished true base-body art.
- Add alternate skin palettes, hair, outfits, and silhouettes.
- Move composition from this generator into a reusable export/compositor step.
- Later upgrade to 48x48 or 64x64 frames once the runtime/compositor contract is
  ready.
