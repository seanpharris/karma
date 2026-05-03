#!/usr/bin/env python3
"""Export a clean 8-direction runtime sheet from the current engineer source sheet.

This is a temporary bridge asset generator. The current source only has four true
movement directions, so diagonal columns are generated as visibly distinct hybrid
placeholders from the nearest cardinal frames. Replace this with true diagonal art
once a base-body/skin pipeline exists.

Output layout: 8 columns x 9 rows, 32x32 RGBA frames, 256x288 total.
"""

from __future__ import annotations

import struct
import sys
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets/art/sprites/scifi_engineer_player_sheet.png"
OUTPUT = ROOT / "assets/art/sprites/scifi_engineer_player_8dir.png"
FRAME = 32
COLUMNS = 8
ROWS = 9

# Source sheet frame boxes, discovered from alpha bounds in the generated sheet.
# Source columns are: front, back, left, right. Source rows are idle, walk-front,
# walk-back, walk-left, walk-right.
SOURCE_ORIGIN_X = 288
SOURCE_ORIGIN_Y = 20
SOURCE_STEP_X = 225
SOURCE_STEP_Y = 210
SOURCE_W = 120
SOURCE_H = 190

# Runtime columns: front, front-right, right, back-right, back, back-left, left, front-left.
# Source columns are: front, back, left, right. Source walk rows are: front, back,
# left, right. Diagonals are temporary hybrid placeholders until true diagonal
# frames land.
DIRECTION_TO_SOURCE_COLUMN = [0, None, 3, None, 1, None, 2, None]
DIRECTION_TO_SOURCE_WALK_ROW = [1, None, 4, None, 2, None, 3, None]
DIAGONAL_IDLE_SOURCES = {
    1: (0, 3),  # front-right = front + right
    3: (1, 3),  # back-right = back + right
    5: (1, 2),  # back-left = back + left
    7: (0, 2),  # front-left = front + left
}
DIAGONAL_WALK_SOURCES = {
    1: (1, 4),  # front walk + right walk
    3: (2, 4),  # back walk + right walk
    5: (2, 3),  # back walk + left walk
    7: (1, 3),  # front walk + left walk
}


def read_png_rgba(path: Path) -> tuple[int, int, list[list[tuple[int, int, int, int]]]]:
    data = path.read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError(f"{path} is not a PNG")

    pos = 8
    width = height = None
    color_type = None
    idat = b""
    while pos < len(data):
        length = struct.unpack(">I", data[pos:pos + 4])[0]
        chunk_type = data[pos + 4:pos + 8]
        chunk = data[pos + 8:pos + 8 + length]
        pos += 12 + length
        if chunk_type == b"IHDR":
            width, height, bit_depth, color_type, _, _, _ = struct.unpack(">IIBBBBB", chunk)
            if bit_depth != 8 or color_type not in (2, 6):
                raise ValueError(f"unsupported PNG format bit_depth={bit_depth} color_type={color_type}")
        elif chunk_type == b"IDAT":
            idat += chunk
        elif chunk_type == b"IEND":
            break

    if width is None or height is None or color_type is None:
        raise ValueError("PNG missing IHDR")

    channels = 4 if color_type == 6 else 3
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

    pixels: list[list[tuple[int, int, int, int]]] = []
    for row in rows:
        out_row = []
        for x in range(width):
            base = x * channels
            if channels == 4:
                out_row.append((row[base], row[base + 1], row[base + 2], row[base + 3]))
            else:
                out_row.append((row[base], row[base + 1], row[base + 2], 255))
        pixels.append(out_row)
    return width, height, pixels


def write_png_rgba(path: Path, width: int, height: int, pixels: list[list[tuple[int, int, int, int]]]) -> None:
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
    path.write_bytes(bytes(png))


def crop(pixels, x: int, y: int, width: int, height: int):
    return [[pixels[yy][xx] for xx in range(x, x + width)] for yy in range(y, y + height)]


