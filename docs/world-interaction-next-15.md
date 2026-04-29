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
