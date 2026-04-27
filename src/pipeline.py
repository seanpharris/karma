import argparse
import json
import math
from collections import Counter
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageFilter

SUPPORTED_EXTENSIONS = {".png", ".jpg", ".jpeg", ".bmp", ".webp", ".tga", ".gif"}
DEFAULT_BG_COLOR = "#ff00ff"


def parse_size_pair(value: object, default: tuple[int, int] | None = None) -> tuple[int, int]:
    if value is None:
        if default is None:
            raise ValueError("Expected a size value.")
        return default
    if isinstance(value, int):
        if value <= 0:
            raise ValueError("Size values must be positive.")
        return (value, value)
    if isinstance(value, list) and len(value) == 2:
        width = int(value[0])
        height = int(value[1])
        if width <= 0 or height <= 0:
            raise ValueError("Size values must be positive.")
        return (width, height)
    raise ValueError("Size values must be an integer or a two-item list like [48, 32].")


def parse_hex_color(value: str) -> tuple[int, int, int]:
    value = value.strip().lstrip("#")
    if len(value) == 3:
        value = "".join(ch * 2 for ch in value)
    if len(value) != 6:
        raise ValueError(f"Invalid color value: {value}. Use a 6-digit hex color like #ff00ff.")
    return tuple(int(value[i : i + 2], 16) for i in (0, 2, 4))


def rgb_to_hex(rgb: tuple[int, int, int]) -> str:
    return "#%02x%02x%02x" % rgb


def get_image_paths(folder: Path) -> list[Path]:
    return sorted(
        p for p in folder.iterdir() if p.is_file() and p.suffix.lower() in SUPPORTED_EXTENSIONS
    )


def load_image(path: Path) -> Image.Image:
    return Image.open(path).convert("RGBA")


def split_csv(value: str) -> list[str]:
    return [item.strip() for item in value.split(",") if item.strip()]


def parse_dimension_arg(value: str) -> tuple[int, int]:
    normalized = value.lower().replace(",", "x")
    parts = [part.strip() for part in normalized.split("x") if part.strip()]
    if len(parts) != 2:
        raise ValueError(f"Invalid dimension value: {value}. Use WIDTHxHEIGHT, for example 96x96.")
    width = int(parts[0])
    height = int(parts[1])
    if width <= 0 or height <= 0:
        raise ValueError("Dimension values must be positive.")
    return (width, height)


def find_background_bounds(
    image: Image.Image,
    background_color: tuple[int, int, int],
    tolerance: int,
) -> tuple[int, int, int, int] | None:
    arr = np.array(image)
    target = np.array(background_color, dtype=np.int32)
    rgb = arr[..., :3].astype(np.int32)
    alpha = arr[..., 3] > 0
    distance2 = np.sum((rgb - target) ** 2, axis=-1)
    mask = (distance2 <= (tolerance * tolerance)) & alpha
    ys, xs = np.where(mask)
    if len(xs) == 0 or len(ys) == 0:
        return None
    return (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)


def connected_components(mask: np.ndarray, min_area: int) -> list[dict[str, object]]:
    height, width = mask.shape
    visited = np.zeros_like(mask, dtype=bool)
    components: list[dict[str, object]] = []

    for start_y in range(height):
        for start_x in range(width):
            if visited[start_y, start_x] or not mask[start_y, start_x]:
                continue

            stack = [(start_y, start_x)]
            visited[start_y, start_x] = True
            pixels: list[tuple[int, int]] = []
            min_x = max_x = start_x
            min_y = max_y = start_y

            while stack:
                y, x = stack.pop()
                pixels.append((y, x))
                min_x = min(min_x, x)
                max_x = max(max_x, x)
                min_y = min(min_y, y)
                max_y = max(max_y, y)

                for next_y, next_x in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                    if 0 <= next_y < height and 0 <= next_x < width:
                        if not visited[next_y, next_x] and mask[next_y, next_x]:
                            visited[next_y, next_x] = True
                            stack.append((next_y, next_x))

            if len(pixels) >= min_area:
                components.append(
                    {
                        "bbox": (min_x, min_y, max_x + 1, max_y + 1),
                        "center": ((min_x + max_x + 1) / 2, (min_y + max_y + 1) / 2),
                        "pixels": pixels,
                    }
                )

    return components


