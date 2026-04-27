extends SceneTree

const INPUTS := {
	"front": "res://assets/art/reference/generated/karma_player_v2_front_pose_batch.jpg",
	"right": "res://assets/art/reference/generated/karma_player_v2_right_pose_batch.jpg",
	"back": "res://assets/art/reference/generated/karma_player_v2_back_pose_batch.jpg",
}
const OUTPUT_DIR := "res://assets/art/sprites/generated"
const FRAME_SIZE := 64
const SOURCE_GRID := 4
const CHROMA_R := 0
const CHROMA_G := 255
const CHROMA_B := 0
const CHROMA_TOLERANCE := 96

func _init() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUTPUT_DIR))
	var front := _load_and_extract("front")
	var right := _load_and_extract("right")
	var back := _load_and_extract("back")
	if front.is_empty() or right.is_empty() or back.is_empty():
		printerr("Failed to extract one or more direction batches.")
		quit(1)
		return

	_save_direction_sheet("front", front)
	_save_direction_sheet("right", right)
	_save_direction_sheet("back", back)
	_save_runtime_preview(front, right, back)
	print("Extracted Gemini pose candidates to ", OUTPUT_DIR)
	quit(0)

func _load_and_extract(direction: String) -> Array[Image]:
	var image := Image.new()
	var err := image.load(INPUTS[direction])
	if err != OK:
		printerr("Could not load ", INPUTS[direction], ": ", err)
		return []

	var cell_w := image.get_width() / SOURCE_GRID
	var cell_h := image.get_height() / SOURCE_GRID
	var frames: Array[Image] = []
	for row in SOURCE_GRID:
		# Use the strongest/clearest pose from column 1 as a stable prototype frame.
		var source_rect := Rect2i(cell_w, row * cell_h, cell_w, cell_h)
		var frame := Image.create(FRAME_SIZE, FRAME_SIZE, false, Image.FORMAT_RGBA8)
		frame.fill(Color(0, 0, 0, 0))
		var cropped := image.get_region(source_rect)
		_key_chroma(cropped)
		var bounds := _content_bounds(cropped)
		if bounds.size.x <= 0 or bounds.size.y <= 0:
			frames.append(frame)
			continue
		var sprite: Image = cropped.get_region(bounds)
		sprite.convert(Image.FORMAT_RGBA8)
		var scale: float = min(float(FRAME_SIZE - 8) / sprite.get_width(), float(FRAME_SIZE - 6) / sprite.get_height())
		scale = min(scale, 1.0)
		var target_w: int = max(1, int(round(sprite.get_width() * scale)))
		var target_h: int = max(1, int(round(sprite.get_height() * scale)))
		sprite.resize(target_w, target_h, Image.INTERPOLATE_NEAREST)
		var dest := Vector2i((FRAME_SIZE - target_w) / 2, FRAME_SIZE - target_h - 3)
		frame.blit_rect(sprite, Rect2i(Vector2i.ZERO, sprite.get_size()), dest)
		frames.append(frame)
	return frames

func _key_chroma(image: Image) -> void:
	for y in image.get_height():
		for x in image.get_width():
			var color: Color = image.get_pixel(x, y)
			var r: int = int(round(color.r * 255.0))
			var g: int = int(round(color.g * 255.0))
			var b: int = int(round(color.b * 255.0))
			var green_distance: int = abs(r - CHROMA_R) + abs(g - CHROMA_G) + abs(b - CHROMA_B)
			if green_distance <= CHROMA_TOLERANCE or (g > 120 and g > r * 1.25 and g > b * 1.25):
				image.set_pixel(x, y, Color(0, 0, 0, 0))
			else:
				image.set_pixel(x, y, Color(color.r, color.g, color.b, 1.0))

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

func _save_direction_sheet(direction: String, frames: Array[Image]) -> void:
	var sheet := Image.create(FRAME_SIZE, FRAME_SIZE * frames.size(), false, Image.FORMAT_RGBA8)
	sheet.fill(Color(0, 0, 0, 0))
	for i in frames.size():
		sheet.blit_rect(frames[i], Rect2i(0, 0, FRAME_SIZE, FRAME_SIZE), Vector2i(0, i * FRAME_SIZE))
	sheet.save_png(OUTPUT_DIR + "/player_v2_%s_pose_extract.png" % direction)

func _save_runtime_preview(front: Array[Image], right: Array[Image], back: Array[Image]) -> void:
	var rows := 4
	var columns := 8
	var sheet := Image.create(FRAME_SIZE * columns, FRAME_SIZE * rows, false, Image.FORMAT_RGBA8)
	sheet.fill(Color(0, 0, 0, 0))
	for row in rows:
		var row_frames: Array[Image] = [
			front[row],
			right[row],
			right[row],
			back[row],
			back[row],
			back[row],
			_flip_horizontal(right[row]),
			_flip_horizontal(right[row]),
		]
		for col in columns:
			sheet.blit_rect(row_frames[col], Rect2i(0, 0, FRAME_SIZE, FRAME_SIZE), Vector2i(col * FRAME_SIZE, row * FRAME_SIZE))
	sheet.save_png(OUTPUT_DIR + "/player_v2_engineer_8dir_4row_candidate.png")

func _flip_horizontal(source: Image) -> Image:
	var copy := source.duplicate()
	copy.flip_x()
	return copy
