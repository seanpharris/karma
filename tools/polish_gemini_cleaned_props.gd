extends SceneTree

const SOURCES := [
    {
        "name": "natural",
        "input_dir": "res://assets/art/sprites/generated/gemini_natural_props_2026_04_27",
        "output_dir": "res://assets/art/sprites/generated/gemini_natural_props_2026_04_27/polished",
        "atlas": "gemini_natural_props_polished_atlas.png",
        "dark_contact": "gemini_natural_props_polished_contact_dark.png",
        "light_contact": "gemini_natural_props_polished_contact_light.png",
    },
    {
        "name": "static",
        "input_dir": "res://assets/art/sprites/generated/gemini_static_props_2026_04_27",
        "output_dir": "res://assets/art/sprites/generated/gemini_static_props_2026_04_27/polished",
        "atlas": "gemini_static_props_polished_atlas.png",
        "dark_contact": "gemini_static_props_polished_contact_dark.png",
        "light_contact": "gemini_static_props_polished_contact_light.png",
    },
]

const CELL := 128
const GRID := 4

func _initialize() -> void:
    for source in SOURCES:
        polish_set(source)
    quit(0)

func polish_set(config: Dictionary) -> void:
    var input_dir: String = config["input_dir"]
    var output_dir: String = config["output_dir"]
    DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(output_dir))

    var manifest_path := input_dir + "/manifest.json"
    var manifest_file := FileAccess.open(manifest_path, FileAccess.READ)
    if manifest_file == null:
        push_error("Missing manifest: " + manifest_path)
        return
    var manifest: Array = JSON.parse_string(manifest_file.get_as_text())

    var atlas := Image.create(CELL * GRID, CELL * GRID, false, Image.FORMAT_RGBA8)
    atlas.fill(Color(0, 0, 0, 0))
    var dark_contact := Image.create(CELL * GRID, CELL * GRID, false, Image.FORMAT_RGBA8)
    dark_contact.fill(Color(0.10, 0.10, 0.10, 1))
    var light_contact := Image.create(CELL * GRID, CELL * GRID, false, Image.FORMAT_RGBA8)
    light_contact.fill(Color(0.82, 0.78, 0.66, 1))

    var polished_manifest := []
    for i in range(manifest.size()):
        var entry: Dictionary = manifest[i]
        var name: String = entry["name"]
        var input_path := input_dir + "/" + name + ".png"
        var img := Image.load_from_file(input_path)
        if img == null or img.is_empty():
            push_warning("Skipping missing image: " + input_path)
            continue
        var polished := polish_image(img)
        var out_path := output_dir + "/" + name + ".png"
        polished.save_png(out_path)

        var col := i % GRID
        var row := int(i / GRID)
        blit(atlas, polished, col * CELL, row * CELL)
        blit(dark_contact, polished, col * CELL, row * CELL)
        blit(light_contact, polished, col * CELL, row * CELL)
        polished_manifest.append({
            "name": name,
            "path": out_path,
            "source": input_path,
            "canvas": [CELL, CELL]
        })

    atlas.save_png(output_dir + "/" + String(config["atlas"]))
    dark_contact.save_png(output_dir + "/" + String(config["dark_contact"]))
    light_contact.save_png(output_dir + "/" + String(config["light_contact"]))
    FileAccess.open(output_dir + "/manifest.json", FileAccess.WRITE).store_string(JSON.stringify(polished_manifest, "  "))
    print("polished ", config["name"], " -> ", output_dir)

func polish_image(image: Image) -> Image:
    var out := image.duplicate()
    # Run a conservative matte/fringe cleanup first, then remove isolated speckles.
    out = remove_gray_edge_fringe(out)
    out = remove_isolated_pixels(out)
    out = snap_tiny_alpha(out)
    return out

func remove_gray_edge_fringe(image: Image) -> Image:
    var out := image.duplicate()
    for y in range(image.get_height()):
        for x in range(image.get_width()):
            var c := image.get_pixel(x, y)
            if c.a <= 0.0:
                continue
            if not touches_transparency(image, x, y):
                continue
            var max_chan: float = maxf(c.r, maxf(c.g, c.b))
            var min_chan: float = minf(c.r, minf(c.g, c.b))
            var saturation: float = max_chan - min_chan
            var brightness: float = (c.r + c.g + c.b) / 3.0
            var transparent_neighbors := count_transparent_neighbors(image, x, y)
            # Remove gray JPG matte pixels while preserving very dark intentional outlines.
            if transparent_neighbors >= 4 and saturation < 0.075 and brightness > 0.16 and brightness < 0.62:
                c.a = 0.0
                out.set_pixel(x, y, c)
            # Remove pale gray edge crumbs too.
            elif transparent_neighbors >= 5 and saturation < 0.09 and brightness >= 0.62:
                c.a = 0.0
                out.set_pixel(x, y, c)
    return out

func remove_isolated_pixels(image: Image) -> Image:
    var out := image.duplicate()
    for y in range(image.get_height()):
        for x in range(image.get_width()):
            var c := image.get_pixel(x, y)
            if c.a <= 0.0:
                continue
            var opaque_neighbors := count_opaque_neighbors(image, x, y)
            if opaque_neighbors <= 1:
                c.a = 0.0
                out.set_pixel(x, y, c)
    return out

func snap_tiny_alpha(image: Image) -> Image:
    var out := image.duplicate()
    for y in range(image.get_height()):
        for x in range(image.get_width()):
            var c := image.get_pixel(x, y)
            if c.a < 0.5:
                c.a = 0.0
            else:
                c.a = 1.0
            out.set_pixel(x, y, c)
    return out

func touches_transparency(image: Image, x: int, y: int) -> bool:
    return count_transparent_neighbors(image, x, y) > 0

func count_transparent_neighbors(image: Image, x: int, y: int) -> int:
    var count := 0
    for oy in range(-1, 2):
        for ox in range(-1, 2):
            if ox == 0 and oy == 0:
                continue
            var nx := x + ox
            var ny := y + oy
            if nx < 0 or ny < 0 or nx >= image.get_width() or ny >= image.get_height():
                count += 1
            elif image.get_pixel(nx, ny).a <= 0.0:
                count += 1
    return count

func count_opaque_neighbors(image: Image, x: int, y: int) -> int:
    var count := 0
    for oy in range(-1, 2):
        for ox in range(-1, 2):
            if ox == 0 and oy == 0:
                continue
            var nx := x + ox
            var ny := y + oy
            if nx < 0 or ny < 0 or nx >= image.get_width() or ny >= image.get_height():
                continue
            if image.get_pixel(nx, ny).a > 0.0:
                count += 1
    return count

func blit(dst: Image, src: Image, ox: int, oy: int) -> void:
    for y in range(src.get_height()):
        for x in range(src.get_width()):
            var c := src.get_pixel(x, y)
            if c.a > 0.0:
                dst.set_pixel(ox + x, oy + y, c)