def is_background(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, a = pixel
    if a <= 10:
        return True

    # The runtime source sheet is already RGBA, and the character uses dark
    # outlines. Do not treat black/dark pixels as background or the exporter
    # will punch holes in the sprite. Only remove transparent pixels and obvious
    # generated chroma green.
    is_chroma_green = g >= 150 and r <= 100 and b <= 100 and g > r * 1.8 and g > b * 1.8
    return is_chroma_green


def clean_pixel(pixel: tuple[int, int, int, int]) -> tuple[int, int, int, int]:
    return (0, 0, 0, 0) if is_background(pixel) else pixel


def alpha_bbox(frame):
    xs = []
    ys = []
    for y, row in enumerate(frame):
        for x, pixel in enumerate(row):
            if not is_background(pixel):
                xs.append(x)
                ys.append(y)
    if not xs:
        return 0, 0, len(frame[0]), len(frame)
    return min(xs), min(ys), max(xs) + 1, max(ys) + 1


def resize_nearest(frame, width: int, height: int):
    src_h = len(frame)
    src_w = len(frame[0])
    out = []
    for y in range(height):
        sy = min(src_h - 1, int(y * src_h / height))
        row = []
        for x in range(width):
            sx = min(src_w - 1, int(x * src_w / width))
            row.append(frame[sy][sx])
        out.append(row)
    return out


def normalized_frame(source_pixels, source_col: int, source_row: int):
    x = SOURCE_ORIGIN_X + source_col * SOURCE_STEP_X
    y = SOURCE_ORIGIN_Y + source_row * SOURCE_STEP_Y
    frame = crop(source_pixels, x, y, SOURCE_W, SOURCE_H)
    left, top, right, bottom = alpha_bbox(frame)
    trimmed = [[clean_pixel(pixel) for pixel in row] for row in crop(frame, left, top, right - left, bottom - top)]

    target_h = 30
    target_w = max(1, min(24, round((right - left) * target_h / max(1, bottom - top))))
    scaled = resize_nearest(trimmed, target_w, target_h)
    out = [[(0, 0, 0, 0) for _ in range(FRAME)] for _ in range(FRAME)]
    ox = (FRAME - target_w) // 2
    oy = FRAME - target_h - 1
    for yy in range(target_h):
        for xx in range(target_w):
            out[oy + yy][ox + xx] = clean_pixel(scaled[yy][xx])
    return out


def blend_pixel(base: tuple[int, int, int, int], overlay: tuple[int, int, int, int], overlay_strength: float):
    br, bg, bb, ba = base
    or_, og, ob, oa = overlay
    if ba <= 10:
        return overlay
    if oa <= 10:
        return base

    strength = max(0.0, min(1.0, overlay_strength))
    return (
        round((br * (1.0 - strength)) + (or_ * strength)),
        round((bg * (1.0 - strength)) + (og * strength)),
        round((bb * (1.0 - strength)) + (ob * strength)),
        max(ba, oa),
    )


def shift_frame(frame, dx: int, dy: int = 0):
    out = [[(0, 0, 0, 0) for _ in range(FRAME)] for _ in range(FRAME)]
    for y in range(FRAME):
        for x in range(FRAME):
            tx = x + dx
            ty = y + dy
            if 0 <= tx < FRAME and 0 <= ty < FRAME:
                out[ty][tx] = frame[y][x]
    return out


def hybrid_diagonal(front_or_back_frame, side_frame, side_on_right: bool):
    """Create a visible temporary diagonal cue without drawing two silhouettes.

    The side-facing frame owns the silhouette, so the diagonal never looks like
    two models standing on top of each other. The silhouette is nudged one pixel
    toward its visible side and then strongly tinted only where the side sprite
    already has opaque pixels. No new body pixels are introduced.
    """

    side_frame = shift_frame(side_frame, 1 if side_on_right else -1)
    out = [[(0, 0, 0, 0) for _ in range(FRAME)] for _ in range(FRAME)]
    center = (FRAME - 1) / 2.0
    for y in range(FRAME):
        for x in range(FRAME):
            side_pixel = side_frame[y][x]
            if side_pixel[3] <= 10:
                continue

            front_or_back_pixel = front_or_back_frame[y][x]
            center_bias = 1.0 - min(1.0, abs(x - center) / center)
            visible_side_bias = max(0.0, (x - center) / center if side_on_right else (center - x) / center)
            front_back_strength = 0.34 + (0.24 * center_bias) - (0.06 * visible_side_bias)
            out[y][x] = blend_pixel(side_pixel, front_or_back_pixel, front_back_strength) if front_or_back_pixel[3] > 10 else side_pixel
    return out


def direction_idle_frame(source_pixels, direction: int):
    source_col = DIRECTION_TO_SOURCE_COLUMN[direction]
    if source_col is not None:
        return normalized_frame(source_pixels, source_col, 0)

    front_or_back_col, side_col = DIAGONAL_IDLE_SOURCES[direction]
    return hybrid_diagonal(
        normalized_frame(source_pixels, front_or_back_col, 0),
        normalized_frame(source_pixels, side_col, 0),
        side_on_right=direction in (1, 3))


def direction_walk_frame(source_pixels, direction: int, walk_frame: int):
    source_walk_row = DIRECTION_TO_SOURCE_WALK_ROW[direction]
    if source_walk_row is not None:
        return normalized_frame(source_pixels, walk_frame, source_walk_row)

    front_or_back_row, side_row = DIAGONAL_WALK_SOURCES[direction]
    return hybrid_diagonal(
        normalized_frame(source_pixels, walk_frame, front_or_back_row),
        normalized_frame(source_pixels, walk_frame, side_row),
        side_on_right=direction in (1, 3))


def paste(dst, frame, column: int, row: int):
    ox = column * FRAME
    oy = row * FRAME
    for y in range(FRAME):
        for x in range(FRAME):
            dst[oy + y][ox + x] = frame[y][x]


def main() -> int:
    if not SOURCE.exists():
        print(f"missing source: {SOURCE}", file=sys.stderr)
        return 1

    _, _, source_pixels = read_png_rgba(SOURCE)
    out_w = COLUMNS * FRAME
    out_h = ROWS * FRAME
    output_pixels = [[(0, 0, 0, 0) for _ in range(out_w)] for _ in range(out_h)]

    for direction in range(COLUMNS):
        idle_frame = direction_idle_frame(source_pixels, direction)
        paste(output_pixels, idle_frame, direction, 0)

        for walk_frame in range(4):
            paste(output_pixels, direction_walk_frame(source_pixels, direction, walk_frame), direction, 1 + walk_frame)

        # Placeholder action rows reuse each direction's idle frame for now,
        # preserving the runtime layout while still reflecting all 8 columns.
        for action_row in range(5, ROWS):
            paste(output_pixels, idle_frame, direction, action_row)

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    write_png_rgba(OUTPUT, out_w, out_h, output_pixels)
    print(f"wrote {OUTPUT} ({out_w}x{out_h})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
