#!/usr/bin/env python3
"""Compose a randomly-picked LPC character into Karma's player atlas format.

Reads variants from the vendored LPC library at
`assets/art/sprites/lpc/`, picks one variant per "useful" layer category
(body + clothing + hair), stacks them in z-order, and re-cells the
result into the existing Karma player_v2 contract:

  - 256x256 RGBA, 8 columns x 4 rows of 32x64 cells
  - Direction column order: south, south-east, east, north-east,
    north, north-west, west, south-front-left
  - Row 0 idle, rows 1-3 walk frames

LPC source format is 576x256 RGBA, 9 cols x 4 rows of 64x64 cells, with
LPC's 4-direction order (up / left / down / right). We map by sampling
specific LPC frames per direction and horizontal-flipping the cardinals
to stand in for the diagonals.

Run:
  python3 tools/lpc_compose_random.py [--seed 42] [--output PATH]
"""

from __future__ import annotations

import argparse
import json
import random
import sys
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parent
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from prepare_character_sheet import (  # noqa: E402
    ImagePixels,
    Pixel,
    read_png_rgba,
    write_png_rgba,
)
from import_pixellab_character import (  # noqa: E402
    TARGET_CELL_W,
    TARGET_CELL_H,
    blank,
    blit,
    fit_cell,
)

REPO_ROOT = TOOLS_DIR.parent
LPC_ROOT = REPO_ROOT / "assets" / "art" / "sprites" / "lpc"

# LPC standard sheet: 9 cols x 4 rows x 64x64.
LPC_FRAME = 64
LPC_COLS = 9
LPC_ROWS = 4

# LPC row order is up / left / down / right.
LPC_ROW_UP = 0
LPC_ROW_LEFT = 1
LPC_ROW_DOWN = 2
LPC_ROW_RIGHT = 3

# Karma column order (matches DIRECTION_ORDER in import_pixellab_character).
KARMA_COL_SOUTH = 0
KARMA_COL_SE = 1
KARMA_COL_EAST = 2
KARMA_COL_NE = 3
KARMA_COL_NORTH = 4
KARMA_COL_NW = 5
KARMA_COL_WEST = 6
KARMA_COL_SW = 7

# Per-Karma-column: which LPC row to sample, and whether to flip horizontally.
# LPC is body-relative 4-direction — diagonals don't flip honestly, so each
# diagonal falls back to its nearest cardinal verbatim. The character will
# appear to face the cardinal during diagonal movement; that's the trade-off
# of using 4-direction source art in an 8-direction renderer.
KARMA_COLUMN_SOURCE: list[tuple[int, bool]] = [
    (LPC_ROW_DOWN, False),    # south       — south
    (LPC_ROW_DOWN, False),    # south-east  ← reuse south
    (LPC_ROW_RIGHT, False),   # east        — east
    (LPC_ROW_UP, False),      # north-east  ← reuse north
    (LPC_ROW_UP, False),      # north       — north
    (LPC_ROW_UP, False),      # north-west  ← reuse north
    (LPC_ROW_LEFT, False),    # west        — west
    (LPC_ROW_LEFT, False),    # south-west  ← reuse west
]

# Karma 4 rows: idle (1 frame), walk-1, walk-2, walk-3.
# Sample LPC walk frames at indices that read as a clean "step left, neutral,
# step right, neutral" cycle. The walk row in LPC is 9 frames; we pick:
#   row 0: idle (LPC frame 0)
#   row 1: step out (LPC frame 1)
#   row 2: mid stride (LPC frame 4)
#   row 3: step out other side (LPC frame 7)
KARMA_ROW_LPC_FRAME = [0, 1, 4, 7]


def lpc_path(rel: str) -> Path:
    return LPC_ROOT / "spritesheets" / rel


def load_layer(rel_path: str, animation: str) -> tuple[int, int, ImagePixels] | None:
    sheet = lpc_path(rel_path) / f"{animation}.png"
    if not sheet.exists():
        return None
    return read_png_rgba(sheet)


def composite_layers(
    layers: list[tuple[str, str]],   # list of (description, relative_layer_path)
    animation: str,
) -> tuple[int, int, ImagePixels]:
    """Stack layer PNGs in supplied order onto a 576x256 LPC sheet.

    Lower-indexed layers are drawn first (under). Layer paths must produce
    the same 576x256 dimensions; LPC asserts this contract for stackable
    layers.
    """
    base = None
    width = LPC_FRAME * LPC_COLS
    height = LPC_FRAME * LPC_ROWS
    for desc, rel in layers:
        loaded = load_layer(rel, animation)
        if loaded is None:
            print(f"  skip {desc}: no '{animation}.png' under {rel}", file=sys.stderr)
            continue
        w, h, pixels = loaded
        if w != width or h != height:
            print(f"  skip {desc}: dimension mismatch {w}x{h}", file=sys.stderr)
            continue
        if base is None:
            base = blank(width, height)
        for y in range(height):
            for x in range(width):
                pixel = pixels[y][x]
                if pixel[3] > 0:
                    base[y][x] = pixel
        print(f"  layered {desc} :: {rel}/{animation}.png")
    if base is None:
        raise RuntimeError(f"no usable layers for animation '{animation}'")
    return width, height, base


