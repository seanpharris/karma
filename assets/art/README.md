# Art Drop Folders

Place source art here while we build the asset pipeline.

- `tilesets/`: terrain, floors, walls, doors, structures, and theme atlases.
- `props/`: objects, interactibles, terminals, furniture, oddities, and pickups.
- `sprites/`: player, NPC, outfit, armor, weapon, and animation sheets.

Prefer descriptive lowercase names such as `scifi_station_atlas.png` or
`western_props_32px.png`. Keep original files intact where practical; we can
create cropped or Godot-specific resources from them later.

The current code expects the first sci-fi tileset candidate at:

`tilesets/scifi_station_atlas.png`

That path is registered in `scripts/World/ThemeArtRegistry.cs`. For now the
game still renders placeholder colors, but the registry already stores atlas
coordinates so we can switch logical tile ids to real art without changing
world generation.

Atlas rendering is intentionally opt-in per tile id. Set `HasAtlasRegion` only
after the exact source rectangle for a tile/prop is mapped from the sheet; until
then the game uses placeholder colors so the prototype stays readable.
