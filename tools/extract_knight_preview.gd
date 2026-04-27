extends SceneTree

const INPUT_IDLE := "res://assets/art/2D Character Knight/1Knight/Idle_Shadowless.png"
const INPUT_WALK := "res://assets/art/2D Character Knight/1Knight/Walk_Shadowless.png"
const OUTPUT := "res://assets/art/sprites/generated/player_v2_knight_8dir_4row_reference.png"
const FRAME_SIZE := 64
const DIRECTIONS := 8
const WALK_COLUMNS := [1, 5, 9]
# Source row order in the knight pack is:
# 0 north-east, 1 east, 2 south-east, 3 south, 4 south-west,
# 5 west, 6 north-west, 7 north.
# Runtime column order is:
# front/south, front-right/south-east, right/east, back-right/north-east,
# back/north, back-left/north-west, left/west, front-left/south-west.
const SOURCE_ROW_FOR_RUNTIME_COLUMN := [3, 2, 1, 0, 7, 6, 5, 4]

func _init() -> void:
	var idle := _load(INPUT_IDLE)
	var walk := _load(INPUT_WALK)
	if idle == null or walk == null:
		quit(1)
		return
	var sheet := Image.create(FRAME_SIZE * DIRECTIONS, FRAME_SIZE * 4, false, Image.FORMAT_RGBA8)
	sheet.fill(Color(0, 0, 0, 0))
	for runtime_column in DIRECTIONS:
		var source_row: int = SOURCE_ROW_FOR_RUNTIME_COLUMN[runtime_column]
		_blit_cell(idle, sheet, 0, source_row, runtime_column, 0)
		for row in WALK_COLUMNS.size():
			_blit_cell(walk, sheet, WALK_COLUMNS[row], source_row, runtime_column, row + 1)
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

func _blit_cell(source: Image, target: Image, source_column: int, source_row: int, target_column: int, target_row: int) -> void:
	var rect := Rect2i(source_column * FRAME_SIZE, source_row * FRAME_SIZE, FRAME_SIZE, FRAME_SIZE)
	target.blit_rect(source, rect, Vector2i(target_column * FRAME_SIZE, target_row * FRAME_SIZE))
