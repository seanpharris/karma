# Agent Notes

## Project

Karma is a Godot 4 .NET/C# top-down multiplayer prototype. It should feel visually simple like a 2D life sim, but the backend should stay server-authoritative and scalable.

Core ideas:

- Players Ascend or Descend through uncapped karma.
- The highest positive player is the Saint; the lowest negative player is the Scourge.
- Only one Saint and one Scourge should exist at a time.
- Death causes a Karma Break: the player respawns, but their karma path/status resets.
- The first game mode is a 30-minute match where the Saint and Scourge at match end both win.
- Scrip is spendable currency and is separate from karma.
- The game should support absurd, comic, helpful, harmful, PvP, trade, NPC, quest, and social-betrayal interactions.

## Branches

- Work primarily on `develop`.
- `main` is for stable merges.
- `add-art` exists for art contributions.
- Commit and push coherent slices when verification passes.

## Local Setup

- Workspace: `C:\Users\pharr\code\karma`
- Engine: Godot 4 .NET
- Known local Godot folder: `C:\Users\pharr\Downloads\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64`
- .NET 10 is acceptable for local development as long as the Godot C# project builds.

## Verification

Before finishing code changes, run the full verification chain. Run each step only if the previous exits `0`.

**From PowerShell:**
```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test.ps1; if ($LASTEXITCODE -eq 0) { powershell -ExecutionPolicy Bypass -File .\tools\snapshot.ps1 }; if ($LASTEXITCODE -eq 0) { powershell -ExecutionPolicy Bypass -File .\tools\check.ps1 } else { exit $LASTEXITCODE }
```

**From bash (e.g. Claude Code on Windows):**
```bash
powershell.exe -ExecutionPolicy Bypass -File tools/test.ps1 && \
powershell.exe -ExecutionPolicy Bypass -File tools/snapshot.ps1 && \
powershell.exe -ExecutionPolicy Bypass -File tools/check.ps1
```

Known note: `tools\check.ps1` may print Godot cleanup/leaked RID warnings. Treat them as non-failing when the command exits with code `0`.

## Multi-Agent Workflow

This repo may be edited by more than one agent or by the user between turns.
Assume uncommitted work may be valuable peer work, not disposable scratch.

At the start of a work turn:

- Run `git status --short --branch`.
- Inspect uncommitted changes with `git diff --stat` and targeted `git diff`.
- Read new untracked source files before editing nearby systems.
- If the existing work is coherent, continue from it instead of redoing it.
- If the existing work is large, run verification before making extra changes so
  failures can be attributed cleanly.
- If uncommitted changes are contradictory, incoherent, or leave the project
  non-building, stop and report the situation to the user rather than silently
  resolving it yourself.

When continuing another agent's work:

- Preserve their intent and code unless it is clearly broken.
- Make small follow-up edits that integrate, verify, or document the work.
- Do not revert, delete, rename, or overwrite another agent's files unless the
  user explicitly asks or the change is required to make the project build.
- Do not mix unrelated cleanup with another agent's feature slice.
- If generated debug files appear under `debug/`, ignore or gitignore them unless
  they are intentionally requested artifacts.

Before committing:

- Prefer one coherent commit per feature or integration slice.
- Include all source, docs, and real assets needed for that slice.
- Exclude local debug outputs, temporary logs, and exploratory images.
- Run the verification command above and only push when it passes.
- In the final note, mention whether the work was continued from existing
  uncommitted changes.

## Architecture Rules

- The server owns truth.
- Clients send intent; the server validates, mutates state, and emits snapshots/events.
- Keep gameplay mutations in the authoritative server/state path, not only in scene scripts.
- Use interest snapshots for scalable visibility instead of broadcasting everything to every client.
- Large-world target is `1000 x 1000` tiles, chunked. Prototype can stay `64 x 64`.
- Prototype starts at 4 players, but code should not hard-code that where a config belongs. The current stress target is 100 players per world.
- LLMs generate proposals, never live authoritative state. Parse, validate, then apply structured data through server-owned adapters.
- Template code can be used as inspiration, but port only narrow pieces that fit
  the Godot 4 .NET/C# server-authoritative architecture. Keep attribution in
  `THIRD_PARTY_NOTICES.md` when adapting third-party code.

