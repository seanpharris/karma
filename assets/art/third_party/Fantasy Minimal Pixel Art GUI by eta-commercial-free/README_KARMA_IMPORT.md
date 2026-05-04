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

## On deck

- `HealthBarPanel_160x41.png` — `KarmaDualityBar` ornate frame
  (9-slice).
- `AttributesIcons_17x17.png` — vitals row icons (heart / bolt /
  shield / etc.) replacing the procedural colored dots.
- `ValueBar_128x16.png` — vitals bar frames.
- `Button_52x14.png` / `HighlightButton_60x23.png` — replace
  `MenuTheme.StyleButton` styleboxes.
- `HotkeyBox_34x34.png` / `ItemBox_24x24.png` — hotbar slot frames.
