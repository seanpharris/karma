#!/usr/bin/env python3
"""Validate and normalize Karma 8-direction character runtime sheets.

Usage examples:

  python tools/prepare_character_sheet.py validate assets/art/sprites/scifi_engineer_player_8dir.png
  python tools/prepare_character_sheet.py normalize input.png assets/art/sprites/scifi_medic_player_8dir.png --chroma

The expected runtime format is 256x288 RGBA: 8 columns x 9 rows of 32x32 frames.
"""

from __future__ import annotations

import argparse
import struct
import sys
import zlib
from pathlib import Path

FRAME = 32
COLUMNS = 8
ROWS = 9
WIDTH = FRAME * COLUMNS
HEIGHT = FRAME * ROWS

DIRECTIONS = [
    "front",
    "front-right",
    "right",
    "back-right",
    "back",
    "back-left",
    "left",
    "front-left",
]

ROW_NAMES = [
    "idle",
    "walk-1",
    "walk-2",
    "walk-3",
    "walk-4",
    "run/action-ready",
    "tool/use",
    "melee/impact",
    "interact/reach",
]

Pixel = tuple[int, int, int, int]
ImagePixels = list[list[Pixel]]


def read_png_rgba(path: Path) -> tuple[int, int, ImagePixels]:
    data = path.read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError(f"{path} is not a PNG")

    pos = 8
    width = height = None
    color_type = None
    palette: list[tuple[int, int, int]] = []
    palette_alpha: list[int] = []
    idat = b""
    while pos < len(data):
        length = struct.unpack(">I", data[pos:pos + 4])[0]
        chunk_type = data[pos + 4:pos + 8]
        chunk = data[pos + 8:pos + 8 + length]
        pos += 12 + length
        if chunk_type == b"IHDR":
            width, height, bit_depth, color_type, _, _, _ = struct.unpack(">IIBBBBB", chunk)
            if bit_depth != 8 or color_type not in (2, 3, 6):
                raise ValueError(f"unsupported PNG format bit_depth={bit_depth} color_type={color_type}")
        elif chunk_type == b"PLTE":
            palette = [
                (chunk[i], chunk[i + 1], chunk[i + 2])
                for i in range(0, len(chunk), 3)
            ]
        elif chunk_type == b"tRNS":
            # Per-palette-entry alpha (color_type 3) or transparent colour
            # selector (color_type 0/2). We only honour the indexed case.
            palette_alpha = list(chunk)
        elif chunk_type == b"IDAT":
            idat += chunk
        elif chunk_type == b"IEND":
            break

    if width is None or height is None or color_type is None:
        raise ValueError("PNG missing IHDR")

    if color_type == 6:
        channels = 4
    elif color_type == 2:
        channels = 3
    else:  # color_type == 3 (indexed)
        channels = 1
    bpp = channels
    stride = width * channels
    raw = zlib.decompress(idat)
    rows: list[bytearray] = []
    previous = bytearray(stride)
    cursor = 0
    for _ in range(height):
        filter_type = raw[cursor]
        cursor += 1
        current = bytearray(raw[cursor:cursor + stride])
        cursor += stride
        for i in range(stride):
            left = current[i - bpp] if i >= bpp else 0
            up = previous[i]
            up_left = previous[i - bpp] if i >= bpp else 0
            if filter_type == 1:
                current[i] = (current[i] + left) & 0xFF
            elif filter_type == 2:
                current[i] = (current[i] + up) & 0xFF
            elif filter_type == 3:
                current[i] = (current[i] + ((left + up) // 2)) & 0xFF
            elif filter_type == 4:
                predictor = left + up - up_left
                pa = abs(predictor - left)
                pb = abs(predictor - up)
                pc = abs(predictor - up_left)
                pr = left if pa <= pb and pa <= pc else up if pb <= pc else up_left
                current[i] = (current[i] + pr) & 0xFF
            elif filter_type != 0:
                raise ValueError(f"unsupported PNG filter {filter_type}")
        rows.append(current)
        previous = current

    pixels: ImagePixels = []
    for row in rows:
        out_row = []
        for x in range(width):
            if color_type == 3:
                idx = row[x]
                if idx >= len(palette):
                    out_row.append((0, 0, 0, 0))
                    continue
                r, g, b = palette[idx]
                a = palette_alpha[idx] if idx < len(palette_alpha) else 255
                out_row.append((r, g, b, a))
            else:
                base = x * channels
                if channels == 4:
                    out_row.append((row[base], row[base + 1], row[base + 2], row[base + 3]))
                else:
                    out_row.append((row[base], row[base + 1], row[base + 2], 255))
        pixels.append(out_row)
    return width, height, pixels


def write_png_rgba(path: Path, width: int, height: int, pixels: ImagePixels) -> None:
    def chunk(kind: bytes, payload: bytes) -> bytes:
        return (
            struct.pack(">I", len(payload))
            + kind
            + payload
            + struct.pack(">I", zlib.crc32(kind + payload) & 0xFFFFFFFF)
        )

    raw_rows = []
    for y in range(height):
        row = bytearray([0])
        for x in range(width):
            row.extend(pixels[y][x])
        raw_rows.append(bytes(row))

    png = bytearray(b"\x89PNG\r\n\x1a\n")
    png.extend(chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)))
    png.extend(chunk(b"IDAT", zlib.compress(b"".join(raw_rows), 9)))
    png.extend(chunk(b"IEND", b""))
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(bytes(png))


