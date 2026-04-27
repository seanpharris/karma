#!/usr/bin/env python3
"""Audit Karma art assets for curation hygiene.

This is intentionally lightweight: it reports naming, folder, PNG dimensions,
alpha presence, and obvious chroma-key leakage without requiring third-party
Python packages.
"""

from __future__ import annotations

import re
import struct
import sys
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ART_ROOT = ROOT / "assets" / "art"
DOMAIN_FOLDERS = {"reference", "sprites", "tilesets", "props", "structures", "ui"}
LEGACY_NAMES = {
    "README.md",
    "character.png",
    ".gitkeep",
    "scifi_engineer_player_8dir.png",
    "scifi_engineer_player_sheet.png",
    "scifi_engineer_player_sheet_chroma.png",
    "scifi_item_atlas.png",
    "scifi_tool_atlas.png",
    "scifi_utility_item_atlas.png",
    "scifi_weapon_atlas.png",
    "scifi_greenhouse_atlas.png",
    "scifi_station_atlas.png",
}
NAME_RE = re.compile(r"^[a-z0-9]+_[a-z0-9]+_[a-z0-9][a-z0-9_]*(?:_[0-9]+px|_atlas|_8dir)?\.png$")


def read_png_summary(path: Path) -> tuple[int, int, int, int, int]:
    data = path.read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a PNG")

    pos = 8
    width = height = color_type = bit_depth = None
    idat = b""
    while pos < len(data):
        length = struct.unpack(">I", data[pos:pos + 4])[0]
        kind = data[pos + 4:pos + 8]
        payload = data[pos + 8:pos + 8 + length]
        pos += 12 + length
        if kind == b"IHDR":
            width, height, bit_depth, color_type, _, _, _ = struct.unpack(">IIBBBBB", payload)
        elif kind == b"IDAT":
            idat += payload
        elif kind == b"IEND":
            break

    if width is None or height is None or color_type is None or bit_depth is None:
        raise ValueError("missing IHDR")
    if bit_depth != 8 or color_type not in (2, 6):
        return width, height, 0, 0, 0

    channels = 4 if color_type == 6 else 3
    stride = width * channels
    raw = zlib.decompress(idat)
    previous = bytearray(stride)
    cursor = 0
    transparent = 0
    chroma = 0
    opaque = 0

    for _ in range(height):
        filter_type = raw[cursor]
        cursor += 1
        row = bytearray(raw[cursor:cursor + stride])
        cursor += stride
        for i in range(stride):
            left = row[i - channels] if i >= channels else 0
            up = previous[i]
            up_left = previous[i - channels] if i >= channels else 0
            if filter_type == 1:
                row[i] = (row[i] + left) & 0xFF
            elif filter_type == 2:
                row[i] = (row[i] + up) & 0xFF
            elif filter_type == 3:
                row[i] = (row[i] + ((left + up) // 2)) & 0xFF
            elif filter_type == 4:
                predictor = left + up - up_left
                pa = abs(predictor - left)
                pb = abs(predictor - up)
                pc = abs(predictor - up_left)
                pr = left if pa <= pb and pa <= pc else up if pb <= pc else up_left
                row[i] = (row[i] + pr) & 0xFF
            elif filter_type != 0:
                raise ValueError(f"unsupported PNG filter {filter_type}")

        for x in range(width):
            base = x * channels
            r, g, b = row[base], row[base + 1], row[base + 2]
            a = row[base + 3] if channels == 4 else 255
            if a <= 10:
                transparent += 1
            else:
                opaque += 1
                if g >= 150 and r <= 110 and b <= 110 and g > r * 1.6 and g > b * 1.6:
                    chroma += 1
        previous = row

    return width, height, transparent, opaque, chroma


def audit() -> int:
    warnings = 0
    errors = 0
    png_count = 0

    if not ART_ROOT.exists():
        print(f"ERROR: missing {ART_ROOT}")
        return 1

    for path in sorted(ART_ROOT.rglob("*")):
        if path.is_dir():
            continue
        rel = path.relative_to(ART_ROOT)
        parts = rel.parts
        if len(parts) > 1 and parts[0] not in DOMAIN_FOLDERS:
            print(f"WARN: {rel}: unexpected domain folder '{parts[0]}'")
            warnings += 1

        if path.suffix.lower() == ".png":
            png_count += 1
            if path.name not in LEGACY_NAMES and not NAME_RE.match(path.name):
                print(f"WARN: {rel}: filename does not follow <theme>_<domain>_<subject>... convention")
                warnings += 1
            try:
                width, height, transparent, opaque, chroma = read_png_summary(path)
            except Exception as exc:  # noqa: BLE001 - report and continue.
                print(f"ERROR: {rel}: cannot read PNG ({exc})")
                errors += 1
                continue

            note = f"{rel}: {width}x{height}, opaque={opaque}, transparent={transparent}, chroma={chroma}"
            print(note)
            if chroma > 0 and "chroma" not in path.stem:
                print(f"WARN: {rel}: chroma green pixels found in non-source asset")
                warnings += 1
            if transparent == 0 and parts and parts[0] in {"sprites", "props", "structures", "ui"}:
                print(f"WARN: {rel}: no transparency detected")
                warnings += 1

    print(f"\nAudit complete: {png_count} PNGs, {warnings} warnings, {errors} errors")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(audit())
