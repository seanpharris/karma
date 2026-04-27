#!/usr/bin/env python3
"""Generate local 32x64 player-model review candidates.

These are deterministic placeholder attempts for visual review when external
PixelLab/MCP generation is unavailable. They stay in the imported/review folder
and are not loaded by runtime code until curated/promoted.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parent
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from prepare_character_sheet import ImagePixels, Pixel, read_png_rgba, write_png_rgba  # noqa: E402

SOURCE = Path("assets/art/sprites/player_v2/player_model_32x64_8dir_4row.png")
OUTPUT_DIR = Path("assets/art/sprites/player_v2/imported/review_2026-04-26")
WIDTH = 256
HEIGHT = 256
CELL_W = 32
CELL_H = 64
COLUMNS = 8
ROWS = 4

OUTLINE = (11, 9, 8)
SKIN = (178, 122, 84)
SKIN_D = (109, 71, 51)
HAIR = (25, 16, 11)
VISOR = (56, 198, 242)
SUIT = (224, 96, 30)
SUIT_D = (122, 43, 19)
PLATE = (76, 91, 107)
PLATE_L = (137, 155, 168)
PACK = (43, 56, 63)
BOOT = (20, 20, 22)
SOLE = (40, 38, 35)

RGBA = tuple[int, int, int, int]
RGB = tuple[int, int, int]


def blank() -> ImagePixels:
    return [[(0, 0, 0, 0) for _ in range(WIDTH)] for _ in range(HEIGHT)]


def rgb(pixel: Pixel) -> RGB:
    return pixel[0], pixel[1], pixel[2]


def recolor(source: ImagePixels, mapping: dict[RGB, RGB]) -> ImagePixels:
    out = blank()
    for y, row in enumerate(source):
        for x, pixel in enumerate(row):
            if pixel[3] <= 5:
                continue
            replacement = mapping.get(rgb(pixel), rgb(pixel))
            out[y][x] = (*replacement, pixel[3])
    return out


def draw_rect(image: ImagePixels, x: int, y: int, w: int, h: int, color: RGBA) -> None:
    for yy in range(max(0, y), min(HEIGHT, y + h)):
        for xx in range(max(0, x), min(WIDTH, x + w)):
            image[yy][xx] = color


def draw_cell_rect(image: ImagePixels, column: int, row: int, x: int, y: int, w: int, h: int, color: RGB) -> None:
    draw_rect(image, column * CELL_W + x, row * CELL_H + y, w, h, (*color, 255))


def add_engineer_marks(image: ImagePixels) -> None:
    # Bright chest/visor cue, readable even at runtime scale.
    for row in range(ROWS):
        for column in range(COLUMNS):
            draw_cell_rect(image, column, row, 13, 27, 6, 2, VISOR)
            draw_cell_rect(image, column, row, 15, 30, 2, 5, PLATE_L)


def add_settler_cloak(image: ImagePixels) -> None:
    cloak = (74, 50, 34)
    cloak_hi = (134, 95, 58)
    for row in range(ROWS):
        for column in range(COLUMNS):
            if column in {3, 4, 5}:
                draw_cell_rect(image, column, row, 8, 23, 16, 23, OUTLINE)
                draw_cell_rect(image, column, row, 9, 24, 14, 21, cloak)
                draw_cell_rect(image, column, row, 12, 25, 8, 2, cloak_hi)
            else:
                draw_cell_rect(image, column, row, 8, 24, 3, 19, cloak)
                draw_cell_rect(image, column, row, 21, 24, 3, 19, cloak)


def add_medic_marks(image: ImagePixels) -> None:
    white = (214, 226, 218)
    red = (190, 38, 42)
    teal = (54, 177, 151)
    for row in range(ROWS):
        for column in range(COLUMNS):
            draw_cell_rect(image, column, row, 12, 25, 8, 8, white)
            draw_cell_rect(image, column, row, 15, 26, 2, 6, red)
            draw_cell_rect(image, column, row, 13, 28, 6, 2, red)
            draw_cell_rect(image, column, row, 10, 40, 12, 2, teal)


def add_scavenger_marks(image: ImagePixels) -> None:
    rust = (151, 83, 38)
    dark_pack = (33, 38, 39)
    yellow = (212, 166, 60)
    for row in range(ROWS):
        for column in range(COLUMNS):
            draw_cell_rect(image, column, row, 7, 37, 3, 14, rust)
            draw_cell_rect(image, column, row, 22, 35, 3, 16, dark_pack)
            draw_cell_rect(image, column, row, 10, 24, 3, 2, yellow)
            if column in {2, 3, 4, 5, 6}:
                draw_cell_rect(image, column, row, 9, 25, 6, 15, dark_pack)


def make_candidates(source: ImagePixels) -> dict[str, ImagePixels]:
    engineer = recolor(source, {
        SUIT: (220, 112, 38),
        SUIT_D: (110, 55, 28),
        PLATE: (72, 91, 106),
        PLATE_L: (132, 158, 172),
    })
    add_engineer_marks(engineer)

    settler = recolor(source, {
        SUIT: (110, 76, 48),
        SUIT_D: (58, 42, 30),
        PLATE: (132, 94, 56),
        PLATE_L: (174, 131, 72),
        PACK: (67, 49, 34),
        HAIR: (90, 56, 32),
    })
    add_settler_cloak(settler)

    medic = recolor(source, {
        SUIT: (70, 139, 128),
        SUIT_D: (35, 76, 74),
        PLATE: (189, 211, 202),
        PLATE_L: (230, 238, 230),
        VISOR: (80, 225, 210),
    })
    add_medic_marks(medic)

    scavenger = recolor(source, {
        SUIT: (126, 85, 45),
        SUIT_D: (61, 48, 34),
        PLATE: (86, 82, 70),
        PLATE_L: (160, 135, 78),
        PACK: (38, 42, 42),
        VISOR: (218, 155, 52),
    })
    add_scavenger_marks(scavenger)

    return {
        "local_engineer_a_32x64_8dir_4row.png": engineer,
        "local_settler_cloak_a_32x64_8dir_4row.png": settler,
        "local_medic_a_32x64_8dir_4row.png": medic,
        "local_scavenger_a_32x64_8dir_4row.png": scavenger,
    }


def main() -> int:
    source_width, source_height, source = read_png_rgba(SOURCE)
    if source_width != WIDTH or source_height != HEIGHT:
        raise SystemExit(f"Expected {SOURCE} to be {WIDTH}x{HEIGHT}, got {source_width}x{source_height}")

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    written: list[str] = []
    for filename, image in make_candidates(source).items():
        path = OUTPUT_DIR / filename
        write_png_rgba(path, WIDTH, HEIGHT, image)
        written.append(str(path))

    index = {
        "schema": "karma.player_v2.review_candidates.v1",
        "source": str(SOURCE),
        "contract": {
            "frameWidth": CELL_W,
            "frameHeight": CELL_H,
            "columns": COLUMNS,
            "rows": ROWS,
            "directions": ["front", "front-right", "right", "back-right", "back", "back-left", "left", "front-left"],
            "animationRows": ["idle", "walk_a", "walk_b", "walk_c"],
        },
        "runtimeLoaded": False,
        "reviewNotes": [
            "Local deterministic placeholders for tomorrow review, not PixelLab outputs.",
            "Use these to choose silhouette/color direction before promoting or replacing with PixelLab imports.",
        ],
        "candidates": written,
    }
    (OUTPUT_DIR / "candidate_index.json").write_text(json.dumps(index, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {len(written)} player model review candidates to {OUTPUT_DIR}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
