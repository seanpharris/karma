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
