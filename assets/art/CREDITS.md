# Karma Art Credits

Tracks the source, author, and license of every third-party-derived
art file shipped under `assets/art/`. Every file here either:

- is original work (no credit required), or
- derives from one of the source packs catalogued below.

---

## Item thumbnails — `assets/art/themes/medieval/items/*.png`

44 medieval item icons composed by `tools/lpc_compose_item_icons.gd`
(2026-05-02) by extracting the densest 64×64 cell from various LPC
sheets and downscaling to 32×32 with nearest-neighbour. The icons are
cropped derivatives of the LPC source sheets — the upstream license
applies (no new copyright is claimed on the crops).

**Source pack:** Universal LPC Spritesheet Character Generator
(<https://github.com/liberatedpixelcup/Universal-LPC-Spritesheet-Character-Generator>)
vendored at `assets/art/sprites/lpc/`. Per-sheet author + license info
is in `assets/art/sprites/lpc/CREDITS.csv`.

**Bulk licenses (every LPC sheet ships under at least one of these):**

- OGA-BY 3.0 — <https://static.opengameart.org/OGA-BY-3.0.txt>
- CC-BY-SA 3.0 — <https://creativecommons.org/licenses/by-sa/3.0/>
- GPL 3.0 — <https://www.gnu.org/licenses/gpl-3.0.html>

**Attribution requirement:** when shipping a build, credits screen
must list "Liberated Pixel Cup contributors" with a link to
<https://opengameart.org/content/liberated-pixel-cup-lpc-base-assets-sprites-map-tiles>.

### Source sheet → item id map

For each item icon, the source LPC path under
`assets/art/sprites/lpc/spritesheets/`:

| Item id | LPC source |
|---------|------------|
| stun_baton, flamethrower, emp_grenade | `weapon/blunt/club/club.png` |
| long_sword, plasma_cutter | `weapon/sword/longsword/walk/longsword.png` |
| short_bow, sniper_x9 | `weapon/ranged/bow/normal/walk/foreground.png` |
| crossbow, rifle_27, railgun | `weapon/ranged/crossbow/walk/crossbow.png` |
| electro_pistol, smg_11, lockpick_set | `weapon/sword/dagger/walk/dagger.png` |
| shotgun_mk1, grenade_launcher, impact_mine | `weapon/blunt/waraxe/walk/waraxe.png` |
| work_vest | `shield/heater/original/wood/fg/walk/oak.png` |
| portable_shield | `shield/spartan/fg/walk/spartan.png` |
| repair_kit, portable_terminal | `tools/smash/foreground/hammer.png` |
| welding_torch, flashlight, chem_injector, stim_spike, downer_haze, tremor_tab, apology_flower | `weapon/magic/wand/male/slash/wand.png` |
| multi_tool | `tools/smash/foreground/pickaxe.png` |
| medi_patch, data_chip | `backpack/basket_contents/ore/fg/walk/silver.png` |
| hacking_device, ration_pack | `backpack/basket_contents/wood/fg/walk/3_logs.png` |
| scanner | `backpack/squarepack/male/walk/leather.png` |
| grappling_hook, magnetic_grabber | `tools/rod/foreground/rod.png` |
| power_cell | `backpack/basket_contents/ore/fg/walk/copper.png` |
| bolt_cutters | `tools/smash/foreground/axe.png` |
| ballistic_round | `weapon/ranged/bow/arrow/shoot/arrow.png` |
| energy_cell | `backpack/basket_contents/ore/fg/walk/gold.png` |
| filter_core | `backpack/basket_contents/ore/fg/walk/iron.png` |
| contraband_package | `backpack/squarepack/male/walk/maroon.png` |
| whoopie_cushion | `cape/solid/male/walk/red.png` |
| deflated_balloon | `cape/solid/male/walk/lavender.png` |
| backpack_brown | `backpack/backpack/male/walk/walnut.png` |

---

## NPC sprites — `assets/art/generated/lpc_npcs/*.png`

LPC bundle composites built by `tools/lpc_compose_theme.gd` and
`tools/lpc_materialize_theme_bundles.gd` from the same LPC source
pack. Same licensing applies.

---

## NPC portraits — `assets/art/themes/medieval/npc_portraits/*.png`

170 medieval NPC portraits cropped by
`tools/lpc_compose_npc_portraits.gd` (2026-05-02). Each portrait is
the top 32×32 (head + shoulders) of the south-facing standing frame
from the corresponding `lpc_npcs/<bundle_id>_32x64_8dir_4row.png`
atlas. Derivative — same LPC license terms apply (OGA-BY 3.0 /
CC-BY-SA 3.0 / GPL 3.0).

---

## Tile atlases — `assets/art/third_party/cainos_pixel_art_top_down_basic_v1_2_3/`

Cainos Pixel Art Top Down Basic v1.2.3.
- Author: Cainos
- License: CC0 (public domain)
- Source: <https://cainos.itch.io/pixel-art-top-down-basic>

### Derived structure icons — `assets/art/themes/medieval/structures/*.png`

Initial 22 medieval structure icons cropped by
`tools/compose_structure_icons.gd` (2026-05-02) from
`TX Props.png`, `TX Struct.png`, `TX Plant.png`. CC0 — no
attribution required.

The 18 props from the original Cainos crops were superseded same-day
by PixelLab generations (see below). The Cainos-derived plant icons
(`tree_oak`, `tree_cypress`, `bush_green`, `bush_round`,
`stone_arch_open`, `stone_ring`) remain untouched.

---

## PixelLab-generated art — 2026-05-02 PM

Generated via `tools/pixellab_generate_batch.js` against
`https://api.pixellab.ai/v1/generate-image-pixflux` using account
`pharris.sean@gmail.com` (Tier 2: Pixel Artisan). Burning the
5,000/month subscription pool, no overage credits used.

**Transparency:** the API flag `no_background: true` is required to
get transparent edges (verified 2026-05-02 — prompt-only "transparent
background" wording is *ignored*). The batch script defaults it to
true; per-item override `"background": true` keeps a solid background
(used for HUD parchment/wood/stone tiled textures). Initial generations
without the flag were regenerated 2026-05-02 PM.

**License:** PixelLab outputs ship under the PixelLab Output License
(<https://www.pixellab.ai/terms>) — usable commercially in games,
no attribution required. We list them here for provenance hygiene
anyway.

### Buildings — `assets/art/themes/medieval/buildings/*.png` (12)

`smithy`, `tavern`, `chapel`, `watchtower`, `market_stall`,
`tithe_barn`, `notice_post`, `well`, `shrine`, `bell_tower`,
`duel_ring`, `memorial`. Sized 64–128 px.

Prompt batch: [`tools/pixellab_batches/buildings.json`](../../tools/pixellab_batches/buildings.json).

### Banners — `assets/art/themes/medieval/banners/*.png` (6)

`banner_chapel_order`, `banner_crown_garrison`,
`banner_shadowed_guild`, `banner_wayfarers`, `banner_wild_folk`,
`banner_freeholders`. 64×96 each.

Prompt batch: [`tools/pixellab_batches/banners.json`](../../tools/pixellab_batches/banners.json).

### Item upgrades — `assets/art/themes/medieval/items/*.png` (18)

PixelLab replaced these items, originally LPC stand-ins:
`stim_spike` (red potion), `downer_haze` (purple potion),
`tremor_tab` (herb packet), `ration_pack` (cloth-wrapped meal),
`energy_cell` (crystal shard), `medi_patch` (bandage), `chem_injector`
(brass syringe), `flashlight` (iron lantern), `welding_torch`
(burning torch), `apology_flower` (red rose), `whoopie_cushion`
(deflated wineskin), `deflated_balloon` (pig bladder),
`data_chip` (sealed scroll), `filter_core` (bronze cog),
`contraband_package` (tied parcel), `portable_terminal`
(spellbook), `emp_grenade` (clay throwing pot), `impact_mine`
(spiked caltrop). 32×32 each.

The remaining 26 item icons under `assets/art/themes/medieval/items/`
are LPC-derived (see "Item thumbnails" section above).

Prompt batch: [`tools/pixellab_batches/consumables.json`](../../tools/pixellab_batches/consumables.json).

### Structure refinements — `assets/art/themes/medieval/structures/*.png` (18 PixelLab + 6 Cainos = 24 total)

PixelLab regenerated and replaced these props that the Cainos
hand-picked rectangles got wrong:
`wooden_door`, `iron_door`, `notice_board`, `wooden_sign`, `chest`,
`crate`, `barrel`, `anvil_block`, `bell_stand`, `stone_cross`,
`tombstone`, `clay_pot`, `ceramic_urn`, `stone_pedestal`, `statue`,
`stone_wall`, `stone_arch`, `stone_stairs`. 64×64 each.

Prompt batch: [`tools/pixellab_batches/structures.json`](../../tools/pixellab_batches/structures.json).

### Decals — `assets/art/themes/medieval/decals/*.png` (8)

`decal_blood_splatter`, `decal_footprints_dirt`, `decal_hay_pile`,
`decal_scorch_mark`, `decal_water_puddle`, `decal_loose_stones`,
`decal_leaves`, `decal_market_rug`. 32×32 except market rug at
64×32.

Prompt batch: [`tools/pixellab_batches/decals.json`](../../tools/pixellab_batches/decals.json).

### Status effect icons — `assets/art/themes/medieval/status_icons/*.png` (14)

`status_hungry`, `status_starving`, `status_dirty`, `status_filthy`,
`status_poisoned`, `status_burning`, `status_chilled`,
`status_silenced`, `status_wraith`, `status_blessed`,
`status_cursed`, `status_stunned`, `status_wanted`,
`status_withdrawal`. 32×32 each. Wired into `RefreshStatusStrip`
(task #24 + #52).

Prompt batch: [`tools/pixellab_batches/status_icons.json`](../../tools/pixellab_batches/status_icons.json).

### HUD chrome — `assets/art/themes/medieval/hud_chrome/*.png` (10)

Background panels (`panel_parchment_bg`, `panel_wood_bg`,
`panel_stone_bg`), buttons (`button_wood`, `button_wood_pressed`),
ornament corners (`frame_corner_iron`, `frame_corner_gold`),
divider (`divider_horizontal`), scroll edges (`scroll_top`,
`scroll_bottom`). For wiring into the medieval `UiPaletteRegistry`
(task #31).

Prompt batch: [`tools/pixellab_batches/hud_chrome.json`](../../tools/pixellab_batches/hud_chrome.json).

### Map / currency / vital icons — `assets/art/themes/medieval/map_icons/*.png` (17)

Map markers (`quest`, `quest_complete`, `vendor`, `chapel`, `smithy`,
`tavern`, `danger`, `player`), currencies (`gold`, `silver`,
`copper`), karma glyphs (`saint`, `scourge`), vital stat icons
(`heart`, `lightning`, `meat`, `water`). All 32×32.

Prompt batch: [`tools/pixellab_batches/map_icons.json`](../../tools/pixellab_batches/map_icons.json).

### Quest glyphs — `assets/art/themes/medieval/quest_glyphs/*.png` (8)

`quest_recover`, `quest_hunt`, `quest_deliver`, `quest_smuggle`,
`quest_investigate`, `quest_escort`, `quest_repair`, `quest_rumor`.
32×32 each. For the quest-log panel (task #23).

Prompt batch: [`tools/pixellab_batches/quest_glyphs.json`](../../tools/pixellab_batches/quest_glyphs.json).

### Mount sprites — `assets/art/themes/medieval/mounts/*.png` (6)

`horse_brown`, `horse_white`, `horse_black`, `donkey_grey`,
`mule_pack` (all 64×64), `ox_cart` (96×64). For `MountEntity`
rendering.

Prompt batch: [`tools/pixellab_batches/mounts.json`](../../tools/pixellab_batches/mounts.json).

### Environment props — `assets/art/themes/medieval/environment/*.png` (10)

`tree_oak_large`, `tree_pine`, `tree_dead`, `bush_berry`,
`wheat_patch`, `campfire`, `hay_bale`, `wagon_wheel`, `fence_post`,
`stump_axe`. Sized 32–96 px. For procedural map decoration.

Prompt batch: [`tools/pixellab_batches/environment.json`](../../tools/pixellab_batches/environment.json).

---

## Original art

Anything else under `assets/art/` not listed above is original work
generated for this project (PixelLab outputs, hand-authored pixel
art, procedurally generated atlases). Default license is the
project's own.
