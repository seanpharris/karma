#!/usr/bin/env python3
"""Import a folder of per-direction player sprite PNGs into Karma's 32x64 contract.

Expected folder layout:
  base_body_south.png
  base_body_south-east.png
  ...
  walk/walk_south_frame00.png
  walk/walk_south_frame01.png
  ...

The script writes the same review-only outputs as the PixelLab importer:
  <stem>_32x64_8dir_4row.png
  <stem>_32x64_8dir_runtime.png

Missing diagonal walk frames can be filled by mirroring the opposite diagonal,
which is useful for partial generator exports while still making an in-game
previewable sheet.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parent
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from import_pixellab_character import (  # noqa: E402
    DIRECTIONS,
    ROWS,
    RUNTIME_CELL,
    TARGET_CELL_H,
    TARGET_CELL_W,
    blank,
    blit,
    build_runtime,
    fit_cell,
)
from prepare_character_sheet import ImagePixels, Pixel, read_png_rgba, write_png_rgba  # noqa: E402

DIRECTION_SLUGS = [
    "south",
    "south-east",
    "east",
    "north-east",
    "north",
    "north-west",
    "west",
    "south-west",
]
MIRROR_FALLBACKS = {
    "south-east": "south-west",
    "south-west": "south-east",
    "north-east": "north-west",
    "north-west": "north-east",
    "east": "west",
    "west": "east",
}


def flip_horizontal(image: ImagePixels) -> ImagePixels:
    return [list(reversed(row)) for row in image]


def load_png(path: Path) -> tuple[int, int, ImagePixels] | None:
    if not path.exists():
        return None
    return read_png_rgba(path)


def fit_image(width: int, height: int, image: ImagePixels) -> ImagePixels:
    return fit_cell(image, 0, 0, width, height)


def read_direction_frame(folder: Path, direction: str, row: int, frame_indices: list[int]) -> tuple[ImagePixels, str]:
    if row == 0:
        primary = folder / f"base_body_{direction}.png"
        loaded = load_png(primary)
        if loaded is not None:
            width, height, pixels = loaded
            return fit_image(width, height, pixels), str(primary)
    else:
        frame_index = frame_indices[row - 1]
        primary = folder / "walk" / f"walk_{direction}_frame{frame_index:02d}.png"
        loaded = load_png(primary)
        if loaded is not None:
            width, height, pixels = loaded
            return fit_image(width, height, pixels), str(primary)

    fallback_direction = MIRROR_FALLBACKS.get(direction)
    if fallback_direction:
        if row == 0:
            fallback = folder / f"base_body_{fallback_direction}.png"
        else:
            frame_index = frame_indices[row - 1]
            fallback = folder / "walk" / f"walk_{fallback_direction}_frame{frame_index:02d}.png"
        loaded = load_png(fallback)
        if loaded is not None:
            width, height, pixels = loaded
            return fit_image(width, height, flip_horizontal(pixels)), f"mirrored:{fallback}"

    return blank(TARGET_CELL_W, TARGET_CELL_H), "missing"


def build_contract_from_folder(folder: Path, frame_indices: list[int]) -> tuple[ImagePixels, list[str]]:
    contract = blank(TARGET_CELL_W * DIRECTIONS, TARGET_CELL_H * ROWS)
    sources: list[str] = []
    for row in range(ROWS):
        for direction_index, direction in enumerate(DIRECTION_SLUGS):
            fitted, source = read_direction_frame(folder, direction, row, frame_indices)
            sources.append(f"row{row}:{direction}={source}")
            blit(contract, fitted, direction_index * TARGET_CELL_W, row * TARGET_CELL_H)
    return contract, sources


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("input_dir", type=Path)
    parser.add_argument("--output-dir", type=Path, default=Path("assets/art/sprites/player_v2/imported"))
    parser.add_argument("--output-stem", default="folder_player_sprite")
    parser.add_argument("--walk-frames", default="0,1,2", help="comma-separated source walk frame indices for rows 2-4")
    args = parser.parse_args()

    frame_indices = [int(part.strip()) for part in args.walk_frames.split(",") if part.strip()]
    if len(frame_indices) != ROWS - 1:
        parser.error(f"--walk-frames must provide {ROWS - 1} frame indices")

    args.output_dir.mkdir(parents=True, exist_ok=True)
    contract, sources = build_contract_from_folder(args.input_dir, frame_indices)
    runtime = build_runtime(contract)

    contract_path = args.output_dir / f"{args.output_stem}_32x64_8dir_4row.png"
    runtime_path = args.output_dir / f"{args.output_stem}_32x64_8dir_runtime.png"
    sources_path = args.output_dir / f"{args.output_stem}_sources.txt"
    write_png_rgba(contract_path, TARGET_CELL_W * DIRECTIONS, TARGET_CELL_H * ROWS, contract)
    write_png_rgba(runtime_path, RUNTIME_CELL * DIRECTIONS, RUNTIME_CELL * ROWS, runtime)
    sources_path.write_text("\n".join(sources) + "\n", encoding="utf-8")
    print(f"wrote {contract_path}")
    print(f"wrote {runtime_path}")
    print(f"wrote {sources_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
