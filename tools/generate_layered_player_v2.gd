extends SceneTree

const OUTPUT_ROOT := "res://assets/art/sprites/player_v2"
const LAYER_DIR := OUTPUT_ROOT + "/layers"
const COMPOSITE_PATH := OUTPUT_ROOT + "/player_v2_layered_preview_8dir.png"
const MANIFEST_PATH := OUTPUT_ROOT + "/player_v2_manifest.json"
const FRAME_SIZE := 32
const COLUMNS := 8
const ROWS := 9

enum Direction { FRONT, FRONT_RIGHT, RIGHT, BACK_RIGHT, BACK, BACK_LEFT, LEFT, FRONT_LEFT }

const SKIN_LIGHT := Color8(216, 166, 122, 255)
const SKIN_LIGHT_SHADE := Color8(145, 99, 70, 255)
const SKIN_MEDIUM := Color8(186, 129, 91, 255)
const SKIN_MEDIUM_SHADE := Color8(118, 76, 58, 255)
const SKIN_DEEP := Color8(103, 62, 45, 255)
const SKIN_DEEP_SHADE := Color8(61, 38, 31, 255)
const BODY_GUIDE := Color8(82, 124, 150, 255)
const BODY_GUIDE_SHADE := Color8(39, 62, 83, 255)
const HAIR := Color8(38, 28, 24, 255)
const SUIT := Color8(35, 62, 78, 255)
const SUIT_SHADE := Color8(18, 35, 48, 255)
const SUIT_LIGHT := Color8(74, 132, 155, 255)
const ACCENT := Color8(230, 185, 68, 255)
const TOOL := Color8(118, 128, 132, 255)
const OUTLINE := Color8(18, 20, 23, 255)

func _init() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(LAYER_DIR))
	var base := _new_sheet()
	var skin_light := _new_sheet()
	var skin_medium := _new_sheet()
	var skin_deep := _new_sheet()
	var hair := _new_sheet()
	var outfit := _new_sheet()
	var tool := _new_sheet()
	var composite := _new_sheet()
	for row in ROWS:
		for col in COLUMNS:
			_draw_base_frame(base, col, row)
			_draw_skin_frame(skin_light, col, row, SKIN_LIGHT, SKIN_LIGHT_SHADE)
			_draw_skin_frame(skin_medium, col, row, SKIN_MEDIUM, SKIN_MEDIUM_SHADE)
			_draw_skin_frame(skin_deep, col, row, SKIN_DEEP, SKIN_DEEP_SHADE)
			_draw_hair_frame(hair, col, row)
			_draw_outfit_frame(outfit, col, row)
			_draw_tool_frame(tool, col, row)
	_save(base, LAYER_DIR + "/base_body_8dir.png")
	_save(skin_light, LAYER_DIR + "/skin_light_8dir.png")
	_save(skin_medium, LAYER_DIR + "/skin_medium_8dir.png")
	_save(skin_deep, LAYER_DIR + "/skin_deep_8dir.png")
	_save(hair, LAYER_DIR + "/hair_short_dark_8dir.png")
	_save(outfit, LAYER_DIR + "/outfit_engineer_8dir.png")
	_save(tool, LAYER_DIR + "/tool_multitool_8dir.png")
	_write_manifest()
	_composite_from_manifest(composite, [
		"base_body",
		"skin_medium",
		"hair_short_dark",
		"outfit_engineer",
		"tool_multitool"
	])
	_save(composite, COMPOSITE_PATH)
	print("Generated layered player v2 preview at ", COMPOSITE_PATH)
	quit(0)

func _new_sheet() -> Image:
	var image := Image.create(FRAME_SIZE * COLUMNS, FRAME_SIZE * ROWS, false, Image.FORMAT_RGBA8)
	image.fill(Color(0, 0, 0, 0))
	return image

func _save(image: Image, path: String) -> void:
	var err := image.save_png(path)
	if err != OK:
		printerr("Could not save ", path, ": ", err)
		quit(1)