Important areas:

- `scripts/Core/GameState.cs`: shared player, karma, wallet, inventory, quest, relationship, and event state.
- `scripts/Net/AuthoritativeWorldServer.cs`: authoritative intent handling, match timer, snapshots, server events.
- `scripts/Net/ServerIntent.cs`: network/intent/snapshot DTOs.
- `scripts/Net/NetworkProtocol.cs`: JSON-friendly protocol envelope.
- `scripts/World/`: generated world, tile rendering, server-rendered pickups/structures.
- `scripts/Art/`: prototype sprite/structure catalogs, native character sprites,
  atlas mappings, and procedural fallbacks.
- `scripts/UI/HudController.cs`: HUD plus lightweight prototype overlays such as
  the `I` inventory panel.
- `scripts/Util/DirectionHelper.cs`: cardinal direction helper adapted from a
  Godot 2D top-down template pattern for movement/facing/animation names.
- `scripts/Tests/GameplaySmokeTest.cs`: primary smoke/regression test.

## Art Paths

Current expected atlas paths:

- `assets/art/tilesets/scifi_station_atlas.png`
- `assets/art/character.png`
- `assets/art/sprites/scifi_engineer_player_sheet.png`
- `assets/art/sprites/scifi_item_atlas.png`
- `assets/art/sprites/scifi_utility_item_atlas.png`
- `assets/art/sprites/scifi_weapon_atlas.png`
- `assets/art/sprites/scifi_tool_atlas.png`
- `assets/art/structures/scifi_greenhouse_atlas.png`

Atlas rendering is opt-in per mapped source rectangle. If a region is unknown, keep the procedural/color fallback readable rather than guessing.

The generated engineer player sheet also keeps its chroma-key source at
`assets/art/sprites/scifi_engineer_player_sheet_chroma.png`. The runtime sheet
is the transparent PNG. Do not wire the chroma source into Godot scenes unless
debugging asset processing.

## Character Sprite Sheet Standard

Use this spec for generated or hand-authored playable character sheets so future
agents can map frames consistently.

- Runtime frame size: `32 x 32 px`.
- Sheet background: transparent PNG whenever possible. If generated by image
  tools that cannot output transparency, use a flat chroma-key background and
  remove it before wiring the asset into Godot.
- Pivot/origin: bottom center, aligned to the feet.
- Character footprint: keep the feet near the lower center of each frame.
- Visual character height: about `28-32 px`, leaving a little top padding.
- Initial direction set: `front`, `back`, `left`, `right`.
- Optional direction set: add `front-right`, `back-right`, `back-left`, and
  `front-left` after the four cardinal directions are working.
- Walk cycle: `4` frames per direction.
- Idle: `1` frame per direction.
- Export filter: nearest-neighbor, no smoothing, no shadows baked into the
  transparent background.
- Avoid labels, UI text, grid lines, rulers, metadata panels, and decorative
  presentation framing in runtime sheets. Reference sheets can include those,
  but runtime atlases should be clean frame grids only.

Suggested runtime layout for a compact four-direction sheet:

```text
row 0: idle_front, idle_back, idle_left, idle_right
row 1: walk_front_1, walk_front_2, walk_front_3, walk_front_4
row 2: walk_back_1, walk_back_2, walk_back_3, walk_back_4
row 3: walk_left_1, walk_left_2, walk_left_3, walk_left_4
row 4: walk_right_1, walk_right_2, walk_right_3, walk_right_4
```

Image generation prompt template:

```text
Create a clean 2D pixel-art top-down RPG character sprite sheet for Karma.
Subject: <role/personality/outfit>, sci-fi frontier colony style.
Runtime frames: each frame is exactly 32x32 px, nearest-neighbor pixel art.
Layout: four cardinal directions only. Row 0 has idle_front, idle_back,
idle_left, idle_right. Rows 1-4 have four-frame walk cycles for front, back,
left, and right.
Background: transparent if possible; otherwise perfectly flat #00ff00
chroma-key with no shadows, no gradients, no texture, and no green in the
character.
Constraints: clean frame grid only, no labels, no metadata, no UI panels, no
watermark, no presentation sheet, no text. Keep feet bottom-centered in every
frame. Keep proportions consistent across frames.
```

