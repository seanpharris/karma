# Known Tasks

Single-source-of-truth checklist for outstanding work. Update as items move from
this list into commits or back into `BRAINSTORMING.md`. Sections are ordered by
likely-priority — adjust freely.

Last compiled: 2026-04-29.

---

## P0 — In-flight follow-ups from the recent session

These are partial/complete-but-incomplete features where the next slice is
already designed.

- [x] **NPC dialogue choice picker UI** — *done 2026-04-29*. E near an NPC
  opens `_dialoguePanel` with a button per `NpcDialogueChoice`. Browse/sell
  choices auto-open the shop overlay (right vendor, right mode); other
  choices send `SelectDialogueChoice` intent. Smoke tests verify open,
  branch routing, and close. See [HudController.cs](scripts/UI/HudController.cs)
  `OpenDialogue` / `SelectDialogueChoice` / `CloseDialogue`, and
  [ServerNpcObject.cs](scripts/World/ServerNpcObject.cs) E-key handler.
- [x] **Shop bubble in-panel selection** — *done 2026-04-29*. Shop overlay
  panel now rebuilds rows as Buttons on each refresh: browse mode shows one
  button per visible offer (disabled when player can't afford), sell mode
  shows one per inventory item, plus a Close row. Click handlers call
  `BuyShopOffer` / `SellInventoryRow` which dispatch `PurchaseItem` /
  `SellItem` intents. See [HudController.cs](scripts/UI/HudController.cs)
  `RefreshShopOverlay`.

---

## P1 — Acknowledged shortcomings of just-shipped work

Things the test suite confirms but a player would notice.

- [x] **Hotbar UX** — *done 2026-04-29*. `_hotbarPanel` renders below the
  HUD with 9 slots; equipped MainHand item is marked with `*`. Refreshes
  on `InventoryChanged`. See [HudController.cs](scripts/UI/HudController.cs)
  `FormatHotbar` / `FindEquippedHotbarIndex` / `RefreshHotbar`.
- [x] **Attack target feedback** — *done 2026-04-29*. HUD now shows a
  dedicated `_targetLabel` line: "Target: NAME (HP/MAXHP, NT)" or
  "Target: none in range." Refreshed on every snapshot.
  `HudController.FindAttackTarget` is the shared helper; `PlayerController`
  uses it for LMB attacks so client and HUD pick the same target.
  *Outstanding follow-up*: in-world reticule / outline sprite (deferred to
  art pass — listed in `ART_NEEDED.md` Step 24/25 area).
- [x] **Lawless zone player feedback** — *done 2026-04-29*. `ProcessMove`
  now tracks `_inLawlessByPlayer` and emits `entered_lawless_zone` /
  `left_lawless_zone` server events on boundary crossings. Lawless-to-
  lawless moves don't emit redundant events. The HUD's
  `FormatLatestServerEvent` will pick these up automatically.
- [x] **Posse quest activation path** — *done 2026-04-29*. New
  `IntentType.StartPosseQuest` + `ProcessStartPosseQuest` path: takes
  `questId` (and optional `giverNpcId`) payload, requires the caller to
  be in a posse, rejects duplicate ids and missing payload. Emits a
  `player_started_posse_quest` event on success on top of the existing
  `posse_quest_started` event from `StartPosseQuest`.
- [x] **Trophy item identity** — *done 2026-04-29*. Trophy id is now
  `trophy_{victimId}_{tick}`. Duplicate display names can no longer
  collide, and repeat Karma Breaks of the same victim across the match
  produce distinct items. Smoke test verifies victim-id and tick suffix
  presence.
- [x] **Crafting recipe table is tiny** — *done 2026-04-29*. `StarterRecipes`
  expanded from 2 to 8 entries: ballistic round (BoltCutters + DataChip),
  energy cell (PowerCell + ChemInjector), flashlight (PortableTerminal +
  PowerCell), stun baton (PracticeStick + PowerCell), grappling hook
  (BoltCutters + MultiTool), and a comedic contraband-package-from-flowers
  laundering recipe in the WhoopieCushion lineage. All ingredients and
  outputs resolve via `StarterItems.TryGetById`.
- [x] **Seamless building interiors** — *full slice done 2026-04-30*.
  Server `BuildingInterior(MinX, MinY, Width, Height, DoorTiles)` enforces
  movement bounds and auto enter/exit on door tiles; `door_opened` event
  fires with `direction=enter|exit`. `PlayerSnapshot.InsideStructureId`
  exposes the viewer's containing structure; `CreateInterestSnapshot`
  scopes the visible player set, NPC set, and map chunks to whichever
  interior the viewer occupies (or excludes interior-only entities when
  outside). `NpcEntity.ResidentStructureEntityId` + `AssignNpcResidency`
  let NPCs live inside structures (Mara/Dallen ready to be assigned).
  `WorldStructureSnapshot` surfaces interior bounds for the client;
  `PlayerController.ApplyInteriorCameraClamp` clamps the camera to
  interior limits when the local player is inside, and unclamps on
  exit. Smoke tests cover snapshot scoping, NPC residency, and the
  door event direction tag.
  *Remaining*: dedicated interior-floor/wall art and door audio cues
  (already listed in `ART_NEEDED.md` and `SOUND_NEEDED.md`).
- [x] **Weapon resource costs: melee→stamina, ranged→ammo** —
  *server done 2026-04-29*. Server now enforces resource costs in
  `ProcessAttack`: ranged weapons (auto-detected from weapon tags
  `pistol`/`smg`/`shotgun`/`rifle`/`energy`/`ballistic`) require ammo
  in the magazine; melee weapons require stamina. `IntentType.Reload`
  consumes one stack of the matching `AmmoItemId` and refills the
  magazine. `BallisticRound` and `EnergyCell` ammo items added. Stamina
  regenerates during `AdvanceIdleTicks` at `StaminaRegenPerIdleTick`.
  Equipping a ranged weapon starts with an empty magazine.
  *Outstanding follow-ups* (move to a fresh entry if you want them as
  separate tasks): expose `CurrentAmmo` / `MaxAmmo` / `Stamina` /
  `EquippedWeaponKind` in `PlayerSnapshot` for HUD readout; rebind R to
  reload (current R = repair-kit-on-peer); art deliverables in
  `ART_NEEDED.md`.

---

## P2 — Polish & coverage gaps

- [x] **Minimap integration into the live HUD** — *done 2026-04-30*. New
  `_minimapPanel` in the corner refreshed each snapshot via
  `FormatMinimap` at radius 6.
- [x] **Fog-of-war client-side rendering** — *done 2026-04-30*. New
  `WorldRoot.ComputeFogChunks` static + `RenderFogOfWar` overlays a dark
  ColorRect on chunks within interest radius that aren't in the visited
  set. Smoke test covers chunk-set delta.
- [x] **NPC patrol world-gen integration** — *done 2026-04-30*. Mara
  ships with a 3-tile clinic patrol; Dallen with a 2-tile shop route.
  Seeded at construction in `SeedStarterNpcs`.
- [x] **Bounty UI** — *done 2026-04-30*. New `_bountyPanel` shows the
  top 5 active bounties parsed from `StatusEffects` via
  `FormatBountyLeaderboard`. Sorted descending; reports "none active"
  when empty.
- [x] **Match summary HUD** — *done 2026-04-30*. New `_matchSummaryPanel`
  becomes visible on `MatchStatus.Finished` and renders the existing
  `FormatMatchSummary(snapshot.MatchSummary)`. Hidden during running /
  lobby.
- [ ] **Wraith perk visual** — speed modifier reflects in snapshot but no
  trail/blur sprite. Listed in `ART_NEEDED.md` step 25.
- [ ] **Warden / Wanted visuals** — wanted poster icon listed in
  `ART_NEEDED.md` step 24, not yet wired into `RenderRemotePlayers`.
- [ ] **Contraband detection flash** — listed in `ART_NEEDED.md` step 28.
- [ ] **Supply drop beacon / landing animation** — listed in `ART_NEEDED.md`
  step 30.

---

## P3 — System/architecture work

- [x] **Replace per-NPC role checks with role tags** — *done 2026-04-30*.
  `NpcRoleTags` constants (`Clinic`, `Vendor`, `Workshop`, `Saloon`,
  `Warden`, `Dealer`, `LawAligned`, `OutlawAligned`); `NpcProfile.Tags`
  + `HasTag(string)` helper. Mara/Dallen tagged. `IsNearClinicNpc` now
  delegates to the generic `IsNearNpcWithTag(position, tag)`.
- [x] **Server-driven shop catalogues per NPC** — *done 2026-04-30*.
  `SeedVendorCatalogue(vendorNpcId, offers)` and `GetVendorCatalogue`
  let any NPC become a vendor without touching the static catalog.
  Templates can use a placeholder vendor id; seed rebinds them on
  attach. `CreateVisibleShopOffers` now merges seeded + static offers
  per visible vendor (was previously only consulting the static set —
  latent bug).
- [x] **PlayerStatus model split** — *done 2026-04-30*. `GetStatusEffectsFor`
  now composes from two private iterators: `GetDerivedStatusEffectsFor`
  (Downed countdown, Karma Break Grace, Attack Cooldown, Inside, Lawless,
  Wanted, Bounty — all recomputed each snapshot from authoritative state)
  and `GetPersistentStatusEffectsFor` (Poisoned/Burning/etc. stored in
  `_persistentStatusByPlayer` and manipulated via `ApplyStatus` /
  `ClearStatus`). Public `GetPersistentStatuses(playerId)` exposes the
  set for tests/HUD. Snapshot output unchanged — internal split only.
- [x] **Quest module discovery** — *done 2026-04-30*. `QuestModuleRegistry`
  now exposes `Register(QuestModule)` (idempotent) and an `All` view.
  Built-in modules are registered at type-init via `Register` calls;
  external modules can plug in at runtime without editing the registry.
- [x] **Sequence guard for non-stateful intents** — *done 2026-04-30*.
  `IsSequenceExempt(IntentType)` whitelists idempotent / unordered
  intents (`SendLocalChat`, `SendPosseChat`, `ReadyUp`,
  `SelectDialogueChoice`, `StartDialogue`, `SetAppearance`); these
  bypass both the stale-sequence reject AND the
  `_lastSequenceByPlayer` update so they can't gum up subsequent
  sequenced intents. Move/Attack/etc. keep the strict guard.

---

## P4 — Idea pool (not yet committed to)

These live in [BRAINSTORMING.md](BRAINSTORMING.md) — over 50 event seeds and
several social system sketches. Worth pulling from when planning the next
roadmap chunk. Highlights:

- **Rumor network** — NPCs gossip simplified summaries of recent server events.
- **Reputation memory** — NPC dialogue/prices subtly shift on long-running
  player patterns.
- **Public witnesses** — crimes/heroics only "land" if witnessed by NPCs,
  cameras, or other players (creates stealth gameplay).
- **Traveling merchant** — periodic event spawning a vendor with rare items
  who remembers protectors and robbers.
- **Theme tag system** — categorize events by mechanical/theme/tone/scale tags
  so the same systems can re-skin between western/space/post-apoc/fantasy.
- **50-event seed pool** — see `BRAINSTORMING.md` sections "50 Event Seed
  Ideas" and "Additional Event Seed Pool" for a long backlog.

---

## P5 — Art backlog

`ART_NEEDED.md` tracks per-step art requirements and is current through step
40 + the shop UX upgrade. Roughly the categories outstanding:

- **Status overlays**: Wraith trail, Warden poster, Wanted icon, contraband
  flash, status effect aura sprites (Poisoned/Burning).
- **HUD chrome**: shop bubble panel, sell bubble panel, hotbar strip, minimap
  panel, ready-up indicator, posse quest banner.
- **World props**: supply drop crate, posse claim flag, workshop bench,
  patrol waypoint marker, lawless zone tile overlay, fog tile overlay.
- **Animations**: downed idle/breath, rescuer carry walk cycle, mount/dismount
  flash, structure damaged variants.

---

## Verification gate (always)

Every code change ends with `tools/test.ps1 → tools/snapshot.ps1 →
tools/check.ps1`, gated on previous exit code 0, then a focused commit on
`develop`.
