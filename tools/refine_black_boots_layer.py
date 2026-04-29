#!/usr/bin/env python3
"""Refine the current black boots layer against the real 32x64 base model.

This is intentionally conservative: it keeps the generated boot silhouette, but
normalizes the black ramp, removes noisy bright pixels, fills tiny holes, and
adds a controlled cuff/toe highlight so the boots read better in-game.
"""

from __future__ import annotations

from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from prepare_character_sheet import read_png_rgba, write_png_rgba  # noqa: E402

ROOT = Path("assets/art/sprites/player_v2")
BASE = ROOT / "imported/player_base_body_sheet_32x64_8dir_4row.png"
IN_BOOT = ROOT / "layers_32x64/boots_black_32x64.png"
OUT_BOOT = ROOT / "layers_32x64/boots_black_32x64.png"
OUT_IMPORT = ROOT / "imported/boots_black_layer_32x64_8dir_4row.png"
REVIEW = ROOT / "review/external_layers/boots_black_refined_review_32x64.png"

W, H = 256, 256
FW, FH = 32, 64
COLS, ROWS = 8, 4
TRANSPARENT = (0, 0, 0, 0)
OUTLINE = (16, 18, 21, 255)
DARK = (24, 27, 31, 255)
MID = (37, 42, 48, 255)
HI = (68, 76, 84, 245)
CUFF = (30, 34, 39, 245)

# Direction columns: south, south-east, east, north-east, north, north-west, west, south-west.
SIDE_SIGN = {1: 1, 2: 1, 3: 1, 5: -1, 6: -1, 7: -1}

Pixel = tuple[int, int, int, int]
Image = list[list[Pixel]]


def is_visible(px: Pixel, alpha: int = 24) -> bool:
    return px[3] >= alpha


def is_skin(px: Pixel) -> bool:
    r, g, b, a = px
    return a > 60 and r > 100 and g > 55 and b > 35 and r > b + 24 and g > b + 8


def neighbors(mask: list[list[bool]], x: int, y: int) -> int:
    c = 0
    for yy in range(max(0, y - 1), min(FH, y + 2)):
        for xx in range(max(0, x - 1), min(FW, x + 2)):
            if xx == x and yy == y:
                continue
            c += 1 if mask[yy][xx] else 0
    return c


def frame_bounds(mask: list[list[bool]]) -> tuple[int, int, int, int] | None:
    xs = [x for y in range(FH) for x in range(FW) if mask[y][x]]
    ys = [y for y in range(FH) for x in range(FW) if mask[y][x]]
    if not xs:
        return None
    return min(xs), min(ys), max(xs), max(ys)


def normalize_color(px: Pixel, x: int, y: int, bounds: tuple[int, int, int, int] | None, col: int) -> Pixel:
    if not is_visible(px):
        return TRANSPARENT
    r, g, b, a = px
    lum = (r + g + b) // 3
    if lum > 116:
        return TRANSPARENT
    if bounds is None:
        return DARK
    minx, miny, maxx, maxy = bounds
    # Controlled highlights only: toe/outer edge and cuff, not noisy soles.
    if y <= miny + 1:
        return CUFF
    if col in SIDE_SIGN:
        sign = SIDE_SIGN[col]
        toe_edge = maxx if sign > 0 else minx
        if abs(x - toe_edge) <= 1 and y >= max(miny + 3, maxy - 8) and y <= maxy - 2:
            return HI
    else:
        if y >= maxy - 7 and y <= maxy - 3 and (x == minx + 1 or x == maxx - 1):
            return HI
    if x in (minx, maxx) or y == maxy:
        return OUTLINE
    return DARK if lum < 70 else MID


