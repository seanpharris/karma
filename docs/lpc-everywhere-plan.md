# Plan: All Characters Use LPC

Current decision (2026-05-02): the PixelLab paper-doll character system is
deprecated. Every character that renders in-game — the local player,
remote players, NPCs (Mara/Dallen/the medieval roster), wandering walkers,
proxy peer stand-ins — composes through the LPC layer system instead.

This doc captures the current state, the target state, and the migration
path.

## Current state

**Player**: prefers LPC.
[`PrototypeCharacterSprite.ApplyPlayerAppearanceSelection`](../scripts/Art/PrototypeCharacterSprite.cs)
returns `LpcRandomCharacterAtlasPath` first when the file exists, falling
through to PixelLab paths only if it doesn't. The LPC composer
[`tools/lpc_compose_random.gd`](../tools/lpc_compose_random.gd) writes
that atlas — currently a single random pick rather than a chosen theme.

**NPCs**: still on PixelLab.
[`PrototypeSpriteCatalog.GetKindForNpc`](../scripts/Art/PrototypeSpriteModels.cs)
maps NPC ids to sprite kinds, and the kinds resolve to PixelLab atlas
paths. No LPC support.

**Theme bundles**: 60 LPC bundle JSONs exist under
[`assets/art/sprites/lpc/themes/`](../assets/art/sprites/lpc/themes/),
one per medieval role plus the three player archetypes. They're data only
— nothing reads them yet.

**Theme roster**: medieval NPC roster is defined in
[`assets/themes/medieval/theme.json`](../assets/themes/medieval/theme.json),
with each NPC binding to an `lpc_bundle` id. Also data only.

## Target state

Three new pieces:

1. **Bundle reader**: `tools/lpc_compose_theme.gd` reads
   `assets/art/sprites/lpc/themes/<bundle>.json`, resolves each layer's
   `walk.png` (and other animations on demand), composites them in LPC
   z-order, and writes a Karma-format atlas.
2. **NPC atlas materialization**: at world-gen / theme-load time, the
   server (or a build step) iterates the theme roster and ensures each
   NPC's `lpc_bundle` has been composited into an atlas at a stable path
   like
   `assets/art/generated/lpc_npcs/<bundle_id>_32x64_8dir_4row.png`.
3. **Renderer dispatch**: `PrototypeCharacterSprite` learns to look up
   the NPC's bundle via theme metadata and load the matching atlas
   (same `forceImageLoad` path the player uses, since these are
   generated PNGs without Godot import metadata).

## Migration path (incremental, each step keeps the build green)

### Step 1 — bundle reader

**File**: `tools/lpc_compose_theme.gd` (new, modelled on
`lpc_compose_random.gd`).

**Args**: `--bundle <id>` reads `assets/art/sprites/lpc/themes/<id>.json`.

**Output**: same shape as the random composer:
- `assets/art/generated/lpc_npcs/<id>_lpc_walk.png` — native 576×256.
- `assets/art/generated/lpc_npcs/<id>_32x64_8dir_4row.png` — Karma atlas.

**Acceptance**: smoke-test invocation against
`medieval_warrior_male.json` produces both files; running it against
every bundle in `themes/` works in a loop.

### Step 2 — batch materialise

**File**: `tools/lpc_materialize_theme_bundles.gd` (new, calls the
bundle reader for each json under `themes/`).

**Acceptance**: produces 60 atlases under
`assets/art/generated/lpc_npcs/`, each renders without warnings.

### Step 3 — wire NpcProfile

Add a string field `LpcBundleId` to
[`NpcProfile`](../scripts/Data/NpcModels.cs).
Defaults to empty. When set, takes priority over `PrototypeSpriteCatalog`
for rendering that NPC.

`StarterNpcs.Mara` / `Dallen` get `LpcBundleId` values pointing at
medieval bundles (e.g. `Mara` → `blacksmith_male`, `Dallen` →
`tavernkeeper_female`).

### Step 4 — renderer dispatch

In `PrototypeCharacterSprite.ApplyPlayerAppearanceSelection` (or a new
`ApplyNpcBundle` for NPCs), if a bundle id is supplied, resolve to
`assets/art/generated/lpc_npcs/<bundle_id>_32x64_8dir_4row.png` and set
that as `AtlasPathOverride` (same `forceImageLoad` path the player uses).

`WorldRoot.RenderServerNpcs` looks up the bundle id from the NPC's
profile, passes it through to its `PrototypeCharacterSprite`.

### Step 5 — remove PixelLab fallbacks

After the LPC path is exercised end-to-end:

- Drop `PlayerV2RealBaseBlackBootsAtlasPath` from
  `ApplyPlayerAppearanceSelection`.
- Drop the prebuilt PixelLab paper-doll atlas from
  `PrototypeSpriteCatalog`.
- Keep the `Neutral_*humanoid_paper-doll_*` source files on disk so
  history is preserved, but the runtime no longer references them.

## Theme bundle ↔ NPC mapping

Captured in
[`assets/themes/medieval/theme.json`](../assets/themes/medieval/theme.json)
as `npc_roster[].lpc_bundle`. The existing
[`StarterNpcs.Mara`](../scripts/Data/NpcModels.cs) +
[`StarterNpcs.Dallen`](../scripts/Data/NpcModels.cs) need explicit
bindings (they pre-date the theme JSON):

| NPC          | Suggested bundle      |
|--------------|-----------------------|
| Mara         | `blacksmith_male`     |
| Dallen       | `tavernkeeper_female` |

(These are draft picks — the medieval text-theming pass already on the
parallel-agent queue can re-skin them with proper medieval names too.)

## Why not just compose at runtime?

We could. The reasons we materialize at build time first:

1. **Compose cost** is non-trivial. 60 NPCs × 8 layers × per-frame blits
   would re-run on every game launch. Materializing once, caching the
   PNG, lets the runtime just load a texture.
2. **Determinism**. Bundle JSON → atlas PNG is a pure function. We can
   diff atlases in CI to detect art regressions.
3. **Hot-reload**. Re-running the materializer is cheap; the in-game
   reload path is just "edit the bundle JSON, re-run the script,
   restart the scene". No engine changes per change.

If runtime composition becomes useful later (random procedural NPCs,
per-player palette swaps), the bundle reader can be ported to C# and
called per-NPC at spawn time. The data model doesn't need to change.