## Godot-Native Art Workflow

Karma is moving toward the Godot-native art workflow used by the referenced
Godot 2D top-down template:

- Project settings should keep pixel art crisp:
  `rendering/textures/canvas_textures/default_texture_filter=0`,
  `rendering/2d/snap/snap_2d_transforms_to_pixel=true`, no generated mipmaps,
  and `process/fix_alpha_border=true` for texture imports.
- Humanoid actors should use `PrototypeCharacterSprite`, which builds a
  Godot `AnimatedSprite2D` with `SpriteFrames` from cataloged atlas regions.
- Mapped props/items should use `PrototypeAtlasSprite`, which builds a Godot
  `Sprite2D` with an `AtlasTexture` from the same catalog data.
- Mapped structures should use `StructureSprite`, which now also builds a
  native `Sprite2D`/`AtlasTexture` from `StructureArtCatalog`.
- Shared atlas math lives in `AtlasFrame`/`AtlasFrames`. Use it for source
  rectangles, display scale, and anchoring instead of duplicating renderer math.
- `ArtAssetManifest` discovers every cataloged atlas path and verifies the
  files exist. Add newly mapped runtime sheets to catalogs so the manifest and
  smoke tests catch missing assets.
- Keep `PrototypeSprite` for temporary procedural fallbacks when an atlas region
  is missing.
- Runtime character sheets should eventually use clean `32 x 32` frame grids.
  When a proper grid exists, map it with
  `PrototypeSpriteCatalog.FourDirectionGridAnimations(origin, frameSize: 32)`.
  The expected animation names are `idle-down`, `idle-up`, `idle-left`,
  `idle-right`, `walk-down`, `walk-up`, `walk-left`, and `walk-right`.
  Walk rows default to four frames each.
- The generated 8-direction template/reference sheet maps to
  `CharacterSheetLayout.EightDirectionTemplate(origin)` once it is exported as a
  clean runtime PNG with only frames: 8 columns x 9 rows, `32 x 32` frames,
  `256 x 288` total. Do not catalog the annotated prompt/reference PNG directly.
- Stable world tiles should eventually become Godot `TileSet` resources with
  atlas sources, terrain rules, collision, and animated tile data. Until then,
  the server-friendly generated tile renderer can keep drawing cataloged atlas
  regions chunk by chunk.
- Store source/reference sheets separately from runtime sheets. Runtime sheets
  should be clean transparent PNGs with no labels, presentation framing, or
  metadata panels.

## Coding Style

- Prefer existing patterns over new abstractions.
- Keep edits scoped to the requested gameplay slice.
- Use server-owned DTOs/snapshots for anything that will matter in multiplayer.
- Add focused smoke tests for new mechanics, especially server intent validation
  and snapshot data. Add cases to `scripts/Tests/GameplaySmokeTest.cs`. Every
  new server intent should have at least one smoke test case.
- Do not revert user changes.
- Avoid unrelated refactors.

## 20-Step Gameplay Plan

Active implementation plan (as of 2026-04-29). Steps complete on `develop`.

