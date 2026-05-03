extends SceneTree

# Slice karma_static_ui_status_icons_atlas (1024x1024, 6x6 = 36 labeled
# generic karma/social status icons) into individual PNGs.
#
# Empirically-measured icon Y offsets from a left-column strip:
const ROW_Y_TOPS := [20, 185, 330, 490, 695, 855]
const ICON_HEIGHT := 100
const ICON_WIDTH := 110
const COLS := 6

const SOURCE := "res://assets/art/generated/static_event_atlases/karma_static_ui_status_icons_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/ui_status_icons"

# Names per (row, col), matching the atlas labels.
const NAMES := [
	["positive", "negative", "neutral", "wanted", "bounty", "witness"],
	["trade", "posse", "rumor", "forward", "contraband", "rescue"],
	["duel", "theft", "evidence", "theft_alt", "law", "clinic"],
	["supply_drop", "structure_repair", "sabotage", "chat", "faction", "faction_alt"],
	["mount", "downed_status", "karma_break", "local_proximity", "shop", "quest"],
	["mount_alt", "return", "bribe", "apology", "trust_vouch", "danger_heat"],
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
	for row in range(ROW_Y_TOPS.size()):
		var y_top: int = ROW_Y_TOPS[row]
		for col in range(COLS):
			var name: String = NAMES[row][col]
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
	print("Wrote %d sliced status icons to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
