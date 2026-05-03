extends SceneTree

# Slice karma_static_crafting_stations_atlas (1024x1024, 3x4 = 12 cells,
# no labels). Step 36 crafting workbench variants.

const COLS := 3
const ROWS := 4
const ICON_PORTION := 0.93

const SOURCE := "res://assets/art/generated/static_event_atlases/karma_static_crafting_stations_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/crafting_stations"

const NAMES := [
	["mechanical_workbench", "alchemy_workbench", "weapons_display_pegboard"],
	["pressure_vessel_meter", "water_filtration_system", "fuel_pump_barrel"],
	["scribe_desk", "cluttered_paper_desk", "recycling_bin_blue"],
	["electronics_workshop", "hydroponics_planter_grown", "computer_workbench"],
]

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
	var atlas := Image.load_from_file(SOURCE)
	if atlas == null:
		push_error("Could not load atlas: " + SOURCE)
		quit(1)
		return
	var cell_w: int = atlas.get_width() / COLS
	var cell_h: int = atlas.get_height() / ROWS
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
	print("Wrote %d sliced crafting stations to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
