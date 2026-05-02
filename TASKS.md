# Known Tasks

Single-source-of-truth checklist for outstanding work. Update as items move from
this list into commits or back into `BRAINSTORMING.md`. Sections are ordered by
likely-priority — adjust freely.

Last compiled: 2026-05-02.

---

## Parallel-agent work queue

Curated list of tasks an Agent / sub-session can pick up cold while the
main session works on art / theme tuning. Each entry is **self-contained**
— acceptance criteria, file pointers, and a known-good test path that
doesn't conflict with parallel art work.

**Conflict guidance for the agent:** the main session is currently
touching art assets, theme registry tile picks, and LPC composer scripts.
Avoid editing:

- `scripts/World/ThemeArtRegistry.cs` (active tuning)
- `scripts/World/WorldConfig.cs` (theme default)
- `tools/lpc_compose_random.gd` / new `lpc_compose_*.gd`
- `assets/art/sprites/lpc/themes/*.json` (theme bundles being authored)
- `assets/art/generated/lpc/*` (composed atlases — regenerate via tool)

Anything else is fair game. The verification gate for every task is
`tools/test.ps1` exit 0 (green build + Godot smoke tests).

### Ready: data-driven catalogs (low conflict, high value)

1. **Drug registry + addiction tracker — server wiring + status emission.** — *done 2026-05-02*.
   - Foundation already exists in
     [scripts/Data/DrugModels.cs](scripts/Data/DrugModels.cs)
     (`DrugCatalog`, `DrugDefinition`, `DrugExposure`).
   - Missing piece: server-side wire-up. When a player uses a `GameItem`
     whose id matches a registered drug id, `ProcessUseItem` should:
     1. Look up the drug via `DrugCatalog.TryGet`.
     2. Apply the `OnUse` effects (heal/stamina/hunger/karma deltas).
     3. Record exposure in a new `_drugExposureByPlayer:
        Dictionary<string, Dictionary<string, DrugExposure>>` keyed by
        player id then drug id.
     4. Emit the `OnUse.StatusKind` as a derived status effect for
        `OnUse.DurationTicks`.
     5. On every tick, check `DrugExposure.InWithdrawal(...)` and emit
        `Withdrawal.StatusKind` when the grace expires.
   - Acceptance: smoke test exercising one drug → status appears →
     duration elapses → withdrawal kicks in.
   - Hint: model after the existing `Hunger`/`Starving` derived status
     code in
     [AuthoritativeWorldServer.GetStatusEffectsFor](scripts/Net/AuthoritativeWorldServer.cs).

2. **Loot table refactor — kill drops + container scavenge.** — *done 2026-05-02*.
   - `ScheduleSupplyDropFromTable` already exists. Extend the same
     `LootTableCatalog` lookup to:
     - Karma-break drops (currently inline in
       `ProcessKarmaBreak`/related). Use
       `LootTableCatalog.DownedPlayerDropsId`.
     - Container interact pickup paths. Use
       `LootTableCatalog.ContainerScavengeId`.
   - Each call site: replace the inline item array with a roll against
     the table.
   - Acceptance: existing tests still green; new test asserts that
     downed-player drops sample only items from the
     `downed_player_drops` table.

