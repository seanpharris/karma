extends SceneTree

# Slice karma_priority_structure_world_state_atlas (1024x1024, 5x6 = 30 cells,
# no labels, uniform grid). Step 12 sabotage / damage state display.

const COLS := 5
const ROWS := 6
const ICON_PORTION := 0.93

const SOURCE := "res://assets/art/generated/priority_static_atlases/karma_priority_structure_world_state_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/structure_world_state"

const NAMES := [
	["generator_pristine", "generator_damaged", "generator_wrecked", "generator_sabotaged_wires", "greenhouse_pristine"],
	["greenhouse_shattered", "work_table_tools", "sabotaged_tool_pile", "electrical_box_closed", "electrical_box_sparking"],
	["chain_fence", "chain_fence_alt", "chain_fence_door_open", "stone_gate", "manhole_cover_a"],
	["concrete_slab", "concrete_slab_sabotaged", "manhole_cover_b", "manhole_uncovered_pit", "water_tank_blue"],
	["water_tank_white", "water_tank_damaged", "surveillance_camera", "security_camera", "broken_camera"],
	["notice_board_cluttered", "notice_board_blank", "notice_board_busy", "lumber_pile", "fire_pit"],
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
	print("Wrote %d sliced structure-state props to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
