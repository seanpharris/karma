#!/usr/bin/env python3
"""Extract a generated presentation character sheet into a Karma runtime candidate.

This is intentionally non-destructive by default. It handles the common case
where image generation produced a clean-ish grid of isolated poses on a baked
checker/white background, but not the exact Karma 8x9 runtime sheet.
"""

from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from prepare_character_sheet import FRAME, HEIGHT, WIDTH, read_png_rgba, write_png_rgba  # noqa: E402

Pixel = tuple[int, int, int, int]


def is_white_background(pixel: Pixel) -> bool:
    r, g, b, a = pixel
    if a <= 10:
        return True
    mx = max(r, g, b)
    mn = min(r, g, b)
    saturation = mx - mn
    return mx >= 190 and saturation <= 38


def is_foreground(pixel: Pixel) -> bool:
    r, g, b, a = pixel
    if a <= 10 or is_white_background(pixel):
        return False
    mx = max(r, g, b)
    mn = min(r, g, b)
    saturation = mx - mn
    # Generated drops often have a baked light checker/white background. Keep
    # dark outlines and saturated character colors, but explicitly null white /
    # low-saturation checker pixels before repairing any internal alpha holes.
    return mx < 180 or (saturation > 38 and mx < 240)


def find_components(pixels: list[list[Pixel]], width: int, height: int):
    mask = [[is_foreground(pixels[y][x]) for x in range(width)] for y in range(height)]
    seen = [[False] * width for _ in range(height)]
    components = []
    for y in range(height):
        for x in range(width):
            if seen[y][x] or not mask[y][x]:
                continue
            q = deque([(x, y)])
            seen[y][x] = True
            xs = []
            ys = []
            while q:
                cx, cy = q.popleft()
                xs.append(cx)
                ys.append(cy)
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if 0 <= nx < width and 0 <= ny < height and not seen[ny][nx] and mask[ny][nx]:
                        seen[ny][nx] = True
                        q.append((nx, ny))
            if len(xs) >= 500:
                components.append((min(xs), min(ys), max(xs) + 1, max(ys) + 1, len(xs)))
    return components


def crop_clean(pixels, box):
    left, top, right, bottom, _ = box
    pad = 4
    left = max(0, left - pad)
    top = max(0, top - pad)
    right = min(len(pixels[0]), right + pad)
    bottom = min(len(pixels), bottom + pad)
    out = []
    for y in range(top, bottom):
        row = []
        for x in range(left, right):
            pixel = pixels[y][x]
            row.append(pixel if is_foreground(pixel) else (0, 0, 0, 0))
        out.append(row)
    return out


def resize_nearest(frame, target_w: int, target_h: int):
    src_h = len(frame)
    src_w = len(frame[0])
    return [[frame[min(src_h - 1, int(y * src_h / target_h))][min(src_w - 1, int(x * src_w / target_w))] for x in range(target_w)] for y in range(target_h)]


def repair_alpha_pinholes(frame):
    out = [row[:] for row in frame]
    for _ in range(4):
        changed = False
        source = [row[:] for row in out]
        for y in range(1, FRAME - 1):
            for x in range(1, FRAME - 1):
                if source[y][x][3] > 10:
                    continue

                neighbors = []
                for yy in range(y - 1, y + 2):
                    for xx in range(x - 1, x + 2):
                        if xx == x and yy == y:
                            continue
                        pixel = source[yy][xx]
                        if pixel[3] > 10:
                            neighbors.append(pixel)

                horizontally_enclosed = source[y][x - 1][3] > 10 and source[y][x + 1][3] > 10
                vertically_enclosed = source[y - 1][x][3] > 10 and source[y + 1][x][3] > 10
                if len(neighbors) >= 4 and (horizontally_enclosed or vertically_enclosed):
                    out[y][x] = (
                        round(sum(pixel[0] for pixel in neighbors) / len(neighbors)),
                        round(sum(pixel[1] for pixel in neighbors) / len(neighbors)),
                        round(sum(pixel[2] for pixel in neighbors) / len(neighbors)),
                        max(pixel[3] for pixel in neighbors),
                    )
                    changed = True
        if not changed:
            break
    return out


