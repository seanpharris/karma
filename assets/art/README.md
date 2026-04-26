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

That path is registered in `scripts/World/ThemeArtRegistry.cs`. The prototype
currently maps a small sci-fi subset from that sheet: floors, metal walls,
airlock doors, duel ring floor, and oddity pile.

Atlas rendering is intentionally opt-in per tile id. Set `HasAtlasRegion` only
after the exact source rectangle for a tile/prop is mapped from the sheet; any
unmapped future ids should keep placeholder colors so the prototype stays
readable.

The active prototype actors and pickups use procedural pixel-style models in
`scripts/Art/PrototypeSpriteModels.cs`. These are deliberately simple Godot draw
layers, not final art, but they give every playable object a recognizable visual
until sprite sheets are ready.
