extends SceneTree

const BASE_32 := "res://assets/art/sprites/player_v2/imported/player_base_body_sheet_32x64_8dir_4row.png"
const BOOTS_32 := "res://assets/art/sprites/player_v2/layers_32x64/boots_black_32x64.png"
const OUT := "res://assets/art/sprites/player_v2/player_real_base_black_boots_32x64_8dir_4row.png"
const REVIEW := "res://assets/art/sprites/player_v2/review/player_real_base_black_boots_preview.png"
const FRAME_W := 32
const FRAME_H := 64
const COLS := 8
const ROWS := 4

func _initialize() -> void:
    var base := Image.load_from_file(BASE_32)
    var boots := Image.load_from_file(BOOTS_32)
    if base == null or boots == null:
        push_error("missing base or boots")
        quit(1)
        return
    var composite := base.duplicate()
    blit(composite, boots, 0, 0)
    composite.save_png(OUT)
    make_review(base, boots, composite, REVIEW)
    print("wrote real-base black boots composite: ", OUT)
    quit(0)

func make_review(base: Image, boots: Image, composite: Image, path: String) -> void:
    var scale := 3
    var rows_per_frame := 3
    var out := Image.create(COLS * FRAME_W * scale, ROWS * rows_per_frame * FRAME_H * scale, false, Image.FORMAT_RGBA8)
    out.fill(Color(0.12,0.12,0.12,1))
    for row in range(ROWS):
        for col in range(COLS):
            var src := Rect2i(col * FRAME_W, row * FRAME_H, FRAME_W, FRAME_H)
            var x := col * FRAME_W * scale
            var y := row * rows_per_frame * FRAME_H * scale
            draw_frame(out, base.get_region(src), x, y, scale)
            draw_frame(out, boots.get_region(src), x, y + FRAME_H * scale, scale)
            draw_frame(out, composite.get_region(src), x, y + FRAME_H * scale * 2, scale)
    out.save_png(path)

func draw_frame(out: Image, frame: Image, ox: int, oy: int, scale: int) -> void:
    for y in range(frame.get_height()):
        for x in range(frame.get_width()):
            var c := frame.get_pixel(x,y)
            var bg := Color(0.78,0.78,0.78,1) if ((x/8 + y/8) % 2 == 0) else Color(0.46,0.46,0.46,1)
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
