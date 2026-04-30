# Art Asset Audit (2026-04-30)

Cross-references every line item in [`ART_NEEDED.md`](ART_NEEDED.md) against
the existing libraries:

- **PRI** — `assets/art/generated/priority_static_atlases/` (8 JPGs, Gemini)
- **SE** — `assets/art/generated/static_event_atlases/` (33 JPGs, Gemini)
- **SP** — `assets/art/sprites/generated/gemini_static_props_2026_04_27/`
- **NP** — `assets/art/sprites/generated/gemini_natural_props_2026_04_27/`
- **REF** — `assets/art/reference/gemini_prototypes/2026-04-27/`
- **3P** — `assets/art/third_party/` (Cainos, Mixel, Knight, nature_free)
- **WIRED** — already present in code via `PrototypeSpriteModels.cs` /
  `StructureArtModels.cs` (sci-fi atlases)

Status legend:
- ✅ **EXISTS** — already in repo, just needs slicing/wiring
- 🟡 **PARTIAL** — partial match in atlases; may need a polish pass
- ❌ **GAP** — genuinely missing; would need fresh generation

Counts at the bottom drive the actual generation budget.

---

## Step 2 — Repair Mission

| Asset | Source | Status |
|---|---|---|
| Repair tool icons (multi-tool, welding torch, 16×16) | SE `crafting_stations_atlas` row 1 col 3 (tool wall); `scifi_tool_atlas.png` (WIRED); 3P `mixel_top_down_rpg_32x32_v1_7/Items v1.0` | ✅ EXISTS |
| Damaged fixture state overlay | PRI `structure_world_state_atlas` row 1 (generator pristine→damaged→wrecked→sabotaged), col 4 (greenhouse pristine→shattered) | ✅ EXISTS |

## Step 3 — Delivery Quest

| Asset | Source | Status |
|---|---|---|
| FilterCore icon (16×16) | SP `power_cell_canister.png` (close substitute); `scifi_item_atlas.png` (WIRED) | ✅ EXISTS |
| DataChip icon (16×16) | `scifi_utility_item_atlas.png` (WIRED — data chip already there) | ✅ EXISTS |

## Step 4 — Rumor Quest

| Asset | Source | Status |
|---|---|---|
| Evidence token / data chip icon | SE `evidence_clues_atlas` (data chip cracked, evidence sack, sealed cert, anonymous letter — 25 cells of evidence flavor) | ✅ EXISTS |
| Exposed/buried outcome stamp | SE `evidence_clues_atlas` (sealed cert, official permit, anonymous letter); PRI `wanted_bounty_law_atlas` (wax seal stamps) | ✅ EXISTS |

## Step 5 — Paragon Favor Perk

| Asset | Source | Status |
|---|---|---|
| Paragon aura ring (animated 2–4 frame glow) | — | ❌ GAP |
| Paragon HUD badge (16×16) | SE `faction_reputation_symbols_atlas` row 1 col 1 (gold winged halo); PRI `prototype_ui_icons_atlas` (BOUNTY CLAIMED check-fist could substitute) | ✅ EXISTS |

## Step 6 — Abyssal Mark Perk

| Asset | Source | Status |
|---|---|---|
| Abyssal aura ring (animated dark distortion) | — | ❌ GAP |
| Abyssal HUD badge (16×16) | SE `faction_reputation_symbols_atlas` row 1 col 2 (red horned demon head) | ✅ EXISTS |

## Step 7 — Posse Formation

| Asset | Source | Status |
|---|---|---|
| Posse leader crown (16×16) | SE `faction_reputation_symbols_atlas` (medals/crowns row 4); PRI `wanted_bounty_law_atlas` (badge cells) | ✅ EXISTS |
| Posse member tag (colored dot/bracket) | SE `event_markers_atlas` row 1 col 1 (target ring); PRI `prototype_ui_icons_atlas` POSSE INVITE / POSSE ACCEPTED | ✅ EXISTS |

## Step 8 — Posse HUD Panel

| Asset | Source | Status |
|---|---|---|
| Posse panel frame chrome | 3P `mixel_top_down_rpg_32x32_v1_7/User Interface v1.0/Topdown RPG 32x32 - UI Panels & Buttons.PNG` | ✅ EXISTS |
| Health bar segment art | 3P `mixel_top_down_rpg_32x32_v1_7/User Interface v1.0/Topdown RPG 32x32 - UI Bars.PNG` | ✅ EXISTS |

