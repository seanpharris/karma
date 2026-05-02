# Audio Credits

One row per shipped audio clip. Pixabay, OpenGameArt, Freesound, etc.
entries record the source URL + author + license fields per
[`docs/medieval-audio-inventory.md`](../../docs/medieval-audio-inventory.md).

## Procedurally generated SFX

All clips below are synthesised from scratch by
[`tools/generate_sfx.ps1`](../../tools/generate_sfx.ps1) (basic oscillator
+ filter + envelope DSP, no sampled material). They ship under the
project license — no third-party attribution needed. Re-run the script
to regenerate.

| File | Cue | Description |
|------|-----|-------------|
| `sfx/footstep_dirt.wav` | `footstep_dirt` | Filtered noise burst, soft |
| `sfx/footstep_dirt_b.wav` | `footstep_dirt` (variant) | Alternate seed for stride variety |
| `sfx/footstep_stone.wav` | `footstep_stone` | Bright high-pass noise click |
| `sfx/footstep_stone_b.wav` | `footstep_stone` (variant) | Alternate seed |
| `sfx/footstep_wood.wav` | `footstep_wood` | Mid-band noise + 230 Hz resonance |
| `sfx/footstep_wood_b.wav` | `footstep_wood` (variant) | Alternate seed |
| `sfx/grunt_pain.wav` | `grunt_pain` | Pitched harmonic stack with pitch drop + noise |
| `sfx/grunt_attack.wav` | `grunt_attack` | Shorter, lower exertion grunt |
| `sfx/sword_swing.wav` | `sword_swing` | Band-passed noise whoosh |
| `sfx/sword_hit.wav` | `sword_hit` | Inharmonic metallic partials + impact |
| `sfx/hit_thud.wav` | `player_attacked` | Low-frequency thud + transient |
| `sfx/door_open.wav` | `door_opened` | LFO-modulated noise creak |
| `sfx/interact_pop.wav` | `structure_interacted` | Sharp high-passed click |
| `sfx/karma_break_stinger.wav` | `karma_break` | Detuned cracked bell + chain rattle |
| `sfx/contraband_alarm.wav` | `contraband_detected` | Three watchtower bell strikes |
| `sfx/purchase_chime.wav` | `purchase_complete` | Small bell + coin clinks |
| `sfx/reload_click.wav` | `weapon_reloaded` | Crossbow draw + sharp click |
| `sfx/supply_drop_horn.wav` | `supply_drop_spawned` | Two-note triangle/saw horn fanfare |
| `sfx/clinic_revive_chime.wav` | `clinic_revive` | Bell + soft choir-pad swell |
| `sfx/bounty_paid.wav` | `wanted_bounty_claimed` | Stream of randomly pitched coin clinks |

## Music

| File | Source | Author | License | Attribution string |
|------|--------|--------|---------|--------------------|
| `music/main_menu_theme_placeholder.wav` | (procedural) | Karma project | Project license | none required |
| `music/emmraan-fallen-in-battle-261253.mp3` | TBD — fill in source URL | Emmraan | TBD (Pixabay Content License likely) | TBD |
| `music/kaazoom-the-knight-the-maid-medieval-tavern-song-510193.mp3` | TBD | Kaazoom | TBD (Pixabay Content License likely) | TBD |
| `music/kaazoom-travelling-on-medieval-celtic-rpg-game-music-434717.mp3` | <https://pixabay.com/music/folk-travelling-on-medieval-celtic-rpg-game-music-434717/> | kaazoom | Pixabay Content License | "Travelling On - Medieval Celtic RPG Game Music" by kaazoom |
| `music/music_for_creators-medieval-celtic-violin-244699.mp3` | TBD | Music_For_Creators | TBD (Pixabay Content License likely) | TBD |
| `music/nakaradaalexander-townsong-228672.mp3` | TBD | Alexander Nakarada | TBD (Pixabay Content License likely) | TBD |
| `music/nakaradaalexander-traveler-2023-228688.mp3` | TBD | Alexander Nakarada | TBD (Pixabay Content License likely) | TBD |
| `music/vjgalaxy-traditional-celtic-music-04-481024.mp3` | TBD | VJ_GALAXY | TBD (Pixabay Content License likely) | TBD |

> **Action item for the curator:** the music rows above were dropped in
> from a Pixabay search session — confirm each track's source URL and
> license id before any commercial publish, then replace each `TBD` cell.
