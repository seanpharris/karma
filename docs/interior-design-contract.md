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
