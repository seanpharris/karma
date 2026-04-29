#!/usr/bin/env python3
"""Frame-by-frame sculpt pass for black boots on the real base model.

The earlier cleanup fixed color/noise globally. This pass uses the real base frame
as an alignment guide so walk-frame boots follow the actual feet instead of the
AI-generated boot silhouette drifting into chunky blobs.
"""

from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from prepare_character_sheet import read_png_rgba, write_png_rgba  # noqa: E402

ROOT = Path("assets/art/sprites/player_v2")
BASE = ROOT / "imported/player_base_body_sheet_32x64_8dir_4row.png"
BOOT = ROOT / "layers_32x64/boots_black_32x64.png"
REVIEW = ROOT / "review/external_layers/boots_black_sculpted_review_32x64.png"
FW, FH = 32, 64
COLS, ROWS = 8, 4
TRANSPARENT = (0, 0, 0, 0)
OUTLINE = (14, 16, 19, 255)
DARK = (24, 27, 31, 255)
MID = (38, 43, 49, 255)
HI = (68, 76, 84, 245)
CUFF = (31, 35, 40, 245)
DIR_SIGN = {1: 1, 2: 1, 3: 1, 5: -1, 6: -1, 7: -1}

Pixel = tuple[int, int, int, int]
Image = list[list[Pixel]]


def visible(px: Pixel, a: int = 24) -> bool:
    return px[3] >= a


def skin(px: Pixel) -> bool:
    r, g, b, a = px
    return a > 60 and r > 100 and g > 50 and b > 30 and r > b + 20 and g > b + 5


def neigh(mask: list[list[bool]], x: int, y: int, radius: int = 1) -> int:
    c = 0
    for yy in range(max(0, y - radius), min(FH, y + radius + 1)):
        for xx in range(max(0, x - radius), min(FW, x + radius + 1)):
            if xx == x and yy == y:
                continue
            c += 1 if mask[yy][xx] else 0
    return c


def close_to(mask: list[list[bool]], x: int, y: int, radius: int) -> bool:
    for yy in range(max(0, y - radius), min(FH, y + radius + 1)):
        for xx in range(max(0, x - radius), min(FW, x + radius + 1)):
            if mask[yy][xx]:
                return True
    return False


def bounds(mask: list[list[bool]]) -> tuple[int, int, int, int] | None:
    pts = [(x, y) for y in range(FH) for x in range(FW) if mask[y][x]]
    if not pts:
        return None
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    return min(xs), min(ys), max(xs), max(ys)


def components(mask: list[list[bool]]) -> list[list[tuple[int, int]]]:
    seen: set[tuple[int, int]] = set()
    out: list[list[tuple[int, int]]] = []
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


def color_for(x: int, y: int, b: tuple[int, int, int, int], col: int) -> Pixel:
    minx, miny, maxx, maxy = b
    # Keep highlights sparse and directional.
    if y <= miny + 1:
        return CUFF
    if col in DIR_SIGN:
        toe = maxx if DIR_SIGN[col] > 0 else minx
        if abs(x - toe) <= 1 and maxy - 8 <= y <= maxy - 2:
            return HI
    elif y >= maxy - 7 and (x == minx + 1 or x == maxx - 1):
        return HI
    if x in (minx, maxx) or y == maxy:
        return OUTLINE
    return MID if neigh([[False]], 0, 0) else DARK  # replaced below; keeps type checkers quiet


def apply_color(mask: list[list[bool]], col: int) -> list[list[Pixel]]:
    b = bounds(mask)
    out = [[TRANSPARENT for _ in range(FW)] for __ in range(FH)]
    if b is None:
        return out
    minx, miny, maxx, maxy = b
    for y in range(FH):
        for x in range(FW):
            if not mask[y][x]:
                continue
            if y <= miny + 1:
                c = CUFF
            elif col in DIR_SIGN:
                toe = maxx if DIR_SIGN[col] > 0 else minx
                c = HI if abs(x - toe) <= 1 and maxy - 8 <= y <= maxy - 2 else DARK
            elif y >= maxy - 7 and (x == minx + 1 or x == maxx - 1):
                c = HI
            elif x in (minx, maxx) or y == maxy:
                c = OUTLINE
            else:
                c = MID if neigh(mask, x, y) <= 4 else DARK
            out[y][x] = c
    return out


