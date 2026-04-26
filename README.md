# Karma

Karma is a multiplayer 2D life-sim RPG prototype with cozy visuals, absurd objects,
PvP, procedural worlds, and a central Ascension/Descension score.

Players start a generated world, meet generated NPCs, help or harm people, prank,
trade, fight, betray, protect, and compete for the highest or lowest karma on the
server. Death causes a **Karma Break**: the player respawns, but their karma path
and perks reset.

## Direction

- Engine: Godot 4 with C#
- Initial player count: 4 per world, configurable toward larger worlds
- Authority: server-owned world state, client sends intent
- Karma is uncapped in both directions
- Positive path: Ascension
- Negative path: Descension
- High leaderboard title: Saint
- Low leaderboard title: Scourge

## First Prototype Goals

- Top-down 2D movement with left Shift sprinting
- A tiny generated town map
- NPC interaction choices that Ascend or Descend the player
- Weird interactible objects such as whoopie cushions and deflated balloons
- Karma tiers and Karma Break reset behavior
- Code structured so an authoritative server can own game state later

## Project Layout

- `docs/` design notes
- `scenes/` Godot scenes
- `scripts/Core/` shared game concepts and state
- `scripts/Data/` item, NPC, and karma models
- `scripts/World/` world generation and interactibles
- `scripts/Player/` player controller
- `scripts/Npc/` NPC interaction scripts
- `scripts/Net/` multiplayer/server boundary notes and stubs
