extends SceneTree

# Find the actual icon row centers in the walls/doors atlas by computing
# per-row "visual mass" (count of non-checkerboard pixels per scanline)
# and reporting peaks. The strip pulls a 60-px-wide column from x=20 to 80
# (column 0's icon area, excluding edges).

const SOURCE := "res://assets/art/generated/static_event_atlases/karma_static_modular_walls_doors_atlas.jpg"

func _initialize() -> void:
	var atlas := Image.load_from_file(SOURCE)
	if atlas == null:
		quit(1)
		return
	var h := atlas.get_height()
	# Scan full atlas width so labels (which can be wider than icons) are seen.
	var col_x_min: int = 0
	var col_x_max: int = atlas.get_width()
	var row_mass: Array[int] = []
	for y in range(h):
		var mass: int = 0
		for x in range(col_x_min, col_x_max):
			var c := atlas.get_pixel(x, y)
			var lightness: float = (c.r + c.g + c.b) / 3.0
			var max_ch: float = max(max(c.r, c.g), c.b)
			var min_ch: float = min(min(c.r, c.g), c.b)
			var saturation: float = max_ch - min_ch
			# Reject grey checkerboard (alternating 0.49 and 0.59 lightness, low saturation)
			var is_bg: bool = (abs(lightness - 0.49) < 0.05 or abs(lightness - 0.59) < 0.05) and saturation < 0.05
			if not is_bg:
				mass += 1
		row_mass.append(mass)

	# Find GAPS — contiguous y ranges with very few non-bg pixels. Gaps separate
	# icon bands and label bands. Threshold tuned for full-width scan.
	print("Y-gaps (rows with low visual mass):")
	var threshold: int = 800
	var gap_start: int = -1
	for y in range(h):
		if row_mass[y] <= threshold:
			if gap_start < 0:
				gap_start = y
		else:
			if gap_start >= 0 and y - gap_start >= 8:
				print("  gap y=%d..%d (height %d)" % [gap_start, y - 1, y - gap_start])
			gap_start = -1
	if gap_start >= 0:
		print("  gap y=%d..%d (height %d)" % [gap_start, h - 1, h - gap_start])
	quit(0)
