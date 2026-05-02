extends SceneTree

# Slice karma_static_modular_walls_doors_atlas (1024x1024, 6 cols x 4 rows
# = 24 cells). Atlas IS labeled but rows are NOT uniform — measured via
# debug_walls_doors_rows.gd by scanning column 0 for non-bg pixel mass:
#   Row 0 icon body: y=51..232 (label y=234..281)
#   Row 1 icon body: y=300..478 (label after)
#   Row 2 icon body: y=486..696 (label y=707..719)
#   Row 3 icon body: y=721..838 (label y=901..1002)
# Per-row HEIGHTS keep each crop tight to its row.

const COLS := 6
const ROWS := 4
const ROW_Y_TOPS := [10, 290, 480, 720]
const ROW_HEIGHTS := [225, 190, 220, 155]
const ICON_WIDTH := 155

const SOURCE := "res://assets/art/generated/static_event_atlases/karma_static_modular_walls_doors_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/modular_walls_doors"

const NAMES := [
	["scifi_wall_straight", "scifi_wall_corner", "wood_wall_straight", "wood_wall_corner", "scrap_wall_straight", "scrap_wall_corner"],
	["stone_wall_straight", "stone_wall_corner", "clinic_door", "shop_door", "jail_door", "curtain_door_black_market"],
	["airlock_door_closed", "airlock_door_open", "gate_closed", "gate_open", "window_lit", "window_broken"],
	["fence_straight", "fence_corner", "railing", "barricade", "checkpoint_turnstile", "archway"],
]

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
	var atlas := Image.load_from_file(SOURCE)
	if atlas == null:
		push_error("Could not load atlas: " + SOURCE)
		quit(1)
		return
	var col_w: int = atlas.get_width() / COLS
	var x_inset: int = (col_w - ICON_WIDTH) / 2

	var manifest_entries: Array = []
	for row in range(ROWS):
		var y_top: int = ROW_Y_TOPS[row]
		var icon_h: int = ROW_HEIGHTS[row]
		for col in range(COLS):
			var name: String = NAMES[row][col]
			if name.begins_with("_blank_"):
				continue
			var x: int = col * col_w + x_inset
			var icon := atlas.get_region(Rect2i(x, y_top, ICON_WIDTH, icon_h))
			var out_path := OUT_DIR + "/" + name + ".png"
			var save_err := icon.save_png(out_path)
			if save_err != OK:
				push_warning("Failed to save: " + out_path)
				continue
			manifest_entries.append({
				"name": name,
				"path": out_path,
				"source_atlas": SOURCE,
				"source_region": [x, y_top, ICON_WIDTH, icon_h]
			})

	var manifest_path := OUT_DIR + "/manifest.json"
	var manifest_file := FileAccess.open(manifest_path, FileAccess.WRITE)
	if manifest_file == null:
		push_error("Could not write manifest")
		quit(1)
		return
	manifest_file.store_string(JSON.stringify(manifest_entries, "  "))
	manifest_file.close()
	print("Wrote %d sliced walls/doors to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
