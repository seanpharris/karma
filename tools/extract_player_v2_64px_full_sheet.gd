extends SceneTree

const INPUT := "res://assets/art/reference/generated/karma_player_v2_64px_full_sheet_chroma_regen.jpg"
const OUTPUT_DIR := "res://assets/art/sprites/generated"
const FRAME_SIZE := 64
const SOURCE_ROWS := 4
const SOURCE_COLUMNS := 4
const CHROMA_R := 0
const CHROMA_G := 255
const CHROMA_B := 0
const CHROMA_TOLERANCE := 110

func _init() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUTPUT_DIR))
	var frames := _load_sheet()
	if frames.is_empty():
		printerr("Failed to extract full 64px chroma sheet.")
		quit(1)
		return
	_save_direction_extracts(frames)
	_save_runtime_preview(frames)
	print("Extracted full-sheet 64px player v2 candidate to ", OUTPUT_DIR)
	quit(0)

func _load_sheet() -> Array[Array]:
	var image := Image.new()
	var err := image.load(INPUT)
	if err != OK:
		printerr("Could not load ", INPUT, ": ", err)
		return []
	image.convert(Image.FORMAT_RGBA8)
	var cell_w := image.get_width() / SOURCE_COLUMNS
	var cell_h := image.get_height() / SOURCE_ROWS
	var rows: Array[Array] = []
	for source_row in SOURCE_ROWS:
		var row_frames: Array[Image] = []
		for source_col in SOURCE_COLUMNS:
			var source_rect := Rect2i(source_col * cell_w, source_row * cell_h, cell_w, cell_h)
			row_frames.append(_normalize_cell(image.get_region(source_rect)))
		rows.append(row_frames)
	return rows

func _normalize_cell(cell: Image) -> Image:
	_key_chroma(cell)
	_remove_edge_background(cell)
	var bounds := _content_bounds(cell)
	var frame := Image.create(FRAME_SIZE, FRAME_SIZE, false, Image.FORMAT_RGBA8)
	frame.fill(Color(0, 0, 0, 0))
	if bounds.size.x <= 0 or bounds.size.y <= 0:
		return frame
	var sprite: Image = cell.get_region(bounds)
	sprite.convert(Image.FORMAT_RGBA8)
	var scale: float = min(float(FRAME_SIZE - 10) / sprite.get_width(), float(FRAME_SIZE - 6) / sprite.get_height())
	scale = min(scale, 1.0)
	var target_w: int = max(1, int(round(sprite.get_width() * scale)))
	var target_h: int = max(1, int(round(sprite.get_height() * scale)))
	sprite.resize(target_w, target_h, Image.INTERPOLATE_NEAREST)
	var dest := Vector2i((FRAME_SIZE - target_w) / 2, FRAME_SIZE - target_h - 3)
	frame.blit_rect(sprite, Rect2i(Vector2i.ZERO, sprite.get_size()), dest)
	_remove_edge_background(frame)
	return frame

func _key_chroma(image: Image) -> void:
	for y in image.get_height():
		for x in image.get_width():
			var color: Color = image.get_pixel(x, y)
			if _is_chroma_green(color):
				image.set_pixel(x, y, Color(0, 0, 0, 0))
			else:
				image.set_pixel(x, y, Color(color.r, color.g, color.b, 1.0))

func _remove_edge_background(image: Image) -> void:
	var width := image.get_width()
	var height := image.get_height()
	var visited := PackedByteArray()
	visited.resize(width * height)
	var queue: Array[Vector2i] = []
	for x in width:
		_queue_background_pixel(image, visited, queue, Vector2i(x, 0))
		_queue_background_pixel(image, visited, queue, Vector2i(x, height - 1))
	for y in height:
		_queue_background_pixel(image, visited, queue, Vector2i(0, y))
		_queue_background_pixel(image, visited, queue, Vector2i(width - 1, y))
	var index := 0
	while index < queue.size():
		var point := queue[index]
		index += 1
		image.set_pixel(point.x, point.y, Color(0, 0, 0, 0))
		_queue_background_pixel(image, visited, queue, point + Vector2i.LEFT)
		_queue_background_pixel(image, visited, queue, point + Vector2i.RIGHT)
		_queue_background_pixel(image, visited, queue, point + Vector2i.UP)
		_queue_background_pixel(image, visited, queue, point + Vector2i.DOWN)

