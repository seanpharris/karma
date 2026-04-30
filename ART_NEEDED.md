# Art Assets Needed

Tracks art required for each gameplay step. Append as features are designed.
Use the base model sheet as the reference skeleton for all character animations.

---

## Step 2 — Repair Mission Quest

- **Repair tool icons** (inventory slot art): multi-tool, welding torch (16×16 each)
- **Damaged fixture state**: overlay or alternate tile for structures in "damaged" condition

---

## Step 3 — Delivery Quest

- **Item icons**: FilterCore, DataChip (16×16 each)

---

## Step 4 — Rumor Quest

- **Evidence token icon**: generic "secret document" or "data chip" item (16×16)
- **Exposed/buried outcome overlay**: small stamp/seal icon for quest resolution (optional)

---

## Step 5 — Paragon Favor Perk

- **Paragon aura**: soft glow ring around player sprite (animated, 2–4 frames) when Paragon perk is active
- **Paragon badge**: small icon for HUD karma panel (16×16)

---

## Step 6 — Abyssal Mark Perk

- **Abyssal aura**: dark distortion ring around player sprite (animated, 2–4 frames) when Abyssal perk is active
- **Abyssal badge**: small icon for HUD karma panel (16×16)

---

## Step 7 — Posse Formation

- **Posse leader crown**: small overlay badge drawn above posse leader's sprite (16×16)
- **Posse member tag**: colored dot or bracket rendered above member sprites

---

## Step 8 — Posse HUD Panel

- **Posse panel frame**: HUD window chrome (128×48 or similar, scalable)
- **Health bar segment art**: thin bar fill and border for compact member readouts

---

## Step 9 — Saint/Scourge NPC Behavior

- **NPC reaction emotes**: floating icons above NPCs (heart for welcome, skull for fear, coin for price change) — 16×16 each, 1–2 frame flicker

---

## Step 10 — Chat Tabs

- **Tab bar art**: Local / Posse / System tab graphics (matching HUD style)
- **Unread indicator**: small dot/count badge on inactive tab

---

## Step 12 — Combat Heat Tracking

- **Heat zone overlay tiles**: translucent red tint tile variants (3 intensity levels: warm / hot / critical) drawn above floor layer

---

## Step 14 — Downed State

All frames assume the existing base model sprite sheet orientation (8 directions or 4 directions, match current walk sheet).

- **Downed idle** (lying on ground): 1–2 frames per direction — base model horizontal, arms spread or tucked
- **Downed breath animation**: 2–3 frames subtle chest rise (can be single-direction only if top-down obscures it)
- **Downed countdown overlay**: small timer bar or pulsing outline rendered beneath the downed player

---

## Step 15 — Rescue / Carry Mechanic

Both sets must match the base model's current directional frame count.

- **Rescuer carry animation**: base model walking while holding another body — all walk directions, idle+walk frames (8 frames per direction minimum)
  - Carried player is rendered as a limp overlay on rescuer's back/shoulder
- **Carried (downed) overlay**: slumped base model silhouette that composites onto the rescuer sprite — 1 frame (static, rescuer walk drives motion)

---

## Step 16 — Clinic Recovery

- **Recovery bed prop**: clinic interior structure tile (stretcher or cot), 16×32 or 32×32
- **NPC healing emote**: green cross or pulse icon floating above clinic NPC (16×16, 2-frame)

---

## Step 17–18 — Road / Path Generation & Rendering

- **Road tile set**: at minimum 4 connection variants (straight H, straight V, corner, T/cross) × 2 surface types (dirt path, worn paved) = 8–16 tiles, 16×16 each
- **Road edge blend**: optional 1-pixel feather tiles for dirt↔grass transitions

---

## Step 19 — Mount / Vehicle Entity

- **Vehicle sprite**: hovercycle or cargo cart (recommend hovercycle for sci-fi setting) — all 4 or 8 directions, idle + 2-frame hover/roll animation
  - Recommended size: 32×32 (straddles 2 tiles)
- **Empty parked state**: vehicle with no rider (1 frame per direction)
- **Mounted rider state**: player atop vehicle — all directions, animation aligned to vehicle frames

---

## Step 20 — Mount / Dismount

- **Mount/dismount flash**: brief 2-frame "hop on / hop off" transition overlay (can be a generic action burst, 16×16 centered on player)

---

## Step 22 — Karma Title-Change Broadcast

