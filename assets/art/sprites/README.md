# LPC Spritesheet Library (vendored)

Pulled from [Liberated Pixel Cup / Universal-LPC-Spritesheet-Character-Generator](https://github.com/LiberatedPixelCup/Universal-LPC-Spritesheet-Character-Generator)
on **2026-05-02**, master branch. ~520 MB of layered character art.

The upstream README is preserved next to this file as
[`LPC_UPSTREAM_README.md`](./LPC_UPSTREAM_README.md). The full upstream
[`LICENSE`](./LICENSE) and the master attribution log [`CREDITS.csv`](./CREDITS.csv)
ship alongside.

## Folder layout

| Path                      | What it holds                                                            |
|---------------------------|--------------------------------------------------------------------------|
| `spritesheets/`           | Raw PNG layers organised by category → variant → animation               |
| `sheet_definitions/`      | JSON metadata per variant: authors, licenses, animation list, file paths |
| `palette_definitions/`    | Per-layer recolour palettes used by the upstream generator               |
| `themes/`                 | Karma-side bundles that pick variants across categories (start here)     |
| `LPC_UPSTREAM_README.md`  | Original LPC README                                                      |
| `LICENSE`                 | Aggregated license text                                                  |
| `CREDITS.csv`             | Master attribution log (~4 MB)                                           |
| `PALETTE_RECOLOR_GUIDE.md`| Recolour pipeline doc                                                    |
| `CONTRIBUTING.md`         | Upstream contribution guide                                              |

## Spritesheet categories

`spritesheets/` is organised by **layer category**. A complete character is a
stack across these:

```
arms       backpack    beards     body       cape       dress
eyes       facial      feet       hair       hat        head
legs       neck        quiver     shadow     shield     shoulders
tools      torso       weapon
```

Each category folder drills into variants (e.g. `body/bodies/male/`,
`body/bodies/female/`, `hair/long/red/`) and each variant ships one PNG per
animation: `walk.png`, `run.png`, `slash.png`, `thrust.png`, `shoot.png`,
`spellcast.png`, `idle.png`, `combat_idle.png`, `hurt.png`, `climb.png`,
`jump.png`, `sit.png`, `emote.png`, plus melee variants like `1h_slash.png`,
`backslash.png`, `halfslash.png`.

## Sheet format

Standard LPC layout for almost everything:

- **64×64 cells** (some weapon "oversize" sheets are bigger; check per-sheet)
- **9 columns** per row (animation frames)
- **4 rows** in this order: **up / left / down / right** (NOT compass — body's-eye)

That's a `576×256` sheet for most animation files. Walking is 9 frames
per direction; running, slashing, etc. mostly the same.

LPC is **4-direction** by default. Diagonals (NE/SE/NW/SW) are not in the
standard sheets; for an 8-direction game you either skip them, generate them
by horizontally flipping the cardinals (works for E↔W and the diagonal
pairs), or pull from one of the rare LPC contributions that include them.

## How a "theme" composes

A theme is just a set of variant picks, one per layer category you want to
show. For example a fantasy mage might be:

| Layer    | Variant path                                                       |
|----------|--------------------------------------------------------------------|
| body     | `spritesheets/body/bodies/male/`                                   |
| hair     | `spritesheets/hair/long/white/`                                    |
| torso    | `spritesheets/torso/clothes/longsleeve/longsleeve_brown/male/`     |
| legs     | `spritesheets/legs/pants/pants_brown/male/`                        |
| feet     | `spritesheets/feet/shoes/shoes_brown/male/`                        |
| weapon   | `spritesheets/weapon/magic/staff/wood/foreground/`                 |

Bundles like this live under [`themes/`](./themes/) as JSON files —
`themes/fantasy_mage.json`, `themes/scifi_ranger.json`, etc. Pick from them
or hand-write new ones. The Karma loader composes them into a single sheet
per character at runtime (importer TBD).

## Animation contract per variant

Not every variant has every animation. The variant's
`sheet_definitions/<category>/<variant>.json` lists exactly which animations
that art set covers. Always check that list before binding a theme to a
runtime animation set, or you'll get gaps. Example
(`sheet_definitions/body/body.json`):

```json
"animations": [
  "spellcast", "thrust", "walk", "slash", "shoot", "hurt",
  "watering", "idle", "jump", "run", "sit", "emote", "climb",
  "combat", "1h_slash", "1h_backslash", "1h_halfslash"
]
```

## Licensing — non-negotiable

LPC is **multi-licensed by author and asset**. Every variant listed in
`sheet_definitions/*.json` carries its own `licenses` array — typically a
mix of these:

- **CC0** — public domain, no attribution required.
- **CC-BY 3.0/4.0** — attribution required.
- **CC-BY-SA 3.0/4.0** — attribution + share-alike (downstream art derived
  from these inherits CC-BY-SA).
- **OGA-BY 3.0** — OpenGameArt's attribution license.
- **GPL 3.0** — copyleft software-style; obligations ripple into derivative
  art *and* the bundled binary unless ring-fenced carefully.

Karma's compliance plan:

1. Every shipped variant we use gets cited in our credits screen.
   `CREDITS.csv` is the source of truth; `theme_bundle_to_credits.py`
   (TODO) will trace bundle → variants → credits rows.
2. Avoid using GPL-only assets in any non-GPL ship (the project is not
   currently GPL). Prefer CC0 / CC-BY / OGA-BY / CC-BY-SA variants.
3. CC-BY-SA assets are fine as long as we publish derivative LPC composites
   under CC-BY-SA too; that's a separate "art credits" SA bundle on the
   download page.

If in doubt, check the variant's JSON `licenses` array. The aggregated
`LICENSE` file in this folder is the upstream collation; treat it as
informational, not a per-asset rule.

## Importing into Karma's sprite contract

LPC ships at 64×64. Karma's player_v2 contract is 32×64 with a 64×64
runtime variant — the runtime cell already matches LPC. Two integration
paths:

1. **Direct LPC mode** — render a chosen theme bundle as-is at 64×64
   without going through the player_v2 32×64 path. Good for NPCs and any
   character that doesn't share rigging with the existing PixelLab
   paper-doll work.
2. **Down-fit to 32×64** — crop/scale the LPC body into the player_v2 cell.
   Loses detail but shares the existing renderer. Only worth it if we're
   replacing the PixelLab base body wholesale.

Importer scripts will live in `tools/` (TBD). The existing
`tools/import_pixellab_per_frame.py` is a reasonable scaffold for the
LPC variant — it already knows how to walk a per-frame folder layout
and emit our 32×64 / 64×64 sheets. The LPC variant differs in:

- Source layout is per-animation PNG (whole sheet, multi-row) rather
  than per-frame folder.
- Direction order is up/left/down/right (NOT south-first).
- Frames are 64×64, not 120×120.

## Repo size note

This vendoring adds **~520 MB** to the working tree. Consider Git LFS or a
build-time download instead if the prototype repo gets too heavy. For now
it's checked in directly so everything is reproducible offline.