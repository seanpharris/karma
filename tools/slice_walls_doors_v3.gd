extends SceneTree

# Per-cell content-aware bounding-box slicing for walls/doors.
# For each (row, col) cell:
# 1. Pre-define a generous candidate y-range based on row index.
# 2. Within that range, find pixels that are NOT checkerboard background.
# 3. Identify the LARGEST contiguous vertical band of saturated content
#    (the icon body) — labels are smaller bands of low-saturation pixels.
# 4. Crop tight to that band's bbox + small margin.

const COLS := 6
const ROWS := 4
const SOURCE := "res://assets/art/generated/static_event_atlases/karma_static_modular_walls_doors_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/modular_walls_doors"

# Per-row y ranges measured by debug_walls_doors_v2.gd (block-averaged
# content mass). Each range is strictly within icon-dominated bands; labels
# are intentionally OUT of range so they cannot leak into neighbouring crops.
#   Row 0 icon mass peak: y=48-191  (label band y=200-271, "Sci-Fi\nStraight")
#   Row 1 icon mass peak: y=304-447 (label band y=448-535)
#   Row 2 icon mass peak: y=544-655 (label band y=672-735, "Airlock Door\nClosed")
#   Row 3 icon mass peak: y=744-823 (label band y=856-1015)
const ROW_Y_RANGES := [[48, 192], [304, 448], [544, 656], [744, 824]]

const NAMES := [
	["scifi_wall_straight", "scifi_wall_corner", "wood_wall_straight", "wood_wall_corner", "scrap_wall_straight", "scrap_wall_corner"],
	["stone_wall_straight", "stone_wall_corner", "clinic_door", "shop_door", "jail_door", "curtain_door_black_market"],
	["airlock_door_closed", "airlock_door_open", "gate_closed", "gate_open", "window_lit", "window_broken"],
	["fence_straight", "fence_corner", "railing", "barricade", "checkpoint_turnstile", "archway"],
]

# A pixel is "icon content" if not checkerboard AND has reasonable saturation
# OR is a strong silhouette outline (very dark pixels are also icon).
func is_content(c: Color) -> bool:
	var lightness: float = (c.r + c.g + c.b) / 3.0
	var max_ch: float = max(max(c.r, c.g), c.b)
	var min_ch: float = min(min(c.r, c.g), c.b)
	var saturation: float = max_ch - min_ch
	# Reject the grey checkerboard.
	var is_checker_a: bool = abs(lightness - 0.49) < 0.05 and saturation < 0.05
	var is_checker_b: bool = abs(lightness - 0.59) < 0.05 and saturation < 0.05
	if is_checker_a or is_checker_b:
		return false
	# Reject pure-white-ish label text.
	if lightness > 0.85 and saturation < 0.10:
		return false
	# Reject pure-black-ish label text (some atlases have black labels).
	# But keep dark icon outlines (they have some color).
	if lightness < 0.15 and saturation < 0.05:
		return false
	return true

func find_icon_bbox(atlas: Image, x0: int, y_min: int, y_max: int, col_w: int) -> Rect2i:
	var min_x := col_w
	var min_y := y_max
	var max_x := -1
	var max_y := -1
	for y in range(y_min, y_max):
		for px in range(col_w):
			var c := atlas.get_pixel(x0 + px, y)
			if not is_content(c):
				continue
			if px < min_x: min_x = px
			if y < min_y: min_y = y
			if px > max_x: max_x = px
			if y > max_y: max_y = y
	if max_x < 0:
		return Rect2i(x0, y_min, 0, 0)
	# Pad by 4 px and clamp.
	min_x = max(0, min_x - 4)
	min_y = max(y_min, min_y - 4)
	max_x = min(col_w - 1, max_x + 4)
	max_y = min(y_max - 1, max_y + 4)
	return Rect2i(x0 + min_x, min_y, max_x - min_x + 1, max_y - min_y + 1)

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
	var atlas := Image.load_from_file(SOURCE)
	if atlas == null:
		quit(1)
		return
	var col_w: int = atlas.get_width() / COLS

	var manifest_entries: Array = []
	for row in range(ROWS):
		var y_min: int = ROW_Y_RANGES[row][0]
		var y_max: int = ROW_Y_RANGES[row][1]
		for col in range(COLS):
			var name: String = NAMES[row][col]
			var x0: int = col * col_w
			var bbox := find_icon_bbox(atlas, x0, y_min, y_max, col_w)
			if bbox.size.x <= 4 or bbox.size.y <= 4:
				push_warning("No icon found for %s; skipping" % name)
				continue
			var icon := atlas.get_region(bbox)
			var out_path := OUT_DIR + "/" + name + ".png"
			if icon.save_png(out_path) != OK:
				continue
			manifest_entries.append({
				"name": name, "path": out_path, "source_atlas": SOURCE,
				"source_region": [bbox.position.x, bbox.position.y, bbox.size.x, bbox.size.y]
			})
	var manifest_path := OUT_DIR + "/manifest.json"
	var manifest_file := FileAccess.open(manifest_path, FileAccess.WRITE)
	if manifest_file != null:
		manifest_file.store_string(JSON.stringify(manifest_entries, "  "))
		manifest_file.close()
	print("Wrote %d v3 sliced walls/doors to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
