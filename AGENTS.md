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
- `assets/art/character.png`
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