def is_chroma_green(pixel: Pixel) -> bool:
    r, g, b, a = pixel
    return a > 0 and g >= 150 and r <= 110 and b <= 110 and g > r * 1.6 and g > b * 1.6


def normalize_pixels(pixels: ImagePixels, remove_chroma: bool) -> ImagePixels:
    out: ImagePixels = []
    for row in pixels:
        out_row = []
        for pixel in row:
            if remove_chroma and is_chroma_green(pixel):
                out_row.append((0, 0, 0, 0))
            else:
                r, g, b, a = pixel
                out_row.append((r, g, b, a))
        out.append(out_row)
    return out


def frame_bbox(pixels: ImagePixels, col: int, row: int) -> tuple[int, int, int, int, int]:
    left = col * FRAME
    top = row * FRAME
    xs: list[int] = []
    ys: list[int] = []
    for y in range(top, top + FRAME):
        for x in range(left, left + FRAME):
            if pixels[y][x][3] > 10:
                xs.append(x - left)
                ys.append(y - top)
    if not xs:
        return 0, 0, 0, 0, 0
    return min(xs), min(ys), max(xs) + 1, max(ys) + 1, len(xs)


def validate(path: Path, verbose: bool = False) -> int:
    errors: list[str] = []
    warnings: list[str] = []
    width, height, pixels = read_png_rgba(path)

    if (width, height) != (WIDTH, HEIGHT):
        errors.append(f"expected {WIDTH}x{HEIGHT}, got {width}x{height}")

    transparent = sum(1 for row in pixels for _, _, _, a in row if a <= 10)
    opaque = (width * height) - transparent
    chroma = sum(1 for row in pixels for pixel in row if is_chroma_green(pixel))

    if transparent == 0:
        warnings.append("no transparent pixels found; run normalize with --chroma or export RGBA")
    if chroma > 0:
        warnings.append(f"found {chroma} chroma-green pixels; run normalize with --chroma")

    if (width, height) == (WIDTH, HEIGHT):
        for row in range(ROWS):
            for col in range(COLUMNS):
                x0, y0, x1, y1, count = frame_bbox(pixels, col, row)
                label = f"{ROW_NAMES[row]} {DIRECTIONS[col]}"
                if count == 0:
                    errors.append(f"empty frame: row {row} col {col} ({label})")
                    continue
                if x0 == 0 or y0 == 0 or x1 == FRAME or y1 == FRAME:
                    warnings.append(f"frame touches cell edge: row {row} col {col} ({label}) bbox=({x0},{y0},{x1},{y1})")
                if verbose:
                    print(f"{row},{col} {label}: bbox=({x0},{y0},{x1},{y1}) pixels={count}")

    print(f"{path}: {width}x{height}, opaque={opaque}, transparent={transparent}, chroma_green={chroma}")
    for warning in warnings:
        print(f"WARN: {warning}")
    for error in errors:
        print(f"ERROR: {error}")
    return 1 if errors else 0


def normalize(input_path: Path, output_path: Path, remove_chroma: bool) -> int:
    width, height, pixels = read_png_rgba(input_path)
    if (width, height) != (WIDTH, HEIGHT):
        print(f"ERROR: expected {WIDTH}x{HEIGHT}, got {width}x{height}", file=sys.stderr)
        return 1
    normalized = normalize_pixels(pixels, remove_chroma)
    write_png_rgba(output_path, width, height, normalized)
    print(f"wrote {output_path}")
    return validate(output_path)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)

    validate_parser = subparsers.add_parser("validate", help="validate a runtime sheet")
    validate_parser.add_argument("path", type=Path)
    validate_parser.add_argument("--verbose", action="store_true")

    normalize_parser = subparsers.add_parser("normalize", help="normalize a runtime sheet into RGBA PNG")
    normalize_parser.add_argument("input", type=Path)
    normalize_parser.add_argument("output", type=Path)
    normalize_parser.add_argument("--chroma", action="store_true", help="remove bright green chroma-key pixels")

    args = parser.parse_args()
    if args.command == "validate":
        return validate(args.path, args.verbose)
    if args.command == "normalize":
        return normalize(args.input, args.output, args.chroma)
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