func _composite_from_manifest(target: Image, layer_ids: Array[String]) -> void:
	for layer_id in layer_ids:
		var path := LAYER_DIR + "/" + layer_id + "_8dir.png"
		var layer := Image.load_from_file(path)
		if layer == null:
			printerr("Missing player v2 layer: ", path)
			quit(1)
		target.blend_rect(layer, Rect2i(Vector2i.ZERO, layer.get_size()), Vector2i.ZERO)

func _write_manifest() -> void:
	var manifest := {
		"schema": "karma.player_v2.layers.v1",
		"frameSize": FRAME_SIZE,
		"columns": COLUMNS,
		"rows": ROWS,
		"directions": ["front", "front_right", "right", "back_right", "back", "back_left", "left", "front_left"],
		"animations": ["idle", "walk1", "walk2", "walk3", "walk4", "run", "tool_use", "melee", "interact"],
		"layerOrder": ["base", "skin", "hair", "outfit", "held_tool"],
		"layers": [
			{ "id": "base_body", "slot": "base", "path": "layers/base_body_8dir.png", "required": true },
			{ "id": "skin_light", "slot": "skin", "path": "layers/skin_light_8dir.png" },
			{ "id": "skin_medium", "slot": "skin", "path": "layers/skin_medium_8dir.png", "default": true },
			{ "id": "skin_deep", "slot": "skin", "path": "layers/skin_deep_8dir.png" },
			{ "id": "hair_short_dark", "slot": "hair", "path": "layers/hair_short_dark_8dir.png", "default": true },
			{ "id": "outfit_engineer", "slot": "outfit", "path": "layers/outfit_engineer_8dir.png", "default": true },
			{ "id": "tool_multitool", "slot": "held_tool", "path": "layers/tool_multitool_8dir.png", "default": true }
		],
		"previewStack": ["base_body", "skin_medium", "hair_short_dark", "outfit_engineer", "tool_multitool"],
		"composite": "player_v2_layered_preview_8dir.png"
	}
	var file := FileAccess.open(MANIFEST_PATH, FileAccess.WRITE)
	if file == null:
		printerr("Could not write ", MANIFEST_PATH)
		quit(1)
	file.store_string(JSON.stringify(manifest, "\t"))

func _draw_base_frame(sheet: Image, col: int, row: int) -> void:
	var o := Vector2i(col * FRAME_SIZE, row * FRAME_SIZE)
	var pose := _pose(row)
	var face := _face(col)
	var side := _side_sign(col)
	# Footprint/outline mannequin layer: intentionally neutral, so skin/outfit can be swapped independently.
	_rect(sheet, o.x + 12 + pose.torso_x, o.y + 9, 8, 13, OUTLINE)
	_rect(sheet, o.x + 13 + pose.torso_x, o.y + 10, 6, 11, BODY_GUIDE)
	_rect(sheet, o.x + 13 + pose.torso_x, o.y + 18, 6, 3, BODY_GUIDE_SHADE)
	# Head guide/outline.
	_rect(sheet, o.x + 11 + face.head_x, o.y + 3, 10, 8, OUTLINE)
	_rect(sheet, o.x + 12 + face.head_x, o.y + 4, 8, 6, BODY_GUIDE)
	# Arms and legs shift by animation row so overlays have a canonical body to follow.
	_rect(sheet, o.x + 9 + pose.left_arm_x, o.y + 11 + pose.left_arm_y, 3, 9, OUTLINE)
	_rect(sheet, o.x + 20 + pose.right_arm_x, o.y + 11 + pose.right_arm_y, 3, 9, OUTLINE)
	_rect(sheet, o.x + 10 + pose.left_arm_x, o.y + 12 + pose.left_arm_y, 1, 7, BODY_GUIDE)
	_rect(sheet, o.x + 21 + pose.right_arm_x, o.y + 12 + pose.right_arm_y, 1, 7, BODY_GUIDE)
	_rect(sheet, o.x + 12 + pose.left_leg_x, o.y + 21, 4, 7, OUTLINE)
	_rect(sheet, o.x + 17 + pose.right_leg_x, o.y + 21, 4, 7, OUTLINE)
	_rect(sheet, o.x + 13 + pose.left_leg_x, o.y + 21, 2, 6, BODY_GUIDE)
	_rect(sheet, o.x + 18 + pose.right_leg_x, o.y + 21, 2, 6, BODY_GUIDE)
	# Side-facing compression makes the directional contract visible even with simple generated pixels.
	if side != 0:
		_rect(sheet, o.x + 11 + side, o.y + 10, 3, 12, Color(0, 0, 0, 0))
	# Diagonals need to stay distinct from pure side-facing columns while better art is pending.
	if col == Direction.FRONT_RIGHT or col == Direction.FRONT_LEFT:
		_rect(sheet, o.x + 15 + side, o.y + 6, 2, 1, BODY_GUIDE_SHADE)
		_rect(sheet, o.x + 15 + side, o.y + 11, 2, 3, BODY_GUIDE_SHADE)
	elif col == Direction.BACK_RIGHT or col == Direction.BACK_LEFT:
		_rect(sheet, o.x + 14 + side, o.y + 4, 3, 2, BODY_GUIDE_SHADE)
		_rect(sheet, o.x + 14 + side, o.y + 15, 3, 4, BODY_GUIDE_SHADE)

