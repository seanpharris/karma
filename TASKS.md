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

- [ ] **Minimap integration into the live HUD** — `HudController.FormatMinimap`
  renders to a string, but no panel currently displays it. Either wire it into
  the developer overlay or add a dedicated minimap panel in the corner.
- [ ] **Fog-of-war client-side rendering** — server filters chunks correctly,
  but `WorldRoot.cs` does not yet render fog overlay tiles for unvisited
  neighbours. Add the dark overlay on chunks within view distance but missing
  from the snapshot.
- [ ] **NPC patrol world-gen integration** — `SetNpcPatrol` is exposed as a
  public method, but `WorldGenerator` does not yet seed patrol routes per NPC.
  Decide on a default cadence (one route per station NPC?) and seed at gen
  time.
- [ ] **Bounty UI** — bounty amount is in `StatusEffects` as `"Bounty: N"` but
  there's no scoreboard or "biggest bounty" leaderboard panel.
- [ ] **Match summary HUD** — `MatchSummarySnapshot` exists; verify it's
  rendered at end of match (smoke test covers data, not UI).
- [ ] **Wraith perk visual** — speed modifier reflects in snapshot but no
  trail/blur sprite. Listed in `ART_NEEDED.md` step 25.
- [ ] **Warden / Wanted visuals** — wanted poster icon listed in
  `ART_NEEDED.md` step 24, not yet wired into `RenderRemotePlayers`.
- [ ] **Contraband detection flash** — listed in `ART_NEEDED.md` step 28.
- [ ] **Supply drop beacon / landing animation** — listed in `ART_NEEDED.md`
  step 30.

---

## P3 — System/architecture work

- [ ] **Replace per-NPC role checks with role tags** — `IsNearClinicNpc` and
  `IsLawAligned` use string contains / boolean flags. As the cast grows, a
  `NpcProfile.Tags` set with constants would scale better.
- [ ] **Server-driven shop catalogues per NPC** — `_seededOffers` exists for
  ad-hoc offers; consider promoting it so each vendor NPC has a
  `VendorCatalogue` rather than every offer being in `StarterShopCatalog`.
  Necessary for "random dealers" mentioned in the original UX request.
- [ ] **PlayerStatus model split** — `_persistentStatusByPlayer` and
  `GetStatusEffectsFor` co-exist. The former is for explicit status effects
  (Poisoned/Burning/etc.); the latter formats *any* derived state as a string.
  Worth a refactor pass to clarify what's persistent vs. derived.
- [ ] **Quest module discovery** — `QuestModuleRegistry` is a static class with
  a hardcoded module list. New modules require editing two places. Consider an
  attribute-based or assembly-scan registration.
- [ ] **Sequence guard for non-stateful intents** — `_lastSequenceByPlayer`
  blocks legitimately-out-of-order intents from a single player. Reasonable for
  movement, less so for `ReadyUp`/`SelectDialogueChoice`. Audit which intent
  types actually need sequencing.

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
