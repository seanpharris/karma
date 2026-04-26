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
6. Extreme karma unlocks perks, status, and social power.
7. Death causes a Karma Break and resets the player's path.

## First Game Mode: Match

The first shipped game type should be a timed match. Players join a generated
world and compete for 30 minutes. At the end of the timer, the current Saint
highest karma and current Scourge lowest karma are both match winners.

This creates two viable races in the same server: Ascend hard enough to become
the Saint, or Descend hard enough to become the Scourge. Karma Breaks still
matter because death resets a player's path status during the match.

Prototype matches stay small, but the production large-world target is
`1000 x 1000` tiles at `16px` logical tile scale. Large worlds must be treated
as streamed/chunked spaces, not fully simulated or rendered to every client.

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
