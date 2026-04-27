extends SceneTree

const OUTPUT := "res://assets/art/sprites/player_v2/player_model_32x64_8dir.png"
const FRAME_W := 32
const FRAME_H := 64
const DIRECTIONS := 8

const O := Color(0.045, 0.038, 0.032, 1.0)
const SKIN := Color(0.70, 0.48, 0.33, 1.0)
const SKIN_D := Color(0.43, 0.28, 0.20, 1.0)
const HAIR := Color(0.10, 0.065, 0.045, 1.0)
const VISOR := Color(0.22, 0.78, 0.95, 1.0)
const SUIT := Color(0.88, 0.38, 0.12, 1.0)
const SUIT_D := Color(0.48, 0.17, 0.075, 1.0)
const PLATE := Color(0.30, 0.36, 0.42, 1.0)
const PLATE_L := Color(0.54, 0.61, 0.66, 1.0)
const PACK := Color(0.17, 0.22, 0.25, 1.0)
const BOOT := Color(0.08, 0.08, 0.09, 1.0)
const SOLE := Color(0.16, 0.15, 0.14, 1.0)

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

func _draw_direction(img: Image, dir: int, o: Vector2i) -> void:
	match dir:
		0: _front(img, o)
		1: _front_diag(img, o, false)
		2: _side(img, o, false)
		3: _back_diag(img, o, false)
		4: _back(img, o)
		5: _back_diag(img, o, true)
		6: _side(img, o, true)
		7: _front_diag(img, o, true)

func _front(img: Image, o: Vector2i) -> void:
	_legs(img, o, 11, 17, false)
	_torso(img, o, 10, 21, 12, false)
	_arm(img, o, 6, 23, 4, 23, false)
	_arm(img, o, 22, 23, 4, 23, false)
	_head_front(img, o, 11, 6, false)
	_chest_plate(img, o, 12, 24, 8, 11)
	_pixel(img, o + Vector2i(15, 19), PLATE_L)

func _back(img: Image, o: Vector2i) -> void:
	_legs(img, o, 11, 17, true)
	_torso(img, o, 9, 21, 14, true)
	_arm(img, o, 6, 24, 4, 22, true)
	_arm(img, o, 22, 24, 4, 22, true)
	_head_back(img, o, 11, 6)
	_pack(img, o, 10, 22, 12, 17)

func _side(img: Image, o: Vector2i, flip: bool) -> void:
	_legs_side(img, o, flip)
	_mrect(img, o, 12, 21, 10, 23, O, flip)
	_mrect(img, o, 13, 22, 8, 21, SUIT, flip)
	_mrect(img, o, 15, 24, 6, 12, PLATE_L, flip)
	_mrect(img, o, 9, 24, 4, 21, O, flip)
	_mrect(img, o, 10, 25, 2, 19, SUIT_D, flip)
	_mrect(img, o, 22, 25, 3, 17, O, flip)
	_mrect(img, o, 22, 26, 2, 15, SUIT_D, flip)
	_mrect(img, o, 10, 22, 6, 16, PACK, flip)
	_head_side(img, o, 12, 6, flip)

func _front_diag(img: Image, o: Vector2i, flip: bool) -> void:
	_legs_diag(img, o, flip, false)
	_mrect(img, o, 10, 21, 13, 23, O, flip)
	_mrect(img, o, 11, 22, 11, 21, SUIT, flip)
	_mrect(img, o, 14, 24, 7, 11, PLATE_L, flip)
	_mrect(img, o, 7, 24, 4, 21, O, flip)
	_mrect(img, o, 8, 25, 2, 18, SUIT_D, flip)
	_mrect(img, o, 23, 25, 3, 17, O, flip)
	_mrect(img, o, 23, 26, 1, 15, SUIT_D, flip)
	_head_front_diag(img, o, 11, 6, flip)

