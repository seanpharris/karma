extends SceneTree

# Slice karma_static_faction_reputation_symbols_atlas (1024x1024, 6x7 = 42
# small flat reputation/badge icons, no labels).

const COLS := 6
const ROWS := 7
const ICON_PORTION := 0.93

const SOURCE := "res://assets/art/generated/static_event_atlases/karma_static_faction_reputation_symbols_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/faction_reputation_symbols"

const NAMES := [
	["winged_halo", "demon_horned_red", "knight_blue_shield", "money_wagon", "leaf_clover_emblem", "hammer_pick_skull"],
	["green_cross_heart", "hammer_wrench_crossed", "hooded_assassin", "blue_flag_star_a", "blue_flag_star_b", "eye_blue"],
	["pirate_skull_swords", "spy_eye_yellow", "target_skull", "target_skull_money", "purple_horn_shout", "purple_question_shout"],
	["scarcity_x_box", "ribbon_medal", "fist_offering", "blade_swords_silver", "dark_blade", "money_bag_lock"],
	["green_shield", "red_shield_swords", "red_shield_swords_alt", "blue_heart_offering", "broken_heart_red", "_blank_4_5"],
	["dove_peace", "gold_coins_marker", "file_folder_evidence", "danger_triangle_a", "danger_triangle_b", "_blank_5_5"],
	["_blank_6_0", "_blank_6_1", "_blank_6_2", "_blank_6_3", "_blank_6_4", "_blank_6_5"],
]

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
	var atlas := Image.load_from_file(SOURCE)
	if atlas == null:
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
			if name.begins_with("_blank_"):
				continue
			var x: int = col * cell_w + x_inset
			var y: int = row * cell_h + y_inset
			var icon := atlas.get_region(Rect2i(x, y, icon_w, icon_h))
			var out_path := OUT_DIR + "/" + name + ".png"
			if icon.save_png(out_path) != OK:
				continue
			manifest_entries.append({
				"name": name, "path": out_path, "source_atlas": SOURCE,
				"source_region": [x, y, icon_w, icon_h]
			})
	var manifest_path := OUT_DIR + "/manifest.json"
	var manifest_file := FileAccess.open(manifest_path, FileAccess.WRITE)
	if manifest_file != null:
		manifest_file.store_string(JSON.stringify(manifest_entries, "  "))
		manifest_file.close()
	print("Wrote %d sliced faction symbols to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
