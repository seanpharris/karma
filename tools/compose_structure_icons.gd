extends SceneTree

# Compose 64×64 medieval structure icons from the Cainos top-down
# basic asset pack (CC0). For each entry below, the script crops the
# named rectangle from the source PNG, centers it in a 64×64 canvas,
# and writes assets/art/themes/medieval/structures/<id>.png.
#
# Cainos sheets are 512×512 with sprites positioned at varied
# rectangles — there's no clean grid, so coordinates are hand-picked
# by visual inspection. Adjust freely.
#
# Run via:
#   godot --headless --path . --script res://tools/compose_structure_icons.gd

const CAINOS_ROOT := "res://assets/art/third_party/cainos_pixel_art_top_down_basic_v1_2_3/Texture/"
const OUT_DIR := "res://assets/art/themes/medieval/structures/"
const TARGET := 64
const ICON_PADDING := 1

# Each entry: { id, src, x, y, w, h }
# x/y/w/h are pixel coords inside the source PNG.
const STRUCTURE_SOURCES := [
    # Doors / entries (TX Props.png — second row)
    { "id": "wooden_door", "src": "TX Props.png", "x": 0, "y": 64, "w": 48, "h": 96 },
    { "id": "iron_door", "src": "TX Props.png", "x": 96, "y": 64, "w": 48, "h": 96 },
    # Notice board / sign post
    { "id": "notice_board", "src": "TX Props.png", "x": 0, "y": 192, "w": 48, "h": 80 },
    { "id": "wooden_sign", "src": "TX Props.png", "x": 0, "y": 256, "w": 48, "h": 64 },
    # Storage / containers
    { "id": "chest", "src": "TX Props.png", "x": 48, "y": 0, "w": 48, "h": 48 },
    { "id": "crate", "src": "TX Props.png", "x": 96, "y": 0, "w": 48, "h": 48 },
    # Tavern / barn
    { "id": "barrel", "src": "TX Props.png", "x": 96, "y": 192, "w": 32, "h": 64 },
    # Chapel / shrine
    { "id": "stone_cross", "src": "TX Props.png", "x": 144, "y": 256, "w": 32, "h": 64 },
    { "id": "statue", "src": "TX Props.png", "x": 336, "y": 0, "w": 64, "h": 96 },
    { "id": "tombstone", "src": "TX Props.png", "x": 192, "y": 192, "w": 32, "h": 48 },
    # Pots / urns (oddity yard)
    { "id": "clay_pot", "src": "TX Props.png", "x": 192, "y": 256, "w": 48, "h": 64 },
    { "id": "ceramic_urn", "src": "TX Props.png", "x": 144, "y": 144, "w": 48, "h": 64 },
    # Forge / workshop
    { "id": "anvil_block", "src": "TX Props.png", "x": 240, "y": 240, "w": 64, "h": 64 },
    { "id": "stone_pedestal", "src": "TX Props.png", "x": 256, "y": 144, "w": 64, "h": 80 },
    # Bell / broadcast tower
    { "id": "bell_stand", "src": "TX Props.png", "x": 384, "y": 144, "w": 96, "h": 96 },
    # Duel ring / arena
    { "id": "stone_ring", "src": "TX Props.png", "x": 320, "y": 320, "w": 96, "h": 80 },
    # Wall sections (TX Struct.png)
    { "id": "stone_wall", "src": "TX Struct.png", "x": 0, "y": 0, "w": 80, "h": 128 },
    { "id": "stone_arch", "src": "TX Struct.png", "x": 384, "y": 0, "w": 96, "h": 80 },
    { "id": "stone_arch_open", "src": "TX Struct.png", "x": 384, "y": 96, "w": 96, "h": 80 },
    { "id": "stone_stairs", "src": "TX Struct.png", "x": 0, "y": 256, "w": 144, "h": 96 },
    # Plants (TX Plant.png) — landscape props
    { "id": "tree_oak", "src": "TX Plant.png", "x": 0, "y": 0, "w": 144, "h": 192 },
    { "id": "tree_cypress", "src": "TX Plant.png", "x": 144, "y": 0, "w": 144, "h": 192 },
    { "id": "bush_green", "src": "TX Plant.png", "x": 0, "y": 192, "w": 64, "h": 64 },
    { "id": "bush_round", "src": "TX Plant.png", "x": 224, "y": 192, "w": 64, "h": 64 },
]

