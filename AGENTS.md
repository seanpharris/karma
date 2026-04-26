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

Before finishing code changes, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test.ps1; if ($LASTEXITCODE -eq 0) { powershell -ExecutionPolicy Bypass -File .\tools\snapshot.ps1 }; if ($LASTEXITCODE -eq 0) { powershell -ExecutionPolicy Bypass -File .\tools\check.ps1 } else { exit $LASTEXITCODE }
```

Known note: `tools\check.ps1` may print Godot cleanup/leaked RID warnings. Treat them as non-failing when the command exits with code `0`.

## Architecture Rules

- The server owns truth.
- Clients send intent; the server validates, mutates state, and emits snapshots/events.
- Keep gameplay mutations in the authoritative server/state path, not only in scene scripts.
- Use interest snapshots for scalable visibility instead of broadcasting everything to every client.
- Large-world target is `1000 x 1000` tiles, chunked. Prototype can stay `64 x 64`.
- Prototype starts at 4 players, but code should not hard-code that where a config belongs. The current stress target is 100 players per world.
- LLMs generate proposals, never live authoritative state. Parse, validate, then apply structured data through server-owned adapters.

Important areas:

- `scripts/Core/GameState.cs`: shared player, karma, wallet, inventory, quest, relationship, and event state.
- `scripts/Net/AuthoritativeWorldServer.cs`: authoritative intent handling, match timer, snapshots, server events.
- `scripts/Net/ServerIntent.cs`: network/intent/snapshot DTOs.
- `scripts/Net/NetworkProtocol.cs`: JSON-friendly protocol envelope.
- `scripts/World/`: generated world, tile rendering, server-rendered pickups/structures.
- `scripts/Art/`: prototype sprite/structure catalogs and procedural fallbacks.
- `scripts/Tests/GameplaySmokeTest.cs`: primary smoke/regression test.

## Art Paths

Current expected atlas paths:

- `assets/art/tilesets/scifi_station_atlas.png`
- `assets/art/sprites/scifi_character_atlas.png`
- `assets/art/sprites/scifi_item_atlas.png`
- `assets/art/sprites/scifi_utility_item_atlas.png`
- `assets/art/sprites/scifi_weapon_atlas.png`
- `assets/art/sprites/scifi_tool_atlas.png`
- `assets/art/structures/scifi_greenhouse_atlas.png`

Atlas rendering is opt-in per mapped source rectangle. If a region is unknown, keep the procedural/color fallback readable rather than guessing.

## Coding Style

- Prefer existing patterns over new abstractions.
- Keep edits scoped to the requested gameplay slice.
- Use server-owned DTOs/snapshots for anything that will matter in multiplayer.
- Add focused smoke tests for new mechanics, especially server intent validation and snapshot data.
- Do not revert user changes.
- Avoid unrelated refactors.

## Current Prototype Features

- Top-down local movement.
- Mouse wheel camera zoom with clamps.
- Left Shift sprint with stamina.
- Server-owned 30-minute match timer and Saint/Scourge winner lock.
- Uncapped karma ranks in both Ascension and Descension.
- Scrip currency, player transfers, shop offers, and server-side pricing perks.
- NPC dialogue and quest choices.
- PvP, duels, attacks, armor, weapons, and Karma Break death drops.
- Server-owned world items and structures rendered from interest snapshots.
- Greenhouse structure set with basic interaction prompts/events.
- Sci-fi item, weapon, tool, utility, tile, and greenhouse atlases placed in the expected paths.
