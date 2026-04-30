extends SceneTree

# Slice karma_static_modular_walls_doors_atlas (1024x1024, 6 cols x 5 rows
# = 30 cells). Atlas IS labeled. Row 4 has only 3 cells filled (cols 2-4);
# we mark the empties as _blank and skip them on save.

const COLS := 6
const ROWS := 5
# Empirically-tuned per-row Y offsets to clip the icon area while skipping
# the label strip just above the next row's icon. Each cell is ~1024/5 = 205
# tall; icon takes the upper ~75%.
const ROW_Y_TOPS := [15, 270, 525, 785, 845]
const ICON_HEIGHT := 175
const ICON_WIDTH := 150

const SOURCE := "res://assets/art/generated/static_event_atlases/karma_static_modular_walls_doors_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/modular_walls_doors"

const NAMES := [
	["scifi_wall_straight", "scifi_wall_corner", "wood_wall_straight", "wood_wall_corner", "scrap_wall_straight", "scrap_wall_corner"],
	["stone_wall_straight", "stone_wall_corner", "clinic_door", "shop_door", "jail_door", "curtain_door_black_market"],
	["airlock_door_closed", "airlock_door_open", "gate_closed", "gate_open", "window_lit", "window_broken"],
	["fence_straight", "fence_corner", "railing", "barricade", "checkpoint_turnstile", "archway"],
	["_blank_4_0", "_blank_4_1", "sign_hanger_blank", "awning", "roof_edge", "_blank_4_5"],
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
		for col in range(COLS):
			var name: String = NAMES[row][col]
			if name.begins_with("_blank_"):
				continue
			var x: int = col * col_w + x_inset
			var icon := atlas.get_region(Rect2i(x, y_top, ICON_WIDTH, ICON_HEIGHT))
			var out_path := OUT_DIR + "/" + name + ".png"
			var save_err := icon.save_png(out_path)
			if save_err != OK:
				push_warning("Failed to save: " + out_path)
				continue
			manifest_entries.append({
				"name": name,
				"path": out_path,
				"source_atlas": SOURCE,
				"source_region": [x, y_top, ICON_WIDTH, ICON_HEIGHT]
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
