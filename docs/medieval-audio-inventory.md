# Medieval Audio Inventory & Sourcing Plan

Companion to [`SOUND_NEEDED.md`](../SOUND_NEEDED.md). That document is the
canonical cue list (tone target = sci-fi/western) — this one re-skins it
for the medieval theme that
[`WorldConfig.CreatePrototype`](../scripts/World/WorldConfig.cs) now ships
as the default. New audio for medieval mode should be wired here first;
the original sci-fi/western set can stay registered for future themes.

## Current state (2026-05-02)

- `assets/audio/music/main_menu_theme_placeholder.wav` — only audio file
  on disk. Original generated placeholder; safe to keep or replace.
- `scripts/Audio/AudioEventCatalog.cs` — registers 10 built-in event-id →
  `res://assets/audio/sfx/*.ogg` paths. **None of those .ogg files exist
  yet.** Catalog is the seam; nothing plays sound.
- `scripts/Audio/PrototypeMusicPlayer.cs` — generates 3 themes
  procedurally (`SandboxCalm`, `EventTension`, `ScenarioAmbient`) via
  `AudioStreamGenerator`. No sample assets needed to play, but the
  procedural output is bland and not theme-flavoured. The medieval
  `theme.json → music` block already binds these three theme names.

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

Targets are the 10 event ids already registered in
[`AudioEventCatalog.BuiltInClips`](../scripts/Audio/AudioEventCatalog.cs).
Each needs a real `.ogg` (or `.wav`) at the registered path. Re-skinned
for medieval flavour:

| Event id | Path | Medieval cue |
|----------|------|--------------|
| `karma_break` | `assets/audio/sfx/karma_break_stinger.ogg` | Cracked bell + dropped chains |
| `contraband_detected` | `assets/audio/sfx/contraband_alarm.ogg` | Watchtower bell, urgent |
| `purchase_complete` | `assets/audio/sfx/purchase_chime.ogg` | Coin clink + small bell |
| `weapon_reloaded` | `assets/audio/sfx/reload_click.ogg` | Crossbow draw + click (or arrow nock) |
| `supply_drop_spawned` | `assets/audio/sfx/supply_drop_horn.ogg` | Town crier horn |
| `door_opened` | `assets/audio/sfx/door_open.ogg` | Heavy wood door creak |
| `structure_interacted` | `assets/audio/sfx/interact_pop.ogg` | Latch click |
| `clinic_revive` | `assets/audio/sfx/clinic_revive_chime.ogg` | Bowl bell + soft choir swell |
| `player_attacked` | `assets/audio/sfx/hit_thud.ogg` | Sword on shield (or fist on leather) |
| `wanted_bounty_claimed` | `assets/audio/sfx/bounty_paid.ogg` | Coin pour into chest |

Stretch (nice-to-have, not gating): footstep variants on cobblestone /
dirt / wood; ambient market chatter loop; distant chapel bell ring.

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
