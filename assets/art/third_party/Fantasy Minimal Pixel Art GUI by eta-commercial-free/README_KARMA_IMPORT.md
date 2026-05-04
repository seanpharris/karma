# Fantasy Minimal Pixel Art GUI (etahoshi) — commercial-free edition

Pixel-art HUD frames, bars, and icons used to dress the karma main /
pause / in-game HUD widgets.

- Source: https://etahoshi.itch.io/minimal-fantasy-gui-by-eta
- Author: etahoshi
- License: "commercial-free" edition — author grants commercial use
  of all assets in this folder. See the upstream itch.io page for the
  current terms; the empty `commercial-free.txt` ships with the
  download as the author's licence marker.

## Wired into

- `BlackBigCircleBoxWithBorder_27x27.png` — `KarmaTierBadge` medallion
  outer frame (replaces the procedural gold ring).
- `RectangleBox_96x96.png` — `MenuTheme.MakePanelStyle()` panel
  background, applied to: Main Menu options/credits overlays, the
  in-game pause panel + audio-options sub-panel, the HUD vitals
  panel. Single source of truth, so any future panel that calls
  `MakePanelStyle()` inherits the same gold frame.
- `Button_52x14.png` — `MenuTheme.MakeButtonStyle()` button background,
  applied to every button styled through `StyleButton` or
  `StyleOptionButton`. Modulate variants per state (normal / hover /
  pressed / disabled); state feedback comes from the modulate tint
  plus the existing font color shift.
- `AttributesIcons_17x17.png` — HUD vitals icons. Sliced via
  `AtlasTexture` into 4 individual 17×17 icons; assigned per-vital:
  Health → slot 0 (heart), Ammo → slot 1, Stamina → slot 2,
  Hunger → slot 3.

## On deck

- `HighlightButton_60x23.png` — bracket overlay for hovered buttons.
  Doesn't compose into a single stylebox, so deferred until a custom
  Button subclass is justified.
- `HealthBarPanel_160x41.png` / `ValueBar_128x16.png` — bar frames
  for the vitals + KarmaDualityBar (custom-draw rework needed for
  the duality bar's marker layering).
- `HotkeyBox_34x34.png` / `ItemBox_24x24.png` — hotbar slot frames.