## Step 9 — Saint/Scourge NPC Behavior

| Asset | Source | Status |
|---|---|---|
| Heart welcome emote (16×16) | SE `ui_status_icons_atlas` row 1 col 1 (POSITIVE — glowing heart) | ✅ EXISTS |
| Skull fear emote (16×16) | SE `ui_status_icons_atlas` row 6 col 6 (DANGER/HEAT — flame skull); PRI `wanted_bounty_law_atlas` skull crossbones | ✅ EXISTS |
| Coin price-change emote (16×16) | SE `ui_status_icons_atlas` row 1 col 5 (BOUNTY — money bag); SE `event_markers_atlas` row 2 col 5 (green money bag) | ✅ EXISTS |

## Step 10 — Chat Tabs

| Asset | Source | Status |
|---|---|---|
| Tab bar (Local / Posse / System) | 3P `mixel_top_down_rpg_32x32_v1_7/User Interface v1.0/Topdown RPG 32x32 - UI Panels & Buttons.PNG` | ✅ EXISTS |
| Unread indicator dot/count | SE `event_markers_atlas` (red exclamation, target dot variations) | ✅ EXISTS |

## Step 12 — Combat Heat Tracking

| Asset | Source | Status |
|---|---|---|
| Heat zone overlay tiles (warm/hot/critical) | PRI `prototype_ui_icons_atlas` "DANGER HEAT" (single icon; needs 3-tier tile derivation); SE `hazards_disasters_atlas` (red barrel hazard, electric portal, fire pool) | 🟡 PARTIAL — needs derivation pass |

## Step 14 — Downed State

| Asset | Source | Status |
|---|---|---|
| Downed idle / breath frames | PRI `prototype_ui_icons_atlas` PLAYER DOWNED (single icon, not animated); player_v2 sprite sheet has no downed pose | ❌ GAP — frames missing |
| Downed countdown overlay | PRI `clinic_rescue_revive_atlas` row 4 cols 4-5 (red-X downed marker); SE `event_markers_atlas` row 1 col 3 (warning triangle) | ✅ EXISTS |

## Step 15 — Rescue / Carry Mechanic

| Asset | Source | Status |
|---|---|---|
| Rescuer carry walk cycle (8 directions) | — | ❌ GAP — needs full directional animation |
| Carried (downed) overlay | PRI `clinic_rescue_revive_atlas` row 4 col 4 (X-stretcher silhouette) | 🟡 PARTIAL — silhouette only, not composited |

## Step 16 — Clinic Recovery

| Asset | Source | Status |
|---|---|---|
| Recovery bed prop | PRI `clinic_rescue_revive_atlas` row 1 cols 3-4 (clinic bed, stretcher); SE `interior_furniture_atlas` row 1 cols 1-2 (bed, medical bed); SE `interior_furniture_atlas` row 2 col 1 (cot) | ✅ EXISTS |
| NPC healing emote (green cross/pulse) | PRI `clinic_rescue_revive_atlas` row 4 col 6 (blue shield buff); SE `ui_status_icons_atlas` row 4 col 6 (CLINIC red cross) | ✅ EXISTS |

## Step 17–18 — Roads / Path Generation

| Asset | Source | Status |
|---|---|---|
| Road tile set (4 connection × 2 surface) | 3P `cainos_pixel_art_top_down_basic_v1_2_3/Texture` (full path tile pack) | ✅ EXISTS |
| Edge-blend feather tiles | 3P Cainos pack has feathered transition tiles | ✅ EXISTS |

## Step 19 — Mount / Vehicle Entity

| Asset | Source | Status |
|---|---|---|
| Vehicle sprite (hovercycle/cart, all dirs + idle/hover) | PRI `theme_variant_matrix_atlas` row 1 col 1 (covered wagon — single direction); SE `transport_props_atlas` (need to check) | 🟡 PARTIAL — single static mount art exists, no directional animation |
| Empty parked state | SE `event_markers_atlas` row 6 col 3 (small mount); SE `ui_status_icons_atlas` row 6 col 1 (MOUNT — black horse) | 🟡 PARTIAL — icon only, not world prop |
| Mounted rider state | — | ❌ GAP — needs player+mount composite |

## Step 20 — Mount / Dismount

