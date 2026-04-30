extends SceneTree

# Slice karma_static_player_interaction_props_atlas (1024x1024, 4x5 = 20 cells, no labels).

const COLS := 4
const ROWS := 5
const ICON_PORTION := 0.93

const SOURCE := "res://assets/art/generated/static_event_atlases/karma_static_player_interaction_props_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/player_interaction_props"

const NAMES := [
	["barter_terminal", "neon_handshake", "gift_box_silver", "alert_sign_post"],
	["rifles_warden_star", "satellite_dish", "locked_chest_rusty", "signed_contract"],
	["faction_flag_small", "wax_seal_red", "voting_decision_tablet", "money_envelope_seal"],
	["magnet_coin", "gear_chest", "blue_arrow_down", "blank_parchment"],
	["signed_parchment", "evil_eye_computer", "badge_slot_board", "casino_chip_dice_table"],
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
	print("Wrote %d sliced player-interaction props to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
