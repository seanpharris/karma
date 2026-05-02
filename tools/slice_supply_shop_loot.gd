extends SceneTree

# Slice karma_priority_supply_shop_loot_atlas (1024x1024, 4x6 = 24 cells,
# no labels, uniform grid). Step 30 supply drop + shop UX prop set.
# The original 5x5 layout was wrong — the atlas is actually 4 cols x 6 rows
# and the bottom row continues with extra props (wheelbarrow, papers, box).

const COLS := 4
const ROWS := 6
const ICON_PORTION := 0.93

const SOURCE := "res://assets/art/generated/priority_static_atlases/karma_priority_supply_shop_loot_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/supply_shop_loot"

const NAMES := [
	["supply_drop_parachute", "barrel_metal", "bedroll_blanket_pack", "trade_table_scales"],
	["shop_kiosk", "shop_shelves", "supply_tent", "locked_metal_chest"],
	["wood_chest_open", "loot_crate_stamped", "backpack_brown", "wrapped_parcel"],
	["ammo_crate_metal", "medical_crate_red_cross", "weapon_case_long", "tool_box_open"],
	["money_sack", "cluttered_loot_pile", "shop_signpost_blank", "discount_tag"],
	["scrap_metal_pile", "wheelbarrow_empty", "papers_stack", "cardboard_box"],
]

# Pixel is "icon content" if it isn't part of the grey checkerboard background.
# Wider tolerance (±0.10) absorbs JPEG-compression noise around the bg colour
# so we don't mistake noise pixels for icon content (which would create thin
# bridges between blobs and break connected-component splits).
func is_content(c: Color) -> bool:
	var lightness: float = (c.r + c.g + c.b) / 3.0
	var max_ch: float = max(max(c.r, c.g), c.b)
	var min_ch: float = min(min(c.r, c.g), c.b)
	var saturation: float = max_ch - min_ch
	# Whole grey range from ~0.39 to ~0.69 with low saturation == background.
	if lightness >= 0.39 and lightness <= 0.69 and saturation < 0.10:
		return false
	return true

# Content-aware crop using connected-component analysis. Icons overflow into
# neighbouring cells, so we flood-fill all 4-connected blobs and take the
# bbox of the LARGEST one (== the actual icon, vs. tiny overflow fragments).
func find_icon_bbox(atlas: Image, x0: int, y0: int, w: int, h: int) -> Rect2i:
	# Build a content mask for the cell.
	var mask: PackedByteArray = PackedByteArray()
	mask.resize(w * h)
	for py in range(h):
		for px in range(w):
			if is_content(atlas.get_pixel(x0 + px, y0 + py)):
				mask[py * w + px] = 1
	# Flood-fill each unvisited content pixel; track size + bbox per blob.
	var visited: PackedByteArray = PackedByteArray()
	visited.resize(w * h)
	var best_size: int = 0
	var best_bbox := Rect2i(0, 0, 0, 0)
	for sy in range(h):
		for sx in range(w):
			var idx: int = sy * w + sx
			if mask[idx] == 0 or visited[idx] == 1:
				continue
			# BFS flood-fill.
			var stack: Array = [[sx, sy]]
			visited[idx] = 1
			var size: int = 0
			var bx_min: int = sx
			var by_min: int = sy
			var bx_max: int = sx
			var by_max: int = sy
			while not stack.is_empty():
				var p: Array = stack.pop_back()
				var cx: int = p[0]
				var cy: int = p[1]
				size += 1
				if cx < bx_min: bx_min = cx
				if cy < by_min: by_min = cy
				if cx > bx_max: bx_max = cx
				if cy > by_max: by_max = cy
				for d in [[1,0],[-1,0],[0,1],[0,-1]]:
					var nx: int = cx + d[0]
					var ny: int = cy + d[1]
					if nx < 0 or nx >= w or ny < 0 or ny >= h:
						continue
					var nidx: int = ny * w + nx
					if mask[nidx] == 1 and visited[nidx] == 0:
						visited[nidx] = 1
						stack.append([nx, ny])
			if size > best_size:
				best_size = size
				best_bbox = Rect2i(bx_min, by_min, bx_max - bx_min + 1, by_max - by_min + 1)
	if best_size == 0:
		return Rect2i(x0, y0, 0, 0)
	# Pad by 3 px and clamp.
	var bx: int = max(0, best_bbox.position.x - 3)
	var by: int = max(0, best_bbox.position.y - 3)
	var bw: int = min(w - bx, best_bbox.size.x + 6)
	var bh: int = min(h - by, best_bbox.size.y + 6)
	return Rect2i(x0 + bx, y0 + by, bw, bh)

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
	var atlas := Image.load_from_file(SOURCE)
	if atlas == null:
		push_error("Could not load atlas: " + SOURCE)
		quit(1)
		return
	var cell_w: int = atlas.get_width() / COLS
	var cell_h: int = atlas.get_height() / ROWS

	# Atlas icons are placed ~35 px below their cell top (visually centered
	# in cell + slight downward offset). Shifting the scan window down by 35
	# excludes overflow from the row above; we still scan the full cell_h so
	# the current row's icon fits.
	const Y_SHIFT := 35
	var manifest_entries: Array = []
	for row in range(ROWS):
		for col in range(COLS):
			var name: String = NAMES[row][col]
			if name.begins_with("_blank_"):
				continue
			var x0: int = col * cell_w
			var y0: int = row * cell_h + Y_SHIFT
			# Clamp scan height so we don't reach past atlas bottom.
			var scan_h: int = cell_h
			if y0 + scan_h > atlas.get_height():
				scan_h = atlas.get_height() - y0
			var bbox := find_icon_bbox(atlas, x0, y0, cell_w, scan_h)
			if bbox.size.x <= 4 or bbox.size.y <= 4:
				push_warning("No icon found for %s; skipping" % name)
				continue
			var icon := atlas.get_region(bbox)
			var x: int = bbox.position.x
			var y: int = bbox.position.y
			var icon_w: int = bbox.size.x
			var icon_h: int = bbox.size.y
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
	print("Wrote %d sliced supply/shop props to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
