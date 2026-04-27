# Downed, Carry, Rescue, and Mercy Mechanics

This is a proposed core Karma loop for what happens before full Karma Break/death.
It should create social choices other players can build on: help, rescue, exploit,
or finish someone off.

## Design goal

A downed player should become a temporary social object in the world, not just a
respawn timer. Other players can make visible karma choices around them.

## State model

Suggested player life states:

1. `Healthy`
   - Normal movement/action.
2. `Injured`
   - Low health warning, still mobile.
3. `Downed`
   - Cannot walk normally or attack.
   - Can crawl slowly or call for help later.
   - Has a bleed-out / collapse timer.
   - Keeps inventory but drops an obvious rescue/loot prompt.
4. `Carried`
   - A rescuer/abductor is carrying or dragging the downed player.
   - Carrier moves slower, cannot sprint, and is vulnerable.
   - Carried player can be delivered to a safe station/hospital or abandoned.
5. `Recovered`
   - Revived at low health with short grace/status effect.
6. `KarmaBreak`
   - Existing death/reset flow.

## Player actions around a downed character

### Help up / revive in place

- Requires time, proximity, and optionally a med item or safe environment.
- Interruptible by damage/movement.
- Ascends the helper.
- Restores the downed player to low health.
- Builds relationship/faction reputation if witnessed.

### Carry / drag

- Starts a `Carried` link between carrier and downed player.
- Carrier receives movement penalties.
- The carried player follows carrier position or occupies a linked offset.
- Can be heroic or predatory depending on destination/action.

Possible delivery outcomes:

- **Hospital/clinic/safe station delivery**
  - Strong Ascension reward.
  - Clinic faction reputation up.
  - Downed player recovers better than field revive.
- **Drop in danger / abandon**
  - Small Descension or local reputation loss if witnessed.
- **Deliver to hostile faction / black market / bounty station**
  - Descension or faction-specific reputation gain/loss depending on context.
  - Could support future kidnapping/bounty mechanics.

### Finish / execute

- Converts downed player to Karma Break.
- Strong Descension / karma loss for the killer.
- Potential Scourge standing/reputation effects.
- Creates a visible world event/rumor.
- Should have a clear confirmation/hold input so accidental finish is hard.

### Loot / steal from downed player

- Optional future mechanic.
- Descends unless special lawful/bounty context exists.
- Could interact with Karma Break drop ownership/provenance.

## Karma consequences

These should be server-owned actions, not client labels:

- Field revive: Ascend.
- Carry to clinic/hospital: stronger Ascend + clinic/faction rep.
- Carry to safe ally station: Ascend + local station rep.
- Abandon after starting carry: Descend if witnessed or if abandonment causes break.
- Execute downed player: strong Descend; public rumor/event.
- Steal from downed player: Descend and reputation damage.
- Mercy finish could be a special exception only if a future status explicitly marks
  it as requested/consented/terminal; default execution should be evil.

## Art and animation requirements

This mechanic affects the professional character art standard. The v2 animation
contract should include:

- `downed_idle` — lying/slumped in all 8 directions or a reduced 4-direction set.
- `downed_crawl` — optional slow movement while downed.
- `revive_kneel` / `help_up` — helper animation.
- `being_helped_up` — target recovery animation.
- `carry_start` — pickup/hoist or drag start.
- `carry_walk` — carrier movement while carrying/dragging.
- `carried_body` — carried player's overlay/pose.
- `drop_carried` — drop/lay down animation.
- `execute_downed` — attacker action, likely bespoke/limited.
- `hurt_to_downed` — transition from standing to downed.

Not every outfit must draw all of these immediately. The manifest should allow
fallbacks such as:

- `downed_crawl -> downed_idle`
- `carry_walk -> walk_slow`
- `revive_kneel -> interact`
- `execute_downed -> melee_slash`

But the **base body** should define these actions early so future outfits and
items can align to them.

## Server implementation sketch

Potential authoritative data:

- `PlayerState.LifeState`
- `PlayerState.DownedByPlayerId`
- `PlayerState.DownedAtTick`
- `PlayerState.BleedOutTick`
- `PlayerState.CarriedByPlayerId`
- `PlayerState.CarryingPlayerId`
- status effects: `downed`, `carrying`, `carried`, `recovery_grace`

Potential intents:

- `RevivePlayer`
- `StartCarryPlayer`
- `DropCarriedPlayer`
- `DeliverCarriedPlayer`
- `ExecuteDownedPlayer`
- `StealFromDownedPlayer`

Potential station hooks:

- Clinics/hospitals: best recovery and reputation reward.
- Social hubs: witnesses/rumors.
- Black markets: predatory delivery/bounty route.
- War memorial/court stations: public judgment consequences.

## Prototype order

1. Add `Downed` state before instant Karma Break for non-overkill lethal damage.
2. Add field revive and execute as two opposite server-owned choices.
3. Add carry/drop.
4. Add clinic delivery using generated clinic station markers.
5. Add HUD/world prompts and developer overlay state.
6. Add v2 art placeholders/fallbacks for downed/carry animations.
