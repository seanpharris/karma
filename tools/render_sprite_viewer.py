#!/usr/bin/env python3
"""Render review-friendly sprite previews for Karma player-v2 layers.

This creates static PNG contact sheets plus an HTML index so humans and agents can
inspect the same generated sprite variants without opening Godot.

Usage:
  python tools/render_sprite_viewer.py
  python tools/render_sprite_viewer.py --scale 3 --output-dir assets/art/sprites/player_v2/review
"""

from __future__ import annotations

import argparse
import html
import json
import sys
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parent
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from prepare_character_sheet import ImagePixels, Pixel, read_png_rgba, write_png_rgba  # noqa: E402

ROOT = Path("assets/art/sprites/player_v2")
MANIFEST = ROOT / "player_model_32x64_manifest.json"
DEFAULT_OUTPUT = ROOT / "review"
DIRECTION_NAMES = ["front", "front-right", "right", "back-right", "back", "back-left", "left", "front-left"]

TRANSPARENT: Pixel = (0, 0, 0, 0)
BG_LIGHT: Pixel = (222, 224, 228, 255)
BG_DARK: Pixel = (184, 188, 196, 255)
PANEL: Pixel = (34, 38, 46, 255)
GRID: Pixel = (72, 78, 88, 255)


def blank(width: int, height: int, color: Pixel = TRANSPARENT) -> ImagePixels:
    return [[color for _ in range(width)] for _ in range(height)]


