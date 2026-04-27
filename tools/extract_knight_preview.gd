extends SceneTree

const INPUT_IDLE := "res://assets/art/2D Character Knight/1Knight/Idle_Shadowless.png"
const INPUT_WALK := "res://assets/art/2D Character Knight/1Knight/Walk_Shadowless.png"
const OUTPUT := "res://assets/art/sprites/generated/player_v2_knight_8dir_4row_reference.png"
const FRAME_SIZE := 64
const DIRECTIONS := 8
const WALK_COLUMNS := [1, 5, 9]

func _init() -> void:
	var idle := _load(INPUT_IDLE)
	var walk := _load(INPUT_WALK)
	if idle == null or walk == null:
		quit(1)
		return
	var sheet := Image.create(FRAME_SIZE * DIRECTIONS, FRAME_SIZE * 4, false, Image.FORMAT_RGBA8)
	sheet.fill(Color(0, 0, 0, 0))
	for direction in DIRECTIONS:
		_blit_cell(idle, sheet, 0, direction, 0)
		for row in WALK_COLUMNS.size():
			_blit_cell(walk, sheet, WALK_COLUMNS[row], direction, row + 1)
	sheet.save_png(OUTPUT)
	print("Extracted knight preview to ", OUTPUT)
	quit(0)

func _load(path: String) -> Image:
	var image := Image.new()
	var err := image.load(path)
	if err != OK:
		printerr("Could not load ", path, ": ", err)
		return null
	image.convert(Image.FORMAT_RGBA8)
	return image

func _blit_cell(source: Image, target: Image, source_column: int, source_row: int, target_row: int) -> void:
	var rect := Rect2i(source_column * FRAME_SIZE, source_row * FRAME_SIZE, FRAME_SIZE, FRAME_SIZE)
	target.blit_rect(source, rect, Vector2i(source_row * FRAME_SIZE, target_row * FRAME_SIZE))
