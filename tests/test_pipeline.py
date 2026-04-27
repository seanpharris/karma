import json
from pathlib import Path

from PIL import Image, ImageDraw

from src.pipeline import (
    build_sprite_atlas,
    build_sprite_sheet,
    crop_to_alpha,
    estimate_background_color,
    quantize_image,
    remove_background_alpha,
    resize_and_center,
    resize_and_center_in_canvas,
    process_folder,
)


def test_custom_background_color_removal():
    image = Image.new("RGBA", (5, 5), (0, 255, 0, 255))
    draw = ImageDraw.Draw(image)
    draw.rectangle([1, 1, 3, 3], fill=(255, 0, 0, 255))
    output = remove_background_alpha(image, (0, 255, 0), 10)
    assert output.getpixel((0, 0))[3] == 0
    assert output.getpixel((2, 2))[3] == 255


def test_auto_background_detection_from_border():
    image = Image.new("RGBA", (5, 5), (0, 255, 0, 255))
    draw = ImageDraw.Draw(image)
    draw.rectangle([1, 1, 3, 3], fill=(255, 0, 0, 255))
    detected = estimate_background_color(image)
    assert detected == (0, 255, 0)


def test_edge_connected_removal_preserves_interior_key_color():
    image = Image.new("RGBA", (7, 7), (0, 255, 0, 255))
    draw = ImageDraw.Draw(image)
    draw.rectangle([1, 1, 5, 5], fill=(255, 0, 0, 255))
    image.putpixel((3, 3), (0, 255, 0, 255))
    output = remove_background_alpha(image, (0, 255, 0), 10)
    assert output.getpixel((3, 3))[3] == 255


def test_no_accidental_removal_of_dark_blue_sprite_pixels():
    image = Image.new("RGBA", (5, 5), (255, 0, 255, 255))
    draw = ImageDraw.Draw(image)
    draw.rectangle([1, 1, 3, 3], fill=(30, 0, 80, 255))
    output = remove_background_alpha(image, (255, 0, 255), 30)
    assert output.getpixel((2, 2))[3] == 255


def test_quantize_preserves_transparency():
    image = Image.new("RGBA", (4, 4), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rectangle([0, 0, 1, 1], fill=(10, 20, 30, 255))
    output = quantize_image(image, colors=8)
    alpha = output.split()[3]
    assert alpha.getpixel((2, 2)) == 0
    assert alpha.getpixel((0, 0)) == 255


def test_crop_to_alpha():
    image = Image.new("RGBA", (8, 8), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rectangle([2, 2, 4, 5], fill=(255, 255, 255, 255))
    cropped = crop_to_alpha(image)
    assert cropped is not None
    assert cropped.size == (3, 4)


def test_resize_and_center():
    image = Image.new("RGBA", (16, 8), (255, 0, 0, 255))
    result = resize_and_center(image, 96)
    assert result.size == (96, 96)
    alpha = result.split()[3]
    bbox = alpha.getbbox()
    assert bbox == (0, 24, 96, 72)


def test_resize_and_center_in_rectangular_canvas():
    image = Image.new("RGBA", (20, 10), (255, 0, 0, 255))
    result = resize_and_center_in_canvas(image, (48, 32), (40, 24))
    assert result.size == (48, 32)
    alpha = result.split()[3]
    bbox = alpha.getbbox()
    assert bbox == (4, 6, 44, 26)


def test_debug_files_created(tmp_path: Path):
    sprite_dir = tmp_path / "input"
    sprite_dir.mkdir()
    sprite = Image.new("RGBA", (8, 8), (0, 255, 0, 255))
    draw = ImageDraw.Draw(sprite)
    draw.rectangle([1, 1, 6, 6], fill=(255, 0, 0, 255))
    sprite.save(sprite_dir / "building.png")

    output_dir = tmp_path / "output"
    process_folder(
        sprite_dir,
        output_dir,
        size=96,
        manifest={},
        colors=32,
        background_color=(0, 255, 0),
        tolerance=10,
        outline=False,
        auto_bg=True,
        edge_cleanup=True,
        edge_tolerance_extra=5,
        decontaminate_bg=True,
        use_hsv_bg_detection=False,
        debug=True,
    )

    assert (output_dir / "building.png").exists()
    assert (output_dir / "building_detected_bg.txt").exists()
    assert (output_dir / "building_candidate_mask.png").exists()
    assert (output_dir / "building_edge_connected_mask.png").exists()
    assert (output_dir / "building_after_bg_remove.png").exists()
    assert (output_dir / "building_final.png").exists()


def test_process_folder_uses_manifest_canvas(tmp_path: Path):
    sprite_dir = tmp_path / "input"
    sprite_dir.mkdir()
    sprite = Image.new("RGBA", (10, 10), (0, 255, 0, 255))
    draw = ImageDraw.Draw(sprite)
    draw.rectangle([2, 2, 7, 7], fill=(255, 0, 0, 255))
    sprite.save(sprite_dir / "fountain.png")

    output_dir = tmp_path / "output"
    process_folder(
        sprite_dir,
        output_dir,
        size=96,
        manifest={"fountain": {"canvas": [48, 48], "fit": [40, 40]}},
        colors=32,
        background_color=(0, 255, 0),
        tolerance=10,
        outline=False,
        auto_bg=False,
        edge_cleanup=False,
        edge_tolerance_extra=5,
        decontaminate_bg=False,
        use_hsv_bg_detection=False,
        debug=False,
    )

    output = Image.open(output_dir / "fountain.png")
    assert output.size == (48, 48)


def test_build_sprite_sheet_metadata(tmp_path: Path):
    sprite_dir = tmp_path / "sprites"
    sprite_dir.mkdir()
    for idx in range(2):
        sprite = Image.new("RGBA", (96, 96), (255, 0, 0, 255))
        sprite.save(sprite_dir / f"building_{idx}.png")

    output_image = tmp_path / "sheet.png"
    metadata_file = tmp_path / "sheet.json"
    build_sprite_sheet(sprite_dir, output_image, tile_size=96, columns=2, metadata_path=metadata_file)

    assert output_image.exists()
    data = json.loads(metadata_file.read_text())
    assert data["tile_size"] == 96
    assert data["columns"] == 2
    assert len(data["sprites"]) == 2
    assert data["sprites"][0]["sheet_x"] == 0
    assert data["sprites"][1]["sheet_x"] == 96


def test_build_variable_sprite_atlas_metadata(tmp_path: Path):
    sprite_dir = tmp_path / "sprites"
    sprite_dir.mkdir()
    Image.new("RGBA", (96, 96), (255, 0, 0, 255)).save(sprite_dir / "main_hall.png")
    Image.new("RGBA", (48, 48), (0, 0, 255, 255)).save(sprite_dir / "fountain.png")

    output_image = tmp_path / "atlas.png"
    metadata_file = tmp_path / "atlas.json"
    build_sprite_atlas(
        sprite_dir,
        output_image,
        columns=2,
        metadata_path=metadata_file,
        manifest={"fountain": {"anchor": [24, 42], "footprint_tiles": [2, 2]}},
    )

    assert output_image.exists()
    data = json.loads(metadata_file.read_text())
    assert data["layout"] == "variable"
    assert data["columns"] == 2
    assert len(data["sprites"]) == 2
    fountain = next(sprite for sprite in data["sprites"] if sprite["name"] == "fountain")
    assert fountain["width"] == 48
    assert fountain["height"] == 48
    assert fountain["anchor"] == [24, 42]
    assert fountain["footprint_tiles"] == [2, 2]