func _draw_skin_frame(sheet: Image, col: int, row: int, skin: Color, shade: Color) -> void:
	var o := Vector2i(col * FRAME_SIZE, row * FRAME_SIZE)
	var pose := _pose(row)
	var face := _face(col)
	_rect(sheet, o.x + 13 + face.head_x, o.y + 5, 6, 5, skin)
	if col != Direction.BACK and col != Direction.BACK_LEFT and col != Direction.BACK_RIGHT:
		_rect(sheet, o.x + 14 + face.head_x, o.y + 7, 1, 1, shade)
		_rect(sheet, o.x + 18 + face.head_x, o.y + 7, 1, 1, shade)
	_rect(sheet, o.x + 10 + pose.left_arm_x, o.y + 17 + pose.left_arm_y, 2, 3, skin)
	_rect(sheet, o.x + 20 + pose.right_arm_x, o.y + 17 + pose.right_arm_y, 2, 3, skin)

func _draw_hair_frame(sheet: Image, col: int, row: int) -> void:
	var o := Vector2i(col * FRAME_SIZE, row * FRAME_SIZE)
	var face := _face(col)
	_rect(sheet, o.x + 12 + face.head_x, o.y + 3, 8, 3, HAIR)
	if col == Direction.BACK or col == Direction.BACK_LEFT or col == Direction.BACK_RIGHT:
		_rect(sheet, o.x + 12 + face.head_x, o.y + 5, 8, 4, HAIR)
	else:
		_rect(sheet, o.x + 12 + face.head_x, o.y + 5, 2, 3, HAIR)

func _draw_outfit_frame(sheet: Image, col: int, row: int) -> void:
	var o := Vector2i(col * FRAME_SIZE, row * FRAME_SIZE)
	var pose := _pose(row)
	_rect(sheet, o.x + 13 + pose.torso_x, o.y + 10, 6, 11, SUIT)
	_rect(sheet, o.x + 13 + pose.torso_x, o.y + 18, 6, 3, SUIT_SHADE)
	_rect(sheet, o.x + 15 + pose.torso_x, o.y + 10, 2, 8, SUIT_LIGHT)
	_rect(sheet, o.x + 13 + pose.torso_x, o.y + 13, 6, 2, ACCENT)
	var side := _side_sign(col)
	if col == Direction.FRONT_RIGHT or col == Direction.FRONT_LEFT:
		_rect(sheet, o.x + 16 + side, o.y + 10, 2, 5, SUIT_LIGHT)
	elif col == Direction.BACK_RIGHT or col == Direction.BACK_LEFT:
		_rect(sheet, o.x + 14 + side, o.y + 10, 2, 5, SUIT_SHADE)
	_rect(sheet, o.x + 10 + pose.left_arm_x, o.y + 12 + pose.left_arm_y, 2, 5, SUIT)
	_rect(sheet, o.x + 20 + pose.right_arm_x, o.y + 12 + pose.right_arm_y, 2, 5, SUIT)
	_rect(sheet, o.x + 13 + pose.left_leg_x, o.y + 21, 2, 6, SUIT_SHADE)
	_rect(sheet, o.x + 18 + pose.right_leg_x, o.y + 21, 2, 6, SUIT_SHADE)