func _queue_background_pixel(image: Image, visited: PackedByteArray, queue: Array[Vector2i], point: Vector2i) -> void:
	if point.x < 0 or point.y < 0 or point.x >= image.get_width() or point.y >= image.get_height():
		return
	var offset := point.y * image.get_width() + point.x
	if visited[offset] != 0:
		return
	visited[offset] = 1
	var color := image.get_pixel(point.x, point.y)
	if color.a <= 0.05 or _is_chroma_green(color):
		queue.append(point)

func _is_chroma_green(color: Color) -> bool:
	var r: int = int(round(color.r * 255.0))
	var g: int = int(round(color.g * 255.0))
	var b: int = int(round(color.b * 255.0))
	var green_distance: int = abs(r - CHROMA_R) + abs(g - CHROMA_G) + abs(b - CHROMA_B)
	return green_distance <= CHROMA_TOLERANCE or (g > 96 and g > r * 1.25 and g > b * 1.25)

func _content_bounds(image: Image) -> Rect2i:
	var min_x := image.get_width()
	var min_y := image.get_height()
	var max_x := -1
	var max_y := -1
	for y in image.get_height():
		for x in image.get_width():
			if image.get_pixel(x, y).a > 0.05:
				min_x = min(min_x, x)
				min_y = min(min_y, y)
				max_x = max(max_x, x)
				max_y = max(max_y, y)
	if max_x < min_x or max_y < min_y:
		return Rect2i(0, 0, 0, 0)
	return Rect2i(min_x, min_y, max_x - min_x + 1, max_y - min_y + 1)

func _save_direction_extracts(frames: Array[Array]) -> void:
	var names := ["front", "right", "back", "front_right"]
	for row in frames.size():
		var sheet := Image.create(FRAME_SIZE * SOURCE_COLUMNS, FRAME_SIZE, false, Image.FORMAT_RGBA8)
		sheet.fill(Color(0, 0, 0, 0))
		for col in SOURCE_COLUMNS:
			sheet.blit_rect(frames[row][col], Rect2i(0, 0, FRAME_SIZE, FRAME_SIZE), Vector2i(col * FRAME_SIZE, 0))
		sheet.save_png(OUTPUT_DIR + "/player_v2_64px_%s_full_sheet_extract.png" % names[row])

func _save_runtime_preview(frames: Array[Array]) -> void:
	var rows := 4
	var columns := 8
	var sheet := Image.create(FRAME_SIZE * columns, FRAME_SIZE * rows, false, Image.FORMAT_RGBA8)
	sheet.fill(Color(0, 0, 0, 0))
	for anim in rows:
		var front: Image = frames[0][anim]
		var right: Image = frames[1][anim]
		var back: Image = frames[2][anim]
		var front_right: Image = frames[3][anim]
		var back_right := back
		var row_frames: Array[Image] = [
			front,
			front_right,
			right,
			back_right,
			back,
			_flip_horizontal(back_right),
			_flip_horizontal(right),
			_flip_horizontal(front_right),
		]
		for col in columns:
			sheet.blit_rect(row_frames[col], Rect2i(0, 0, FRAME_SIZE, FRAME_SIZE), Vector2i(col * FRAME_SIZE, anim * FRAME_SIZE))
	sheet.save_png(OUTPUT_DIR + "/player_v2_engineer_8dir_4row_candidate.png")

func _flip_horizontal(source: Image) -> Image:
	var copy := source.duplicate()
	copy.flip_x()
	return copy