func _back_diag(img: Image, o: Vector2i, flip: bool) -> void:
	_legs_diag(img, o, flip, true)
	_mrect(img, o, 9, 21, 14, 23, O, flip)
	_mrect(img, o, 10, 22, 12, 21, SUIT_D, flip)
	_mrect(img, o, 10, 23, 11, 16, PACK, flip)
	_mrect(img, o, 7, 24, 4, 21, O, flip)
	_mrect(img, o, 8, 25, 2, 18, SUIT_D, flip)
	_mrect(img, o, 23, 25, 3, 17, O, flip)
	_mrect(img, o, 23, 26, 1, 15, SUIT_D, flip)
	_head_back_diag(img, o, 11, 6, flip)

func _head_front(img: Image, o: Vector2i, x: int, y: int, _flip: bool) -> void:
	_ellipse(img, o + Vector2i(x, y), Vector2i(10, 13), O)
	_ellipse(img, o + Vector2i(x + 1, y + 1), Vector2i(8, 11), SKIN)
	_rect(img, o + Vector2i(x + 2, y + 5), Vector2i(6, 3), VISOR)
	_rect(img, o + Vector2i(x + 2, y + 1), Vector2i(6, 2), HAIR)

func _head_front_diag(img: Image, o: Vector2i, x: int, y: int, flip: bool) -> void:
	_mellipse(img, o, x, y, 10, 13, O, flip)
	_mellipse(img, o, x + 1, y + 1, 8, 11, SKIN, flip)
	_mrect(img, o, x + 4, y + 5, 5, 3, VISOR, flip)
	_mrect(img, o, x + 2, y + 1, 6, 2, HAIR, flip)

func _head_side(img: Image, o: Vector2i, x: int, y: int, flip: bool) -> void:
	_mellipse(img, o, x, y, 9, 13, O, flip)
	_mellipse(img, o, x + 1, y + 1, 7, 11, SKIN, flip)
	_mrect(img, o, x + 5, y + 5, 4, 3, VISOR, flip)
	_mrect(img, o, x + 2, y + 1, 5, 2, HAIR, flip)

func _head_back(img: Image, o: Vector2i, x: int, y: int) -> void:
	_ellipse(img, o + Vector2i(x, y), Vector2i(10, 13), O)
	_ellipse(img, o + Vector2i(x + 1, y + 1), Vector2i(8, 11), SKIN_D)
	_rect(img, o + Vector2i(x + 2, y + 1), Vector2i(6, 5), HAIR)

func _head_back_diag(img: Image, o: Vector2i, x: int, y: int, flip: bool) -> void:
	_mellipse(img, o, x, y, 10, 13, O, flip)
	_mellipse(img, o, x + 1, y + 1, 8, 11, SKIN_D, flip)
	_mrect(img, o, x + 2, y + 1, 6, 5, HAIR, flip)

func _torso(img: Image, o: Vector2i, x: int, y: int, w: int, back: bool) -> void:
	_rect(img, o + Vector2i(x, y), Vector2i(w, 23), O)
	_rect(img, o + Vector2i(x + 1, y + 1), Vector2i(w - 2, 21), SUIT_D if back else SUIT)

func _chest_plate(img: Image, o: Vector2i, x: int, y: int, w: int, h: int) -> void:
	_rect(img, o + Vector2i(x, y), Vector2i(w, h), PLATE)
	_rect(img, o + Vector2i(x + 1, y + 1), Vector2i(w - 2, 2), PLATE_L)

func _pack(img: Image, o: Vector2i, x: int, y: int, w: int, h: int) -> void:
	_rect(img, o + Vector2i(x, y), Vector2i(w, h), O)
	_rect(img, o + Vector2i(x + 1, y + 1), Vector2i(w - 2, h - 2), PACK)
	_rect(img, o + Vector2i(x + 3, y + 3), Vector2i(w - 6, 2), PLATE)

