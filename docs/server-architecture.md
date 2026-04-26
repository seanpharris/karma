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
- Route future transports through explicit network message envelopes for join,
  intent, snapshot request, ping, and response/error messages. The current
  in-process protocol adapter uses those envelopes before any socket layer is
  introduced.
- Keep NPC simulation tiered: active nearby NPCs update often, distant NPCs
  update in coarse batches.
- Keep LLM generation out of the live tick loop.
- Process player input as intent with sequence numbers.
- Keep match time server-owned. Interest snapshots include match status so
  clients can render the timer/winners without computing authority locally.
- Include current Saint/Scourge leaders in running match snapshots, then locked
  Saint/Scourge winners after finish.
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
- Validate item use intents on the server: known item id, equippable slot,
  inventory/equipment mutation, and syncable equipment event.
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
  item consumption, completion karma mutation, and syncable quest event.
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
