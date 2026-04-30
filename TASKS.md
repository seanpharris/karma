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
- [ ] **Shop bubble in-panel selection** — clicking an item row in the Browse
  bubble should send `PurchaseItem`; clicking a row in the Sell bubble should
  send `SellItem`. Today the panel only renders text; you still need keys/CLI
  to actually transact.

---

## P1 — Acknowledged shortcomings of just-shipped work

Things the test suite confirms but a player would notice.

- [ ] **Hotbar UX** — number keys 1–9 equip the Nth inventory item, but there's
  no visible hotbar strip on the HUD telling you which slot is which. Add a
  fixed bottom-of-screen row showing items 1–9 with a highlight on the
  currently-equipped slot. Art listed in `ART_NEEDED.md` ("Shop UX Upgrade").
- [ ] **Attack target feedback** — LMB picks the nearest player in combat range
  and silently shoots. No on-screen indicator of *who* you're targeting before
  the click, no miss/hit feedback beyond the event log. Consider a reticule or
  hover outline.
- [ ] **Lawless zone player feedback** — server emits no `zone_changed` event
  when a player crosses a lawless boundary. Status effect appears in the
  snapshot but not as a transient toast. Add a one-shot `entered_lawless` /
  `left_lawless` event on per-tick boundary crossing.
- [ ] **Posse quest activation path** — `StartPosseQuest` is a public server
  method but no intent or NPC dialogue option triggers it. Wire either an
  `IntentType.StartPosseQuest` or a "posse_outpost"-roled NPC dialogue choice
  that calls it.
- [ ] **Trophy item identity** — trophies use the victim's display name to
  derive an item id, but two players with the same display name would collide
  on the same trophy id. Either include `victimId` or a tick suffix in the id.
- [ ] **Crafting recipe table is tiny** — `StarterRecipes` has only 2 entries.
  Add 4–6 more covering common item categories (weapon repair, food prep,
  ammunition, contraband repackaging).
- [ ] **Seamless building interiors** — going through a structure's door
  should drop the player into a constrained interior view: movement clamps
  to the building's footprint, the world outside is hidden, only the
  interior tiles render. Today entering only sets a status effect (`Inside:
  {name}`) and muffles local chat (Step 11) — movement and rendering still
  use world tiles.
  - Server: extend `WorldStructureEntity` (or a new `BuildingInterior`
    record) with `InteriorBoundsTiles` (rect) and an `InteriorTileMap`
    layout (floor/wall/door/furniture). Reuse the existing
    `_enteredStructureByPlayer` dictionary as the gate.
  - In `ProcessMovePlayer`: when the player is inside, reject moves that
    leave `InteriorBoundsTiles`. Door tile triggers `ExitStructure` instead
    of a normal move.
  - Snapshot: when a player is inside, `CreateInterestSnapshot` returns
    only the interior tile set + the players/NPCs that share the same
    interior, hiding the outside world. (Other players outside do not see
    the inside-player either, except optionally via window tiles.)
  - Client: `WorldRoot` swaps to interior rendering when the local player
    is inside — replace the chunked world tiles with the interior layout
    and hide structures/NPCs outside that interior. Camera bounds clamp
    to the interior rect.
  - Door handling: each structure declares one or more `DoorTilePosition`
    entries. Walking onto a door from outside enters; from inside exits.
    Use the existing Interact intent for explicit door use, but also fire
    on tile-entry for seamlessness.
  - NPCs: NPCs that "live" in a structure (Mara at the clinic, Dallen at
    the saloon) should appear inside, not outside, unless their patrol
    route (Step 31) takes them out a door.
  - Smoke tests: enter through door → bounds enforced, snapshot scoped to
    interior; attempt to walk outside the bounds → rejected; exit through
    door → world view restored; another player outside cannot see the
    inside-player.
  - Art (add to `ART_NEEDED.md`): interior floor + wall tiles per
    building (clinic, saloon, workshop, etc.), door-arrival fade
    transition, optional window cutout tiles for partial outside-view.
- [ ] **Weapon resource costs: melee→stamina, ranged→ammo** — today every
  weapon swings/fires for free; the only friction is the global
  `AttackCooldownTicks = 3`. Split the cost model by weapon kind.
  - Extend the `Weapon(...)` factory in `ItemModels.cs` with a `WeaponKind`
    (`Melee` / `Ranged`) plus per-kind fields:
    - **Melee**: `StaminaCost` (per swing).
    - **Ranged**: `MagazineSize`, `AmmoItemId` (e.g. `"ballistic_round"`,
      `"energy_cell"`, `"arrow"`).
  - Stamina is currently client-side only in `PlayerController` (sprint).
    Promote it to server-authoritative: add `Stamina`/`MaxStamina` to
    `PlayerState`, regen per `AdvanceIdleTicks`, decrement on accepted melee
    attacks. Keep the client-side sprint stamina display but source it from
    the snapshot.
  - Track `_currentAmmoByPlayer: Dictionary<string,int>` server-side, keyed
    by player. Reset on weapon swap and Karma Break. (Only used when current
    weapon is `Ranged`.)
  - In `ProcessAttack`:
    - Lookup the equipped weapon's `WeaponKind`.
    - **Ranged**: reject when ammo == 0 ("click — empty!"); decrement on hit.
    - **Melee**: reject when stamina < `StaminaCost` ("too tired"); deduct on
      hit.
    - Unarmed (no main-hand) keeps current behaviour or costs a small flat
      stamina charge — decide during implementation.
  - Add `IntentType.Reload` + `ProcessReload`: only valid for `Ranged`
    weapons; consume one stack of the matching ammo item from inventory,
    refill magazine to `MagazineSize`. Reject for melee.
  - Surface in `PlayerSnapshot`: `Stamina`, `MaxStamina`, `CurrentAmmo`,
    `MagazineSize`, `EquippedWeaponKind` (so HUD picks the right readout).
  - HUD: dedicated combat readout area showing either `Ammo: 12/30` or
    `Stamina: 64/100` based on equipped weapon, with a "Reload (R)" prompt
    when ranged is below ~25% and a "Tired" warning when stamina is below
    one swing's cost.
  - Input: rebind R to reload. The current R handler is
    `UseRepairKitOnPeerThroughServer` — move that to a different key or to a
    context-sensitive interact.
  - Smoke tests:
    - melee: stamina decrements on swing, attack rejected at low stamina,
      regen happens during AdvanceIdleTicks
    - ranged: ammo decrements on hit, attack rejected when empty, reload
      consumes ammo item and refills, reload rejected for melee
    - snapshot reflects current state for both weapon kinds
  - Art (add to `ART_NEEDED.md`): ammo type icons (16x16), reload-progress
    overlay (3-frame muzzle dim animation), magazine-empty HUD pulse,
    stamina-low fatigue tint on player sprite.

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