def sculpt_frame(base: Image, boot: Image, col: int, row: int) -> list[list[Pixel]]:
    ox, oy = col * FW, row * FH
    old = [[visible(boot[oy + y][ox + x]) for x in range(FW)] for y in range(FH)]
    # Skin seeds: only the lower foot/ankle area. Using the base frame prevents
    # diagonal/side walk frames from gaining arbitrary extra mass.
    seed = [[skin(base[oy + y][ox + x]) and y >= 41 for x in range(FW)] for y in range(FH)]
    if sum(1 for y in range(FH) for x in range(FW) if seed[y][x]) < 8:
        seed = [[skin(base[oy + y][ox + x]) and y >= 38 for x in range(FW)] for y in range(FH)]

    oldb = bounds(old)
    if oldb is None:
        return [[TRANSPARENT for _ in range(FW)] for __ in range(FH)]
    ominx, ominy, omaxx, omaxy = oldb

    # Start with existing pixels that are near the actual foot/ankle, trimming
    # far-out chunks and high calf creep.
    mask = [[False for _ in range(FW)] for __ in range(FH)]
    for y in range(FH):
        for x in range(FW):
            if old[y][x] and y >= max(43, ominy) and close_to(seed, x, y, 2):
                mask[y][x] = True

    # Cover only actual visible foot/ankle skin that would otherwise peek
    # through. Avoid dilating this aggressively; that was what made some walk
    # frames read as black pants instead of boots.
    for y in range(FH):
        for x in range(FW):
            if seed[y][x] and y >= 43 and ominx - 1 <= x <= omaxx + 1 and ominy - 2 <= y <= omaxy + 1:
                if close_to(old, x, y, 1):
                    mask[y][x] = True

    # Fill tiny internal holes, then remove isolated pixels.
    for _ in range(1):
        add = [r[:] for r in mask]
        for y in range(1, FH - 1):
            for x in range(1, FW - 1):
                if not mask[y][x] and neigh(mask, x, y) >= 5:
                    add[y][x] = True
        mask = add
    mask = [[mask[y][x] and neigh(mask, x, y) >= 1 for x in range(FW)] for y in range(FH)]

    # Directional trimming: keep profile/diagonal steps from ballooning wider
    # than the underlying foot motion. Prefer removing high/outer pixels; keep
    # lower toe/heel pixels because they sell the step.
    b = bounds(mask)
    max_width = 16 if col in (0, 4) else (14 if col in (2, 6) else 15)
    if b:
        minx, miny, maxx, maxy = b
        guard = 0
        while maxx - minx + 1 > max_width and guard < 12:
            guard += 1
            left_count = sum(1 for y in range(miny, maxy + 1) if mask[y][minx])
            right_count = sum(1 for y in range(miny, maxy + 1) if mask[y][maxx])
            trim_x = minx if left_count <= right_count else maxx
            for y in range(miny, maxy + 1):
                mask[y][trim_x] = False
            b = bounds(mask)
            if not b:
                break
            minx, miny, maxx, maxy = b

    # Final leak cover: exact lower-foot skin pixels that remain next to the
    # sculpted boot get painted over. This catches flickering bare toes/heels
    # without re-inflating the entire silhouette.
    for y in range(44, FH):
        for x in range(FW):
            if not mask[y][x] and skin(base[oy + y][ox + x]) and close_to(mask, x, y, 2):
                mask[y][x] = True

    # Front/back readability: if a single large blob spans the center, carve a
    # 1px negative seam through the upper/middle boot mass.
    if col in (0, 4):
        b = bounds(mask)
        if b:
            minx, miny, maxx, maxy = b
            cx = (minx + maxx) // 2
            for y in range(miny + 2, maxy - 1):
                if 0 < cx < FW - 1 and mask[y][cx - 1] and mask[y][cx] and mask[y][cx + 1]:
                    mask[y][cx] = False

    # Drop tiny detached components; they read as jittering pixels in motion.
    kept = [[False for _ in range(FW)] for __ in range(FH)]
    comps = components(mask)
    for pts in comps:
        if len(pts) < 4:
            continue
        xs = [p[0] for p in pts]
        ys = [p[1] for p in pts]
        if max(ys) < 44:
            continue
        for x, y in pts:
            kept[y][x] = True
    mask = kept

    # If the sculpt was too aggressive, fall back to the refined source frame.
    if sum(1 for y in range(FH) for x in range(FW) if mask[y][x]) < 20:
        mask = old

    return apply_color(mask, col)


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
            paste(new, sculpt_frame(base, old, col, row), col, row)
    write_png_rgba(BOOT, w, h, new)
    write_png_rgba(ROOT / "imported/boots_black_layer_32x64_8dir_4row.png", w, h, new)
    write_png_rgba(REVIEW, COLS * FW * 4 * 4, ROWS * FH * 4, make_review(base, old, new))
    print(f"wrote {BOOT}")
    print(f"wrote {REVIEW}")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
