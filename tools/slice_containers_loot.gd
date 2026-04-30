extends SceneTree

# Slice karma_static_containers_loot_atlas (1024x1024, 5x5 = 25 cells, no labels).

const COLS := 5
const ROWS := 5
const ICON_PORTION := 0.93

const SOURCE := "res://assets/art/generated/static_event_atlases/karma_static_containers_loot_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/containers_loot"

const NAMES := [
	["wood_chest", "scifi_chest", "dumpster_wood", "ornate_chest_jeweled", "medkit_white_box"],
	["gun_case_open", "tool_box_grey", "lockbox_metal", "duffel_bag_olive", "backpack_brown"],
	["cardboard_box_a", "cardboard_box_b", "money_sack_brown", "cash_pile_green", "bedroll_tan"],
	["cardboard_box_dark", "energy_cells_crate", "tools_in_open_box", "military_trunk_locked", "shipping_container_blue"],
	["cardboard_fragile_box", "treasure_chest_gold_trim", "safe_padlock", "scrap_junk_pile", "barrel_wood"],
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
	print("Wrote %d sliced containers/loot to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
