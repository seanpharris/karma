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

### Ready: round 9 — agent 1 (variant-aware random spawning)

Drafted 2026-05-02 PM. User flagged that the world should "feel
randomly generated every time it's played" — so every art category
needs **multiple variants** and the world generator must pick one
at random per spawn.

Asset state on disk after 2026-05-02 PM batch (totals will land
as the variant batches finish):
- `assets/art/themes/medieval/buildings/` — 12 base + ~32 variants
  (smithy_a/b/c, tavern_a/b/c, etc.)
- `assets/art/themes/medieval/structures/` — 24 base + ~38 variants
  (chest_wood_a/b, chest_iron_a, barrel_a/b/c, etc.)
- `assets/art/themes/medieval/environment/` — 10 base + ~34 variants
  (tree_oak_a/b/c, tree_pine_a/b, tree_birch_a, bush_berry_a/b, etc.)
- `assets/art/themes/medieval/decals/` — 8 base + ~24 variants
- `assets/art/themes/medieval/tiles/` — 28 ground tile variants
  (grass × 6, dirt × 5, path × 5, mud × 2, sand × 2, floor × 4,
  water × 2, snow × 2)

66. **Variant-aware art registry pattern.**
    Generalise `ItemArtRegistry` (#47) /
    `BuildingArtRegistry` (#59) / `StructureArtRegistry` (#55) /
    `BannerArtRegistry` (#62) so each can return a random variant.
    - Add `Get(themeId, kind, worldId, entityId) -> ArtEntry` that
      hashes `(worldId, entityId)` to deterministically pick one
      of the available variants. Same world + entity always picks
      the same variant; new world picks a different one.
    - Variant discovery: at registry init, scan
      `assets/art/themes/<theme>/<category>/<kind>*.png`. Treat
      `kind.png` and `kind_a.png`, `kind_b.png`, etc. as variants
      of the same `kind`. `*_pressed`, `*_open`, etc. (semantic
      suffixes from a fixed allow-list) are NOT variants.
    - Acceptance: smoke test that two different `worldId`s pick
      different variants for the same kind; same `(worldId,
      entityId)` always picks the same one; if no variant exists,
      falls back to `kind.png`.

67. **Variant tile renderer — random ground tiles per cell.**
    Today `WorldTileIds.GroundScrub` resolves to a single
    Cainos-derived atlas region. With ~28 variant tiles under
    `assets/art/themes/medieval/tiles/`, the renderer should pick
    a variant per cell (deterministic by `(worldId, x, y)` hash)
    so the map looks textured instead of repeating one tile.
    - Map logical tile id → set of variant ids (e.g.
      `GroundScrub` → `tile_grass_a`..`tile_grass_f`,
      `GroundDust` → `tile_dirt_a`..`tile_dirt_e`,
      `PathDust` → `tile_path_a`..`tile_path_e`).
    - Update `TileRenderer` (or whatever paints the tile map) to
      query the variant by per-cell hash.
    - Acceptance: smoke test the variant picker; manual: launch
      with two different seeds, see different tile distributions.
    - **Do not** edit `ThemeArtRegistry.cs` — extend its registry
      with a sibling `TileVariantRegistry` instead.

68. **Building / structure / environment variant spawning.**
    World generator currently spawns one structure per archetype.
    Update so each spawn picks a variant via `(worldId,
    entityId)` hash:
    - `WorldGenerator.GenerateLocations` → for each station,
      pick a building variant id from
      `BuildingVariantRegistry.GetForArchetype(archetype)`.
    - `WorldGenerator.GenerateOddityPlacements` → pick a
      structure variant id from
      `StructureVariantRegistry.GetVariantsFor(kind)`.
    - `WorldGenerator.decorate_world` (the new decoration pass
      from #63) → scatter environment variants randomly.
    - Acceptance: smoke test that two seeds produce different
      sets of variant ids; manual: regenerate the world, see
      different building/tree mixes.

69. **Theme audit + variant manifest.**
    Output a `assets/art/themes/medieval/MANIFEST.json` listing
    every variant under each kind, generated at art-pipeline
    time. This is what the registries from #66 read instead of
    scanning the filesystem on every lookup.
    - New tool: `tools/build_theme_manifest.gd` walks
      `assets/art/themes/<theme>/` and emits
      `MANIFEST.json` with shape:
      `{ buildings: { smithy: ["smithy.png", "smithy_a.png", ...], ... }, ... }`.
    - The registries from #66 read this manifest at startup
      instead of scanning at runtime.
    - Acceptance: running the tool produces a manifest with all
      329+ entries listed by kind; registries pass smoke tests
      using the manifest.

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
---

# Consolidated Reference Appendix

Below: every former `docs/*.md`, `AGENTS.md`, `ART_AUDIT.md`, `ART_NEEDED.md`,
and `SOUND_NEEDED.md` merged into TASKS.md as labeled sections. Consolidated
2026-05-03 to make TASKS.md the single source of work tracking + reference.

Each section preserves the original heading hierarchy verbatim. Some content
overlaps with the active task queue at the top of this file; in case of
conflict, **the active queue takes precedence** for the current state of
work, while these sections are the historical / reference record.

## Index

### Architecture, Standards, & Workflow
- [AGENTS — Agent onboarding, branches, verification, architecture rules](#agents)
- [Server architecture](#server-architecture)
- [Game design](#game-design)
- [Karma system reference](#karma-system)
- [Theme art curation standard](#theme-art-curation-standard)
- [Character animation pipeline](#character-animation-pipeline)
- [Character art generation workflow](#character-art-generation-workflow)
- [PixelLab MCP workflow](#pixellab-mcp-workflow)
- [Testing launch paths](#testing-launch-paths)
- [Playtest checklist](#playtest-checklist)
- [Interior design contract](#interior-design-contract)
- [Downed / carry / rescue mechanics](#downed-carry-rescue-mechanics)

### Art Inventories & Plans
- [Art audit (point-in-time)](#art-audit)
- [Art needed (per-step)](#art-needed)
- [Item thumbnails inventory](#item-thumbnails-inventory)
- [Medieval theme inventory](#medieval-theme-inventory)
- [Medieval NPC randomization](#medieval-npc-randomization)
- [Sprite modeling status](#sprite-modeling-status)
- [Player V2 next-10 plan](#player-v2-next-10-plan)
- [Player model generation next 15](#player-model-generation-next-15)
- [LPC everywhere plan](#lpc-everywhere-plan)
- [PixelLab medieval buildings queue](#pixellab-medieval-buildings-queue)
- [Prototype model art prompts](#prototype-model-art-prompts)
- [Professional character art systems](#professional-character-art-systems)

### Audio
- [Sound needed (per-step)](#sound-needed)
- [Medieval audio inventory](#medieval-audio-inventory)
- [Proximity chat & NPC voice research](#proximity-chat-npc-voice-research)
- [NPC conversational AI plan](#npc-conversational-ai-plan)

### Gameplay
- [Local chat prototype](#local-chat-prototype)
- [World interaction next 15](#world-interaction-next-15)
- [Prototype progress & roadmap](#prototype-progress-and-roadmap)
- [Recent work log 2026-05-03](#recent-work-2026-05-03)
- [Reusable procgen research](#reusable-procgen-research)

---

## AGENTS

_(Consolidated 2026-05-03 from `AGENTS.md`. Original file deleted; this section is the canonical copy.)_

# Agent Notes

## Project

Karma is a Godot 4 .NET/C# top-down multiplayer prototype. It should feel visually simple like a 2D life sim, but the backend should stay server-authoritative and scalable.

Core ideas:

- Players Ascend or Descend through uncapped karma.
- The highest positive player is the Saint; the lowest negative player is the Scourge.
- Only one Saint and one Scourge should exist at a time.
- Death causes a Karma Break: the player respawns, but their karma path/status resets.
- The first game mode is a 30-minute match where the Saint and Scourge at match end both win.
- Scrip is spendable currency and is separate from karma.
- The game should support absurd, comic, helpful, harmful, PvP, trade, NPC, quest, and social-betrayal interactions.

## Branches

- Work primarily on `develop`.
- `main` is for stable merges.
- `add-art` exists for art contributions.
- Commit coherent slices when verification passes. Push only when explicitly asked
  or when the current task clearly includes publishing the finished slice.

## Local Setup

- Workspace: `C:\Users\pharr\code\karma`
- Engine: Godot 4 .NET
- Known local Godot folder: `C:\Users\pharr\Downloads\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64`
- .NET 10 is acceptable for local development as long as the Godot C# project builds.

## Verification

Before finishing code changes, run the full verification chain. Run each step only if the previous exits `0`.

**From PowerShell:**
```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test.ps1; if ($LASTEXITCODE -eq 0) { powershell -ExecutionPolicy Bypass -File .\tools\snapshot.ps1 }; if ($LASTEXITCODE -eq 0) { powershell -ExecutionPolicy Bypass -File .\tools\check.ps1 } else { exit $LASTEXITCODE }
```

**From bash (e.g. Claude Code on Windows):**
```bash
powershell.exe -ExecutionPolicy Bypass -File tools/test.ps1 && \
powershell.exe -ExecutionPolicy Bypass -File tools/snapshot.ps1 && \
powershell.exe -ExecutionPolicy Bypass -File tools/check.ps1
```

Known note: `tools\check.ps1` may print Godot cleanup/leaked RID warnings. Treat them as non-failing when the command exits with code `0`.

## Multi-Agent Workflow

This repo may be edited by more than one agent or by the user between turns.
Assume uncommitted work may be valuable peer work, not disposable scratch.

At the start of a work turn:

- Run `git status --short --branch`.
- Inspect uncommitted changes with `git diff --stat` and targeted `git diff`.
- Read new untracked source files before editing nearby systems.
- If the existing work is coherent, continue from it instead of redoing it.
- If the existing work is large, run verification before making extra changes so
  failures can be attributed cleanly.
- If uncommitted changes are contradictory, incoherent, or leave the project
  non-building, stop and report the situation to the user rather than silently
  resolving it yourself.

When continuing another agent's work:

- Preserve their intent and code unless it is clearly broken.
- Make small follow-up edits that integrate, verify, or document the work.
- Do not revert, delete, rename, or overwrite another agent's files unless the
  user explicitly asks or the change is required to make the project build.
- Do not mix unrelated cleanup with another agent's feature slice.
- If generated debug files appear under `debug/`, ignore or gitignore them unless
  they are intentionally requested artifacts.

Before committing:

- Prefer one coherent commit per feature or integration slice.
- Include all source, docs, and real assets needed for that slice.
- Exclude local debug outputs, temporary logs, and exploratory images.
- Run the verification command above before committing. Push only when explicitly
  asked or when the current task clearly includes publishing the finished slice.
- In the final note, mention whether the work was continued from existing
  uncommitted changes.

## Architecture Rules

- The server owns truth.
- Clients send intent; the server validates, mutates state, and emits snapshots/events.
- Keep gameplay mutations in the authoritative server/state path, not only in scene scripts.
- Use interest snapshots for scalable visibility instead of broadcasting everything to every client.
- Large-world target is `1000 x 1000` tiles, chunked. Prototype can stay `64 x 64`.
- Prototype starts at 4 players, but code should not hard-code that where a config belongs. The current stress target is 100 players per world.
- LLMs generate proposals, never live authoritative state. Parse, validate, then apply structured data through server-owned adapters.
- Template code can be used as inspiration, but port only narrow pieces that fit
  the Godot 4 .NET/C# server-authoritative architecture. Keep attribution in
  `THIRD_PARTY_NOTICES.md` when adapting third-party code.

Important areas:

- `scripts/Core/GameState.cs`: shared player, karma, wallet, inventory, quest, relationship, and event state.
- `scripts/Net/AuthoritativeWorldServer.cs`: authoritative intent handling, match timer, snapshots, server events.
- `scripts/Net/ServerIntent.cs`: network/intent/snapshot DTOs.
- `scripts/Net/NetworkProtocol.cs`: JSON-friendly protocol envelope.
- `scripts/World/`: generated world, tile rendering, server-rendered pickups/structures.
- `scripts/Art/`: prototype sprite/structure catalogs, native character sprites,
  atlas mappings, and procedural fallbacks.
- `scripts/UI/HudController.cs`: HUD plus lightweight prototype overlays such as
  the `I` inventory panel.
- `scripts/Util/DirectionHelper.cs`: cardinal direction helper adapted from a
  Godot 2D top-down template pattern for movement/facing/animation names.
- `scripts/Tests/GameplaySmokeTest.cs`: primary smoke/regression test.

## Art Paths

Current expected atlas paths:

- `assets/art/tilesets/scifi_station_atlas.png`
- `assets/art/character.png`
- `assets/art/sprites/scifi_engineer_player_sheet.png`
- `assets/art/sprites/scifi_item_atlas.png`
- `assets/art/sprites/scifi_utility_item_atlas.png`
- `assets/art/sprites/scifi_weapon_atlas.png`
- `assets/art/sprites/scifi_tool_atlas.png`
- `assets/art/structures/scifi_greenhouse_atlas.png`

Atlas rendering is opt-in per mapped source rectangle. If a region is unknown, keep the procedural/color fallback readable rather than guessing.

The generated engineer player sheet also keeps its chroma-key source at
`assets/art/sprites/scifi_engineer_player_sheet_chroma.png`. The runtime sheet
is the transparent PNG. Do not wire the chroma source into Godot scenes unless
debugging asset processing.

## Character Sprite Sheet Standard

Use this spec for generated or hand-authored playable character sheets so future
agents can map frames consistently.

- Runtime frame size: `32 x 32 px`.
- Sheet background: transparent PNG whenever possible. If generated by image
  tools that cannot output transparency, use a flat chroma-key background and
  remove it before wiring the asset into Godot.
- Pivot/origin: bottom center, aligned to the feet.
- Character footprint: keep the feet near the lower center of each frame.
- Visual character height: about `28-32 px`, leaving a little top padding.
- Initial direction set: `front`, `back`, `left`, `right`.
- Optional direction set: add `front-right`, `back-right`, `back-left`, and
  `front-left` after the four cardinal directions are working.
- Walk cycle: `4` frames per direction.
- Idle: `1` frame per direction.
- Export filter: nearest-neighbor, no smoothing, no shadows baked into the
  transparent background.
- Avoid labels, UI text, grid lines, rulers, metadata panels, and decorative
  presentation framing in runtime sheets. Reference sheets can include those,
  but runtime atlases should be clean frame grids only.

Suggested runtime layout for a compact four-direction sheet:

```text
row 0: idle_front, idle_back, idle_left, idle_right
row 1: walk_front_1, walk_front_2, walk_front_3, walk_front_4
row 2: walk_back_1, walk_back_2, walk_back_3, walk_back_4
row 3: walk_left_1, walk_left_2, walk_left_3, walk_left_4
row 4: walk_right_1, walk_right_2, walk_right_3, walk_right_4
```

Image generation prompt template:

```text
Create a clean 2D pixel-art top-down RPG character sprite sheet for Karma.
Subject: <role/personality/outfit>, sci-fi frontier colony style.
Runtime frames: each frame is exactly 32x32 px, nearest-neighbor pixel art.
Layout: four cardinal directions only. Row 0 has idle_front, idle_back,
idle_left, idle_right. Rows 1-4 have four-frame walk cycles for front, back,
left, and right.
Background: transparent if possible; otherwise perfectly flat #00ff00
chroma-key with no shadows, no gradients, no texture, and no green in the
character.
Constraints: clean frame grid only, no labels, no metadata, no UI panels, no
watermark, no presentation sheet, no text. Keep feet bottom-centered in every
frame. Keep proportions consistent across frames.
```

## Godot-Native Art Workflow

Karma is moving toward the Godot-native art workflow used by the referenced
Godot 2D top-down template:

- Project settings should keep pixel art crisp:
  `rendering/textures/canvas_textures/default_texture_filter=0`,
  `rendering/2d/snap/snap_2d_transforms_to_pixel=true`, no generated mipmaps,
  and `process/fix_alpha_border=true` for texture imports.
- Humanoid actors should use `PrototypeCharacterSprite`, which builds a
  Godot `AnimatedSprite2D` with `SpriteFrames` from cataloged atlas regions.
- Mapped props/items should use `PrototypeAtlasSprite`, which builds a Godot
  `Sprite2D` with an `AtlasTexture` from the same catalog data.
- Mapped structures should use `StructureSprite`, which now also builds a
  native `Sprite2D`/`AtlasTexture` from `StructureArtCatalog`.
- Shared atlas math lives in `AtlasFrame`/`AtlasFrames`. Use it for source
  rectangles, display scale, and anchoring instead of duplicating renderer math.
- `ArtAssetManifest` discovers every cataloged atlas path and verifies the
  files exist. Add newly mapped runtime sheets to catalogs so the manifest and
  smoke tests catch missing assets.
- Keep `PrototypeSprite` for temporary procedural fallbacks when an atlas region
  is missing.
- Runtime character sheets should eventually use clean `32 x 32` frame grids.
  When a proper grid exists, map it with
  `PrototypeSpriteCatalog.FourDirectionGridAnimations(origin, frameSize: 32)`.
  The expected animation names are `idle-down`, `idle-up`, `idle-left`,
  `idle-right`, `walk-down`, `walk-up`, `walk-left`, and `walk-right`.
  Walk rows default to four frames each.
- The generated 8-direction template/reference sheet maps to
  `CharacterSheetLayout.EightDirectionTemplate(origin)` once it is exported as a
  clean runtime PNG with only frames: 8 columns x 9 rows, `32 x 32` frames,
  `256 x 288` total. Do not catalog the annotated prompt/reference PNG directly.
- Stable world tiles should eventually become Godot `TileSet` resources with
  atlas sources, terrain rules, collision, and animated tile data. Until then,
  the server-friendly generated tile renderer can keep drawing cataloged atlas
  regions chunk by chunk.
- Store source/reference sheets separately from runtime sheets. Runtime sheets
  should be clean transparent PNGs with no labels, presentation framing, or
  metadata panels.

## Coding Style

- Prefer existing patterns over new abstractions.
- Keep edits scoped to the requested gameplay slice.
- Use server-owned DTOs/snapshots for anything that will matter in multiplayer.
- Add focused smoke tests for new mechanics, especially server intent validation
  and snapshot data. Add cases to `scripts/Tests/GameplaySmokeTest.cs`. Every
  new server intent should have at least one smoke test case.
- Do not revert user changes.
- Avoid unrelated refactors.

## Gameplay Plan

Steps complete on `develop`. Add new slices to the table as they are planned.

### Steps 1–29 (complete as of 2026-04-29)

| # | Feature | Status |
|---|---------|--------|
| 1 | Multi-step quest server state (step conditions, per-step karma/scrip) | ✅ done |
| 2 | Repair mission quest (locate fixture → get tool → repair) | ✅ done |
| 3 | Delivery quest (collect item at source → bring to destination) | ✅ done |
| 4 | Rumor quest (discover secret → expose or bury → consequence) | ✅ done |
| 5 | Paragon Favor perk (+50 karma threshold) | ✅ done |
| 6 | Abyssal Mark perk (-100 karma threshold) | ✅ done |
| 7 | Posse formation (InvitePosse/AcceptPosse/LeavePosse intents) | ✅ done |
| 8 | Posse HUD panel (member list, karma, health) | ✅ done |
| 9 | Saint/Scourge NPC behavior (greetings, prices, reactions) | ✅ done |
| 10 | Chat tabs — Local / Posse / System | ✅ done |
| 11 | Interior audibility filtering | ✅ done |
| 12 | Combat heat tracking (tile-chunk heat map with decay) | ✅ done |
| 13 | Smarter respawn placement (avoid heat, prefer stabilized stations) | ✅ done |
| 14 | Downed state (0 HP countdown, can still chat) | ✅ done |
| 15 | Rescue intent (rescuer carries downed player, Ascend reward) | ✅ done |
| 16 | Clinic recovery hook (extend countdown, NPC auto-revive for scrip) | ✅ done |
| 17 | Road/path generation (spanning path graph at world-gen) | ✅ done |
| 18 | Path-aware world rendering (road tiles between station pairs) | ✅ done |
| 19 | Mount/vehicle entity model (speed modifier, parking, occupancy) | ✅ done |
| 20 | Mount/dismount intents + karma hooks | ✅ done |

### Steps 30–40 (active plan as of 2026-04-29)

Theme groups: **match lifecycle** → **karma depth** → **player state** → **world events**
→ **economy/factions** → **crafting/social** → **map and UI**.
Each step is independent enough to land as a focused slice; earlier steps within a
group are prerequisites for later ones in the same group.

| # | Feature | Status |
|---|---------|--------|
| 21 | Karma watermark tracking — record per-player karma peak and floor across the match; store on `GameState` player | ✅ done |
| 22 | Karma title-change broadcast — server event when a player first takes or loses Saint/Scourge status mid-match | ✅ done |
| 23 | Match end summary snapshot — `MatchSummarySnapshot` record with final standings, per-player karma peak/floor, quests completed, and kills; surfaced in HUD at match end | ✅ done |
| 24 | Warden perk (karma ≥ +150) — new `IssueWanted` intent marks one player Wanted; others earn karma for downing the Wanted player | ✅ done |
| 25 | Wraith perk (karma ≤ -150) — server applies a speed modifier to players at ≤ 30% HP who hold this perk; modifier reflected in snapshot | ✅ done |
| 26 | Bounty system — players whose karma falls below −50 automatically accrue a scrip bounty; downing them (Karma Break) transfers the bounty to the scorer | ✅ done |
| 27 | Player status effects model — server-owned `PlayerStatus` set (Wanted, Wraith-buffed, Poisoned, etc.); status list included in `PlayerSnapshot`; cleared on Karma Break | ✅ done |
| 28 | Contraband item tag — items flagged as contraband decay karma once per tick while held by a player near a law-aligned NPC; `GameItem` gets an `IsContraband` flag | ✅ done |
| 29 | Lobby / ready-up flow — `ReadyUp` intent; `MatchStatus.Lobby` state before `InProgress`; timer only starts once a quorum of connected players has sent `ReadyUp` | ✅ done |
| 30 | Supply drop world event — server schedules a rare item spawn at a broadcast location; first player to reach it claims the cache; event expires after a timeout | ✅ done |
| 31 | NPC patrol routes — NPCs step between 2–3 tile waypoints on a per-tick cadence; position updated in `NpcEntity`; snapshot reflects current position | ✅ done |
| 32 | Reputation decay — NPC opinions and faction standings drift toward 0 each tick cycle proportional to inactivity; decay rate configured in `ServerConfig` | ✅ done |
| 33 | Faction store gating — `PurchaseItem` intent rejected when player's faction reputation is below the offer's minimum threshold; `ShopOfferSnapshot` includes `MinReputation` field | ✅ done |
| 34 | Station claim intent — `ClaimStation` intent lets a posse flag an unclaimed station as theirs; station owners receive passive scrip per server tick; `WorldStructureEntity` gains `ClaimingPosseId` | ✅ done |
| 35 | Death trophy drop — when a player triggers a Karma Break on another, the scorer receives a named unique item (e.g., "Ace's Dog Tag") seeded from the victim's display name | ✅ done |
| 36 | Crafting intent — `CraftItem` intent validated at a workshop structure; server holds a recipe table (`CraftingRecipe[]`); consumes ingredients and produces the output item | ✅ done |
| 37 | Posse shared quest module — `PosseQuestModule` subclass assigns the same multi-step objective to all posse members; shared completion triggers a group scrip bonus via `QuestModuleRegistry` | ✅ done |
| 38 | World tier zones — tile-level `IsLawless` flag set at world-gen for fringe areas; attacks in lawless zones skip the karma-descent penalty; shown on HUD when player enters/exits | ✅ done |
| 39 | Fog of war — `AuthoritativeWorldServer` tracks which chunks each player has visited; `CreateInterestSnapshot` excludes unvisited chunks beyond a minimum reveal radius | ✅ done |
| 40 | HUD minimap — small radar panel rendering nearby player, NPC, and structure positions as dots relative to the local player; updates from the interest snapshot each tick | ✅ done |

Art requirements for each step are tracked in `ART_NEEDED.md`.

## Quest System Overview

Multi-step quests are implemented via `QuestStep`/`QuestStepCondition` in
`scripts/Data/QuestModels.cs`. Step condition kinds:

- `None` — always satisfied
- `HoldItem(targetId)` — player must have the item in inventory
- `HoldRepairTool` — player must hold MultiTool or WeldingTorch
- `NearNpc(npcId)` — player within interest radius of the named NPC
- `NearStructureCategory(role)` — any structure with matching `Category`
  within interest radius

`CompleteQuest` intent is rejected if the quest is multi-step and not
all steps are finished (`AllStepsDone == false`).

### Plugin-Modular Quest Modules

Quest types live in `scripts/Quests/` as `QuestModule` subclasses. Each module is
self-contained: it owns quest creation (factory) and completion resolution (karma).
Add a new quest type by subclassing `QuestModule`, declaring `StationRoles`, and
registering in `QuestModuleRegistry` — the world generator and server pick it up
automatically.

Registered modules:
| Module | Station roles | Completion prefix | Karma (expose/resolve) |
|--------|--------------|-------------------|------------------------|
| `RepairMissionModule` | `workshop`, `clinic` | `generated_station_help:` | default action lookup |
| `DeliveryQuestModule` | `market` | `generated_station_help:` | default action lookup |
| `RumorQuestModule` | `notice-board` | `rumor_resolve:` | expose=+5, bury=+8 |

Stations not matched by any module get a flat `QuestDefinition` (stabilize fallback).

`QuestCreationContext` carries: `QuestId`, `LocationId`, `LocationName`, `LocationRole`,
`GiverNpcId`, `ScripReward`, `OtherPlacements` (list of `QuestPlacementInfo`).

## Current Prototype Features

_Last updated 2026-04-29 (steps 1–29 complete). This list drifts — verify against
the code before using it to plan work or assuming a feature is or isn't present._

- Top-down local movement, mouse-wheel camera zoom, and Left Shift sprint with stamina.
- `I` toggles an inventory overlay with scrip, equipment, and grouped items.
- Server-owned 30-minute match timer and Saint/Scourge winner lock.
- Uncapped karma ranks in both Ascension and Descension; Paragon Favor (+50) and Abyssal Mark (−100) perks.
- Scrip currency, player transfers, shop offers, and server-side pricing perks.
- NPC dialogue and quest choices; Saint/Scourge-aware NPC greetings, prices, and reactions.
- Multi-step quests with `AdvanceQuestStep` intent and per-step karma/scrip rewards.
- Repair, Delivery, and Rumor quest types; plugin-modular quest system (`QuestModule` + `QuestModuleRegistry`).
- Generated station quests seeded by role at world-gen.
- PvP duels, attacks, armor, weapons, and Karma Break death drops.
- Downed state (0-HP countdown, chat still active); Rescue intent (ascends rescuer karma).
- Clinic recovery hook: auto-revive near Mara/Dallen if player has enough scrip.
- Posse formation (Invite/Accept/Leave), Posse HUD panel, and Posse chat channel.
- Chat tabs — Local / Posse / System — with interior audibility filtering and volume attenuation.
- Combat heat map (tile-chunk heat with decay); heat-aware respawn placement.
- Road/path generation (MST spanning all stations) with Bresenham road-tile overlay.
- Mount/vehicle entity model (speed modifier, parking, occupancy) and Mount/Dismount intents with karma hooks.
- Server-owned world items and structures rendered from interest snapshots.
- Greenhouse structure set with basic interaction prompts/events.
- Sci-fi item, weapon, tool, utility, tile, and greenhouse atlases placed in the expected paths.


---

## Server architecture

_(Consolidated 2026-05-03 from `docs/server-architecture.md`. Original file deleted; this section is the canonical copy.)_

# Server Architecture

## Rule

The server owns truth.

Clients send intent, such as "interact with NPC", "attack", "use item", or
"place object". The server validates intent, updates world state, and broadcasts
results.

## LLM Boundary

LLMs generate proposals, never authoritative live state.

Good LLM uses:

- World themes
- NPC biographies
- Dialogue variants
- Quest proposals
- Object descriptions
- Rumors and secrets

Server-owned systems:

- Karma score
- Scrip wallets and currency transfers
- Match timer and match winners
- Inventory
- Combat
- Position
- Logical tile map and world zones
- NPC relationship state
- World object state
- Death and Karma Breaks
- World event and rumor log

LLM proposal validation rules:

- proposal size limits are enforced before use
- referenced item/action/NPC ids must exist
- generated text is bounded
- accepted proposals are converted into server DTOs
- rejected proposals never mutate live state
- model providers are hidden behind a content generation interface, so Codex,
  hosted APIs, local models, and deterministic test generators can be swapped
  without changing server gameplay systems
- model output is treated as proposal JSON: parse it, validate it, then apply it
  through the same server-owned adapter path

## Multiplayer Scale

The prototype starts at 4 players per world, but this should be treated as a
server configuration, not a gameplay constant. The architecture should support
testing larger worlds up to 100 players if the design moves that direction.

Targets:

- Prototype: 4 players per world
- Prototype map: `64 x 64` tiles
- Production large world target: `1000 x 1000` tiles
- Default world chunk size: `32 x 32` tiles
- Stress target: 100 players per world
- First match type: 30-minute Saint/Scourge race
- Authoritative host/server
- Deterministic karma calculations
- Event log for replay/debugging

## Scaling Rules

- Never broadcast every event to every client by default.
- Track player interest areas and only send nearby/relevant entities.
- Store server-owned player tile positions so interest checks do not depend on
  client scene nodes.
- Send local movement as sequenced server `Move` intent when the player changes
  tile; scene motion can be immediate, but authoritative tile position comes
  from the server path.
- Build client sync from interest snapshots: self, nearby players, global
  leaderboard standing, visible NPC snapshots, visible item entity snapshots,
  visible world events, and server events after the client's last known tick.
- Generate logical tiles with stable ids first, then let the client map those
  ids to theme-specific tileset art.
- Treat large worlds as chunked/streamed spaces. Clients should receive only
  nearby chunks/entities through interest snapshots.
- Generated tile maps expose chunk coordinates and nearby chunk queries so the
  server/client can stream map data around each player.
- Map chunk interest radius is derived from the server interest radius and
  chunk size, so tuning visibility for 4-player prototypes or 100-player worlds
  changes terrain streaming without special-case code.
- Interest snapshots include nearby map chunk snapshots when the server has a
  generated tile map registered for the world.
- Interest snapshots include shop offers only for visible vendor NPCs so clients
  cannot browse or buy from distant vendors without server validation.
- Map chunk snapshots include stable chunk keys and deterministic revisions so
  clients can skip unchanged terrain payloads as players move through large
  worlds.
- Interest snapshots carry sync hints with the requested delta tick, visible
  event counts, visible map chunk count, and a map revision checksum. This keeps
  network clients from guessing whether a snapshot is a full refresh or a
  smaller incremental update.
- The prototype client owns a small interest snapshot cache that tracks the last
  applied tick and visible chunk revisions. Future network transports should
  feed snapshots through this cache before rendering.
- The prototype world renderer consumes map chunks from the local server
  snapshot, keeping terrain rendering on the same path as future network clients.
- The client renderer keeps a loaded chunk cache and evicts chunks that leave the
  latest interest snapshot, while retaining unchanged chunk revisions.
- Let the local prototype client read the same interest snapshot summary that a
  real network client would consume, so UI/debug feedback is based on server
  visibility rather than scene assumptions.
- Keep camera zoom client-side and clamped. Zoom can change how much of the
  already-rendered local area is visible, but server interest snapshots still
  decide what terrain, players, NPCs, items, and events the client receives.
- Route future transports through explicit network message envelopes for join,
  intent, snapshot request, ping, and response/error messages. The current
  in-process protocol adapter uses those envelopes before any socket layer is
  introduced.
- Network envelopes can be encoded as JSON with readable enum names so the same
  protocol can be logged, replayed, or sent over a later transport.
- Keep NPC simulation tiered: active nearby NPCs update often, distant NPCs
  update in coarse batches.
- Keep LLM generation out of the live tick loop.
- Process player input as intent with sequence numbers.
- Keep match time server-owned. Interest snapshots include match status so
  clients can render the timer/winners without computing authority locally.
- Include current Saint/Scourge leaders in running match snapshots, then locked
  Saint/Scourge winners after finish.
- Pay match winner scrip rewards once, at the same server-owned transition that
  locks the Saint/Scourge winners.
- Once a match is finished, reject score-changing intents so the locked
  Saint/Scourge result cannot be mutated after the timer expires.
- Validate PvP attack intents on the server: connected target, range check,
  karma consequence, damage, combat event, and Karma Break if lethal.
- Validate duel request/accept intents on the server: both players must be
  connected and nearby; active duel attacks still deal damage but do not use the
  outside-duel Descend penalty.
- Include only relevant duel state in client interest snapshots so distant
  players do not receive unrelated duel updates.
- On server-owned Karma Breaks, drain loose player inventory into nearby world
  item entities so death can create recoverable loot without trusting clients.
- Treat another player's Karma Break drops as owned loot: pickup is allowed, but
  it applies a server-owned Descend consequence and includes the drop owner in
  the pickup event.
- Validate player-targeted karma actions on the server: connected target actions
  must be in range before social help, robbery, or return-item consequences can
  apply.
- Validate player-to-player item transfers on the server: connected target,
  proximity, known item id, source inventory ownership, inventory mutation,
  karma consequence, and syncable transfer event.
- Validate player-to-player scrip transfers on the server: connected target,
  proximity, positive amount, wallet balance, wallet mutation, karma
  consequence, and syncable currency event.
- Validate shop purchases on the server: known offer id, reachable vendor NPC,
  known item id, player-specific perk pricing, wallet balance, inventory
  mutation, and syncable purchase event.
- Validate item use intents on the server: known item id, equipment or tool
  behavior, target range where needed, inventory/equipment mutation, and
  syncable item event.
- Validate pickup interactions on the server: visible world item entity,
  one-time availability, player inventory mutation, and syncable pickup event.
- Validate placed objects on the server: item ownership, short placement range,
  inventory consumption, world entity creation, and syncable placement event.
- Validate dialogue starts on the server: visible NPC, server-approved choice
  ids, and syncable dialogue event.
- Validate dialogue choice selection on the server: visible NPC, approved
  choice id, required item consumption, karma mutation, and syncable choice
  event.
- Include visible NPC dialogue options in client interest snapshots so clients
  render only server-approved choices.
- Validate quest start/completion on the server: visible quest giver, required
  item consumption, completion karma mutation, scrip reward payout, and syncable
  quest event.
- Include quests from visible NPC givers in client interest snapshots, while
  hiding distant quest state.
- Validate entanglement start/exposure on the server: visible NPC, known affected
  NPC, approved karma action, relationship/faction mutation, rumor event, and
  syncable entanglement event.
- Make `MaxPlayers` a world/server config value.
- Gate player joins through the server profile so a 4-player prototype world can
  reject overflow while a 100-player profile can accept larger sessions.
- Design UI around parties/nearby players, not a full list of 100 players.

## Persistence

The server should be able to create structured snapshots of authoritative state:

- players, position, health, karma, equipment
- current Saint/Scourge leaderboard standing
- inventory
- quests
- NPC relationships
- entanglements
- duel request/active/ended state
- world event history

Snapshots are the basis for saves, debugging, replay tools, migration tests, and
eventual server handoff.

## Config Profiles

Initial profiles:

- `Prototype4Player`: 4 max players, small map, wider interest radius
- `Large100Player`: 100 max players, `1000 x 1000` tile map target, tighter
  interest radius

Both profiles also define a short combat range so PvP remains server-validated
instead of trusting client-side hit claims.

The 100-player profile is a design target, not a promise that one machine can
run all features without further optimization.


---

## Game design

_(Consolidated 2026-05-03 from `docs/game-design.md`. Original file deleted; this section is the canonical copy.)_

# Karma Game Design

## Core Fantasy

Karma looks like a simple 2D life sim, but each world is an unstable social sandbox.
Players can become beloved, feared, ridiculous, helpful, dangerous, or some messy
combination of all of those.

## Core Loop

1. A player starts a world.
2. The server generates a theme, town, NPCs, objects, factions, and conflicts.
3. Up to 4 players enter the world.
4. Players complete tasks, talk to NPCs, fight, trade, prank, steal, help, and betray.
5. Actions cause the player to Ascend or Descend.
6. Players earn and spend scrip for tools, cosmetics, services, trades, and bribes.
7. Extreme karma unlocks perks, status, and social power.
8. Death causes a Karma Break and resets the player's path.

## World and NPC Generation

World generation should be imaginative but still mechanically useful. The server
starts from **social stations**: places that create decisions instead of just
scenery. Examples include clinics, markets, repair yards, rumor boards, saloons,
restricted sheds, oddity yards, duel rings, farms, black markets, apology engines,
broadcast towers, war memorials, and witness courts.

Each generated location carries:

- a role, such as care, trade, repair, rumor, combat, crime, or redemption;
- a local karma hook, such as repair vs sabotage, gift vs theft, expose vs bury,
  confession vs fake remorse, or clean duel vs cheap shot;
- a suggested faction that should care about what happens there.

NPCs are then derived from those stations. Instead of random decorative NPCs,
each generated NPC gets a role, faction, need, secret, likes, dislikes, and a
placement tied to the station that created them. This keeps environment generation
and NPC generation connected: the map produces story machines, and the NPCs make
those machines socially legible.

## First Game Mode: Match

Players begin each match at random spawn tiles chosen by the server with a soft
minimum separation so the opening does not immediately collapse into one pile.
Initial teams are not part of the default match start. Temporary in-game team or
posse status can be layered on later, but a Karma Break/death clears that status
so respawn is a true social reset.

The first shipped game type should be a timed match. Players join a generated
world and compete for 30 minutes. At the end of the timer, the current Saint
highest karma and current Scourge lowest karma are both match winners.

This creates two viable races in the same server: Ascend hard enough to become
the Saint, or Descend hard enough to become the Scourge. Karma Breaks still
matter because death resets a player's path status during the match.
When a match ends, the locked Saint and Scourge winners receive server-owned
scrip payouts in addition to their leaderboard status.

Scrip is the prototype currency. It is separate from karma: karma is the
Ascend/Descend match score and social identity, while scrip is spendable money
for tools, cosmetics, services, bribes, and player trades.
Quests can pay server-owned scrip rewards in addition to their karma and
relationship consequences.
The first prototype shop is Dallen's stall, which sells starter objects through
server-validated offers. Nearby vendor offers are exposed through interest
snapshots, so distant shops remain hidden until the player is in range. Dallen
now renders from the server NPC snapshot as the first lightweight vendor body,
and that body can browse visible offers with `-` and `=` or buy the selected
offer with `9`.
Shop prices are calculated by the server for each player, so economy perks such
as Trusted Discount and Shifty Prices change both the displayed offer price and
the authoritative wallet debit.
Tools can have server-owned effects instead of only living in inventory. The
first functional tool is the repair kit, which can heal a nearby player and is
consumed when used.
Consumables use the same server-owned item path: ration packs now restore a
small amount of health and are consumed on use, while stronger field tools still
matter for larger repairs or helping another player.

Prototype matches stay small, but the production large-world target is
`1000 x 1000` tiles at `16px` logical tile scale. Large worlds must be treated
as streamed/chunked spaces, not fully simulated or rendered to every client.
The default chunk size is `32 x 32` tiles, giving the large target roughly
`32 x 32` chunks for streaming and interest management.
The map chunk stream radius is derived from server visibility settings, letting
prototype and large-world profiles tune terrain bandwidth differently.
Client interest snapshots carry nearby map chunk data, so terrain streaming can
follow the same server-owned visibility path as NPCs, items, and players.
Chunks carry stable keys and deterministic revisions, which lets clients keep
cached terrain when the visible chunk has not changed.
Interest snapshots also carry a compact sync hint so future network clients can
show/debug whether they received a full refresh or an incremental update.
The local prototype client now applies snapshots through a cache that tracks
visible chunk revisions, matching the shape a real network client will need.
Players can use the mouse wheel to zoom the camera between a close character
view and a wider scouting view. The zoom range is clamped so it improves local
awareness without turning into a whole-map reveal.
The server has an in-process network protocol adapter with explicit envelopes
for joins, intents, snapshot requests, pings, and errors, ready to sit behind a
real transport later. Those envelopes can be serialized as readable JSON for
debugging, replay, and future socket messages.
The current prototype renderer draws those server-provided chunks with
placeholder colors until the tileset atlas mapping is ready.
Renderer state is chunk-cached, so visible chunks can be added, updated, and
evicted as players move through larger worlds.
Atlas rendering is opt-in per logical tile id. Until exact source regions are
mapped from the sheet, placeholder colors remain the readable fallback.

Calming Presence softens negative NPC relationship reactions through goodwill,
while Dread Reputation softens some harmful, violent, deceptive, or humiliating
NPC reactions through fear.

Match time is server-owned and deterministic. The server advances elapsed match
seconds, emits a `match_finished` event when time expires, and locks the Saint
and Scourge winners from the leaderboard at that moment.
After winners are locked, score-changing intents are rejected while movement can
continue for post-match wandering, debugging, and result review.

The local prototype advances the server match timer during play and shows the
server snapshot's match summary in the HUD.
During a running match, the match summary shows the current Saint and Scourge
leaders so players can chase either victory path before the winners are locked.

## Procedural World Data

World generation should produce structured data first:

- theme
- logical tile map
- locations
- NPC profiles
- oddities
- factions
- local conflicts

LLM-generated content should enter as proposed structured data, then the server
validates it before it becomes live state.
Model access is routed through a content generation adapter. During prototyping
that adapter can be deterministic or Codex-backed; later it can point at a
smaller local model as long as it returns the same proposal schema.
Provider text is parsed as proposal JSON and rejected if malformed or invalid,
so model swaps should affect generation quality rather than core game rules.

Tile art should map onto stable logical ids such as `clinic_floor`,
`wall_metal`, `door_airlock`, and `duel_ring_floor`. This lets us keep
procedural generation, collision, and server state stable while swapping
placeholder visuals for real tileset sheets later.

Theme art is routed through an art registry. Each logical tile id has a
placeholder color now and an atlas path/coordinate reserved for future sprites,
starting with `assets/art/tilesets/scifi_station_atlas.png`.
The current prototype uses mapped regions from that sci-fi atlas for core
terrain and structure ids while keeping placeholder colors available for future
unmapped tile ids.
Actor sprites use the same approach: player, Mara, and peer stand-in have
source regions reserved in `assets/art/sprites/scifi_character_atlas.png`, with
procedural fallbacks until that sheet is present locally.
Core item world models use `assets/art/sprites/scifi_item_atlas.png` for
whoopie cushion, deflated balloon, repair kit, practice stick, work vest, and
scrip when that item sheet is available.
Utility item world models use `assets/art/sprites/scifi_utility_item_atlas.png`
for ration pack, data chip, filter core, contraband package, apology flower,
and portable terminal.
Weapon world models use `assets/art/sprites/scifi_weapon_atlas.png` for the
starter sci-fi weapon set, from non-lethal stun baton up through heavy weapons
and thrown explosives.
Tool world models use `assets/art/sprites/scifi_tool_atlas.png` for the wider
utility set, including repair, medical, hacking, scanning, mobility, and
resource tools.
Large structure models are catalog-first. The greenhouse sheet lives at
`assets/art/structures/scifi_greenhouse_atlas.png` and maps greenhouse variants
and modular parts without placing them into the active prototype scene yet.
The prototype server seeds greenhouse structures into local interest snapshots;
`WorldRoot` renders them from server-owned structure state with atlas art when
available and a procedural fallback while the sheet is absent. Structures carry
server-owned integrity. Inspecting records a world event, repairing with a
multi-tool or welding torch Ascends, restores integrity, pays a small repair
bounty, and improves Civic Repair Guild reputation. Sabotaging Descends,
damages integrity, and hurts Civic Repair Guild reputation.

The prototype item set covers the current loops: oddities (`whoopie_cushion`,
`deflated_balloon`, `apology_flower`), support tools (`repair_kit`,
`ration_pack`, `filter_core`), equipment (`practice_stick`, `work_vest`), and
interactible objects (`data_chip`, `contraband_package`, `portable_terminal`).
The sci-fi weapon expansion adds `stun_baton`, `electro_pistol`, `smg_11`,
`shotgun_mk1`, `rifle_27`, `sniper_x9`, `plasma_cutter`, `flame_thrower`,
`grenade_launcher`, `railgun`, `impact_mine`, and `emp_grenade`.
The sci-fi tool expansion adds `multi_tool`, `welding_torch`, `medi_patch`,
`lockpick_set`, `flashlight`, `portable_shield`, `hacking_device`, `scanner`,
`grappling_hook`, `chem_injector`, `power_cell`, `bolt_cutters`, and
`magnetic_grabber`.
The runtime catalog exposes all starter items through `StarterItems.All`; the
prototype scene auto-spawns any cataloged item that is not already hand-placed
into a small pickup/art showcase near the starter area.

## Karma Perks

Karma perks should be mechanical identity, not just titles. Ascension perks make
helpful/social play easier to sustain; Descension perks make darker play more
powerful but more dangerous. Current wired examples include discounts,
relationship-damage softening, stamina modifiers, Dread Reputation reaction
softening, and Rumorcraft: once unlocked on the Descension path, exposed rumors
become global server-owned world events instead of only local gossip.

## NPC Relationships

Karma is global score and path identity. NPC relationships are local memory.

An action can Ascend the player while still upsetting one NPC, or Descend the
player while pleasing another. Relationship state should be server-owned and
tracked per NPC/player pair.

## Factions

Factions are larger social memory. Helping one NPC can move reputation with
their faction, while betrayal or public scandal can damage faction standing.
Faction reputation should influence access, prices, protection, rumors, and
quest availability later.

## Entanglements

Entanglements are secret or exposed social bonds such as romance, debt,
blackmail, rivalry, or betrayal. They are tracked as structured state so dark
actions can have persistent consequences without relying on freeform text alone.
Exposing an entanglement should create rumor/event hooks and usually damages
multiple relationships at once.

## World Events

Important consequences should be recorded as structured world events. Rumors,
quest outcomes, combat incidents, and karma milestones can then drive NPC
dialogue, future quests, and server history without relying on raw UI messages.

## Quests

Quests are structured server-owned state, not freeform dialogue. An LLM can
propose quest flavor, but the server validates required items, completion
conditions, rewards, relationship effects, and karma shifts.

## Equipment

Equipment is server-owned state. Weapons, armor, and tools can share the same
item model but use slots and stats to affect validated server actions.

## Tone

Cozy, absurd, socially reactive, and occasionally dark.

Objects should often be useful in several ways: joke, weapon, gift, bribe, clue,
quest item, or mistake.

Loose inventory objects can be placed back into the world through server intent.
This lets oddities such as balloons and joke objects become shared world props
instead of private inventory text.
Placed objects remain server-owned pickups. Other nearby players can collect
them through the same interaction rules, which turns silly object placement into
the seed of trades, theft, bait, clutter, and emergent jokes.

Player inventories are part of the social sandbox. Giving an item, stealing a
satchel item, and returning it should move real server-owned objects, not only
change karma text. Scrip transfers follow the same server-owned social rule:
gifting money Ascends, while stealing money Descends and moves the actual
currency balance.

A Karma Break drops loose inventory into the world as recoverable objects.
Players keep their body and respawn, but death can scatter props, gifts, stolen
goods, and jokes into the shared space.
Picking up another player's Karma Break drop is allowed, but it is remembered as
claiming someone else's scattered goods and Descends the picker. Returning that
specific drop to its owner is recognized as a restorative gift and Ascends the
returning player.

## PvP

PvP is allowed, but consequences depend on context.

- Friendly duel: no or minor karma shift
- Attacking a peaceful player: Descend
- Saving a player from death: Ascend
- Robbing a player after death: Descend
- Returning lost items after a Karma Break: Ascend

The goal is not to prevent bad behavior. The goal is to make the world remember it.

Duels are server-owned consent state. A player can request a duel with a nearby
player, and attacks during an accepted duel are marked as duel combat instead of
outside-duel aggression.

Prototype controls let the local player request a duel from the stand-in, then
let the stand-in accept it. This keeps consent explicit while still making the
loop quick to test in one running client.
Near the stand-in, the prototype also exposes quick keys for combat/tool loops:
`Z` equips the practice stick, `X` equips the work vest, `C` places the first
loose inventory item, `R` uses a repair kit on the stand-in, `7` gifts 5 scrip,
and `9` steals 3 scrip. `T` uses a repair kit on the local player. `8` lets the
stand-in attack the local player so the local health bar, cooldowns, and
duel-strike feedback can be tested in one client.
Movement uses WASD, and holding left Shift sprints at a modest speed boost for
faster prototype traversal. Sprinting drains stamina while held and stamina
recovers when the player stops sprinting. Empty stamina makes the player winded;
sprint resumes only after stamina recovers to a small buffer.
Some karma perks affect traversal: Beacon Aura improves stamina recovery, while
Renegade Nerve reduces sprint stamina cost.
Calming Presence softens negative NPC relationship reactions, giving high-karma
players a little more room to recover from awkward social mistakes.

## Prototype HUD

The HUD is intentionally debug-forward for now. It shows local karma, inventory,
leaderboard standing, perks, relationships, factions, quests, combat,
entanglements, duels, sprint stamina, recent rumors, and the local server
interest snapshot.
The sync line includes nearby server-approved dialogue choices and visible quest
state so we can confirm the client is rendering from authoritative state instead
of trusting scene-only assumptions.


---

## Karma system

_(Consolidated 2026-05-03 from `docs/karma-system.md`. Original file deleted; this section is the canonical copy.)_

# Karma System

## Vocabulary

- **Karma**: the numerical score, centered on 0
- **Ascend**: move in the positive karma direction
- **Descend**: move in the negative karma direction
- **Ascension**: positive karma path
- **Descension**: negative karma path
- **Karma Break**: death reset that returns a player to 0 karma
- **Saint**: the single current highest-karma player on a server
- **Scourge**: the single current lowest-karma player on a server

## Karma Scale

Players start at 0. Karma is uncapped in both directions so players can keep
Ascending or Descending indefinitely.

After `+100`, players continue through repeatable `Exalted` ranks. After `-100`,
players continue through repeatable `Abyssal` ranks. For example, `+220` is
`Exalted 2`, while `-340` is `Abyssal 3`.

Rank progress is tracked toward the next milestone. A player at `+220` sees
`20/100 toward Exalted 3`, while a player at `-340` sees `40/100 toward
Abyssal 4`.

## Leaderboard Standing

Each world tracks exactly one current positive leader and one current negative
leader:

- Highest karma: **Saint**
- Lowest karma: **Scourge**

These are exclusive server standings, separate from non-exclusive tier names.
Only one player can be Saint and only one player can be Scourge at a time.
Snapshots store both the global leaderboard and each player's current standing
so save/debug tools can verify the exclusivity rule.

## Perks

Perks unlock from karma magnitude and current leaderboard standing.

Ascension examples:

- +10: Trusted Discount
- +20: Calming Presence (softens negative NPC relationship reactions)
- +35: Beacon Aura (faster stamina recovery)
- +50: Paragon Favor
- +100: Exalted Grace
- Every +100 after that: repeat Exalted rank perk

Descension examples:

- -10: Shifty Prices
- -20: Rumorcraft
- -35: Renegade Nerve (reduced sprint stamina cost)
- -50: Dread Reputation (fear softens some negative NPC reactions)
- -100: Abyssal Mark
- Every -100 after that: repeat Abyssal rank perk

Standing perks:

- Saint: current highest positive player
- Scourge: current lowest negative player

## Death

Player death causes a **Karma Break** for the player who died. Their health and
body return, but their karma score, path, and path perks reset.

## Entanglements

Some relationship actions create persistent entanglements. These can Descend the
player, alter NPC opinions, and become hooks for future quests, rumors, or
blackmail.

## Tiers

| Karma | Tier |
| ---: | --- |
| +100 | Exalted |
| +75 | Luminary |
| +50 | Paragon |
| +35 | Beacon |
| +20 | Advocate |
| +10 | Trusted |
| 0 | Unmarked |
| -10 | Shifty |
| -20 | Outlaw |
| -35 | Renegade |
| -50 | Dread |
| -75 | Wraith |
| -100 | Abyssal |

## Action Tags

Karma shifts should be computed from structured action tags plus context.

Example tags:

- helpful
- harmful
- funny
- humiliating
- violent
- deceptive
- generous
- selfish
- romantic
- betrayal
- protective
- chaotic
- lawful
- forbidden


---

## Theme art curation standard

_(Consolidated 2026-05-03 from `docs/theme-art-curation-standard.md`. Original file deleted; this section is the canonical copy.)_

# Theme Art Curation Standard

Karma should be able to support many visual themes — sci-fi frontier, western,
farm, WW2, medieval, cyberpunk, etc. — without rewriting gameplay code or
hand-mapping every image differently. This document defines how we curate art so
new themes can plug into the same catalogs, validators, and runtime conventions.

## Goals

- Keep theme art swappable by gameplay role, not by one-off file names.
- Separate raw/reference art from runtime-ready atlases.
- Make generated art auditable before it enters a code catalog.
- Preserve a consistent scale, camera angle, and interaction footprint across themes.

## Folder Model

Use `assets/art/` as the art library root.

```text
assets/art/
  reference/              # raw prompts, mood boards, labeled/generated sheets
  sprites/                # runtime character, NPC, item, weapon, tool sheets
  tilesets/               # runtime terrain/floor/wall/door/zone atlases
  props/                  # runtime furniture, pickups, interactibles, oddities
  structures/             # runtime buildings, rooms, greenhouses, modules
  ui/                     # icons, portraits, HUD art later
```

Reference art can be messy. Runtime art should be clean, transparent/chroma-free
where appropriate, and mapped only when it matches a documented contract.

## Naming Convention

Use lowercase snake-case with this shape:

```text
<theme>_<domain>_<subject>[_variant][_size].png
```

Examples:

```text
scifi_sprites_engineer_player_8dir.png
western_sprites_rancher_player_8dir.png
farm_tilesets_homestead_16px.png
ww2_props_field_radio_32px.png
western_structures_sheriff_office_atlas.png
```

Current prototype files predate the full convention, so do not rename them
casually. For new curated packs, use the convention.

## Theme IDs

Theme IDs should be short, stable, and lowercase:

- `scifi`
- `western`
- `farm`
- `ww2`
- `medieval`
- `cyberpunk`

A theme pack should fill the same gameplay roles whenever possible. Example:

| Gameplay role | Sci-fi | Western | Farm | WW2 |
| --- | --- | --- | --- | --- |
| clinic / safe hub | frontier clinic | town doctor | farmhouse clinic | field hospital |
| repair tool | multi-tool | wrench | farm tool kit | engineer kit |
| ranged weapon | electro pistol | revolver | varmint rifle | service rifle |
| oddity | alien relic | cursed idol | scarecrow charm | encrypted orders |
| currency | scrip | dollars | farm credits | ration stamps |

## Runtime Contracts

### Character Sheets

Use the existing character contract:

- `256 x 288` PNG.
- `8 columns x 9 rows`.
- `32 x 32` frames.
- Transparent background.
- Feet bottom-centered.
- Direction columns and action rows from `docs/character-art-generation-workflow.md`.

Validate with:

```bash
python3 tools/prepare_character_sheet.py validate assets/art/sprites/<sheet>.png
```

### Item / Prop / Weapon Atlases

Preferred constraints:

- transparent PNG,
- clear silhouettes,
- consistent top-down or three-quarter top-down angle,
- each object fits an intentional footprint: `16x16`, `24x24`, `32x32`, or `64x64`,
- no labels or grid lines in runtime atlases,
- no baked drop shadows unless the whole theme uses them consistently.

Generated item sheets are allowed, but they should be converted into explicit
catalog rectangles before game code depends on them.

### Tilesets

Preferred constraints:

- tile size should be documented in the filename or catalog mapping,
- no perspective that breaks grid readability,
- floor/wall/door variants should be visually distinct at game scale,
- use repeatable patterns for floor/terrain tiles.

### Structures

Preferred constraints:

- use transparent PNGs,
- keep a predictable footprint and anchor point,
- split huge presentation renders into reusable runtime pieces when possible:
  base, door, wall/cap, damaged overlay, powered/off variants, etc.

## Theme Pack Manifest

For each curated theme, keep a small manifest alongside the art or in docs. Use
this shape:

```yaml
theme: western
status: reference | runtime-ready | cataloged
source: chatgpt | hand-drawn | purchased | mixed
scale: 32px characters, 16px tiles
camera: top-down / 3-quarter top-down
palette_notes: dusty frontier, warm browns, muted reds
runtime_assets:
  characters:
    player: assets/art/sprites/western_sprites_rancher_player_8dir.png
  tilesets:
    town: assets/art/tilesets/western_tilesets_town_16px.png
  props:
    general: assets/art/props/western_props_town_32px.png
missing_roles:
  - clinic equivalent
  - faction banners
  - currency icon
notes:
  - Generated sheet had labels removed manually.
  - Needs catalog rectangles before runtime use.
```

## Prompt Template: New Theme Pack

```text
Create a cohesive 2D pixel-art runtime art pack for the game Karma.

Theme: <western / farm / WW2 / etc.>
Camera/style: top-down or slight three-quarter top-down, readable at small scale.
Visual constraints: crisp pixel art, consistent palette, no text, no labels, no guide boxes, no watermark.
Runtime constraints: transparent PNG preferred; if transparency is impossible, use flat #00ff00 chroma key.

Create assets that correspond to these gameplay roles:
- player character concept
- friendly NPC concept
- hostile/rough NPC concept
- safe hub/clinic structure
- common floor/terrain tile
- wall/fence/barrier tile
- door/gate tile
- small pickup/currency item
- repair/healing/support tool
- melee weapon
- ranged weapon
- oddity/quest object
- furniture/cover prop

Keep all objects at a consistent scale so they can be split into runtime atlases later.
Do not make a poster, mockup, UI screen, or labeled presentation sheet.
```

## Prompt Template: Runtime Atlas

```text
Create a clean runtime PNG atlas for Karma.

Theme: <theme id>.
Domain: <props / weapons / tools / tiles / structures>.
Subjects: <list of exact objects>.
Style: cohesive pixel art, top-down or slight three-quarter top-down.
Background: transparent PNG, or flat #00ff00 chroma key if transparency is impossible.
Constraints: no labels, no text, no guide boxes, no watermark, no decorative border.
Spacing: leave at least 2 px of transparent padding around each object.
Scale: small objects fit 16x16 or 32x32; structures can use larger cells but must remain reusable.
```

## Curation Checklist

Before cataloging an image in code:

- [ ] It has a clear theme ID.
- [ ] It lives in the correct domain folder.
- [ ] Filename follows the new convention, unless preserving a legacy file.
- [ ] Runtime image has no labels/guides/watermarks.
- [ ] Background is transparent or normalized from chroma key.
- [ ] Scale matches the existing prototype.
- [ ] Source/reference image is preserved separately when useful.
- [ ] Catalog rectangles are mapped intentionally.
- [ ] Smoke tests/build pass after wiring it into code.

## Audit Tool

Run this to get a quick library report:

```bash
python3 tools/audit_art_library.py
```

The audit does not replace visual review, but it catches naming, folder, PNG
size, alpha, and chroma-key issues early.


---

## Character animation pipeline

_(Consolidated 2026-05-03 from `docs/character-animation-pipeline.md`. Original file deleted; this section is the canonical copy.)_

# Character Animation Pipeline

Karma should support two practical character-art paths:

1. **Short-term runtime sheets**: clean transparent PNG frame grids that Godot can slice directly.
2. **Long-term base + skin pipeline**: a consistent blank/body rig with outfit, hair, gear, and accessory layers composited into the same runtime grid.

## Current Asset Status

- `assets/art/sprites/scifi_engineer_player_8dir.png` is the preferred active runtime player sheet when present.
  - It is a clean `256 x 288` bridge sheet exported by `tools/export_engineer_8dir.py`.
  - Its true source art is still four-direction, so diagonal/action columns are temporary approximations.
  - It will not look like a true authored eight-direction template until new diagonal frames are generated or drawn.
- `assets/art/sprites/scifi_engineer_player_sheet.png` remains the source/fallback player sheet.
  - It is mapped as a four-direction sheet: front, back, left, right.
  - The runtime code understands eight-direction animation names and falls back to cardinal directions when a sheet has no diagonal frames.
- `assets/art/ChatGPT Image Apr 26, 2026, 08_19_08 AM.png` is a useful eight-direction reference/template.
  - It is not runtime-ready because it contains labels, guide boxes, metadata, and presentation text.
  - Use it as a layout reference, not as an in-game atlas.
- `assets/art/character.png` is a concept/source atlas with many character looks and poses.
  - Use it as design reference or source material for skins.
  - Do not wire it as an eight-direction player runtime sheet unless a clean grid has been exported.

## Runtime Sheet Standard

Preferred eight-direction runtime sheet:

- Frame size: `32 x 32 px`.
- Sheet size: `256 x 288 px`.
- Columns: 8 directions.
- Rows: 9 animation/action rows.
- Background: transparent PNG.
- Pivot: bottom center / feet aligned.
- No labels, text, guide boxes, metadata, shadows, or presentation panels.

Direction columns:

```text
0 front
1 front-right
2 right
3 back-right
4 back
5 back-left
6 left
7 front-left
```

Rows:

```text
0 idle, one frame per direction
1-4 walk cycle frames 1-4
5 run pose/cycle placeholder
6 shoot pose/cycle placeholder
7 melee pose/cycle placeholder
8 interact pose/cycle placeholder
```

The code maps this with `CharacterSheetLayout.EightDirectionTemplate(origin)`.

## Recommended Next Step

Do **not** keep hand-mapping large generated presentation images. Instead:

1. Generate or draw a clean **blank/base body** eight-direction sheet using the runtime standard.
2. Generate matching **skin layers** using the exact same grid:
   - outfit/suit
   - hair/helmet
   - backpack/toolbelt
   - held weapon/tool overlays later
3. Composite layers offline into final runtime PNGs, e.g.:
   - `scifi_engineer_player_8dir.png`
   - `scifi_medic_player_8dir.png`
   - `scifi_raider_player_8dir.png`
4. Drop the first true engineer export at `assets/art/sprites/scifi_engineer_player_8dir.png`.
   The player catalog already prefers that file when it exists and falls back to
   `scifi_engineer_player_sheet.png` while it is missing. The current file at
   that path is a bridge export and can be replaced by better art without code
   changes if it preserves the runtime layout.
5. Keep source/reference images in `assets/art/` or a future `assets/art/reference/` folder.
6. Put only clean runtime atlases under `assets/art/sprites/` once they are ready to catalog.

This gives us reusable animation consistency without regenerating every NPC/player from scratch.

For the hands-on prompt + Python validation workflow, see
`docs/character-art-generation-workflow.md`.

## Generation Prompt Skeleton

```text
Create a clean 2D pixel-art top-down RPG character runtime sprite sheet for Karma.
Subject: blank neutral humanoid base body, sci-fi frontier colony proportions.
Frame size: every frame is exactly 32x32 px, nearest-neighbor pixel art.
Canvas: 8 columns x 9 rows, total 256x288 px.
Directions by column: front, front-right, right, back-right, back, back-left, left, front-left.
Rows: row 0 idle; rows 1-4 are a four-frame walk cycle; row 5 run; row 6 shoot; row 7 melee; row 8 interact.
Background: transparent PNG, or perfectly flat #00ff00 chroma key if transparency is impossible.
Alignment: feet bottom-centered in every frame, consistent body proportions, no camera angle changes.
Constraints: runtime grid only, no labels, no text, no guide boxes, no metadata, no watermark, no shadows baked into background.
```

For skins, replace the subject with the layer while preserving the exact grid/alignment:

```text
Subject: engineer jumpsuit/clothing layer only for the same blank humanoid base, transparent where body is visible.
```


---

## Character art generation workflow

_(Consolidated 2026-05-03 from `docs/character-art-generation-workflow.md`. Original file deleted; this section is the canonical copy.)_

# Character Art Generation Workflow

This is the practical side-pipeline for generating Karma character art manually
with ChatGPT/image tools, Python, Aseprite, Krita, or similar.

For the better long-term paper-doll/layered character standard, see
[`professional-character-art-systems.md`](professional-character-art-systems.md).
The current `32x32` sheet is a prototype fallback, not the final quality target.

## Target Runtime Contract

The game code expects a runtime-ready PNG with this exact shape:

- File type: PNG with RGBA transparency.
- Size: `256 x 288 px`.
- Grid: `8 columns x 9 rows`.
- Frame size: `32 x 32 px`.
- No labels, text, grid lines, guide boxes, metadata panels, watermarks, or decorative borders.
- Character feet should be bottom-centered in every frame.
- Transparent background. If a generator cannot do transparency, use flat bright green chroma key and remove it before committing.

Direction columns:

```text
0 front
1 front-right
2 right
3 back-right
4 back
5 back-left
6 left
7 front-left
```

Rows:

```text
0 idle
1 walk frame 1
2 walk frame 2
3 walk frame 3
4 walk frame 4
5 run/action-ready
6 tool/use
7 melee/impact
8 interact/reach
```

## Recommended Manual Pipeline

1. Generate a **reference sheet** with ChatGPT or another image model.
2. If the output contains labels/guides/text, treat it as reference only.
3. Create/export a clean runtime PNG at exactly `256 x 288`.
4. Run the validator:

```bash
python3 tools/prepare_character_sheet.py validate path/to/sheet.png
```

5. If the image has green chroma background, normalize it:

```bash
python3 tools/prepare_character_sheet.py normalize path/to/input.png assets/art/sprites/scifi_engineer_player_8dir.png --chroma
```

6. Validate the final asset again:

```bash
python3 tools/prepare_character_sheet.py validate assets/art/sprites/scifi_engineer_player_8dir.png
```

7. Run the game verification:

```bash
powershell -ExecutionPolicy Bypass -File .\tools\test.ps1; if ($LASTEXITCODE -eq 0) { powershell -ExecutionPolicy Bypass -File .\tools\snapshot.ps1 }; if ($LASTEXITCODE -eq 0) { powershell -ExecutionPolicy Bypass -File .\tools\check.ps1 } else { exit $LASTEXITCODE }
```

From WSL without `powershell.exe`, use the Windows executables directly as done
in existing agent sessions.

## Prompt: Full Runtime Sheet

Use this when asking ChatGPT/image tools for a complete character sheet.

```text
Create a clean 2D pixel-art top-down RPG character runtime sprite sheet for the game Karma.

Subject: <describe character/outfit/personality>. Sci-fi frontier colony style. Compact readable pixel art.

STRICT RUNTIME FORMAT:
- Output must be a clean sprite grid only.
- Canvas must be exactly 256x288 px if possible.
- 8 columns x 9 rows.
- Each frame is exactly 32x32 px.
- Direction columns left-to-right: front, front-right, right, back-right, back, back-left, left, front-left.
- Row 0: idle pose, one per direction.
- Rows 1-4: four-frame walking cycle for every direction.
- Row 5: run/action-ready pose for every direction.
- Row 6: tool/use pose for every direction.
- Row 7: melee/impact pose for every direction.
- Row 8: interact/reach pose for every direction.

TRUE 8-DIRECTION REQUIREMENT:
- Do not duplicate side-facing sprites for diagonal directions.
- Front-right and front-left must show a real three-quarter/front diagonal body silhouette.
- Back-right and back-left must show a real three-quarter/back diagonal body silhouette.
- Diagonal poses should have distinct head, torso, shoulder, arm, leg, and foot angles.
- The character should visibly rotate through all eight directions like a true 8-way RPG sprite.
- Mirroring left/right is acceptable, but front/back/diagonal views must not be the same pose.

VISUAL RULES:
- Transparent background preferred.
- If transparency is impossible, use perfectly flat #00ff00 chroma-key background.
- No shadows baked into the background.
- No labels, no text, no metadata, no guide boxes, no decorative border, no watermark.
- Feet bottom-centered in every 32x32 frame.
- Character proportions consistent across all frames.
- Nearest-neighbor crisp pixel art, no painterly blur, no antialiased soft edges.
- Keep outline pixels intact; do not use green inside the character.
```

## Prompt: True 8-Direction Template Like the Reference Image

Use this if the model keeps making cardinal-only or copied diagonal frames.

```text
Create a true 8-direction pixel-art character animation sheet similar to a professional RPG sprite template, but output ONLY the clean runtime grid.

Character: <engineer / rancher / medic / soldier / farmer>, readable 32x32 pixel-art proportions.

Output contract:
- PNG sprite sheet only.
- 256x288 px canvas.
- 8 columns x 9 rows.
- 32x32 px per frame.
- Transparent background, or flat #00ff00 chroma if transparency is impossible.
- No labels, no text, no grid lines, no side panels, no metadata, no preview boxes, no watermark.

Columns, left to right:
1 front
2 front-right three-quarter view
3 right side view
4 back-right three-quarter view
5 back view
6 back-left three-quarter view
7 left side view
8 front-left three-quarter view

Rows:
1 idle
2 walk frame 1
3 walk frame 2
4 walk frame 3
5 walk frame 4
6 run/action-ready
7 tool/use or shoot-ready
8 melee/impact
9 interact/reach

Important:
- Every diagonal column must be a real diagonal pose, not a copy of right/left/front/back.
- Front-right/front-left should show the face and chest partly turned.
- Back-right/back-left should show the back and shoulder partly turned.
- Side views should be profile only.
- Back view should clearly show the rear of the head/body.
- Keep feet aligned to bottom center in every 32x32 frame.
- Keep silhouette size consistent so the sprite does not pop or jitter while walking.
```

## Prompt: Blank Base Body

Use this first if building a reusable base + skin system.

```text
Create a clean 2D pixel-art top-down RPG blank humanoid base-body runtime sprite sheet for Karma.

Subject: neutral blank humanoid body, no clothing except minimal neutral underlayer, sci-fi frontier RPG proportions.

Format: exact 256x288 px runtime grid, 8 columns x 9 rows, 32x32 px frames.
Columns: front, front-right, right, back-right, back, back-left, left, front-left.
Rows: idle, walk1, walk2, walk3, walk4, run/action-ready, tool/use, melee/impact, interact/reach.

Transparent background, feet bottom-centered, consistent proportions, no labels, no guides, no text, no borders, no watermark.
```

## Prompt: Skin/Outfit Layer

Use this after the blank body exists. The output should align to the exact same
runtime grid.

```text
Create a transparent clothing/skin layer for the Karma 8-direction character runtime sheet.

Layer subject: <engineer suit / medic coat / raider armor / helmet / hair / backpack>.
This layer must align perfectly over the existing blank humanoid base-body sheet.

Exact format: 256x288 px, 8 columns x 9 rows, 32x32 px frames.
Keep all body-visible pixels transparent unless covered by the clothing/gear layer.
No labels, no guide boxes, no text, no border, no watermark.
```

## Validator Notes

`tools/prepare_character_sheet.py` checks:

- exact dimensions (`256 x 288`),
- transparent pixel presence,
- chroma-green leakage,
- empty frames,
- frames touching cell edges, which usually means cropping/alignment problems.

Warnings are not always fatal, but errors should be fixed before wiring the art
into the prototype.

## Current Bridge Asset

`tools/export_engineer_8dir.py` creates the current bridge player sheet from the
existing four-direction engineer art. It is useful for testing code and layout,
but it is not final art: diagonal/action frames are approximations. Replace
`assets/art/sprites/scifi_engineer_player_8dir.png` with a true generated or
hand-authored runtime sheet when ready.


---

## PixelLab MCP workflow

_(Consolidated 2026-05-03 from `docs/pixellab-mcp-workflow.md`. Original file deleted; this section is the canonical copy.)_

# PixelLab MCP Workflow

PixelLab MCP can be used as an external candidate generator for Karma pixel art, while Karma keeps the runtime contract and final curation local.

## Safety / token handling

- Do **not** commit PixelLab API tokens.
- Do **not** paste tokens into Discord or docs.
- The repo tooling here is offline-only: it imports downloaded PixelLab PNG/ZIP files and never calls the PixelLab API.
- Configure PixelLab MCP in the local AI client that supports MCP, then download generated assets into a local scratch folder.

PixelLab's MCP docs describe tools such as `create_character`, `animate_character`, `get_character`, top-down tilesets, and map objects. The MCP calls are made from an MCP-capable assistant/client; Karma's repo tools only handle downloaded files after PixelLab generates them.

For Karma player art, the expected external flow is:

1. Ask the MCP client to call `create_character(description="...", n_directions=8)` for the base 8-direction character.
2. Ask it to call `animate_character(character_id="...", animation="walk")` on the returned character id.
3. Use `get_character` or the PixelLab result link to download the resulting PNG/ZIP.
4. Normalize it into Karma's player-v2 format with `tools/import_pixellab_character.py`.
5. Review the normalized sheet before copying it into active runtime art.

## Karma target contract

The current player-v2 original-art target is:

- `32x64` cells.
- `8` columns.
- `4` rows.
- Sheet size: `256x256`.
- Direction order:
  1. front/down
  2. front-right
  3. right
  4. back-right
  5. back/up
  6. back-left
  7. left
  8. front-left
- Row contract:
  1. idle/facing
  2. walk-1
  3. walk-2
  4. walk-3

The current runtime renderer still previews this in square `64x64` cells, so the importer also creates a centered runtime sheet:

- `512x256`.
- `8` columns x `4` rows.
- `64x64` runtime cells with the `32x64` body centered horizontally.

## Import command

From the repo root:

```powershell
python tools/import_pixellab_character.py path\to\pixellab-download.png --output-dir assets\art\sprites\player_v2\imported --output-stem pixellab_engineer_v1
```

For a ZIP download:

```powershell
python tools/import_pixellab_character.py path\to\pixellab-download.zip --output-dir assets\art\sprites\player_v2\imported --output-stem pixellab_engineer_v1
```

If the source has a chroma-key background:

```powershell
python tools/import_pixellab_character.py path\to\sheet.png --chroma --output-dir assets\art\sprites\player_v2\imported --output-stem pixellab_engineer_v1
```

Outputs:

- `pixellab_engineer_v1_32x64_8dir_4row.png`
- `pixellab_engineer_v1_32x64_8dir_runtime.png`

## Prompt seed for PixelLab

Use wording like this in the MCP-capable client:

```text
Use PixelLab create_character with n_directions=8. Description:
Original top-down low-angle pixel art survivor engineer for a cozy sci-fi life-sim RPG. Compact readable 32x64-ish humanoid proportions, no weapons, no held tools, no text, no labels, transparent background, clean black/dark outline, orange work jacket, small backpack, simple boots, readable head/torso rotation. Generate 8 directions: south, south-east, east, north-east, north, north-west, west, south-west. Keep identity, outfit, scale, silhouette, and foot baseline consistent across directions.
```

If the client exposes raw tool arguments, the call should be shaped like:

```text
create_character(
  description="Original top-down low-angle pixel art survivor engineer for a cozy sci-fi life-sim RPG. Compact readable 32x64-ish humanoid proportions, no weapons, no held tools, no text, no labels, transparent background, clean black/dark outline, orange work jacket, small backpack, simple boots, readable head/torso rotation. Generate 8 directions: south, south-east, east, north-east, north, north-west, west, south-west. Keep identity, outfit, scale, silhouette, and foot baseline consistent across directions.",
  n_directions=8
)
```

Shorter fallback wording for clients that prefer natural language:

```text
Original top-down low-angle pixel art survivor engineer for a cozy sci-fi life-sim RPG. Compact readable 32x64-ish humanoid proportions, no weapons, no text, no labels, transparent background, clean black/dark outline, orange work jacket, small backpack, simple boots, readable head/torso rotation. Generate 8 directions: south, south-east, east, north-east, north, north-west, west, south-west. Keep identity, outfit, scale, and silhouette consistent across directions.
```

Then animate with a simple walk/idle request:

```text
Use PixelLab animate_character on the generated character id with animation="walk". Simple readable walking loop, arms swing opposite legs, no tools or weapons, same character and outfit, consistent scale and foot baseline.
```

If raw tool arguments are available:

```text
animate_character(character_id="<returned character id>", animation="walk")
```

## Curation notes

- Treat PixelLab output as a candidate, not source of truth.
- Prefer the local `player_model_32x64_8dir_4row.png` skeleton when checking direction order and baseline.
- Reject outputs with mixed identities, wrong direction order, weapons/tools in normal walk, cropped feet/head, or inconsistent scale.
- If a candidate is good, copy the normalized `*_runtime.png` into the active player-v2 path only after visual review and smoke tests.


---

## Testing launch paths

_(Consolidated 2026-05-03 from `docs/testing-launch-paths.md`. Original file deleted; this section is the canonical copy.)_

# Testing Launch Paths

Karma has two practical launch paths during prototype development.

## Main menu path

Use this when testing the real player-facing boot flow:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-main-menu.ps1
```

Equivalent direct Godot command:

```powershell
& 'C:\Users\pharr\Downloads\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64.exe' --path 'C:\Users\pharr\code\karma' --scene 'res://scenes/MainMenu.tscn'
```

The project default still boots here via `project.godot`:

```text
application/run/main_scene = res://scenes/MainMenu.tscn
```

## Direct gameplay path

Use this for fast gameplay iteration when the menu is in the way:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-gameplay.ps1
```

Equivalent direct Godot command:

```powershell
& 'C:\Users\pharr\Downloads\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64.exe' --path 'C:\Users\pharr\code\karma' --scene 'res://scenes/Main.tscn'
```

`tools/run-game.ps1` remains as a compatibility alias for direct gameplay.

## Automated checks

Use the headless smoke test path before committing gameplay/UI changes:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test.ps1
```

From WSL, the known-good commands are:

```bash
'/mnt/c/Program Files/dotnet/dotnet.exe' build Karma.csproj
'/mnt/c/Users/pharr/Downloads/Godot_v4.6.2-stable_mono_win64/Godot_v4.6.2-stable_mono_win64/Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path "C:\Users\pharr\code\karma" "res://scenes/TestHarness.tscn"
```

## Windows exports

The player-facing build exports as its own executable:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\export-main-game.ps1
```

Output: `build\windows\main\Karma.exe`

The direct gameplay prototype remains separate:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\export-prototype-game.ps1
```

Output: `build\windows\prototype\KarmaPrototype.exe`

Both scripts create the local, ignored `export_presets.cfg` from
`tools\export_presets.template.cfg` if needed.
The raw LPC source tree is kept out of Godot exports; runtime character bundles
come from `assets\art\generated\lpc_npcs`.


---

## Playtest checklist

_(Consolidated 2026-05-03 from `docs/playtest-checklist.md`. Original file deleted; this section is the canonical copy.)_

# Playtest Checklist

1. Launch the project and start the main playable prototype from the main menu.
2. Confirm the match begins fresh: the local player has 25 scrip, 0 karma, no completed quest progress, and the HUD is responsive.
3. Move around the starting area for at least 30 seconds and confirm movement, camera, minimap, stamina, hunger, and status strip updates feel stable.
4. Attack an NPC or valid combat target once, then confirm health, ammo or stamina, combat log, latest event text, and event icon update.
5. Talk to a nearby NPC and confirm the dialogue panel opens, choices render, and selecting a choice writes a local chat or dialogue event.
6. Open a vendor, confirm every vendor row has an item icon, buy one affordable item, and verify scrip decreases while the inventory row appears with an icon.
7. Open inventory, drag an item into a hotbar slot, and confirm the inventory row and hotbar slot both show item icons.
8. Pick up a world drop and verify it appears in inventory, respects capacity, and can be bound to the hotbar.
9. Play until the match summary appears or advance a test build to match end, then press the match summary's Return to Main Menu button.
10. Start a second match from the main menu and confirm the player starts fresh again with 25 scrip, 0 karma, no completed quest progress, and no leftover temporary status effects.
11. Spend five minutes in the default small world and watch density: the prototype should show 5 social stations and about 10 NPC profiles, enough to feel occupied without crowding the 80x72 map.
12. Keep `tools/test.ps1` green after any gameplay-loop change before doing a manual pass.


---

## Interior design contract

_(Consolidated 2026-05-03 from `docs/interior-design-contract.md`. Original file deleted; this section is the canonical copy.)_

# Interior Design Contract

This contract defines how Karma represents real interiors behind the current server-owned `enter` / `exit` placeholder. The goal is to let buildings, generated stations, NPCs, shops, and local chat agree on the same lightweight interior identity before we build full interior maps.

## Runtime identity

Every enterable structure or generated station should resolve to an interior descriptor:

- `interiorId` — stable id scoped to the world seed/location, such as `interior_clinic_0`.
- `interiorKind` — reusable gameplay/layout category, such as `clinic`, `market`, `workshop`, `shrine`, `saloon`, `farmstead`, or `common-room`.
- `ownerStructureId` — world structure/station marker that owns the doorway.
- `displayName` — player-facing name, usually the structure or station name.
- `entryTile` — exterior tile where `enter` is allowed.
- `exitTile` — exterior tile where `exit` returns the player if no custom doorway exists.
- `doorwayTag` — optional art/layout hook such as `front-door`, `hatch`, `stall`, or `threshold`.

The server remains authoritative. Clients may show prompts, doors, and room previews, but `enter` / `exit` state comes from server intents and interest snapshots.

## Doorway and exit behavior

Initial rule:

1. Player must be in range of the owning structure/station marker.
2. `enter` records the player as inside that owner and emits an entry event.
3. `exit` clears the inside state and returns the player to the exterior marker/exit tile.
4. A player may only be inside one interior at a time.

Later real interiors can replace the placeholder without changing the intent names.

## Snapshot visibility

Until interior maps exist, interest snapshots expose inside state as player status text (`Inside: <name>`). When real interiors land, snapshots should filter by interior identity:

- Players inside the same `interiorId` can see each other.
- Exterior players do not see interior-only NPCs/items unless a doorway/window rule says otherwise.
- Interior NPCs, shops, storage, quest boards, and props should be keyed by `interiorId`.
- Global match, karma, and faction state remains visible unless deliberately hidden.

## Local chat and audibility

Default local chat rule:

- Same interior: normal local chat falloff.
- Exterior to interior: muted or muffled unless the doorway is open/audible.
- Different interiors: inaudible by default.
- Special interiors may override this: saloons are loud, clinics are private, witness courts may broadcast, broadcast towers may amplify.

Do not make privacy client-only. Any future chat/audibility filtering should use the server-known interior state.

## NPC, shop, and quest hooks

Generated station interiors are the first content hook:

- `clinic` — triage NPCs, injury recovery, medical supplies, rescue drop-offs.
- `market` — buying/selling, gifting, debt, fence/stolen-goods checks.
- `workshop` — repairs, crafting, public infrastructure jobs, sabotage evidence.
- `shrine` — confession, apology, reparations, memory/karma rituals.
- `saloon` — rumors, relationships, posse formation, local chat hub.
- `farmstead` — food, growing, harvest theft, hunger rescue loops.
- `duel-ring` — consent-based combat staging and witnesses.
- `broadcast-room` — announcements, reputation, rumor amplification.
- `common-room` — fallback for stations that do not yet have a bespoke room.

Generated content should declare the hook even before the interior is rendered. That lets prompts, tests, and future systems agree on the target room type.

## Implementation checkpoints

1. Generated station locations include `InteriorId` and `InteriorKind`.
2. Station marker prompts expose the future interior kind for debugging/discovery.
3. `enter` / `exit` events keep using structure ownership as the current placeholder.
4. Future interior maps should consume these ids rather than inventing parallel ids.
5. Tests should prove generated stations carry interior hooks and that prompts surface them.


---

## Downed / carry / rescue mechanics

_(Consolidated 2026-05-03 from `docs/downed-carry-rescue-mechanics.md`. Original file deleted; this section is the canonical copy.)_

# Downed, Carry, Rescue, and Mercy Mechanics

This is a proposed core Karma loop for what happens before full Karma Break/death.
It should create social choices other players can build on: help, rescue, exploit,
or finish someone off.

## Design goal

A downed player should become a temporary social object in the world, not just a
respawn timer. Other players can make visible karma choices around them.

## State model

Suggested player life states:

1. `Healthy`
   - Normal movement/action.
2. `Injured`
   - Low health warning, still mobile.
3. `Downed`
   - Cannot walk normally or attack.
   - Can crawl slowly or call for help later.
   - Has a bleed-out / collapse timer.
   - Keeps inventory but drops an obvious rescue/loot prompt.
4. `Carried`
   - A rescuer/abductor is carrying or dragging the downed player.
   - Carrier moves slower, cannot sprint, and is vulnerable.
   - Carried player can be delivered to a safe station/hospital or abandoned.
5. `Recovered`
   - Revived at low health with short grace/status effect.
6. `KarmaBreak`
   - Existing death/reset flow.

## Player actions around a downed character

### Help up / revive in place

- Requires time, proximity, and optionally a med item or safe environment.
- Interruptible by damage/movement.
- Ascends the helper.
- Restores the downed player to low health.
- Builds relationship/faction reputation if witnessed.

### Carry / drag

- Starts a `Carried` link between carrier and downed player.
- Carrier receives movement penalties.
- The carried player follows carrier position or occupies a linked offset.
- Can be heroic or predatory depending on destination/action.

Possible delivery outcomes:

- **Hospital/clinic/safe station delivery**
  - Strong Ascension reward.
  - Clinic faction reputation up.
  - Downed player recovers better than field revive.
- **Drop in danger / abandon**
  - Small Descension or local reputation loss if witnessed.
- **Deliver to hostile faction / black market / bounty station**
  - Descension or faction-specific reputation gain/loss depending on context.
  - Could support future kidnapping/bounty mechanics.

### Finish / execute

- Converts downed player to Karma Break.
- Strong Descension / karma loss for the killer.
- Potential Scourge standing/reputation effects.
- Creates a visible world event/rumor.
- Should have a clear confirmation/hold input so accidental finish is hard.

### Loot / steal from downed player

- Optional future mechanic.
- Descends unless special lawful/bounty context exists.
- Could interact with Karma Break drop ownership/provenance.

## Karma consequences

These should be server-owned actions, not client labels:

- Field revive: Ascend.
- Carry to clinic/hospital: stronger Ascend + clinic/faction rep.
- Carry to safe ally station: Ascend + local station rep.
- Abandon after starting carry: Descend if witnessed or if abandonment causes break.
- Execute downed player: strong Descend; public rumor/event.
- Steal from downed player: Descend and reputation damage.
- Mercy finish could be a special exception only if a future status explicitly marks
  it as requested/consented/terminal; default execution should be evil.

## Art and animation requirements

This mechanic affects the professional character art standard. The v2 animation
contract should include:

- `downed_idle` — lying/slumped in all 8 directions or a reduced 4-direction set.
- `downed_crawl` — optional slow movement while downed.
- `revive_kneel` / `help_up` — helper animation.
- `being_helped_up` — target recovery animation.
- `carry_start` — pickup/hoist or drag start.
- `carry_walk` — carrier movement while carrying/dragging.
- `carried_body` — carried player's overlay/pose.
- `drop_carried` — drop/lay down animation.
- `execute_downed` — attacker action, likely bespoke/limited.
- `hurt_to_downed` — transition from standing to downed.

Not every outfit must draw all of these immediately. The manifest should allow
fallbacks such as:

- `downed_crawl -> downed_idle`
- `carry_walk -> walk_slow`
- `revive_kneel -> interact`
- `execute_downed -> melee_slash`

But the **base body** should define these actions early so future outfits and
items can align to them.

## Server implementation sketch

Potential authoritative data:

- `PlayerState.LifeState`
- `PlayerState.DownedByPlayerId`
- `PlayerState.DownedAtTick`
- `PlayerState.BleedOutTick`
- `PlayerState.CarriedByPlayerId`
- `PlayerState.CarryingPlayerId`
- status effects: `downed`, `carrying`, `carried`, `recovery_grace`

Potential intents:

- `RevivePlayer`
- `StartCarryPlayer`
- `DropCarriedPlayer`
- `DeliverCarriedPlayer`
- `ExecuteDownedPlayer`
- `StealFromDownedPlayer`

Potential station hooks:

- Clinics/hospitals: best recovery and reputation reward.
- Social hubs: witnesses/rumors.
- Black markets: predatory delivery/bounty route.
- War memorial/court stations: public judgment consequences.

## Prototype order

1. Add `Downed` state before instant Karma Break for non-overkill lethal damage.
2. Add field revive and execute as two opposite server-owned choices.
3. Add carry/drop.
4. Add clinic delivery using generated clinic station markers.
5. Add HUD/world prompts and developer overlay state.
6. Add v2 art placeholders/fallbacks for downed/carry animations.


---

## Art audit

_(Consolidated 2026-05-03 from `ART_AUDIT.md`. Original file deleted; this section is the canonical copy.)_

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
| `karma_static_event_markers_atlas.jpg` | `tools/slice_event_markers.gd` | `assets/art/generated/sliced/event_markers/` | 36 small flat icons (target ring, location pin, warning triangle, shield, racing flag, ring buoy, skull, wanted poster, parachute mushroom, red cross, money bag, exclamation, crossed swords, magnifier, eye bubble, posse circle, handshake, X box, package, fire triangle, horse, shovel chest) |
| `karma_priority_clinic_rescue_revive_atlas.jpg` | `tools/slice_clinic_rescue_revive.gd` | `assets/art/generated/sliced/clinic_rescue_revive/` | 30 clinic/rescue props (clinic sign, terminals, bed, ambulance stretcher, medic bag, medicine cabinet, holo tripod, diagnostic kiosk, scrip tray, biohazard barricade, pillbox tray, oxygen tanks, surgical light, downed markers, shield buff, RESCUE banner, medical crate, medical terminals, refugee tent, ritual altar, bench) |
| `karma_priority_supply_shop_loot_atlas.jpg` | `tools/slice_supply_shop_loot.gd` | `assets/art/generated/sliced/supply_shop_loot/` | 21 supply/shop props (supply drop parachute, barrel, bedroll, scales table, shop kiosk/shelves/tent, locked + open chests, ammo crate, medical crate, weapon case, tool box, backpack, parcel, money sack, scrap pile, cluttered loot, signpost, discount tag) |
| `karma_priority_wanted_bounty_law_atlas.jpg` | `tools/slice_wanted_bounty_law.gd` | `assets/art/generated/sliced/wanted_bounty_law/` | 25 wanted/bounty props (bulletin board, scanner kiosk, ledger desk, guard booth, barricade gate, scanner archway, evidence bag/lockers/table, jail barred window, handcuffs, mail envelope, badges, mug shot frame, ballot box, paperwork stack, tripod camera, ATM kiosk) |
| `karma_priority_structure_world_state_atlas.jpg` | `tools/slice_structure_world_state.gd` | `assets/art/generated/sliced/structure_world_state/` | 30 structure damage states (generator pristine→damaged→wrecked→sabotaged, greenhouse pristine + shattered, electrical box closed/sparking, fences, gates, manhole covers, water tanks, surveillance cameras, notice boards, lumber pile, fire pit) |
| `karma_static_interior_furniture_atlas.jpg` | `tools/slice_interior_furniture.gd` | `assets/art/generated/sliced/interior_furniture/` | 28 interior props (beds, cot, computer desks, scribe desk, lockers, benches, sofas, chairs, stool, vending machine, corkboard, vending, register, stocked shelves, first aid kit) |
| `karma_static_crafting_stations_atlas.jpg` | `tools/slice_crafting_stations.gd` | `assets/art/generated/sliced/crafting_stations/` | 12 workbench variants (mechanical, alchemy, weapons display, pressure vessel, water filtration, fuel pump, scribe desk, paper desk, recycling, electronics, hydroponics, computer) |
| `karma_static_containers_loot_atlas.jpg` | `tools/slice_containers_loot.gd` | `assets/art/generated/sliced/containers_loot/` | 25 container variants (wood/scifi/ornate chests, dumpster, medkit box, gun case, tool box, lockbox, duffel/backpack, money sack, cash pile, bedroll, energy cells crate, military trunk, shipping container, fragile box, treasure chest, safe, scrap pile, barrel) |
| `karma_static_modular_walls_doors_atlas.jpg` | `tools/slice_modular_walls_doors.gd` | `assets/art/generated/sliced/modular_walls_doors/` | 27 wall/door tiles (sci-fi/wood/scrap/stone walls, clinic/shop/jail/curtain doors, airlock open/closed, gates, windows, fence, railing, barricade, checkpoint, archway, sign hanger, awning, roof edge) |
| `karma_priority_player_interactions_atlas.jpg` | `tools/slice_player_interactions.gd` | `assets/art/generated/sliced/player_interactions/` | 36 interaction props (duel post sign, parade flags, gambling table, handshake statue, gift box, accept/decline/prohibited handshake medals, posse banners, contracts pinboard, treasure chests open/closed, treasure maps, gold medallion, danger signs, ballot box, sealed letters, magic ritual circle, torches, hunter sign, board game, casino chips, mailbox) |
| `karma_priority_location_exteriors_atlas.jpg` | `tools/slice_location_exteriors.gd` | `assets/art/generated/sliced/location_exteriors/` | 12 building exteriors (clinic, shop kiosk, bounty office, jail block, checkpoint guard tower, safehouse tent, repair garage, greenhouse dome, posse camp/rally, faction request board shelter, safehouse cave entrance, supply drop landing pad) |
| `karma_priority_theme_variant_matrix_atlas.jpg` | `tools/slice_theme_variant_matrix.gd` | `assets/art/generated/sliced/theme_variant_matrix/` | 12 theme alternates (covered wagon=western supply, drop pod=sci-fi supply, fuel barrels=post-apoc supply, ornate chest=fantasy loot, plank=lawless, sci-fi archway, tire barricade=post-apoc, fantasy guild gate, red cross sign=western clinic, ATM=sci-fi clinic, red cross tent=post-apoc clinic, fantasy shrine) |
| `karma_static_hazards_disasters_atlas.jpg` | `tools/slice_hazards_disasters.gd` | `assets/art/generated/sliced/hazards_disasters/` | 30 hazard decals (warning barricade, fire, electric portal, sparking cable, leaking pipe, steam manhole, oil/toxic pools, radioactive sign, biohazard cone, broken glass, rock pile, broken metal beams, blue puddle, red flag, purple meteor, broken viewscreen, broken planks, alert siren, repair signs, biohazard crate, medkit, dark hole, red barrel) |
| `karma_static_evidence_clues_atlas.jpg` | `tools/slice_evidence_clues.gd` | `assets/art/generated/sliced/evidence_clues/` | 25 evidence/clue items (footprints, signed letter, broken padlock, cracked tablet, torn cloth, bullet casings, prybar/pliers, evidence sack, bagged jewelry, clipboard, blood splatter, tire skids, engraved stone, official permit, forged permit, anonymous letter, sealed certificate, ID badge, broken radio, rumor newspaper, sparking wire, antique key) |
| `karma_static_player_interaction_props_atlas.jpg` | `tools/slice_player_interaction_props.gd` | `assets/art/generated/sliced/player_interaction_props/` | 20 interaction props (barter terminal, neon handshake, gift box, alert sign, rifles+star, satellite dish, locked chest, signed contract, faction flag, wax seal, voting tablet, money envelope+seal, magnet+coin, gear+chest, blue arrow, parchments, evil eye computer, badge slot board, casino chips dice) |
| `karma_static_faction_reputation_symbols_atlas.jpg` | `tools/slice_faction_reputation_symbols.gd` | `assets/art/generated/sliced/faction_reputation_symbols/` | 34 faction/reputation badge icons (winged halo, demon, knight shield, money wagon, hammer/pick skull, hooded assassin, blue flags, eye, pirate skull, target skull, purple horns, x-box, ribbon medals, blade swords, money bag lock, green/red shields, dove peace, gold coins, danger triangles) |
| `karma_static_mission_boards_atlas.jpg` | `tools/slice_mission_boards.gd` | `assets/art/generated/sliced/mission_boards/` | 20 mission board variants (adventure quest, computer data, danger skull, ornate faction, bandage health, wanted poster, supply check, repair tool, lost-and-found, rumor, posse recruitment, faction notice, black market coded, law bulletin, community vote, delivery, salvage claim, duel challenge, apology, warning) |

The first slice is **wired into the live HUD** —
`HudController.ResolveEventIconName(eventId)` maps server-event ids to
icon names, `_eventIcon` displays the matched icon next to the event
label, and 9 smoke tests cover the resolver logic.

### Atlases NOT yet sliced

**All surveyed atlases have now been sliced.** The remaining ~14 atlases
in `assets/art/generated/static_event_atlases/` (event_props_universal,
fantasy/scifi/post-apoc/western variants, terrain_ground_details, etc.)
are theme-specific re-skins or world-state filler with low immediate
runtime priority — the slicer pattern is now well-established and
follows the same `slice_<atlas>.gd` template if/when those become
needed.

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


---

## Art needed

_(Consolidated 2026-05-03 from `ART_NEEDED.md`. Original file deleted; this section is the canonical copy.)_

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


---

## Item thumbnails inventory

_(Consolidated 2026-05-03 from `docs/item-thumbnails-inventory.md`. Original file deleted; this section is the canonical copy.)_

# Item Thumbnails / Icons Inventory

User-flagged 2026-05-02: vendors / inventory / loot caches / ground drops
should all surface a thumbnail per item. Audit done same day —
**most of the art already exists**; the gap is wiring + a few missing
enum entries + future medieval re-skin.

## Current state

**Items.** 42 items in [`StarterItems`](../scripts/Data/StarterItems.cs)
across 9 `ItemCategory` values (Weapon ×11, Tool ×12, Armor ×2,
Cosmetic ×3, Oddity ×3, Consumable ×3, Ammo ×2, InteractibleObject ×3,
Misc ×3 — some items appear in multiple buckets).

**Sprites already on disk.**
- `assets/art/sprites/scifi_item_atlas.png`
- `assets/art/sprites/scifi_utility_item_atlas.png`
- `assets/art/sprites/scifi_weapon_atlas.png`
- `assets/art/sprites/scifi_tool_atlas.png`
- A handful of polished one-off PNGs under
  `assets/art/sprites/generated/gemini_static_props_2026_04_27/polished/`
  (`power_cell_canister.png`, `portable_terminal.png`,
  `repair_kit_case.png`).

**Registry.**
[`PrototypeSpriteCatalog`](../scripts/Art/PrototypeSpriteModels.cs)
already maps 36/42 items to atlas regions via `GetKindForItem(itemId)`
→ `PrototypeSpriteKind` enum → `PrototypeSpriteDefinition` with full
`AtlasPath` + `SourceRegion` coordinates.

**What renders today:**
- **World drops** (ground tile sprites) — already render the catalog
  art via `PrototypeAtlasSprite` in
  [`WorldRoot.RenderServerItems`](../scripts/World/WorldRoot.cs).
- **Vendor rows** — text-only buttons (price + name).
- **Inventory rows** — text-only `InventoryDragRow` buttons.
- **Hotbar** — text + key digit, no icon.
- **Loot cache / supply-drop** — same as vendor (text-only).

## Gap

1. **Six items have no enum entry / no atlas region:**
   `BackpackBrown`, `BallisticRound`, `EnergyCell`, `StimSpike`,
   `DownerHaze`, `TremorTab`. They render a placeholder square.
2. **Vendor + inventory + hotbar** show text only; need a 32×32
   `TextureRect` child built from the catalog atlas region.
3. **Medieval re-skin** — the existing atlases are sci-fi flavored.
   For the medieval theme we eventually want medieval-flavored
   thumbnails (sword for "stun_baton", arrow for "ballistic_round",
   torch for "flashlight", etc.) keyed by the same item ids so the
   catalog lookup still works.

## Plan (two tasks)

The wiring + missing-enum work is a small agent-1 task and can land
immediately on the existing sci-fi atlases. The medieval re-skin is a
larger curation/generation task in the same shape as
[`docs/medieval-audio-inventory.md`](medieval-audio-inventory.md).

### Wiring (small)

- Add the 6 missing entries to `PrototypeSpriteKind` + sprite
  definitions for them. Use placeholder atlas regions until the art
  agent generates real cells.
- Refactor vendor row / inventory row / hotbar slot render in
  [`HudController.cs`](../scripts/UI/HudController.cs) to add a
  `TextureRect` child showing the atlas region from
  `PrototypeSpriteCatalog.Get(kind)`.

### Medieval re-skin (large — separate task)

- Decide source: AI generation (PixelLab when balance returns) vs
  asset-pack (LPC `weapon/`, `head/`, `accessory/` sheets — dozens of
  free items already), vs OpenGameArt CC0 icon packs.
- For each of the 42 item ids, pick a medieval visual.
- Output: `assets/art/themes/medieval/items/<item_id>.png` (32×32
  each), wired into a per-theme `ItemArtRegistry` keyed by
  `(themeId, itemId)`.
- Add `ItemArtRegistry.Get(themeId, itemId)` that falls back to the
  existing sci-fi catalog when the theme doesn't have a re-skin —
  same module+registry shape as `ThemeArtRegistry`.

## Sources for medieval re-skin

- **LPC weapon/armor/accessory sheets** — already vendored under
  `assets/art/sprites/lpc/`. Hundreds of medieval weapons (swords,
  bows, staves, daggers), shields, hats, pouches. Per-frame; need a
  GDScript composer like `lpc_compose_theme.gd` to crop a single
  inventory cell out of each sheet.
- **OpenGameArt** — search "icons CC0 RPG" for full RPG icon packs.
  Game-Icons.net (CC-BY 3.0) has 4000+ vector icons, also useful.
- **Pixabay** — secondary source; less likely to have game-shaped
  pixel art.
- **PixelLab** — best fidelity match to the existing player sprites
  but blocked on $0 balance (see `docs/pixellab-medieval-buildings-queue.md`).

## License rules (same as audio)

- CC0, CC-BY, OGA-BY, Pixabay Content License, MIT-style audio = OK.
- CC-BY-NC, CC-BY-ND, "personal use only" = NOT OK.
- Every shipped icon goes into `assets/art/CREDITS.md` (new) with
  source URL + author + license id.


---

## Medieval theme inventory

_(Consolidated 2026-05-03 from `docs/medieval-theme-inventory.md`. Original file deleted; this section is the canonical copy.)_

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


---

## Medieval NPC randomization

_(Consolidated 2026-05-03 from `docs/medieval-npc-randomization.md`. Original file deleted; this section is the canonical copy.)_

# Medieval NPC — Identity, Appearance, and Interaction Randomization

How the medieval roster keeps stable identities while randomizing their
look and their dialogue at spawn / encounter time.

## Three layers of randomization

| Layer       | What's stable                                | What randomizes                               | Where it lives                                                 |
|-------------|----------------------------------------------|-----------------------------------------------|----------------------------------------------------------------|
| Identity    | id, name, role, faction, personality, secret | nothing                                       | `npc_roster[]` in `assets/themes/medieval/theme.json`          |
| Appearance  | base bundle + body kind + role               | body tint, hair/beard tint, outfit, gear, hair, hat, weapon/tool layers | `npc.appearance_options[]` -> `assets/art/sprites/lpc/themes/` |
| Interaction | none                                         | greeting line, reaction line, gossip line     | `theme.interactions.*` pools, sampled at runtime               |

## Identity — fixed per NPC

Every NPC entry under `theme.json → npc_roster[]` carries:

```json
{
  "id": "blacksmith_garrick",
  "name": "Garrick the Smith",
  "role": "Village Blacksmith",
  "faction": "village_freeholders",
  "alignment": "neutral",
  "spawn_weight": 3,
  "personality": "...",
  "need": "...",
  "secret": "...",
  "likes": [...],
  "dislikes": [...],
  "description": "..."
}
```

These don't randomize. The same NPC id always gets the same name and the
same secret across worlds, so quests, dialogues, relationships, and
player memory all track to a stable identity.

## Appearance — pick one of N variants

Two new fields per NPC:

```json
"lpc_bundle": "blacksmith_male",
"appearance_options": [
  "blacksmith_male",
  "blacksmith_male_v2",
  "blacksmith_male_v3"
]
```

`lpc_bundle` is the canonical "default" look. `appearance_options` is the
set the server may pick from at spawn. The picker is deterministic per
`(worldId, npcId)`, so a given world always shows the same Garrick variant,
but a different world can roll a different look.

### How the variants are produced

[`tools/lpc_generate_medieval_bundles.py`](../tools/lpc_generate_medieval_bundles.py)
now emits 24 variants per base role. The current role list produces 1,368
theme bundle JSON files plus matching materialized preview/runtime PNGs.

The generator scans the local LPC spritesheet tree and builds broad layer pools
instead of hand-picking only a few hair swaps. It includes medieval-appropriate
options for:

- body/head layers and skin tints,
- hair, beard, and hair/beard tints,
- torso, leg, foot, waist, hat, backpack, shield, and weapon/tool layers,
- role-sensitive combat gear so noncombat villagers do not all spawn with
  shields or weapons.

The generator also blocklists non-theme options such as glowswords, lasers,
firearms, sci-fi armor, robots, space/cyber pieces, and other assets that clash
with the medieval art direction.

The materializer reads each bundle's `tints` data and applies body/hair/beard
colourization while producing the final generated sheets.

### Current variant scale

Current validation from the regeneration pass:

- 1,368 bundle JSON files.
- 1,368 runtime `32x64_8dir_4row` PNGs.
- 1,368 walk-preview PNGs.
- 0 missing layer paths in generated bundle JSON.
- Broad layer variety across torso, legs, feet, hair, hats, weapons, waist,
  beards, backpacks, shields, body tints, hair tints, and beard tints.

Re-run the generator when adding/removing LPC layer families:

```bash
python3 tools/lpc_generate_medieval_bundles.py
```

Then re-run the Godot materializer so `assets/art/generated/lpc_npcs/` matches
the JSON bundle set.

## Relationships — directed graph between NPCs

Each NPC entry now carries a `relationships[]` array:

```json
"relationships": [
  { "target": "tavernkeep_meri", "type": "friend",        "intensity": 2 },
  { "target": "miller_aenwyn",   "type": "rival",         "intensity": 2 },
  { "target": "acolyte_nesta",   "type": "family",        "intensity": 1 }
]
```

Relationship types:

- `friend` — positive, casual
- `rival` — competitive, professional or personal
- `family` — blood or marriage
- `creditor` — `from` is owed something by `target`
- `debtor` — `from` owes something to `target`
- `lover` — current or unrequited
- `distrusts` — wary, not actively hostile
- `fears` — actively avoids
- `mentors` — `from` mentors `target`
- `employs` — `from` employs `target`
- `knows_secret` — `from` knows something compromising about `target`

Intensity is a 0-3 hint for how strongly the relationship colours
their interactions (used to weight gossip lines, dialogue choice
modifiers, etc.).

The graph is **directed and asymmetric** by design: A may consider B a
friend while B considers A a rival. That mismatch is the dramatic
material the prototype's social systems should mine.

## Interactions — sampled from shared pools

Top-level `theme.interactions` carries three pools:

### `greetings_pool` — keyed by role tag

```json
"greetings_pool": {
  "law": [
    "Move along. Or don't. Your call, but I'm watching.",
    "State your business at the gate.",
    ...
  ],
  "trade": [...],
  "chapel": [...],
  "wayfarer": [...],
  "outlaw": [...],
  "wild": [...],
  "peasant": [...]
}
```

When the server starts a dialogue, it samples one greeting from the
union of pools whose tag matches at least one of the NPC's `tags`.

### `reactions_pool` — keyed by event context

```json
"reactions_pool": {
  "approached": [...],
  "complimented": [...],
  "insulted": [...],
  "witnessed_crime": [...],
  "given_gift": [...]
}
```

Triggered by events on the NPC's tile (player approaches, player gifts
an item, NPC witnesses a karma_break, etc.).

### `gossip_templates` — keyed by role tag, with `{relation_name}` placeholder

```json
"gossip_templates": {
  "trade": [
    "{relation_name} owes me three coppers and a debt of honour.",
    "Don't tell {relation_name} I said this, but the work has been sloppy."
  ],
  ...
}
```

When asked for gossip, the server picks one of the NPC's relationships
(weighted by intensity, biased toward `rival` / `knows_secret` /
`creditor`) and substitutes the target NPC's display name into the
template. Result: contextual, character-grounded gossip that stays
consistent across encounters.

## Server-side wiring

The theme NPC wiring is now live:

1. **Spawn-time appearance roll.** NPC spawn picks a deterministic LPC bundle
   from `appearance_options` using `(WorldId, npcId)` and surfaces it through
   snapshots for rendering.
2. **Greeting injection.** Dialogue roots can prepend a themed greeting from
   the matching role/tag pool.
3. **Gossip dialogue.** `dialogue_advance:gossip` resolves a relationship and
   themed template into contextual gossip text.

Still worth expanding:

- Reaction events from `reactions_pool` for approach, gift, witnessed crime,
  and other local triggers.
- More authored dialogue nodes for major NPCs, so procedural greeting/gossip
  sits on top of stronger character-specific trees.

## Re-run procedure

If you edit any of the inputs:

```bash
# 1. Regenerate variant bundles
python3 tools/lpc_generate_medieval_bundles.py

# 2. Materialize generated bundles in Godot so preview/runtime PNGs update
godot --headless --path . --script res://tools/lpc_materialize_theme_bundles.gd --force
```

Both scripts are idempotent — re-runnable as the spec table grows or as
the relationship graph evolves.


---

## Sprite modeling status

_(Consolidated 2026-05-03 from `docs/sprite-modeling-status.md`. Original file deleted; this section is the canonical copy.)_

# Current Sprite Modeling Status

The current prototype sprite work is mostly a **runtime/animation pipeline change**,
not a final visual-art upgrade.

## What changed in-game

- The player sprite now prefers the generated layered v2 preview at `assets/art/sprites/player_v2/player_v2_layered_preview_8dir.png` when present, then falls back to `assets/art/sprites/scifi_engineer_player_8dir.png`.
- The runtime supports named 8-direction idle/walk animations.
- Movement can select diagonal animation names instead of only cardinal directions.
- The art loader can read the PNG alpha directly so stale imported texture data does
  not leave green/chroma artifacts.

## Why the visual difference may be subtle

The active sheet is still a prototype/extracted engineer sheet. It is useful for
wiring the runtime contract, but it is **not** a polished new character model.

Important limitations:

- The source art did not provide fully unique, professional diagonal/body poses.
- Some diagonal directions are still close to side/cardinal poses, so the rotation
  difference is easy to miss during play.
- The current frame size is only `32x32`, which limits visible outfit/body detail.
- The first paper-doll/layer step now exists under `assets/art/sprites/player_v2/`: base body, skin, hair, outfit, and held-tool layers are composited into the active preview sheet.
- The bigger professional plan is still a polished v2 paper-doll/layer system, likely `48x48` or `64x64`, with cleaner animation groups.

## Practical expectation

For now, the current player sheet should be judged as:

- good enough to test movement animation wiring;
- good enough to validate transparent runtime loading;
- not good enough to represent final character quality or final customization.

If the goal is a visible art improvement, the next art slice should replace the generated mannequin pixels with polished base-body art and add more swappable skins/outfits rather than keep making one-off complete characters.


---

## Player V2 next-10 plan

_(Consolidated 2026-05-03 from `docs/player-v2-next-10-plan.md`. Original file deleted; this section is the canonical copy.)_

# Player V2 Next 10 Plan

This is the active follow-through plan for the original `32x64` player model, PixelLab intake, and the reusable paper-doll player pipeline.

## Goal

Turn the current readable `32x64` skeleton into Karma's real reusable player-art pipeline:

- native `32x64` animation contract,
- swappable paper-doll layers,
- safe PixelLab candidate import/review,
- runtime/server-owned appearance selection,
- then return to gameplay systems once the character pipeline is stable.

## Task checklist

### 1. Wire the native `32x64` layered manifest into runtime

Status: done

- Load `assets/art/sprites/player_v2/player_model_32x64_manifest.json` through the existing player-v2 layer manifest/compositor path.
- Support rectangular `frameWidth`/`frameHeight` metadata while preserving old square `frameSize` manifests.
- Make the native `32x64` layered preview/composite usable as a runtime atlas.

### 2. Make appearance selection use the `32x64` layers

Status: done

- Route skin/hair/outfit choices to `layers_32x64` variants.
- Keep `SetAppearance` server-owned.
- Preserve snapshot-driven local/peer rendering.

### 3. Retire the old `32x32` mannequin path as fallback only

Status: done

- Keep legacy `player_v2_manifest.json` and `player_v2_layered_preview_8dir.png` as compatibility/fallback.
- Ensure the active default path is the native `32x64` model/layer stack.
- Update docs/tests so future work does not accidentally polish the old mannequin.

Implemented in this slice: `PlayerV2LayerManifest.DefaultManifestPath` now points at the native `32x64` manifest, the loader supports `frameWidth`/`frameHeight` while preserving legacy square `frameSize`, and smoke tests assert the legacy manifest remains fallback-only.

### 4. Add a PixelLab import review folder/process

Status: done

- Standardize `assets/art/sprites/player_v2/imported/` for PixelLab-normalized outputs.
- Document naming and review expectations.
- Keep imported candidates out of active runtime until reviewed.

Implemented in this slice: `assets/art/sprites/player_v2/imported/README.md` defines the candidate review folder rules and safe import command.

### 5. Import the first PixelLab candidate when available

Status: blocked until PixelLab output exists

- Use `tools/import_pixellab_character.py` on a downloaded PixelLab PNG/ZIP.
- Normalize to the `32x64` 8-direction 4-row contract.
- Compare against the skeleton for direction order, baseline, scale, and no-tool walking.

### 6. Improve the `32x64` skeleton art pass

Status: done

- Improve proportions, hands/feet, diagonal silhouettes, and frame-to-frame consistency.
- Preserve the contract: `8 columns x 4 rows`, `32x64` cells.
- Avoid breaking current runtime readability.

Implemented in this slice: added a small waist/gear cue across directions, regenerated the one-row, 4-row, and compatibility runtime sheets, then regenerated native layer splits with pixel-perfect default recomposition.

### 7. Add tool/backpack/weapon overlay layers

Status: done

- Keep ordinary idle/walk tool-free.
- Add overlays for backpack, held tool, and future weapon/tool states.
- Prepare for later action rows without contaminating movement frames.

Implemented in this slice: added optional `backpack_daypack_32x64`, `tool_multitool_32x64`, and `weapon_practice_baton_32x64` layers to the native manifest. They are omitted from the default preview stack and can be composed explicitly for future loadout/action states.

### 8. Expand the appearance menu

Status: done

- Show current skin/hair/outfit names clearly.
- Add room for preview thumbnails or selectors once variants grow.
- Keep non-pausing Escape menu behavior.

Implemented in this slice: the Escape appearance panel now shows separate current skin/hair/outfit/held-tool labels and reserves preview copy for future thumbnails while keeping the existing server-owned cycle buttons.

### 9. Apply appearance rendering to more player avatars

Status: done

- Broaden snapshot-driven appearance rendering beyond local player and prototype peer.
- Ensure dynamically spawned/multiplayer stand-ins resolve selected layer stacks.

Implemented in this slice: `WorldRoot` now creates, updates, and removes dynamic remote-player avatar nodes from snapshot players outside the local player and static prototype peer, applying each snapshot appearance through the player-v2 compositor.

### 10. Return to gameplay systems after the character pipeline stabilizes

Status: done

Recommended next gameplay slice after tasks 1-9:

- local chat polish and/or fake proximity audio falloff, or
- downed/rescue/carry/execute/clinic loop.

Implemented in this slice: returned to communication polish by adding bounded server-side local chat retention/pruning, with smoke coverage and doc updates. Fake proximity audio and downed/carry remain future gameplay slices, but this documented follow-through list is complete.

## Verification expectations

For each implemented slice:

- Check `git status --short --branch` before editing.
- Run the smallest meaningful verification, usually:
  - Windows `dotnet build Karma.csproj`, and
  - Godot headless smoke test: `res://scenes/TestHarness.tscn`.
- Commit and push verified chunks to `develop`.
- Use Windows PowerShell Git credentials for push from WSL.

## Current recommendation

Do tasks 1-3 first. They turn the current skeleton from a static art experiment into the active reusable runtime character pipeline.


---

## Player model generation next 15

_(Consolidated 2026-05-03 from `docs/player-model-generation-next-15.md`. Original file deleted; this section is the canonical copy.)_

# Player Model Generation Next 15

This is the next review-oriented task list for generating and curating player model candidates after the native `32x64` player-v2 pipeline landed.

## Current intent

Generate a few candidate player models now if possible, keep them out of runtime, and let Sean review them tomorrow before promotion. PixelLab is preferred when available, but local deterministic candidates are useful as fallback/reference attempts.

## Tasks

1. **Create a review-only candidate folder** — keep all generated attempts under `assets/art/sprites/player_v2/imported/review_YYYY-MM-DD/` so runtime does not accidentally load them.
2. **Generate local fallback candidates** — create 3-5 deterministic `32x64`, 8-direction, 4-row candidates from the current skeleton for immediate review when external generation is blocked.
3. **Record candidate metadata** — write an index with source sheet, contract, generation method, and review notes.
4. **Try PixelLab candidate generation when available** — use PixelLab for stronger original concepts, but never paste/commit PixelLab API tokens.
5. **Import PixelLab outputs offline** — normalize downloaded PNG/ZIP outputs through `tools/import_pixellab_character.py` into the review folder.
6. **Audit candidate dimensions** — verify `256x256`, `32x64` cells, 8 direction columns, 4 rows, transparent background.
7. **Review direction readability** — check front/front-right/right/back-right/back/back-left/left/front-left ordering and whether diagonals read as true diagonals.
8. **Review baseline and scale** — ensure feet stay on a consistent baseline and head/body proportions stay stable across frames.
9. **Review animation rows** — confirm idle and three walk rows have visible but not chaotic stepping.
10. **Review paper-doll separability** — decide whether the candidate can split cleanly into base body, skin, hair, outfit, backpack/tool/weapon overlays.
11. **Pick one candidate direction** — choose one visual target or combine the best traits from multiple candidates.
12. **Normalize selected candidate** — clean transparent pixels, trim artifacts, enforce contract, and save a promoted candidate stem.
13. **Split selected candidate into layers** — derive base/skin/hair/outfit/overlay layers and verify pixel-perfect recomposition.
14. **Preview in runtime only after review** — wire the selected candidate behind the existing manifest/compositor path once accepted.
15. **Commit/push verified candidate slice** — run Windows `dotnet build Karma.csproj` and Godot headless `res://scenes/TestHarness.tscn`, then commit/push the reviewed slice.

## Review checklist for tomorrow

- Which silhouette is closest to Karma's player fantasy?
- Which color/material language works best: engineer, settler, medic, scavenger, or another role?
- Are the characters too busy at runtime scale?
- Should the active base body stay tool-free/backpack-free, with overlays for loadout states?
- Should PixelLab replace these local placeholders or use them as references?


---

## LPC everywhere plan

_(Consolidated 2026-05-03 from `docs/lpc-everywhere-plan.md`. Original file deleted; this section is the canonical copy.)_

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


---

## PixelLab medieval buildings queue

_(Consolidated 2026-05-03 from `docs/pixellab-medieval-buildings-queue.md`. Original file deleted; this section is the canonical copy.)_

# PixelLab Buildings Queue — Medieval Theme

Ready-to-run PixelLab MCP commands for the medieval village buildings
atlas. **Blocked on credit balance** — `pixellab-mcp get_balance`
returned `$0 USD` last time we checked. Top up at
[pixellab.ai](https://pixellab.ai), then run these.

## Sizing + style contract

- **View**: `low top-down` (matches the LPC characters and the Cainos
  tiles we use for ground).
- **Output size**: 128×128 per building for one-tile huts, 256×256 for
  larger buildings (chapel, gatehouse, mill). PixelLab's `width` /
  `height` parameters take pixel values; `64` / `128` / `256` are
  recommended.
- **No background**: pass `no_background=true` so the building drops on
  the existing ground tiles without a colour swatch.
- **Style** locked to: "low-top-down medieval village pixel art,
  thatched roofs where appropriate, stone walls, wooden shutters,
  cobblestone foundations".
- **Outline**: `selective outline` (most LPC art uses selective).
- **Shading**: `medium shading` to match Cainos.
- **Detail**: `medium detail`.

## Saved-to paths

Buildings land in
`assets/art/structures/medieval/<id>.png`. Create the folder before
running the prompts:

```bash
mkdir -p assets/art/structures/medieval
```

Each prompt's `save_to_file` should be the absolute path, e.g.:

```
"save_to_file": "C:/Users/pharr/code/karma/assets/art/structures/medieval/smithy.png"
```

## The roster (priority-ordered)

The 13 location archetypes from
[`assets/themes/medieval/theme.json`](../assets/themes/medieval/theme.json),
plus a few generic dressing pieces.

### Tier 1 — core buildings (commission first)

```jsonc
{
  "tool": "generate_image_pixflux",
  "arguments": {
    "description": "low top-down view medieval village blacksmith forge, stone-walled, slate roof, wooden shutters open, anvil + bellows visible at front, smoke from chimney",
    "negative_description": "modern, sci-fi, blurry, photo, text",
    "width": 128, "height": 128,
    "no_background": true,
    "outline": "selective outline",
    "shading": "medium shading",
    "detail": "medium detail",
    "save_to_file": "C:/Users/pharr/code/karma/assets/art/structures/medieval/smithy.png"
  }
}
```

```jsonc
// tavern — The Cracked Tankard
{
  "description": "low top-down view medieval village tavern, two-story, thatched roof, wooden front door with a hanging tankard sign, warm window glow",
  "save_to_file": "...tavern.png"
}
```

```jsonc
// chapel — Chapel of the Pale Star
{
  "description": "low top-down view stone chapel, small bell tower, narrow stained-glass window over the entrance, stone steps, slate roof",
  "width": 256, "height": 256,
  "save_to_file": "...chapel.png"
}
```

```jsonc
// market stall (single)
{
  "description": "low top-down view medieval market stall, striped canvas awning red and cream, wooden counter, baskets of produce, cobblestone footing",
  "width": 96, "height": 96,
  "save_to_file": "...market_stall.png"
}
```

```jsonc
// gatehouse / barracks
{
  "description": "low top-down view medieval gatehouse with a portcullis, two stone towers flanking, banners on the towers, drawbridge approach",
  "width": 256, "height": 256,
  "save_to_file": "...gatehouse.png"
}
```

```jsonc
// almshouse
{
  "description": "low top-down view modest stone almshouse, single story, thatched roof, wooden door with a charity bowl beside it",
  "save_to_file": "...almshouse.png"
}
```

```jsonc
// mill — Greywater Mill
{
  "description": "low top-down view medieval water mill, stone base, thatched roof, large wooden water wheel on one side, mill stream running along the building",
  "width": 256, "height": 256,
  "save_to_file": "...mill.png"
}
```

```jsonc
// stables
{
  "description": "low top-down view wooden stables, multiple stall doors visible, thatched roof, hay bales and a wooden fence at the entrance",
  "save_to_file": "...stables.png"
}
```

### Tier 2 — secondary buildings

```jsonc
// mason yard
{
  "description": "low top-down view stonecutter's yard, stacks of cut stone, partially-finished column, wooden cart with chisels, low workshop hut at back",
  "save_to_file": "...mason_yard.png"
}
```

```jsonc
// duel ring / skirmish pit
{
  "description": "low top-down view circular fighting pit dug into the earth, wooden palisade fence around it, rough-cut benches, training dummies at one end",
  "save_to_file": "...skirmish_pit.png"
}
```

```jsonc
// graveyard
{
  "description": "low top-down view small medieval graveyard, weathered stone gravemarkers in rows, wrought-iron gate, single bare tree, tufts of long grass",
  "save_to_file": "...graveyard.png"
}
```

```jsonc
// hidden den / shadowed guild front
{
  "description": "low top-down view of a narrow alley building with a heavy iron-bound wooden door, small barred window, no signage, half-buried into a stone wall",
  "save_to_file": "...shadow_den.png"
}
```

```jsonc
// hermitage
{
  "description": "low top-down view of a hermit's hut at a wood's edge, daub-and-wattle walls, thatched roof, single shuttered window, smoke from a small stone chimney, herb garden plot beside it",
  "save_to_file": "...hermitage.png"
}
```

### Tier 3 — dressing / props (low priority)

```jsonc
// well
{
  "description": "low top-down view medieval village well, circular stone wellhead, wooden roof on four posts, bucket on a chain, cobblestone surround",
  "width": 96, "height": 96,
  "save_to_file": "...well.png"
}
```

```jsonc
// village sign
{
  "description": "low top-down view medieval wooden village sign post, two cross-arms with carved place names, planted in a small stone cairn",
  "width": 64, "height": 64,
  "save_to_file": "...village_sign.png"
}
```

```jsonc
// trough
{
  "description": "low top-down view rectangular wooden water trough, stone foundation, slight overflow puddle beside it",
  "width": 96, "height": 64,
  "save_to_file": "...trough.png"
}
```

```jsonc
// haystacks (3 sizes)
{
  "description": "low top-down view three medieval hay stacks of varying size on a packed-earth floor",
  "width": 128, "height": 96,
  "save_to_file": "...haystacks.png"
}
```

## After buildings land

Once the PNGs are on disk, the wiring tasks are:

1. **Add a `MedievalBuildings` atlas registry entry.** Sibling to the
   tile registry at
   [`scripts/World/ThemeArtRegistry.cs`](../scripts/World/ThemeArtRegistry.cs).
   Probably easiest to add a separate `BuildingArtRegistry` keyed by a
   `BuildingId` enum (Smithy, Tavern, Chapel, Market, …).
2. **Wire the world generator** — locations from the
   `location_archetypes` array in
   [`assets/themes/medieval/theme.json`](../assets/themes/medieval/theme.json)
   pick a building id; the renderer looks it up in the registry.
3. **Smoke test** — assert that for each `location_archetypes` entry
   in `theme.json`, the registry returns a populated atlas region.

## If we want to skip PixelLab

Alternative: composite Mixel `Topdown RPG 32x32 - Ruins.PNG` slices
plus Cainos `TX Struct.png` into a hand-built atlas. Lower fidelity
but no API spend.


---

## Prototype model art prompts

_(Consolidated 2026-05-03 from `docs/prototype-model-art-prompts.md`. Original file deleted; this section is the canonical copy.)_

# Prototype Model Art Prompts

Paste these prompts into ChatGPT/image tools to generate cleaner art for the
current Karma prototype models. Keep outputs license-safe/original. Treat first
outputs as **reference art** unless they exactly match the runtime contract.

## Shared Style Block

Use this at the top of any prompt below if the tool allows longer prompts:

```text
Game: Karma, a top-down 2D multiplayer life-sim RPG about Ascension/Descension karma choices.
Style: compact readable pixel art, sci-fi frontier colony, cozy but slightly weird, clean silhouettes, crisp nearest-neighbor pixels, no painterly blur.
Camera: top-down RPG / three-quarter top-down object view.
Runtime rules: transparent background preferred; if impossible use flat #00ff00 chroma key. No labels, text, UI panels, metadata, watermarks, grid lines, decorative borders, shadows baked into the background, or prompt notes.
Asset should feel original and license-safe, not copied from an existing game.
```

## Character Sheet Runtime Contract

Use this contract for player/NPC character sheets:

```text
STRICT CHARACTER SHEET FORMAT:
- PNG sprite sheet only.
- Canvas exactly 256x288 px if possible.
- 8 columns x 9 rows.
- Each frame exactly 32x32 px.
- Direction columns left-to-right: front, front-right, right, back-right, back, back-left, left, front-left.
- Row 0: idle pose.
- Rows 1-4: four-frame walking cycle.
- Row 5: run/action-ready pose.
- Row 6: tool/use pose.
- Row 7: melee/impact pose.
- Row 8: interact/reach pose.
- Feet bottom-centered in every frame.
- Character proportions consistent across all frames.
- True 8-direction rotation: diagonals must be real three-quarter views, not copies of side/front/back frames.
```

---

## Characters

### Local Player: Sci-Fi Frontier Engineer

```text
Create a clean 2D pixel-art top-down RPG character runtime sprite sheet for Karma.

Subject: player character, sci-fi frontier engineer / colony repair tech, practical jumpsuit, utility belt, compact backpack or tool harness, readable friendly silhouette, teal/cyan accent lights, rugged boots, no helmet.

Use the Shared Style Block and STRICT CHARACTER SHEET FORMAT.

Important visual notes:
- Should look like a capable frontier mechanic/explorer.
- Outfit should support repair, tool use, and social interactions.
- Avoid bulky armor; keep silhouette readable at 32x32.
- Make diagonal frames visibly distinct.
```

### Mara Venn: Clinic / Repair NPC

```text
Create a clean 2D pixel-art top-down RPG character runtime sprite sheet for Karma.

Subject: Mara Venn, warm but exhausted frontier clinic technician, amber/orange work clothes, teal medical/repair accents, short practical hair or head wrap, tool pouch, small med patch satchel, kind but no-nonsense posture.

Use the Shared Style Block and STRICT CHARACTER SHEET FORMAT.

Important visual notes:
- Must read as a healer/repair-helper NPC at tiny scale.
- Blend medical clinic and repair-yard vibes.
- Avoid fantasy robes; this is sci-fi frontier colony gear.
- Make idle and interact/reach poses feel welcoming/helpful.
```

### Dallen Venn: Tense Civilian / Rival NPC

```text
Create a clean 2D pixel-art top-down RPG character runtime sprite sheet for Karma.

Subject: Dallen Venn, guarded frontier civilian / worried rival NPC, muted blue-gray jacket, tan utility scarf, practical boots, tense posture, slightly suspicious expression, compact silhouette.

Use the Shared Style Block and STRICT CHARACTER SHEET FORMAT.

Important visual notes:
- Should contrast with Mara: cooler colors, more closed-off body language.
- Not a villain; more anxious, protective, distrustful.
- Interact/reach pose should feel like pointing, warning, or reluctant negotiation.
```

### Stranded Peer Player / Generic Multiplayer Stand-In

```text
Create a clean 2D pixel-art top-down RPG character runtime sprite sheet for Karma.

Subject: stranded peer player stand-in, sci-fi frontier traveler, purple/gray improvised clothes, dusty survival pack, patched sleeves, readable neutral multiplayer silhouette.

Use the Shared Style Block and STRICT CHARACTER SHEET FORMAT.

Important visual notes:
- Should be visually distinct from the local engineer but same scale/proportions.
- Good as a generic other-player model before customization exists.
- Keep outfit modular-looking for future paper-doll layers.
```

---

## Item Atlas Prompt

Use this when generating small pickup/quest item art. The current prototype can
use individual icons or sliced atlases; clean transparent icons are easiest to
curate.

```text
Create a single clean pixel-art item icon sheet for Karma.

Canvas: square or compact atlas, transparent background, no labels/text/grid.
Style: top-down/three-quarter pixel-art items, sci-fi frontier colony, readable at 16-32 px in game.
Include each object separated with generous transparent padding.
Objects to include:
1. ration pack — compact tan survival food packet
2. data chip — cyan glowing memory wafer
3. filter core — small cylindrical air/water filter cartridge with teal center
4. contraband package — suspicious dark wrapped parcel with red warning ties
5. apology flower — small bright flower in a rough sci-fi planter/pot
6. portable terminal — chunky handheld screen device with cyan display
7. scrip — small brass/credit coin or token stack

Rules: original license-safe art, crisp pixels, transparent background, no words, no logos, no labels, no watermark.
```

### Individual Item Prompts

```text
Create one transparent pixel-art game item icon for Karma: a tan sci-fi frontier ration pack, compact wrapped survival food packet, readable at 24x24, crisp pixels, no text, no label, no background.
```

```text
Create one transparent pixel-art game item icon for Karma: a cyan glowing data chip / memory wafer, tiny sci-fi circuit detail, readable at 18x18 to 24x24, crisp pixels, no text, no background.
```

```text
Create one transparent pixel-art game item icon for Karma: a small cylindrical filter core cartridge, gray metal shell with teal filter glow, readable at 18x22, crisp pixels, no text, no background.
```

```text
Create one transparent pixel-art game item icon for Karma: a suspicious contraband package, dark wrapped parcel with red hazard ties, readable at 22x18, crisp pixels, no text, no background.
```

```text
Create one transparent pixel-art game item icon for Karma: an apology flower in a small rugged sci-fi planter, pink/yellow flower, readable at 20x24, crisp pixels, no text, no background.
```

```text
Create one transparent pixel-art game item icon for Karma: a chunky portable terminal, dark handheld device with cyan screen and small amber buttons, readable at 24x22, crisp pixels, no text, no background.
```

---

## Utility / Joke / Support Item Atlas Prompt

```text
Create a clean transparent pixel-art item icon sheet for Karma's weird utility/support items.

Style: sci-fi frontier colony, cozy but absurd, readable 16-32 px icons, crisp nearest-neighbor pixels, no labels/text/grid/watermark.
Objects to include, separated with transparent padding:
1. whoopie cushion — red prank cushion with small nozzle
2. deflated balloon — limp pink/purple balloon scrap
3. repair kit — teal compact repair/med-style kit with cross-like tool mark but no text
4. practice stick — simple wooden training baton/stick
5. work vest — orange utility safety vest folded or icon-ready
6. scrip token — brass sci-fi currency token

Transparent background. Original license-safe art only.
```

---

## Weapon Atlas Prompt

```text
Create a clean transparent pixel-art weapon icon atlas for Karma.

Style: sci-fi frontier improvised weapons, readable at 24-40 px, top-down/side three-quarter item icons, crisp pixels, no labels/text/grid/watermark.
Objects to include, separated with transparent padding:
1. stun baton — short black/metal baton with blue electric tip
2. electro pistol — compact pistol with cyan coil accents
3. SMG-11 — small sci-fi submachine gun, dark metal, cyan accent
4. shotgun mk1 — chunky frontier shotgun, worn metal and grip
5. rifle-27 — long practical colony rifle
6. sniper X9 — long precision rifle with small scope
7. plasma cutter — industrial tool-weapon with glowing cutting head
8. flamethrower — compact tank-and-nozzle weapon, orange accent
9. grenade launcher — stubby launcher with drum/chamber
10. railgun — sleek long electromagnetic rifle with blue rails
11. impact mine — small disk mine with warning color accents but no symbols/text
12. EMP grenade — small sci-fi grenade with blue pulse core

Transparent background. Original license-safe art only.
```

## Tool Atlas Prompt

```text
Create a clean transparent pixel-art tool icon atlas for Karma.

Style: sci-fi frontier repair/survival tools, readable at 20-36 px, crisp pixels, no labels/text/grid/watermark.
Objects to include, separated with transparent padding:
1. multi-tool — compact folding sci-fi utility tool
2. welding torch — handheld repair torch with blue flame/nozzle
3. medi patch — small medical patch injector/packet
4. lockpick set — compact electronic lockpick kit
5. flashlight — rugged frontier flashlight with blue-white lens
6. portable shield — folded shield generator puck/bracelet
7. hacking device — small black/cyan cracking module
8. scanner — handheld scanner with glowing display
9. grappling hook — compact launcher/hook device
10. chem injector — small injector vial tool
11. power cell — glowing battery cell
12. bolt cutters — compact heavy cutters
13. magnetic grabber — telescoping magnet grabber tool

Transparent background. Original license-safe art only.
```

---

## Structure / Station Prompt

Use this for replacing placeholder station/fixture visuals.

```text
Create a clean transparent pixel-art top-down/three-quarter prop and structure atlas for Karma, sci-fi frontier colony style.

Canvas: compact atlas with transparent background, objects separated with padding, no labels/text/grid/watermark.
Objects to include:
1. clinic marker sign — small medical/repair clinic sign, no text, cross-like icon allowed if abstract
2. market stall marker — small barter kiosk/stall
3. repair yard fixture — filter stack / machine console that can be repaired or sabotaged
4. rumor board — public notice board with papers but no readable writing
5. saloon/social hub sign — neon-ish social station prop, no text
6. restricted shed marker — locked storage shed/door prop
7. oddity yard marker — strange fenced relic display
8. duel ring marker — floor circle/marker prop
9. farm plot marker — compact hydroponic/farm bed
10. black market marker — shady tarp-covered kiosk
11. apology engine — weird machine with heart/gear motif, no text
12. broadcast tower base — small antenna console
13. war memorial marker — abstract memorial slab, no text
14. witness court marker — small civic podium/marker

Style: readable at small game scale, cozy sci-fi frontier, original license-safe pixel art, transparent background.
```

---

## Generated NPC Role Prompt Template

Use this when making a batch of generated NPC role variants.

```text
Create a clean 2D pixel-art top-down RPG character runtime sprite sheet for Karma.

Subject: <NPC role>, from a <station type> in a sci-fi frontier colony.
Personality: <friendly / suspicious / exhausted / flashy / nervous / stern>.
Outfit: <short outfit description>.
Color identity: <2-3 key colors>.
Gameplay read: should immediately communicate <medic / trader / repair worker / rumor broker / farmer / guard / black-market dealer / witness clerk>.

Use the Shared Style Block and STRICT CHARACTER SHEET FORMAT.

Important:
- True 8-direction rotation.
- Compact readable silhouette at 32x32.
- No labels, no background, no text, no grid.
```

Example subjects:

- clinic medic with teal/white utility coat
- market trader with yellow scarf and cargo apron
- repair yard mechanic with orange work vest and welding mask pushed up
- rumor broker with purple coat and portable radio headset
- saloon host with warm red jacket and neon pin
- restricted shed guard with gray armor vest
- hydroponic farmer with green utility overalls
- black-market dealer with dark coat and hidden satchel
- witness court clerk with blue civic sash and tablet

## Validation Reminder

After saving generated art into the repo:

```bash
python3 tools/audit_art_library.py
python3 tools/prepare_character_sheet.py validate assets/art/sprites/<character_sheet>.png
```

Then run the gameplay checks before wiring anything into code:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test.ps1
```


---

## Professional character art systems

_(Consolidated 2026-05-03 from `docs/professional-character-art-systems.md`. Original file deleted; this section is the canonical copy.)_

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


---

## Sound needed

_(Consolidated 2026-05-03 from `SOUND_NEEDED.md`. Original file deleted; this section is the canonical copy.)_

# Audio Assets Needed

Tracks music, ambience, and sound effects required for each gameplay step
and the systems built so far. Append as features are designed. Mirror the
style of `ART_NEEDED.md`.

Tone target (matches the comedy karma sandbox feel): grounded but not
gritty — a sci-fi/western frontier vibe that leaves room for slapstick.
Avoid horror/sting motifs; karma swings should feel social, not tragic.

Implementation note: most cues fire from server events (`ServerEvent` ids).
The list below names the *event hook* in parens where one already exists
so future audio wiring has a clear binding point.

---

## Music

### Match-state stems
- **Lobby loop** (1–2 min loop) — calm, anticipatory; plays during
  `MatchStatus.Lobby` while players ready up. Soft western/space-frontier
  themed pad with a subtle pulse.
- **Match running theme** (3–5 min looping bed) — main gameplay music;
  starts on `match_started` event. Should leave dynamic headroom for
  layered stingers.
- **Match end fanfare** — fires on `match_finished`; ~10 sec resolution
  motif before transitioning to a quiet post-match bed.
- **Post-match free-roam bed** — ambient loop after the match ends, while
  players read the `MatchSummarySnapshot`.

### Saint / Scourge intensity layers
- **Saint motif layer** — additive, hopeful, choral or warm strings;
  swells when a player first claims Saint (`saint_claimed` event).
- **Scourge motif layer** — additive, low brass / dissonant strings;
  swells when a player first claims Scourge (`scourge_claimed` event).
- **Crossfade rules** — when both Saint and Scourge are held, both layers
  ride at half volume.

### Combat heat layer
- **Heat-warm cue** — small percussive flourish when a tile chunk crosses
  into the "warm" heat threshold (Step 12). One-shot, not looping.
- **Heat-critical layer** — additive low rumble / tense bed that loops
  while the local player stands on a "critical" heat chunk.

---

## World Event Cues

These are one-shot stingers tied to specific server events.

- **`supply_drop_spawned`** — a clear bell + faint comms-chatter cue when
  a supply drop appears.
- **`supply_drop_claimed`** — short positive flourish for the claimer
  (positional).
- **`supply_drop_expired`** — disappointed two-note "missed it" cue.
- **`station_claimed`** — short triumphant flag-raising stinger.
- **`bounty_claimed`** — coin-purse stinger; warmer than a kill cue.
- **`wanted_bounty_claimed`** — heavier law-flavored stinger over the
  bounty cue.
- **`player_wanted`** — alert siren / wanted-poster stamp; one-shot for
  the marked player and nearby witnesses.
- **`trophy_drop`** — ceremonial dog-tag clink layered over a soft
  victory whoosh.
- **`karma_break`** — distinctive chime + pitch dip; recognizable
  signature for the moment of break.
- **`saint_claimed` / `scourge_claimed`** — title fanfare (one variant
  each, ~3 sec).
- **`match_started`** — bell + crowd murmur into the running theme.
- **`match_finished`** — long resolution chime; gates into the post-match
  bed.

---

## Per-Step SFX

### Step 2 — Repair Mission
- **Repair tool use** — wrench/torch SFX loop while the repair action is
  in progress; finishes with a "fixed" snap.
- **Sabotage** — mirrored variant: tool wrench but with a shorted-circuit
  zap on completion.

### Step 3 — Delivery Quest
- **Item handoff** — cloth/paper rustle when a quest item is delivered.

### Step 4 — Rumor Quest
- **Rumor shared** — gossip whisper layered over the standard chat blip.
- **Rumor exposed (public)** — short brass flourish; the secret is out.
- **Rumor buried** — muted thud; the player chose discretion.

### Step 5 — Paragon Favor (perk)
- **Paragon aura loop** — soft choral/airy bed that loops while the buff
  is active.

### Step 6 — Abyssal Mark (perk)
- **Abyssal aura loop** — low warbly bed; mirrors Paragon but uneasy.

### Step 7 — Posse Formation
- **Invite sent** — single notification ping (cute, not alarming).
- **Posse formed** — warm two-note "alliance" stinger.
- **Posse left** — soft sigh / thud cue.

### Step 8 — Posse HUD Panel
- *(no new audio)*

### Step 9 — Saint/Scourge NPC Behavior
- **NPC welcome (Saint)** — bright greeting bell underlay on dialogue
  open.
- **NPC fearful (Scourge)** — quieter dialogue open with a subtle drop
  in pitch.
- **Price-shift cue** — coin clink when a price changes due to title.

### Step 10 — Chat Tabs
- **Local chat send** — short blip (existing in spirit; confirm asset).
- **Posse chat send** — same blip but with a slight pitch bump for the
  posse channel.
- **System message arrive** — neutral notification ping.

### Step 11 — Interior Audibility Filtering
- **Inside-muffle filter** — apply a low-pass / dampened version of the
  local chat blip and ambient bed when the listener is inside a structure
  hearing an outside speaker (or vice versa).
- **Door open/close** — short wood/metal door cue (theme-flavored).

### Step 12 — Combat Heat Tracking
- **Heat-warm one-shot** — see "Combat heat layer" under Music.
- **Heat-critical loop** — see above.

### Step 13 — Smarter Respawn
- **Respawn arrive** — soft re-materialize cue at the new spawn tile.

### Step 14 — Downed State
- **Player downed** — heavy thud + breath; positional.
- **Downed countdown tick** — quiet metronome cue every N seconds while
  countdown runs (only for the downed player).
- **Countdown expiring (last 5s)** — pulse rises in pitch.

### Step 15 — Rescue Intent
- **Rescue grab** — cloth/grunt cue when a rescuer picks up a downed
  player.
- **Rescue carry loop** — heavy footstep loop while carrying.
- **Rescue drop-off** — soft "set down" thud when rescue completes
  near a clinic.

### Step 16 — Clinic Recovery
- **Clinic auto-revive** — warm chime + scrip-deduct coin clink.
- **Clinic-revive denied (no scrip)** — short "denied" buzz.

### Step 17 — Road/Path Generation
- *(no audio)*

### Step 18 — Path-Aware Rendering
- **Footstep variants** — different footstep tone per surface
  (dirt path / paved / interior / mount). Loop driven by movement speed.

### Step 19 — Mount/Vehicle Entity
- **Mount idle hum** — soft hover/breath loop near unoccupied mounts.
- **Mount move loop** — engine/hoof loop while moving.

### Step 20 — Mount/Dismount
- **Mount-up cue** — short hop + click.
- **Dismount cue** — hop down + click.

### Step 21 — Karma Watermark
- *(no audio)*

### Step 22 — Karma Title-Change Broadcast
- **New Saint** — see Saint title fanfare (Music section).
- **New Scourge** — see Scourge title fanfare.
- **Lost title** — quiet descending two-note motif.

### Step 23 — Match End Summary Snapshot
- **Summary panel open** — soft paper-unfurl cue.

### Step 24 — Warden Perk / Wanted
- **IssueWanted accepted** — heavy stamp + warning klaxon (short, not
  alarming over the music bed).

### Step 25 — Wraith Perk
- **Wraith speed loop** — windy whoosh layer while SpeedModifier > 1.
- **Wraith trail tail** — short fade-out when the buff drops.

### Step 26 — Bounty System
- **Bounty accrued** — quiet coin-stack ramp when bounty rises across
  threshold.
- **Bounty claimed** — see World Event Cues.

### Step 27 — Player Status Effects
- **Poisoned tick** — soft sick gurgle every N seconds while poisoned.
- **Burning tick** — crackle every N seconds.
- **Status applied / cleared** — short flair; one variant per status.

### Step 28 — Contraband Item Tag
- **Contraband near law NPC tick** — quiet alert blip when the karma
  decay fires (positional, only the carrier hears it loud).
- **Contraband detected (full reveal)** — siren-flavored cue.

### Step 29 — Lobby / Ready-Up Flow
- **ReadyUp** — short positive ping when a player readies.
- **All-ready (match start countdown)** — building drum roll into the
  match-running theme.

### Step 30 — Supply Drop World Event
- *(see World Event Cues)*
- **Drop arrive** — descending whistle + thud as the crate lands.

### Step 31 — NPC Patrol Routes
- **Patrol footstep** — uses general NPC footstep set; no new cue
  required unless we want a soft wood/concrete clack on certain tiles.

### Step 32 — Reputation Decay
- *(no audio)*

### Step 33 — Faction Store Gating
- **Locked offer attempt** — denied buzz with a slight faction-themed
  flavor (clinic = chime, guild = wrench tap, dealer = scoff).

### Step 34 — Station Claim Intent
- *(see `station_claimed` cue)*
- **Passive scrip tick** — barely-audible coin-bell every N seconds when
  near a claimed structure (positional, low volume).

### Step 35 — Death Trophy Drop
- *(see `trophy_drop` cue)*

### Step 36 — Crafting Intent
- **Workbench loop** — short work loop while crafting.
- **Craft complete** — bright success chime.
- **Craft denied (missing ingredient)** — denied buzz.

### Step 37 — Posse Shared Quest
- **Posse quest start banner** — group-flavored fanfare (similar to
  posse-formed but longer, ~2 sec).
- **Posse quest complete** — celebratory chime over the standard
  quest_completed cue, scaled across all members.

### Step 38 — World Tier Zones (Lawless)
- **Enter lawless zone** — short low whoosh + ambient bed swap to a
  more ominous lawless ambience.
- **Exit lawless zone** — bright reverse whoosh + return to default bed.

### Step 39 — Fog of War
- *(no audio for filtering, but optional:)*
- **New chunk revealed** — soft sparkle when a previously-unvisited
  chunk becomes visible.

### Step 40 — HUD Minimap
- *(no audio)*

---

## UI / HUD Sounds

- **Inventory open / close** — soft latch.
- **Hotbar slot select** — quiet click; pitch shifts slightly per slot
  (1 lowest, 9 highest).
- **Shop browse panel open** — coin-tray unfurl.
- **Sell panel open** — same coin-tray with a slight downward pitch.
- **Purchase complete** — coin-clink + small chime.
- **Sell complete** — coin-clink + neutral pip.
- **Insufficient funds / rep** — short buzz.
- **Dialogue open** — soft attention chime.
- **Dialogue choice select** — short pip.
- **Wallet/scrip update** — barely-audible counter when scrip changes.
- **HUD prompt show** — soft pop.
- **Escape menu open** — pause-style swell.
- **Notification toast** — gentle neutral pip for one-line HUD prompts.

---

## Ambience Beds

- **Default outdoor bed** — wind, distant industry, occasional bird/comms
  bird-equivalent. Looping, ~2 min.
- **Saloon / interior bed** — low chatter, glasses clinking; 90-sec loop.
- **Clinic interior bed** — soft hum, paper rustle, distant footsteps.
- **Workshop interior bed** — gentle work clatter, idle machinery.
- **Lawless zone bed** — sparse wind, distant howls/comms-static.
- **Night layer** — additive cricket/wind/static layer for late-match
  ambience; mixed in over the existing bed.

---

## Voice / Vocalizations

(Avoid full voice-over — too expensive. Use short *vocal stingers* the
way Animal Crossing does.)

- **Player vocal set** — short laugh, sigh, taunt, ouch grunt, ready-up
  affirmative, surrender; ~6 cues per player voice slot. Multiple voice
  slot variants for character variety.
- **NPC greet/farewell vocal** — one short affirmative + one short
  goodbye per NPC role (clinic, vendor, law, dealer).
- **NPC reaction vocals** — quick mutters keyed to events:
  shocked-at-theft, impressed-by-saint, scared-by-scourge.
- **Crowd murmur** — short layered crowd reaction stinger for big public
  events (Saint claimed, Scourge tribute, public confession).

---

## Theme Variants

When implementing per-theme palettes, the same cue names should resolve
to theme-appropriate samples without code changes:

| Cue family | Western flavor | Space flavor | Post-Apoc | Fantasy |
|------------|----------------|--------------|-----------|---------|
| Music bed | Acoustic guitar + harmonica | Synth pad + comms | Ambient drone + scrap | Strings + flute |
| Combat heat | Snare drum roll | Alert klaxon | Distorted siren | War drum |
| Saint motif | Choir + church organ | Ethereal pad | Distant choir | Cathedral choir |
| Scourge motif | Low brass + cymbal | Bass synth pulse | Distorted howl | Pipe organ minor |
| Death/trophy | Spurs + clink | Comms beep | Scrap clatter | Metal clang |
| Footstep dirt | Boot crunch | Mag-boot | Boot + grit | Leather boot |
| Door | Saloon swing | Pneumatic hiss | Metal scrape | Wooden creak |
| Coin | Silver eagle clink | Credit chip | Bottle cap clatter | Gold pile clink |
| Bell / chime | Saloon piano note | Synth tone | Found-metal ring | Temple bell |

---

*Updated: 2026-04-29 (initial — covers steps 1–40 + UI/shop layer).*


---

## Medieval audio inventory

_(Consolidated 2026-05-03 from `docs/medieval-audio-inventory.md`. Original file deleted; this section is the canonical copy.)_

# Medieval Audio Inventory & Sourcing Plan

Companion to [`SOUND_NEEDED.md`](../SOUND_NEEDED.md). That document is the
canonical cue list (tone target = sci-fi/western) — this one re-skins it
for the medieval theme that
[`WorldConfig.CreatePrototype`](../scripts/World/WorldConfig.cs) now ships
as the default. New audio for medieval mode should be wired here first;
the original sci-fi/western set can stay registered for future themes.

## Current state (2026-05-03)

- `assets/audio/music/` contains the menu placeholder plus imported medieval /
  Celtic tracks used by the menu/gameplay music loader.
- `assets/audio/sfx/` now contains real `.wav` one-shots for the current
  prototype cues: karma break, contraband, purchase, reload, supply horn, door,
  interaction, clinic revive, bounty, attack/hit, footsteps, grunts, and sword
  cues.
- `scripts/Audio/AudioEventCatalog.cs` is the active binding layer. It maps
  server event ids and explicit `audioCue` ids to the SFX files above.
- `scripts/Net/AuthoritativeWorldServer.cs` now attaches `audioCue` data to
  most gameplay interactions: movement, pickups, structure interactions,
  purchases/sales/transfers, item use, repair, craft, place, reload, dialogue,
  quests, mounts, and combat.
- `scripts/UI/HudController.cs` plays the latest event cue from snapshots,
  preferring `audioCue` when present and falling back to the event id.
- `scripts/World/WorldRoot.cs` still handles positional door-open playback for
  `door_opened` events.

Most medieval cues currently reuse the closest available one-shot. The cue ids
are intentionally stable so replacing a reused placeholder with a bespoke clip
is just a catalog path change.

## Sources (license-safe)

User-flagged starting point (added 2026-05-02):

- **Pixabay — Medieval Game music search**
  <https://pixabay.com/music/search/medieval%20game/> — large catalogue
  of medieval-flavoured loops + cues. License: Pixabay Content License
  (free for commercial use, no attribution required, but cannot be sold
  or redistributed unaltered as a standalone audio asset). Capture the
  track URL + author name in the credits manifest even though attribution
  isn't required, because licensors occasionally retract clips and we
  want to be able to swap them out cleanly.

Additional license-safe sources to triage in the same pass:

- **OpenGameArt.org** — filter by CC0 / CC-BY 3.0 / CC-BY 4.0 / OGA-BY
  3.0. Search "medieval", "fantasy", "tavern", "village". Tag CC-BY
  entries clearly — they require attribution in the credits manifest +
  game credits screen.
- **Freesound.org** — filter to CC0 only for SFX (CC-BY allowed but
  attribution overhead is high for many small clips). Great for
  one-shots: bell tolls, sword hits, footsteps on cobblestone.
- **Kevin MacLeod (incompetech.com)** — CC-BY 4.0; attribution required.
  Strong medieval / fantasy catalogue; useful for music beds.
- **Free Music Archive (freemusicarchive.org)** — filter to CC0 / CC-BY;
  watch out for "non-commercial only" tracks (those are off-limits since
  Karma may eventually ship paid).
- **YouTube Audio Library** — only the "no attribution required" subset
  is safe to ship. Avoid the "attribution required" half unless we want
  to maintain that overhead.

License rules for this repo:

- **Allowed**: CC0, CC-BY 3.0, CC-BY 4.0, OGA-BY 3.0, Pixabay Content
  License, MIT-style audio licenses.
- **Not allowed**: any "non-commercial" license (CC-BY-NC, CC-BY-NC-SA),
  any "no derivatives" license (CC-BY-ND), unclear or "personal use
  only" terms.
- **Required for every shipped clip**: file path under `assets/audio/`,
  source URL, author display name, license id, attribution string (if
  any), short description of intended use. All recorded in
  `assets/audio/CREDITS.md` (new) and mirrored in `THIRD_PARTY_NOTICES.md`.

## What's needed (medieval re-skin)

### Music (3-5 tracks)

Bind these in `PrototypeMusicPlayer` so the procedural fallback only
runs when the file is missing.

| Theme name (existing enum) | Medieval flavor | Approx length | Where it plays |
|----------------------------|-----------------|---------------|----------------|
| `SandboxCalm` | Lute + soft strings, mid-village mood | 2-4 min loop | Default sandbox, free-roam |
| `EventTension` | Hand drums + low strings, urgent | 2-3 min loop | In-game event prototype |
| `ScenarioAmbient` | Drone + bowed strings, atmospheric | 3-5 min loop | Scenario playback |
| `TavernInterior` (new) | Lute + tin whistle + crowd | 90 sec loop | Tavern interior bed |
| `ChapelInterior` (new) | Choir + soft bell | 90 sec loop | Chapel interior bed |

`TavernInterior` and `ChapelInterior` are stretch — useful to have on
hand even before the interior-music swap is wired.

### SFX (event-keyed)

The original 10 high-level targets are now covered by `.wav` files and catalog
bindings:

| Event id | Path | Medieval cue |
|----------|------|--------------|
| `karma_break` | `assets/audio/sfx/karma_break_stinger.wav` | Cracked bell + dropped chains |
| `contraband_detected` | `assets/audio/sfx/contraband_alarm.wav` | Watchtower bell, urgent |
| `purchase_complete` / `item_purchased` / `item_sold` | `assets/audio/sfx/purchase_chime.wav` | Coin clink + small bell |
| `weapon_reloaded` | `assets/audio/sfx/reload_click.wav` | Crossbow draw + click (or arrow nock) |
| `supply_drop_spawned` | `assets/audio/sfx/supply_drop_horn.wav` | Town crier horn |
| `door_opened` | `assets/audio/sfx/door_open.wav` | Heavy wood door creak |
| `structure_interacted` and general interactions | `assets/audio/sfx/interact_pop.wav` | Latch click |
| `clinic_revive` / `item_used_heal` | `assets/audio/sfx/clinic_revive_chime.wav` | Bowl bell + soft choir swell |
| `player_attacked` | `assets/audio/sfx/hit_thud.wav` | Sword on shield (or fist on leather) |
| `wanted_bounty_claimed` | `assets/audio/sfx/bounty_paid.wav` | Coin pour into chest |

Additional covered prototype cues include `item_picked_up`,
`item_transferred`, `item_placed`, `item_used`, `drug_used`,
`item_repaired`, `item_crafted`, `dialogue_*`, `quest_*`, `player_mounted`,
`player_dismounted`, `mount_bag_transfer`, `footstep_dirt`,
`footstep_stone`, `footstep_wood`, `grunt_pain`, `grunt_attack`,
`sword_swing`, and `sword_hit`.

Still useful to source: bespoke per-item equip sounds, proper medieval
footstep variants on cobblestone / dirt / wood, ambient market chatter loop,
and distant chapel bell rings.

### Ambience beds (low priority)

- Outdoor village bed — wind, distant cart, occasional bird (90 sec loop)
- Tavern bed — chatter, mugs, lute one-shots (90 sec loop)
- Chapel bed — soft choir hum, candle-flicker rustle (90 sec loop)
- Wilderness bed — wind, leaves, distant wolf (60 sec loop)
- Night layer — owl, cricket, distant bell (additive, 60 sec)

## Wiring contract for the agent

Audio integration is a two-step:

1. Drop files under `assets/audio/{music,sfx,ambient}/`. Use the exact
   paths above so `AudioEventCatalog` resolves them without code changes.
   For new music slots (`TavernInterior` / `ChapelInterior`), add the
   enum + sample-load in `PrototypeMusicPlayer` and gate procedural
   generation behind "no file present at expected path".
2. Update credits in two places:
   - `assets/audio/CREDITS.md` (new) — one entry per clip with all license
     fields listed above.
   - `THIRD_PARTY_NOTICES.md` — append a "Medieval audio" section with
     the same author / license summary.

Smoke verification: `tools/test.ps1` exit 0; manually in-game, the menu
theme stays the same (it's a different file), but interacting with a
door / smith / chapel triggers the new clips.

## Out of scope (for now)

- Voice-over or spoken NPC lines.
- Per-NPC vocal stingers (mentioned in `SOUND_NEEDED.md` "Voice /
  Vocalizations" — too expensive to commission for prototype).
- Adaptive / vertical-mix layered music. Single-loop tracks are fine.
- Spatial / 3D audio. Stereo positional via Godot's
  `AudioStreamPlayer2D` is enough.


---

## Proximity chat & NPC voice research

_(Consolidated 2026-05-03 from `docs/proximity-chat-and-npc-voice-research.md`. Original file deleted; this section is the canonical copy.)_

# Proximity Chat and NPC Voice Research

This note captures the likely communication direction for Karma: local text chat,
proximity voice chat between players, and longer-term spoken NPC conversations.

Current priority decision: **stick with player-to-player proximity voice/text as
the practical feature path**. NPC speech-to-text / LLM / text-to-speech remains a
research backlog item until the player communication stack is stable.

## Goals

- Let nearby players communicate naturally without global voice chaos.
- Make distance matter: voices get quieter as players move apart.
- Preserve server authority for who can hear whom, while keeping voice transport
  efficient enough for a multiplayer prototype.
- Leave room for NPCs that can listen to a player and speak back with spatial
  volume, even if that stays research/prototype-only for a while.

## Player text chat

Text chat is the safest first communication feature.

Recommended channels:

- `Local` — visible only within proximity range.
- `Party/Posse` — visible to temporary team/posse members.
- `Faction` — optional later.
- `System` — server messages, world events, clinic delivery notices, etc.

For local text, the server should decide recipients by distance, not the client.
The client submits a chat intent; the authoritative server snapshots/events route
it only to eligible players.

Potential karma/social hooks:

- Rumorcraft can distort or amplify overheard local text.
- NPCs/factions can react to shouted/nearby public chat later.
- Moderation/report tooling is easier with text than raw voice.

## Player proximity voice chat

Proximity voice is feasible, but it is a separate networking/audio system from
normal server-authoritative gameplay messages.

Godot-relevant options found during research:

- Godot has `AudioStreamMicrophone` and audio buses for microphone capture.
- Godot supports WebRTC classes; native desktop builds require an external
  `webrtc-native` GDExtension, while browser exports have built-in WebRTC support.
- Community Godot VOIP projects such as `one-voip-godot-4` use microphone capture,
  Godot bus effects/audio streams, Opus compression, and packet push/playback.
  That project points to `two-voip-godot-4` as a more active successor.
- GodotSteam has Steam voice APIs for Steam-specific builds, including recording,
  compressed voice data, decompression, and playback.

### Recommended architecture

Use the gameplay server for **voice permissions and proximity metadata**, not raw
high-bandwidth audio mixing at first.

1. Client captures microphone with push-to-talk or voice activation.
2. Audio is encoded with Opus or platform voice API.
3. Voice packets are sent peer-to-peer, via a voice relay, or through a dedicated
   voice service depending on the final networking stack.
4. The authoritative game server periodically tells each client which speakers are
   audible and each speaker's relative distance/falloff.
5. Each receiving client plays one audio stream per remote speaker with volume and
   optional stereo pan based on server-approved proximity.

### Distance falloff

Use a clamped falloff curve instead of pure inverse square, because readable voice
in a top-down game should feel designed rather than physically exact.

Suggested prototype values:

- `clearRadius`: within 3-4 tiles, voice is full volume.
- `fadeRadius`: from 4-14 tiles, voice smoothly fades.
- `maxRadius`: beyond 14-18 tiles, voice is inaudible.
- Optional `shout` mode later can increase radius with social/stealth tradeoffs.

Example normalized volume:

```text
if distance <= clearRadius: volume = 1.0
if distance >= maxRadius: volume = 0.0
otherwise:
  t = (distance - clearRadius) / (maxRadius - clearRadius)
  volume = (1 - smoothstep(t)) * speakerVolume * listenerVoiceVolume
```

For 2D presentation:

- Volume communicates distance.
- Stereo pan can lightly reflect left/right position.
- Avoid aggressive panning; players may use mono speakers/headsets.
- Occlusion through walls/structures can be added later as a multiplier, not a
  first-pass requirement.

### UX and safety requirements

Voice needs more safety/settings than most features:

- Push-to-talk default for early builds.
- Mute self / mute player / block player.
- Per-player voice volume.
- Global voice volume slider.
- Visual speaking indicator over nearby players.
- Accessibility: text chat remains available; optional speech-to-text later.
- Abuse reporting strategy before public playtests.
- Clear mic permission flow and no recording without explicit consent.

## NPC voice conversations

NPC voice conversations are intentionally **research/to-do**, not the first
implementation target. The current NPC interaction model remains: walk up to an
NPC, inspect their prompt, and choose from a few server-generated options.

The long-term idea is to make that feel more organic without losing game-state
authority:

- NPCs can greet the player with a contextual line or exclamation when approached.
- The player can still choose options, type a freeform line, or eventually speak
  a line through push-to-talk speech-to-text.
- An LLM-style dialogue layer can respond more naturally using bounded context:
  NPC personality, station state, quest state, faction, relationship, and player
  karma.
- The response text can later be voiced through TTS and played spatially from the
  NPC's world position.

The player-to-NPC voice idea is plausible, but it is a larger research track than
player proximity voice. It has four separate hard problems:

1. **Speech-to-text** — convert player speech to text.
2. **Dialogue brain** — decide what the NPC says using scripted dialogue,
   generated quest state, faction/karma context, or an LLM-like service.
3. **Text-to-speech** — synthesize NPC spoken audio.
4. **Spatial playback** — play the NPC response from the NPC position with the
   same distance falloff as proximity voice.

The last part is very doable in Godot: once we have an audio clip/stream, play it
through an NPC-attached audio player and apply the same 2D volume falloff model.
The harder parts are live STT/TTS latency, cost, privacy, moderation, and keeping
NPC dialogue grounded in server-owned game state.

### Recommended NPC approach

Start with a more natural **text-first interaction shell**, then voice-enable later:

1. Player approaches NPC.
2. NPC may emit a short contextual greeting/exclamation, e.g. station stabilized,
   station compromised, faction-friendly, suspicious, quest-ready, etc.
3. Player opens NPC interaction.
4. Player can choose from generated choices, type a freeform line, or eventually
   speak via STT.
5. Server sends compact NPC context:
   - NPC id/personality/faction
   - station state
   - quest state
   - player karma/faction/relationship context
   - recent safe conversation summary, if any
6. Dialogue system returns text constrained to allowed intents/options.
7. Server-owned systems decide any real quest/reward/karma/item changes.
8. Optional later: TTS turns the returned text into audio.
9. NPC audio plays spatially with proximity falloff and subtitles.

For spoken input later:

1. Player holds an `Ask NPC` push-to-talk key while near the NPC.
2. Client captures mic audio.
3. STT returns text.
4. The normal text NPC pipeline handles the response.
5. TTS response plays from the NPC's world position.

### NPC voice constraints

- NPCs must not invent game authority. Generated speech can suggest flavor, but
  quests/rewards/items/karma changes must come from server-owned systems.
- Avoid always-on NPC listening. Use explicit interaction/push-to-talk.
- Cache common NPC lines and TTS outputs to reduce cost/latency.
- Provide subtitles for every NPC voice line.
- Keep a non-voice fallback for accessibility and offline/dev builds.

## Open research questions

- Which target multiplayer stack do we prefer for Karma long term: Steam,
  WebRTC, ENet plus a separate voice relay, or a hosted voice service?
- Is a community Godot VOIP extension mature enough for Godot 4.6 desktop builds?
- How much voice traffic should pass through our servers vs peer-to-peer?
- What moderation tools are required before wider playtesting?
- Which STT/TTS provider is acceptable for prototype NPC voice, if any?
- Can we support local-only/offline generated NPC text without external services?

## Prototype order

1. Implement local text chat routed by server proximity.
2. Add chat bubbles and/or compact local chat log.
3. Add server-owned audibility model: who can hear whom and at what falloff.
4. Prototype proximity voice locally with fake/generated audio sources first.
5. Evaluate a Godot 4 VOIP extension or Steam voice path.
6. Add push-to-talk proximity voice for player-to-player only.
7. Add NPC text conversation backed by existing generated NPC/quest/state systems.
8. Research STT/TTS and play NPC TTS spatially as an optional experiment.


---

## NPC conversational AI plan

_(Consolidated 2026-05-03 from `docs/npc-conversational-ai-plan.md`. Original file deleted; this section is the canonical copy.)_

# NPC Conversational AI — STT + LLM + TTS Plan

User-flagged 2026-05-02:
- "the audio agent could also work on the tts from the npcs"
- "we will also need to come up with a lot of voices for the tts for the npcs"
- "we will need to work on speech to text as well so the npcs can 'hear' the players"
- "aka the llm"

So the full loop is:

```
Player mic
  → STT (transcribe player speech to text)
  → LLM (NPC identity + transcript + context → dialogue response)
  → TTS (NPC voice id + response text → audio clip)
  → Audio playback (positional, NPC-anchored)
```

This is a five-piece system. Each piece can be a separate agent task
because they communicate through narrow seams (transcript text,
response text, voice id, audio path).

## Architecture sketch

A new `Karma.Voice` namespace under `scripts/Voice/` holds the runtime.

```
scripts/Voice/
  PlayerMicCapture.cs          // capture push-to-talk audio (Godot 4 microphone API)
  SpeechRecognizer.cs          // STT seam — Whisper.cpp local, OpenAI Whisper API, or stub
  NpcDialogueLLM.cs            // LLM seam — Anthropic API or local llama or stub
  NpcVoiceSynthesizer.cs       // TTS seam — Piper local, ElevenLabs API, or stub
  VoiceCatalog.cs              // many-voices registry, NPC → voice id mapping
  ConversationOrchestrator.cs  // wires the above into a single push-to-talk flow
```

Each seam ships with a **stub backend** that returns deterministic
placeholder data so the loop can be developed end-to-end before any
external service is wired in. This keeps the prototype offline-friendly
and tests deterministic.

## Stage 1 — Speech-to-Text (STT)

**Trigger:** push-to-talk hold key (default `V`). Capture starts on
press, stops on release, transcribes on release.

**Options:**
- **Whisper.cpp** (offline, MIT license) — best for privacy + offline
  builds. ~250 MB for the small model; runs on CPU. Native binding via
  Godot's `OS.Execute` calling a `whisper-cli` binary, or a C# wrapper.
- **OpenAI Whisper API** — high quality, online, ~$0.006/min.
  Requires an `OPENAI_API_KEY` env var; the audio agent should keep
  the API call in a single class so it's easy to swap.
- **Stub** — fixed-string mapping from clip duration buckets to canned
  test phrases. Used in unit tests + when no backend configured.

**Recommended default:** stub for the prototype, with the
Whisper.cpp path stubbed-out behind a feature flag in the same shape
so the local-offline mode can be turned on later.

## Stage 2 — LLM dialogue

**Inputs to the prompt:**
- NPC identity from `ThemeData.NpcRoster[npcId]` (name, role,
  faction, alignment, personality, secret, likes, dislikes).
- Relationship context (gossip targets, intensity).
- Recent server events the NPC witnessed (witness propagation
  already records this).
- Player's transcribed message.
- A short history of the conversation so far (last 3 turns).

**System prompt template:**
```
You are {npc.name}, a {npc.role} in the {theme.display_name}
setting. {npc.personality}. You {npc.likes} and you don't trust
{npc.dislikes}. Recent events: {witness_summary}. Stay in character.
Reply in 1-3 short sentences. Don't break the fourth wall.
```

**Options:**
- **Anthropic API (Claude)** — recommended. The codebase has the
  `userEmail` set, suggesting a developer with API access. Use
  `claude-haiku-4-5-20251001` for cost; fall back to Sonnet if
  responses feel flat. Keep request body in one helper.
- **OpenAI API** — fine alternative; same shape.
- **Local llama** (via llama.cpp + a small model like Qwen 2.5
  3B-Instruct quantized) — offline, free, slower. Useful for soak
  testing without burning API budget.
- **Stub** — pulls from `theme.json` `gossip_templates` /
  `greetings_pool` and rotates them deterministically. Used in tests
  + when no API key is configured.

**Cost guard:** cap LLM calls per minute per player; cache responses
keyed by `(npc_id, player_intent_summary)`. Prototype budget: 50¢ /
session if Anthropic.

## Stage 3 — Text-to-Speech (TTS)

**Engine options:**
- **Piper** (offline, MIT license, CC0 voices) — recommended default.
  Dozens of voices in many accents; ~25 MB per voice. Runs as a
  subprocess: `piper --model en_GB-alba-medium.onnx --output_file
  out.wav < text`. Voice list at
  <https://github.com/rhasspy/piper/blob/master/VOICES.md>.
- **Coqui TTS** — also offline; deprecated upstream but still works.
- **ElevenLabs API** — best quality, ~$0.18/1k chars; hundreds of
  voices including custom. Online only.
- **Azure / Google Cloud TTS** — similar shape.
- **System TTS** (Windows SAPI / macOS speech) — terrible quality,
  but free + always available. Useful as a stub.
- **Stub** — silent .wav of the right duration for transcript length;
  tests assert the file exists, not its content.

**Recommended default:** Piper offline + a curated set of ~30 voices
covering adult male / adult female / older male / older female /
gruff / soft / accented variants.

## Stage 4 — Voice catalog (many voices)

Each NPC needs a stable voice. Mirror the LPC bundle pick pattern:

```csharp
// New: ThemeData.PickVoiceId(string worldId, string npcId)
// Returns a deterministic voice id from npc.voice_options[] (new
// theme.json field), or falls back to a faction default.
```

**theme.json schema additions:**
```json
"voice_pools": {
  "law":      ["en_GB-alba-medium",  "en_US-ryan-high"],
  "outlaw":   ["en_US-libritts_r-medium-23",  "en_US-amy-low"],
  "chapel":   ["en_GB-aru-medium",  "en_GB-northern_english_male-medium"],
  "wayfarer": ["en_US-arctic-medium",  "en_US-l2arctic-medium"]
}
```

Each NPC's voice pool is its `RoleTags`; pick one deterministically
via `HashCode.Combine(worldId, npcId)`.

**Variety target:** ~30 distinct voices across the medieval roster of
60 NPCs. Some NPCs may share a voice — that's fine for a prototype.

**Voice-curation work for the audio agent:**
1. Download Piper voices (CC0 / public domain — check each model card).
2. Audition each voice with a stock medieval line (e.g.
   "By the king's order, mind the gate"). Keep voices that feel
   medieval-appropriate.
3. Commit voices under
   `assets/audio/voice/piper/<voice_id>.onnx` (+ `.json` config).
4. Populate `voice_pools` in
   `assets/themes/medieval/theme.json` (NEW — main session keeps the
   conflict zone updated; agent 2 must coordinate before editing).
5. Stretch: record/source extra distinctive voices for the named NPCs
   that should sound unique (Headmaster Braydon, Captain Wace, etc.).

## Stage 5 — Conversation orchestrator

A `ConversationOrchestrator` Node attached to the gameplay scene
listens for push-to-talk + the active dialogue NPC. On each
end-of-utterance:

1. Get `PlayerMicCapture.LastClipPath`.
2. Call `SpeechRecognizer.Transcribe(clip)` → text.
3. Call `NpcDialogueLLM.GenerateReply(npcId, text, history)` → text.
4. Call `NpcVoiceSynthesizer.Synthesize(voiceId, text)` → wav path.
5. Play through `PositionalAudioPlayer` anchored at NPC position.

Latency target: <2s end-to-end with stubs; <5s with full
local-offline backends; <3s with API backends.

## Privacy + opt-in

- **Push-to-talk only.** No always-on mic. Always-on is a non-starter
  for a multiplayer game.
- **Local-first when possible.** Whisper.cpp + Piper means no audio
  leaves the machine.
- **Settings panel toggle** to disable the conversational AI feature
  entirely (falls back to existing typed dialogue).
- Microphone permissions handled per-OS at first use.

## License rules

- Whisper.cpp = MIT.
- Piper = MIT; voices vary (most are public domain or CC0).
- ElevenLabs = proprietary; stock voices are licensed for game audio
  but check each one.
- Anthropic / OpenAI = service ToS; no licensing concern for output.
- Same audit hygiene: track per-voice license in
  `assets/audio/voice/CREDITS.md`.

## Out of scope (for now)

- Multi-NPC conversations (overlapping speakers).
- Voice cloning.
- Realtime streaming TTS (clip-by-clip is fine for the prototype).
- Spatial 3D-audio reverb processing.


---

## Local chat prototype

_(Consolidated 2026-05-03 from `docs/local-chat-prototype.md`. Original file deleted; this section is the canonical copy.)_

# Local Chat Prototype

This is the first implementation step toward player-to-player proximity voice.
It intentionally starts with server-owned local text chat and an audibility model,
without microphone capture or VOIP networking yet.

## What exists now

- New server intent: `SendLocalChat`.
- Payload:
  - `text` — local message text.
- The authoritative server sanitizes whitespace, truncates messages to
  `AuthoritativeWorldServer.LocalChatMaxMessageLength`, stores the message, and
  emits a `local_chat` server event.
- Interest snapshots now include `LocalChatMessages`, filtered by listener
  distance.
- Each local chat snapshot includes:
  - message id/tick,
  - speaker id/name,
  - text,
  - speaker tile position,
  - listener distance in tiles,
  - normalized volume.
- The HUD shows the latest audible local chat line.
- Press `/` or `T` in gameplay to open the local chat entry; press Enter to send
  or Esc to cancel.
- Recent audible messages render as lightweight world-space chat bubbles above
  speakers for a short prototype window.
- The developer overlay Events page lists recent audible local chat with distance
  and volume.

## Current falloff model

Prototype constants:

- Full volume through `LocalChatClearRadiusTiles = 4`.
- Smooth fade until `LocalChatMaxRadiusTiles = 18`.
- Inaudible at or beyond max radius.

This matches the future proximity voice behavior: text first, then voice can reuse
the same server-approved audibility metadata.

## Next steps

1. Add `Local`, `Posse`, and `System` chat tabs or filters.
2. Decide server-side message expiry/pruning instead of keeping the prototype log
   for the whole session.
3. Improve bubble styling with a proper panel/tail and text-safe contrast.
4. Feed the same falloff/audibility model into a fake audio-source prototype.
5. Evaluate real VOIP transport after gameplay-side audibility feels right.


---

## World interaction next 15

_(Consolidated 2026-05-03 from `docs/world-interaction-next-15.md`. Original file deleted; this section is the canonical copy.)_

# World Interaction Next 15

This is the next gameplay task set after the native player-v2 art pipeline and local-chat polish. The focus is making the generated world feel more enterable, traversable, and socially useful without abandoning the server-authoritative prototype architecture.

## Priority order

1. **Building entry foundation** — add a reusable server-owned `enter`/`exit` interaction for structures/station markers so the game can represent being inside a building before real interiors exist.
2. **Entry HUD prompts** — expose entry/exit affordances in nearby structure prompts without breaking inspect/repair/sabotage.
3. **Entered-building status** — carry a lightweight player status such as `Inside: Greenhouse` in interest snapshots so UI/debug/server tests can see entry state.
4. **Interior design contract** — document how real interiors should map to structures: interior id, doorway/exit tile, privacy/audibility behavior, NPC/shop hooks, and snapshot filtering.
5. **Generated station interior hooks** — let generated station markers declare future interior types from station role/theme (`clinic`, `market`, `workshop`, `shrine`, etc.).
6. **Door/threshold visuals** — add placeholder door/threshold markers for enterable buildings using existing structure/prop art.
7. **Vehicle/mount design contract** — document reusable mount/vehicle data: speed, capacity, access rules, karma consequences, storage, and server movement authority.
8. **Prototype rideable mount entity** — add a server-visible mount/vehicle entity model, initially non-rendered or placeholder-rendered.
9. **Mount/dismount intents** — add server-owned mount/dismount actions with range checks and occupancy rules.
10. **Mounted movement modifier** — apply a modest movement/sprint modifier while mounted, with stamina/fatigue implications later.
11. **Vehicle parking/recall rule** — decide whether vehicles stay in-world, return to stations, or despawn safely after match end.
12. **Cargo/storage loop** — let vehicles or mounts hold a small item inventory for station-delivery and rescue loops.
13. **Karma hooks for transport** — add helpful/harmful consequences for giving rides, stealing mounts, abandoning passengers, or rescuing downed players by vehicle.
14. **Local chat/interior audibility** — decide how building entry and vehicles affect local chat bubbles/falloff.
15. **First integrated slice** — combine enterable building + one generated station interior hook + one transport design stub into a verified commit before deeper art.

## Current recommended slice

Tasks 1-3 are implemented as the building entry placeholder. Tasks 4-5 now have their first pass: `docs/interior-design-contract.md` defines the interior identity/snapshot/audibility/NPC-shop contract, and generated station locations declare `InteriorId`/`InteriorKind` hooks that station prompts expose.

Next recommended slice: task 6, add simple placeholder door/threshold visuals for enterable structures and station markers so the new interior hooks become visible in the world.


---

## Prototype progress and roadmap

_(Consolidated 2026-05-03 from `docs/prototype-progress-and-roadmap.md`. Original file deleted; this section is the canonical copy.)_

# Karma Prototype Progress and Roadmap

This document tracks what the prototype can already do, what needs to be better,
and the next practical build slices.

## What we have done

### Main menu and entry flow

- The project now boots into a separate `MainMenu.tscn` prototype instead of directly into the gameplay sandbox, while `tools/run-gameplay.ps1` launches gameplay directly for fast iteration.
- The menu has Start Local Prototype, Options, Credits, and Quit controls.
- Start Local Prototype loads the existing gameplay prototype scene (`Main.tscn`) without folding menu UI into the world prototype.
- Options now includes prototype video settings (resolution list, display-resolution detection, fullscreen/windowed, VSync), audio sliders, controls/accessibility notes, and apply/save behavior.
- The main menu has an original generated placeholder theme loop, with master/music sliders affecting menu music volume.
- Gameplay now has a non-pausing Escape menu overlay with Resume, Options, Main Menu, and Quit actions; the options panel is a placeholder ready to reuse the main settings model.
- The normal gameplay HUD is cleaner by default; verbose relationship/faction/quest/combat/sync/perf details move behind a tilde (`~`) developer overlay.
- The tilde developer overlay shows local player details, nearby players, nearby NPCs, snapshot counts, performance, map chunks, items, structures, and event counts, split into Tab/Shift+Tab pages so it is not a wall of text.
- Gameplay HUD now includes a simple perf line showing FPS, local snapshot refresh rate, and visible map chunk count for prototype stutter diagnosis.

### Server-owned karma loop

- Saint/Scourge match mode has clearer match-end UI and locked results.
- Karma Break/death resets path status and now clears temporary team/posse status.
- Karma Break drops carry owner id/name through the server, snapshots, prompts, and HUD.
- Claiming someone else's Karma Break drop Descends; returning that specific claimed drop to its owner Ascends.
- Scrip transfer is explicit: `gift` Ascends and moves money actor -> target; `steal` Descends and moves money target -> actor.
- Structure integrity exists as a reusable loop: inspect, repair, sabotage, repair bounty, and faction reputation.
- Rumorcraft is a real Descension perk: exposed entanglements become global rumors.

### Match start and respawn foundation

- Initial match spawns are now server-owned, random per player/world, edge-padded, and separated when possible.
- There are no starting teams.
- Temporary teams/posses can be added during play, but death/Karma Break clears them.
- Respawns are now context-aware: Karma Break/death uses separated candidate placement to avoid the death location, nearby players, map edges, and immediate re-entry into the same pile-up.

### World and NPC generation

- World generation now starts from **social stations** instead of decorative locations.
- Stations include clinics, markets, repair yards, rumor boards, saloons, restricted sheds, oddity yards, duel rings, farm plots, black markets, apology engines, broadcast towers, war memorials, and witness courts.
- Each station carries a role, theme tag, karma hook, and suggested faction.
- NPCs derive from stations, giving them roles, needs, secrets, likes/dislikes, factions, and station placements.
- Generated station locations are seeded as inspectable server structure markers, so their roles and karma hooks are visible in snapshots/rendering.
- Each generated station now also gets a repairable/sabotageable fixture tied to its gameplay hook and suggested faction; repair/sabotage reputation now targets that station faction instead of always using the Civic Repair Guild, and the linked station marker state changes to stabilized/compromised.
- Stabilized/compromised station state now feeds back into generated NPC dialogue and generated quest scrip rewards.
- Context-aware Karma Break respawns now prefer safe stabilized station markers before falling back to blue-noise placement.
- Generated NPC placements are seeded into the authoritative server world and show up through interest snapshots/rendering.
- Generated NPCs now provide station-specific dialogue choices and station-driven quests derived from local needs and karma hooks.
- Oddities now have generated placements with local gameplay reasons tied to nearby stations.
- Generated oddities are seeded into the authoritative server world as pickup items and show up through interest snapshots.
- A reusable deterministic best-candidate / blue-noise-style placement sampler spaces stations and oddities more naturally.

### Art and animation pipeline

- The prototype player now has animated movement rather than a static sprite.
- Runtime supports 8-direction character animation names and fallback to 4-direction sheets.
- Current generated/extracted 8-direction engineer sheet is active, with a transparent runtime PNG pipeline.
- Art curation docs/tools exist for generated sheets, theme packs, audits, current prototype model prompts, and future base-body + outfit/skin layering.
- Research notes now point toward a professional paper-doll/layered character standard inspired by LPC/RapidLPC/Godot LPC patterns, while avoiding direct art imports unless licensing is deliberately accepted.
- Medieval LPC NPC generation now produces a much larger variant set from the local LPC library, with non-theme assets filtered out and body/hair/beard tint support materialized into generated previews/runtime sheets.
- Player LPC rendering now composes equipped items over the base character atlas at runtime, so equipped weapons/tools, body armor/shields, and backpacks can show on local and remote player sprites.

### Audio and interaction feedback

- The prototype now has real SFX files under `assets/audio/sfx/` and the audio catalog resolves interaction, combat, movement, equipment, dialogue, quest, shop, mount, and item-use cues.
- Authoritative server events carry `audioCue` data for most gameplay interactions, and the HUD plays the latest event cue from interest snapshots.
- Door-open events also have a positional playback path in the world renderer.
- Current cue coverage is broad, but many medieval sounds intentionally reuse closest-fit prototype one-shots until bespoke clips are sourced.

### Reusable code research

- Researched permissive procedural-generation sources on GitHub.
- Current posture: adapt ideas and algorithms, avoid importing assets or whole frameworks unless intentionally chosen.
- Added `docs/reusable-procgen-research.md` and third-party notices.

## What needs to be better

### Gameplay integration

- Main menu, HUD, developer overlay, and Escape menu visuals/settings are still prototype UI and need final styling, broader audio bus wiring, shared options persistence, full control remapping/accessibility settings, searchable/filterable debug views, and eventual multiplayer/session entry flows.
- Character art still needs a professional v2 standard: likely `48x48` or `64x64`, true 8-direction, layered paper-doll sheets, animation-group manifests, and a compositor/export pipeline.
- Downed/rescue/carry/execute mechanics are now documented as a future core karma loop that also informs v2 character animations.
- Proximity communication research now prioritizes player-to-player proximity voice/text first, with NPC speech-to-text/LLM/text-to-speech interactions parked as research/to-do.
- Server-owned local text chat now exists as the first proximity communication slice: `SendLocalChat` messages are filtered by listener distance, carry distance/volume falloff in interest snapshots, can be sent from gameplay with `/` or `T`, and render in the HUD/developer overlay plus short-lived world chat bubbles.
- Gemini-generated player v2 references now have an extraction pass: direction-specific front/right/back pose batches are chroma-keyed into transparent `64x64` candidate frames and composited into `assets/art/sprites/generated/player_v2_engineer_8dir_4row_candidate.png`. Sean chose this `64x64` scale/style as the v2 visual target because the procedural `32x32` mannequin looked worse in-game. The player catalog now prefers the `64x64` candidate as the default visual preview when present, while the `32x32` layered stack remains as the architecture/fallback until true `64x64` paper-doll layers exist.
- The reusable player v2 architecture remains layered instead of one-off character generation: `tools/generate_layered_player_v2.gd` creates base body, multiple skin tones, hair variants, outfit variants, and tool layers under `assets/art/sprites/player_v2/layers/`, writes `player_v2_manifest.json`, composites the default stack into `player_v2_layered_preview_8dir.png`, and runtime code can load the manifest, build default/custom appearance slot selections, compose selected layers into an image, export deterministic cached composite PNGs, carry player appearance selections through player state/snapshots, accept server-owned `SetAppearance` intents, and point local/prototype-peer character sprites at selected composite atlas overrides. The Escape menu includes a prototype Appearance panel for server-owned skin/hair/outfit cycling, with `V`/`B`/`N` retained as quick debug shortcuts; non-default appearance cycling still uses the temporary `32x32` layer stack until the `64x64` layer set is rebuilt.
- NPC voice research direction is to keep current walk-up options, then later make interactions feel more organic with contextual greetings/exclamations, optional spoken/freeform player input, bounded LLM responses, subtitles, and spatial NPC voice playback.
- Generated station locations have inspectable markers and interactable fixtures, but their art is still placeholder greenhouse components and needs proper sign/landmark visuals.
- Generated NPCs have first-pass station dialogue/quests, but those choices are still broad templates rather than bespoke quest chains.
- Generated oddities are server-seeded, but pickup placement needs more visual/station context and balancing.
- Generated NPC needs/secrets should feed real quests, dialogue choices, rumors, bounties, and faction consequences.
- Structure repair/sabotage should expand beyond the starter greenhouse into generated station-specific objects.

### Respawns

- Movement now avoids snapping back to server tile positions when local client prediction already explains the authoritative tile update, but we still need more playtesting for camera/render hitching.
- Respawn now avoids death locations and nearby players, but should get richer candidate pools:
  - avoid active combat areas more explicitly;
  - prefer safe-ish stations or neutral landmarks;
  - add cooldown/heat logic to prevent abuse such as instant return to a fight.

### Perks and social systems

- Several perks are still catalog/future-facing and need concrete mechanics.
- Saint/Scourge standing should affect more NPC/faction behavior.
- Temporary teams/posses need explicit creation, invitation, UI, and expiry rules.
- NPC relationships and faction reputation should react to generated station events.

### World feel

- Tile generation is still simple and rectangular around starter areas.
- Station placement is better spaced, but tiles/roads/landmarks should be shaped around those stations.
- The world needs paths, districts, danger zones, resource pockets, and visual identity per theme.

### Art

- Current player art is pivoting from the ugly `32x32` procedural mannequin to the better-reading `64x64` Gemini candidate scale/style. The old `32x32` layer stack should be treated as architecture/fallback, not the visual target.
- Long-term pipeline should move toward blank/base bodies plus outfit/skin layers.
- Legacy art library still needs migration into the newer curation structure.

## What we need to do next

1. **Deepen station quests/dialogue.** Turn broad generated choices into multi-step repair, rumor, theft, apology, bounty, delivery, and mediation tasks.
2. **Prototype player communication.** Add chat tabs/log polish and server-side chat expiry, then use the audibility/falloff model to prototype fake audio before real player-to-player proximity voice.
3. **Upgrade player art pipeline.** Rebuild the layered base-body/skin/hair/outfit/tool system at the chosen `64x64` v2 scale/style, using the current `64x64` candidate as a visual/proportion target and the old `32x32` layer stack only as architecture/fallback.
4. **Improve generated structure consequences.** Tune faction-specific rewards/penalties by station role and let stabilized/compromised station state affect local prices and richer quest branches.
5. **Improve respawn candidate pools.** Prefer safe stations/landmarks and avoid active combat heat, not just death/player positions.
6. **Improve station presentation.** Replace placeholder station marker art with signs, landmarks, or theme-specific props.
7. **Wire one more perk.** Good candidates: `Paragon Favor`, `Abyssal Mark`, or deeper `Renegade Nerve` intimidation behavior.
8. **Improve world layout.** Generate roads/paths between stations and shape districts around the station graph.
9. **Balance generated oddities.** Tune station proximity, rarity, and item selection so pickups support interesting choices instead of noise.
10. **Keep documentation current.** Update this file after each meaningful gameplay slice.

## Verification standard

For code changes, prefer:

```powershell
'/mnt/c/Program Files/dotnet/dotnet.exe' build Karma.csproj
'/mnt/c/Users/pharr/Downloads/Godot_v4.6.2-stable_mono_win64/Godot_v4.6.2-stable_mono_win64/Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path "C:\Users\pharr\code\karma" "res://scenes/TestHarness.tscn"
```

Push from WSL through Windows PowerShell credentials:

```bash
/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe -NoProfile -Command "Set-Location 'C:\Users\pharr\code\karma'; git push origin develop"
```


---

## Recent work 2026-05-03

_(Consolidated 2026-05-03 from `docs/recent-work-2026-05-03.md`. Original file deleted; this section is the canonical copy.)_

# Recent Work Log - Medieval Prototype Pass

Date: 2026-05-03

This note records the recent implementation pass so future agents can pick up
without re-discovering the same context.

## Medieval NPC Generation

- Reworked `tools/lpc_generate_medieval_bundles.py` to regenerate medieval LPC
  NPC theme bundles with much wider layer variety.
- The generator now scans the local LPC library for usable medieval layers,
  filters out non-theme assets such as sci-fi/glow weapons, and emits many
  deterministic variants per role.
- Current generated target is 24 variants per base role, producing 1,368
  medieval NPC bundle JSONs and matching generated preview/runtime PNGs.
- Added tint support for body, hair, and beard layers, then updated
  `tools/lpc_materialize_theme_bundles.gd` so generated bundles can apply those
  tints while materializing.
- `assets/art/sprites/lpc/themes/README.md` now describes the generator,
  blocklist, tint data, and re-run flow.

## Player LPC Equipment Rendering

- Added `scripts/Art/LpcPlayerEquipmentComposer.cs`.
- Player and peer sprites now build a cached equipped LPC atlas from:
  - the player LPC base bundle,
  - equipped main-hand weapon/tool,
  - equipped body armor/shield,
  - equipped backpack.
- `PrototypeCharacterSprite`, `WorldRoot`, `PlayerController`, and
  `PeerStandInController` now refresh the rendered sprite when equipment
  changes, not only when the base appearance changes.
- Smoke tests cover equipment signatures, equipped atlas generation, and the
  no-equipment fallback to the base atlas.
- Important limitation: this is equipment-driven, not loose-inventory driven.
  Items become visible when equipped/held by gameplay state.

## Interaction Audio Wiring

- Expanded `scripts/Audio/AudioEventCatalog.cs` from a small set of generic
  cues into a wider event map.
- Every equippable item now has a stable item-specific cue id such as
  `item_equipped_practice_stick`. Most cues currently reuse the nearest
  existing SFX, so bespoke medieval replacements can be swapped in later by
  changing only the catalog path.
- `AuthoritativeWorldServer` now adds `audioCue` data to key interaction
  events:
  - movement footsteps,
  - pickups and supply claims,
  - structure interactions, scavenging, restroom use,
  - shop purchases, sales, currency and item transfers,
  - craft, place, repair, reload,
  - heal/food/drug item use,
  - dialogue open/advance/choice/close,
  - quest start/advance/complete,
  - mount, dismount, and mount-bag transfer,
  - bounty / wanted bounty / combat hit feedback.
- `HudController` now resolves the latest snapshot event through `audioCue`
  first, then falls back to the event id itself. Karma Break and contraband keep
  their existing dedicated flash/stinger paths.
- Existing positional door audio remains in `WorldRoot`.

## Test and Harness Fixes

- Fixed carry-state preference saving by closing the Godot `FileAccess` writer
  before immediate reload.
- Added Dallen's missing `sell_items` authored dialogue-tree choice.
- Made smoke tests less brittle around current shop price modifiers and posse
  spawn/sequence assumptions.
- Added audio tests that verify every registered catalog clip exists and every
  equippable resolves to an existing equipment cue.

## Verification

Known-good verification from this pass:

```bash
'/mnt/c/Program Files/dotnet/dotnet.exe' build Karma.csproj
'/mnt/c/Users/pharr/Downloads/Godot_v4.6.2-stable_mono_win64/Godot_v4.6.2-stable_mono_win64/Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path 'C:\Users\pharr\code\karma' 'res://scenes/TestHarness.tscn'
```

Results:

- `dotnet build` passes.
- Godot headless smoke harness reports `Gameplay smoke tests passed.`
- The build still reports the existing nullable annotation warnings in
  `ProceduralPlacementSampler.cs` and `ServerIntent.cs`.

## Follow-Ups

- Replace reused placeholder SFX with bespoke medieval clips per cue id.
- Add more precise positional playback for non-door world events if/when the
  listener-side snapshot gives enough spatial context.
- Consider visible held variants for non-equippable quest/consumable items if
  gameplay wants "found but not equipped" items to show on the player.
- Keep generated NPC `.import` files out of source control; Godot can recreate
  them on import.


---

## Reusable procgen research

_(Consolidated 2026-05-03 from `docs/reusable-procgen-research.md`. Original file deleted; this section is the canonical copy.)_

# Reusable Procedural Generation Research

Karma should reuse permissively licensed ideas where they fit, while keeping the
runtime generator server-owned, deterministic, and native to the Godot .NET/C#
prototype.

## Current candidates

### SirNeirda/godot_procedural_infinite_world

- URL: <https://github.com/SirNeirda/godot_procedural_infinite_world>
- License: MIT
- Fit: Godot 4 C# procedural world example.
- Useful ideas: deterministic object spreading, environment/weather/day-night
  knobs, chunk-friendly world management.
- Current use: research/reference only; no source files or assets imported.

### gaea-godot/gaea

- URL: <https://github.com/gaea-godot/gaea>
- License: MIT
- Fit: Godot 4 procedural generation addon.
- Useful ideas: graph/node-based generation architecture and renderer separation.
- Current use: research/reference only; no source files or assets imported.

### gdquest-demos/godot-procedural-generation

- URL: <https://github.com/gdquest-demos/godot-procedural-generation>
- License: source code MIT; assets CC-BY 4.0.
- Fit: educational procedural generation algorithms for Godot.
- Useful ideas: random walkers, cellular automata, world maps, blue-noise style
  distribution, chunked/infinite placement demos.
- Current use: algorithm inspiration only; no source files or assets imported.

### LayerProcGen

- URL: <https://github.com/runevision/LayerProcGen>
- License: MPL-2.0.
- Fit: deterministic contextual layered C# generation.
- Useful ideas: layered dependency model and cross-chunk context.
- Current use: ideas only. Avoid direct import unless MPL obligations are
  intentionally accepted.

## First reuse direction

The first adapted idea is best-candidate / blue-noise-style placement. Karma uses
`ProceduralPlacementSampler` as a small project-native C# sampler for naturally
spaced social stations, future oddities, structures, NPC spawn hubs, and respawn
candidate pools.

This keeps the good part of reusable procedural generation — separated placement
that feels less random and less clumpy — without importing a full third-party
framework.

