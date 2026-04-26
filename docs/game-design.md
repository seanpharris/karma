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

## First Game Mode: Match

The first shipped game type should be a timed match. Players join a generated
world and compete for 30 minutes. At the end of the timer, the current Saint
highest karma and current Scourge lowest karma are both match winners.

This creates two viable races in the same server: Ascend hard enough to become
the Saint, or Descend hard enough to become the Scourge. Karma Breaks still
matter because death resets a player's path status during the match.

Scrip is the prototype currency. It is separate from karma: karma is the
Ascend/Descend match score and social identity, while scrip is spendable money
for tools, cosmetics, services, bribes, and player trades.
The first prototype shop is Dallen's stall, which sells starter objects through
server-validated offers.

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
change karma text.

A Karma Break drops loose inventory into the world as recoverable objects.
Players keep their body and respawn, but death can scatter props, gifts, stolen
goods, and jokes into the shared space.
Picking up another player's Karma Break drop is allowed, but it is remembered as
claiming someone else's scattered goods and should Descend the picker.

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

## Prototype HUD

The HUD is intentionally debug-forward for now. It shows local karma, inventory,
leaderboard standing, perks, relationships, factions, quests, combat,
entanglements, duels, recent rumors, and the local server interest snapshot.
The sync line includes nearby server-approved dialogue choices and visible quest
state so we can confirm the client is rendering from authoritative state instead
of trusting scene-only assumptions.