3. **Cleanliness / restroom mechanic.** — *done 2026-05-02*.
   - Add `Cleanliness` (0-100) to
     [`PlayerState`](scripts/Data/PlayerModels.cs), default 100.
   - Decay during `AdvanceIdleTicks` (similar to hunger; pick a rate
     ~2x slower than hunger so players notice but it isn't punishing).
   - Steeper decay after combat: in `ProcessAttack` after a hit,
     subtract a constant from both attacker + target cleanliness.
   - Add a `restroom` interactable structure kind (server-seedable;
     drives `ApplyShift`/`ApplyEffect` to reset cleanliness to 100).
   - At `Cleanliness <= 25`, derive a `Dirty` status; at 0, `Filthy`.
   - Acceptance: smoke test covering decay over time, combat-induced
     drop, and reset via restroom interact.

4. **Witness propagation — extend the event log.** — *done 2026-05-02*.
   - Each `ServerEvent` should optionally carry a `Witnesses` field
     listing player ids + npc ids in line-of-sight when the event fired.
   - Add a helper `BuildWitnessSet(position, radiusTiles)` to
     `AuthoritativeWorldServer` that returns the set.
   - Wire `ProcessAttack` and other "reportable" events to populate it.
   - Karma swing (`ApplyShift`) scales by witness count: minimum
     swing if no witnesses, max if 5+.
   - Acceptance: attack with no nearby witnesses produces less karma
     change than the same attack near 5 NPCs.

### Ready: completion of in-flight follow-ups

5. **Hunger move-speed debuff.** — *done 2026-05-02*.
   - The `Hungry` status already appears at hunger ≤ 25 and `Starving`
     at 0 (see
     [`AuthoritativeWorldServer.GetStatusEffectsFor`](scripts/Net/AuthoritativeWorldServer.cs)).
   - Wire those into the movement code:
     `SnapshotBuilder.CalculateSpeedModifier` already handles wraith;
     extend it to multiply by `0.85` for `Hungry` and `0.6` for
     `Starving`.
   - Acceptance: snapshot speed modifier reflects the lower value;
     smoke test asserting Hungry < Wraith < Starving as multipliers.

6. **NPC dialogue tree — second NPC binding (Dallen).** — *done 2026-05-02*.
   - Mara is unbound (legacy procedural choices). Wire Dallen to a new
     `DialogueRegistry.DallenShopkeeperTreeId` tree that branches
     between "browse wares" / "ask about Mara" / "leave".
   - Tree definition lives in
     [scripts/Data/DialogueModels.cs](scripts/Data/DialogueModels.cs)
     `DialogueRegistry`. Add it next to `MaraClinicTreeId`.
   - Set `StarterNpcs.Dallen.DialogueTreeId =
     DialogueRegistry.DallenShopkeeperTreeId`.
   - Acceptance: existing smoke tests still pass; new test walks the
     tree end-to-end with `dialogue_advance:` action ids.

### Ready: text / data theming (zero art dependency)

7. **Medieval text-theming pass.** — *done 2026-05-02*.
   - Rename sci-fi-flavoured names to medieval equivalents WITHOUT
     touching gameplay logic. Targets:
     - `StarterNpcs.Mara` (Clinic Mechanic → Blacksmith)
     - `StarterNpcs.Dallen` (Clinic Bookkeeper → Tavernkeeper)
     - `StarterFactions` ("Free Settlers" → "Village Freeholders" or
       similar)
     - `WorldGenerator.SocialStations` archetype names + descriptions
   - Keep the IDs unchanged so existing tests + saves don't break;
     only the display name / description / role text fields change.
   - Acceptance: smoke tests still green; manually re-skim NPC dialogue
     and event strings for residual sci-fi terms.

8. **Match replay log.** — *done 2026-05-02*.
   - Per-tick file sink: write `(tick, intent, snapshot delta)` rows
     to a sidecar file when a `--replay` config flag is set.
   - Loader scaffold: read the file back and reconstruct the snapshot
     stream tick-by-tick.
   - Useful for bug repros and balance review.
   - Acceptance: round-trip test — record a 10-tick session, replay
     it, assert the reconstructed snapshots match the originals.

### Ready: UI

9. **Drag-and-drop hotbar slot assignment.** — *done 2026-05-02*.
   - HUD-only change in
     [`HudController.cs`](scripts/UI/HudController.cs).
   - Let the player drag an inventory row onto a hotbar slot to bind
     it. Persist the binding in `_hotbarBindings: Dictionary<int,
     string>` keyed by slot index → item id.
   - Pressing 1-9 dispatches `EquipItem` for the bound id (overrides
     the current "first matching" logic).
   - Acceptance: smoke test for the binding map; manual test: drag
     ration onto slot 3, press 3, observe ration equipped.

### Ready: theme NPC roster server wiring

10. **Medieval NPC server wiring — appearance + interactions.** — *done 2026-05-02*.
    - Data is fully in place. See
      [docs/medieval-npc-randomization.md](docs/medieval-npc-randomization.md).
      `assets/themes/medieval/theme.json` carries 60 NPCs each with
      `appearance_options[]` (variant LPC bundle ids),
      `relationships[]` (typed graph with `friend` / `rival` /
      `family` / `creditor` / `lover` / `knows_secret` / etc.),
      `tags[]` (role tags). Top-level `theme.interactions` has
      `greetings_pool` / `reactions_pool` / `gossip_templates`
      keyed by role tag.
    - Server work:
      1. Theme loader. New `MedievalThemeData` class that reads
         `assets/themes/medieval/theme.json` once at world boot; cache
         `npc_roster`, `interactions`, `location_archetypes`,
         `factions`.
      2. Spawn-time appearance pick. At NPC spawn, pick
         `appearance_options[hash(WorldId, npcId) % len]` and store on
         the `NpcEntity`. Surface in `NpcSnapshot` (new
         `LpcBundleId` field) so the renderer loads the right composite.
      3. Greeting injection. When `ProcessStartDialogue` opens against
         a bound NPC, sample a line from
         `interactions.greetings_pool[role_tag]` and prepend it to the
         root dialogue text.
      4. Gossip dispatch. Add `dialogue_advance:gossip` action id;
         when selected, pick a relationship from the NPC's
         `relationships[]` (weighted by intensity, biased toward
         rival / knows_secret / creditor), fetch a template from
         `gossip_templates[role_tag]`, substitute `{relation_name}`,
         return as next node text.
    - Acceptance: smoke tests for theme load + bundle pick
      determinism; a synthetic encounter test asserts the same NPC in
      the same world always emits the same APPEARANCE pick across
      runs (per-encounter greeting / gossip lines vary, that's fine).

### Ready: round 2 — medieval-flavoured gameplay + system follow-ups

Drafted after round 1 (#1-#10) all landed. New conflict guidance: avoid
editing `assets/themes/medieval/theme.json`,
`assets/art/sprites/lpc/themes/*.json`, and the LPC composer scripts —
those are the active art surface. Everything else is fair game.

11. **Medieval crafting recipes.** — *done 2026-05-02*.
    - Replace [`StarterRecipes`](scripts/Data/CraftingModels.cs) entries
      with medieval-flavoured equivalents while keeping recipe IDs
      stable. Mappings:
      - "ballistic round" → arrow (yew + feather + iron tip)
      - "energy cell" → blessing oil (herb + wax + holy water)
      - "flashlight" → torch (wood + cloth + oil)
      - "stun baton" → cudgel (wood + iron strip)
      - Add: long sword, short bow, healing tincture, lock pick set.
    - Output `GameItem` ids stay sci-fi-named for now (tracked in
      `theme.json → items_theming` rename map); the recipe
      ingredients should be other existing items where possible.
    - Acceptance: smoke tests still green; manual diff of
      `StarterRecipes` shows medieval ingredient lists.

12. **Medieval quest pack.** — *done 2026-05-02*.
    - Add 4-6 quests under
      [`StarterQuests`](scripts/Data/QuestModels.cs). Each quest has a
      giver NPC drawn from `assets/themes/medieval/theme.json →
      npc_roster[]` so the new medieval NPCs become quest givers.
    - Quest seeds (each can ship as a one-step or three-step quest):
      - Garrick: "Find my stolen smithing tools" (recover quest)
      - Father Calden: "Recover the missing reliquary" (escort/find)
      - Captain Wace: "Root out the Ash Hollow bandit camp" (hunt)
      - Meri Brindle: "Track down who's skimming my ale" (investigate)
      - Ysolt: "Deliver these herbs past the gate guards" (smuggle)
      - Brother Velmont: "Copy the chapel gospel before the visit" (escort)
    - Acceptance: each quest's `GiverNpcId` resolves to a real medieval
      NPC; `StarterQuests.All` count grows; existing tests still green.

13. **Faction reputation HUD panel.** — *done 2026-05-02*.
    - The `FactionLedger` already tracks standing per faction (-100..+100).
      Add a small HUD panel listing each faction with its current value
      and a one-line mood label (Hostile/Wary/Neutral/Friendly/Loyal).
    - File: [`HudController.cs`](scripts/UI/HudController.cs). New
      `_factionPanel` + `RefreshFactionPanel(snapshot)`. Existing
      `FormatBountyLeaderboard` is the closest pattern.
    - Acceptance: smoke test for the formatter; visual-only verification
      in-game by walking past a vendor.

14. **Cleanliness debuff effects on shopkeepers.** — *done 2026-05-02*.
    - `Cleanliness` is now tracked + has `Dirty` (≤ 25) and `Filthy`
      (= 0) status entries. Need to wire those to behaviour:
      - `Dirty`: vendor prices +20% in `ProcessPurchaseItem`.
      - `Filthy`: vendor refuses; reject with "You reek. Wash, then
        come back." in `ProcessPurchaseItem`.
    - Apply via a small `CleanlinessPriceModifier(player)` helper.
    - Acceptance: smoke test buying as Filthy → reject; as Dirty →
      higher final cost; as Clean → unchanged.

15. **Drug withdrawal tick.** — *done 2026-05-02*.
    - `DrugExposure` already tracks per-(player,drug) cumulative load +
      last dose tick. The status emission for the active `OnUse` effect
      is wired. Missing: every-tick check in `AdvanceIdleTicks` that
      iterates exposure entries and emits `Withdrawal.StatusKind` once
      grace ticks expire while the player is still addicted.
    - Apply minor HP/stamina penalty per tick during withdrawal (use the
      `Withdrawal` effect's existing fields).
    - Acceptance: smoke test using a drug → wait grace ticks → assert
      withdrawal status appears; assert HP/stamina drop while withdrawn.

16. **Witness-driven NPC reporting.** — *done 2026-05-02*.
    - Witness propagation now records per-event witnesses. New behaviour:
      when an NPC is in the witness set of a `karma_break` or
      `player_attacked` event, schedule a `crime_reported` event ~10s
      later (configurable), with the reporting NPC's path biased toward
      the nearest law-aligned NPC (captain, magistrate, or guard).
    - Adds a "consequences arrive later" loop without needing a
      full AI tree.
    - Acceptance: smoke test — attack with a known law-aligned NPC in
      witness range, advance 10s of ticks, assert a `crime_reported`
      event fires citing the witness id.

17. **Day/night phase counter.** — *done 2026-05-02*.
    - Extend `AdvanceMatchTime` (or `AdvanceIdleTicks`) with a simple
      `MatchPhase` enum: Dawn / Morning / Noon / Afternoon / Dusk /
      Night. Phase transitions every N ticks (configurable; default
      6 phases over a 30-min match = ~5 min each).
    - Surface in `MatchSnapshot.Phase` so HUD + downstream systems can
      read it.
    - Acceptance: smoke test asserts phase advances at the right tick
      counts; HUD panel optionally renders the phase string (stretch).

18. **NPC ambient position drift by phase.** — *done 2026-05-02*.
    - Each NPC role gets a daytime/night-time anchor location id. During
      day phases, drift toward day-anchor; at night, drift to
      night-anchor. Anchors live in
      `assets/themes/medieval/theme.json → location_archetypes` matched
      by faction or role tag.
    - Cheap implementation: each tick, move 1 tile toward the current
      anchor with probability 1/N (so most ticks they stay still).
    - Acceptance: smoke test — over a simulated phase transition, the
      smith ends closer to the smithy at noon than at midnight.

19. **NPC tooltip on player approach.** — *done 2026-05-02*.
    - When the player is within 2 tiles of any NPC, render a small
      tooltip above the HUD showing `name • role • faction`. Hides when
      out of range.
    - File: [`HudController.cs`](scripts/UI/HudController.cs). Refresh
      on every snapshot. No new server intent.
    - Acceptance: visual verification + smoke test for the formatter.

20. **Save / restore local player state.** — *done 2026-05-02*.
    - On clean exit, write the local player's snapshot (karma, scrip,
      inventory ids, position, equipment) to
      `user://prototype_save.json`.
    - On scene `_Ready`, if the file exists, restore it before the
      first `RegisterPlayer`.
    - File: [`GameState.cs`](scripts/Core/GameState.cs). New
      `SaveLocalPlayer()` + `LoadLocalPlayer()`.
    - Acceptance: smoke test round-trips a save file (write fields →
      read fields → all match).

### Ready: medieval audio research + curation + wiring

21. **Medieval audio — research, curate, license, wire.**
    - Plan + license rules + targets are spelled out in
      [docs/medieval-audio-inventory.md](docs/medieval-audio-inventory.md).
      Read it first; it lists every cue, the license-safe sources, and
      the wiring contract.
    - User-flagged starting source:
      <https://pixabay.com/music/search/medieval%20game/> (Pixabay
      Content License — free for commercial use, no attribution
      required, not redistributable as a standalone asset). Use this as
      the first stop for music; fall back to OpenGameArt (CC0 / CC-BY /
      OGA-BY), Freesound (CC0), Kevin MacLeod (CC-BY 4.0) for SFX +
      additional music.
    - Sub-tasks:
      1. Research + triage candidates for each row in the doc's "Music"
         and "SFX" tables. For every candidate clip, capture: source
         URL, author, license id, attribution string (if required),
         intended cue id.
      2. Download + commit the curated picks under
         `assets/audio/music/` and `assets/audio/sfx/` using the **exact
         paths** that
         [`AudioEventCatalog.BuiltInClips`](scripts/Audio/AudioEventCatalog.cs)
         already references, so no code wiring is needed for the SFX
         set. New music slots (`TavernInterior`, `ChapelInterior`) need
         the enum + sample load added to
         [`PrototypeMusicPlayer.cs`](scripts/Audio/PrototypeMusicPlayer.cs).
      3. For the existing `SandboxCalm` / `EventTension` /
         `ScenarioAmbient` themes: gate the procedural generator behind
         "no file present at the expected path"; when a file exists,
         load + loop it via `AudioStreamPlayer` instead.
      4. Create `assets/audio/CREDITS.md` (new) — one entry per clip
         with license fields per the doc. Append a "Medieval audio"
         section to `THIRD_PARTY_NOTICES.md` mirroring the same author /
         license summary.
    - Conflict guidance: do not edit
      `assets/themes/medieval/theme.json` (the `music` block already
      points at the three theme enum names, and that's correct — leave
      it). Do not edit `SOUND_NEEDED.md` (the canonical sci-fi/western
      cue list — medieval re-skins live in the new doc). **Important:**
      the main session is concurrently refactoring theme dispatch
      (Phase 1: `ThemeRegistry` + `ThemeDefinition`). When you touch
      [`PrototypeMusicPlayer.cs`](scripts/Audio/PrototypeMusicPlayer.cs),
      do **not** refactor how `MusicTheme` is selected — leave the
      `[Export] MusicTheme` property intact. Only add the
      file-presence gate + `AudioStreamPlayer` loader for the existing
      enum values, plus the new `TavernInterior` / `ChapelInterior`
      enum entries. The main session will wire theme→enum selection on
      its side after both branches merge.
    - Acceptance:
      - `tools/test.ps1` exits 0.
      - Every shipped audio clip is listed in `assets/audio/CREDITS.md`
        with a valid license id and source URL.
      - In-game manual check: opening a door triggers the new
        `door_opened` clip; opening the prototype scene plays the
        medieval music bed instead of the procedural one.
    - License hard rule: refuse any clip with a non-commercial,
      no-derivatives, or unclear license — even if it sounds perfect.

### Ready: round 3 — agent 1 batch (gameplay/UI, theme-infrastructure-safe)

Drafted 2026-05-02 while agent 2 works on #21 (audio) and the main
session works on Phase 1/2/3 of modular-theme refactoring. **Do not
edit these files** (they're being actively refactored or owned by
agent 2):

- `scripts/World/ThemeRegistry.cs` (new — Phase 1)
- `scripts/World/ThemeArtRegistry.cs`
- `scripts/World/WorldGenerator.cs`
- `scripts/World/WorldRoot.cs`
- `scripts/World/WorldConfig.cs`
- `scripts/Audio/PrototypeMusicPlayer.cs`
- `scripts/Audio/AudioEventCatalog.cs`
- `scripts/Data/MedievalThemeData.cs`
- `scripts/Data/StarterNpcs` / `StarterFactions` / `StarterRecipes` /
  `StarterQuests` / `DialogueRegistry` / `LootTableCatalog`
- `assets/themes/**/theme.json`
- `assets/audio/**`
- `SOUND_NEEDED.md`, `docs/medieval-audio-inventory.md`

Anything else is fair game. Each task ends with `tools/test.ps1` exit 0.

22. **Fix the 5 currently-failing smoke tests.** — *done 2026-05-02*.
    - As of the Phase 1 verification run on 2026-05-02 the build is
      clean (0 errors) but five smoke-test assertions fail. They look
      like aftermath from in-flight round-2 work where tests weren't
      updated to match. For each, decide whether to bump the assertion
      or fix the implementation; commit the choice with a one-line
      reason.
    - Failures (line numbers from `scripts/Tests/GameplaySmokeTest.cs`):
      1. `starter item catalog exposes all prototype items. Expected
         37, got 40.` — assertion at line ~1637. Likely the medieval
         crafting work added 3 items and missed the count bump.
         Verify the new items are intentional and bump to 40.
      2. `vendor NPC dialogue includes 'sell_items' choice` — find the
         assertion (grep the message), check whether `DialogueRegistry`
         dropped the choice or the test name drifted.
      3. `crime_reported event cites the reporting witness NPC.
         Expected guard_reporter, got mara_venn.` — task #16 wired
         witness reporting; the picker must be selecting the wrong NPC
         from the witness set. Check the law-aligned-NPC scoring.
      4. `FormatFactionPanel shows loyal standings for positive high
         reputation` — task #13's HUD formatter. Likely a threshold or
         label string off-by-one.
      5. `empty hotbar binding clears the assigned slot. Expected ,
         got ration_pack.` — task #9 follow-up. The clear path doesn't
         actually clear the binding map.
    - Acceptance: `tools/test.ps1` exits 0 with all five back to PASS.

23. **Quest log HUD panel.** — *done 2026-05-02*.
    - Player has no in-game view of accepted quests beyond the dialogue
      that started them. Add a `_questLogPanel` to
      [`HudController.cs`](scripts/UI/HudController.cs) that lists each
      active quest's `Title`, current step text, and step counter (X/Y).
    - Hotkey: `J` toggles the panel. Refresh from `snapshot.Quests`
      every snapshot tick.
    - Mirror the `RefreshFactionPanel` pattern (panel + label + format
      helper). Add `FormatQuestLog(IReadOnlyList<QuestSnapshot> quests)`
      next to `FormatFactionPanel`.
    - Acceptance: smoke test for the formatter (empty list, 1 quest, 3
      quests with mixed step indices); manual verification: accept a
      quest, press `J`, see it listed.

24. **Active status-effect icon strip in HUD.** — *done 2026-05-02*.
    - The server already emits per-player status effects (Hungry,
      Starving, Wraith, Poisoned, Burning, Chilled, Silenced, Dirty,
      Filthy, Withdrawal). HUD currently surfaces only a few; add a
      compact icon strip near the health bar that shows every active
      status with a tooltip on hover.
    - Files: [`HudController.cs`](scripts/UI/HudController.cs) (new
      `_statusStrip` HBoxContainer + `RefreshStatusStrip(snapshot)`).
      Use a colored circle Label for each status until art lands; the
      icon resolution can be a placeholder dictionary
      `status_id → glyph_text + color`.
    - Acceptance: smoke test for the formatter (renders one entry per
      active status, sorted by status id); manual verification: poison
      yourself in-game, see the entry appear.

25. **Bounty board structure UI.** — *done 2026-05-02*.
    - When the player interacts with a `notice-board` or
      `broadcast-tower` structure, open a `_bountyBoardPanel` listing
      the current top-N wanted players (read from `snapshot.Match` /
      `MatchSnapshot` — find the existing wanted leaderboard helper).
    - File: [`HudController.cs`](scripts/UI/HudController.cs). Wire the
      open trigger from the existing structure interact path
      (`HandleInteractResult` or similar — grep for `notice-board`).
    - Acceptance: smoke test that the formatter produces the expected
      row for each wanted entry; manual verification: gain a bounty,
      walk to a notice board, interact, see your name on the list.

26. **Reputation decay tick.** — *done 2026-05-02*.
    - `FactionLedger` tracks per-player per-faction standing. Each
      hour-equivalent of ticks (configurable; default 600 ticks),
      drift every standing toward 0 by 1 point. Don't decay below
      `|2|`; that's the dead band.
    - File: [`AuthoritativeWorldServer.cs`](scripts/Net/AuthoritativeWorldServer.cs).
      Add `_reputationDecayTickCounter` field; tick logic next to
      `AdvanceIdleTicks`.
    - Acceptance: smoke test: set a player's standing to +50, advance
      600 ticks, assert new standing is +49; advance enough ticks to
      reach the dead band, assert standing freezes at +2.

27. **Match summary highlights — per-player bests.** — *done 2026-05-02*.
    - `MatchSummarySnapshot` already lists top karma / wanted leaders.
      Extend it with a `Highlights` field: a dictionary keyed by
      player id, each value a `PlayerMatchHighlights` record carrying
      `MostKarmaGained`, `MostKarmaLost`, `LongestSpree`,
      `BountyClaimed`, `RescuesPerformed` (0-5 ints; the source data
      is already in the per-tick stats the server tracks).
    - Files: [`SnapshotModels.cs`](scripts/Data/SnapshotModels.cs),
      [`AuthoritativeWorldServer.cs`](scripts/Net/AuthoritativeWorldServer.cs)
      (`BuildMatchSummary` already exists). HUD: extend the summary
      panel to render the local player's highlight row.
    - Acceptance: smoke test populating bested fields, asserting the
      MatchSummarySnapshot round-trips them; manual: finish a match
      and see the highlights row.

28. **Posse name + leader designation.** — *done 2026-05-02*.
    - Posses are currently anonymous — no name, no formal leader.
      Extend the posse data model with `Name` (auto-generated 2-word
      "{adjective} {animal}" picker; ~30×20 word lists hardcoded for
      now) and `LeaderId` (the inviter at `InvitePosse` time;
      transferable via a new `TransferPosseLeadership` intent).
    - Files: [`PosseModels.cs`](scripts/Data/PosseModels.cs),
      [`AuthoritativeWorldServer.cs`](scripts/Net/AuthoritativeWorldServer.cs).
      HUD: surface the posse name in the existing posse panel.
    - Acceptance: smoke test asserts posse name is generated on
      formation + sticks across snapshots; new test for leadership
      transfer.

29. **Item rarity tag + HUD tinting.** — *done 2026-05-02*.
    - Add `Rarity` (Common / Uncommon / Rare / Contraband) to
      [`GameItem`](scripts/Data/ItemModels.cs); default Common; flag
      contraband items via the existing `IsContraband` field.
    - HUD inventory rows tint by rarity (gray / green / blue / red).
    - Files: `ItemModels.cs`, `HudController.cs` inventory render
      path. Don't touch crafting recipes or starter items count
      (let task #22 handle the count drift).
    - Acceptance: smoke test for the rarity field on a couple of items;
      manual: open inventory and see contraband items tinted.

30. **First-run tutorial popup.** — *done 2026-05-02*.
    - On the very first launch (detect via `user://first_run.json`
      absence), show a one-time popup over the gameplay scene:
      "Welcome to Karma. WASD/arrows to move, E to interact, T to
      chat, J for quest log, Esc for menu." Dismissible with Enter or
      Esc; touch the marker file on dismiss.
    - File: [`HudController.cs`](scripts/UI/HudController.cs). New
      `_tutorialOverlay` panel + `ShowFirstRunTutorial()` called from
      `_Ready` after layout.
    - Acceptance: smoke test that the marker write/read works; manual:
      delete the marker, launch, see the popup; dismiss, relaunch,
      no popup.

### Ready: round 4 — agent 1 batch (gameplay + theme-aware UI)

Drafted 2026-05-02. Same conflict guidance as round 3 (do not touch
ThemeRegistry / ThemeArtRegistry / WorldGenerator / WorldRoot /
WorldConfig / AuthoritativeWorldServer constructor, Audio/* files,
Starter* catalogs, MedievalThemeData, ThemeData, theme.json files).

31. **UI / pause menu / HUD palette matches active world theme.** — *done 2026-05-02*.
    - User-flagged 2026-05-02: pause menu + HUD chrome should re-skin
      with the world theme so a medieval round looks medieval and a
      boarding-school round looks boarding-school.
    - Add a `UiPalette` record with theme-keyed colors (panel
      background, panel border, accent, text, dim text, danger,
      success). Register one palette per supported theme.
    - File: new `scripts/UI/UiPaletteRegistry.cs` (mirror the module+
      registry pattern of `ThemeRegistry`). Lookup is keyed by the
      same theme id strings (`medieval`, `boarding_school`,
      `western_sci_fi`).
    - Wiring: in [`HudController.cs`](scripts/UI/HudController.cs)
      and the pause-menu / Escape overlay, on `_Ready` (or on theme
      change once the BeginNewRound API lands), call
      `UiPaletteRegistry.Get(activeTheme)` and apply the colors via
      `AddThemeColorOverride` to each panel/label that currently has
      a hardcoded color.
    - **Do not** touch `ThemeRegistry.cs` itself; the active theme id
      should be read via `GameState` or `ServerSession` (whichever
      already exposes the world). If neither does yet, expose a
      `string ActiveThemeId` on `ServerSession` that reads from the
      authoritative server's new `ThemeId` property.
    - Three concrete palettes to seed:
      - **medieval**: warm parchment / brown / brass; #f4e6c7 bg,
        #8b6f47 border, #5d2a1f accent, #2a1810 text, #b68b65 dim.
      - **boarding_school**: deep green / mahogany / gold; #1f3a2b bg,
        #5c2a1e border, #c9a64a accent, #f0e9d6 text, #95a486 dim.
      - **western_sci_fi**: dust + brushed-metal blue; #2b2f36 bg,
        #4a5563 border, #c98847 accent, #d8d8d8 text, #7a8290 dim.
    - Acceptance: smoke test that `UiPaletteRegistry.Get(...)` returns
      the right palette per theme + falls back gracefully for unknown
      theme strings; manual: launch with `medieval` (default), open
      pause menu, see warm/parchment chrome; switch the prototype
      theme to `boarding_school`, see the green/mahogany chrome.

32. **Equipment durability + repair flow.** — *done 2026-05-02*.
    - Add `Durability` (current/max ints) to
      [`GameItem`](scripts/Data/ItemModels.cs); default 100/100; weapons
      lose 1 on attack, tools lose 1 on use, armor loses 1 on incoming
      hit. At 0 durability the item is "broken" — usable interactions
      reject with a denial event.
    - Repair path: a new `RepairItem` server intent paired with a
      smithy-station-only check (`structure.Category == "workshop"`
      already exists). Repair costs scrip proportional to missing
      durability (1 scrip per 5 points, min 1).
    - Files: `ItemModels.cs`,
      [`AuthoritativeWorldServer.cs`](scripts/Net/AuthoritativeWorldServer.cs)
      (add new intent in `ProcessIntent` switch + handler),
      [`HudController.cs`](scripts/UI/HudController.cs) (durability
      bar on equipped weapon).
    - Acceptance: smoke test attack-until-broken (durability hits 0,
      next attack rejected); smoke test repair near a smithy
      (durability restored, scrip deducted).

33. **Combat log scrolling overlay.** — *done 2026-05-02*.
    - HUD currently shows event toasts that fade. Add a persistent
      combat log panel (last 20 events) toggled by `L`. Each row
      includes the event tick, formatted summary, and an icon chip
      keyed off the existing `ResolveEventIconName` path.
    - File: [`HudController.cs`](scripts/UI/HudController.cs). New
      `_combatLogPanel` + `RefreshCombatLog(snapshot.ServerEvents)`.
    - Use a fixed-height `ScrollContainer` with newest at the bottom;
      auto-scroll on append unless the user has scrolled up.
    - Acceptance: smoke test for the formatter (formats 5 mixed
      events into the right rows); manual: trigger several events,
      press `L`, see them stacked.

34. **Mount inventory bag.** — *done 2026-05-02*.
    - When a player is `Mounted` on a `MountEntity`, `E` near the
      mount opens a small bag panel that holds up to 8 items. Items
      transfer between player inventory and bag via drag-and-drop or
      right-click.
    - Bag persists on the `MountEntity` even when the player
      dismounts; the next rider sees the same items. Items dropped
      from a destroyed mount fall to the tile.
    - Files: extend `MountEntity` in
      [`ServerIntent.cs`](scripts/Net/ServerIntent.cs) with
      `IReadOnlyList<string> BagItemIds`; add
      `MountBagTransfer` intent;
      [`AuthoritativeWorldServer.cs`](scripts/Net/AuthoritativeWorldServer.cs)
      handler;
      [`HudController.cs`](scripts/UI/HudController.cs) panel.
    - Acceptance: smoke test transferring an item to the bag,
      dismounting, remounting, item still in the bag.

### Ready: round 4 — agent 2 batch (audio additions, all in scripts/Audio)

Drafted 2026-05-02. Conflict guidance: do not touch the medieval
`theme.json` `music` block or `PrototypeMusicPlayer.MusicTheme`
selection logic (the main session is wiring theme→enum after agent 2
finishes #21). Stay inside `scripts/Audio/`, `scripts/UI/`, and
`assets/audio/` only.

35. **Audio mixer settings UI — Master / Music / SFX / Ambient.**
    - The pause menu has a single volume slider today. Split it into
      four buses: Master, Music, SFX, Ambient. Persist values in
      `user://audio_settings.json`.
    - Files: extend the Options panel in
      [`MainMenuController.cs`](scripts/UI/MainMenuController.cs) and
      [`HudController.cs`](scripts/UI/HudController.cs) Escape menu;
      new `AudioSettings` static (in `scripts/Audio/`) holding the
      four float values + load/save helpers.
    - Wire the saved values into `AudioServer.SetBusVolumeDb` for
      buses named "Master" / "Music" / "SFX" / "Ambient" — register
      missing buses on boot.
    - Acceptance: smoke test that AudioSettings round-trips through
      JSON; manual: change the music slider, hear it change live.

36. **Spatial audio attenuation for world events.**
    - World event SFX play at fixed volume regardless of distance.
      Add a `PositionalAudioPlayer` helper that wraps
      `AudioStreamPlayer2D` with a default 8-tile linear falloff
      (`MaxDistance = 8 * TilePixelSize`,
      `AttenuationFilterCutoffHz = 5000` for muffled distance).
    - File: new `scripts/Audio/PositionalAudioPlayer.cs`. Add an
      overload to `AudioEventCatalog.Resolve` (or a sibling helper)
      that returns the registered clip plus a default falloff
      profile keyed by event id (e.g. `karma_break` heard 16 tiles,
      `purchase_complete` heard 4).
    - Wire one demonstration call site:
      [`WorldRoot.cs`](scripts/World/WorldRoot.cs) door-open path. Do
      not refactor the broader audio pipeline — just prove the
      pattern. **Exception to the WorldRoot conflict-zone rule for
      this single call site only.**
    - Acceptance: smoke test that the falloff profile registry holds
      defaults + accepts overrides; manual: open a door, walk away,
      hear it fade.

37. **Voice bark catalog — short player vocal stingers.**
    - `SOUND_NEEDED.md` describes a "vocal stingers" pattern (laugh /
      sigh / taunt / ouch / ready / surrender). Build the catalog
      seam now, even before clips land:
      - New `VoiceBarkCatalog` (mirror `AudioEventCatalog` shape).
        Built-in entries point at expected paths under
        `assets/audio/voice/<voice_slot>/<bark_id>.ogg`; missing files
        no-op silently.
      - New `VoiceSlot` enum (`Voice1` / `Voice2` / `Voice3`).
        Store the player's current slot in `PlayerState`.
      - Wire fire points: on `karma_break` → `ouch`; on
        `match_started` → `ready`; on `posse_formed` → `laugh`.
    - Files: new `scripts/Audio/VoiceBarkCatalog.cs`;
      [`PlayerModels.cs`](scripts/Data/PlayerModels.cs) (`VoiceSlot`
      field, default `Voice1`).
    - Acceptance: smoke test that the catalog resolves a bark id +
      voice slot to the expected path; manual: trigger a karma break,
      see the audio call site invoke the resolver (silently no-ops
      while clips are missing).

38. **Ambient bed manager — interior vs exterior swap.**
    - Loop a default outdoor ambient bed; when the player enters a
      structure (already detected via `_enteredStructureByPlayer` on
      the server, surfaced through snapshot), crossfade to an
      interior bed for that structure category (tavern / chapel /
      smithy / market / clinic). On exit, crossfade back.
    - Files: new `scripts/Audio/AmbientBedManager.cs` Node attached
      to the gameplay scene root. Reads the player's snapshot once
      per second to decide which bed to play. Bed file paths come
      from a small registry inside the manager (e.g.
      `assets/audio/ambient/outdoor.ogg`,
      `assets/audio/ambient/tavern.ogg`).
    - Crossfade: 1.5-second linear; only one bed plays at a time
      after the fade.
    - Acceptance: smoke test the bed-id picker (returns
      "outdoor" → "tavern" → "outdoor" given the right player
      states); manual: walk into a tavern, hear the swap.

### Ready: round 5 — item thumbnails + conversational AI + playable build

Drafted 2026-05-02 from a batch of user requests:
- "we need to generate thumbnails for each of the
  weapons/items/consumables/etc so they can be found from
  vendors/ground/loot caches/etc."
- "the audio agent could also work on the tts from the npcs"
- "we will also need to come up with a lot of voices for the tts for
  the npcs"
- "we will need to work on speech to text as well so the npcs can
  'hear' the players. aka the llm"
- "we will need some work done getting the actual game put together
  so I can start testing the real game as intended"

Two new docs are the canonical specs for this batch:
- [docs/item-thumbnails-inventory.md](docs/item-thumbnails-inventory.md)
- [docs/npc-conversational-ai-plan.md](docs/npc-conversational-ai-plan.md)

Conflict guidance same as round 4 (do not touch theme infra,
StarterNpcs/Factions/Recipes/Quests, MedievalThemeData/ThemeData,
theme.json, AuthoritativeWorldServer ctor area, audio pipeline files
that round-4 agent 2 owns until that batch lands).

39. **Wire item thumbnails into vendor / inventory / hotbar UI.** — *done 2026-05-02*. *(agent 1)*
    - The art + atlas registry already exist
      ([`PrototypeSpriteCatalog`](scripts/Art/PrototypeSpriteModels.cs)
      maps 36/42 items to atlas regions). Vendor rows + inventory
      rows + hotbar slots are still text-only.
    - Sub-tasks:
      1. Add 6 missing entries to `PrototypeSpriteKind` enum +
         sprite definitions for: `BackpackBrown`, `BallisticRound`,
         `EnergyCell`, `StimSpike`, `DownerHaze`, `TremorTab`. Use
         placeholder atlas regions (32×32 from any utility/weapon
         atlas) until the medieval re-skin task lands.
      2. Update `GetKindForItem(itemId)` to resolve those 6.
      3. Refactor vendor row in
         [`HudController.cs`](scripts/UI/HudController.cs)
         (`RefreshShopOverlay` / vendor button render) to add a
         32×32 `TextureRect` child built from the catalog.
      4. Same treatment for the inventory row
         (`InventoryDragRow` build path) and the hotbar slot
         (`HotbarDropSlot`).
    - Acceptance: smoke test that `PrototypeSpriteCatalog.GetKindForItem`
      resolves all 42 starter items; manual: open vendor + inventory
      and see icons for every row.
    - Done: all 42 starter items resolve catalog art; vendor buy/sell
      rows, inventory drag rows, and hotbar drop slots now render
      atlas-backed item thumbnails.

40. **Medieval item icon re-skin — research, curate, license, wire.** *(art curation agent — separate spawn)*
    - Plan + license rules + sources in
      [docs/item-thumbnails-inventory.md](docs/item-thumbnails-inventory.md).
    - Goal: 42 medieval-flavoured 32×32 icons under
      `assets/art/themes/medieval/items/<item_id>.png`, plus a new
      `ItemArtRegistry.Get(themeId, itemId)` keyed by theme + item id
      with fallback to the existing sci-fi catalog when no re-skin
      exists.
    - Sources to triage (in priority order): LPC weapon/armor sheets
      (already vendored, MIT-flavoured CC licenses), OpenGameArt
      CC0 RPG icon packs, Game-Icons.net (CC-BY 3.0), PixelLab when
      balance returns.
    - Acceptance: 42 PNGs committed; `assets/art/CREDITS.md` lists
      each with source URL + license id; `ItemArtRegistry` smoke test
      green; manual: switch to medieval theme, see medieval icons in
      vendor / inventory.
    - License hard rule: refuse non-commercial / no-derivatives /
      unclear licenses.

41. **NPC TTS engine seam — Piper + stub backend.** *(agent 2 expansion)*
    - See [docs/npc-conversational-ai-plan.md](docs/npc-conversational-ai-plan.md)
      Stage 3 + Stage 4.
    - Build the seam first (works without any TTS engine), then wire
      Piper:
      1. New `scripts/Voice/NpcVoiceSynthesizer.cs` with a
         `Synthesize(string voiceId, string text) -> Task<string>`
         interface; backends: `PiperLocalBackend` (subprocess invoke,
         shells out to `piper-cli`), `StubBackend` (writes a silent
         WAV of duration `text.Length * 80ms`).
      2. New `scripts/Voice/VoiceCatalog.cs` — registry mapping
         voice ids to Piper model paths. Seed with 5 placeholder
         voice ids (`alba`, `ryan`, `aru`, `arctic`, `amy`); real
         downloads land in #42.
      3. Smoke fire point: on `dialogue_opened` event, kick off TTS
         with the NPC's voice + greeting; play the resulting WAV via
         `AudioStreamPlayer2D` anchored on the NPC.
    - Acceptance: stub backend round-trips text → WAV file; smoke
      test asserts file exists + duration > 0; manual (after #42):
      open dialogue with an NPC, hear the greeting in their voice.

42. **NPC voice catalog — curate ~30 Piper voices.** *(agent 2 expansion)*
    - Download from <https://github.com/rhasspy/piper/blob/master/VOICES.md>.
      Filter to voices with permissive licenses (most are public
      domain or CC0 — check each model's `MODEL_CARD`).
    - Audition each voice with the test phrase
      `"By the king's order, mind the gate"` (medieval) and
      `"Detention starts whenever I decide it does"` (boarding
      school). Keep ~30 distinct voices spanning gender / age /
      timbre.
    - Commit voices under `assets/audio/voice/piper/<voice_id>/`
      (`.onnx` + `.json` config per voice).
    - Author `assets/audio/voice/CREDITS.md` mapping each voice to
      source URL + license id.
    - Add `voice_pools` block to a new top-level section in the main
      session's queue — agent 2 must NOT edit theme.json directly;
      instead, output a `voice_pools_proposed.json` file alongside
      the voices and the main session will merge it into the
      authoritative `theme.json` on a single coordinated commit.
    - Acceptance: 30 voices on disk; CREDITS file lists each;
      `VoiceCatalog.All.Count >= 30`.

43. **Player STT integration — Whisper.cpp + push-to-talk.** *(agent 2 expansion)*
    - See [docs/npc-conversational-ai-plan.md](docs/npc-conversational-ai-plan.md)
      Stage 1.
    - Sub-tasks:
      1. New `scripts/Voice/PlayerMicCapture.cs` — capture mic to
         WAV via Godot's `AudioServer.AddBus` + `AudioEffectRecord`.
         Push-to-talk hold key `V`; clip max length 10s.
      2. New `scripts/Voice/SpeechRecognizer.cs` with
         `Transcribe(string clipPath) -> Task<string>` interface;
         backends: `WhisperLocalBackend` (subprocess shells to
         `whisper-cli` against the small.en model), `StubBackend`
         (returns a fixed test phrase).
      3. Wire UX: holding `V` near an NPC starts capture; releasing
         submits the clip. Show a "Listening…" indicator on the HUD
         while held.
    - Acceptance: stub backend round-trips a 1-second silent clip to
      a fixed transcript; manual: hold `V`, speak, see the
      transcript in the developer overlay (no LLM yet).

44. **NPC LLM dialogue — Anthropic API + stub backend.** *(new agent — AI/LLM specialist)*
    - See [docs/npc-conversational-ai-plan.md](docs/npc-conversational-ai-plan.md)
      Stage 2.
    - Sub-tasks:
      1. New `scripts/Voice/NpcDialogueLLM.cs` with
         `GenerateReply(string npcId, string playerText, IReadOnlyList<DialogueTurn> history) -> Task<string>`
         interface; backends: `AnthropicBackend` (HTTPS POST to
         `https://api.anthropic.com/v1/messages` with model
         `claude-haiku-4-5-20251001`), `StubBackend` (rotates from
         the NPC's `gossip_templates` deterministically).
      2. Read API key from env `ANTHROPIC_API_KEY` at startup; if
         absent, route to stub backend.
      3. Build the system prompt from `ThemeData.NpcRoster[npcId]`
         identity + relationship summary + recent witnessed events.
         Cap reply length to 3 short sentences.
      4. Cost guard: cap LLM calls per minute per player at 6;
         reject above that with a "{npc.name} pauses to think…" stub
         line.
    - Acceptance: stub backend produces a deterministic reply for a
      given (npc_id, player_text); when `ANTHROPIC_API_KEY` is set,
      manual: speak to an NPC and get a real LLM reply within 3
      seconds.
    - Cost guard hard rule: never make an LLM call without the
      per-minute cap in place.

45. **Conversation orchestrator — wire STT → LLM → TTS.** *(after #41-#44 land)*
    - See [docs/npc-conversational-ai-plan.md](docs/npc-conversational-ai-plan.md)
      Stage 5.
    - New `scripts/Voice/ConversationOrchestrator.cs` Node attached
      to the gameplay scene. On player release of `V` near an NPC:
      1. Get clip from `PlayerMicCapture.LastClipPath`.
      2. `SpeechRecognizer.Transcribe(clip)` → text.
      3. `NpcDialogueLLM.GenerateReply(npcId, text, history)` → text.
      4. `NpcVoiceSynthesizer.Synthesize(npcVoiceId, text)` → wav.
      5. Play through `PositionalAudioPlayer` (from #36) anchored
         on NPC.
    - Show transcript + reply in the existing dialogue panel.
    - Acceptance: end-to-end stub run produces a reply within 1
      second; smoke test the orchestrator's wiring with mocked
      backends; manual (with full backends configured): hold V,
      speak, hear NPC reply in their voice.

46. **Playable build integration pass.** — *done 2026-05-02*. *(handed off to agent 1 — main session pivoted to PixelLab art generation)*

    **Status as of 2026-05-02 PM:** main session opened the work, made
    the high-priority restart-hygiene fixes, and handed off the rest.
    Build is green; smoke tests still have unrelated failures.

    **Already landed by main session — DO NOT redo:**
    - Disabled auto-save / auto-load on `GameState._Ready` /
      `_ExitTree`. The saved-state pollution that caused
      `prototype local player starts with scrip. Expected 25, got 28`
      is fixed. `SaveLocalPlayer` / `LoadLocalPlayer` remain as
      explicit APIs (used by the round-trip smoke test).
    - Added `GameState.ResetForNewMatch()` that wipes per-match state
      (players, ledgers, quest progress, world events) and re-seeds.
    - Added `Clear()` to `RelationshipLedger`, `FactionLedger`,
      `EntanglementLedger`, `DuelLedger`, `WorldEventLog`.
    - Added `QuestLedger.Reset(definitions)`.
    - Added `PrototypeServerSession.RestartForNewMatch(themeId)` —
      tears down + rebuilds the authoritative server.
    - Added `InterestSnapshotCache.Reset()`.
    - Wired `HudController.ReturnToMainMenu` to call both
      `GameState.ResetForNewMatch()` and
      `PrototypeServerSession.RestartForNewMatch()` before
      `ChangeSceneToFile`.

    **Completed by agent 1:**
    1. `tools/test.ps1` exits 0; the handed-off quest-completion
       smoke failures are no longer present in this workspace.
    2. Added a match-summary "Return to Main Menu" button that reuses
       `ReturnToMainMenu()` and therefore runs the reset hygiene path.
    3. Restart smoke is covered by the green headless test gate and
       the new manual checklist; GUI manual pass remains a human
       playtest item.
    4. Tuned small-world NPC density from 12 to 10 generated profiles;
       small worlds still use 5 social stations from the 14 archetype
       catalog, 30-minute default matches, and 25 starting scrip.
    5. Added [`docs/playtest-checklist.md`](docs/playtest-checklist.md)
       for repeatable gameplay-loop validation.

    **Conflict guidance:** main session is concurrently generating
    PixelLab item / building / icon art. Stay out of
    `assets/art/themes/medieval/` and any `.png`/`.json` under
    `assets/art/sprites/generated/` — those are art-output paths.
    Code is fair game.

    **Acceptance:** `tools/test.ps1` exits 0; manual restart smoke in
    (3) works without error; checklist doc shipped.

### Ready: round 6 — agent 1 (gameplay polish, theme-infra-safe)

Drafted 2026-05-02 PM. Same conflict zones (no theme infra, no audio
pipeline, no `Starter*` catalogs, no `MedievalThemeData` /
`ThemeData` / `theme.json`, no `assets/art/themes/medieval/items/` —
that path is the main session's PixelLab/LPC art output target).

47. **Wire `ItemArtRegistry` into HUD inventory + vendor + hotbar.** — *done 2026-05-02*.
    The main session is now generating per-theme item icons under
    `assets/art/themes/medieval/items/<item_id>.png`. Once a few
    exist on disk:
    - New `scripts/Art/ItemArtRegistry.cs` mirroring `ThemeRegistry`
      shape: `ItemArtRegistry.Get(themeId, itemId) -> ItemArtEntry`
      with fields `IconPath` (string) + `HasIcon` (bool). Cache the
      lookup; resolve a missing theme icon by falling back to the
      existing `PrototypeSpriteCatalog` atlas region.
    - Update `HudController` rows (vendor, inventory, hotbar) to add
      a 32×32 `TextureRect` child built from
      `ItemArtRegistry.Get(activeTheme, item.Id)`. If `HasIcon`,
      load the PNG; otherwise fall back to the catalog
      `AtlasTexture`.
    - **Active theme** comes from
      `PrototypeServerSession.ActiveThemeId` (already exists from
      Phase 2 work).
    - Acceptance: smoke test that the registry returns a path for
      any `assets/art/themes/medieval/items/*.png` file present on
      disk; manual: open vendor + inventory + hotbar and confirm
      icons render where art exists.
    - **Do not** commit any new art files; only the wiring code.
    - Done: added `scripts/Art/ItemArtRegistry.cs`, wired HUD item
      thumbnails to `PrototypeServerSession.ActiveThemeId`, and kept
      `PrototypeSpriteCatalog` atlas fallback when theme art is
      missing. Smoke tests cover present medieval PNG lookup, compact
      filename aliases, and fallback behavior.

48. **Match-end auto-return button.** — *done 2026-05-02*. *(deferred from #46 sub-task 2)*
    Add a "Return to Main Menu" button at the bottom of the
    `_matchSummaryPanel` (`HudController.cs` ~line 1442). Clicking
    it routes through `ReturnToMainMenu()` so the reset hygiene
    (already wired by main session in #46) runs. Acceptance: manual
    smoke — finish a match, click the button, land back on main
    menu cleanly.
    - Done in the #46 pass; smoke test verifies the button node exists
      at `HudRoot/MatchSummaryPanel/MatchSummaryContent/ReturnToMainMenuButton`.

49. **Faction store gating polish.** — *done 2026-05-02*.
    Today vendors price every offer the same regardless of
    `requiredFactionId` / `minReputation` constraints on the
    `ShopOffer`. When a player tries to buy an offer they don't
    meet, surface a clear "denied" path in HUD: vendor row shows a
    🔒 prefix, click triggers a "{Faction} won't sell to you yet
    (need rep ≥ N, you're at M)" toast.
    Files: [`HudController.cs`](scripts/UI/HudController.cs)
    `RefreshShopOverlay`,
    [`AuthoritativeWorldServer.cs`](scripts/Net/AuthoritativeWorldServer.cs)
    `ProcessPurchaseItem` rejection path. Acceptance: smoke test
    the rejection event message; manual: walk up to a faction
    store as Hostile and see locked rows.
    - Done: HUD rows show a lock prefix and denial tooltip when the
      local reputation misses the requirement; clicking routes through
      the authoritative rejection path and shows a display-name based
      denial prompt.

50. **NPC vendor pricing breakdown tooltip.** — *done 2026-05-02*.
    Each NPC vendor adjusts prices based on the player's karma tier
    + relationship score. Surface this when hovering a vendor row:
    "Garrick: -10% (Friendly), +5% (Outlaw tier). Net price: 18
    scrip (base 20)." Files:
    [`HudController.cs`](scripts/UI/HudController.cs) `BuildShopRow`
    (or whatever builds the vendor button row). Acceptance: smoke
    test the tooltip formatter for three input combos; manual:
    hover, see the breakdown.
    - Done: shop snapshots now carry `BasePrice` +
      `PricingBreakdown`; tooltips show relationship, karma/perk, and
      cleanliness modifiers. Smoke tests cover friendly, hostile, and
      base-price tooltip paths plus a live vendor snapshot.

51. **Carry-state opt-in toggle for next round.** — *done 2026-05-02*.
    Per `#46` the new round wipes ledgers via `ResetForNewMatch()`.
    Add an opt-in toggle in the Escape menu's options panel:
    "Carry karma + relationships + faction rep into next round
    [ ]". When checked, persist those three ledgers across
    `ResetForNewMatch()` calls (everything else still wipes).
    Persist the toggle's value in `user://carry_state.json`. Files:
    [`GameState.cs`](scripts/Core/GameState.cs),
    [`HudController.cs`](scripts/UI/HudController.cs). Acceptance:
    smoke test the toggle round-trips through JSON; manual: enable
    toggle, finish match, start new match, see karma persist.
    - Done: Escape options include `CarryStateToggle`, the preference
      round-trips through JSON, and `ResetForNewMatch()` preserves
      karma, relationships, and faction reputation when enabled.

52. **Status icon palette matches active theme.** — *done 2026-05-02*.
    Task #24 shipped a status-effect icon strip with a
    placeholder-color glyph table. Pull the colors from
    `UiPaletteRegistry` (from #31, may or may not have landed yet —
    if not, hardcode the medieval palette and leave a TODO for the
    palette wiring). Files:
    [`HudController.cs`](scripts/UI/HudController.cs)
    `RefreshStatusStrip`. Acceptance: smoke test that the palette
    lookup returns medieval colors for medieval theme; manual:
    poison yourself, see a parchment-tinted status row.
    - Done: status strip colors now come from `UiPaletteRegistry`
      using the active theme palette; smoke tests assert medieval
      danger/success/dim colors.

53. **Death-pile pickup window UX.** — *done 2026-05-02*.
    When a player dies, their drops sit on a tile owned by them
    for `DeathPileGracePeriodTicks`. Today the player has no visual
    cue for "this pile is mine" / "this pile is fair game". Add:
    - Tinted outline around the tile while ownership is active
      (subtle yellow for own pile, red for someone else's).
    - HUD toast "Drop ownership expires in N ticks" while standing
      on the pile.
    Files: [`WorldRoot.cs`](scripts/World/WorldRoot.cs)
    `RenderServerItems` overlay;
    [`HudController.cs`](scripts/UI/HudController.cs) snapshot
    refresh. Acceptance: smoke test the formatter; manual: die,
    see your own pile glow, see the timer countdown.
    - Done: owned drops carry an expiry tick, ownership clears after
      `DeathPileGracePeriodTicks`, HUD formats the countdown while
      standing on a pile, and world item render nodes get own/other
      ownership tint.

### Ready: round 7 — agent 1 (post-art-ship + tuning)

Drafted 2026-05-02 PM. The main session has shipped 44 item icons +
170 NPC portraits + 22 structure icons. Agent 1 work below assumes
those exist on disk under `assets/art/themes/medieval/`. Same
conflict zones (no theme infra, no audio pipeline, no `Starter*`
catalogs, no `MedievalThemeData` / `ThemeData` / `theme.json`,
**but agent 1 IS allowed to edit
`tools/compose_structure_icons.gd`** for #54 below).

54. **Refine `tools/compose_structure_icons.gd` rectangles.**
    Several structure icons in
    `assets/art/themes/medieval/structures/*.png` have wrong
    rectangles in the source TX Props.png — the file named `barrel.png`
    actually contains a wooden T-sign, `chest.png` actually contains
    a wall section, etc. Tune the `STRUCTURE_SOURCES` array in
    [`tools/compose_structure_icons.gd`](tools/compose_structure_icons.gd)
    so each item id matches a sensible Cainos prop:
    - Inspect `assets/art/third_party/cainos_pixel_art_top_down_basic_v1_2_3/Texture/TX Props.png`
      visually + measure pixel coordinates of each prop.
    - Adjust `(x, y, w, h)` per item id.
    - Re-run the composer; spot-check 10 outputs.
    - Add 5-10 more structure ids that are useful for the medieval
      theme (anvil, wooden_post, market_stall, well, gravestone,
      lantern_post, hay_bale, etc.).
    - Acceptance: every `<id>.png` file under
      `assets/art/themes/medieval/structures/` visually matches its
      filename when opened.

55. **Wire `StructureArtRegistry` keyed by theme + structure id.**
    Mirror `ItemArtRegistry` (#47) for structures.
    `StructureArtRegistry.Get(themeId, structureId) -> StructureArtEntry`
    returns the `IconPath` under
    `assets/art/themes/<themeId>/structures/<id>.png` if it exists,
    else falls back to the existing `StructureArtCatalog`. Wire
    [`WorldRoot.cs`](scripts/World/WorldRoot.cs) `RenderServerStructures`
    to consult the registry first before its current sprite-kind
    lookup. Acceptance: smoke test the registry; manual: walk near a
    structure that matches a generated icon and see the medieval art
    instead of the placeholder.

56. **Wire `NpcPortraitRegistry` for dialogue UI.**
    Each NPC has an `LpcBundleId` (see `NpcSnapshot`). When dialogue
    opens, render the NPC's portrait from
    `assets/art/themes/medieval/npc_portraits/<bundle_id>.png` in
    the dialogue panel header. New
    `scripts/Art/NpcPortraitRegistry.cs` mirroring the `ItemArtRegistry`
    shape (lookup with fallback to a default placeholder). HUD
    update in [`HudController.cs`](scripts/UI/HudController.cs)
    dialogue panel build. Acceptance: smoke test the registry;
    manual: open dialogue with any NPC, see their face in the panel.

57. **NPC tooltip avatar.**
    Task #19 added an "NPC name • role • faction" tooltip when the
    player is within 2 tiles. Add a 32×32 portrait avatar inside
    the tooltip, sourced from `NpcPortraitRegistry` (#56). Falls
    back to a colored circle if no portrait exists. File:
    [`HudController.cs`](scripts/UI/HudController.cs)
    `RefreshNpcTooltip` / `FormatNpcTooltip`. Acceptance: smoke
    test the tooltip builder includes a TextureRect when the
    portrait exists; manual: walk near an NPC and see the avatar.

58. **Inventory grid (visual upgrade from list).**
    Inventory currently renders as a vertical list of buttons. Once
    item icons land (#47 wires `ItemArtRegistry`), upgrade to a 4×N
    grid of 48×48 cells (32×32 icon + 16-pixel padding for stack
    count text). Same drag/drop semantics carry over. File:
    [`HudController.cs`](scripts/UI/HudController.cs) inventory
    panel build + refresh path. Acceptance: smoke test the grid
    layout helper; manual: open inventory, see icons in a grid.

### Ready: round 8 — agent 1 (art wiring across 329 PNGs)

Drafted 2026-05-02 PM. Main session generated 329 medieval-themed
PNGs across 12 categories under
`assets/art/themes/medieval/{items,npc_portraits,buildings,banners,
decals,structures,status_icons,hud_chrome,map_icons,quest_glyphs,
mounts,environment}/`. Manifest in
[`assets/art/CREDITS.md`](assets/art/CREDITS.md). Agent 1 wires
them in. Conflict guidance: do not regenerate any art under
`assets/art/themes/medieval/`; you may add code that *reads* them.

59. **`BuildingArtRegistry` + render buildings on the world map.**
    [`assets/art/themes/medieval/buildings/`](assets/art/themes/medieval/buildings/)
    has 12 PNGs (`smithy.png`, `tavern.png`, `chapel.png`,
    `watchtower.png`, `market_stall.png`, `tithe_barn.png`,
    `notice_post.png`, `well.png`, `shrine.png`, `bell_tower.png`,
    `duel_ring.png`, `memorial.png`).
    - New `scripts/Art/BuildingArtRegistry.cs` mirroring
      `ItemArtRegistry` shape. Lookup `Get(themeId, buildingKind)`.
      Map `WorldGenerator.SocialStations` archetype keys
      (`clinic`/`market`/`workshop`/`notice-board`/`social-hub`/
      `restricted-storage`/`oddity-yard`/`duel-ring`/`farm-plot`/
      `black-market`/`memory-shrine`/`broadcast-tower`/`war-memorial`/
      `court-of-crows`) → building PNG ids (e.g. `clinic` → `smithy`,
      `market` → `market_stall`, `workshop` → `smithy`, `notice-board`
      → `notice_post`, `social-hub` → `tavern`,
      `restricted-storage` → `tithe_barn`, `oddity-yard` →
      `shrine`, `duel-ring` → `duel_ring`, `farm-plot` → `well`,
      `black-market` → `tithe_barn`, `memory-shrine` → `shrine`,
      `broadcast-tower` → `bell_tower`, `war-memorial` →
      `memorial`, `court-of-crows` → `chapel`).
    - Wire `WorldRoot.RenderServerStructures` to render the building
      sprite at the structure's tile position when the registry has
      a hit; fall back to existing prototype sprite otherwise.
    - Acceptance: smoke test that the registry resolves all 14
      archetypes for theme `medieval`; manual: launch a match,
      walk to each station, see the building art.

60. **`StatusIconRegistry` + wire into the HUD status strip.**
    [`assets/art/themes/medieval/status_icons/`](assets/art/themes/medieval/status_icons/)
    has 14 PNGs (`status_hungry`, `status_starving`, `status_dirty`,
    `status_filthy`, `status_poisoned`, `status_burning`,
    `status_chilled`, `status_silenced`, `status_wraith`,
    `status_blessed`, `status_cursed`, `status_stunned`,
    `status_wanted`, `status_withdrawal`).
    - Replace the placeholder colored-glyph map in task #24's
      `RefreshStatusStrip` (HudController.cs) with `TextureRect`
      children that load these PNGs.
    - Status id (e.g. `Hungry`, `Starving`) → file id
      (`status_hungry`, `status_starving`) via simple snake-case
      conversion.
    - Acceptance: smoke test the id mapping; manual: poison
      yourself, see the green-skull poison icon.

61. **Quest log glyphs + map markers.**
    [`assets/art/themes/medieval/quest_glyphs/`](assets/art/themes/medieval/quest_glyphs/)
    has 8 quest type icons (`quest_recover`, `quest_hunt`,
    `quest_deliver`, `quest_smuggle`, `quest_investigate`,
    `quest_escort`, `quest_repair`, `quest_rumor`).
    [`assets/art/themes/medieval/map_icons/`](assets/art/themes/medieval/map_icons/)
    has 8 map markers + 3 currency + 6 vital glyphs.
    - Wire quest-log rows (#23) to display the matching
      `quest_glyphs/quest_<type>.png` next to each quest title.
      Quest type comes from `QuestDefinition.Type` (extend if
      missing — recover/hunt/deliver/smuggle/investigate/escort/
      repair/rumor).
    - Wire HUD scrip label to show
      `map_icons/currency_silver.png` next to the count.
    - Wire HUD karma display to show `icon_karma_saint.png` /
      `icon_karma_scourge.png` based on tier.
    - Acceptance: smoke test the formatter for each glyph;
      manual: see the icons appear in the HUD.

62. **`BannerArtRegistry` + faction-banner rendering on the
    faction-rep panel.**
    [`assets/art/themes/medieval/banners/`](assets/art/themes/medieval/banners/)
    has 6 banners (`banner_chapel_order`, `banner_crown_garrison`,
    `banner_shadowed_guild`, `banner_wayfarers`,
    `banner_wild_folk`, `banner_freeholders`).
    - New `scripts/Art/BannerArtRegistry.cs`. Faction id →
      banner PNG id (read from `theme.json → factions[].id`).
    - Wire into the faction reputation HUD panel (#13) so each
      faction row shows its banner.
    - Acceptance: smoke test the registry; manual: open the
      faction panel, see all 6 banners next to faction rows.

63. **Mount art + animated environment props.**
    [`assets/art/themes/medieval/mounts/`](assets/art/themes/medieval/mounts/)
    has 6 mount sprites.
    [`assets/art/themes/medieval/environment/`](assets/art/themes/medieval/environment/)
    has 10 environment props (trees, bushes, hay, campfire,
    fence, etc.).
    - Wire `MountEntity.Name` → mount sprite (default
      `horse_brown` if no match).
    - Add a `decorate_world` pass in `WorldGenerator` that
      sprinkles environment props around locations using the same
      `ProceduralPlacementSampler` as oddities.
    - Acceptance: smoke test mount rendering; manual: see horses
      at mount tiles + scattered trees/bushes in unused tile
      patches.

64. **Decals on combat events.**
    [`assets/art/themes/medieval/decals/`](assets/art/themes/medieval/decals/)
    has 8 decal PNGs.
    - On `player_attacked` event with damage > 0, spawn
      `decal_blood_splatter.png` at the target's tile.
      Decal lifetime: 30 ticks.
    - On `player_respawned` with downed → respawned, spawn
      `decal_footprints_dirt.png` along the path.
    - File: [`WorldRoot.cs`](scripts/World/WorldRoot.cs)
      event-driven render pass (mirror the existing chat-bubble
      cleanup loop).
    - Acceptance: smoke test the decal lifetime tracker; manual:
      attack an NPC, see blood splatter that fades.

65. **Medieval HUD chrome — apply the panel/scroll PNGs.**
    [`assets/art/themes/medieval/hud_chrome/`](assets/art/themes/medieval/hud_chrome/)
    has 10 chrome assets.
    - Use `panel_parchment_bg.png` as the background of the
      Escape menu, faction panel, quest log, and dialogue panel
      (via `StyleBoxTexture`).
    - Use `button_wood.png` / `button_wood_pressed.png` as the
      `StyleBox` for menu buttons.
    - Use `divider_horizontal.png` between sections in tall
      panels.
    - Use `scroll_top.png` + `scroll_bottom.png` to bracket the
      quest log.
    - File: [`HudController.cs`](scripts/UI/HudController.cs)
      panel build paths.
    - Acceptance: open Escape menu, see parchment chrome with
      iron-rimmed buttons.

### How to run a parallel agent on these

```bash
# inside Claude Code
/agent
```

Then point it at one of the tasks above by id (`#1`, `#2`, …). Each
entry is self-contained enough that the agent shouldn't need anything
beyond the linked file + the verification gate.

---

## Where we left off (2026-04-30 PM session, continued)

Long session. All work below is on `develop` working tree, not committed yet.
`tools/test.ps1` (build + smoke tests) is green at the stopping point —
1700+ assertions passing.

### Landed this session

Completed end-to-end (code + smoke tests + TASKS.md updated):

1. **Surface ammo / stamina / weapon kind in `PlayerSnapshot`** — fields
   added to [SnapshotModels.cs](scripts/Data/SnapshotModels.cs).
2. **Ammo + combat-stamina in HUD** — `_ammoLabel` + `_combatStaminaLabel`
   in [HudController.cs](scripts/UI/HudController.cs); `FormatAmmo`,
   `FormatCombatStamina`.
3. **R rebound to reload** — bare R = `IntentType.Reload`; Shift+R kept
   legacy repair-kit-on-peer ([PlayerController.cs](scripts/Player/PlayerController.cs)).
4. **Configurable server tick rate** — verified already in
   [ServerConfig.cs](scripts/Net/ServerConfig.cs); no change needed.
5. **Backpack equipment slot** — `EquipmentSlot.Backpack`, `InventoryBoost`,
   `BackpackBrown` starter item, `MaxInventorySlots` getter, snapshot field.
6. **Karma-break impact flash** — full-screen white tween fade on local
   karma_break / player_respawned; `FindKarmaBreakTriggerTick` static helper.
7. **Hunger / food system (first slice)** — `Hunger` / `MaxHunger` on
   PlayerState, `FoodValue` on `GameItem` (RationPack: 40), tick decay,
   ration restores hunger without needing injury, snapshot fields, HUD label.
8. **Contraband detection flash** — red tween fade on `contraband_detected`
   for the local player; `FindContrabandFlashTriggerTick` static helper.
9. **Wraith perk speed trail VFX** — purple ColorRect placeholder behind
   any remote player whose `SpeedModifier != 1f`
   ([WorldRoot.cs](scripts/World/WorldRoot.cs) `ApplyWraithTrail`).
10. **Wanted poster overlay** — sliced `wanted_mug_shot_frame.png`
    floats above any remote player whose `StatusEffects` includes
    `Wanted` or `Bounty:` (`WorldRoot.ApplyWantedOverlay`).
11. **Audio event registry** —
    [scripts/Audio/AudioEventCatalog.cs](scripts/Audio/AudioEventCatalog.cs)
    with built-in mappings + runtime override + substring fallback.
12. **Prototype gameplay music** —
    [scripts/Audio/PrototypeMusicPlayer.cs](scripts/Audio/PrototypeMusicPlayer.cs)
    with 3 themes (`SandboxCalm`, `EventTension`, `ScenarioAmbient`),
    wired into `Main.tscn`, `InGameEventPrototype.tscn`,
    `EventPlaybackPrototype.tscn`.
13. **Pants + shirt appearance layers** — manifest entries, new slots in
    `layerOrder` (placed AFTER `outfit` so they draw on top), fields on
    `PlayerAppearanceSelection`, server payload, cycle helpers, default
    kit ships with `pants_blue_32x64` + `shirt_black_32x64`.
14. **Pants/shirt visible on the model** — pants/shirt PNGs padded from
    255×252/253×251 to 256×256. New
    `PrototypeCharacterSprite.ComposePantsShirtOntoBase` blends them on
    top of the prebuilt black-boots atlas at runtime, caches under
    `user://player_v2/composites`. Tests pass; visual confirmation in
    Main.tscn was NOT done yet — open the game and verify they show.

### Round-2 landings (continuation 2026-04-30 PM)

- **Pause menu Options audio sliders** — *done*. Three HSliders
  (master / music / effects) replaced the placeholder label + Back
  button in `BuildEscapeMenu`. ValueChanged → `RefreshPauseVolumeLabels`
  + `ApplyPauseAudioSettings` (sets `Master` bus dB, tweaks any
  `PrototypeMusicPlayer` directly) + `SavePauseAudioSettings` to the
  same `options.cfg` path the main menu uses. `LoadPauseAudioSettings`
  runs every time the panel opens. Static helpers `PercentToDb` /
  `LinearToDb` covered by 5 smoke tests.
- **Hungry / Starving status + HP decay** — *done*. `Hungry` derived
  status at hunger ≤ 25; `Starving` at 0; `ApplyHungerDecay` deals
  `StarvationDamagePerStep` per decay step when starving.
  `GetStatusEffectsFor` is now public. 5 smoke tests cover threshold,
  swap-on-zero, and HP loss.
- **Loot drop table registry** — *done*. New
  [scripts/Data/LootModels.cs](scripts/Data/LootModels.cs) with
  `LootTable`, `LootTableEntry`, `LootRollResult`, and
  `LootTableCatalog` (5 built-in tables: supply common/ammo/medical,
  container scavenge, downed player). `Roll(tableId, Random)` does
  weighted selection with quantity ranges, deterministic with a
  seeded RNG. 11 smoke tests cover lookup, determinism, qty range,
  override, reset.
- **NPC dialogue tree DSL** — *done*. New
  [scripts/Data/DialogueModels.cs](scripts/Data/DialogueModels.cs)
  with `DialogueChoice`, `DialogueNode`, `DialogueTree`, and
  `DialogueRegistry`. `BuildChoiceArray` adapts a node into the legacy
  `NpcDialogueChoice[]` shape so today's HUD/server keeps working;
  branching choices project `NextNodeId` into `ActionId` as
  `dialogue_advance:<id>`. Built-in `mara_clinic_default` tree shows
  branching offer / ask / decline. 11 smoke tests cover resolve,
  traversal, adapter, override, reset.

### Round-3 landings

- **Loot table → supply drop spawn** — *done 2026-04-30 PM*. New
  `AuthoritativeWorldServer.ScheduleSupplyDropFromTable(position,
  tableId, expiryTicks, seedSalt)` rolls a `LootTableCatalog` table
  and spawns one `WorldItemEntity` per rolled item in a small
  spiral pattern around the centre tile. Deterministic via
  `HashCode.Combine(WorldId, tick, tableId, seedSalt)`. Smoke tests
  cover spawn, item-id constraint, event emission, and unknown-table
  no-op.
- **NPC dialogue tree binding (data layer)** — *done 2026-04-30 PM*.
  Added `DialogueTreeId` to [NpcProfile](scripts/Data/NpcModels.cs)
  (default empty). `StarterNpcs.Mara.DialogueTreeId` now points to
  `DialogueRegistry.MaraClinicTreeId`. 3 smoke tests verify the
  binding resolves through the registry.
- **Backpack inventory cap enforcement** — *done 2026-04-30 PM*. New
  `TryAddItem` on `PlayerState` + `GameState` honours
  `MaxInventorySlots`. `ProcessClaimEntity` rejects "Inventory is
  full" pickups *before* the karma shift so a refused claim doesn't
  penalise the player. `ProcessCraftItem` falls back to the same
  path. Legacy `AddItem` stays unchecked so trophies / karma break
  drops can keep bypassing the cap. 3 smoke tests cover the full
  reject flow plus the backpack-expands-cap regression.

### Round-4 in-flight (paused mid-task to pivot)

Build + smoke tests are **green** as of 2026-05-01 (`tools/test.ps1` exit 0; Godot smoke tests passed). The earlier incomplete monitor-window runs were resolved by restoring missing tracked assets and loading generated PNG textures through the atlas/image fallback.

- **Server-side dialogue walker** — *done / smoke-tested 2026-05-01*.
  `_activeDialogueNodeByPlayerNpc` dictionary keyed by `playerId::npcId`
  tracks the player's current node per NPC. `GetDialogueFor` now branches:
  if the NPC has `DialogueTreeId` and the registry resolves the tree, it
  returns choices from the active node (or root) via
  `DialogueRegistry.BuildChoiceArray`. `ProcessSelectDialogueChoice`
  intercepts the `dialogue_close` and `dialogue_advance:<nodeId>` action
  ids before karma dispatch:
  - `dialogue_close` clears the active node and emits a
    `dialogue_closed` event.
  - `dialogue_advance:<id>` validates the tree + node exist, sets the
    active node, emits a `dialogue_advanced` event with `nextNodeId`.
  - Unknown next node → reject.
  - `ProcessStartDialogue` resets the active node so re-opening a
    dialogue starts at the root.
  Public helper `GetActiveDialogueNodeId(playerId, npcId)` for tests.
  Tests added at the bottom of `GameplaySmokeTest.cs` (search for
  "Server-side dialogue walker"). The unknown-advance test calls
  `DialogueRegistry.Reset()` mid-flow — note that Reset only clears
  runtime overrides, NOT the built-in `mara_clinic_default`, so the
  rejection it expects actually comes from the player being on the
  "supplies" node (where `ask_about_supplies` doesn't exist) rather
  than from the tree being missing. The assertion still holds but the
  reasoning in the test comment is slightly off — clean up if you
  revisit.

- **Audio stinger via AudioEventCatalog** — *done / smoke-tested 2026-05-01*. New `_eventStingerPlayer` `AudioStreamPlayer` on the HUD.
  `PlayEventStinger(eventId)` resolves the catalog and plays the clip
  if both the path is registered AND the file exists on disk. Silent
  no-op otherwise — clip files are still TBD per `SOUND_NEEDED.md`.
  Wired into `MaybeTriggerKarmaBreakFlash` (plays `karma_break`) and
  `MaybeTriggerContrabandFlash` (plays `contraband_detected`).

### Round-5 landings — Medieval theme tier 1 (2026-05-02)

The first slice from
[`docs/medieval-theme-inventory.md`](docs/medieval-theme-inventory.md).
Build + smoke tests **green** at exit.

- **`ThemeArtRegistry.Medieval(theme)`** —
  [scripts/World/ThemeArtRegistry.cs](scripts/World/ThemeArtRegistry.cs)
  now wires the existing tile contract (`GroundScrub`, `PathDust`,
  `MarketFloor`, `WallMetal`, etc.) onto the vendored Cainos tilesets:
  - `MedievalGrassAtlasPath` — `TX Tileset Grass.png`
  - `MedievalStoneAtlasPath` — `TX Tileset Stone Ground.png`
  - `MedievalWallAtlasPath` — `TX Tileset Wall.png`
  Atlas regions are first-cut picks (32×32 each). Easy to nudge in
  `Medieval(theme)` if specific tiles look wrong in-game.
- **Default prototype theme flipped to medieval** —
  `WorldConfig.CreatePrototype()` now ships
  `new WorldSeed(8675309, "Medieval Prototype", "medieval")`. The
  generator falls into its standard procedural path (no special-case).
- **LPC theme bundles seeded** under
  [`assets/art/sprites/lpc/themes/`](assets/art/sprites/lpc/themes/):
  `medieval_warrior_male.json`, `medieval_archer_female.json`,
  `medieval_peasant_male.json`. The bundle reader / composer that
  consumes these (next-step task) doesn't exist yet —
  `tools/lpc_compose_random.gd` still does pure random picks.
- **LPC library vendored** —
  [`assets/art/sprites/lpc/`](assets/art/sprites/lpc/) carries the
  full Universal-LPC-Spritesheet collection (151,875 sheets, 767
  metadata definitions, license + credits). 519 MB on disk, all
  offline-ready. Layout doc: [README](assets/art/sprites/lpc/README.md).
- **LPC random-character composer** —
  [`tools/lpc_compose_random.gd`](tools/lpc_compose_random.gd) picks
  body + clothing + head + eyes + hair + weapon, blends them in LPC
  z-order, and emits both the native 576×256 LPC sheet and a
  re-celled 256×256 Karma 8-dir / 4-row atlas. Written into
  [`assets/art/generated/lpc/`](assets/art/generated/lpc/).
  `PrototypeCharacterSprite.ApplyPlayerAppearanceSelection` prefers
  this atlas when present, so the prototype player IS the LPC
  character at runtime.
- **Boarding-school smoke tests pinned to explicit config** — the test
  block at line 1165+ of `GameplaySmokeTest.cs` no longer leans on
  `WorldConfig.CreatePrototype()` (since the default flipped); it
  builds its own boarding-school config so the boarding-school art
  mapping still validates regardless of prototype defaults.

### Medieval theme — what's next

Tier 2 from [`docs/medieval-theme-inventory.md`](docs/medieval-theme-inventory.md).
Most are blocked on art commissions / atlas building rather than code:

- **Medieval village buildings atlas** — biggest gap. Hand-compose
  Cainos walls + Mixel ruins into a thatched-village atlas, OR find
  a third-party LPC-style village pack.
- **NPC bundles for medieval roles** — blacksmith, tavernkeeper,
  merchant, guard, peasant, priest, knight. Each is a JSON bundle
  alongside `medieval_warrior_male.json`. (Doesn't need new art —
  it's the LPC stack picks.)
- **Medieval prop atlas** — barrels, hay bales, wells, anvils, market
  stalls, signs, lanterns. Cainos + Mixel cover ~60%.
- **Theme-appropriate text** — rename Mara → Blacksmith, Free
  Settlers → village faction, etc. Pure data work in
  `StarterNpcs` / `StarterFactions` / `WorldGenerator.SocialStations`.
- **LPC theme-bundle reader / composer** —
  `tools/lpc_compose_theme.gd` (sibling to `_random.gd`) that reads
  `themes/<id>.json` and produces a deterministic stack instead of
  randomly picking layers.

### Still in-flight

- **Visual confirmation of pants/shirt in Main.tscn.** Composer is
  written and unit-tested but I haven't launched the game and looked
  at the player. Run `tools/run-game.ps1` and open Main, then verify
  the blue pants + black shirt are drawn on top of the base atlas.
  If the composer fails silently (e.g. `BlendRect` mismatch), check
  `user://player_v2/composites/` for whether the cached PNG looks right.
- **Loot table refactor (other call sites).**
  `ScheduleSupplyDropFromTable` is built, but containers, kills, and
  hand-placed supply drops still enumerate items inline. Pick those
  off one at a time when you next touch them.
- **Dialogue tree wiring (server-side walker).** Mara is bound to
  `mara_clinic_default` via `DialogueTreeId`; the registry + adapter
  let `BuildChoiceArray` project a node into the legacy choice shape.
  Server still hardcodes the choice list in
  [AuthoritativeWorldServer.cs](scripts/Net/AuthoritativeWorldServer.cs).
  Next step: in `ProcessSelectDialogueChoice`, when the choice
  ActionId starts with `dialogue_advance:`, look up `npc.DialogueTreeId`
  + `next_node_id` and re-emit the new node's choices instead of
  closing the dialogue.

### Still pending from the original 20-task batch

P0 batch items not yet started:

- Drag-and-drop hotbar slot assignment
- Drug registry + addiction tracker
- Cleanliness / restroom mechanic
- Absurd-interaction module registry
- Witness propagation system
- Match replay log

Follow-ups still outstanding:

- Hunger: move-speed debuff actually wired off the `Hungry` status (the
  status now appears, but no movement code consumes it yet); visual flash
  on threshold cross.
- Wraith trail: swap placeholder ColorRect for a real ghosting sprite once
  art lands.
- Audio catalog: actual clip files (the stinger hook + `AudioEventCatalog`
  resolve are wired — only the on-disk audio files are missing per
  `SOUND_NEEDED.md`).
- Pants/shirt: HUD button + key binding to cycle without going through
  the appearance panel — *done 2026-05-02*. Appearance panel now has pants/shirt labels and Cycle pants/shirt buttons; M/H cycle the same slots directly in-game. Smoke tests cover the UI nodes, formatting, and payloads.
- Loot table refactor: convert other call sites (containers, kills,
  hand-placed drops) from inline arrays to table ids. Supply drop is
  already done via `ScheduleSupplyDropFromTable`.

### Verification gate before continuing

```
tools/test.ps1
```

Last run: **green on 2026-05-02** (`tools/test.ps1`; `dotnet build` 0 warnings / 0 errors; Godot smoke tests passed).

---

---

## P0 — New batch (2026-04-30)

20 fresh tasks added in one pass. Grouped by theme; pick from any group when
planning the next slice. Several are now complete below; verify against current
code before claiming any unchecked item.

### Combat HUD finish-up (loose ends from the weapon-resource pass)

- [x] **Surface ammo / stamina in `PlayerSnapshot`** — *done 2026-04-30*.
  Added `Stamina`, `MaxStamina`, `CurrentAmmo`, `MaxAmmo`, `EquippedWeaponKind`
  to [PlayerSnapshot](scripts/Data/SnapshotModels.cs). `SnapshotBuilder`
  populates them via the new `ResolveEquippedWeaponKind` helper. Smoke
  tests verify mirror-correctness for ranged + melee equip cases.
- [x] **Render ammo + stamina in HUD** — *done 2026-04-30*. Added
  `_ammoLabel` and `_combatStaminaLabel` to
  [HudController.cs](scripts/UI/HudController.cs); both refresh per
  snapshot via `SetAmmoFromSnapshot` / `SetCombatStaminaFromSnapshot`.
  Ammo hides for non-ranged weapons; both have `FormatAmmo` /
  `FormatCombatStamina` static formatters with smoke-test coverage.
- [x] **Rebind R to reload** — *done 2026-04-30*. In
  [PlayerController.cs](scripts/Player/PlayerController.cs), bare R now
  dispatches `IntentType.Reload` via `ReloadThroughServer`; Shift+R
  preserves the legacy repair-kit-on-peer chord.
- [ ] **Drag-and-drop hotbar slot assignment** — let the player drag an
  inventory row onto a hotbar slot to bind it. Persist binding in client
  state; pressing 1–9 dispatches `EquipItem` for the bound id.
  See [HudController.cs](scripts/UI/HudController.cs) `RefreshHotbar`.

### Visual feedback gaps (still listed in `ART_NEEDED.md`)

- [x] **Wraith perk speed trail VFX** — *first slice done 2026-04-30*.
  `WorldRoot.ApplyWraithTrail` adds a tinted purple
  `WraithTrail` ColorRect behind any rendered remote player whose
  `SpeedModifier != 1f`. `IsWraithTrailActive` is the testable static
  helper. *Outstanding follow-up*: replace placeholder ColorRect with
  real ghosting sprite from art catalog.
- [x] **Wanted poster overlay on Wanted players** — *done 2026-04-30*.
  `WorldRoot.ApplyWantedOverlay` adds a `WantedOverlay` Sprite2D
  above any rendered remote player whose `StatusEffects` contains
  "Wanted" or a "Bounty:" entry. Texture loaded from sliced
  `wanted_mug_shot_frame.png`. `IsWantedOverlayActive` static helper
  has 4 smoke-test cases.
- [x] **Contraband detection flash** — *done 2026-04-30*. Red
  `_contrabandFlash` ColorRect (alpha=0.45 → 0 over 0.6s) fires when
  a `contraband_detected` event arrives for the local player.
  `FindContrabandFlashTriggerTick` static helper has 4 smoke-test
  cases (no events, foreign player, local trigger, re-trigger guard).
- [x] **Karma-break impact flash** — *done 2026-04-30*. Full-screen
  white `_karmaBreakFlash` ColorRect in
  [HudController.cs](scripts/UI/HudController.cs) fades from α=0.6 to
  0 over 0.9s when a karma_break or player_respawned event fires for
  the local player. `FindKarmaBreakTriggerTick` is the testable static
  helper; smoke tests cover the no-trigger, foreign-player, local-trigger,
  re-trigger guard, and respawn-edge cases. *Outstanding follow-up*:
  audio stinger (listed in `SOUND_NEEDED.md`).

### New gameplay systems (promote from `BRAINSTORMING.md`)

- [x] **Hunger / food system** — *first slice done 2026-04-30*. Added
  `Hunger` / `MaxHunger` to [PlayerState](scripts/Data/PlayerModels.cs)
  (default 100); decays one point per `HungerDecayTickInterval` (600
  ticks, ~30 s at 20 Hz) inside `AdvanceIdleTicks`. `GameItem` gained
  a `FoodValue` field; `RationPack.FoodValue = 40`. Using a food item
  via `UseItem` now also restores hunger (no rejection at full HP).
  Surfaced in `PlayerSnapshot.Hunger` / `MaxHunger`; HUD shows
  `_hungerLabel` with peckish/hungry/starving thresholds.
  *Status + decay extension done 2026-04-30 PM*: at hunger ≤ 25 the
  derived status set yields `Hungry`; at 0 it yields `Starving` AND
  `ApplyHungerDecay` does `StarvationDamagePerStep` HP per decay tick.
  `GetStatusEffectsFor` is now public for tests. *Outstanding
  follow-up*: actual move-speed debuff hook off the `Hungry` status
  (currently advisory); visual screen-edge flash on threshold cross.
- [x] **Drug registry + addiction tracker** — *server wiring done 2026-05-02*. Declarative module
  `DrugDefinition` (id, onUseEffects, durationTicks, addictionWeight,
  withdrawalEffects). `_drugExposureByPlayer` accumulates exposure;
  past a threshold, missing a dose triggers withdrawal status. Built-in
  drug items now route through `UseItem` and emit timed status effects.
- [x] **Cleanliness / restroom mechanic** — *done 2026-05-02*. `Cleanliness`
  (0–100) decays over idle time, drops after combat hits, emits Dirty /
  Filthy derived statuses, and a server-seedable restroom interaction
  resets it to full.
- [x] **Backpack equipment slot** — *done 2026-04-30*. Added
  `EquipmentSlot.Backpack` enum value, `InventoryBoost` field on
  `GameItem`, and `BackpackBrown` starter item (8-slot boost). New
  `PlayerState.MaxInventorySlots` getter combines `BaseInventorySlots`
  (12) with the equipped backpack's boost; surfaced in
  `PlayerSnapshot.MaxInventorySlots` for HUD readout.
  *Cap enforcement done 2026-04-30 PM*: new `TryAddItem` on
  `PlayerState` + `GameState` returns false when full;
  `ProcessClaimEntity` rejects "Inventory is full" pickups before the
  karma shift; `ProcessCraftItem` falls back to the same path. Legacy
  `AddItem` stays unchecked so trophies / karma break drops can still
  bypass the cap explicitly.
- [ ] **Absurd-interaction module registry** — plug-in pattern for
  joke/dark-comedic interactions (whoopie cushion, pie throw, etc.).
  Each module declares `InteractionDefinition { id, range, cooldown,
  serverEffect }`; matching `IntentType.AbsurdInteraction` dispatches.
  Lifts the comedic backlog out of `BRAINSTORMING.md` into a real seam.

### System / architecture (foundations that unblock other work)

- [x] **NPC dialogue tree DSL** — *first slice done 2026-04-30 PM*.
  New [scripts/Data/DialogueModels.cs](scripts/Data/DialogueModels.cs)
  defines `DialogueChoice`, `DialogueNode`, `DialogueTree`, and a
  static `DialogueRegistry` with a built-in
  `mara_clinic_default` tree showing branching with offer / ask /
  decline paths. `BuildChoiceArray` adapts a node into the legacy
  `NpcDialogueChoice[]` shape so existing HUD/server consumers keep
  working — non-action choices project their `NextNodeId` into
  `ActionId` as `dialogue_advance:<id>` so a server dispatcher can
  branch. Runtime override + reset support. 11 smoke tests cover
  resolve, traversal, adapter, override, and reset. *Outstanding
  follow-up*: server-side tree walker (`dialogue_advance:` route),
  per-NPC binding via `NpcProfile.DialogueTreeId`.
- [x] **Loot drop table registry** — *first slice done 2026-04-30 PM*.
  New [scripts/Data/LootModels.cs](scripts/Data/LootModels.cs) defines
  `LootTable`, `LootTableEntry`, `LootRollResult`, and a static
  `LootTableCatalog` with built-in tables (`supply_drop_common`,
  `supply_drop_ammo`, `supply_drop_medical`, `container_scavenge`,
  `downed_player_drops`). `Roll(tableId, Random)` does weighted
  selection with quantity ranges; deterministic with a seeded RNG.
  Runtime override + reset support. 11 smoke tests cover lookup,
  unknown-table, determinism, qty-range, and override flow.
  *Outstanding follow-up*: refactor existing ad-hoc drop arrays in
  `AuthoritativeWorldServer` (containers, kills, supply drops) to
  cite a table id instead of enumerating items inline.
- [x] **Audio event registry** — *first slice done 2026-04-30*. New
  [scripts/Audio/AudioEventCatalog.cs](scripts/Audio/AudioEventCatalog.cs)
  exposes `Resolve(eventId)` with built-in mappings (`karma_break`,
  `contraband_detected`, `door_opened`, etc.) plus runtime override
  via `Register`. Substring fallback handles compound ids
  (`player_attacked_with_pistol` → `hit_thud.ogg`). Smoke tests cover
  built-in lookup, unknown event, runtime override, fallback, reset.
  *Outstanding follow-up*: actual clip files + AudioStreamPlayer wiring.

### Prototype music + appearance (added by request 2026-04-30)

- [x] **Prototype gameplay music** — *done 2026-04-30*. New
  [scripts/Audio/PrototypeMusicPlayer.cs](scripts/Audio/PrototypeMusicPlayer.cs)
  procedurally generates background music (mirrors the menu theme's
  `AudioStreamGenerator` pattern). Three themes: `SandboxCalm` for
  `Main.tscn`, `EventTension` for `InGameEventPrototype.tscn`,
  `ScenarioAmbient` for `EventPlaybackPrototype.tscn`. Volume + theme
  are inspector-tweakable.
- [x] **Pants + shirt appearance layers** — *done 2026-04-30*. The
  player_v2 manifest now has `pants` and `shirt` slots placed AFTER
  `outfit` in the layer order so they draw on top. New `PantsLayerId`
  / `ShirtLayerId` fields on `PlayerAppearanceSelection` and the
  default kit ships with `pants_blue_32x64` + `shirt_black_32x64`
  visible on the model. Server `SetAppearance` payload reads/writes
  both. New `PlayerController.CyclePantsLayerId` / `CycleShirtLayerId`
  advance through `{none → blue → none}` and `{none → black → none}`.
  HUD `BuildAppearanceCyclePayload` recognises the new slots.
  *HUD/keybinding follow-up done 2026-05-02*: Appearance panel now includes pants/shirt labels plus Cycle pants (M) and Cycle shirt (H) buttons; M/H also cycle those slots directly from gameplay. Smoke tests cover UI nodes, formatting, and payloads.
- [x] **Witness propagation system** — *done 2026-05-02*. Attack events
  carry nearby player/NPC witness ids, expose witness count in event data,
  and scale attack karma from a small unwitnessed swing to full impact at
  five or more witnesses.
- [ ] **Match replay log** — write per-tick `(intent, delta)` pairs to
  a sidecar file during a match. Loader reconstructs a snapshot stream
  from the file. Useful for bug repros and balance review. Keep behind
  a config flag so production runs aren't slowed.
- [x] **Configurable server tick rate** — *verified already-done
  2026-04-30*. `TickRate` lives on
  [ServerConfig.cs](scripts/Net/ServerConfig.cs) (default 20); no
  hardcoded `TICK_RATE` constant remains in
  `AuthoritativeWorldServer`. Smoke tests can construct configs with
  custom rates already.

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
