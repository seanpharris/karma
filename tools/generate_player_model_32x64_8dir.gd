extends SceneTree

const OUTPUT := "res://assets/art/sprites/player_v2/player_model_32x64_8dir.png"
const FRAME_W := 32
const FRAME_H := 64
const DIRECTIONS := 8

const OUTLINE := Color(0.055, 0.045, 0.04, 1.0)
const SKIN := Color(0.67, 0.46, 0.32, 1.0)
const SKIN_SHADE := Color(0.42, 0.27, 0.20, 1.0)
const VISOR := Color(0.30, 0.80, 0.95, 1.0)
const HAIR := Color(0.12, 0.08, 0.06, 1.0)
const SUIT := Color(0.86, 0.38, 0.13, 1.0)
const SUIT_DARK := Color(0.46, 0.18, 0.09, 1.0)
const ARMOR := Color(0.30, 0.36, 0.42, 1.0)
const ARMOR_LIT := Color(0.48, 0.56, 0.62, 1.0)
const BOOT := Color(0.10, 0.10, 0.11, 1.0)
const PACK := Color(0.20, 0.25, 0.28, 1.0)

# Runtime direction order: front/down, front-right, right, back-right,
# back/up, back-left, left, front-left.
func _init() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path("res://assets/art/sprites/player_v2"))
	var sheet := Image.create(FRAME_W * DIRECTIONS, FRAME_H, false, Image.FORMAT_RGBA8)
	sheet.fill(Color(0, 0, 0, 0))
	for direction in DIRECTIONS:
		_draw_direction(sheet, direction, Vector2i(direction * FRAME_W, 0))
	sheet.save_png(OUTPUT)
	print("Generated 32x64 8-direction player model: ", OUTPUT)
	quit(0)

func _draw_direction(image: Image, direction: int, o: Vector2i) -> void:
	match direction:
		0:
			_draw_front(image, o)
		1:
			_draw_front_diag(image, o, false)
		2:
			_draw_side(image, o, false)
		3:
			_draw_back_diag(image, o, false)
		4:
			_draw_back(image, o)
		5:
			_draw_back_diag(image, o, true)
		6:
			_draw_side(image, o, true)
		7:
			_draw_front_diag(image, o, true)

func _draw_front(image: Image, o: Vector2i) -> void:
	_draw_head(image, o, 0, true, false)
	_draw_body(image, o, 0, false)
	_draw_arm(image, o, Vector2i(7, 22), Vector2i(4, 22), SUIT_DARK)
	_draw_arm(image, o, Vector2i(21, 22), Vector2i(4, 22), SUIT_DARK)
	_draw_leg(image, o, 11, 42, 4, 16)
	_draw_leg(image, o, 17, 42, 4, 16)
	_draw_boot(image, o, 10, 57)
	_draw_boot(image, o, 17, 57)

func _draw_back(image: Image, o: Vector2i) -> void:
	_draw_head(image, o, 0, false, true)
	_draw_pack(image, o, 10, 20, 12, 18)
	_draw_body(image, o, 0, true)
	_draw_arm(image, o, Vector2i(7, 23), Vector2i(4, 21), SUIT_DARK)
	_draw_arm(image, o, Vector2i(21, 23), Vector2i(4, 21), SUIT_DARK)
	_draw_leg(image, o, 11, 42, 4, 16)
	_draw_leg(image, o, 17, 42, 4, 16)
	_draw_boot(image, o, 10, 57)
	_draw_boot(image, o, 17, 57)

func _draw_side(image: Image, o: Vector2i, flip: bool) -> void:
	var sx := -1 if flip else 1
	_draw_head_side(image, o, sx)
	_draw_pack(image, o, 10 if flip else 15, 21, 7, 17)
	_draw_rect_m(image, o, 12, 19, 10, 23, OUTLINE, flip)
	_draw_rect_m(image, o, 13, 20, 8, 21, SUIT, flip)
	_draw_rect_m(image, o, 13, 22, 3, 10, ARMOR_LIT, flip)
	_draw_arm(image, o, Vector2i(10 if not flip else 20, 23), Vector2i(4, 21), SUIT_DARK)
	_draw_arm(image, o, Vector2i(21 if not flip else 8, 24), Vector2i(3, 18), SUIT_DARK)
	_draw_leg(image, o, 13 if not flip else 16, 42, 4, 16)
	_draw_leg(image, o, 18 if not flip else 11, 42, 4, 16)
	_draw_boot(image, o, 13 if not flip else 15, 57)
	_draw_boot(image, o, 18 if not flip else 10, 57)

func _draw_front_diag(image: Image, o: Vector2i, flip: bool) -> void:
	_draw_head(image, o, -1 if flip else 1, true, false)
	_draw_body(image, o, -1 if flip else 1, false)
	_draw_rect_m(image, o, 16, 22, 5, 14, ARMOR_LIT, flip)
	_draw_arm(image, o, Vector2i(8 if not flip else 20, 23), Vector2i(4, 20), SUIT_DARK)
	_draw_arm(image, o, Vector2i(22 if not flip else 8, 24), Vector2i(3, 18), SUIT_DARK)
	_draw_leg(image, o, 12 if not flip else 16, 42, 4, 16)
	_draw_leg(image, o, 18 if not flip else 11, 42, 4, 16)
	_draw_boot(image, o, 11 if not flip else 16, 57)
	_draw_boot(image, o, 18 if not flip else 10, 57)

