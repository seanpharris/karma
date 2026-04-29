extends SceneTree

const BASE := "C:/Users/pharr/code/player-sprite/player_base_body_sheet.png"
const LAYER := "C:/Users/pharr/code/player-sprite/player_boots_layer.png"
const OUT_DIR := "res://assets/art/sprites/player_v2/review/external_layers"
const CELL := 64
const COLS := 8
const ROWS := 4

func _initialize() -> void:
    DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
    var base := Image.load_from_file(BASE)
    var layer := Image.load_from_file(LAYER)
    if base == null or layer == null:
        push_error("missing base or layer")
        quit(1)
        return
    var boots_alpha := count_alpha(layer)
    var base_alpha := count_alpha(base)
    var overlap := count_overlap(base, layer)
    print("base=", base.get_size(), " layer=", layer.get_size(), " base_alpha=", base_alpha, " layer_alpha=", boots_alpha, " overlap=", overlap)
    make_sheet(base, layer, OUT_DIR + "/player_boots_layer_review_64px.png", 2)
    make_sheet_32(base, layer, OUT_DIR + "/player_boots_layer_review_32x64.png", 3)
    layer.save_png(OUT_DIR + "/player_boots_layer_source_copy.png")
    quit(0)

func count_alpha(img: Image) -> int:
    var n := 0
    for y in range(img.get_height()):
        for x in range(img.get_width()):
            if img.get_pixel(x, y).a > 0.02:
                n += 1
    return n

func count_overlap(a: Image, b: Image) -> int:
    var n := 0
    var w = min(a.get_width(), b.get_width())
    var h = min(a.get_height(), b.get_height())
    for y in range(h):
        for x in range(w):
            if a.get_pixel(x, y).a > 0.02 and b.get_pixel(x, y).a > 0.02:
                n += 1
    return n

func make_sheet(base: Image, layer: Image, path: String, scale: int) -> void:
    var rows_per_frame := 3
    var out := Image.create(COLS * CELL * scale, ROWS * rows_per_frame * CELL * scale, false, Image.FORMAT_RGBA8)
    out.fill(Color(0.12, 0.12, 0.12, 1))
    for row in range(ROWS):
        for col in range(COLS):
            var src := Rect2i(col * CELL, row * CELL, CELL, CELL)
            var base_cell := base.get_region(src)
            var layer_cell := layer.get_region(src)
            var combo := base_cell.duplicate()
            blit(combo, layer_cell, 0, 0)
            draw_cell(out, base_cell, col, row * rows_per_frame, scale)
            draw_cell(out, layer_cell, col, row * rows_per_frame + 1, scale)
            draw_cell(out, combo, col, row * rows_per_frame + 2, scale)
    out.save_png(path)

func make_sheet_32(base: Image, layer: Image, path: String, scale: int) -> void:
    var frame_w := 32
    var frame_h := 64
    var rows_per_frame := 3
    var out := Image.create(COLS * frame_w * scale, ROWS * rows_per_frame * frame_h * scale, false, Image.FORMAT_RGBA8)
    out.fill(Color(0.12, 0.12, 0.12, 1))
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

func draw_cell(out: Image, cell: Image, col: int, row: int, scale: int) -> void:
    draw_frame(out, cell, col * CELL * scale, row * CELL * scale, scale)

func draw_frame(out: Image, frame: Image, ox: int, oy: int, scale: int) -> void:
    for y in range(frame.get_height()):
        for x in range(frame.get_width()):
            var c := frame.get_pixel(x, y)
            var bg := Color(0.18, 0.18, 0.18, 1) if ((x / 8 + y / 8) % 2 == 0) else Color(0.08, 0.08, 0.08, 1)
            var mixed := bg.lerp(c, c.a)
            for sy in range(scale):
                for sx in range(scale):
                    out.set_pixel(ox + x * scale + sx, oy + y * scale + sy, mixed)

func blit(dst: Image, src: Image, ox: int, oy: int) -> void:
    for y in range(src.get_height()):
        for x in range(src.get_width()):
            var s := src.get_pixel(x, y)
            if s.a <= 0:
                continue
            var d := dst.get_pixel(ox + x, oy + y)
            dst.set_pixel(ox + x, oy + y, d.lerp(s, s.a))