| Asset | Source | Status |
|---|---|---|
| Mount/dismount flash (2-frame burst) | PRI `prototype_ui_icons_atlas` MOUNT / DISMOUNT (single icons each, not animated burst) | 🟡 PARTIAL — static icons exist; animation frames missing |

## Step 22 — Karma Title-Change Broadcast

| Asset | Source | Status |
|---|---|---|
| Title-change toast banner | PRI `prototype_ui_icons_atlas` MATCH STARTED / MATCH SUMMARY (banner-style icons) | ✅ EXISTS |
| Crown / skull flash near new holder | SE `faction_reputation_symbols_atlas` (medals/crowns/skulls row 1, row 4) | ✅ EXISTS |

## Step 24 — Warden Perk / Wanted Warrant

| Asset | Source | Status |
|---|---|---|
| Wanted poster icon (16×16, red border + !) | PRI `wanted_bounty_law_atlas` (wanted poster wall, mug-shot frame, missing-person sign); PRI `prototype_ui_icons_atlas` WANTED; SE `event_markers_atlas` WANTED poster (rows 2-3 col 2) | ✅ EXISTS — multiple variants |
| Warden HUD badge (star/shield, 16×16) | PRI `prototype_ui_icons_atlas` BOUNTY CLAIMED (sheriff star); SE `player_interaction_props_atlas` row 2 col 1 (rifles + sheriff star) | ✅ EXISTS |

## Step 25 — Wraith Surge Perk

| Asset | Source | Status |
|---|---|---|
| Speed trail / motion blur (3-frame fade) | — | ❌ GAP — animated trail missing |
| Wraith HUD badge (skull outline, 16×16) | SE `faction_reputation_symbols_atlas` row 3 col 1 (pirate skull); SE `ui_status_icons_atlas` row 6 col 6 (DANGER/HEAT flame skull) | ✅ EXISTS |

## Step 26 — Bounty System

| Asset | Source | Status |
|---|---|---|
| Bounty floater coin tag (16×16) | SE `ui_status_icons_atlas` row 1 col 5 (BOUNTY money bag); SE `event_markers_atlas` row 2 col 5 (money bag) | ✅ EXISTS |

## Step 27 — Player Status Effects

| Asset | Source | Status |
|---|---|---|
| Poisoned overlay (green dot pulse, 1-2 frame) | SE `hazards_disasters_atlas` row 2 col 3 (toxic green pool — static); needs 2-frame pulse derivation | 🟡 PARTIAL — base art exists; pulse animation needed |
| Burning overlay (orange flicker, 2-3 frame) | SE `hazards_disasters_atlas` row 1 col 2 (fire — static) | 🟡 PARTIAL — base art exists; flicker frames needed |
| Status icon panel row (8×8 each) | SE `ui_status_icons_atlas` is the entire 36-icon set (used as 16×16 or scale-down to 8×8) | ✅ EXISTS |

## Step 28 — Contraband Item Tag

| Asset | Source | Status |
|---|---|---|
| Contraband Package sprite (wrapped parcel + caution tape) | PRI `wanted_bounty_law_atlas` row 2 col 3 (sealed contraband bag with question mark); PRI `prototype_ui_icons_atlas` CONTRABAND DETECTED (red-X box) | ✅ EXISTS |
| Detection flash (red scan tint, 1 frame) | — | ❌ GAP — overlay tint missing |

## Step 29 — Lobby / Ready-Up Flow

| Asset | Source | Status |
|---|---|---|
| Ready-up checkmark / READY badge (16×16) | PRI `prototype_ui_icons_atlas` READY UP (green check) | ✅ EXISTS |
| Lobby count panel | 3P `mixel_top_down_rpg_32x32_v1_7/User Interface v1.0/Topdown RPG 32x32 - UI Panels & Buttons.PNG` | ✅ EXISTS |

## Step 30 — Supply Drop World Event

| Asset | Source | Status |
|---|---|---|
| Supply drop crate (32×32 + 2-3 frame landing) | PRI `supply_shop_loot_atlas` row 1 col 1 (parachute crate); SP `cargo_crate.png`; PRI `theme_variant_matrix_atlas` (4 themed supply drop variants); PRI `prototype_ui_icons_atlas` SUPPLY SPAWNED / SUPPLY CLAIMED | ✅ EXISTS — static art plentiful |
| Drop beacon overlay (pulsing column) | PRI `clinic_rescue_revive_atlas` row 1 col 4 (medical hologram tripod — repurposable); SE `event_markers_atlas` row 1 col 4 (shield), row 1 col 1 (target ring) | 🟡 PARTIAL — static beacon exists; pulse animation missing |
| Expired drop ghost (2-3 frame fade) | — | ❌ GAP — animated fade missing |

