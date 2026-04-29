#!/usr/bin/env python3
"""Final readability polish for the 32x64 black boots layer.

Runs after the base-aligned sculpt pass. It focuses on sprite readability: cover
remaining 1px skin leaks, trim flipper-like endpoints, add per-component toe/cuff
highlights, and keep a tiny seam where boots merge.
"""

from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from prepare_character_sheet import read_png_rgba, write_png_rgba  # noqa: E402

ROOT = Path("assets/art/sprites/player_v2")
BASE = ROOT / "imported/player_base_body_sheet_32x64_8dir_4row.png"
BOOT = ROOT / "layers_32x64/boots_black_32x64.png"
REVIEW = ROOT / "review/external_layers/boots_black_final_polish_review_32x64.png"
FW, FH, COLS, ROWS = 32, 64, 8, 4
TRANSPARENT = (0, 0, 0, 0)
OUTLINE = (13, 15, 18, 255)
DARK = (23, 26, 30, 255)
MID = (38, 43, 50, 255)
HI = (76, 84, 92, 245)
CUFF = (34, 38, 44, 245)
DIR_SIGN = {1: 1, 2: 1, 3: 1, 5: -1, 6: -1, 7: -1}

Pixel = tuple[int, int, int, int]
Image = list[list[Pixel]]


def vis(px: Pixel) -> bool:
    return px[3] > 20


def skin(px: Pixel) -> bool:
    r, g, b, a = px
    return a > 60 and r > 100 and g > 50 and b > 30 and r > b + 20 and g > b + 5


def neigh(mask: list[list[bool]], x: int, y: int, r: int = 1) -> int:
    c = 0
    for yy in range(max(0, y - r), min(FH, y + r + 1)):
        for xx in range(max(0, x - r), min(FW, x + r + 1)):
            if xx == x and yy == y:
                continue
            if mask[yy][xx]:
                c += 1
    return c


def close(mask: list[list[bool]], x: int, y: int, r: int) -> bool:
    for yy in range(max(0, y - r), min(FH, y + r + 1)):
        for xx in range(max(0, x - r), min(FW, x + r + 1)):
            if mask[yy][xx]:
                return True
    return False


def components(mask: list[list[bool]]) -> list[list[tuple[int, int]]]:
    seen = set()
    out = []
    for y in range(FH):
        for x in range(FW):
            if not mask[y][x] or (x, y) in seen:
                continue
            stack = [(x, y)]
            seen.add((x, y))
            pts = []
            while stack:
                px, py = stack.pop()
                pts.append((px, py))
                for nx, ny in ((px + 1, py), (px - 1, py), (px, py + 1), (px, py - 1)):
                    if 0 <= nx < FW and 0 <= ny < FH and mask[ny][nx] and (nx, ny) not in seen:
                        seen.add((nx, ny))
                        stack.append((nx, ny))
            out.append(pts)
    return out


def bbox(pts: list[tuple[int, int]]) -> tuple[int, int, int, int]:
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    return min(xs), min(ys), max(xs), max(ys)


