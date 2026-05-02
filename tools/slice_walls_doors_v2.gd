extends SceneTree

# Re-slice karma_static_modular_walls_doors_atlas with content-aware
# bounding-box detection. The original uniform-grid + per-row Y_TOPS
# slicing produced clipping because rows have varying label heights.
#
# This version: for each (col, row) cell, scan the cell's pixels and find
# the bounding box of OPAQUE / NON-LABEL content. Labels are detected as
# light-grey-on-grey text (low saturation, high lightness in narrow band
# at bottom of cell) and excluded from the bounding box.

const COLS := 6
const ROWS := 4
const SOURCE := "res://assets/art/generated/static_event_atlases/karma_static_modular_walls_doors_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/modular_walls_doors"

# Atlas is 6x4 (= 24 cells, each ~170x256), each cell containing icon + label.
# Earlier mis-read had ROWS=5; the bottom strip with sign-hanger/awning/roof
# items appears to be a separate sub-strip we treat as missing for now.
const NAMES := [
	["scifi_wall_straight", "scifi_wall_corner", "wood_wall_straight", "wood_wall_corner", "scrap_wall_straight", "scrap_wall_corner"],
	["stone_wall_straight", "stone_wall_corner", "clinic_door", "shop_door", "jail_door", "curtain_door_black_market"],
	["airlock_door_closed", "airlock_door_open", "gate_closed", "gate_open", "window_lit", "window_broken"],
	["fence_straight", "fence_corner", "railing", "barricade", "checkpoint_turnstile", "archway"],
]

# Content-aware crop: scan ONLY the top 60% of each cell (label-free zone)
# and find the bounding box of non-checkerboard pixels (the icon).
func find_icon_bbox(atlas: Image, x0: int, y0: int, w: int, h: int) -> Rect2i:
	var min_x := w
	var min_y := h
	var max_x := 0
	var max_y := 0
	var found := false
	# Only scan top 60% so we never grab label pixels.
	var scan_h: int = int(h * 0.60)
	for py in range(scan_h):
		for px in range(w):
			var c := atlas.get_pixel(x0 + px, y0 + py)
			var lightness: float = (c.r + c.g + c.b) / 3.0
			var max_ch: float = max(max(c.r, c.g), c.b)
			var min_ch: float = min(min(c.r, c.g), c.b)
			var saturation: float = max_ch - min_ch
			# Reject the grey checkerboard cells.
			var is_checker_a: bool = abs(lightness - 0.49) < 0.04 and saturation < 0.05
			var is_checker_b: bool = abs(lightness - 0.59) < 0.04 and saturation < 0.05
			if is_checker_a or is_checker_b:
				continue
			if px < min_x: min_x = px
			if py < min_y: min_y = py
			if px > max_x: max_x = px
			if py > max_y: max_y = py
			found = true
	if not found:
		return Rect2i(x0, y0, 0, 0)
	# Pad bbox by 4 px (so we don't shave the silhouette outline).
	min_x = max(0, min_x - 4)
	min_y = max(0, min_y - 4)
	max_x = min(w - 1, max_x + 4)
	max_y = min(scan_h - 1, max_y + 4)
	return Rect2i(x0 + min_x, y0 + min_y, max_x - min_x + 1, max_y - min_y + 1)

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
	var atlas := Image.load_from_file(SOURCE)
	if atlas == null:
		quit(1)
		return
	var cell_w: int = atlas.get_width() / COLS
	var cell_h: int = atlas.get_height() / ROWS

	var manifest_entries: Array = []
	for row in range(ROWS):
		for col in range(COLS):
			var name: String = NAMES[row][col]
			if name.begins_with("_blank_"):
				continue
			var x0: int = col * cell_w
			var y0: int = row * cell_h
			var bbox := find_icon_bbox(atlas, x0, y0, cell_w, cell_h)
			if bbox.size.x <= 4 or bbox.size.y <= 4:
				push_warning("No content found in cell %s; skipping" % name)
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
	print("Wrote %d content-aware sliced walls/doors to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