def resize_and_center_in_canvas(
    image: Image.Image,
    canvas_size: tuple[int, int],
    fit_size: tuple[int, int] | None = None,
) -> Image.Image:
    width, height = image.size
    fit_width, fit_height = fit_size or canvas_size
    scale = min(fit_width / width, fit_height / height)
    new_width = max(1, round(width * scale))
    new_height = max(1, round(height * scale))
    resized = image.resize((new_width, new_height), Image.NEAREST)
    canvas_width, canvas_height = canvas_size
    canvas = Image.new("RGBA", canvas_size, (0, 0, 0, 0))
    offset_x = (canvas_width - new_width) // 2
    offset_y = (canvas_height - new_height) // 2
    canvas.paste(resized, (offset_x, offset_y), resized)
    return canvas


def resize_and_center(image: Image.Image, size: int) -> Image.Image:
    return resize_and_center_in_canvas(image, (size, size))


def load_sprite_manifest(path: Path | None) -> dict[str, dict[str, object]]:
    if path is None:
        return {}
    data = json.loads(path.expanduser().read_text(encoding="utf-8-sig"))
    if not isinstance(data, dict):
        raise ValueError("Sprite manifest must be a JSON object keyed by sprite name.")
    manifest: dict[str, dict[str, object]] = {}
    for name, raw_entry in data.items():
        if not isinstance(raw_entry, dict):
            raise ValueError(f"Manifest entry for {name} must be an object.")
        manifest[name] = raw_entry
    return manifest


def sprite_canvas_and_fit(
    sprite_name: str,
    manifest: dict[str, dict[str, object]],
    default_size: int,
) -> tuple[tuple[int, int], tuple[int, int]]:
    entry = manifest.get(sprite_name, {})
    canvas_size = parse_size_pair(entry.get("canvas"), (default_size, default_size))
    fit_size = parse_size_pair(entry.get("fit"), canvas_size)
    return canvas_size, fit_size


def manifest_metadata(sprite_name: str, manifest: dict[str, dict[str, object]]) -> dict[str, object]:
    entry = manifest.get(sprite_name, {})
    metadata: dict[str, object] = {}
    if "anchor" in entry:
        metadata["anchor"] = parse_size_pair(entry["anchor"])
    if "footprint_tiles" in entry:
        metadata["footprint_tiles"] = parse_size_pair(entry["footprint_tiles"])
    if "category" in entry:
        metadata["category"] = str(entry["category"])
    return metadata


def order_sprite_files(
    sprite_files: list[Path],
    manifest: dict[str, dict[str, object]],
) -> list[Path]:
    if not manifest:
        return sorted(sprite_files)
    by_stem = {sprite_path.stem: sprite_path for sprite_path in sprite_files}
    ordered = [by_stem[name] for name in manifest.keys() if name in by_stem]
    ordered_names = {sprite_path.stem for sprite_path in ordered}
    ordered.extend(sorted(sprite_path for sprite_path in sprite_files if sprite_path.stem not in ordered_names))
    return ordered


def quantize_image(image: Image.Image, colors: int) -> Image.Image:
    alpha = image.split()[3]
    rgb_canvas = Image.new("RGB", image.size, (0, 0, 0))
    rgb_canvas.paste(image, mask=alpha)
    quantized = rgb_canvas.quantize(colors=colors, method=Image.FASTOCTREE, dither=0)
    quant_rgb = quantized.convert("RGB")
    return Image.merge("RGBA", (*quant_rgb.split(), alpha))


def add_outline(image: Image.Image, color: tuple[int, int, int, int] = (0, 0, 0, 255)) -> Image.Image:
    alpha = image.split()[3]
    binary = alpha.point(lambda px: 255 if px > 0 else 0).convert("L")
    expanded = binary.filter(ImageFilter.MaxFilter(3))
    outline_mask = ImageChops.subtract(expanded, binary)
    outline_layer = Image.new("RGBA", image.size, color)
    outline_layer.putalpha(outline_mask)
    return Image.alpha_composite(outline_layer, image)


def rgb_to_hsv_np(rgb: np.ndarray) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    rgb = rgb.astype(np.float32) / 255.0
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    maxc = np.maximum(np.maximum(r, g), b)
    minc = np.minimum(np.minimum(r, g), b)
    delta = maxc - minc

    hue = np.zeros_like(maxc)
    mask = delta > 1e-6
    mask_r = mask & (maxc == r)
    mask_g = mask & (maxc == g)
    mask_b = mask & (maxc == b)
    hue[mask_r] = ((g - b)[mask_r] / delta[mask_r]) % 6
    hue[mask_g] = ((b - r)[mask_g] / delta[mask_g]) + 2
    hue[mask_b] = ((r - g)[mask_b] / delta[mask_b]) + 4
    hue = hue / 6.0
    hue = np.where(hue < 0, hue + 1, hue)

    saturation = np.zeros_like(maxc)
    saturation[mask] = delta[mask] / maxc[mask]
    value = maxc
    return hue, saturation, value