def polish_frame(base: Image, boot: Image, col: int, row: int) -> list[list[Pixel]]:
    ox, oy = col * FW, row * FH
    mask = [[vis(boot[oy + y][ox + x]) for x in range(FW)] for y in range(FH)]

    # Kill tiny detached specks first.
    for pts in components(mask):
        if len(pts) < 4:
            for x, y in pts:
                mask[y][x] = False

    # Cover remaining one-pixel skin leaks near the lower foot/heel.
    for y in range(44, FH):
        for x in range(FW):
            if skin(base[oy + y][ox + x]) and not mask[y][x] and close(mask, x, y, 2):
                mask[y][x] = True

    # Trim flipper-like side/diagonal endpoints that are not covering skin and
    # only exist as horizontal protrusions.
    # Slightly stricter than the sculpt pass: the previous polish fixed leaks but
    # made profile frames a little galosh-like. Keep front/back wider, but slim
    # side and 3/4 stride silhouettes unless they are covering actual skin.
    max_width = 14 if col in (1, 3, 5, 7) else (13 if col in (2, 6) else 15)
    pts_all = [(x, y) for y in range(FH) for x in range(FW) if mask[y][x]]
    if pts_all:
        minx, miny, maxx, maxy = bbox(pts_all)
        guard = 0
        while maxx - minx + 1 > max_width and guard < 8:
            guard += 1
            trim_x = minx if sum(mask[y][minx] for y in range(miny, maxy + 1)) <= sum(mask[y][maxx] for y in range(miny, maxy + 1)) else maxx
            for y in range(miny, maxy + 1):
                if not skin(base[oy + y][ox + trim_x]) or neigh(mask, trim_x, y) <= 3:
                    mask[y][trim_x] = False
            pts_all = [(x, y) for y in range(FH) for x in range(FW) if mask[y][x]]
            if not pts_all:
                break
            minx, miny, maxx, maxy = bbox(pts_all)

    # Preserve boot separation in front/back and dense diagonal overlaps.
    pts_all = [(x, y) for y in range(FH) for x in range(FW) if mask[y][x]]
    if pts_all:
        minx, miny, maxx, maxy = bbox(pts_all)
        if maxx - minx + 1 >= 11:
            cx = (minx + maxx) // 2
            for y in range(miny + 2, min(maxy, 52)):
                if 0 < cx < FW - 1 and mask[y][cx - 1] and mask[y][cx] and mask[y][cx + 1]:
                    # Don't uncover actual skin; move seam only where it is a pure merge.
                    if not skin(base[oy + y][ox + cx]):
                        mask[y][cx] = False

    # Re-drop any specks introduced by trimming/seams.
    kept = [[False for _ in range(FW)] for __ in range(FH)]
    for pts in components(mask):
        if len(pts) >= 4:
            for x, y in pts:
                kept[y][x] = True
    mask = kept

    out = [[TRANSPARENT for _ in range(FW)] for __ in range(FH)]
    comps = [pts for pts in components(mask) if len(pts) >= 4]
    for pts in comps:
        minx, miny, maxx, maxy = bbox(pts)
        sign = DIR_SIGN.get(col, 0)
        toe_x = maxx if sign > 0 else minx if sign < 0 else None
        for x, y in pts:
            edge = x in (minx, maxx) or y == maxy
            if y <= miny + 1:
                c = CUFF
            elif toe_x is not None and abs(x - toe_x) <= 1 and maxy - 6 <= y <= maxy - 2:
                c = HI
            elif toe_x is None and y >= maxy - 6 and (x == minx + 1 or x == maxx - 1):
                c = HI
            elif edge:
                c = OUTLINE
            else:
                c = MID if neigh(mask, x, y) <= 4 else DARK
            out[y][x] = c
    return out


def paste(dst: Image, frame: list[list[Pixel]], col: int, row: int) -> None:
    ox, oy = col * FW, row * FH
    for y in range(FH):
        for x in range(FW):
            dst[oy + y][ox + x] = frame[y][x]


def over(dst: Pixel, src: Pixel) -> Pixel:
    if src[3] == 0:
        return dst
    a = src[3] / 255
    return (int(dst[0] * (1 - a) + src[0] * a), int(dst[1] * (1 - a) + src[1] * a), int(dst[2] * (1 - a) + src[2] * a), max(dst[3], src[3]))


def make_review(base: Image, old: Image, new: Image) -> Image:
    scale = 4
    panels = 4
    outw = COLS * FW * panels * scale
    outh = ROWS * FH * scale
    out = [[(28, 28, 28, 255) for _ in range(outw)] for __ in range(outh)]
    for row in range(ROWS):
        for col in range(COLS):
            for panel, img in enumerate((base, old, new, new)):
                dx = (col * panels + panel) * FW * scale
                dy = row * FH * scale
                for y in range(FH):
                    for x in range(FW):
                        sx, sy = col * FW + x, row * FH + y
                        c = img[sy][sx]
                        if panel == 3:
                            c = over(base[sy][sx], c)
                        bg = (180, 180, 180, 255) if ((x // 4 + y // 4) % 2 == 0) else (80, 80, 80, 255)
                        a = c[3] / 255
                        c = (int(bg[0] * (1 - a) + c[0] * a), int(bg[1] * (1 - a) + c[1] * a), int(bg[2] * (1 - a) + c[2] * a), 255)
                        for yy in range(scale):
                            for xx in range(scale):
                                out[dy + y * scale + yy][dx + x * scale + xx] = c
    return out


def main() -> int:
    w, h, base = read_png_rgba(BASE)
    _, _, old = read_png_rgba(BOOT)
    new = [[TRANSPARENT for _ in range(w)] for __ in range(h)]
    for row in range(ROWS):
        for col in range(COLS):
            paste(new, polish_frame(base, old, col, row), col, row)
    write_png_rgba(BOOT, w, h, new)
    write_png_rgba(ROOT / "imported/boots_black_layer_32x64_8dir_4row.png", w, h, new)
    write_png_rgba(REVIEW, COLS * FW * 4 * 4, ROWS * FH * 4, make_review(base, old, new))
    print(f"wrote {BOOT}")
    print(f"wrote {REVIEW}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