func _arm(img: Image, o: Vector2i, x: int, y: int, w: int, h: int, back: bool) -> void:
	_rect(img, o + Vector2i(x, y), Vector2i(w, h), O)
	_rect(img, o + Vector2i(x + 1, y + 1), Vector2i(max(1, w - 2), h - 2), SUIT_D if back else SUIT)
	_rect(img, o + Vector2i(x + 1, y + h - 3), Vector2i(max(1, w - 2), 3), SKIN_D)

func _legs(img: Image, o: Vector2i, lx: int, rx: int, back: bool) -> void:
	_leg(img, o, lx, 43, 4, 15, back)
	_leg(img, o, rx, 43, 4, 15, back)
	_boot(img, o, lx - 1, 57)
	_boot(img, o, rx, 57)

func _legs_side(img: Image, o: Vector2i, flip: bool) -> void:
	_mleg(img, o, 13, 43, 4, 15, false, flip)
	_mleg(img, o, 19, 43, 4, 15, true, flip)
	_mboot(img, o, 12, 57, flip)
	_mboot(img, o, 18, 57, flip)

func _legs_diag(img: Image, o: Vector2i, flip: bool, back: bool) -> void:
	_mleg(img, o, 12, 43, 4, 15, back, flip)
	_mleg(img, o, 18, 43, 4, 15, back, flip)
	_mboot(img, o, 11, 57, flip)
	_mboot(img, o, 18, 57, flip)

func _leg(img: Image, o: Vector2i, x: int, y: int, w: int, h: int, back: bool) -> void:
	_rect(img, o + Vector2i(x, y), Vector2i(w, h), O)
	_rect(img, o + Vector2i(x + 1, y + 1), Vector2i(w - 2, h - 2), SUIT_D if back else SUIT)

func _mleg(img: Image, o: Vector2i, x: int, y: int, w: int, h: int, back: bool, flip: bool) -> void:
	var mx := FRAME_W - x - w if flip else x
	_leg(img, o, mx, y, w, h, back)

func _boot(img: Image, o: Vector2i, x: int, y: int) -> void:
	_rect(img, o + Vector2i(x, y), Vector2i(6, 3), O)
	_rect(img, o + Vector2i(x + 1, y), Vector2i(4, 2), BOOT)
	_pixel(img, o + Vector2i(x + 5, y + 1), SOLE)

func _mboot(img: Image, o: Vector2i, x: int, y: int, flip: bool) -> void:
	_boot(img, o, FRAME_W - x - 6 if flip else x, y)

func _mrect(img: Image, o: Vector2i, x: int, y: int, w: int, h: int, c: Color, flip: bool) -> void:
	_rect(img, o + Vector2i(FRAME_W - x - w if flip else x, y), Vector2i(w, h), c)

func _mellipse(img: Image, o: Vector2i, x: int, y: int, w: int, h: int, c: Color, flip: bool) -> void:
	_ellipse(img, o + Vector2i(FRAME_W - x - w if flip else x, y), Vector2i(w, h), c)

func _rect(img: Image, pos: Vector2i, size: Vector2i, color: Color) -> void:
	for y in size.y:
		for x in size.x:
			_pixel(img, pos + Vector2i(x, y), color)

func _ellipse(img: Image, pos: Vector2i, size: Vector2i, color: Color) -> void:
	var center := Vector2(pos.x + (size.x - 1) / 2.0, pos.y + (size.y - 1) / 2.0)
	var rx := size.x / 2.0
	var ry := size.y / 2.0
	for y in size.y:
		for x in size.x:
			var p := Vector2(pos.x + x, pos.y + y)
			var dx := (p.x - center.x) / rx
			var dy := (p.y - center.y) / ry
			if dx * dx + dy * dy <= 1.0:
				_pixel(img, Vector2i(pos.x + x, pos.y + y), color)

func _pixel(img: Image, p: Vector2i, color: Color) -> void:
	if p.x >= 0 and p.y >= 0 and p.x < FRAME_W * DIRECTIONS and p.y < FRAME_H:
		img.set_pixel(p.x, p.y, color)
