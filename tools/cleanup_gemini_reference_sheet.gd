extends SceneTree

const SOURCE := "res://assets/art/reference/gemini_prototypes/2026-04-27/karma_natural_props_ref.jpg"
const OUT_DIR := "res://assets/art/sprites/generated/gemini_natural_props_2026_04_27"
const STATIC_SOURCE := "res://assets/art/reference/gemini_prototypes/2026-04-27/karma_static_props_ref.jpg"
const STATIC_OUT_DIR := "res://assets/art/sprites/generated/gemini_static_props_2026_04_27"
const CELL := 256
const OUT_CELL := 128
const GRID := 4
const BG_DISTANCE_THRESHOLD := 0.18
const EDGE_ALPHA_THRESHOLD := 0.22

const NAMES := [
    "alien_shrub",
    "dry_grass_clump",
    "moss_patch",
    "tiny_flowering_plant",
    "mushroom_cluster",
    "red_mineral_rock",
    "blue_crystal_shard",
    "fallen_branch",
    "dead_bush",
    "smooth_river_stone",
    "cactus_succulent",
    "glowing_lichen_patch",
    "puddle",
    "cracked_mud_clump",
    "berry_bush",
    "small_sapling",
]

const STATIC_NAMES := [
    "cargo_crate",
    "repair_kit_case",
    "utility_junction_box",
    "hydroponics_planter",
    "oxygen_tank_rack",
    "compact_kiosk_terminal",
    "airlock_door_front",
    "station_wall_segment",
    "solar_panel",
    "pipe_cluster",
    "landing_beacon",
    "exterior_lamp_post",
    "maintenance_hatch",
    "medical_supply_box",
    "portable_terminal",
    "power_cell_canister",
]

func _initialize() -> void:
    process_sheet(SOURCE, OUT_DIR, NAMES, "gemini_natural_props")
    process_sheet(STATIC_SOURCE, STATIC_OUT_DIR, STATIC_NAMES, "gemini_static_props")
    quit(0)

func process_sheet(source_path: String, out_dir: String, names: Array, stem: String) -> void:
    var source := Image.load_from_file(source_path)
    if source == null or source.is_empty():
        push_error("Unable to load " + source_path)
        quit(1)
        return

    DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(out_dir))
    var bg := estimate_background(source)
    var atlas := Image.create(OUT_CELL * GRID, OUT_CELL * GRID, false, Image.FORMAT_RGBA8)
    atlas.fill(Color(0, 0, 0, 0))
    var contact := Image.create(OUT_CELL * GRID, OUT_CELL * GRID, false, Image.FORMAT_RGBA8)
    contact.fill(Color(0.12, 0.12, 0.12, 1))

    var index := 0
    var manifest := []
    for row in range(GRID):
        for col in range(GRID):
            var name: String = names[index]
            var raw := crop_cell(source, col * CELL, row * CELL, CELL, CELL)
            var isolated := isolate_background(raw, bg)
            var trimmed := trim_to_content(isolated, 8)
            var normalized := center_on_canvas(trimmed, OUT_CELL, OUT_CELL)
            var path: String = out_dir + "/" + name + ".png"
            normalized.save_png(path)
            blit(atlas, normalized, col * OUT_CELL, row * OUT_CELL)
            blit(contact, normalized, col * OUT_CELL, row * OUT_CELL)
            manifest.append({
                "name": name,
                "path": path,
                "source_cell": [col, row],
                "canvas": [OUT_CELL, OUT_CELL]
            })
            index += 1

    atlas.save_png(out_dir + "/" + stem + "_cleaned_atlas.png")
    contact.save_png(out_dir + "/" + stem + "_cleaned_contact.png")
    FileAccess.open(out_dir + "/manifest.json", FileAccess.WRITE).store_string(JSON.stringify(manifest, "  "))
    print("background ~= ", bg, " for ", source_path)
    print("wrote ", out_dir)

func estimate_background(image: Image) -> Color:
    var samples := [
        image.get_pixel(4, 4),
        image.get_pixel(image.get_width() - 5, 4),
        image.get_pixel(4, image.get_height() - 5),
        image.get_pixel(image.get_width() - 5, image.get_height() - 5),
        image.get_pixel(image.get_width() / 2, 4),
        image.get_pixel(4, image.get_height() / 2),
    ]
    var r := 0.0
    var g := 0.0
    var b := 0.0
    for c in samples:
        r += c.r
        g += c.g
        b += c.b
    return Color(r / samples.size(), g / samples.size(), b / samples.size(), 1)

