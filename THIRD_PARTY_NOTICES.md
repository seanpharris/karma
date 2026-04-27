# Third-Party Notices

This project adapts ideas and small implementation patterns from MIT-licensed
open-source projects. Full imported assets or source files should keep their
original notices beside the copied material.

## Godot 2D Top-Down Template

- Source: `https://github.com/stesproject/godot-2d-topdown-template`
- License: MIT
- Copyright: Copyright (c) 2025 Stefano Mercadante (Godot 2D Top-Down Template)

Karma currently ports the direction-mapping idea from the template's
`Direction.gd` into C# as `scripts/Util/DirectionHelper.cs`, adapts the
template's lightweight inventory-overlay pattern into `scripts/UI/HudController.cs`,
adapts its acceleration/friction movement feel into
`scripts/Player/PlayerController.cs`, and follows its Godot-native pixel-art
setup for nearest-neighbor texture filtering, pixel snapping, and
`AnimatedSprite2D` character presentation.
The implementation is adapted for this project's Godot 4 .NET/C# architecture
and does not import the template's GDScript plugins, scenes, or assets.

The upstream license permits use, copy, modification, merge, publishing,
distribution, sublicensing, and sale, provided the copyright and permission
notice are included in substantial copies of the software.

## Procedural Generation Research

Karma's procedural-generation direction is informed by permissively licensed
Godot/C# procedural generation projects, documented in
`docs/reusable-procgen-research.md`.

Current researched sources include:

- `https://github.com/SirNeirda/godot_procedural_infinite_world` (MIT)
- `https://github.com/gaea-godot/gaea` (MIT)
- `https://github.com/gdquest-demos/godot-procedural-generation` (source code MIT;
  assets CC-BY 4.0)

No source files or assets from those repositories are currently imported. The
`ProceduralPlacementSampler` is a project-native C# implementation of a common
best-candidate / blue-noise-style placement pattern inspired by procedural
generation literature and demos.
