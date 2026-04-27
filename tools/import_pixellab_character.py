#!/usr/bin/env python3
"""Import a PixelLab character sheet into Karma's 32x64 player-v2 contract.

This script is intentionally local/offline: it does not call PixelLab, does not
read API tokens, and does not upload anything. Export/download a PixelLab PNG or
ZIP, then normalize it here.

Expected default input:
- 8 direction columns in PixelLab/Karma order:
  south, south-east, east, north-east, north, north-west, west, south-west
- 1+ animation rows. The first four rows are mapped to:
  idle, walk-1, walk-2, walk-3

Outputs:
- <output-stem>_32x64_8dir_4row.png      256x256, 8x4 cells, 32x64 cells
- <output-stem>_32x64_8dir_runtime.png   512x256, 8x4 cells, centered in 64x64 cells
"""

from __future__ import annotations

import argparse
import sys
import tempfile
import zipfile
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parent
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from prepare_character_sheet import (  # noqa: E402
    ImagePixels,
    Pixel,
    is_chroma_green,
    read_png_rgba,
    write_png_rgba,
)

TARGET_CELL_W = 32
TARGET_CELL_H = 64
RUNTIME_CELL = 64
DIRECTIONS = 8
ROWS = 4
MAX_BODY_W = 28
MAX_BODY_H = 60

DIRECTION_ORDER = [
    "front/down",
    "front-right",
    "right",
    "back-right",
    "back/up",
    "back-left",
    "left",
    "front-left",
]

PixelSource = tuple[int, int, ImagePixels]


def blank(width: int, height: int) -> ImagePixels:
    return [[(0, 0, 0, 0) for _ in range(width)] for _ in range(height)]


def normalize_pixel(pixel: Pixel, remove_chroma: bool) -> Pixel:
    r, g, b, a = pixel
    if remove_chroma and is_chroma_green(pixel):
        return (0, 0, 0, 0)
    # PixelLab usually exports alpha correctly, but keep fully transparent pixels
    # colorless so Godot import previews do not show odd fringes.
    if a <= 5:
        return (0, 0, 0, 0)
    return (r, g, b, a)


def extract_png_from_zip(path: Path) -> Path:
    temp_dir = Path(tempfile.mkdtemp(prefix="karma-pixellab-"))
    with zipfile.ZipFile(path) as archive:
        candidates: list[tuple[int, str, Path]] = []
        for name in archive.namelist():
            if name.lower().endswith(".png") and not name.endswith("/"):
                output = temp_dir / Path(name).name
                output.write_bytes(archive.read(name))
                try:
                    width, height, _ = read_png_rgba(output)
                except Exception:
                    continue
                score = width * height
                lowered = name.lower()
                if "sheet" in lowered or "spritesheet" in lowered or "walk" in lowered:
                    score *= 4
                candidates.append((score, name, output))
        if not candidates:
            raise ValueError(f"{path} did not contain a readable PNG")
        candidates.sort(reverse=True)
        chosen = candidates[0]
        print(f"selected {chosen[1]} from {path}")
        return chosen[2]


def load_source(path: Path) -> PixelSource:
    source_path = extract_png_from_zip(path) if path.suffix.lower() == ".zip" else path
    return read_png_rgba(source_path)


def alpha_bbox(pixels: ImagePixels, left: int, top: int, width: int, height: int) -> tuple[int, int, int, int] | None:
    xs: list[int] = []
    ys: list[int] = []
    for y in range(top, min(top + height, len(pixels))):
        row = pixels[y]
        for x in range(left, min(left + width, len(row))):
            if row[x][3] > 10:
                xs.append(x)
                ys.append(y)
    if not xs:
        return None
    return min(xs), min(ys), max(xs) + 1, max(ys) + 1


def nearest_sample(source: ImagePixels, src_left: int, src_top: int, src_w: int, src_h: int, out_w: int, out_h: int) -> ImagePixels:
    out = blank(out_w, out_h)
    if src_w <= 0 or src_h <= 0 or out_w <= 0 or out_h <= 0:
        return out
    for y in range(out_h):
        sy = src_top + min(src_h - 1, int((y + 0.5) * src_h / out_h))
        for x in range(out_w):
            sx = src_left + min(src_w - 1, int((x + 0.5) * src_w / out_w))
            out[y][x] = source[sy][sx]
    return out


def blit(dest: ImagePixels, src: ImagePixels, left: int, top: int) -> None:
    for y, row in enumerate(src):
        dy = top + y
        if dy < 0 or dy >= len(dest):
            continue
        for x, pixel in enumerate(row):
            dx = left + x
            if dx < 0 or dx >= len(dest[dy]):
                continue
            if pixel[3] > 0:
                dest[dy][dx] = pixel