def edge_connected_background_mask(initial_mask: np.ndarray) -> np.ndarray:
    height, width = initial_mask.shape
    connected = np.zeros_like(initial_mask, dtype=bool)
    stack: list[tuple[int, int]] = []

    def enqueue(y: int, x: int) -> None:
        if not connected[y, x] and initial_mask[y, x]:
            connected[y, x] = True
            stack.append((y, x))

    for x in range(width):
        enqueue(0, x)
        enqueue(height - 1, x)
    for y in range(height):
        enqueue(y, 0)
        enqueue(y, width - 1)

    while stack:
        y, x = stack.pop()
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < height and 0 <= nx < width:
                enqueue(ny, nx)

    return connected


def build_background_candidate_mask(
    image: Image.Image,
    background_color: tuple[int, int, int],
    tolerance: int,
    use_hsv_bg_detection: bool,
) -> np.ndarray:
    arr = np.array(image)
    target = np.array(background_color, dtype=np.int32)
    rgb = arr[..., :3].astype(np.int32)
    distance2 = np.sum((rgb - target) ** 2, axis=-1)
    mask = distance2 <= (tolerance * tolerance)

    if use_hsv_bg_detection:
        hue, saturation, value = rgb_to_hsv_np(arr[..., :3])
        target_hue, _, _ = rgb_to_hsv_np(
            np.array(background_color, dtype=np.uint8).reshape((1, 1, 3))
        )
        hue_diff = np.minimum(np.abs(hue - target_hue), 1.0 - np.abs(hue - target_hue))
        hsv_mask = (hue_diff <= 0.08) & (saturation >= 0.3) & (value >= 0.2)
        mask = mask | hsv_mask

    alpha = arr[..., 3] > 0
    return mask & alpha


def apply_background_mask(image: Image.Image, background_mask: np.ndarray) -> Image.Image:
    arr = np.array(image)
    arr[..., 3] = np.where(background_mask, 0, arr[..., 3])
    return Image.fromarray(arr, mode="RGBA")


def remove_background_alpha(
    image: Image.Image,
    background_color: tuple[int, int, int],
    tolerance: int,
    use_hsv_bg_detection: bool = False,
) -> Image.Image:
    candidate_mask = build_background_candidate_mask(
        image, background_color, tolerance, use_hsv_bg_detection
    )
    background_mask = edge_connected_background_mask(candidate_mask)
    return apply_background_mask(image, background_mask)


def crop_to_alpha(image: Image.Image) -> Image.Image | None:
    alpha = image.split()[3]
    bbox = alpha.getbbox()
    if bbox is None:
        return None
    return image.crop(bbox)


def mask_to_debug_image(mask: np.ndarray) -> Image.Image:
    return Image.fromarray((mask.astype(np.uint8) * 255), mode="L")


def cleanup_edge_pixels(
    image: Image.Image,
    background_color: tuple[int, int, int],
    tolerance: int,
    extra: int,
) -> Image.Image:
    arr = np.array(image)
    alpha = arr[..., 3]
    rgb = arr[..., :3].astype(np.int32)
    target = np.array(background_color, dtype=np.int32)
    distance2 = np.sum((rgb - target) ** 2, axis=-1)
    threshold = (tolerance + extra) * (tolerance + extra)
    candidate = distance2 <= threshold

    transparent = alpha == 0
    adjacent = np.zeros_like(transparent)
    adjacent[1:, :] |= transparent[:-1, :]
    adjacent[:-1, :] |= transparent[1:, :]
    adjacent[:, 1:] |= transparent[:, :-1]
    adjacent[:, :-1] |= transparent[:, 1:]

    cleanup = candidate & (alpha > 0) & adjacent
    arr[..., 3] = np.where(cleanup, 0, arr[..., 3])
    return Image.fromarray(arr, mode="RGBA")


def decontaminate_background(
    image: Image.Image,
    background_color: tuple[int, int, int],
    tolerance: int,
    extra: int,
) -> Image.Image:
    arr = np.array(image)
    alpha = arr[..., 3]
    rgb = arr[..., :3].astype(np.int32)
    target = np.array(background_color, dtype=np.int32)
    distance2 = np.sum((rgb - target) ** 2, axis=-1)
    threshold = (tolerance + extra) * (tolerance + extra)
    candidate = distance2 <= threshold

    transparent = alpha == 0
    adjacent = np.zeros_like(transparent)
    adjacent[1:, :] |= transparent[:-1, :]
    adjacent[:-1, :] |= transparent[1:, :]
    adjacent[:, 1:] |= transparent[:, :-1]
    adjacent[:, :-1] |= transparent[:, 1:]

    cleanup = candidate & (alpha > 0) & adjacent
    positions = np.argwhere(cleanup)
    for y, x in positions:
        neighbors = []
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < arr.shape[0] and 0 <= nx < arr.shape[1]:
                if alpha[ny, nx] > 0 and not candidate[ny, nx]:
                    neighbors.append(arr[ny, nx, :3])
        if neighbors:
            arr[y, x, :3] = np.round(np.mean(neighbors, axis=0)).astype(np.uint8)
    return Image.fromarray(arr, mode="RGBA")


