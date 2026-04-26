# Reusable Procedural Generation Research

Karma should reuse permissively licensed ideas where they fit, while keeping the
runtime generator server-owned, deterministic, and native to the Godot .NET/C#
prototype.

## Current candidates

### SirNeirda/godot_procedural_infinite_world

- URL: <https://github.com/SirNeirda/godot_procedural_infinite_world>
- License: MIT
- Fit: Godot 4 C# procedural world example.
- Useful ideas: deterministic object spreading, environment/weather/day-night
  knobs, chunk-friendly world management.
- Current use: research/reference only; no source files or assets imported.

### gaea-godot/gaea

- URL: <https://github.com/gaea-godot/gaea>
- License: MIT
- Fit: Godot 4 procedural generation addon.
- Useful ideas: graph/node-based generation architecture and renderer separation.
- Current use: research/reference only; no source files or assets imported.

### gdquest-demos/godot-procedural-generation

- URL: <https://github.com/gdquest-demos/godot-procedural-generation>
- License: source code MIT; assets CC-BY 4.0.
- Fit: educational procedural generation algorithms for Godot.
- Useful ideas: random walkers, cellular automata, world maps, blue-noise style
  distribution, chunked/infinite placement demos.
- Current use: algorithm inspiration only; no source files or assets imported.

### LayerProcGen

- URL: <https://github.com/runevision/LayerProcGen>
- License: MPL-2.0.
- Fit: deterministic contextual layered C# generation.
- Useful ideas: layered dependency model and cross-chunk context.
- Current use: ideas only. Avoid direct import unless MPL obligations are
  intentionally accepted.

## First reuse direction

The first adapted idea is best-candidate / blue-noise-style placement. Karma uses
`ProceduralPlacementSampler` as a small project-native C# sampler for naturally
spaced social stations, future oddities, structures, NPC spawn hubs, and respawn
candidate pools.

This keeps the good part of reusable procedural generation — separated placement
that feels less random and less clumpy — without importing a full third-party
framework.
