extends SceneTree

# Slice karma_priority_theme_variant_matrix_atlas (1024x1024). Irregular
# layout — items aren't a strict grid. Approximate 4x4 with several blank
# cells; the named items show the same mechanical object across themes.

const COLS := 4
const ROWS := 4
const ICON_PORTION := 0.93

const SOURCE := "res://assets/art/generated/priority_static_atlases/karma_priority_theme_variant_matrix_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/theme_variant_matrix"

const NAMES := [
	["wagon_supply_western", "pod_supply_scifi", "barrels_supply_postapoc", "_blank_0_3"],
	["ornate_chest_loot_fantasy", "_blank_1_1", "_blank_1_2", "_blank_1_3"],
	["plank_lawless_western", "scifi_archway", "tire_barricade_postapoc", "fantasy_guild_gate"],
	["red_cross_sign_western", "atm_clinic_scifi", "red_cross_tent_postapoc", "fantasy_shrine"],
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
	print("Wrote %d sliced theme variants to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