def estimate_background_color(image: Image.Image) -> tuple[int, int, int]:
    arr = np.array(image)
    height, width = arr.shape[:2]
    border_pixels = np.vstack(
        [arr[0, :, :], arr[-1, :, :], arr[:, 0, :], arr[:, -1, :]]
    )
    valid = [tuple(pixel[:3]) for pixel in border_pixels if pixel[3] > 0]
    if not valid:
        valid = [tuple(pixel[:3]) for pixel in border_pixels]
    most_common = Counter(valid).most_common(1)
    if most_common:
        return most_common[0][0]
    return parse_hex_color(DEFAULT_BG_COLOR)


def save_debug_text(text: str, target_path: Path) -> None:
    target_path.parent.mkdir(parents=True, exist_ok=True)
    target_path.write_text(text)


def save_debug_image(image: Image.Image, target_path: Path) -> None:
    target_path.parent.mkdir(parents=True, exist_ok=True)
    image.save(target_path, format="PNG")


def process_image(
    path: Path,
    output_path: Path,
    size: int,
    manifest: dict[str, dict[str, object]],
    colors: int,
    background_color: tuple[int, int, int],
    tolerance: int,
    outline: bool,
    auto_bg: bool,
    edge_cleanup: bool,
    edge_tolerance_extra: int,
    decontaminate_bg: bool,
    use_hsv_bg_detection: bool,
    debug: bool,
) -> bool:
    image = load_image(path)
    if auto_bg:
        detected_color = estimate_background_color(image)
        background_color = detected_color
        print(f"Detected background color {rgb_to_hex(background_color)} for {path.name}")

    if debug:
        detected_file = output_path.with_name(f"{output_path.stem}_detected_bg.txt")
        save_debug_text(rgb_to_hex(background_color), detected_file)

    candidate_mask = build_background_candidate_mask(
        image, background_color, tolerance, use_hsv_bg_detection
    )
    if debug:
        save_debug_image(
            mask_to_debug_image(candidate_mask),
            output_path.with_name(f"{output_path.stem}_candidate_mask.png"),
        )

    edge_mask = edge_connected_background_mask(candidate_mask)
    if debug:
        save_debug_image(
            mask_to_debug_image(edge_mask),
            output_path.with_name(f"{output_path.stem}_edge_connected_mask.png"),
        )

    image = apply_background_mask(image, edge_mask)
    if debug:
        save_debug_image(
            image, output_path.with_name(f"{output_path.stem}_after_bg_remove.png")
        )

    if edge_cleanup:
        image = cleanup_edge_pixels(image, background_color, tolerance, edge_tolerance_extra)
        if debug:
            save_debug_image(
                image,
                output_path.with_name(f"{output_path.stem}_after_edge_cleanup.png"),
            )

    if decontaminate_bg:
        image = decontaminate_background(image, background_color, tolerance, edge_tolerance_extra)
        if debug:
            save_debug_image(
                image,
                output_path.with_name(f"{output_path.stem}_after_decontaminate.png"),
            )

    cropped = crop_to_alpha(image)
    if cropped is None:
        print(f"Skipping {path.name}: no opaque pixels found after background removal.")
        return False

    canvas_size, fit_size = sprite_canvas_and_fit(path.stem, manifest, size)
    processed = resize_and_center_in_canvas(cropped, canvas_size, fit_size)
    processed = quantize_image(processed, colors)
    if outline:
        processed = add_outline(processed)

    if debug:
        save_debug_image(
            processed, output_path.with_name(f"{output_path.stem}_final.png")
        )

    output_path.parent.mkdir(parents=True, exist_ok=True)
    processed.save(output_path, format="PNG")
    return True


