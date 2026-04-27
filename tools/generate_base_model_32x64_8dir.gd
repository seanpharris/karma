extends SceneTree

const OUTPUT := "res://assets/art/sprites/player_v2/layers/base_model_32x64_8dir.png"
const FRAME_W := 32
const FRAME_H := 64
const DIRECTIONS := 8

const OUTLINE := Color(0.09, 0.08, 0.075, 1.0)
const MID := Color(0.52, 0.49, 0.45, 1.0)
const LIGHT := Color(0.68, 0.65, 0.59, 1.0)
const SHADE := Color(0.34, 0.32, 0.30, 1.0)

# Karma runtime direction order:
# front/down, front-right, right, back-right, back, back-left, left, front-left.
func _init() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path("res://assets/art/sprites/player_v2/layers"))
	var sheet := Image.create(FRAME_W * DIRECTIONS, FRAME_H, false, Image.FORMAT_RGBA8)
	sheet.fill(Color(0, 0, 0, 0))
	for direction in DIRECTIONS:
		_draw_direction(sheet, direction, Vector2i(direction * FRAME_W, 0))
	sheet.save_png(OUTPUT)
	print("Generated 32x64 8-direction base model layer: ", OUTPUT)
	quit(0)

func _draw_direction(image: Image, direction: int, offset: Vector2i) -> void:
	var mirror := direction in [5, 6, 7]
	var canonical := direction
	if direction == 5:
		canonical = 3
	elif direction == 6:
		canonical = 2
	elif direction == 7:
		canonical = 1

	match canonical:
		0:
			_draw_front(image, offset)
		1:
			_draw_diagonal_front(image, offset, false)
		2:
			_draw_side(image, offset, false)
		3:
			_draw_diagonal_back(image, offset, false)
		4:
			_draw_back(image, offset)
	if mirror:
		_flip_frame(image, offset)

func _draw_front(image: Image, o: Vector2i) -> void:
	_draw_capsule(image, o + Vector2i(11, 6), Vector2i(10, 12), OUTLINE)
	_draw_capsule(image, o + Vector2i(12, 7), Vector2i(8, 10), LIGHT)
	_draw_rect(image, o + Vector2i(10, 19), Vector2i(12, 22), OUTLINE)
	_draw_rect(image, o + Vector2i(11, 20), Vector2i(10, 20), MID)
	_draw_rect(image, o + Vector2i(7, 21), Vector2i(4, 22), OUTLINE)
	_draw_rect(image, o + Vector2i(21, 21), Vector2i(4, 22), OUTLINE)
	_draw_rect(image, o + Vector2i(8, 22), Vector2i(2, 19), SHADE)
	_draw_rect(image, o + Vector2i(22, 22), Vector2i(2, 19), SHADE)
	_draw_rect(image, o + Vector2i(11, 41), Vector2i(4, 17), OUTLINE)
	_draw_rect(image, o + Vector2i(17, 41), Vector2i(4, 17), OUTLINE)
	_draw_rect(image, o + Vector2i(12, 42), Vector2i(2, 14), SHADE)
	_draw_rect(image, o + Vector2i(18, 42), Vector2i(2, 14), SHADE)

func _draw_back(image: Image, o: Vector2i) -> void:
	_draw_capsule(image, o + Vector2i(11, 6), Vector2i(10, 12), OUTLINE)
	_draw_capsule(image, o + Vector2i(12, 7), Vector2i(8, 10), SHADE)
	_draw_rect(image, o + Vector2i(9, 19), Vector2i(14, 22), OUTLINE)
	_draw_rect(image, o + Vector2i(10, 20), Vector2i(12, 20), MID)
	_draw_rect(image, o + Vector2i(7, 22), Vector2i(4, 21), OUTLINE)
	_draw_rect(image, o + Vector2i(21, 22), Vector2i(4, 21), OUTLINE)
	_draw_rect(image, o + Vector2i(11, 41), Vector2i(4, 17), OUTLINE)
	_draw_rect(image, o + Vector2i(17, 41), Vector2i(4, 17), OUTLINE)

func _draw_side(image: Image, o: Vector2i, _unused: bool) -> void:
	_draw_capsule(image, o + Vector2i(12, 6), Vector2i(9, 12), OUTLINE)
	_draw_capsule(image, o + Vector2i(13, 7), Vector2i(7, 10), LIGHT)
	_draw_rect(image, o + Vector2i(12, 19), Vector2i(10, 22), OUTLINE)
	_draw_rect(image, o + Vector2i(13, 20), Vector2i(8, 20), MID)
	_draw_rect(image, o + Vector2i(10, 22), Vector2i(4, 21), OUTLINE)
	_draw_rect(image, o + Vector2i(21, 22), Vector2i(3, 19), OUTLINE)
	_draw_rect(image, o + Vector2i(13, 41), Vector2i(4, 17), OUTLINE)
	_draw_rect(image, o + Vector2i(18, 41), Vector2i(4, 17), OUTLINE)

func _draw_diagonal_front(image: Image, o: Vector2i, _unused: bool) -> void:
	_draw_capsule(image, o + Vector2i(11, 6), Vector2i(10, 12), OUTLINE)
	_draw_capsule(image, o + Vector2i(12, 7), Vector2i(8, 10), LIGHT)
	_draw_rect(image, o + Vector2i(10, 19), Vector2i(13, 22), OUTLINE)
	_draw_rect(image, o + Vector2i(11, 20), Vector2i(11, 20), MID)
	_draw_rect(image, o + Vector2i(8, 22), Vector2i(4, 21), OUTLINE)
	_draw_rect(image, o + Vector2i(22, 22), Vector2i(3, 19), OUTLINE)
	_draw_rect(image, o + Vector2i(12, 41), Vector2i(4, 17), OUTLINE)
	_draw_rect(image, o + Vector2i(18, 41), Vector2i(4, 17), OUTLINE)

func _draw_diagonal_back(image: Image, o: Vector2i, _unused: bool) -> void:
	_draw_capsule(image, o + Vector2i(11, 6), Vector2i(10, 12), OUTLINE)
	_draw_capsule(image, o + Vector2i(12, 7), Vector2i(8, 10), SHADE)
	_draw_rect(image, o + Vector2i(9, 19), Vector2i(14, 22), OUTLINE)
	_draw_rect(image, o + Vector2i(10, 20), Vector2i(12, 20), MID)
	_draw_rect(image, o + Vector2i(8, 22), Vector2i(4, 21), OUTLINE)
	_draw_rect(image, o + Vector2i(22, 22), Vector2i(3, 19), OUTLINE)
	_draw_rect(image, o + Vector2i(12, 41), Vector2i(4, 17), OUTLINE)
	_draw_rect(image, o + Vector2i(18, 41), Vector2i(4, 17), OUTLINE)

func _draw_rect(image: Image, pos: Vector2i, size: Vector2i, color: Color) -> void:
	for y in size.y:
		for x in size.x:
			image.set_pixel(pos.x + x, pos.y + y, color)

func _draw_capsule(image: Image, pos: Vector2i, size: Vector2i, color: Color) -> void:
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

func _flip_frame(image: Image, offset: Vector2i) -> void:
	for y in FRAME_H:
		for x in FRAME_W / 2:
			var left := image.get_pixel(offset.x + x, offset.y + y)
			var right_x := FRAME_W - 1 - x
			var right := image.get_pixel(offset.x + right_x, offset.y + y)
			image.set_pixel(offset.x + x, offset.y + y, right)
			image.set_pixel(offset.x + right_x, offset.y + y, left)
