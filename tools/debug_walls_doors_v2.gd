extends SceneTree

# Print row-by-row content mass for the walls/doors atlas, downsampled to
# blocks of 4 y-rows so output is manageable. Helps locate real icon bands
# vs label bands without a too-high threshold.

const SOURCE := "res://assets/art/generated/static_event_atlases/karma_static_modular_walls_doors_atlas.jpg"

func _initialize() -> void:
	var atlas := Image.load_from_file(SOURCE)
	if atlas == null:
		quit(1)
		return
	var w := atlas.get_width()
	var h := atlas.get_height()
	print("Atlas: %dx%d" % [w, h])
	var row_mass: Array[int] = []
	for y in range(h):
		var mass: int = 0
		for x in range(w):
			var c := atlas.get_pixel(x, y)
			var lightness: float = (c.r + c.g + c.b) / 3.0
			var max_ch: float = max(max(c.r, c.g), c.b)
			var min_ch: float = min(min(c.r, c.g), c.b)
			var saturation: float = max_ch - min_ch
			# Reject grey checkerboard.
			var is_bg: bool = (abs(lightness - 0.49) < 0.05 or abs(lightness - 0.59) < 0.05) and saturation < 0.05
			if not is_bg:
				mass += 1
		row_mass.append(mass)

	# Print 8-row block averages so you can see content distribution.
	print("Per-8-row block averages (y, avg_mass):")
	var block_size: int = 8
	for by in range(0, h, block_size):
		var sum: int = 0
		for k in range(block_size):
			if by + k < h:
				sum += row_mass[by + k]
		var avg: int = sum / block_size
		var bar := ""
		for i in range(avg / 30):
			bar += "#"
		print("  y=%4d-%4d  mass=%4d  %s" % [by, by + block_size - 1, avg, bar])
	quit(0)