def process_folder(
    input_folder: Path,
    output_folder: Path,
    size: int,
    manifest: dict[str, dict[str, object]],
    colors: int,
    background_color: tuple[int, int, int],
    tolerance: int,
    outline: bool,
    auto_bg: bool,
    edge_cleanup: bool,
    edge_tolerance_extra: int,
    decontaminate_bg: bool,
    use_hsv_bg_detection: bool,
    debug: bool,
) -> list[Path]:
    input_folder = input_folder.expanduser()
    output_folder = output_folder.expanduser()
    if not input_folder.exists() or not input_folder.is_dir():
        raise FileNotFoundError(f"Input folder not found: {input_folder}")
    output_folder.mkdir(parents=True, exist_ok=True)
    image_files = get_image_paths(input_folder)
    if not image_files:
        print(f"No supported image files found in {input_folder}")
        return []
    saved_paths: list[Path] = []
    for image_path in image_files:
        output_path = output_folder / f"{image_path.stem}.png"
        print(f"Processing {image_path.name} -> {output_path.name}")
        if process_image(
            image_path,
            output_path,
            size,
            manifest,
            colors,
            background_color,
            tolerance,
            outline,
            auto_bg,
            edge_cleanup,
            edge_tolerance_extra,
            decontaminate_bg,
            use_hsv_bg_detection,
            debug,
        ):
            saved_paths.append(output_path)
    return saved_paths


def build_sprite_sheet(
    input_folder: Path,
    output_image_path: Path,
    tile_size: int,
    columns: int,
    metadata_path: Path,
    manifest: dict[str, dict[str, object]] | None = None,
) -> None:
    manifest = manifest or {}
    input_folder = input_folder.expanduser()
    sprite_files = order_sprite_files(
        [p for p in input_folder.iterdir() if p.is_file() and p.suffix.lower() == ".png"],
        manifest,
    )
    if not sprite_files:
        raise FileNotFoundError(f"No sprite PNG files found in {input_folder}")
    columns = max(1, columns)
    rows = math.ceil(len(sprite_files) / columns)
    sheet_width = columns * tile_size
    sheet_height = rows * tile_size
    sheet = Image.new("RGBA", (sheet_width, sheet_height), (0, 0, 0, 0))
    metadata = {
        "tile_size": tile_size,
        "columns": columns,
        "sprites": [],
    }
    for index, sprite_path in enumerate(sprite_files):
        sprite = load_image(sprite_path)
        if sprite.size != (tile_size, tile_size):
            print(
                f"Warning: sprite {sprite_path.name} is {sprite.size}, but sheet tile size is {tile_size}."
            )
        row = index // columns
        column = index % columns
        x = column * tile_size
        y = row * tile_size
        sheet.paste(sprite, (x, y), sprite)
        sprite_metadata = {
                "name": sprite_path.stem,
                "source_file": sprite_path.name,
                "sheet_x": x,
                "sheet_y": y,
                "width": sprite.width,
                "height": sprite.height,
        }
        sprite_metadata.update(manifest_metadata(sprite_path.stem, manifest))
        metadata["sprites"].append(sprite_metadata)
    output_image_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output_image_path, format="PNG")
    metadata_path.parent.mkdir(parents=True, exist_ok=True)
    metadata_path.write_text(json.dumps(metadata, indent=2))


def build_sprite_atlas(
    input_folder: Path,
    output_image_path: Path,
    columns: int,
    metadata_path: Path,
    manifest: dict[str, dict[str, object]] | None = None,
) -> None:
    manifest = manifest or {}
    input_folder = input_folder.expanduser()
    sprite_files = order_sprite_files(
        [p for p in input_folder.iterdir() if p.is_file() and p.suffix.lower() == ".png"],
        manifest,
    )
    if not sprite_files:
        raise FileNotFoundError(f"No sprite PNG files found in {input_folder}")

    columns = max(1, columns)
    sprites = [(sprite_path, load_image(sprite_path)) for sprite_path in sprite_files]
    column_widths = [0] * columns
    row_heights: list[int] = []
    placements: list[tuple[Path, Image.Image, int, int, int]] = []

    for index, (sprite_path, sprite) in enumerate(sprites):
        row = index // columns
        column = index % columns
        if row == len(row_heights):
            row_heights.append(0)
        column_widths[column] = max(column_widths[column], sprite.width)
        row_heights[row] = max(row_heights[row], sprite.height)
        placements.append((sprite_path, sprite, row, column, index))

    column_x: list[int] = []
    current_x = 0
    for width in column_widths:
        column_x.append(current_x)
        current_x += width

    row_y: list[int] = []
    current_y = 0
    for height in row_heights:
        row_y.append(current_y)
        current_y += height

    sheet = Image.new("RGBA", (current_x, current_y), (0, 0, 0, 0))
    metadata = {
        "layout": "variable",
        "columns": columns,
        "sprites": [],
    }

    for sprite_path, sprite, row, column, _ in placements:
        x = column_x[column]
        y = row_y[row]
        sheet.paste(sprite, (x, y), sprite)
        sprite_metadata = {
            "name": sprite_path.stem,
            "source_file": sprite_path.name,
            "sheet_x": x,
            "sheet_y": y,
            "width": sprite.width,
            "height": sprite.height,
        }
        sprite_metadata.update(manifest_metadata(sprite_path.stem, manifest))
        metadata["sprites"].append(sprite_metadata)

    output_image_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output_image_path, format="PNG")
    metadata_path.parent.mkdir(parents=True, exist_ok=True)
    metadata_path.write_text(json.dumps(metadata, indent=2))


