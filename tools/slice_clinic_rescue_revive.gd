extends SceneTree

# Slice karma_priority_clinic_rescue_revive_atlas (1024x1024, 5x6 = 30 cells,
# no labels, uniform grid). Step 16 / clinic prop set.

const COLS := 5
const ROWS := 6
const ICON_PORTION := 0.92

const SOURCE := "res://assets/art/generated/priority_static_atlases/karma_priority_clinic_rescue_revive_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/clinic_rescue_revive"

const NAMES := [
	["clinic_sign", "clinic_terminals", "clinic_bed", "ambulance_stretcher", "medic_bag"],
	["medicine_cabinet", "holo_tripod", "diagnostic_kiosk", "scrip_tray", "biohazard_barricade"],
	["pillbox_tray", "biohazard_quarantine", "oxygen_tank_short", "oxygen_tank_tall", "surgical_light"],
	["bucket_caution_sign", "surgical_light_alt", "downed_marker_red_a", "downed_marker_red_b", "shield_buff"],
	["rescue_banner", "receipt", "medical_crate", "medical_terminal", "medical_terminal_alt"],
	["medical_terminal_b", "medical_terminal_c", "refugee_tent", "ritual_altar", "bench"],
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
	print("Wrote %d sliced clinic props to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