- **Title-change toast**: brief on-screen banner for "New Saint" / "New Scourge" (no dedicated sprite; HUD text overlay is sufficient, but a 2-frame crown/skull flash near the new holder's name would help)

---

## Step 24 — Warden Perk / Wanted Warrant

- **Wanted poster icon**: small 16×16 badge rendered above or beside a Wanted player's sprite (red border, exclamation mark)
- **Warden badge**: HUD perk icon indicating the player holds the Warden perk (star/shield, 16×16)

---

## Step 25 — Wraith Surge Perk

- **Speed trail**: ghostly after-image or motion blur frames drawn behind the player when SpeedModifier > 1 (2–3 faded copies at decreasing opacity, same sprite frame)
- **Wraith badge**: HUD perk icon (skull outline, 16×16)

---

## Step 26 — Bounty System

- **Bounty tag**: floating scrip-coin icon above a bounty player's sprite (16×16 coin, showing amount or just a generic tag)

---

## Step 27 — Player Status Effects

- **Poisoned overlay**: green dot-pulse aura on player sprite (1–2 frames, 16×16 radial)
- **Burning overlay**: orange flicker above player head (2–3 frames, 16×16)
- **Status icon panel**: small row of 8×8 status icons in HUD (can reuse perk-badge slot)

---

## Step 28 — Contraband Item Tag

- **Contraband Package sprite**: distinct "suspicious parcel" inventory icon (16×16) — current item uses placeholder; a wrapped box with caution tape reads clearly
- **Detection flash**: brief red scan effect over the player when contraband is detected by a law NPC (optional, 1-frame tint)

---

## Step 29 — Lobby / Ready-Up Flow

- **Ready-up indicator**: a checkmark or "READY" badge shown above connected players who have sent ReadyUp (16×16, 1–2 frame pulse)
- **Lobby count panel**: small pre-match overlay showing "X / Y ready" with player names

---

## Step 30 — Supply Drop World Event

- **Supply drop crate sprite**: distinctive cargo container that drops onto a tile (32×32 or 16×16; ideally with a brief 2–3 frame "landing" animation)
- **Drop beacon overlay**: pulsing column or marker rendered above the drop position so it's findable from range
- **Expired drop ghost**: faded version of the crate that dissipates over a 2–3 frame fadeout when the drop times out unclaimed

---

## Step 31 — NPC Patrol Routes

- **Waypoint marker** (debug/dev only): small dotted-circle tile overlay used during world-gen tuning to visualise patrol paths
- *(No new player-facing art needed — patrol uses existing NPC walk frames.)*

---

## Step 32 — Reputation Decay

- **Reputation HUD bar segments**: 3-state segment art for the faction reputation strip (high/neutral/low), 8×8 each — used so the bar reads cleanly even as values drift toward 0

---

## Step 33 — Faction Store Gating

- **Locked offer overlay**: small lock icon (16×16) drawn on shop offers the player can't yet purchase (insufficient reputation)
- **Reputation-required tag** (HUD): a tiny faction badge (8×8) next to gated offer rows so the requirement is glanceable

---

## Step 34 — Station Claim Intent

- **Posse claim flag**: a banner/pennant prop attached to claimed structures (16×16; one variant per posse colour, or a single neutral flag with palette-swap support)
- **Claim spark**: brief 2–3 frame flash centered on the structure when the claim is granted
- **Passive scrip tick**: small "+1" coin floater that drifts up from claimed structures every few seconds (16×16, 4-frame fade)

---

## Step 35 — Death Trophy Drop

- **Generic dog-tag icon**: a 16×16 dog-tag inventory icon used for trophy items ("X's Dog Tag"); name overlay is the differentiator
- **Trophy drop flash**: brief 2-frame icon that floats over the scorer when the tag is awarded

---

## Step 36 — Crafting Intent

- **Workshop interaction prop**: workbench tile (16×32 or 32×32) that signals a craftable structure
- **Craft confirmation puff**: 2–3 frame smoke/spark animation at the workbench when an item is produced
- **Recipe panel chrome**: HUD window for the recipe list (use existing inventory chrome if possible — only ingredient/result row art is new: 16×16 ingredient slots with arrow → output)

---

## Step 37 — Posse Shared Quest

- **Posse quest banner**: a HUD banner (similar to the Saint/Scourge title-change banner) announcing a posse quest start/completion to all members
- **Shared objective marker**: a small posse-coloured pin (8×8) drawn on the minimap/HUD over the active posse-quest target tile

---

## Step 38 — World Tier Zones (Lawless)

- **Lawless tile overlay**: a sparse warning hatch or red tint variant of the floor tiles (16×16, kept subtle so movement isn't impaired)
- **Zone boundary marker**: a 1-tile-wide skull-or-warning border tile rendered along lawless/lawful boundaries (16×16, 4 directions)
- **HUD zone toast**: a short banner sprite for "Entered Lawless Zone" / "Returned to Patrolled Territory"

---

## Step 39 — Fog of War

- **Fog tile overlay**: solid dark tile drawn over unvisited chunks (16×16, full opacity)
- **Reveal edge feathering**: optional 1-pixel gradient tile for the boundary between visited/unvisited chunks

---

## Step 40 — HUD Minimap

- **Minimap panel chrome**: small radar window in the corner of the HUD (96×96 or 128×128) — frame + background
- **Minimap dot icons**: 4×4 colour-coded markers for self (white), friendlies (green), hostiles (red), NPCs (yellow), structures (blue)
- **Minimap compass tick**: optional N/S/E/W tick marks on the panel border (4×4 each)

---

## Shop UX Upgrade (input + dialog)

- **Shop bubble panel**: a HUD popup window (192×128 or similar) opened from a vendor's "Browse wares" dialogue choice; lists item name, price, and a "purchase" highlight on hover
- **Sell bubble panel**: paired window for "Sell items" listing inventory rows with computed sell-back prices
- **Hotbar strip**: 1–9 numbered inventory slot row drawn above the existing inventory display (each slot 16×16 with number badge)
- **Active hotbar slot highlight**: a pulse/glow around the currently-equipped hotbar slot

---

*Updated: 2026-04-29 (steps 30–40 + shop UX)*
