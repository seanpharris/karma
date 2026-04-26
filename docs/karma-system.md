# Karma System

## Vocabulary

- **Karma**: the numerical score, centered on 0
- **Ascend**: move in the positive karma direction
- **Descend**: move in the negative karma direction
- **Ascension**: positive karma path
- **Descension**: negative karma path
- **Karma Break**: death reset that returns a player to 0 karma
- **Saint**: the single current highest-karma player on a server
- **Scourge**: the single current lowest-karma player on a server

## Karma Scale

Players start at 0. Karma is uncapped in both directions so players can keep
Ascending or Descending indefinitely.

After `+100`, players continue through repeatable `Exalted` ranks. After `-100`,
players continue through repeatable `Abyssal` ranks. For example, `+220` is
`Exalted 2`, while `-340` is `Abyssal 3`.

Rank progress is tracked toward the next milestone. A player at `+220` sees
`20/100 toward Exalted 3`, while a player at `-340` sees `40/100 toward
Abyssal 4`.

## Leaderboard Standing

Each world tracks exactly one current positive leader and one current negative
leader:

- Highest karma: **Saint**
- Lowest karma: **Scourge**

These are exclusive server standings, separate from non-exclusive tier names.
Only one player can be Saint and only one player can be Scourge at a time.
Snapshots store both the global leaderboard and each player's current standing
so save/debug tools can verify the exclusivity rule.

## Perks

Perks unlock from karma magnitude and current leaderboard standing.

Ascension examples:

- +10: Trusted Discount
- +20: Calming Presence
- +35: Beacon Aura
- +50: Paragon Favor
- +100: Exalted Grace
- Every +100 after that: repeat Exalted rank perk

Descension examples:

- -10: Shifty Prices
- -20: Rumorcraft
- -35: Renegade Nerve
- -50: Dread Reputation
- -100: Abyssal Mark
- Every -100 after that: repeat Abyssal rank perk

Standing perks:

- Saint: current highest positive player
- Scourge: current lowest negative player

## Death

Player death causes a **Karma Break** for the player who died. Their health and
body return, but their karma score, path, and path perks reset.

## Entanglements

Some relationship actions create persistent entanglements. These can Descend the
player, alter NPC opinions, and become hooks for future quests, rumors, or
blackmail.

## Tiers

| Karma | Tier |
| ---: | --- |
| +100 | Exalted |
| +75 | Luminary |
| +50 | Paragon |
| +35 | Beacon |
| +20 | Advocate |
| +10 | Trusted |
| 0 | Unmarked |
| -10 | Shifty |
| -20 | Outlaw |
| -35 | Renegade |
| -50 | Dread |
| -75 | Wraith |
| -100 | Abyssal |

## Action Tags

Karma shifts should be computed from structured action tags plus context.

Example tags:

- helpful
- harmful
- funny
- humiliating
- violent
- deceptive
- generous
- selfish
- romantic
- betrayal
- protective
- chaotic
- lawful
- forbidden
