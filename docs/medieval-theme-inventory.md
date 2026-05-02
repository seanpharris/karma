# Medieval Theme — Art Inventory

Audit of every asset in the repo that could plausibly slot into a medieval
theme. Compiled 2026-05-02 to plan what we have vs. what we need before
wiring `ThemeArtRegistry.GetForTheme("medieval")`.

## What we have

### Characters & costume layers — **strong** (LPC)

[`assets/art/sprites/lpc/`](../assets/art/sprites/lpc/) is the gold mine.
Every layer ships at LPC's standard 64×64 / 9-col × 4-row format with the
full 13-animation suite (walk, run, slash, thrust, shoot, spellcast, hurt,
idle, jump, sit, climb, emote, combat_idle).

| Need                | LPC variants available                                                                |
|---------------------|---------------------------------------------------------------------------------------|
| Bodies              | male, female, muscular, pregnant, teen, child, skeleton, zombie                       |
| Heads               | human/{child, male, female, male_elderly, female_elderly, male_gaunt, male_plump, ...} |
| Eyes                | human/{adult, child, elderly} × {default, anger, closing, eyeroll, look_*}            |
| Hair                | short, long, messy, bob, afro, braids, beards (+26 palette colours)                    |
| Torso (armour)      | `torso/armour/{leather, legion, plate}`                                               |
| Torso (clothing)    | `torso/clothes/{longsleeve, shortsleeve, sleeveless, ...}`                            |
| Legs                | pants, shorts, skirts                                                                 |
| Feet                | shoes, boots                                                                          |
| Helmets             | `hat/helmet/{armet, armet_simple, barbarian, barbarian_nasal, barbarian_viking, barbuta, barbuta_simple, ...}` (dozens) |
| Shields             | `shield/{crusader, crusader2, round}` and more                                        |
| Melee weapons       | `weapon/sword/{arming, longsword, dagger, katana, rapier, saber, scimitar}`           |
|                     | `weapon/blunt/{club, mace, flail, waraxe}`                                            |
|                     | `weapon/polearm/{spear, halberd, longspear, trident, scythe}`                         |
| Ranged weapons      | `weapon/ranged/{bow/great, slingshot, crossbow}`                                      |
| Magic               | `weapon/magic/{crystal, wand, loop_off, gnarled, dragonspear}`                        |
| Quivers, arrows     | `quiver/`, `weapon/ranged/.../arrow.png`                                              |

LPC weapons each ship in 8 metal palettes (brass/bronze/ceramic/copper/
gold/iron/silver/steel) so a "rusty iron sword" vs "polished gold sword"
is purely a palette swap, not a separate sheet.

**Gap inside LPC**: `cape` category exists as a folder but ships **0
walk.png** sheets — capes only have non-walk animations (slash, etc.).
If we want walking capes we'd need to commission one or skip capes.

### Knight character preview — **drop-in**

[`assets/art/third_party/2D Character Knight/`](../assets/art/third_party/)
has 87 PNGs of a stylised knight (Idle, Walk, Run, Crouch, Melee, Kick,
Slide, Hurt, Die, FrontFlip, CastSpell). Different art style from LPC —
darker line work, more detailed silhouette. Useful as a stand-in player
character or as a key NPC if we don't want to use LPC composites.

### Tilesets — **partial**

| Pack                                             | Useful for                                                     | Status                       |
|--------------------------------------------------|----------------------------------------------------------------|------------------------------|
| [`cainos_pixel_art_top_down_basic_v1_2_3`](../assets/art/third_party/cainos_pixel_art_top_down_basic_v1_2_3/Texture/) | grass tiles, stone-ground tiles, walls, props, plants — TOP-DOWN, fits medieval country | core terrain coverage ✓     |
| [`mixel_top_down_rpg_32x32_v1_7`](../assets/art/third_party/mixel_top_down_rpg_32x32_v1_7/) | ruins building, items, ground tiles, water, trees, rocks, mushrooms, bushes | strong fantasy/medieval RPG vibe ✓ |
| [`nature_free_noncommercial`](../assets/art/third_party/nature_free_noncommercial/) | a single global.png nature atlas                            | **non-commercial only** — ringfence if we ship   |

The Cainos `TX Tileset Grass.png` + `TX Tileset Stone Ground.png` + `TX
Tileset Wall.png` are the "lots of grass with worn paths and stone roads"
look that fits a medieval village.

The Mixel `Topdown RPG 32x32 - Ruins.PNG` is the building shell — castle
walls, towers, archways, derelict structures. Pairs well with Cainos for
ground.

**Gap**: neither pack has finished medieval-village BUILDINGS the way
`assets/themes/boarding_school/buildings_atlas.png` does for the school
theme. The Mixel ruins pack is closer to "ancient stone ruins" than to
"thatched-roof village". For active village buildings (taverns, smithy,
church, market stalls) we don't have a medieval atlas.

### Existing Karma tilesets — **wrong theme**

[`assets/themes/`](../assets/themes/) has `boarding_school` (modern school
buildings) and `space` and `western`. None medieval.

[`assets/art/sprites/scifi_*`](../assets/art/sprites/) are sci-fi. Not useful.

[`assets/art/sprites/Neutral_*humanoid_paper-doll_*`](../assets/art/sprites/) are
the PixelLab paper-doll work — neutral style, could be re-skinned medieval
later but not currently themed.

### Structures atlases — **none medieval**

[`assets/art/structures/`](../assets/art/structures/) only ships
`scifi_greenhouse_atlas.png`. No medieval houses, taverns, or churches.

### Generated / sliced atlases — **mostly wrong theme**

