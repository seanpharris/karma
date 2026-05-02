#!/usr/bin/env python3
"""Derive a walking animation folder from the matching sprint folder.

PixelLab generates walk and sprint as distinct animations, but engine timing
is the only thing that meaningfully separates them at this scale — the body
poses cycle through the same leg-forward / leg-back arc. Until walk is
commissioned, copy each sprint direction over to its walking twin so the
walking folder renders a complete 8-direction cycle.

For each `<sprint_dir>/<direction>/*.png`, the matching frames are copied
to `<walking_dir>/<direction>/<direction>{N}.png`.

Usage:

  python3 tools/derive_walking_from_sprint.py \
      assets/.../body_type_2/sprint \
      assets/.../body_type_2/walking
"""

from __future__ import annotations

import argparse
import shutil
import sys
from pathlib import Path

VALID_DIRECTIONS = {
    "south", "south-east", "east", "north-east",
    "north", "north-west", "west", "south-west",
}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("sprint_dir", type=Path,
                        help="Source animation root containing direction sub-folders")
    parser.add_argument("walking_dir", type=Path,
                        help="Target animation root; missing direction folders will be created")
    parser.add_argument("--overwrite", action="store_true",
                        help="Replace existing target frames")
    parser.add_argument("--directions", nargs="*", default=None,
                        help="Restrict to a subset (e.g. --directions south north)")
    args = parser.parse_args()

    if not args.sprint_dir.is_dir():
        print(f"ERROR: sprint folder not found: {args.sprint_dir}", file=sys.stderr)
        return 1

    selected = (
        set(args.directions) if args.directions else VALID_DIRECTIONS
    )
    selected &= VALID_DIRECTIONS

    args.walking_dir.mkdir(parents=True, exist_ok=True)

    summary: dict[str, tuple[int, int]] = {}
    for direction in sorted(selected):
        src = args.sprint_dir / direction
        dst = args.walking_dir / direction
        if not src.is_dir():
            print(f"  skip ({direction}): no sprint source folder")
            continue
        dst.mkdir(parents=True, exist_ok=True)
        wrote = 0
        skipped = 0
        for png in sorted(src.iterdir()):
            if png.suffix.lower() != ".png" or not png.is_file():
                continue
            target = dst / png.name
            if target.exists() and not args.overwrite:
                skipped += 1
                continue
            shutil.copy2(png, target)
            wrote += 1
        summary[direction] = (wrote, skipped)
        print(f"  {direction}: wrote {wrote}, skipped {skipped}")

    total_wrote = sum(w for w, _ in summary.values())
    total_skipped = sum(s for _, s in summary.values())
    print(f"done: wrote {total_wrote}, skipped {total_skipped}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