def crop_lpc_frame(
    sheet: ImagePixels,
    row: int,
    col: int,
) -> tuple[int, int, ImagePixels]:
    out = blank(LPC_FRAME, LPC_FRAME)
    src_x = col * LPC_FRAME
    src_y = row * LPC_FRAME
    for y in range(LPC_FRAME):
        for x in range(LPC_FRAME):
            out[y][x] = sheet[src_y + y][src_x + x]
    return LPC_FRAME, LPC_FRAME, out


def flip_horizontal(pixels: ImagePixels) -> ImagePixels:
    return [list(reversed(row)) for row in pixels]


def build_karma_sheet(lpc_sheet: ImagePixels) -> ImagePixels:
    """Re-cell a composed 576x256 LPC sheet into Karma's 256x256 8-dir 4-row contract."""
    out_w = TARGET_CELL_W * 8
    out_h = TARGET_CELL_H * 4
    out = blank(out_w, out_h)
    for column, (lpc_row, flip) in enumerate(KARMA_COLUMN_SOURCE):
        for row, frame_index in enumerate(KARMA_ROW_LPC_FRAME):
            _, _, frame = crop_lpc_frame(lpc_sheet, lpc_row, frame_index)
            if flip:
                frame = flip_horizontal(frame)
            fitted = fit_cell(frame, 0, 0, LPC_FRAME, LPC_FRAME)
            blit(out, fitted, column * TARGET_CELL_W, row * TARGET_CELL_H)
    return out


def gather_random_layers(rng: random.Random, body_kind: str = "male") -> list[tuple[str, str]]:
    """Pick a random body + a few clothing/hair layers that have walk.png."""
    picks: list[tuple[str, str]] = []
    bodies_dir = LPC_ROOT / "spritesheets" / "body" / "bodies" / body_kind
    if (bodies_dir / "walk.png").exists():
        picks.append(("body", f"body/bodies/{body_kind}"))

    layer_buckets = {
        "torso":  ["torso/clothes", "torso/jacket", "torso/jacket_collared"],
        "legs":   ["legs/pants", "legs/shorts", "legs/skirts"],
        "feet":   ["feet/shoes", "feet/boots"],
        "hair":   ["hair/short", "hair/long", "hair/messy"],
    }
    for label, roots in layer_buckets.items():
        candidates: list[Path] = []
        for root in roots:
            base = LPC_ROOT / "spritesheets" / root
            if not base.exists():
                continue
            for path in base.rglob("walk.png"):
                relative = path.parent.relative_to(LPC_ROOT / "spritesheets")
                candidates.append(relative)
        if not candidates:
            print(f"  no candidates for {label}", file=sys.stderr)
            continue
        chosen = rng.choice(candidates)
        picks.append((label, str(chosen).replace("\\", "/")))

    return picks


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--seed", type=int, default=None,
                        help="Random seed for reproducible picks")
    parser.add_argument("--body", default="male",
                        choices=["male", "female", "muscular", "pregnant", "teen", "child"],
                        help="Body type to compose against")
    parser.add_argument("--output", type=Path,
                        default=REPO_ROOT / "assets" / "art" / "generated" / "lpc" / "random_character_32x64_8dir_4row.png",
                        help="Output path for the Karma-format sheet")
    parser.add_argument("--lpc-output", type=Path,
                        default=REPO_ROOT / "assets" / "art" / "generated" / "lpc" / "random_character_lpc_walk.png",
                        help="Optional output for the raw 576x256 composed LPC sheet")
    args = parser.parse_args()

    rng = random.Random(args.seed) if args.seed is not None else random.Random()

    layers = gather_random_layers(rng, body_kind=args.body)
    if not layers:
        print("ERROR: could not gather any layers", file=sys.stderr)
        return 1
    print("Picked layers:")
    for desc, rel in layers:
        print(f"  {desc:8s}  {rel}")

    width, height, lpc_sheet = composite_layers(layers, animation="walk")

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.lpc_output.parent.mkdir(parents=True, exist_ok=True)

    write_png_rgba(args.lpc_output, width, height, lpc_sheet)
    print(f"wrote LPC composite: {args.lpc_output}")

    karma = build_karma_sheet(lpc_sheet)
    write_png_rgba(args.output, TARGET_CELL_W * 8, TARGET_CELL_H * 4, karma)
    print(f"wrote Karma atlas: {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
