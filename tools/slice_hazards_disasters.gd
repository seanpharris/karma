extends SceneTree

# Slice karma_static_hazards_disasters_atlas (1024x1024, 6x6 = 36 cells, no labels).

const COLS := 6
const ROWS := 6
const ICON_PORTION := 0.93

const SOURCE := "res://assets/art/generated/static_event_atlases/karma_static_hazards_disasters_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/hazards_disasters"

const NAMES := [
	["warning_barricade", "fire_flames", "electric_portal_blue", "sparking_cable_yellow", "leaking_pipe_green", "_blank_0_5"],
	["steam_manhole", "oil_spill_dark", "toxic_pool_green", "radioactive_sign", "biohazard_cone_orange", "_blank_1_5"],
	["broken_glass_blue", "rock_pile_grey", "broken_metal_beam", "beam_debris", "dark_rocks", "_blank_2_5"],
	["blue_puddle", "red_flag_swirl", "purple_meteor", "broken_viewscreen", "sparking_generator_grey", "_blank_3_5"],
	["toxic_canister_green", "broken_planks", "broken_x_sign", "alert_siren_red", "repair_sign_yellow", "_blank_4_5"],
	["biohazard_crate_green", "medkit_white_first_aid", "dark_hole", "red_barrel_hazard", "repair_sign_yellow_alt", "_blank_5_5"],
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
	print("Wrote %d sliced hazards to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
