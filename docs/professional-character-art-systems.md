# Professional Pixel Character Art Systems

This note captures better reference standards for replacing Karma's rough prototype
character sheet with a scalable, professional-looking character system.

## What polished pixel RPGs usually do

Most character-heavy 2D RPGs do **not** redraw every full character for every
outfit and item combination. They use a hybrid of:

1. **Paper-doll layers**
   - base body / skin
   - hair / head details
   - torso clothing
   - legs / boots
   - armor / backpack
   - held items / weapons
   - optional effects/highlights

2. **Palette swaps**
   - skin tones
   - clothing color variants
   - faction variants
   - NPC recolors

3. **Bespoke full sprites only when needed**
   - non-human bodies
   - bulky silhouettes
   - huge coats/dresses/armor
   - unique boss/NPC shapes
   - special cinematic actions

The important professional constraint is that every swappable layer must share
one exact animation/frame contract. If frame 42 is `run/front-right`, then body,
hair, jacket, pants, backpack, and weapon overlays all need compatible pixels for
frame 42.

## Reference systems worth learning from

### Universal LPC Spritesheet Generator / Liberated Pixel Cup

- Site: <https://liberatedpixelcup.github.io/Universal-LPC-Spritesheet-Character-Generator/>
- Repository: <https://github.com/liberatedpixelcup/Universal-LPC-Spritesheet-Character-Generator>
- It is one of the clearest open examples of a large paper-doll character
  ecosystem: bodies, clothing, hair, weapons, tools, and many actions share a
  common sheet convention.
- The generator supports many action families such as idle, walk, run, slash,
  thrust, shoot, hurt, watering/tool use, climb, jump, sit, emote, and combat
  variants depending on the fork/assets selected.
- **License caution:** LPC art commonly uses CC-BY-SA/GPL/OGA-style licenses and
  requires careful attribution. Treat it as a professional reference unless we
  intentionally accept those art-license obligations.

### Godot LPC Character Spritesheet Plugin

- Godot Asset Library: <https://godotengine.org/asset-library/asset/1673>
- Repository: <https://github.com/DrJamgo/Godot_LPC_Spritesheet_Gen>
- Asset Library lists the plugin as MIT.
- The plugin imports LPC generator output into Godot and models a useful runtime
  idea: a blueprint/resource that owns animation/layer definitions, plus a sprite
  class that can select movement animations and expose animation climax signals.
- **Karma fit:** useful architecture reference. We should not copy/import until
  deliberately reviewed, but the blueprint + layered sprite concept maps well to
  Godot/C#.

### RapidLPC

- Repository: <https://github.com/etamity/rapidlpc>
- MIT code license.
- Describes a Godot 4 paper-doll approach with modular body parts, live swapping,
  resource-driven design, z-index/layer sorting, and export options.
- **Karma fit:** useful proof that a Godot-friendly modular pixel character
  creator is practical. Its code is MIT, but any LPC art it consumes keeps its
  own license obligations.

### Commercial/pro art packs

Commercial farming/RPG pixel packs often sell a **base body plus matching layer
sheets**. The key pattern is still the same: identical layout for clothes, hair,
tools, etc. These are good style references, but we should only import assets if
we buy/verify the license and document it.

## Better Karma standard proposal

Our current `256x288` / `8 columns x 9 rows` sheet is useful for quick runtime
experiments, but it is too small and underspecified for the long term. It also
compresses too many actions into single rows.

Recommended next standard:

- **Frame size:** move from `32x32` to `48x48` or `64x64` for the professional
  character pipeline.
  - `32x32` is fine for tiny prototypes.
  - `48x48` is a good compromise for readable outfits/weapons.
  - `64x64` is best for polished animation but costs more art time.
- **Directions:** keep true 8-direction support.
- **Layer stack:** every character is rendered from ordered layers:
  1. shadow/effects, if separate
  2. base body / skin
  3. eyes/face where applicable
  4. hair behind body
  5. legs / boots
  6. torso clothing
  7. armor / coat / backpack
  8. hair/front/headwear
  9. held item / weapon
  10. muzzle/tool/effect overlay
- **Animation groups:** split actions into named groups rather than one giant
  flat row list:
  - `idle`
  - `walk`
  - `run`
  - `interact/reach`
  - `tool_use`
  - `melee_slash`
  - `melee_thrust`
  - `ranged_aim/shoot`
  - `carry/hold`
  - `downed_idle`
  - `downed_crawl`
  - `revive/help_up`
  - `carry_walk/drag`
  - `carried_body`
  - `hurt_to_downed`
- **Fallbacks:** not every layer/item must support every group immediately. A
  layer manifest can declare fallback groups, e.g. `run -> walk`, `ranged -> hold`,
  `revive/help_up -> interact`, or `carry_walk/drag -> walk`.
- **Manifest-first:** every sheet should ship with metadata describing frame
  size, direction order, animation groups, layer type, z-order, and fallbacks.

## Downed/carry gameplay implications

Karma's future downed/rescue loop needs to be part of the art contract, not an
afterthought. If a player can be downed, helped up, carried to a clinic, abandoned,
or executed for karma consequences, the base body must establish compatible poses
for those states early. See [`downed-carry-rescue-mechanics.md`](downed-carry-rescue-mechanics.md)
for the gameplay loop and animation requirements.

## Practical path from current prototype

1. Keep the existing `32x32` sheet as a throwaway prototype/runtime fallback.
2. Add a `karma-character-v2` art contract based on a layered paper-doll system.
3. Make or source one polished neutral base body in `48x48` or `64x64`.
4. Add only a few initial layers:
   - skin/base body
   - hair
   - simple outfit
   - boots
   - one tool/weapon hold overlay
5. Build a compositor/exporter that can combine layers into a runtime atlas.
6. Make Godot draw either:
   - precomposited runtime sheets for performance/simplicity, or
   - live layered sprites for customization previews and debug/dev tools.

## Recommendation

Use LPC/RapidLPC/Godot LPC as **architecture references**, not direct runtime art
sources. The LPC ecosystem is professional and mature, but its art licensing is
not frictionless for a custom commercial-friendly project. Karma should adopt the
paper-doll/manifest/compositor pattern and commission/generate/draw original art
against that standard.