## Step 31 — NPC Patrol Routes

| Asset | Source | Status |
|---|---|---|
| Waypoint debug marker | SE `event_markers_atlas` row 1 col 2 (red pin) | ✅ EXISTS |

## Step 32 — Reputation Decay

| Asset | Source | Status |
|---|---|---|
| Reputation HUD bar segments (3-state) | 3P `mixel_top_down_rpg_32x32_v1_7/User Interface v1.0/Topdown RPG 32x32 - UI Bars.PNG`; SE `faction_reputation_symbols_atlas` (medal tiers) | ✅ EXISTS |

## Step 33 — Faction Store Gating

| Asset | Source | Status |
|---|---|---|
| Locked offer overlay (lock icon, 16×16) | SE `evidence_clues_atlas` row 1 col 3 (broken padlock); PRI `wanted_bounty_law_atlas` (locked safe row 4); SE `containers_loot_atlas` (safe variants) | ✅ EXISTS |
| Reputation-required tag badge (8×8) | SE `faction_reputation_symbols_atlas` (32 reputation badges) | ✅ EXISTS |

## Step 34 — Station Claim Intent

| Asset | Source | Status |
|---|---|---|
| Posse claim flag (16×16, palette-swap) | PRI `player_interactions_atlas` row 3 cols 1-2 (posse banner standing/hanging); SE `faction_reputation_symbols_atlas` row 2 col 4 (sheriff flag) | ✅ EXISTS |
| Claim spark (2-3 frame flash) | SE `hazards_disasters_atlas` row 1 col 4 (sparking cable — static); SE `event_markers_atlas` row 4 col 2 (explosion icon) | 🟡 PARTIAL — static spark exists; flash animation missing |
| Passive scrip "+1" floater (4-frame fade) | SE `ui_status_icons_atlas` BOUNTY (money bag); needs animated lift-and-fade | 🟡 PARTIAL — base art exists; animation missing |

## Step 35 — Death Trophy Drop

| Asset | Source | Status |
|---|---|---|
| Generic dog-tag icon (16×16) | SE `evidence_clues_atlas` row 5 col 1 (ID badge); 3P `mixel_top_down_rpg_32x32_v1_7/Items v1.0` likely has tag variants | ✅ EXISTS |
| Trophy drop flash (2-frame) | — | ❌ GAP |

## Step 36 — Crafting Intent

| Asset | Source | Status |
|---|---|---|
| Workshop bench prop | SE `crafting_stations_atlas` (12 workbench variants); SE `interior_furniture_atlas` row 3 col 3 (workbench tools) | ✅ EXISTS — abundant |
| Craft confirmation puff (2-3 frame) | SE `hazards_disasters_atlas` (steam manhole, sparks) — derivable | 🟡 PARTIAL — needs derivation |
| Recipe panel chrome / ingredient slot art | 3P `mixel_top_down_rpg_32x32_v1_7/User Interface v1.0/Topdown RPG 32x32 - UI HotTB & Inv.PNG` | ✅ EXISTS |

## Step 37 — Posse Shared Quest

| Asset | Source | Status |
|---|---|---|
| Posse quest banner (HUD) | PRI `player_interactions_atlas` row 1 col 2 (parade flags); PRI `prototype_ui_icons_atlas` POSSE INVITE/ACCEPTED, QUEST STARTED/COMPLETED | ✅ EXISTS |
| Shared objective minimap pin (8×8) | SE `event_markers_atlas` row 1 col 2 (red pin); reusable | ✅ EXISTS |

## Step 38 — World Tier Zones (Lawless)

| Asset | Source | Status |
|---|---|---|
| Lawless tile overlay (red-tint hatch) | SE `hazards_disasters_atlas` row 6 col 5 (red barrel hazard); SE `event_markers_atlas` row 6 col 6 (DANGER triangle) — needs tile-tint derivation | 🟡 PARTIAL — needs tint pass |
| Boundary marker tile (skull/warning) | SE `hazards_disasters_atlas` row 5 col 4 (warning sign), row 1 col 1 (warning barricade); SE `event_markers_atlas` warning triangle | ✅ EXISTS |
| HUD zone toast banner | PRI `prototype_ui_icons_atlas` (banner-style cells reusable) | ✅ EXISTS |

