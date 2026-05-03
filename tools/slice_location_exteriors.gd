extends SceneTree

# Slice karma_priority_location_exteriors_atlas (1024x1024, 4x3 = 12 cells).
# Building exteriors. Rows 0-1 unlabeled (icons fill cell); row 2 has number
# labels at bottom ("9. POSSE CAMP/RALLY POINT" etc.) — its slice height is
# trimmed to exclude the label band.

const COLS := 4
const ROWS := 3
const ICON_W_PORTION := 0.92

const SOURCE := "res://assets/art/generated/priority_static_atlases/karma_priority_location_exteriors_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/location_exteriors"

# Per-row Y_TOPS and HEIGHTS (in source pixels). Row 2's height stops at the
# top of the number-label band (around y=915).
const ROW_Y_TOPS := [10, 345, 645]
const ROW_HEIGHTS := [320, 295, 270]

const NAMES := [
	["clinic_exterior", "shop_kiosk_exterior", "bounty_office_exterior", "jail_block_exterior"],
	["checkpoint_guard_tower", "safehouse_tent", "repair_garage", "greenhouse_dome"],
	["posse_camp_rally", "faction_request_board_shelter", "safehouse_cave_entrance", "supply_drop_landing_pad"],
]

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
	var atlas := Image.load_from_file(SOURCE)
	if atlas == null:
		quit(1)
		return
	var cell_w: int = atlas.get_width() / COLS
	var icon_w: int = int(cell_w * ICON_W_PORTION)
	var x_inset: int = (cell_w - icon_w) / 2

	var manifest_entries: Array = []
	for row in range(ROWS):
		var y: int = ROW_Y_TOPS[row]
		var icon_h: int = ROW_HEIGHTS[row]
		for col in range(COLS):
			var name: String = NAMES[row][col]
			var x: int = col * cell_w + x_inset
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
	print("Wrote %d sliced location-exteriors to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
