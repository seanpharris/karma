# Player v2 layered prototype

This folder is the first runtime-visible step toward the intended character art
architecture: build one canonical base body, then add skin, hair, outfit, and
held-tool layers instead of generating bespoke art for every character variant.

## Active preview

- `player_v2_manifest.json`
  - Declares the current frame contract, directions, animation rows, layer slots,
    available layer files, and the default preview stack.
  - `scripts/Art/PlayerV2LayerManifest.cs` can load this manifest, build default
    or custom slot selections, composite selected layers into an image, and export
    deterministic cached composite PNGs for selected appearances.
  - Gameplay can route `SetAppearance` intents through the authoritative server;
    the current prototype maps `V`/`B`/`N` to cycle the local player's skin,
    hair, and outfit layers for quick runtime inspection.
  - This is the bridge from a hardcoded generated preview toward real character
    customization/composition.
- `player_v2_layered_preview_8dir.png`
  - 256x288, 8 columns x 9 rows, 32x32 frames.
  - This is a generated composite of the default manifest stack and is currently
    the preferred player runtime sheet when present.
  - It deliberately matches the existing prototype sheet contract so it can be
    viewed in-game immediately without a larger renderer rewrite.

## Layers

Layer files live in `layers/` and share the exact same 8-direction/9-row grid:

- `base_body_8dir.png` — neutral mannequin/body-guide silhouette.
- `skin_light_8dir.png`, `skin_medium_8dir.png`, `skin_deep_8dir.png` — replaceable skin layers.
- `hair_short_dark_8dir.png`, `hair_short_blond_8dir.png` — replaceable hair layers.
- `outfit_engineer_8dir.png`, `outfit_settler_8dir.png` — replaceable clothing/equipment layers.
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
- Add more alternate palettes, hair, outfits, tools, and silhouettes.
- Add a real appearance selection UI instead of the current `V/B/N` prototype
  skin/hair/outfit cycle shortcuts.
- Broaden per-snapshot rendering beyond the local player and prototype peer to
  dynamically spawned player avatars once multiplayer stand-ins are expanded.
- Later upgrade to 48x48 or 64x64 frames once the runtime/compositor contract is
  ready.
