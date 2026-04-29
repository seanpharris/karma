extends SceneTree

const BASE := "C:/Users/pharr/code/player-sprite/player_base_body_sheet.png"
const LAYER := "C:/Users/pharr/code/player-sprite/player_boots_layer.png"
const OUT_DIR := "res://assets/art/sprites/player_v2/imported"
const REVIEW_DIR := "res://assets/art/sprites/player_v2/review/external_layers"
const OUT_LAYER_64 := OUT_DIR + "/boots_utility_layer_64px_8dir_4row.png"
const OUT_LAYER_32 := OUT_DIR + "/boots_utility_layer_32x64_8dir_4row.png"
const OUT_REVIEW := REVIEW_DIR + "/boots_utility_layer_cleaned_review_32x64.png"
const CELL := 64
const COLS := 8
const ROWS := 4

func _initialize() -> void:
    DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
    DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(REVIEW_DIR))
    var base := Image.load_from_file(BASE)
    var layer := Image.load_from_file(LAYER)
    if base == null or layer == null:
        push_error("missing base or boots layer")
        quit(1)
        return
    var cleaned := clean_layer(base, layer)
    cleaned.save_png(OUT_LAYER_64)
    save_center_crop(cleaned, OUT_LAYER_32)
    make_review(base, cleaned, OUT_REVIEW)
    print("saved cleaned boots: ", OUT_LAYER_64, " and ", OUT_LAYER_32)
    quit(0)

func clean_layer(base: Image, layer: Image) -> Image:
    var out := layer.duplicate()
    for y in range(layer.get_height()):
        for x in range(layer.get_width()):
            var c := layer.get_pixel(x, y)
            if c.a <= 0.02:
                out.set_pixel(x, y, Color(0, 0, 0, 0))
                continue
            var b := base.get_pixel(x, y)
            var max_chan: float = maxf(c.r, maxf(c.g, c.b))
            var min_chan: float = minf(c.r, minf(c.g, c.b))
            var sat := max_chan - min_chan
            var bright := (c.r + c.g + c.b) / 3.0
            # Remove medium neutral matte/contact-block pixels. Keep very dark outlines/soles.
            if sat < 0.08 and bright > 0.20 and bright < 0.62:
                out.set_pixel(x, y, Color(0, 0, 0, 0))
                continue
            # Remove pale near-white/gray crumbs.
            if sat < 0.10 and bright >= 0.62:
                out.set_pixel(x, y, Color(0, 0, 0, 0))
                continue
            # Remove likely skin/body fragments in the boot layer if they are not mostly covering base feet.
            var skin_like := c.r > 0.42 and c.g > 0.28 and c.b > 0.18 and c.r > c.b * 1.25 and sat > 0.12
            if skin_like and b.a < 0.7:
                out.set_pixel(x, y, Color(0, 0, 0, 0))
                continue
            c.a = 1.0
            out.set_pixel(x, y, c)
    out = remove_isolated_pixels(out)
    return out

func remove_isolated_pixels(image: Image) -> Image:
    var out := image.duplicate()
    for y in range(image.get_height()):
        for x in range(image.get_width()):
            var c := image.get_pixel(x, y)
            if c.a <= 0.02:
                continue
            var neighbors := 0
            for oy in range(-1, 2):
                for ox in range(-1, 2):
                    if ox == 0 and oy == 0:
                        continue
                    var nx := x + ox
                    var ny := y + oy
                    if nx < 0 or ny < 0 or nx >= image.get_width() or ny >= image.get_height():
                        continue
                    if image.get_pixel(nx, ny).a > 0.02:
                        neighbors += 1
            if neighbors <= 1:
                out.set_pixel(x, y, Color(0,0,0,0))
    return out

func save_center_crop(image: Image, path: String) -> void:
    var out := Image.create(32 * COLS, 64 * ROWS, false, Image.FORMAT_RGBA8)
    out.fill(Color(0,0,0,0))
    for row in range(ROWS):
        for col in range(COLS):
            var src := image.get_region(Rect2i(col * CELL + 16, row * CELL, 32, 64))
            blit(out, src, col * 32, row * 64)
    out.save_png(path)

func make_review(base: Image, layer: Image, path: String) -> void:
    var scale := 3
    var frame_w := 32
    var frame_h := 64
    var rows_per_frame := 3
    var out := Image.create(COLS * frame_w * scale, ROWS * rows_per_frame * frame_h * scale, false, Image.FORMAT_RGBA8)
    out.fill(Color(0.12,0.12,0.12,1))
    for row in range(ROWS):
        for col in range(COLS):
            var src := Rect2i(col * CELL + 16, row * CELL, frame_w, frame_h)
            var base_cell := base.get_region(src)
            var layer_cell := layer.get_region(src)
            var combo := base_cell.duplicate()
            blit(combo, layer_cell, 0, 0)
            draw_frame(out, base_cell, col * frame_w * scale, (row * rows_per_frame) * frame_h * scale, scale)
            draw_frame(out, layer_cell, col * frame_w * scale, (row * rows_per_frame + 1) * frame_h * scale, scale)
            draw_frame(out, combo, col * frame_w * scale, (row * rows_per_frame + 2) * frame_h * scale, scale)
    out.save_png(path)

func draw_frame(out: Image, frame: Image, ox: int, oy: int, scale: int) -> void:
    for y in range(frame.get_height()):
        for x in range(frame.get_width()):
            var c := frame.get_pixel(x,y)
            var bg := Color(0.18,0.18,0.18,1) if ((x/8 + y/8) % 2 == 0) else Color(0.08,0.08,0.08,1)
            var mixed := bg.lerp(c, c.a)
            for sy in range(scale):
                for sx in range(scale):
                    out.set_pixel(ox + x * scale + sx, oy + y * scale + sy, mixed)

func blit(dst: Image, src: Image, ox: int, oy: int) -> void:
    for y in range(src.get_height()):
        for x in range(src.get_width()):
            var s := src.get_pixel(x,y)
            if s.a <= 0.02:
                continue
            var d := dst.get_pixel(ox+x, oy+y)
            dst.set_pixel(ox+x, oy+y, d.lerp(s, s.a))