def copy_cell(source: ImagePixels, source_left: int, source_top: int, source_w: int, source_h: int) -> ImagePixels:
    cell = blank(source_w, source_h)
    for y in range(source_h):
        sy = source_top + y
        if sy < 0 or sy >= len(source):
            continue
        for x in range(source_w):
            sx = source_left + x
            if sx < 0 or sx >= len(source[sy]):
                continue
            cell[y][x] = source[sy][sx]
    return cell


def fit_cell(source: ImagePixels, source_left: int, source_top: int, source_w: int, source_h: int) -> ImagePixels:
    raw_cell = copy_cell(source, source_left, source_top, source_w, source_h)
    bbox = alpha_bbox(raw_cell, 0, 0, source_w, source_h)
    out = blank(TARGET_CELL_W, TARGET_CELL_H)
    if bbox is None:
        return out
    x0, y0, x1, y1 = bbox
    body_w = x1 - x0
    body_h = y1 - y0
    scale = min(MAX_BODY_W / body_w, MAX_BODY_H / body_h, 1.0 if source_w <= TARGET_CELL_W and source_h <= TARGET_CELL_H else 999.0)
    scaled_w = max(1, min(MAX_BODY_W, round(body_w * scale)))
    scaled_h = max(1, min(MAX_BODY_H, round(body_h * scale)))
    scaled = nearest_sample(raw_cell, x0, y0, body_w, body_h, scaled_w, scaled_h)
    dest_x = (TARGET_CELL_W - scaled_w) // 2
    dest_y = TARGET_CELL_H - scaled_h - 2
    blit(out, scaled, dest_x, dest_y)
    return out


def build_contract(width: int, height: int, pixels: ImagePixels, source_rows: int) -> ImagePixels:
    if width % DIRECTIONS != 0:
        raise ValueError(f"source width {width} is not divisible by {DIRECTIONS} directions")
    if height % source_rows != 0:
        raise ValueError(f"source height {height} is not divisible by {source_rows} rows")
    source_cell_w = width // DIRECTIONS
    source_cell_h = height // source_rows
    out = blank(TARGET_CELL_W * DIRECTIONS, TARGET_CELL_H * ROWS)
    for row in range(ROWS):
        source_row = min(row, source_rows - 1)
        for direction in range(DIRECTIONS):
            fitted = fit_cell(
                pixels,
                direction * source_cell_w,
                source_row * source_cell_h,
                source_cell_w,
                source_cell_h,
            )
            blit(out, fitted, direction * TARGET_CELL_W, row * TARGET_CELL_H)
    return out


def build_runtime(contract: ImagePixels) -> ImagePixels:
    out = blank(RUNTIME_CELL * DIRECTIONS, RUNTIME_CELL * ROWS)
    for row in range(ROWS):
        for direction in range(DIRECTIONS):
            cell = copy_cell(contract, direction * TARGET_CELL_W, row * TARGET_CELL_H, TARGET_CELL_W, TARGET_CELL_H)
            blit(out, cell, direction * RUNTIME_CELL + ((RUNTIME_CELL - TARGET_CELL_W) // 2), row * RUNTIME_CELL)
    return out


def import_pixellab(input_path: Path, output_dir: Path, output_stem: str, source_rows: int, remove_chroma: bool) -> int:
    width, height, pixels = load_source(input_path)
    normalized = [[normalize_pixel(pixel, remove_chroma) for pixel in row] for row in pixels]
    contract = build_contract(width, height, normalized, source_rows)
    runtime = build_runtime(contract)

    contract_path = output_dir / f"{output_stem}_32x64_8dir_4row.png"
    runtime_path = output_dir / f"{output_stem}_32x64_8dir_runtime.png"
    write_png_rgba(contract_path, TARGET_CELL_W * DIRECTIONS, TARGET_CELL_H * ROWS, contract)
    write_png_rgba(runtime_path, RUNTIME_CELL * DIRECTIONS, RUNTIME_CELL * ROWS, runtime)

    print(f"source: {width}x{height}, assumed {DIRECTIONS}x{source_rows} cells")
    print("direction order:")
    for index, direction in enumerate(DIRECTION_ORDER):
        print(f"  {index}: {direction}")
    print(f"wrote {contract_path}")
    print(f"wrote {runtime_path}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input", type=Path, help="PixelLab PNG sheet or downloaded ZIP containing PNGs")
    parser.add_argument("--output-dir", type=Path, default=Path("assets/art/sprites/player_v2/imported"))
    parser.add_argument("--output-stem", default="pixellab_player")
    parser.add_argument("--source-rows", type=int, default=4, help="number of animation rows in the source sheet")
    parser.add_argument("--chroma", action="store_true", help="remove bright green chroma pixels")
    args = parser.parse_args()

    try:
        return import_pixellab(args.input, args.output_dir, args.output_stem, args.source_rows, args.chroma)
    except Exception as exc:  # noqa: BLE001 - command-line tool should print concise failure
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