## Step 39 — Fog of War

| Asset | Source | Status |
|---|---|---|
| Fog tile overlay (solid dark) | Pure ColorRect (already implemented in `WorldRoot.RenderFogOfWar`) — no asset needed | ✅ NA (solved in code) |
| Reveal edge feather tile | Optional 1-pixel gradient — derivable from any opaque tile | 🟡 PARTIAL |

## Step 40 — HUD Minimap

| Asset | Source | Status |
|---|---|---|
| Minimap panel chrome | 3P `mixel_top_down_rpg_32x32_v1_7/User Interface v1.0/Topdown RPG 32x32 - UI Panels & Buttons.PNG` | ✅ EXISTS |
| Minimap dot icons (4×4 colored) | SE `event_markers_atlas` row 1 col 1 (target ring); trivially derivable as ColorRect | ✅ EXISTS |
| Compass tick marks | Trivial in code (4 small lines) | ✅ NA |

## Shop UX Upgrade

| Asset | Source | Status |
|---|---|---|
| Shop bubble panel chrome | 3P `mixel_top_down_rpg_32x32_v1_7/User Interface v1.0/Topdown RPG 32x32 - UI Panels & Buttons.PNG` | ✅ EXISTS |
| Sell bubble panel chrome | 3P (same panel pack — palette-swap) | ✅ EXISTS |
| Hotbar strip (1–9 slot row) | 3P `mixel_top_down_rpg_32x32_v1_7/User Interface v1.0/Topdown RPG 32x32 - UI HotTB & Inv.PNG` | ✅ EXISTS |
| Active hotbar slot highlight | 3P (same UI pack — pulse outline) | ✅ EXISTS |

---

## Tally

- ✅ **EXISTS**: 47 line items
- 🟡 **PARTIAL** (needs slicing / animation derivation pass): 12 items
- ❌ **GAP** (genuinely needs generation): 9 items

### The actual generation budget

The 9 genuine gaps that warrant fresh PixelLab generation:

1. **Paragon aura ring** — animated 2–4 frame glow
2. **Abyssal aura ring** — animated 2–4 frame dark distortion
3. **Wraith speed trail** — 3-frame after-image fade
4. **Downed idle / breath frames** — base model lying down (per direction)
5. **Rescuer carry walk cycle** — base model carrying body, 8 directions
6. **Mounted rider composite** — player atop mount, all directions
7. **Contraband detection flash** — 1-frame red scan tint
8. **Trophy drop flash** — 2-frame ceremonial flash
9. **Expired supply drop fade** — 2-3 frame dissipate

The 12 partial items are mostly one-off **animation derivations** from
existing static art (heat tile tinting, claim spark flash, scrip floater,
mount/dismount burst, etc.) — these are quicker to produce manually with
Python scripts that re-tint or re-arrange existing PNGs than to spend
PixelLab credits on.

### Recommended sequence

1. **Wire the existing atlases first** (~80% of the backlog). Run a slicing
   pass that splits the priority + static-event JPGs into named PNGs and
   updates `PrototypeSpriteModels.cs` / `StructureArtModels.cs` references.
   Zero PixelLab credits.
2. **Author the 12 partial-derivation passes** as small Python scripts in
   `tools/` (e.g. `derive_heat_tint_tiles.py`, `derive_claim_spark_frames.py`).
   Zero PixelLab credits.
3. **Generate only the 9 true gaps** — most are aura/trail animations on the
   existing 32×64 base model, so they share a prompt template. Estimated
   total: ~10–15 PixelLab credits if done as a batch.

---

## Slicing Pipeline (started 2026-04-30)

The Gemini-generated 1024×1024 atlases are NOT laid out on a strict
uniform grid — labels of varying line counts shift each row's icon
position by 10–30 px. A naive `(row * cellHeight)` slice produces label
spillover at row boundaries. Empirically-measured per-row Y offsets are
required.

### Slicer scripts