def split_grid_sheet(
    input_path: Path,
    output_folder: Path,
    names: list[str],
    rows: int,
    columns: int,
    background_color: tuple[int, int, int],
    tolerance: int,
    padding: int,
    manifest_path: Path | None = None,
    canvas_size: tuple[int, int] | None = None,
    fit_size: tuple[int, int] | None = None,
    footprint_tiles: tuple[int, int] | None = None,
    category: str | None = None,
    component_split: bool = False,
    min_component_area: int = 64,
) -> None:
    expected_count = rows * columns
    if len(names) != expected_count:
        raise ValueError(f"Expected {expected_count} names for a {columns}x{rows} grid, got {len(names)}.")

    image = load_image(input_path.expanduser())
    bounds = find_background_bounds(image, background_color, tolerance)
    if bounds is None:
        raise ValueError(f"Could not find background color {rgb_to_hex(background_color)} in {input_path}.")

    left, top, right, bottom = bounds
    sheet = image.crop(bounds)
    cell_width = sheet.width / columns
    cell_height = sheet.height / rows

    output_folder = output_folder.expanduser()
    output_folder.mkdir(parents=True, exist_ok=True)
    manifest: dict[str, dict[str, object]] = {}

    if component_split:
        arr = np.array(sheet)
        target = np.array(background_color, dtype=np.int32)
        rgb = arr[..., :3].astype(np.int32)
        alpha = arr[..., 3] > 0
        distance2 = np.sum((rgb - target) ** 2, axis=-1)
        foreground = (distance2 > (tolerance * tolerance)) & alpha
        assignments: list[list[dict[str, object]]] = [[] for _ in names]
        for component in connected_components(foreground, min_component_area):
            center_x, center_y = component["center"]
            column = min(columns - 1, max(0, int(center_x / cell_width)))
            row = min(rows - 1, max(0, int(center_y / cell_height)))
            assignments[row * columns + column].append(component)

    for index, name in enumerate(names):
        if component_split:
            components = assignments[index]
            if not components:
                print(f"Warning: no foreground component found for {name}; falling back to grid cell.")
                row = index // columns
                column = index % columns
                cell_left = max(0, round(column * cell_width) - padding)
                cell_top = max(0, round(row * cell_height) - padding)
                cell_right = min(sheet.width, round((column + 1) * cell_width) + padding)
                cell_bottom = min(sheet.height, round((row + 1) * cell_height) + padding)
                cell = sheet.crop((cell_left, cell_top, cell_right, cell_bottom))
            else:
                min_x = max(0, min(component["bbox"][0] for component in components) - padding)
                min_y = max(0, min(component["bbox"][1] for component in components) - padding)
                max_x = min(sheet.width, max(component["bbox"][2] for component in components) + padding)
                max_y = min(sheet.height, max(component["bbox"][3] for component in components) + padding)
                cell_arr = np.full((max_y - min_y, max_x - min_x, 4), [*background_color, 255], dtype=np.uint8)
                for component in components:
                    for y, x in component["pixels"]:
                        cell_arr[y - min_y, x - min_x] = arr[y, x]
                cell = Image.fromarray(cell_arr, mode="RGBA")
        else:
            row = index // columns
            column = index % columns
            cell_left = max(0, round(column * cell_width) - padding)
            cell_top = max(0, round(row * cell_height) - padding)
            cell_right = min(sheet.width, round((column + 1) * cell_width) + padding)
            cell_bottom = min(sheet.height, round((row + 1) * cell_height) + padding)
            cell = sheet.crop((cell_left, cell_top, cell_right, cell_bottom))

        cell.save(output_folder / f"{name}.png", format="PNG")

        if manifest_path is not None:
            entry: dict[str, object] = {}
            if canvas_size is not None:
                entry["canvas"] = list(canvas_size)
            if fit_size is not None:
                entry["fit"] = list(fit_size)
            if canvas_size is not None:
                entry["anchor"] = [canvas_size[0] // 2, max(0, canvas_size[1] - 8)]
            if footprint_tiles is not None:
                entry["footprint_tiles"] = list(footprint_tiles)
            if category is not None:
                entry["category"] = category
            manifest[name] = entry

    if manifest_path is not None:
        manifest_path.parent.mkdir(parents=True, exist_ok=True)
        manifest_path.write_text(json.dumps(manifest, indent=2))


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Sprite pipeline for converting raw building artwork into 96x96 pixel game sprites."
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    process_parser = subparsers.add_parser("process", help="Convert raw images into processed sprites.")
    process_parser.add_argument("--input", required=True, help="Input folder containing raw image files.")
    process_parser.add_argument("--output", required=True, help="Output folder for processed PNG sprites.")
    process_parser.add_argument("--size", type=int, default=96, help="Target sprite size in pixels.")
    process_parser.add_argument("--manifest", help="Optional JSON manifest keyed by input file stem for per-sprite canvas, fit, anchor, and footprint settings.")
    process_parser.add_argument("--colors", type=int, default=32, help="Palette size for quantization.")
    process_parser.add_argument("--bg", default=DEFAULT_BG_COLOR, help="Background color to remove.")
    process_parser.add_argument("--auto-bg", action="store_true", help="Detect the background color from the image border.")
    process_parser.add_argument("--tolerance", type=int, default=40, help="Tolerance for background removal.")
    process_parser.add_argument("--edge-cleanup", action="store_true", help="Remove pixels adjacent to transparent background if they are close to the background color.")
    process_parser.add_argument("--edge-tolerance-extra", type=int, default=15, help="Extra tolerance used by edge cleanup and decontamination.")
    process_parser.add_argument("--decontaminate-bg", action="store_true", help="Reduce background color contamination on visible edge pixels.")
    process_parser.add_argument("--use-hsv-bg-detection", action="store_true", help="Enable hue-based background detection in addition to strict RGB distance.")
    process_parser.add_argument("--outline", action="store_true", help="Add a 1px dark outline behind each sprite.")
    process_parser.add_argument("--debug", action="store_true", help="Write debug masks and intermediate command images for each input file.")

    sheet_parser = subparsers.add_parser("sheet", help="Generate a sprite sheet from processed sprites.")
    sheet_parser.add_argument("--input", required=True, help="Input folder containing processed sprite PNGs.")
    sheet_parser.add_argument("--output", required=True, help="Output path for the generated sprite sheet PNG.")
    sheet_parser.add_argument("--tile-size", type=int, default=96, help="Tile size used in the sprite sheet.")
    sheet_parser.add_argument("--cols", type=int, default=8, help="Number of columns in the sprite sheet.")
    sheet_parser.add_argument("--metadata", required=True, help="Output path for the sheet metadata JSON file.")
    sheet_parser.add_argument("--manifest", help="Optional JSON manifest keyed by sprite file stem for metadata such as anchor and footprint_tiles.")
    sheet_parser.add_argument("--variable-atlas", action="store_true", help="Pack sprites at their actual dimensions instead of forcing a fixed tile grid.")

    split_parser = subparsers.add_parser("split-grid", help="Split a generated grid sheet or screenshot into named raw sprite images.")
    split_parser.add_argument("--input", required=True, help="Input generated grid image or full screenshot.")
    split_parser.add_argument("--output", required=True, help="Output folder for named raw cell images.")
    split_parser.add_argument("--names", required=True, help="Comma-separated sprite names in left-to-right, top-to-bottom grid order.")
    split_parser.add_argument("--rows", type=int, default=3, help="Number of grid rows.")
    split_parser.add_argument("--cols", type=int, default=3, help="Number of grid columns.")
    split_parser.add_argument("--bg", default=DEFAULT_BG_COLOR, help="Background color used behind the generated sheet.")
    split_parser.add_argument("--tolerance", type=int, default=40, help="Tolerance for finding the generated sheet background inside a screenshot.")
    split_parser.add_argument("--padding", type=int, default=0, help="Extra pixels to include around each grid cell.")
    split_parser.add_argument("--manifest-output", help="Optional path to write a starter manifest for the split sprites.")
    split_parser.add_argument("--canvas", help="Optional manifest canvas size as WIDTHxHEIGHT, for example 96x96.")
    split_parser.add_argument("--fit", help="Optional manifest fit size as WIDTHxHEIGHT, for example 92x92.")
    split_parser.add_argument("--footprint-tiles", help="Optional manifest footprint as WIDTHxHEIGHT, for example 6x6.")
    split_parser.add_argument("--category", help="Optional manifest category value.")
    split_parser.add_argument("--component-split", action="store_true", help="Detect foreground components and assign them to grid slots by center point instead of slicing fixed cells.")
    split_parser.add_argument("--min-component-area", type=int, default=64, help="Minimum foreground component area to keep when using --component-split.")

    all_parser = subparsers.add_parser("all", help="Run the full sprite pipeline and generate a sheet.")
    all_parser.add_argument("--input", required=True, help="Input folder containing raw image files.")
    all_parser.add_argument("--sprites", required=True, help="Output folder for processed sprites.")
    all_parser.add_argument("--sheet", required=True, help="Output path for the generated sprite sheet PNG.")
    all_parser.add_argument("--metadata", required=True, help="Output path for the sheet metadata JSON file.")
    all_parser.add_argument("--size", type=int, default=96, help="Target sprite size in pixels.")
    all_parser.add_argument("--cols", type=int, default=8, help="Number of columns in the generated sheet or atlas.")
    all_parser.add_argument("--manifest", help="Optional JSON manifest keyed by input file stem for per-sprite canvas, fit, anchor, and footprint settings.")
    all_parser.add_argument("--colors", type=int, default=32, help="Palette size for quantization.")
    all_parser.add_argument("--bg", default=DEFAULT_BG_COLOR, help="Background color to remove.")
    all_parser.add_argument("--auto-bg", action="store_true", help="Detect the background color from the image border.")
    all_parser.add_argument("--tolerance", type=int, default=40, help="Tolerance for background removal.")
    all_parser.add_argument("--edge-cleanup", action="store_true", help="Remove pixels adjacent to transparent background if they are close to the background color.")
    all_parser.add_argument("--edge-tolerance-extra", type=int, default=15, help="Extra tolerance used by edge cleanup and decontamination.")
    all_parser.add_argument("--decontaminate-bg", action="store_true", help="Reduce background color contamination on visible edge pixels.")
    all_parser.add_argument("--use-hsv-bg-detection", action="store_true", help="Enable hue-based background detection in addition to strict RGB distance.")
    all_parser.add_argument("--outline", action="store_true", help="Add a 1px dark outline behind processed sprites.")
    all_parser.add_argument("--debug", action="store_true", help="Write debug masks and intermediate command images for each input file.")
    all_parser.add_argument("--variable-atlas", action="store_true", help="Pack sprites at their actual dimensions instead of forcing a fixed tile grid.")

    args = parser.parse_args()
    try:
        if args.command in ("process", "all", "split-grid"):
            background_color = parse_hex_color(args.bg)
        manifest = load_sprite_manifest(Path(args.manifest) if getattr(args, "manifest", None) else None)
    except ValueError as exc:
        print(exc)
        return 1

    if args.command == "process":
        process_folder(
            Path(args.input),
            Path(args.output),
            args.size,
            manifest,
            args.colors,
            background_color,
            args.tolerance,
            args.outline,
            args.auto_bg,
            args.edge_cleanup,
            args.edge_tolerance_extra,
            args.decontaminate_bg,
            args.use_hsv_bg_detection,
            args.debug,
        )
    elif args.command == "sheet":
        if args.variable_atlas:
            build_sprite_atlas(
                Path(args.input),
                Path(args.output),
                args.cols,
                Path(args.metadata),
                manifest,
            )
        else:
            build_sprite_sheet(
                Path(args.input),
                Path(args.output),
                args.tile_size,
                args.cols,
                Path(args.metadata),
                manifest,
            )
    elif args.command == "all":
        process_folder(
            Path(args.input),
            Path(args.sprites),
            args.size,
            manifest,
            args.colors,
            background_color,
            args.tolerance,
            args.outline,
            args.auto_bg,
            args.edge_cleanup,
            args.edge_tolerance_extra,
            args.decontaminate_bg,
            args.use_hsv_bg_detection,
            args.debug,
        )
        if args.variable_atlas:
            build_sprite_atlas(
                Path(args.sprites),
                Path(args.sheet),
                args.cols,
                Path(args.metadata),
                manifest,
            )
        else:
            build_sprite_sheet(
                Path(args.sprites),
                Path(args.sheet),
                args.size,
                args.cols,
                Path(args.metadata),
                manifest,
            )
    elif args.command == "split-grid":
        try:
            split_grid_sheet(
                Path(args.input),
                Path(args.output),
                split_csv(args.names),
                args.rows,
                args.cols,
                background_color,
                args.tolerance,
                args.padding,
                Path(args.manifest_output) if args.manifest_output else None,
                parse_dimension_arg(args.canvas) if args.canvas else None,
                parse_dimension_arg(args.fit) if args.fit else None,
                parse_dimension_arg(args.footprint_tiles) if args.footprint_tiles else None,
                args.category,
                args.component_split,
                args.min_component_area,
            )
        except ValueError as exc:
            print(exc)
            return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