def remove_tiny_components(frame, min_pixels: int = 3):
    seen = [[False] * FRAME for _ in range(FRAME)]
    out = [row[:] for row in frame]
    for y in range(FRAME):
        for x in range(FRAME):
            if seen[y][x] or out[y][x][3] <= 10:
                continue
            q = deque([(x, y)])
            seen[y][x] = True
            pixels = []
            while q:
                cx, cy = q.popleft()
                pixels.append((cx, cy))
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if 0 <= nx < FRAME and 0 <= ny < FRAME and not seen[ny][nx] and out[ny][nx][3] > 10:
                        seen[ny][nx] = True
                        q.append((nx, ny))
            if len(pixels) < min_pixels:
                for px, py in pixels:
                    out[py][px] = (0, 0, 0, 0)
    return out


def normalize_frame(frame):
    src_h = len(frame)
    src_w = len(frame[0])
    # Leave some breathing room for weapons/tools, but keep body readable.
    scale = min(28 / max(1, src_w), 30 / max(1, src_h), 1.0)
    target_w = max(1, round(src_w * scale))
    target_h = max(1, round(src_h * scale))
    scaled = resize_nearest(frame, target_w, target_h)
    out = [[(0, 0, 0, 0) for _ in range(FRAME)] for _ in range(FRAME)]
    ox = (FRAME - target_w) // 2
    oy = FRAME - target_h - 1
    for y in range(target_h):
        for x in range(target_w):
            out[oy + y][ox + x] = scaled[y][x]
    return remove_tiny_components(repair_alpha_pinholes(out))


def mirror_frame(frame):
    return [list(reversed(row)) for row in frame]


def paste(dst, frame, col: int, row: int):
    ox = col * FRAME
    oy = row * FRAME
    for y in range(FRAME):
        for x in range(FRAME):
            dst[oy + y][ox + x] = frame[y][x]


def group_components(components):
    rows = []
    for box in sorted(components, key=lambda b: ((b[1] + b[3]) / 2, b[0])):
        cy = (box[1] + box[3]) / 2
        for row in rows:
            row_cy = sum((b[1] + b[3]) / 2 for b in row) / len(row)
            if abs(cy - row_cy) < 70:
                row.append(box)
                break
        else:
            rows.append([box])
    return [sorted(row, key=lambda b: b[0]) for row in rows]


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("input", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()

    width, height, pixels = read_png_rgba(args.input)
    components = find_components(pixels, width, height)
    grouped = group_components(components)
    print(f"found {len(components)} pose components in {len(grouped)} rows")
    print("row lengths:", [len(row) for row in grouped])

    if len(grouped) < 5:
        print("ERROR: not enough rows detected", file=sys.stderr)
        return 1

    frames = [[normalize_frame(crop_clean(pixels, box)) for box in row] for row in grouped]
    output = [[(0, 0, 0, 0) for _ in range(WIDTH)] for _ in range(HEIGHT)]

    # The generated drop has 8 rows, but source row 5 (1-indexed) contains the
    # wrong model/action pose for the walk cycle. Do not use it anywhere in the
    # runtime sheet. Reuse source row 3 as the fourth walk row, then map later
    # rows to action placeholders.
    runtime_to_source_row = [0, 1, 2, 3, 2, 5, 6, 7, 7]
    for runtime_row, source_row in enumerate(runtime_to_source_row):
        source_row = min(source_row, len(frames) - 1)
        source_frames = frames[source_row]
        if len(source_frames) < 7:
            print(f"ERROR: source row {source_row} only has {len(source_frames)} frames", file=sys.stderr)
            return 1

        # The generated drop is 7 columns. Treat them as front, front-right,
        # right, back-right, back, back-left, left. Generate front-left by
        # mirroring front-right as a temporary candidate.
        runtime_frames = [
            source_frames[0],
            source_frames[1],
            source_frames[2],
            source_frames[3],
            source_frames[4],
            source_frames[5],
            source_frames[6],
            mirror_frame(source_frames[1]),
        ]
        for col, frame in enumerate(runtime_frames):
            paste(output, frame, col, runtime_row)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    write_png_rgba(args.output, WIDTH, HEIGHT, output)
    print(f"wrote {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
