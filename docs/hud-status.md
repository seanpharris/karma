# HUD Status — `feature/ui-updates` branch

Snapshot of the in-game HUD redesign work and what's still on deck.
The branch is open; merge to `develop` once the in-game pause menu
redesign and the deferred PixelLab generations land.

## What's done

### Layout

| Surface | Position | Status |
|---|---|---|
| Vitals widget | top-left | portrait circle + 3 horizontal bar rows + value labels (data-driven via `EnabledVitalsConfig`) |
| Karma duality bar | top-center | slim 14×600 spectrum, blue→vortex→red gradient, tick marks, marker triangle |
| Match timer | right of karma bar | shows just `MM:SS` |
| Karma tier badge | top-right | 100×140 medallion with progress arc + tier name + progress text |
| Bounties card | upper-right | hidden when no players have bounties |
| Quest helper card | mid-right | new — shows the player's first active quest, hidden when none |
| Hotbar | bottom-left | 4 independently-framed 64×64 slots with hotkey number band |
| Interaction prompt | anchored bottom-center, above hotbar | repositioned from fixed-pixel top-left |

### Theme adoption

Pack: [Fantasy Minimal Pixel Art GUI by etahoshi](../assets/art/third_party/Fantasy%20Minimal%20Pixel%20Art%20GUI%20by%20eta-commercial-free/README_KARMA_IMPORT.md)
(commercial-free edition).

| Surface | Pack asset | Helper |
|---|---|---|
| Vitals panel + bounty + quest helper + pause menu + audio sub-panel | `RectangleBox_96x96.png` | `MenuTheme.MakeHudPanelStyle()` |
| Pause menu buttons | `Button_52x14.png` | `MenuTheme.StyleHudButton()` |
| Hotbar slots | `HotkeyBox_34x34.png` | `MenuTheme.StyleHudSlot()` |
| Karma tier badge medallion | `BlackBigCircleBoxWithBorder_27x27.png` | direct draw inside `KarmaTierBadge` |
| Vitals portrait circle (placeholder) | `BlackBigCircleBoxWithBorder_27x27.png` | direct in `BuildVitalsPanel` |
| Vitals row icons (legacy) | `AttributesIcons_17x17.png` (4-icon strip) | sliced via `AtlasTexture` — currently unused since the portrait+bars layout dropped per-row icons |

Texture loading is routed through `AtlasTextureLoader.Load(path, forceImageLoad: true)`
so the pack works without `.import` sidecars in the dev workflow.
`PaletteOptOutMeta` is set on every pack-styled control so the medieval
palette walker doesn't repaint over the pack art.

### Removed / cleaned up

- Old karma text label (`statusPanel` / `_karmaLabel`) — redundant with the tier badge.
- Floating debug labels: leaderboard, target-in-range, faction list, inventory text, perks text.
- Faction panel — entirely removed from HUD.
- Match label's `Phase / Saint / Scourge` detail — only the timer remains.
- Bounties "none active" idle text — panel hides when empty.
- Carry-karma toggle in pause menu — option removed.
- Appearance sub-panel in pause menu — entirely removed (PlayerController keyboard cycling still works).
- Hotbar redundant `[1: --] [2: --]` text row — slot Buttons cover the same info.
- "YOUR KARMA" caption under the duality bar.

### Pause menu styling

- Pause panel + audio sub-panel use the etahoshi `RectangleBox` 9-slice.
- Pause buttons (Resume / Options / Main Menu / Quit / Back) use the etahoshi `Button_52x14` 9-slice.
- Main menu Options + Credits overlays intentionally stay procedural (deep-navy + gold border) so they read against the painted karma duality splash.

## What's still to do

### High-priority (visible HUD work)