Each slicer is a one-shot Godot SceneTree script that:
1. Loads a 1024×1024 atlas JPG.
2. Crops a fixed-size icon region from each (row, col) cell using
   `ROW_Y_TOPS[row]` as the top edge and a centered `ICON_WIDTH × ICON_HEIGHT`
   box.
3. Writes named PNGs + a JSON manifest to
   `assets/art/generated/sliced/<atlas_name>/`.

Run from project root:
```powershell
. ./tools/env.ps1
& $GodotConsoleExe --headless --path $ProjectRoot --script res://tools/slice_<atlas>.gd
```

### Sliced so far

| Atlas | Tool | Output | Cells |
|---|---|---|---|
| `karma_priority_prototype_ui_icons_atlas.jpg` | `tools/slice_prototype_ui_icons.gd` | `assets/art/generated/sliced/prototype_ui_icons/` | 36 labeled HUD-event icons (interact, ready_up, match_started, wanted, bounty_claimed, contraband_detected, supply_spawned, supply_claimed, clinic_revive, duel_*, player_*, karma_break, item_*, structure_interacted, posse_*, local_chat, mount, dismount, quest_*, dialogue, entanglement, rumor, witness, evidence, danger_heat, restart) |
| `karma_static_ui_status_icons_atlas.jpg` | `tools/slice_ui_status_icons.gd` | `assets/art/generated/sliced/ui_status_icons/` | 36 generic karma/social status icons (positive, negative, wanted, bounty, witness, trade, posse, rumor, contraband, rescue, duel, theft, evidence, law, clinic, supply_drop, structure_repair, sabotage, chat, faction, mount, downed_status, karma_break, local_proximity, shop, quest, return, bribe, apology, trust_vouch, danger_heat) |

The first slice is **wired into the live HUD** —
`HudController.ResolveEventIconName(eventId)` maps server-event ids to
icon names, `_eventIcon` displays the matched icon next to the event
label, and 9 smoke tests cover the resolver logic.

### Atlases NOT yet sliced (ranked by next priority)

1. **`karma_static_event_markers_atlas.jpg`** — 36 small flat icons
   (target, pin, warning triangle, shield, flag, ring buoy, skull, etc.).
   Useful for minimap markers and tile overlays. ~6×6 grid; appears to be
   no labels, so a uniform-grid slice should work cleanly.
2. **`karma_priority_clinic_rescue_revive_atlas.jpg`** — clinic structure
   variants (bed, stretcher, medic bag, oxygen tank, biohazard barricade,
   surgical light, etc.). For Step 16 wiring.
3. **`karma_priority_supply_shop_loot_atlas.jpg`** — supply drop crate
   (parachute), shop kiosk, chests, ammo crate, weapon case, sack. For
   Step 30 + shop UX.
4. **`karma_priority_wanted_bounty_law_atlas.jpg`** — wanted poster, jail
   bars, handcuffs, badge, evidence bag. For Step 24 Warden visuals.
5. **`karma_priority_structure_world_state_atlas.jpg`** — generator and
   greenhouse damage states; for Step 12 sabotage display.
6. **`karma_static_modular_walls_doors_atlas.jpg`** — modular walls/doors
   per theme; for Step 17–18 path generation and seamless interiors.
7. **`karma_static_interior_furniture_atlas.jpg`** — beds, desks,
   cabinets; for interior-rendering follow-ups.
8. **`karma_static_crafting_stations_atlas.jpg`** — workbench variants;
   for Step 36 crafting UI.
9. **`karma_static_containers_loot_atlas.jpg`** — chests, lockers, sacks;
   for general loot prop variety.

Each subsequent slicer should follow the same pattern: a `debug_*_strip.gd`
that exports the leftmost column for visual measurement, then a
`slice_*.gd` with empirically-calibrated `ROW_Y_TOPS` per row.

### Wiring pattern

For atlases where the cells map cleanly to runtime entities (e.g. a
clinic bed prop in `clinic_rescue_revive_atlas`), wire through
`scripts/Art/StructureArtModels.cs` and `scripts/Art/PrototypeSpriteModels.cs`
following the existing `scifi_*_atlas` wiring.

For atlases that map to HUD/event semantics, follow the
`ResolveEventIconName` pattern: a static helper that takes an id string
and returns an icon name + a refresh hook in `OnLocalSnapshotChanged`.

---

*Updated: 2026-04-30 (initial audit + first slicing pass + HUD event icon wiring).*
