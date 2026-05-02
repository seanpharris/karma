# Medieval NPC — Identity, Appearance, and Interaction Randomization

How the medieval roster keeps stable identities while randomizing their
look and their dialogue at spawn / encounter time.

## Three layers of randomization

| Layer       | What's stable                                | What randomizes                               | Where it lives                                                 |
|-------------|----------------------------------------------|-----------------------------------------------|----------------------------------------------------------------|
| Identity    | id, name, role, faction, personality, secret | nothing                                       | `npc_roster[]` in `assets/themes/medieval/theme.json`          |
| Appearance  | base bundle + body kind + role               | hair style (variants); future: palette swaps  | `npc.appearance_options[]` → `assets/art/sprites/lpc/themes/`  |
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
set the server may pick from at spawn. The picker should be **deterministic
per (worldId, npcId)** so a given world always shows the same Garrick
hair-style, but a different world rolls a different one.

### How the variants are produced

[`tools/lpc_generate_medieval_bundles.py`](../tools/lpc_generate_medieval_bundles.py)
emits one base bundle per role plus 1-2 hair-style variants:

| Base spec hair       | Variant `_v2` swap       | Variant `_v3` swap       |
|----------------------|---------------------------|---------------------------|
| `hair/short/male`    | `hair/messy/adult`        | `hair/bald/adult`         |
| `hair/short/female`  | `hair/long/adult`         | `hair/messy/adult`        |
| `hair/long/adult`    | `hair/messy/adult`        | `hair/short/male`         |
| `hair/messy/adult`   | `hair/long/adult`         | `hair/short/male`         |
| `hair/bald/adult`    | `hair/short/male`         | (none)                    |

Re-run the generator and the variant map at
[`assets/art/sprites/lpc/themes/_appearance_variants.json`](../assets/art/sprites/lpc/themes/_appearance_variants.json)
updates automatically. Then re-run
[`tools/medieval_extend_theme.py`](../tools/medieval_extend_theme.py)
to push the new variant ids into each NPC's `appearance_options`.

### Future axes

Beyond hair-style swaps, future variant axes (cheap to add — same
generator pattern):

- **Hair palette colour** (26 LPC palette entries — needs the runtime
  recolour pass before this becomes free)
- **Skin tone / body palette** (22 entries)
- **Outfit colour palette** (24 entries)
- **Beard / no beard** for adult-male NPCs
- **Helmet on / off** for warrior types
- **Cloak draped / shouldered** (once LPC capes are filled in for walk)

These can stack — e.g., `blacksmith_male_v2_blonde_palette` is one
"identity, but with the Tuesday hair." Generator can emit the matrix.

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

## Server-side wiring (TODO — task #10 in the parallel-agent queue)

The data is in place. The server needs:

1. **Spawn-time appearance roll.** When `_npcs[id]` is created, pick
   `appearance_options[hash(WorldId, npcId) % len]` and stash it on
   the `NpcEntity`. Surface in `NpcSnapshot` so the client renderer
   can load the right LPC composite.
2. **Greeting injection.** When `ProcessStartDialogue` opens a fresh
   conversation with a tree-bound NPC, prepend a greeting line from
   the matching role pool to the root node text.
3. **Reaction events.** Wire `reactions_pool` to a small set of event
   triggers — at a minimum: player approach, player gift, witnessed
   crime nearby.
4. **Gossip dialogue choice.** Add a `dialogue_advance:gossip` action
   id to the standard NPC dialogue tree; when selected, the server
   resolves a relationship + template + name substitution and emits
   it as the next node's text.

These are pure server wiring — the data + variant assets are ready.

## Re-run procedure

If you edit any of the inputs:

```bash
# 1. Regenerate variant bundles + variant map
python3 tools/lpc_generate_medieval_bundles.py

# 2. Push variant lists + interaction pools + relationships into theme.json
python3 tools/medieval_extend_theme.py
```

Both scripts are idempotent — re-runnable as the spec table grows or as
the relationship graph evolves.