- **Pause menu redesign** — replace the simple Resume/Quit panel
  with the user's mockup of a STATUS / EQUIPMENT character screen.
  Pack assets identified for each part:
  - Title plaques (`TitleBox_64x16`)
  - Equipment slots (`BlankEquipmentIcons_20x17`)
  - Stat icons (`AttributesIcons_17x17`)
  - Effect-row icons (`BuffIcons_16x16`)
  - Stat increment buttons (`StatusIncDecButton_7x9`)
  - Currency strip (`CoinIcon_16x18`)
  - Bottom-right map/settings (`MenusBox_34x34` + `MenusIcons_34x34`)
  - Tooltip + equipped-item subpanel (`RectangleBox_96x96`)
  - Decorative borders (`BottomPatternPanel_119x17`, `Pipe_36x11`)

  Proposed splits when picked back up:
  1. Layout skeleton — 2-column STATUS/EQUIPMENT panels, hotbar
     + bottom-right action buttons, placeholders for content.
  2. Stats wiring — populate STATUS values from `GameState.LocalKarma`
     + `PlayerSnapshot`; render effect icons via `BuffIcons` strip.
  3. Equipment wiring — populate EQUIPMENT slots from
     `GameState.LocalPlayer.Equipment`.

### Smaller follow-ups

- **Perks row** — `_perksRow` is built as an empty `HBoxContainer`
  hidden by default. Populating it requires:
  - A perks data source (GameState.LocalKarma.Perks?)
  - A perk-id → buff-icon-slot mapping
  - Show/hide tied to perk count
- **Karma duality bar frame texture** — currently the bar is a flat
  procedural gold rectangle. Pack has `HealthBarPanel_160x41` /
  `ValueBar_128x16` for a 9-slice frame, but `KarmaDualityBar`
  custom-draws gradient + ticks + marker, so adding a 9-slice frame
  needs a draw-order rework (frame border drawn around but not over
  the marker pip). Deferred.
- **`HighlightButton_60x23` overlay** for hovered buttons — pack has
  this 4-corner-bracket overlay but it can't compose into a single
  Godot `StyleBox`. Deferred until a custom Button subclass is worth it.
- **Bar fills via pack sprites** — `ValueRed_120x8` + `ValueBlue_120x8`
  could replace the procedural color rectangles inside the vitals
  bars. Currently the procedural fills are tinted per vital so we
  get more colors than the pack ships; would need runtime tinting.
- **Floating label cleanup** — `_chatLabel` ("Local chat: quiet") and
  `_eventLabel` are still floating top-left labels. Should fold into
  themed panels or convert to a chat scroll widget.

### Deferred PixelLab generations

User confirmed:
- **Player portrait** — will be generated alongside the player sprite
  in a future session, not as a HUD-specific PixelLab call. The
  vitals portrait circle stays empty (etahoshi `BlackBigCircleBox`
  frame) until then.
- **Currency emblem** — `CoinIcon_16x18` from the pack already
  covers this; no PixelLab needed.

Still candidates if/when we want bespoke karma art:
- **Karma tier emblems** (Paragon halo / Neutral balance glyph /
  Renegade flame) for the medallion interior. Pack has generic gems
  in `BuffIcons` but nothing karma-specific.
- **Karma-themed scroll/banner** for the QUEST card title (currently
  the plain caption "QUEST"). `TitleBox_64x16` from the pack could
  be a stopgap.

## Files of interest

- [scripts/UI/HudController.cs](../scripts/UI/HudController.cs) —
  in-game HUD; `BuildVitalsPanel`, `BuildEscapeMenu`, hotbar /
  bounty / quest helper / match-timer wiring.
- [scripts/UI/MenuTheme.cs](../scripts/UI/MenuTheme.cs) —
  shared palette + helpers (`MakePanelStyle`, `MakeHudPanelStyle`,
  `StyleButton`, `StyleHudButton`, `StyleHudSlot`).
- [scripts/UI/KarmaDualityBar.cs](../scripts/UI/KarmaDualityBar.cs) —
  top-center spectrum bar.
- [scripts/UI/KarmaTierBadge.cs](../scripts/UI/KarmaTierBadge.cs) —
  top-right karma tier crest.
- [assets/art/third_party/Fantasy Minimal Pixel Art GUI by eta-commercial-free/README_KARMA_IMPORT.md](../assets/art/third_party/Fantasy%20Minimal%20Pixel%20Art%20GUI%20by%20eta-commercial-free/README_KARMA_IMPORT.md) —
  pack inventory, license, and per-asset wiring notes.
