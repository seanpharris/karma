# Art Drop Folders

Place source art here while we build the asset pipeline.

- `tilesets/`: terrain, floors, walls, doors, structures, and theme atlases.
- `props/`: objects, interactibles, terminals, furniture, oddities, and pickups.
- `structures/`: large world models such as buildings, domes, bases, and modules.
- `sprites/`: player, NPC, outfit, armor, weapon, and animation sheets.

Prefer descriptive lowercase names such as `scifi_station_atlas.png` or
`western_props_32px.png`. Keep original files intact where practical; we can
create cropped or Godot-specific resources from them later.

The current code expects the first sci-fi tileset candidate at:

`tilesets/scifi_station_atlas.png`

The active sci-fi character sheet is:

`character.png`

The generated sci-fi engineer player runtime sheet is:

`sprites/scifi_engineer_player_sheet.png`

The matching chroma-key source sheet is kept beside it for reproducibility:

`sprites/scifi_engineer_player_sheet_chroma.png`

The first sci-fi item model sheet should be dropped at:

`sprites/scifi_item_atlas.png`

The second sci-fi utility item model sheet should be dropped at:

`sprites/scifi_utility_item_atlas.png`

The first sci-fi weapon model sheet should be dropped at:

`sprites/scifi_weapon_atlas.png`

The first sci-fi tool model sheet should be dropped at:

`sprites/scifi_tool_atlas.png`

The first sci-fi greenhouse structure sheet should be dropped at:

`structures/scifi_greenhouse_atlas.png`

That path is registered in `scripts/World/ThemeArtRegistry.cs`. The prototype
currently maps a small sci-fi subset from that sheet: floors, metal walls,
airlock doors, duel ring floor, and oddity pile.

Atlas rendering is intentionally opt-in per tile id. Set `HasAtlasRegion` only
after the exact source rectangle for a tile/prop is mapped from the sheet; any
unmapped future ids should keep placeholder colors so the prototype stays
readable.

Humanoid actors now start from a Godot-native workflow: `PrototypeCharacterSprite`
builds an `AnimatedSprite2D` plus `SpriteFrames` from the mapped atlas regions
in `scripts/Art/PrototypeSpriteModels.cs`. Player, Mara, and the peer stand-in
have character atlas source regions mapped there; the local player currently
uses the generated engineer sheet at `sprites/scifi_engineer_player_sheet.png`,
while NPCs still use `character.png`. If an expected atlas is missing, actors
fall back to the procedural models.

Mapped props and pickups use `PrototypeAtlasSprite`, which builds a native
Godot `Sprite2D` with an `AtlasTexture` from catalog source rectangles. If a
region is missing, the component falls back to `PrototypeSprite` procedural
placeholder art.

Mapped structures use `StructureSprite`, which builds a native `Sprite2D` with
an `AtlasTexture` from `StructureArtCatalog` and keeps its procedural greenhouse
fallback for missing regions.

All native atlas renderers share `AtlasFrame`/`AtlasFrames` for source
rectangles, display scaling, and footprint/prop anchoring. New art mappers
should add catalog data first, then convert through that shared helper.

`ArtAssetManifest` discovers atlas paths from the sprite, structure, and tile
catalogs. When a catalog path is mapped, smoke tests expect that file to exist
on disk, so keep reference sheets separate from runtime sheets until they are
ready to be wired into a catalog.

For clean runtime character sheets, use the `32 x 32` grid documented in
`AGENTS.md`. Once a sheet lands, map its top-left runtime-frame origin with
`PrototypeSpriteCatalog.FourDirectionGridAnimations(...)` so Godot receives
named `SpriteFrames` animations instead of a single still frame.
The current core item models also have source regions mapped for
`sprites/scifi_item_atlas.png`: whoopie cushion, deflated balloon, repair kit,
practice stick, work vest, and scrip.
The utility item sheet maps ration pack, data chip, filter core, contraband
package, apology flower, and portable terminal.
The weapon sheet maps stun baton, electro pistol, SMG-11, shotgun Mk1,
Rifle-27, Sniper X9, plasma cutter, flame thrower, grenade launcher, railgun,
impact mine, and EMP grenade.
The tool sheet maps multi tool, welding torch, medi patch, repair kit-style
support tools, lockpick set, flashlight, portable shield, hacking device,
scanner, grappling hook, chem injector, power cell, bolt cutters, and magnetic
grabber.
The greenhouse structure sheet is cataloged only for now. It maps standard,
overgrown, damaged, powered-off, and top-down greenhouse models plus base,
door, cap, panel, planter, grow rack, and support parts.
