#!/usr/bin/env python3
"""Mirror a PixelLab per-frame animation folder into its left/right twin.

Cheap content fill for the directions PixelLab hasn't generated yet. Pairs
where horizontal flipping is honest:

  east        <->  west
  south-east  <->  south-west
  north-east  <->  north-west

Each input PNG (e.g. east1.png .. east9.png) is horizontally mirrored and
written under the target folder with the target prefix (west1.png .. west9.png).
Existing files in the target folder are skipped unless --overwrite is passed.

Usage:

  python3 tools/flip_pixellab_direction.py \
      assets/.../body_type_2/sprint/east \
      assets/.../body_type_2/sprint/west \
      --target-name west
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parent
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from prepare_character_sheet import read_png_rgba, write_png_rgba  # noqa: E402


def trailing_digits(stem: str) -> str:
    rev = []
    for char in reversed(stem):
        if char.isdigit():
            rev.append(char)
        else:
            break
    return "".join(reversed(rev))


def flip_horizontal(pixels: list) -> list:
    return [list(reversed(row)) for row in pixels]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source_dir", type=Path, help="Folder containing source frames")
    parser.add_argument("target_dir", type=Path, help="Folder to write mirrored frames into")
    parser.add_argument("--target-name", required=True,
                        help="Prefix for output filenames (e.g. 'west' produces west1.png ..)")
    parser.add_argument("--overwrite", action="store_true",
                        help="Replace existing target files")
    args = parser.parse_args()

    if not args.source_dir.is_dir():
        print(f"ERROR: source folder not found: {args.source_dir}", file=sys.stderr)
        return 1

    args.target_dir.mkdir(parents=True, exist_ok=True)

    written = 0
    skipped = 0
    for entry in sorted(args.source_dir.iterdir()):
        if entry.suffix.lower() != ".png" or not entry.is_file():
            continue
        digits = trailing_digits(entry.stem)
        if not digits:
            continue
        target = args.target_dir / f"{args.target_name}{digits}.png"
        if target.exists() and not args.overwrite:
            print(f"  skip (exists): {target}")
            skipped += 1
            continue
        width, height, pixels = read_png_rgba(entry)
        flipped = flip_horizontal(pixels)
        write_png_rgba(target, width, height, flipped)
        print(f"  wrote: {target}")
        written += 1

    print(f"done: wrote {written}, skipped {skipped}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