def refine_frame(base: Image, boot: Image, col: int, row: int) -> list[list[Pixel]]:
    ox, oy = col * FW, row * FH
    src_mask = [[is_visible(boot[oy + y][ox + x]) for x in range(FW)] for y in range(FH)]

    # Remove isolated speckles/noisy transparent fringe, but do not expand the
    # silhouette much. The generated boots are already a bit chunky.
    mask = [[src_mask[y][x] and neighbors(src_mask, x, y) >= 1 for x in range(FW)] for y in range(FH)]

    bounds = frame_bounds(mask)
    out = [[TRANSPARENT for _ in range(FW)] for __ in range(FH)]
    if bounds is None:
        return out

    minx, miny, maxx, maxy = bounds

    # Cover only the most obvious skin/toe leaks immediately enclosed by boot
    # pixels near the lower foot. Avoid adding calf bulk.
    for y in range(max(0, miny), min(FH, maxy + 1)):
        for x in range(max(0, minx), min(FW, maxx + 1)):
            if mask[y][x]:
                continue
            if is_skin(base[oy + y][ox + x]) and neighbors(mask, x, y) >= 4:
                mask[y][x] = True

    # Preserve leg separation: carve a narrow transparent notch for front/back frames when both boots merge.
    if col in (0, 4) and row in (0, 1, 2, 3):
        bounds = frame_bounds(mask)
        if bounds is not None:
            minx, miny, maxx, maxy = bounds
            cx = (minx + maxx) // 2
            for y in range(miny + 3, maxy - 1):
                # Only carve if it is a very dense blob across the center.
                if 0 < cx < FW - 1 and mask[y][cx - 1] and mask[y][cx] and mask[y][cx + 1]:
                    mask[y][cx] = False

    bounds = frame_bounds(mask)
    for y in range(FH):
        for x in range(FW):
            if not mask[y][x]:
                continue
            source = boot[oy + y][ox + x] if src_mask[y][x] else DARK
            out[y][x] = normalize_color(source, x, y, bounds, col)

    return out


def paste(dst: Image, frame: list[list[Pixel]], col: int, row: int) -> None:
    ox, oy = col * FW, row * FH
    for y in range(FH):
        for x in range(FW):
            dst[oy + y][ox + x] = frame[y][x]


def blend(dst: Pixel, src: Pixel) -> Pixel:
    if src[3] == 0:
        return dst
    a = src[3] / 255.0
    dr, dg, db, da = dst
    sr, sg, sb, sa = src
    return (int(dr * (1 - a) + sr * a), int(dg * (1 - a) + sg * a), int(db * (1 - a) + sb * a), max(da, sa))


def make_review(base: Image, old: Image, new: Image) -> Image:
    scale = 3
    groups = 4  # base, old boot, new boot, composite
    rw, rh = W * scale, H * groups * scale
    out = [[(24, 24, 24, 255) for _ in range(rw)] for __ in range(rh)]
    def draw(src: Image, yoff: int, composite: bool = False) -> None:
        for y in range(H):
            for x in range(W):
                c = src[y][x]
                if composite:
                    c = blend(base[y][x], c)
                bg = (190, 190, 190, 255) if ((x // 8 + y // 8) % 2 == 0) else (90, 90, 90, 255)
                if c[3] < 255:
                    a = c[3] / 255.0
                    c = (int(bg[0] * (1 - a) + c[0] * a), int(bg[1] * (1 - a) + c[1] * a), int(bg[2] * (1 - a) + c[2] * a), 255)
                for sy in range(scale):
                    for sx in range(scale):
                        out[yoff + y * scale + sy][x * scale + sx] = c
    draw(base, 0)
    draw(old, H * scale)
    draw(new, H * scale * 2)
    draw(new, H * scale * 3, composite=True)
    return out


def main() -> int:
    bw, bh, base = read_png_rgba(BASE)
    ow, oh, old = read_png_rgba(IN_BOOT)
    if (bw, bh) != (W, H) or (ow, oh) != (W, H):
        raise SystemExit("expected 256x256 base and boot sheets")

    new = [[TRANSPARENT for _ in range(W)] for __ in range(H)]
    for row in range(ROWS):
        for col in range(COLS):
            paste(new, refine_frame(base, old, col, row), col, row)

    write_png_rgba(OUT_BOOT, W, H, new)
    write_png_rgba(OUT_IMPORT, W, H, new)
    REVIEW.parent.mkdir(parents=True, exist_ok=True)
    write_png_rgba(REVIEW, W, H * 4, make_review(base, old, new))
    print(f"wrote {OUT_BOOT}")
    print(f"wrote {REVIEW}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
