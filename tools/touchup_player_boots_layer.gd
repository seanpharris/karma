extends SceneTree

const SRC_64 := "res://assets/art/sprites/player_v2/imported/boots_utility_layer_64px_8dir_4row.png"
const OUT_64 := "res://assets/art/sprites/player_v2/imported/boots_utility_layer_64px_8dir_4row.png"
const OUT_32 := "res://assets/art/sprites/player_v2/layers_32x64/boots_utility_32x64.png"
const OUT_BLACK_64 := "res://assets/art/sprites/player_v2/imported/boots_black_layer_64px_8dir_4row.png"
const OUT_BLACK_32 := "res://assets/art/sprites/player_v2/layers_32x64/boots_black_32x64.png"
const REVIEW_DIR := "res://assets/art/sprites/player_v2/review/external_layers"
const BASE := "C:/Users/pharr/code/player-sprite/player_base_body_sheet.png"
const CELL := 64
const COLS := 8
const ROWS := 4

func _initialize() -> void:
    DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(REVIEW_DIR))
    var boots := Image.load_from_file(SRC_64)
    var base := Image.load_from_file(BASE)
    if boots == null or base == null:
        push_error("missing boots or base image")
        quit(1)
        return

    var touched := touchup_utility_boots(boots)
    var black := make_black_boots(touched)
    touched.save_png(OUT_64)
    save_center_crop(touched, OUT_32)
    black.save_png(OUT_BLACK_64)
    save_center_crop(black, OUT_BLACK_32)
    make_review(base, touched, black, REVIEW_DIR + "/boots_touchup_and_black_review_32x64.png")
    print("wrote touched boots and black variant")
    quit(0)

func touchup_utility_boots(src: Image) -> Image:
    var out := src.duplicate()
    for y in range(src.get_height()):
        for x in range(src.get_width()):
            var c := src.get_pixel(x, y)
            if c.a <= 0.02:
                continue
            var max_chan: float = maxf(c.r, maxf(c.g, c.b))
            var min_chan: float = minf(c.r, minf(c.g, c.b))
            var sat := max_chan - min_chan
            var bright := (c.r + c.g + c.b) / 3.0

            # Tame the bright tan/orange sole pixels that flicker in walk frames.
            if c.r > 0.38 and c.g > 0.20 and c.b < 0.18 and sat > 0.12:
                c = Color(0.30, 0.18, 0.095, 1.0)
            # Nudge very bright neutral chips into the normal boot highlight range.
            elif sat < 0.10 and bright > 0.55:
                c = Color(0.34, 0.30, 0.24, 1.0)
            # Keep mid gray/brown highlights but reduce sparkle.
            elif bright > 0.42 and sat < 0.18:
                c.r *= 0.82
                c.g *= 0.82
                c.b *= 0.82
                c.a = 1.0
            else:
                c.a = 1.0
            out.set_pixel(x, y, c)
    return smooth_single_pixel_highlights(out)

func smooth_single_pixel_highlights(src: Image) -> Image:
    var out := src.duplicate()
    for y in range(src.get_height()):
        for x in range(src.get_width()):
            var c := src.get_pixel(x, y)
            if c.a <= 0.02:
                continue
            var bright := (c.r + c.g + c.b) / 3.0
            if bright <= 0.44:
                continue
            var nearby_bright := 0
            for oy in range(-1, 2):
                for ox in range(-1, 2):
                    if ox == 0 and oy == 0:
                        continue
                    var nx := x + ox
                    var ny := y + oy
                    if nx < 0 or ny < 0 or nx >= src.get_width() or ny >= src.get_height():
                        continue
                    var n := src.get_pixel(nx, ny)
                    if n.a > 0.02 and ((n.r + n.g + n.b) / 3.0) > 0.38:
                        nearby_bright += 1
            if nearby_bright <= 1:
                c.r *= 0.72
                c.g *= 0.72
                c.b *= 0.72
                out.set_pixel(x, y, c)
    return out

func make_black_boots(src: Image) -> Image:
    var out := src.duplicate()
    for y in range(src.get_height()):
        for x in range(src.get_width()):
            var c := src.get_pixel(x, y)
            if c.a <= 0.02:
                out.set_pixel(x, y, Color(0,0,0,0))
                continue
            var v := clampf((c.r + c.g + c.b) / 3.0, 0.0, 1.0)
            var mapped: Color
            if v < 0.13:
                mapped = Color(0.018, 0.020, 0.026, 1.0)
            elif v < 0.25:
                mapped = Color(0.045, 0.050, 0.062, 1.0)
            elif v < 0.40:
                mapped = Color(0.075, 0.083, 0.100, 1.0)
            else:
                mapped = Color(0.135, 0.145, 0.165, 1.0)
            out.set_pixel(x, y, mapped)
    return out

func save_center_crop(image: Image, path: String) -> void:
    var out := Image.create(32 * COLS, 64 * ROWS, false, Image.FORMAT_RGBA8)
    out.fill(Color(0,0,0,0))
    for row in range(ROWS):
        for col in range(COLS):
            var src := image.get_region(Rect2i(col * CELL + 16, row * CELL, 32, 64))
            blit(out, src, col * 32, row * 64)
    out.save_png(path)

func make_review(base: Image, utility: Image, black: Image, path: String) -> void:
    var scale := 3
    var frame_w := 32
    var frame_h := 64
    var rows_per_frame := 5
    var out := Image.create(COLS * frame_w * scale, ROWS * rows_per_frame * frame_h * scale, false, Image.FORMAT_RGBA8)
    out.fill(Color(0.12,0.12,0.12,1))
    for row in range(ROWS):
        for col in range(COLS):
            var src := Rect2i(col * CELL + 16, row * CELL, frame_w, frame_h)
            var base_cell := base.get_region(src)
            var utility_cell := utility.get_region(src)
            var black_cell := black.get_region(src)
            var utility_combo := base_cell.duplicate()
            blit(utility_combo, utility_cell, 0, 0)
            var black_combo := base_cell.duplicate()
            blit(black_combo, black_cell, 0, 0)
            var x := col * frame_w * scale
            var y := row * rows_per_frame * frame_h * scale
            draw_frame(out, base_cell, x, y, scale)
            draw_frame(out, utility_cell, x, y + frame_h * scale, scale)
            draw_frame(out, utility_combo, x, y + frame_h * scale * 2, scale)
            draw_frame(out, black_cell, x, y + frame_h * scale * 3, scale)
            draw_frame(out, black_combo, x, y + frame_h * scale * 4, scale)
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
