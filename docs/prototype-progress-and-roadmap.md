# Karma Prototype Progress and Roadmap

This document tracks what the prototype can already do, what needs to be better,
and the next practical build slices.

## What we have done

### Main menu and entry flow

- The project now boots into a separate `MainMenu.tscn` prototype instead of directly into the gameplay sandbox.
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
- Art curation docs/tools exist for generated sheets, theme packs, audits, and future base-body + outfit/skin layering.

### Reusable code research

- Researched permissive procedural-generation sources on GitHub.
- Current posture: adapt ideas and algorithms, avoid importing assets or whole frameworks unless intentionally chosen.
- Added `docs/reusable-procgen-research.md` and third-party notices.

## What needs to be better

### Gameplay integration

- Main menu, HUD, developer overlay, and Escape menu visuals/settings are still prototype UI and need final styling, broader audio bus wiring, shared options persistence, full control remapping/accessibility settings, searchable/filterable debug views, and eventual multiplayer/session entry flows.
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

- Current player art is good enough for prototype use, but still generated/extracted and should be revisited later.
- Long-term pipeline should move toward blank/base bodies plus outfit/skin layers.
- Legacy art library still needs migration into the newer curation structure.

## What we need to do next

1. **Deepen station quests/dialogue.** Turn broad generated choices into multi-step repair, rumor, theft, apology, bounty, delivery, and mediation tasks.
2. **Improve generated structure consequences.** Tune faction-specific rewards/penalties by station role and let stabilized/compromised station state affect local prices and richer quest branches.
3. **Improve respawn candidate pools.** Prefer safe stations/landmarks and avoid active combat heat, not just death/player positions.
5. **Improve station presentation.** Replace placeholder station marker art with signs, landmarks, or theme-specific props.
6. **Wire one more perk.** Good candidates: `Paragon Favor`, `Abyssal Mark`, or deeper `Renegade Nerve` intimidation behavior.
7. **Improve world layout.** Generate roads/paths between stations and shape districts around the station graph.
8. **Balance generated oddities.** Tune station proximity, rarity, and item selection so pickups support interesting choices instead of noise.
9. **Keep documentation current.** Update this file after each meaningful gameplay slice.

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