func _init() -> void:
    var dir := DirAccess.open("res://")
    dir.make_dir_recursive(OUT_DIR.replace("res://", ""))

    var ok := 0
    var skipped := 0
    var failed := 0

    for entry in STRUCTURE_SOURCES:
        var result := _compose(entry)
        match result:
            "ok": ok += 1
            "skipped": skipped += 1
            "failed": failed += 1

    print("compose_structure_icons: ok=%d skipped=%d failed=%d" % [ok, skipped, failed])
    quit(0)

func _compose(entry: Dictionary) -> String:
    var item_id: String = entry["id"]
    var src: String = entry["src"]
    var x: int = entry["x"]
    var y: int = entry["y"]
    var w: int = entry["w"]
    var h: int = entry["h"]

    var sheet_path := CAINOS_ROOT + src
    if not FileAccess.file_exists(sheet_path):
        push_warning("compose_structure_icons: source missing: %s" % sheet_path)
        return "skipped"

    var sheet: Image = Image.load_from_file(sheet_path)
    if sheet == null:
        push_warning("compose_structure_icons: failed to load %s" % sheet_path)
        return "skipped"
    if sheet.get_format() != Image.FORMAT_RGBA8:
        sheet.convert(Image.FORMAT_RGBA8)

    var crop_rect := Rect2i(x, y, w, h)
    if x < 0 or y < 0 or x + w > sheet.get_width() or y + h > sheet.get_height():
        push_warning("compose_structure_icons: rect out of bounds for %s: (%d,%d,%d,%d) sheet=%dx%d" %
            [item_id, x, y, w, h, sheet.get_width(), sheet.get_height()])
        return "skipped"

    var cropped := sheet.get_region(crop_rect)
    var bbox := _opaque_bbox(cropped)
    if bbox.size.x == 0 or bbox.size.y == 0:
        push_warning("compose_structure_icons: empty crop for %s" % item_id)
        return "skipped"
    bbox = _expand_with_padding(bbox, ICON_PADDING, cropped.get_size())
    var trimmed := cropped.get_region(bbox)
    var canvas := _scale_into_target(trimmed)

    var out_path := OUT_DIR + item_id + ".png"
    var save_err := canvas.save_png(out_path)
    if save_err != OK:
        push_error("compose_structure_icons: save_png failed for %s (%d)" % [item_id, save_err])
        return "failed"
    return "ok"

func _opaque_bbox(image: Image) -> Rect2i:
    var width := image.get_width()
    var height := image.get_height()
    var min_x := width
    var min_y := height
    var max_x := -1
    var max_y := -1
    for y in range(height):
        for x in range(width):
            if image.get_pixel(x, y).a > 0.05:
                if x < min_x: min_x = x
                if y < min_y: min_y = y
                if x > max_x: max_x = x
                if y > max_y: max_y = y
    if max_x < 0:
        return Rect2i(0, 0, 0, 0)
    return Rect2i(min_x, min_y, max_x - min_x + 1, max_y - min_y + 1)

func _expand_with_padding(rect: Rect2i, padding: int, max_size: Vector2i) -> Rect2i:
    var x: int = max(0, rect.position.x - padding)
    var y: int = max(0, rect.position.y - padding)
    var w_max: int = min(max_size.x - x, rect.size.x + 2 * padding)
    var h_max: int = min(max_size.y - y, rect.size.y + 2 * padding)
    return Rect2i(x, y, w_max, h_max)

func _scale_into_target(cropped: Image) -> Image:
    var w := cropped.get_width()
    var h := cropped.get_height()
    var max_dim: int = max(w, h)
    var scaled: Image = cropped.duplicate()

    if max_dim != TARGET:
        var scale_factor: float = float(TARGET) / float(max_dim)
        var new_w: int = max(1, int(round(w * scale_factor)))
        var new_h: int = max(1, int(round(h * scale_factor)))
        new_w = min(new_w, TARGET)
        new_h = min(new_h, TARGET)
        scaled.resize(new_w, new_h, Image.INTERPOLATE_NEAREST)

    var canvas := Image.create(TARGET, TARGET, false, Image.FORMAT_RGBA8)
    canvas.fill(Color(0, 0, 0, 0))
    var sw := scaled.get_width()
    var sh := scaled.get_height()
    var ox: int = int((TARGET - sw) / 2)
    var oy: int = int((TARGET - sh) / 2)
    canvas.blend_rect(scaled, Rect2i(0, 0, sw, sh), Vector2i(ox, oy))
    return canvas