| # | Feature | Status |
|---|---------|--------|
| 1 | Multi-step quest server state (step conditions, per-step karma/scrip) | ✅ done |
| 2 | Repair mission quest (locate fixture → get tool → repair) | ✅ done |
| 3 | Delivery quest (collect item at source → bring to destination) | ✅ done |
| 4 | Rumor quest (discover secret → expose or bury → consequence) | ✅ done |
| 5 | Paragon Favor perk (+50 karma threshold) | ✅ done |
| 6 | Abyssal Mark perk (-100 karma threshold) | ✅ done |
| 7 | Posse formation (InvitePosse/AcceptPosse/LeavePosse intents) | ✅ done |
| 8 | Posse HUD panel (member list, karma, health) | ✅ done |
| 9 | Saint/Scourge NPC behavior (greetings, prices, reactions) | ✅ done |
| 10 | Chat tabs — Local / Posse / System | ✅ done |
| 11 | Interior audibility filtering | ✅ done |
| 12 | Combat heat tracking (tile-chunk heat map with decay) | ✅ done |
| 13 | Smarter respawn placement (avoid heat, prefer stabilized stations) | ✅ done |
| 14 | Downed state (0 HP countdown, can still chat) | pending |
| 15 | Rescue intent (rescuer carries downed player, Ascend reward) | pending |
| 16 | Clinic recovery hook (extend countdown, NPC auto-revive for scrip) | pending |
| 17 | Road/path generation (spanning path graph at world-gen) | pending |
| 18 | Path-aware world rendering (road tiles between station pairs) | pending |
| 19 | Mount/vehicle entity model (speed modifier, parking, occupancy) | pending |
| 20 | Mount/dismount intents + karma hooks | pending |

Art requirements for each step are tracked in `ART_NEEDED.md`.

## Quest System Overview

Multi-step quests are implemented via `QuestStep`/`QuestStepCondition` in
`scripts/Data/QuestModels.cs`. Step condition kinds:

- `None` — always satisfied
- `HoldItem(targetId)` — player must have the item in inventory
- `HoldRepairTool` — player must hold MultiTool or WeldingTorch
- `NearNpc(npcId)` — player within interest radius of the named NPC
- `NearStructureCategory(role)` — any structure with matching `Category`
  within interest radius

`CompleteQuest` intent is rejected if the quest is multi-step and not
all steps are finished (`AllStepsDone == false`).

### Plugin-Modular Quest Modules

Quest types live in `scripts/Quests/` as `QuestModule` subclasses. Each module is
self-contained: it owns quest creation (factory) and completion resolution (karma).
Add a new quest type by subclassing `QuestModule`, declaring `StationRoles`, and
registering in `QuestModuleRegistry` — the world generator and server pick it up
automatically.

Registered modules:
| Module | Station roles | Completion prefix | Karma (expose/resolve) |
|--------|--------------|-------------------|------------------------|
| `RepairMissionModule` | `workshop`, `clinic` | `generated_station_help:` | default action lookup |
| `DeliveryQuestModule` | `market` | `generated_station_help:` | default action lookup |
| `RumorQuestModule` | `notice-board` | `rumor_resolve:` | expose=+5, bury=+8 |

Stations not matched by any module get a flat `QuestDefinition` (stabilize fallback).

`QuestCreationContext` carries: `QuestId`, `LocationId`, `LocationName`, `LocationRole`,
`GiverNpcId`, `ScripReward`, `OtherPlacements` (list of `QuestPlacementInfo`).

## Current Prototype Features

_Last updated 2026-04-29. This list drifts — verify against the code before
assuming a feature is or isn't present._

- Top-down local movement.
- Mouse wheel camera zoom with clamps.
- Left Shift sprint with stamina.
- `I` toggles an inventory overlay with scrip, equipment, and grouped items.
- Server-owned 30-minute match timer and Saint/Scourge winner lock.
- Uncapped karma ranks in both Ascension and Descension.
- Scrip currency, player transfers, shop offers, and server-side pricing perks.
- NPC dialogue and quest choices.
- Multi-step quests with `AdvanceQuestStep` intent and per-step karma/scrip rewards.
- Repair mission quests (3-step: locate fixture → equip tool → repair).
- Delivery quests (3-step: go to source → hold item → deliver to destination).
- Rumor quests (2-step: read notice-board → find subject → expose or bury choice with karma consequence).
- Generated station quests seeded by world generator based on station role.
- Plugin-modular quest system: `scripts/Quests/QuestModule.cs` + `QuestModuleRegistry`. New quest types self-register by subclassing `QuestModule`.
- PvP, duels, attacks, armor, weapons, and Karma Break death drops.
- Server-owned world items and structures rendered from interest snapshots.
- Greenhouse structure set with basic interaction prompts/events.
- Sci-fi item, weapon, tool, utility, tile, and greenhouse atlases placed in the expected paths.
