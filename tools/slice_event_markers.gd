extends SceneTree

# Slice karma_static_event_markers_atlas (1024x1024, 6x6 = 36 small flat
# icons with NO labels — uniform grid, simplest of the labeled atlases).
#
# Visually surveyed layout (row, col):
#   Row 0: target_ring, location_pin, warning_triangle, shield, racing_flag, ring_buoy
#   Row 1: skull_crossbones, wanted_poster, kneeling_figure, red_cross, money_bag, exclamation
#   Row 2: crossed_swords, wanted_poster_alt, parachute_mushroom, red_cross_alt, money_bag_alt, exclamation_alt
#   Row 3: wrench_hammer, fire_gear, magnifier, eye_bubble, question_bubble, posse_circle
#   Row 4: handshake_top, crossed_X, lock_alert, x_box, package, fire_triangle
#   Row 5: handshake_bottom, big_X_swords, horse, shovel_chest, money_arrow, fire_triangle_alt

const COLS := 6
const ROWS := 6
const ICON_PORTION := 0.95 # near-full cell since there are no labels

const SOURCE := "res://assets/art/generated/static_event_atlases/karma_static_event_markers_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/event_markers"

const NAMES := [
	["target_ring", "location_pin", "warning_triangle", "shield", "racing_flag", "ring_buoy"],
	["skull_crossbones", "wanted_poster", "kneeling_figure", "red_cross", "money_bag", "exclamation"],
	["crossed_swords", "wanted_poster_alt", "parachute_mushroom", "red_cross_alt", "money_bag_alt", "exclamation_alt"],
	["wrench_hammer", "fire_gear", "magnifier", "eye_bubble", "question_bubble", "posse_circle"],
	["handshake_top", "crossed_x", "lock_alert", "x_box", "package", "fire_triangle"],
	["handshake_bottom", "big_x_swords", "horse", "shovel_chest", "money_arrow", "fire_triangle_alt"],
]

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
	var atlas := Image.load_from_file(SOURCE)
	if atlas == null:
		push_error("Could not load atlas: " + SOURCE)
		quit(1)
		return
	var atlas_w := atlas.get_width()
	var atlas_h := atlas.get_height()
	var cell_w: int = atlas_w / COLS
	var cell_h: int = atlas_h / ROWS
	var icon_w: int = int(cell_w * ICON_PORTION)
	var icon_h: int = int(cell_h * ICON_PORTION)
	var x_inset: int = (cell_w - icon_w) / 2
	var y_inset: int = (cell_h - icon_h) / 2

	var manifest_entries: Array = []
	for row in range(ROWS):
		for col in range(COLS):
			var name: String = NAMES[row][col]
			var x: int = col * cell_w + x_inset
			var y: int = row * cell_h + y_inset
			var icon := atlas.get_region(Rect2i(x, y, icon_w, icon_h))
			var out_path := OUT_DIR + "/" + name + ".png"
			var save_err := icon.save_png(out_path)
			if save_err != OK:
				push_warning("Failed to save: " + out_path)
				continue
			manifest_entries.append({
				"name": name,
				"path": out_path,
				"source_atlas": SOURCE,
				"source_region": [x, y, icon_w, icon_h]
			})

	var manifest_path := OUT_DIR + "/manifest.json"
	var manifest_file := FileAccess.open(manifest_path, FileAccess.WRITE)
	if manifest_file == null:
		push_error("Could not write manifest")
		quit(1)
		return
	manifest_file.store_string(JSON.stringify(manifest_entries, "  "))
	manifest_file.close()
	print("Wrote %d sliced event markers to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