func _draw_tool_frame(sheet: Image, col: int, row: int) -> void:
	if row != 6 and row != 8:
		return
	var o := Vector2i(col * FRAME_SIZE, row * FRAME_SIZE)
	var side := _side_sign(col)
	if side == 0:
		side = 1
	_rect(sheet, o.x + 21 + side, o.y + 15, 5, 2, OUTLINE)
	_rect(sheet, o.x + 22 + side, o.y + 15, 3, 1, TOOL)
	_rect(sheet, o.x + 25 + side, o.y + 14, 1, 3, ACCENT)

func _pose(row: int) -> Dictionary:
	match row:
		1:
			return { "torso_x": 0, "left_arm_x": -1, "left_arm_y": 1, "right_arm_x": 1, "right_arm_y": -1, "left_leg_x": -1, "right_leg_x": 1 }
		2:
			return { "torso_x": 0, "left_arm_x": 0, "left_arm_y": 0, "right_arm_x": 0, "right_arm_y": 0, "left_leg_x": 0, "right_leg_x": 0 }
		3:
			return { "torso_x": 0, "left_arm_x": 1, "left_arm_y": -1, "right_arm_x": -1, "right_arm_y": 1, "left_leg_x": 1, "right_leg_x": -1 }
		4:
			return { "torso_x": 0, "left_arm_x": 0, "left_arm_y": 0, "right_arm_x": 0, "right_arm_y": 0, "left_leg_x": 0, "right_leg_x": 0 }
		5:
			return { "torso_x": 1, "left_arm_x": -2, "left_arm_y": 1, "right_arm_x": 2, "right_arm_y": -2, "left_leg_x": -2, "right_leg_x": 2 }
		7:
			return { "torso_x": 1, "left_arm_x": -1, "left_arm_y": 0, "right_arm_x": 3, "right_arm_y": -2, "left_leg_x": -1, "right_leg_x": 1 }
		8:
			return { "torso_x": 0, "left_arm_x": 0, "left_arm_y": 0, "right_arm_x": 4, "right_arm_y": -1, "left_leg_x": 0, "right_leg_x": 0 }
		_:
			return { "torso_x": 0, "left_arm_x": 0, "left_arm_y": 0, "right_arm_x": 0, "right_arm_y": 0, "left_leg_x": 0, "right_leg_x": 0 }

func _face(col: int) -> Dictionary:
	match col:
		Direction.RIGHT, Direction.FRONT_RIGHT, Direction.BACK_RIGHT:
			return { "head_x": 1 }
		Direction.LEFT, Direction.FRONT_LEFT, Direction.BACK_LEFT:
			return { "head_x": -1 }
		_:
			return { "head_x": 0 }

func _side_sign(col: int) -> int:
	match col:
		Direction.RIGHT, Direction.FRONT_RIGHT, Direction.BACK_RIGHT:
			return 1
		Direction.LEFT, Direction.FRONT_LEFT, Direction.BACK_LEFT:
			return -1
		_:
			return 0

func _rect(image: Image, x: int, y: int, w: int, h: int, color: Color) -> void:
	for yy in range(max(0, y), min(image.get_height(), y + h)):
		for xx in range(max(0, x), min(image.get_width(), x + w)):
			image.set_pixel(xx, yy, color)
