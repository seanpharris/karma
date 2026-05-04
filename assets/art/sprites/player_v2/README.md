# Player v2 layered prototype

This folder is the first runtime-visible step toward the intended character art
architecture: build one canonical base body, then add skin, hair, outfit, and
held-tool layers instead of generating bespoke art for every character variant.

Important: the active runtime target is now the original native `32x64` player
contract. The older `32x32` layered mannequin sheets remain compatibility and
architecture-reference assets only, not the art style to polish.

## Active preview

- `player_model_32x64_manifest.json`
  - Active runtime layer manifest. It declares rectangular `32x64` frames, 8 direction columns, 4 animation rows, layer slots, available layer files, and the default preview stack.
  - `scripts/Art/PlayerV2LayerManifest.cs` loads this manifest by default, builds default or custom slot selections, composites selected layers into an image, and exports deterministic cached composite PNGs for selected appearances.
  - Gameplay routes `SetAppearance` intents through the authoritative server; the Escape menu Appearance panel and `V`/`B`/`N` debug shortcuts now cycle native `32x64` skin, hair, and outfit layers.
- `player_model_32x64_layered_preview.png`
  - 256x256, 8 columns x 4 rows, 32x64 frames.
  - Pixel-perfect recomposite of the canonical 32x64 model's default layer stack.
- `player_v2_manifest.json` and `player_v2_layered_preview_8dir.png`
  - Legacy 32x32 mannequin/compositor assets. Keep them as fallback/reference only.

## Layers

Legacy layer files live in `layers/`. These fallback compositor layers share the exact same 8-direction/9-row, 32x32 grid:

- `base_body_8dir.png` — neutral mannequin/body-guide silhouette.
- `skin_light_8dir.png`, `skin_medium_8dir.png`, `skin_deep_8dir.png` — replaceable skin layers.
- `hair_short_dark_8dir.png`, `hair_short_blond_8dir.png` — replaceable hair layers.
- `outfit_engineer_8dir.png`, `outfit_settler_8dir.png` — replaceable clothing/equipment layers.
- `tool_multitool_8dir.png` — held-tool overlay for tool/interact rows.

Additional base-model references:

- `base_model_32x64_8dir.png`
  - 256x64, 8 columns x 1 row, 32x64 cells.
  - Direction order: front/down, front-right, right, back-right, back, back-left, left, front-left.
  - Rough neutral body/proportion layer for testing a taller paper-doll base model; not wired into the current 32x32 compositor manifest yet.
- `player_model_32x64_8dir.png`
  - 256x64, 8 columns x 1 row, 32x64 cells.
  - First built-out single-model attempt using the 32x64 dimensions: skin/head, work outfit, backpack/armor cues, and transparent background.
  - Intended as a focused standardization target.
- `player_model_32x64_8dir_4row.png`
  - 256x256, 8 columns x 4 rows, 32x64 cells.
  - Current canonical 32x64 skeleton contract and active default runtime player sheet: row 1 idle/facing, rows 2-4 walk stepping.
  - Same runtime direction order: front/down, front-right, right, back-right, back, back-left, left, front-left.
- `player_model_32x64_8dir_runtime.png`
  - 512x256, 8 columns x 4 rows, 64x64 runtime cells.
  - Centers the 32x64 animated contract in square runtime cells as a compatibility/debug preview, but the prototype now prefers the real rectangular 32x64 contract when present.
- `player_model_32x64_manifest.json`
  - Starter manifest for the native 32x64 paper-doll split.
  - Uses rectangular metadata (`frameWidth: 32`, `frameHeight: 64`) and the same 8-direction/4-row contract.
- `layers_32x64/*.png`
  - Active split of the canonical skeleton into base, skin, hair, outfit, boots, backpack, held-tool, and weapon overlay layers.
  - `player_model_32x64_layered_preview.png` is a pixel-perfect recomposite of the current default layer stack so we can iterate layers without changing runtime selection accidentally.
  - Current test variants include light/medium/deep skin, dark/blond/copper/white short hair, engineer/settler/medic/ranger outfits, cleaned utility boots, and black boots. Backpack/tool/weapon overlays are review-ready optional layers for future loadout/action states. Old 32x32 IDs are accepted only as migration/fallback cycle inputs.
  - `boots_utility_32x64.png` came from the manually generated `player_boots_layer.png`, then had baked gray contact/shadow blocks removed and bright sole pixels toned down. It is prototype-ready, but still flagged for hand touch-up: a few tiny bright sole/toe pixels and 1px walk-frame jitter may need cleanup.
  - `boots_black_32x64.png` is a dark recolor derived from the touched-up utility boots. It reads well on the character, though boots-only previews need a light/checker background because the pixels are intentionally dark.
- `imported/`
  - Review-only folder for normalized PixelLab/downloaded candidates. Runtime should not load imported candidates until one is explicitly curated and promoted.

The layer order is:

1. base body
2. skin
3. hair
4. outfit
5. boots
6. backpack overlay
7. held tool overlay
8. weapon overlay

## Regenerate

Run from the repo root:

```powershell
& 'C:\Users\pharr\Downloads\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path 'C:\Users\pharr\code\karma' --script 'res://tools/generate_layered_player_v2.gd'

# Optional taller base-model reference:
& 'C:\Users\pharr\Downloads\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path 'C:\Users\pharr\code\karma' --script 'res://tools/generate_base_model_32x64_8dir.gd'

# Optional built-out 32x64 player model attempt:
& 'C:\Users\pharr\Downloads\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path 'C:\Users\pharr\code\karma' --script 'res://tools/generate_player_model_32x64_8dir.gd'

# Split the canonical 32x64 model into starter paper-doll layers:
python tools/generate_player_model_32x64_layers.py

# Render static review sheets + an HTML viewer for human/agent sprite review:
python tools/render_sprite_viewer.py

# Optional PixelLab MCP download import/normalization:
python tools/import_pixellab_character.py path\to\pixellab-download.png --output-dir assets\art\sprites\player_v2\imported --output-stem pixellab_engineer_v1
```

The sprite viewer writes `assets/art/sprites/player_v2/review/index.html`, `player_v2_variant_matrix.png`, and `player_v2_layer_contact_sheet.png`. Use it whenever layer variants change so Sean and the assistant can review the same preview artifacts.

See [TASKS.md#pixellab-mcp-workflow](../../../../TASKS.md#pixellab-mcp-workflow) for the PixelLab MCP candidate-generation workflow and token-safety notes.

## Next steps

- Use `player_model_32x64_8dir_4row.png` as the focused skeleton contract for
  the next original player-art pass.
- Split the 32x64 skeleton into matching paper-doll layers: base body, skin/tint,
  hair, outfit, backpack/tool/weapon overlays.
- Replace the current cycle-only Appearance panel with a fuller picker/dropdown UI
  once there are enough 32x64 layers to browse.
- Broaden per-snapshot rendering beyond the local player and prototype peer to
  dynamically spawned player avatars once multiplayer stand-ins are expanded.