def checker(width: int, height: int, cell: int = 8) -> ImagePixels:
    image = blank(width, height)
    for y in range(height):
        for x in range(width):
            image[y][x] = BG_LIGHT if ((x // cell) + (y // cell)) % 2 == 0 else BG_DARK
    return image


def blit(dst: ImagePixels, src: ImagePixels, dst_x: int, dst_y: int) -> None:
    height = len(src)
    width = len(src[0]) if height else 0
    for y in range(height):
        yy = dst_y + y
        if yy < 0 or yy >= len(dst):
            continue
        for x in range(width):
            xx = dst_x + x
            if xx < 0 or xx >= len(dst[0]):
                continue
            pixel = src[y][x]
            if pixel[3] > 0:
                dst[yy][xx] = pixel


def crop(src: ImagePixels, x: int, y: int, width: int, height: int) -> ImagePixels:
    return [row[x:x + width] for row in src[y:y + height]]


def scale_nearest(src: ImagePixels, factor: int) -> ImagePixels:
    if factor == 1:
        return [row[:] for row in src]
    out: ImagePixels = []
    for row in src:
        expanded_row: list[Pixel] = []
        for pixel in row:
            expanded_row.extend([pixel] * factor)
        for _ in range(factor):
            out.append(expanded_row[:])
    return out


def compose(layers: list[ImagePixels]) -> ImagePixels:
    height = len(layers[0])
    width = len(layers[0][0])
    out = blank(width, height)
    for layer in layers:
        blit(out, layer, 0, 0)
    return out


def frame_strip(sheet: ImagePixels, frame_width: int, frame_height: int, row: int, scale: int) -> ImagePixels:
    columns = 8
    gap = scale * 2
    out_width = columns * frame_width * scale + (columns - 1) * gap
    out_height = frame_height * scale
    out = checker(out_width, out_height, cell=max(4, scale * 4))
    for column in range(columns):
        frame = crop(sheet, column * frame_width, row * frame_height, frame_width, frame_height)
        blit(out, scale_nearest(frame, scale), column * (frame_width * scale + gap), 0)
    return out


def make_variant_matrix(manifest: dict, output_dir: Path, scale: int) -> Path:
    frame_width = manifest["frameWidth"]
    frame_height = manifest["frameHeight"]
    layer_lookup = {layer["id"]: layer for layer in manifest["layers"]}
    by_slot: dict[str, list[str]] = {}
    for layer in manifest["layers"]:
        by_slot.setdefault(layer["slot"], []).append(layer["id"])

    required_ids = [layer["id"] for layer in manifest["layers"] if layer.get("required")]
    default_skin = next(layer["id"] for layer in manifest["layers"] if layer["slot"] == "skin" and layer.get("default"))
    default_optional_ids = [
        next((layer["id"] for layer in manifest["layers"] if layer["slot"] == slot and layer.get("default")), "")
        for slot in manifest.get("layerOrder", [])
        if slot not in {"base", "skin", "hair", "outfit"}
    ]
    default_optional_ids = [layer_id for layer_id in default_optional_ids if layer_id]
    hairs = by_slot.get("hair", [])
    outfits = by_slot.get("outfit", [])

    loaded: dict[str, ImagePixels] = {}
    for layer_id, layer in layer_lookup.items():
        width, height, pixels = read_png_rgba(ROOT / layer["path"])
        if width != manifest["columns"] * frame_width or height != manifest["rows"] * frame_height:
            raise ValueError(f"{layer_id} has unexpected size {width}x{height}")
        loaded[layer_id] = pixels

    strip_width = 8 * frame_width * scale + 7 * (scale * 2)
    strip_height = frame_height * scale
    pad = 12 * scale
    cell_width = strip_width + pad
    cell_height = strip_height + pad
    out_width = len(outfits) * cell_width + pad
    out_height = len(hairs) * cell_height + pad
    out = blank(out_width, out_height, PANEL)

    for hair_index, hair_id in enumerate(hairs):
        for outfit_index, outfit_id in enumerate(outfits):
            stack = required_ids + [default_skin, hair_id, outfit_id] + default_optional_ids
            sheet = compose([loaded[layer_id] for layer_id in stack])
            strip = frame_strip(sheet, frame_width, frame_height, row=0, scale=scale)
            x = pad + outfit_index * cell_width
            y = pad + hair_index * cell_height
            blit(out, strip, x, y)

    path = output_dir / "player_v2_variant_matrix.png"
    write_png_rgba(path, out_width, out_height, out)
    return path


def make_layer_contact_sheet(manifest: dict, output_dir: Path, scale: int) -> Path:
    frame_width = manifest["frameWidth"]
    frame_height = manifest["frameHeight"]
    columns = 4
    preview_width = frame_width * scale * 8 + scale * 2 * 7
    preview_height = frame_height * scale
    pad = 10 * scale
    cell_width = preview_width + pad
    cell_height = preview_height + pad
    rows = (len(manifest["layers"]) + columns - 1) // columns
    out_width = columns * cell_width + pad
    out_height = rows * cell_height + pad
    out = blank(out_width, out_height, PANEL)

    for index, layer in enumerate(manifest["layers"]):
        width, height, pixels = read_png_rgba(ROOT / layer["path"])
        if width != manifest["columns"] * frame_width or height != manifest["rows"] * frame_height:
            raise ValueError(f"{layer['id']} has unexpected size {width}x{height}")
        strip = frame_strip(pixels, frame_width, frame_height, row=0, scale=scale)
        x = pad + (index % columns) * cell_width
        y = pad + (index // columns) * cell_height
        blit(out, strip, x, y)

    path = output_dir / "player_v2_layer_contact_sheet.png"
    write_png_rgba(path, out_width, out_height, out)
    return path


def write_html(manifest: dict, output_dir: Path, variant_path: Path, layer_path: Path) -> Path:
    layers_by_slot: dict[str, list[str]] = {}
    for layer in manifest["layers"]:
        layers_by_slot.setdefault(layer["slot"], []).append(layer["id"])

    slot_lists = "\n".join(
        f"<h3>{html.escape(slot)}</h3><ul>" + "".join(f"<li>{html.escape(layer_id)}</li>" for layer_id in ids) + "</ul>"
        for slot, ids in layers_by_slot.items()
    )
    directions = ", ".join(DIRECTION_NAMES)
    content = f"""<!doctype html>
<html>
<head>
  <meta charset=\"utf-8\">
  <title>Karma player-v2 sprite viewer</title>
  <style>
    body {{ background: #151820; color: #eef1f7; font-family: system-ui, sans-serif; margin: 24px; }}
    img {{ image-rendering: pixelated; max-width: 100%; border: 1px solid #485060; background: #242936; }}
    code {{ background: #252b36; padding: 2px 5px; border-radius: 4px; }}
    .grid {{ display: grid; grid-template-columns: minmax(240px, 1fr) minmax(240px, 1fr); gap: 24px; align-items: start; }}
    @media (max-width: 900px) {{ .grid {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
  <h1>Karma player-v2 sprite viewer</h1>
  <p>Manifest: <code>{html.escape(str(MANIFEST))}</code></p>
  <p>Direction order in each strip: {html.escape(directions)}. The variant matrix shows idle/facing frames for every hair × outfit combination using the default medium skin.</p>
  <h2>Variant matrix</h2>
  <img src=\"{html.escape(variant_path.name)}\" alt=\"player variant matrix\">
  <h2>Layer contact sheet</h2>
  <img src=\"{html.escape(layer_path.name)}\" alt=\"player layer contact sheet\">
  <div class=\"grid\"><section><h2>Layer ids by slot</h2>{slot_lists}</section></div>
</body>
</html>
"""
    path = output_dir / "index.html"
    path.write_text(content, encoding="utf-8")
    return path


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", type=Path, default=MANIFEST)
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--scale", type=int, default=3)
    args = parser.parse_args()

    if args.scale < 1:
        parser.error("--scale must be >= 1")

    manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
    args.output_dir.mkdir(parents=True, exist_ok=True)
    variant_path = make_variant_matrix(manifest, args.output_dir, args.scale)
    layer_path = make_layer_contact_sheet(manifest, args.output_dir, args.scale)
    html_path = write_html(manifest, args.output_dir, variant_path, layer_path)
    print(f"wrote {variant_path}")
    print(f"wrote {layer_path}")
    print(f"wrote {html_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