func _draw_back_diag(image: Image, o: Vector2i, flip: bool) -> void:
	_draw_head(image, o, -1 if flip else 1, false, true)
	_draw_pack(image, o, 10 if not flip else 12, 20, 12, 18)
	_draw_body(image, o, -1 if flip else 1, true)
	_draw_rect_m(image, o, 10, 22, 5, 13, ARMOR, flip)
	_draw_arm(image, o, Vector2i(8 if not flip else 20, 23), Vector2i(4, 20), SUIT_DARK)
	_draw_arm(image, o, Vector2i(22 if not flip else 8, 24), Vector2i(3, 18), SUIT_DARK)
	_draw_leg(image, o, 12 if not flip else 16, 42, 4, 16)
	_draw_leg(image, o, 18 if not flip else 11, 42, 4, 16)
	_draw_boot(image, o, 11 if not flip else 16, 57)
	_draw_boot(image, o, 18 if not flip else 10, 57)

func _draw_head(image: Image, o: Vector2i, turn: int, front: bool, back: bool) -> void:
	var x := 11 + turn
	_draw_ellipse(image, o + Vector2i(x, 6), Vector2i(10, 12), OUTLINE)
	_draw_ellipse(image, o + Vector2i(x + 1, 7), Vector2i(8, 10), SKIN_SHADE if back else SKIN)
	if front:
		_draw_rect(image, o + Vector2i(x + 2, 10), Vector2i(6, 3), VISOR)
	else:
		_draw_rect(image, o + Vector2i(x + 2, 7), Vector2i(6, 3), HAIR)

func _draw_head_side(image: Image, o: Vector2i, sx: int) -> void:
	var x := 11 if sx > 0 else 12
	_draw_ellipse(image, o + Vector2i(x, 6), Vector2i(10, 12), OUTLINE)
	_draw_ellipse(image, o + Vector2i(x + 1, 7), Vector2i(8, 10), SKIN)
	if sx > 0:
		_draw_rect(image, o + Vector2i(x + 5, 10), Vector2i(4, 3), VISOR)
	else:
		_draw_rect(image, o + Vector2i(x + 1, 10), Vector2i(4, 3), VISOR)

func _draw_body(image: Image, o: Vector2i, turn: int, back: bool) -> void:
	var x := 10 + turn
	_draw_rect(image, o + Vector2i(x, 19), Vector2i(12, 23), OUTLINE)
	_draw_rect(image, o + Vector2i(x + 1, 20), Vector2i(10, 21), SUIT_DARK if back else SUIT)
	_draw_rect(image, o + Vector2i(x + 3, 22), Vector2i(6, 12), ARMOR)

func _draw_pack(image: Image, o: Vector2i, x: int, y: int, w: int, h: int) -> void:
	_draw_rect(image, o + Vector2i(x, y), Vector2i(w, h), OUTLINE)
	_draw_rect(image, o + Vector2i(x + 1, y + 1), Vector2i(max(1, w - 2), max(1, h - 2)), PACK)

func _draw_arm(image: Image, o: Vector2i, p: Vector2i, s: Vector2i, color: Color) -> void:
	_draw_rect(image, o + p, s, OUTLINE)
	_draw_rect(image, o + p + Vector2i(1, 1), Vector2i(max(1, s.x - 2), max(1, s.y - 2)), color)

func _draw_leg(image: Image, o: Vector2i, x: int, y: int, w: int, h: int) -> void:
	_draw_rect(image, o + Vector2i(x, y), Vector2i(w, h), OUTLINE)
	_draw_rect(image, o + Vector2i(x + 1, y + 1), Vector2i(max(1, w - 2), max(1, h - 2)), SUIT_DARK)

func _draw_boot(image: Image, o: Vector2i, x: int, y: int) -> void:
	_draw_rect(image, o + Vector2i(x, y), Vector2i(5, 3), BOOT)

func _draw_rect_m(image: Image, o: Vector2i, x: int, y: int, w: int, h: int, color: Color, flip: bool) -> void:
	_draw_rect(image, o + Vector2i((FRAME_W - x - w) if flip else x, y), Vector2i(w, h), color)

func _draw_rect(image: Image, pos: Vector2i, size: Vector2i, color: Color) -> void:
	for y in size.y:
		for x in size.x:
			var px := pos.x + x
			var py := pos.y + y
			if px >= 0 and py >= 0 and px < FRAME_W * DIRECTIONS and py < FRAME_H:
				image.set_pixel(px, py, color)

func _draw_ellipse(image: Image, pos: Vector2i, size: Vector2i, color: Color) -> void:
	var center := Vector2(pos.x + (size.x - 1) / 2.0, pos.y + (size.y - 1) / 2.0)
	var rx := size.x / 2.0
	var ry := size.y / 2.0
	for y in size.y:
		for x in size.x:
			var p := Vector2(pos.x + x, pos.y + y)
			var dx := (p.x - center.x) / rx
			var dy := (p.y - center.y) / ry
			if dx * dx + dy * dy <= 1.0:
				image.set_pixel(pos.x + x, pos.y + y, color)