func crop_cell(image: Image, x0: int, y0: int, width: int, height: int) -> Image:
    var out := Image.create(width, height, false, Image.FORMAT_RGBA8)
    for y in range(height):
        for x in range(width):
            var c := image.get_pixel(x0 + x, y0 + y)
            c.a = 1.0
            out.set_pixel(x, y, c)
    return out

func isolate_background(image: Image, bg: Color) -> Image:
    var out := Image.create(image.get_width(), image.get_height(), false, Image.FORMAT_RGBA8)
    for y in range(image.get_height()):
        for x in range(image.get_width()):
            var c := image.get_pixel(x, y)
            var d := color_distance(c, bg)
            # Preserve strong dark outlines even when low saturation; remove gray JPG background and its soft edge noise.
            if d < BG_DISTANCE_THRESHOLD or is_edge_background_noise(c, bg, d):
                c.a = 0.0
            else:
                c.a = 1.0
            out.set_pixel(x, y, c)
    return out

func is_edge_background_noise(c: Color, bg: Color, distance: float) -> bool:
    var max_chan: float = maxf(c.r, maxf(c.g, c.b))
    var min_chan: float = minf(c.r, minf(c.g, c.b))
    var saturation: float = max_chan - min_chan
    var brightness_delta: float = absf(((c.r + c.g + c.b) / 3.0) - ((bg.r + bg.g + bg.b) / 3.0))
    return saturation < 0.055 and brightness_delta < EDGE_ALPHA_THRESHOLD and distance < 0.30

func color_distance(a: Color, b: Color) -> float:
    var dr := a.r - b.r
    var dg := a.g - b.g
    var db := a.b - b.b
    return sqrt(dr * dr + dg * dg + db * db)

func trim_to_content(image: Image, pad: int) -> Image:
    var min_x := image.get_width()
    var min_y := image.get_height()
    var max_x := -1
    var max_y := -1
    for y in range(image.get_height()):
        for x in range(image.get_width()):
            if image.get_pixel(x, y).a > 0.5:
                min_x = min(min_x, x)
                min_y = min(min_y, y)
                max_x = max(max_x, x)
                max_y = max(max_y, y)
    if max_x < min_x or max_y < min_y:
        var blank := Image.create(OUT_CELL, OUT_CELL, false, Image.FORMAT_RGBA8)
        blank.fill(Color(0, 0, 0, 0))
        return blank
    min_x = max(0, min_x - pad)
    min_y = max(0, min_y - pad)
    max_x = min(image.get_width() - 1, max_x + pad)
    max_y = min(image.get_height() - 1, max_y + pad)
    var w := max_x - min_x + 1
    var h := max_y - min_y + 1
    var out := Image.create(w, h, false, Image.FORMAT_RGBA8)
    out.fill(Color(0, 0, 0, 0))
    for y in range(h):
        for x in range(w):
            out.set_pixel(x, y, image.get_pixel(min_x + x, min_y + y))
    return out

func center_on_canvas(image: Image, width: int, height: int) -> Image:
    var scaled: Image = image
    var scale: float = minf(float(width - 8) / maxf(1.0, float(image.get_width())), float(height - 8) / maxf(1.0, float(image.get_height())))
    if scale < 1.0:
        scaled = image.duplicate()
        scaled.resize(max(1, int(round(image.get_width() * scale))), max(1, int(round(image.get_height() * scale))), Image.INTERPOLATE_NEAREST)
    var out := Image.create(width, height, false, Image.FORMAT_RGBA8)
    out.fill(Color(0, 0, 0, 0))
    var dx := int((width - scaled.get_width()) / 2)
    var dy := int((height - scaled.get_height()) / 2)
    blit(out, scaled, dx, dy)
    return out

func blit(dst: Image, src: Image, ox: int, oy: int) -> void:
    for y in range(src.get_height()):
        for x in range(src.get_width()):
            var c := src.get_pixel(x, y)
            if c.a > 0.0:
                dst.set_pixel(ox + x, oy + y, c)