[`assets/art/generated/`](../assets/art/generated/) is dominated by
sci-fi-prototype slicing work (clinic, supply shop, walls/doors,
hazards, mission boards). The Gemini/PixelLab static atlases are
sci-fi-themed. **None of this is medieval**.

## What we'd need to build a medieval theme

Rough priority order for a playable medieval prototype:

### Tier 1 — **must have for "looks medieval"**

1. **`ThemeArtRegistry.Medieval(theme)`** — wire the Cainos grass / stone
   / wall tiles into the existing tile contract (`WorldTileIds.GroundScrub`,
   `PathDust`, `MarketFloor`, `ClinicFloor`, `WallMetal`, etc.). Most
   tiles map cleanly; `WallMetal` needs renaming or tolerating the
   stone-walls subtitution.
2. **A medieval village buildings atlas.** Either:
   - Compose Mixel ruins + Cainos walls into a custom village atlas, OR
   - Find/commission a thatched-house tileset.
3. **An LPC theme bundle for the player character** — `themes/medieval_warrior_male.json`,
   `themes/medieval_peasant_female.json`, etc. — 4 to 8 starter
   character archetypes that reuse the LPC stack we already built the
   composer for.

### Tier 2 — **gives the world life**

4. **NPC bundles** for blacksmith, tavernkeeper, merchant, guard, peasant,
   priest, knight. Each is a theme bundle JSON.
5. **Medieval prop atlas** (barrels, crates, hay bales, wells, lanterns,
   anvils, market stalls, signs). Mixel + Cainos cover ~60% of this.
6. **Theme-appropriate dialogue + faction names**. Currently NPCs and
   factions use sci-fi names (Free Settlers, Mara Venn the Clinic
   Mechanic). For real medieval feel we'd rename (Mara → "Mara the
   Blacksmith"), or add a faction set per theme and rename only the
   ones shown in the current scene.
7. **Item icons** — medieval-themed icons for sword/bow/herb/loaf/coin
   replacing the sci-fi item atlas mappings.

### Tier 3 — **polish**

8. **Animated water tiles** for moats / rivers (Mixel has `WaterTileset`).
9. **Bow + arrow VFX** to match the LPC ranged weapons.
10. **Banners and heraldry** to mark faction territory.
11. **Music swap** — `PrototypeMusicPlayer` themes (`SandboxCalm`,
    `EventTension`) tweaked into medieval moods (lute progression?).

## Recommended first slice

Smallest playable change: do tier 1 only — wire the existing Cainos +
Mixel tiles into a `medieval` ThemeArtSet, ship an LPC theme bundle for
the player, and force the world generator to use theme=`medieval`. The
buildings problem can be deferred by reusing the existing structure
spawn logic with placeholder atlas regions; visually the world will look
"medieval-with-sci-fi-buildings" until the atlas lands, but the *ground*,
*paths*, *trees*, and *player* will all be in-theme. That's enough to
feel medieval at a glance and gives us a working baseline.

After that, the tavern/smithy/church atlas is the next big domino.

---

## Tier 1 status — **landed 2026-05-02**

- [x] `ThemeArtRegistry.Medieval(theme)` mapping the existing tile
      contract onto Cainos grass / stone / wall sheets. See
      [`scripts/World/ThemeArtRegistry.cs`](../scripts/World/ThemeArtRegistry.cs).
- [x] `WorldConfig.CreatePrototype()` defaults to
      `new WorldSeed(8675309, "Medieval Prototype", "medieval")`.
- [x] LPC theme bundle JSON shipped for three archetypes:
      `medieval_warrior_male`, `medieval_archer_female`,
      `medieval_peasant_male`. Live under
      [`assets/art/sprites/lpc/themes/`](../assets/art/sprites/lpc/themes/).
- [x] Boarding-school smoke tests pinned to an explicit
      `boarding_school` config so they keep validating that theme path
      regardless of prototype defaults.
- [ ] **Pending visual eyeball:** open `Main.tscn` / `tools/run-game.ps1`
      and check whether the Cainos region picks I chose for each tile
      role look right. Likely some atlas-region nudging needed in
      `Medieval(theme)`.
- [ ] **Pending: theme-bundle reader.** The composer
      [`tools/lpc_compose_random.gd`](../tools/lpc_compose_random.gd)
      still does pure random picks. A sibling tool
      `tools/lpc_compose_theme.gd` should read a `themes/<id>.json`,
      look each layer up, and emit a deterministic stack — that's how
      the new `medieval_*.json` bundles become an actual playable
      character.

## Tier 2 priorities (after eyeball pass)

In rough order of player-visibility impact:

1. **Medieval village buildings atlas** — biggest remaining gap. Either
   compose Cainos walls + Mixel ruins into a custom atlas or sub in a
   third-party LPC-style village tileset.
2. **Theme-appropriate text** (NPC names, faction names, dialogue,
   location descriptions). Currently sci-fi (Mara the Clinic Mechanic /
   Free Settlers / supply drops). Pure data work; flagged as task #7
   in the parallel-agent queue at the top of `TASKS.md`.
3. **Medieval prop atlas** — barrels, hay bales, wells, anvils, market
   stalls, signs, lanterns. Cainos + Mixel cover ~60%, rest TBD.
4. **NPC bundles** for blacksmith / tavernkeeper / merchant / guard /
   peasant / priest / knight, alongside the existing player
   archetypes. Same JSON shape as
   `assets/art/sprites/lpc/themes/medieval_*.json`.
5. **Item icons** — medieval-themed icons for sword/bow/herb/loaf/coin
   replacing the sci-fi item atlas regions.
