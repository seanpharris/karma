# Player V2 Next 10 Plan

This is the active follow-through plan for the original `32x64` player model, PixelLab intake, and the reusable paper-doll player pipeline.

## Goal

Turn the current readable `32x64` skeleton into Karma's real reusable player-art pipeline:

- native `32x64` animation contract,
- swappable paper-doll layers,
- safe PixelLab candidate import/review,
- runtime/server-owned appearance selection,
- then return to gameplay systems once the character pipeline is stable.

## Task checklist

### 1. Wire the native `32x64` layered manifest into runtime

Status: done

- Load `assets/art/sprites/player_v2/player_model_32x64_manifest.json` through the existing player-v2 layer manifest/compositor path.
- Support rectangular `frameWidth`/`frameHeight` metadata while preserving old square `frameSize` manifests.
- Make the native `32x64` layered preview/composite usable as a runtime atlas.

### 2. Make appearance selection use the `32x64` layers

Status: done

- Route skin/hair/outfit choices to `layers_32x64` variants.
- Keep `SetAppearance` server-owned.
- Preserve snapshot-driven local/peer rendering.

### 3. Retire the old `32x32` mannequin path as fallback only

Status: done

- Keep legacy `player_v2_manifest.json` and `player_v2_layered_preview_8dir.png` as compatibility/fallback.
- Ensure the active default path is the native `32x64` model/layer stack.
- Update docs/tests so future work does not accidentally polish the old mannequin.

Implemented in this slice: `PlayerV2LayerManifest.DefaultManifestPath` now points at the native `32x64` manifest, the loader supports `frameWidth`/`frameHeight` while preserving legacy square `frameSize`, and smoke tests assert the legacy manifest remains fallback-only.

### 4. Add a PixelLab import review folder/process

Status: done

- Standardize `assets/art/sprites/player_v2/imported/` for PixelLab-normalized outputs.
- Document naming and review expectations.
- Keep imported candidates out of active runtime until reviewed.

Implemented in this slice: `assets/art/sprites/player_v2/imported/README.md` defines the candidate review folder rules and safe import command.

### 5. Import the first PixelLab candidate when available

Status: blocked until PixelLab output exists

- Use `tools/import_pixellab_character.py` on a downloaded PixelLab PNG/ZIP.
- Normalize to the `32x64` 8-direction 4-row contract.
- Compare against the skeleton for direction order, baseline, scale, and no-tool walking.

### 6. Improve the `32x64` skeleton art pass

Status: done

- Improve proportions, hands/feet, diagonal silhouettes, and frame-to-frame consistency.
- Preserve the contract: `8 columns x 4 rows`, `32x64` cells.
- Avoid breaking current runtime readability.

Implemented in this slice: added a small waist/gear cue across directions, regenerated the one-row, 4-row, and compatibility runtime sheets, then regenerated native layer splits with pixel-perfect default recomposition.

### 7. Add tool/backpack/weapon overlay layers

Status: done

- Keep ordinary idle/walk tool-free.
- Add overlays for backpack, held tool, and future weapon/tool states.
- Prepare for later action rows without contaminating movement frames.

Implemented in this slice: added optional `backpack_daypack_32x64`, `tool_multitool_32x64`, and `weapon_practice_baton_32x64` layers to the native manifest. They are omitted from the default preview stack and can be composed explicitly for future loadout/action states.

### 8. Expand the appearance menu

Status: pending

- Show current skin/hair/outfit names clearly.
- Add room for preview thumbnails or selectors once variants grow.
- Keep non-pausing Escape menu behavior.

### 9. Apply appearance rendering to more player avatars

Status: pending

- Broaden snapshot-driven appearance rendering beyond local player and prototype peer.
- Ensure dynamically spawned/multiplayer stand-ins resolve selected layer stacks.

### 10. Return to gameplay systems after the character pipeline stabilizes

Status: pending

Recommended next gameplay slice after tasks 1-9:

- local chat polish and/or fake proximity audio falloff, or
- downed/rescue/carry/execute/clinic loop.

## Verification expectations

For each implemented slice:

- Check `git status --short --branch` before editing.
- Run the smallest meaningful verification, usually:
  - Windows `dotnet build Karma.csproj`, and
  - Godot headless smoke test: `res://scenes/TestHarness.tscn`.
- Commit and push verified chunks to `develop`.
- Use Windows PowerShell Git credentials for push from WSL.

## Current recommendation

Do tasks 1-3 first. They turn the current skeleton from a static art experiment into the active reusable runtime character pipeline.
