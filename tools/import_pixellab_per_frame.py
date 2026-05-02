#!/usr/bin/env python3
"""Import a PixelLab per-frame folder layout into a Karma sprite sheet.

PixelLab can export an animation as one PNG per frame, organised under a
direction sub-folder. body_type_2 lays out as:

    body_type_2/
      sprint/
        south/         south1.png .. south9.png
        south-east/    south-east1.png .. south-east9.png
        east/          east1.png .. east9.png
        ...
      walking/
        south/         south1.png .. south9.png
        ...

This script composes one such animation folder into a single sprite sheet
matching Karma's player-v2 cell contract:

  - 32x64 cells, 8 columns (one per direction), N rows (one per frame).
  - Direction column order: south, south-east, east, north-east, north,
    north-west, west, south-west (matches Karma front/down -> front-left).
  - Missing directions are filled by reusing the closest neighbour so the
    sheet still renders.

Output files (next to existing player-v2 imports):

  <output-stem>_32x64_8dir_{N}row.png         tight contract (256 x N*64)
  <output-stem>_32x64_8dir_{N}row_runtime.png runtime variant (512 x N*64)
  <output-stem>_per_frame_manifest.json       direction map + frame counts

Usage:

  python tools/import_pixellab_per_frame.py \
      assets/art/sprites/Neutral_malehumanoid_paper-doll_base_body_bal/body_type_2/sprint \
      --output-dir assets/art/sprites/player_v2/imported \
      --output-stem body_type_2_sprint
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parent
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from import_pixellab_character import (  # noqa: E402
    DIRECTION_ORDER,
    RUNTIME_CELL,
    TARGET_CELL_H,
    TARGET_CELL_W,
    blank,
    blit,
    copy_cell,
    fit_cell,
)
from prepare_character_sheet import (  # noqa: E402
    read_png_rgba,
    write_png_rgba,
)

# PixelLab folder name -> Karma column index (matches DIRECTION_ORDER above).
PIXELLAB_DIRECTION_TO_COLUMN: dict[str, int] = {
    "south": 0,
    "south-east": 1,
    "east": 2,
    "north-east": 3,
    "north": 4,
    "north-west": 5,
    "west": 6,
    "south-west": 7,
}


def load_direction_frames(direction_dir: Path) -> list[tuple[int, int, list]]:
    """Load every PNG in a direction folder, sorted by trailing frame index."""
    if not direction_dir.is_dir():
        return []

    candidates: list[tuple[int, Path]] = []
    for entry in direction_dir.iterdir():
        if entry.suffix.lower() != ".png" or not entry.is_file():
            continue
        # Frames are named "<dirname><n>.png" — pull the trailing integer.
        digits = _trailing_digits(entry.stem)
        if not digits:
            continue
        candidates.append((int(digits), entry))

    candidates.sort(key=lambda pair: pair[0])
    return [read_png_rgba(path) for _, path in candidates]


def _trailing_digits(stem: str) -> str:
    """Return the longest run of digits at the end of `stem`, in order."""
    rev = []
    for char in reversed(stem):
        if char.isdigit():
            rev.append(char)
        else:
            break
    return "".join(reversed(rev))


def fit_frame(width: int, height: int, pixels: list) -> list:
    """Fit a single 120x120-ish PixelLab frame into a 32x64 Karma cell."""
    return fit_cell(pixels, 0, 0, width, height)


def _flip_horizontal(cell: list) -> list:
    """Return a horizontally-mirrored copy of a cell (list of pixel rows)."""
    return [list(reversed(row)) for row in cell]


def load_rotation_frame(rotations_dir: Path, direction_name: str) -> list | None:
    """Load a single static rotation as a 32x64 fitted cell (or None)."""
    if not rotations_dir.is_dir():
        return None
    candidate = rotations_dir / f"{direction_name}.png"
    if not candidate.is_file():
        return None
    width, height, pixels = read_png_rgba(candidate)
    return fit_frame(width, height, pixels)


def build_per_frame_sheet(
    animation_dir: Path,
    rotations_dir: Path | None = None,
) -> tuple[int, list[int], list[list], dict[int, str]]:
    """Compose direction folders under animation_dir into a 8-col x N-row sheet.

    Returns (rows, frame_counts_by_column, contract_pixels, source_tags).
    `source_tags` maps each column to a short label describing where its
    frames came from ("animation", "flipped:<seed>", "rotation", "any").
    """
    direction_frames: dict[int, list[list]] = {}
    direction_counts: dict[int, int] = {}
    source_tags: dict[int, str] = {}

    for direction_name, column in PIXELLAB_DIRECTION_TO_COLUMN.items():
        frames = load_direction_frames(animation_dir / direction_name)
        if not frames:
            print(f"warning: missing direction folder {animation_dir / direction_name}")
            direction_counts[column] = 0
            continue
        fitted = [fit_frame(width, height, pixels) for width, height, pixels in frames]
        direction_frames[column] = fitted
        direction_counts[column] = len(fitted)
        source_tags[column] = "animation"

    if not direction_frames and rotations_dir is None:
        raise ValueError(f"no PixelLab direction folders found under {animation_dir}")

    rows = max(direction_counts.values()) if direction_counts.values() else 0
    if rows == 0 and rotations_dir is None:
        raise ValueError(f"no frames found under {animation_dir}")
    # When the animation folder is empty but rotations are available, render a
    # 1-row sheet of static idle frames so the engine can still load the asset
    # while the real PixelLab animations are commissioned.
    if rows == 0:
        rows = 1

    # Fill missing columns by mirroring or reusing a neighbour so the sheet
    # still renders. Pairs marked `horizontal_flip=True` are the left/right
    # swaps where flipping is the correct stand-in (e.g. west drawn from
    # east). The 0/4 pair (south/north) cannot be derived by flipping, so
    # those reuse the source unchanged.
    fallback_for_column = {
        0: (4, False),  # south    <- north (no clean fallback; reuse)
        4: (0, False),  # north    <- south
        1: (7, True),   # south-east <- south-west (horizontal flip)
        3: (5, True),   # north-east <- north-west
        2: (6, True),   # east     <- west
        6: (2, True),   # west     <- east
        5: (3, True),   # north-west <- north-east
        7: (1, True),   # south-west <- south-east
    }

    column_to_direction = {col: name for name, col in PIXELLAB_DIRECTION_TO_COLUMN.items()}
    # Snapshot which columns came from real animation data BEFORE we start
    # filling. Otherwise priority-1 (flipped neighbour) would pick a column
    # that was itself a rotation fallback, producing flipped-rotation chains
    # instead of using the per-direction rotation file directly.
    animation_columns = {col for col, tag in source_tags.items() if tag == "animation"}

    for column in range(8):
        if direction_frames.get(column):
            continue
        seed_pair = fallback_for_column.get(column)

        # Priority 1: a flipped neighbour SOURCED FROM ANIMATION DATA.
        if seed_pair is not None and seed_pair[1] and seed_pair[0] in animation_columns:
            seed, _flip = seed_pair
            source = direction_frames[seed]
            direction_frames[column] = [_flip_horizontal(cell) for cell in source]
            print(f"  filled column {column} from neighbour column {seed} (flipped from animation)")
            source_tags[column] = f"flipped:{seed}"
            continue

        # Priority 2: per-direction rotation idle frame. Correct facing, no animation.
        if rotations_dir is not None:
            rotation = load_rotation_frame(rotations_dir, column_to_direction[column])
            if rotation is not None:
                direction_frames[column] = [rotation] * rows
                print(f"  filled column {column} from rotations/{column_to_direction[column]}.png")
                source_tags[column] = "rotation"
                continue

        # Priority 3: a flipped or copied neighbour from any source (worst case for
        # flippable pairs; an unflippable copy for the south<->north pair).
        if seed_pair is not None and direction_frames.get(seed_pair[0]):
            seed, flip = seed_pair
            source = direction_frames[seed]
            direction_frames[column] = (
                [_flip_horizontal(cell) for cell in source] if flip else source
            )
            tag = "flipped" if flip else "copied"
            print(f"  filled column {column} from neighbour column {seed} ({tag} from {source_tags.get(seed, 'unknown')})")
            source_tags[column] = f"{tag}:{seed}"
            continue

        # Last resort: reuse whatever is available.
        any_column = next(iter(direction_frames.values()), None)
        if any_column is None:
            raise ValueError("no frames available to fill missing direction")
        direction_frames[column] = any_column
        print(f"  filled column {column} from any-available column")
        source_tags[column] = "any"

    contract_w = TARGET_CELL_W * 8
    contract_h = TARGET_CELL_H * rows
    contract = blank(contract_w, contract_h)
    counts: list[int] = []
    for column in range(8):
        frames = direction_frames[column]
        counts.append(len(frames))
        for row in range(rows):
            cell = frames[min(row, len(frames) - 1)]
            blit(contract, cell, column * TARGET_CELL_W, row * TARGET_CELL_H)
    return rows, counts, contract, source_tags


def build_runtime(rows: int, contract: list[list]) -> list[list]:
    width = RUNTIME_CELL * 8
    height = RUNTIME_CELL * rows
    out = blank(width, height)
    for row in range(rows):
        for column in range(8):
            cell = copy_cell(
                contract,
                column * TARGET_CELL_W,
                row * TARGET_CELL_H,
                TARGET_CELL_W,
                TARGET_CELL_H,
            )
            blit(
                out,
                cell,
                column * RUNTIME_CELL + ((RUNTIME_CELL - TARGET_CELL_W) // 2),
                row * RUNTIME_CELL,
            )
    return out


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("animation_dir", type=Path,
                        help="PixelLab animation root (e.g. .../body_type_2/sprint)")
    parser.add_argument("--output-dir", type=Path,
                        default=Path("assets/art/sprites/player_v2/imported"))
    parser.add_argument("--output-stem", required=True,
                        help="Stem used for the resulting PNG and manifest")
    parser.add_argument("--rotations-dir", type=Path, default=None,
                        help="Optional rotations folder (e.g. .../body_type_2/rotations) "
                             "used to fall back on idle-facing frames for missing directions.")
    args = parser.parse_args()

    rows, counts, contract, source_tags = build_per_frame_sheet(
        args.animation_dir, args.rotations_dir
    )
    runtime = build_runtime(rows, contract)

    args.output_dir.mkdir(parents=True, exist_ok=True)
    contract_path = args.output_dir / f"{args.output_stem}_32x64_8dir_{rows}row.png"
    runtime_path = args.output_dir / f"{args.output_stem}_32x64_8dir_{rows}row_runtime.png"
    manifest_path = args.output_dir / f"{args.output_stem}_per_frame_manifest.json"

    write_png_rgba(contract_path, TARGET_CELL_W * 8, TARGET_CELL_H * rows, contract)
    write_png_rgba(runtime_path, RUNTIME_CELL * 8, RUNTIME_CELL * rows, runtime)

    manifest = {
        "stem": args.output_stem,
        "source": str(args.animation_dir),
        "rotations_source": str(args.rotations_dir) if args.rotations_dir else None,
        "direction_order": list(DIRECTION_ORDER),
        "direction_columns": PIXELLAB_DIRECTION_TO_COLUMN,
        "rows": rows,
        "frame_counts_by_column": counts,
        "source_tag_by_column": {str(k): v for k, v in sorted(source_tags.items())},
        "contract_path": str(contract_path),
        "runtime_path": str(runtime_path),
    }
    manifest_path.write_text(json.dumps(manifest, indent=2))

    print(f"animation: {args.animation_dir}")
    print(f"rows: {rows}")
    print(f"per-column frame counts: {counts}")
    print(f"wrote {contract_path}")
    print(f"wrote {runtime_path}")
    print(f"wrote {manifest_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
