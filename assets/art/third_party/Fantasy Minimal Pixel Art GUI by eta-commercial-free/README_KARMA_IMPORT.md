# Fantasy Minimal Pixel Art GUI (etahoshi) — commercial-free edition

Pixel-art HUD frames, bars, and icons used to dress the karma main /
pause / in-game HUD widgets.

- Source: https://etahoshi.itch.io/minimal-fantasy-gui-by-eta
- Author: etahoshi
- License: "commercial-free" edition — author grants commercial use
  of all assets in this folder. See the upstream itch.io page for the
  current terms; the empty `commercial-free.txt` ships with the
  download as the author's licence marker.

## Scope

Pack art is used by the **in-game HUD** and the **pause menu**. The
main menu Options + Credits overlays keep their procedural
karma-duality look so they read against the painted splash without
competing with it.

## Wired into

- `BlackBigCircleBoxWithBorder_27x27.png` — `KarmaTierBadge` medallion
  outer frame (replaces the procedural gold ring).
- `RectangleBox_96x96.png` — `MenuTheme.MakeHudPanelStyle()`, applied
  to the HUD vitals panel + the pause menu and pause options
  sub-panel.
- `Button_52x14.png` — `MenuTheme.StyleHudButton()`, applied to every
  pause menu button (Resume / Options / Main Menu / Quit + Back).
  Modulate variants per state (normal / hover / pressed / disabled).
- `AttributesIcons_17x17.png` — HUD vitals icons. Sliced via
  `AtlasTexture` into 4 individual 17×17 icons; assigned per-vital:
  Health → slot 0 (heart), Ammo → slot 1, Stamina → slot 2,
  Hunger → slot 3.

## On deck (HUD-only)

- `HealthBarPanel_160x41.png` / `ValueBar_128x16.png` — vitals bar
  frames + KarmaDualityBar frame (custom-draw rework needed for the
  duality bar's marker layering).
- `HotkeyBox_34x34.png` / `ItemBox_24x24.png` — hotbar slot frames.
