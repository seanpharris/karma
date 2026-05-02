extends SceneTree

# Compose a random LPC character into Karma's player atlas format.
#
# Reads variant layers from assets/art/sprites/lpc/spritesheets/, picks a
# random body + clothing/hair stack, composes them into the LPC native
# 576x256 sheet (9 cols x 4 rows x 64x64), and re-cells into the existing
# Karma 32x64 8-dir 4-row contract (256x256). LPC is body-relative
# 4-direction; diagonals reuse the closest cardinal verbatim.
#
# Run:
#   godot --headless --path . --script res://tools/lpc_compose_random.gd
#
# Optional CLI arg: --seed=NNN (Godot doesn't make this easy; use the
# SEED_SALT constant below or edit the script).

const LPC_ROOT := "res://assets/art/sprites/lpc/spritesheets/"
const OUT_LPC := "res://assets/art/generated/lpc/random_character_lpc_walk.png"
const OUT_KARMA := "res://assets/art/generated/lpc/random_character_32x64_8dir_4row.png"

const SEED_SALT := 42

const LPC_FRAME := 64
const LPC_COLS := 9
const LPC_ROWS := 4
const LPC_W := LPC_FRAME * LPC_COLS    # 576
const LPC_H := LPC_FRAME * LPC_ROWS    # 256

const TARGET_CELL_W := 32
const TARGET_CELL_H := 64
const KARMA_W := TARGET_CELL_W * 8     # 256
const KARMA_H := TARGET_CELL_H * 4     # 256

# Karma column → LPC row. LPC rows: 0 up, 1 left, 2 down, 3 right.
const COLUMN_LPC_ROW := [
	2, # south
	2, # south-east  (reuse south)
	3, # east
	0, # north-east  (reuse north)
	0, # north
	0, # north-west  (reuse north)
	1, # west
	1, # south-west  (reuse west)
]

# Karma row → LPC frame index in the 9-frame walk cycle.
# row 0: idle (frame 0), rows 1-3: stride frames evenly spaced.
const ROW_LPC_FRAME := [0, 1, 4, 7]

# LPC layer roots to randomly pick from. Each entry: (label, list of root paths
# under spritesheets/). The first matching subfolder (any depth) that has
# walk.png becomes a candidate.
const BODY_KIND := "male"

# Fallback recursive-walk depth so we don't blow up on huge dirs.
const MAX_WALK_DEPTH := 8

func _initialize() -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = SEED_SALT
	var picks := pick_random_layers(rng, BODY_KIND)
	if picks.is_empty():
		push_error("Could not pick any layers")
		quit(1)
		return
	print("Picked layers:")
	for entry in picks:
		print("  %-8s %s" % [entry[0], entry[1]])

	var lpc_sheet := composite_layers(picks)
	if lpc_sheet == null:
		push_error("No usable layers for animation 'walk'")
		quit(1)
		return

	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_LPC.get_base_dir()))
	if lpc_sheet.save_png(OUT_LPC) != OK:
		push_error("Could not save LPC composite to %s" % OUT_LPC)
		quit(1)
		return
	print("wrote LPC composite: %s" % OUT_LPC)

	var karma_sheet := build_karma_sheet(lpc_sheet)
	if karma_sheet.save_png(OUT_KARMA) != OK:
		push_error("Could not save Karma atlas to %s" % OUT_KARMA)
		quit(1)
		return
	print("wrote Karma atlas: %s" % OUT_KARMA)

	quit(0)


func pick_random_layers(rng: RandomNumberGenerator, body_kind: String) -> Array:
	# Each pick is [label, full_walk_png_path_under_LPC_ROOT]. Order matters:
	# layers are blended later-on-top, so place body first, hair before
	# weapon, etc. LPC z-order convention:
	#   body (10) → legs/feet/torso (35-50) → head (80) → eyes (90) →
	#   hair (120) → weapon (140).
	var picks: Array = []
	var body_walk := "body/bodies/%s/walk.png" % body_kind
	if FileAccess.file_exists(LPC_ROOT + body_walk):
		picks.append(["body", body_walk])

	# Body+clothing: <variant>/walk.png convention. Drawn over the body.
	var body_buckets := {
		"legs": ["legs/pants", "legs/shorts", "legs/skirts"],
		"feet": ["feet/shoes", "feet/boots"],
		"torso": ["torso/clothes", "torso/jacket", "torso/jacket_collared"],
	}
	for label in body_buckets:
		var roots: Array = body_buckets[label]
		var candidates: Array = []
		for root in roots:
			collect_named_animation(LPC_ROOT + root, "", "walk.png", candidates, 0)
		if candidates.is_empty():
			print("  no candidates for %s" % label)
			continue
		var idx := rng.randi_range(0, candidates.size() - 1)
		picks.append([label, candidates[idx]])

	# Head + eyes: pin to a body-kind-matched human head with default eyes so
	# the character actually has a face. Fall back to "male" if the requested
	# kind doesn't have a head sheet (e.g. muscular reuses male).
	var head_walk := "head/heads/human/%s/walk.png" % body_kind
	if not FileAccess.file_exists(LPC_ROOT + head_walk):
		head_walk = "head/heads/human/male/walk.png"
	if FileAccess.file_exists(LPC_ROOT + head_walk):
		picks.append(["head", head_walk])

	var eyes_walk := "eyes/human/adult/default/walk.png"
	if FileAccess.file_exists(LPC_ROOT + eyes_walk):
		picks.append(["eyes", eyes_walk])

	# Hair on top of head.
	var hair_candidates: Array = []
	for hair_root in ["hair/short", "hair/long", "hair/messy"]:
		collect_named_animation(LPC_ROOT + hair_root, "", "walk.png", hair_candidates, 0)
	if not hair_candidates.is_empty():
		var idx := rng.randi_range(0, hair_candidates.size() - 1)
		picks.append(["hair", hair_candidates[idx]])
	else:
		print("  no candidates for hair")

	# Weapon on top of everything: weapon/<class>/<name>/walk/<name>.png.
	var weapon_candidates: Array = []
	collect_weapon_walks(LPC_ROOT + "weapon", "", weapon_candidates, 0)
	if not weapon_candidates.is_empty():
		var idx := rng.randi_range(0, weapon_candidates.size() - 1)
		picks.append(["weapon", weapon_candidates[idx]])
	else:
		print("  no candidates for weapon")
	return picks


# Recurse through `abs_root` looking for any subdirectory that contains a file
# named exactly `target_filename`. Each match is appended as a path relative to
# LPC_ROOT (with `/<target_filename>` suffix so callers get the full asset path).
func collect_named_animation(abs_root: String, rel_root: String, target_filename: String, out: Array, depth: int) -> void:
	if depth > MAX_WALK_DEPTH:
		return
	var probe_dir := abs_root if rel_root == "" else (abs_root + "/" + rel_root)
	var dir := DirAccess.open(probe_dir)
	if dir == null:
		return
	dir.list_dir_begin()
	while true:
		var entry := dir.get_next()
		if entry == "":
			break
		if entry == "." or entry == "..":
			continue
		var rel := (rel_root + "/" + entry) if rel_root != "" else entry
		if dir.current_is_dir():
			var probe := abs_root + "/" + rel + "/" + target_filename
			if FileAccess.file_exists(probe):
				var rel_to_root := abs_root.replace(LPC_ROOT, "").strip_edges() + "/" + rel + "/" + target_filename
				out.append(rel_to_root)
			collect_named_animation(abs_root, rel, target_filename, out, depth + 1)
	dir.list_dir_end()


# Recurse through `abs_root` (typically `<LPC_ROOT>/weapon`) looking for any
# `<...>/walk/<file>.png` and emit one entry per file found. The "walk" segment
# in a weapon path indicates the animation context, mirroring the LPC convention.
func collect_weapon_walks(abs_root: String, rel_root: String, out: Array, depth: int) -> void:
	if depth > MAX_WALK_DEPTH:
		return
	var probe_dir := abs_root if rel_root == "" else (abs_root + "/" + rel_root)
	var dir := DirAccess.open(probe_dir)
	if dir == null:
		return
	dir.list_dir_begin()
	while true:
		var entry := dir.get_next()
		if entry == "":
			break
		if entry == "." or entry == "..":
			continue
		var rel := (rel_root + "/" + entry) if rel_root != "" else entry
		if dir.current_is_dir():
			if entry == "walk":
				# Emit each PNG inside this walk folder.
				var walk_dir := DirAccess.open(abs_root + "/" + rel)
				if walk_dir != null:
					walk_dir.list_dir_begin()
					while true:
						var walk_entry := walk_dir.get_next()
						if walk_entry == "":
							break
						if walk_entry == "." or walk_entry == "..":
							continue
						if not walk_dir.current_is_dir() and walk_entry.ends_with(".png"):
							var rel_to_root := abs_root.replace(LPC_ROOT, "").strip_edges() + "/" + rel + "/" + walk_entry
							out.append(rel_to_root)
					walk_dir.list_dir_end()
			else:
				collect_weapon_walks(abs_root, rel, out, depth + 1)
	dir.list_dir_end()


func composite_layers(layers: Array) -> Image:
	var base: Image = null
	for entry in layers:
		var label: String = entry[0]
		var rel_png_path: String = entry[1]
		var sheet_path: String = LPC_ROOT + rel_png_path
		if not FileAccess.file_exists(sheet_path):
			print("  skip %s: %s not found" % [label, rel_png_path])
			continue
		var img := Image.load_from_file(sheet_path)
		if img == null:
			print("  skip %s: failed to load %s" % [label, sheet_path])
			continue
		if img.get_width() != LPC_W or img.get_height() != LPC_H:
			print("  skip %s: dimension mismatch %sx%s" % [label, img.get_width(), img.get_height()])
			continue
		if img.get_format() != Image.FORMAT_RGBA8:
			img.convert(Image.FORMAT_RGBA8)
		if base == null:
			base = Image.create_empty(LPC_W, LPC_H, false, Image.FORMAT_RGBA8)
			base.fill(Color(0, 0, 0, 0))
		base.blend_rect(img, Rect2i(0, 0, LPC_W, LPC_H), Vector2i.ZERO)
		print("  layered %s :: %s" % [label, rel_png_path])
	return base


func build_karma_sheet(lpc_sheet: Image) -> Image:
	var out := Image.create_empty(KARMA_W, KARMA_H, false, Image.FORMAT_RGBA8)
	out.fill(Color(0, 0, 0, 0))
	for column in range(8):
		var lpc_row: int = COLUMN_LPC_ROW[column]
		for row in range(4):
			var frame_index: int = ROW_LPC_FRAME[row]
			var src_rect := Rect2i(frame_index * LPC_FRAME, lpc_row * LPC_FRAME, LPC_FRAME, LPC_FRAME)
			var fitted := fit_lpc_cell(lpc_sheet, src_rect)
			out.blend_rect(fitted, Rect2i(0, 0, TARGET_CELL_W, TARGET_CELL_H), Vector2i(column * TARGET_CELL_W, row * TARGET_CELL_H))
	return out


# Fit a 64x64 LPC frame into a 32x64 Karma cell by bbox-cropping the body and
# scaling horizontally. LPC bodies live in the bottom-centre of their cell;
# we keep that vertical anchor.
func fit_lpc_cell(lpc_sheet: Image, src: Rect2i) -> Image:
	var sub := Image.create_empty(LPC_FRAME, LPC_FRAME, false, Image.FORMAT_RGBA8)
	sub.fill(Color(0, 0, 0, 0))
	sub.blit_rect(lpc_sheet, src, Vector2i.ZERO)

	# Compute alpha-bbox.
	var min_x := LPC_FRAME
	var min_y := LPC_FRAME
	var max_x := -1
	var max_y := -1
	for py in range(LPC_FRAME):
		for px in range(LPC_FRAME):
			if sub.get_pixel(px, py).a > 0.04:
				if px < min_x: min_x = px
				if py < min_y: min_y = py
				if px > max_x: max_x = px
				if py > max_y: max_y = py
	var out := Image.create_empty(TARGET_CELL_W, TARGET_CELL_H, false, Image.FORMAT_RGBA8)
	out.fill(Color(0, 0, 0, 0))
	if max_x < 0:
		return out
	var body_w := max_x - min_x + 1
	var body_h := max_y - min_y + 1
	var max_target_w: int = TARGET_CELL_W - 2
	var max_target_h: int = TARGET_CELL_H - 2
	var scale: float = min(float(max_target_w) / body_w, float(max_target_h) / body_h, 1.0)
	var tw: int = max(1, int(round(body_w * scale)))
	var th: int = max(1, int(round(body_h * scale)))
	var cropped := Image.create_empty(body_w, body_h, false, Image.FORMAT_RGBA8)
	cropped.fill(Color(0, 0, 0, 0))
	cropped.blit_rect(sub, Rect2i(min_x, min_y, body_w, body_h), Vector2i.ZERO)
	cropped.resize(tw, th, Image.INTERPOLATE_NEAREST)
	var dest_x: int = (TARGET_CELL_W - tw) / 2
	var dest_y: int = TARGET_CELL_H - th - 2
	out.blend_rect(cropped, Rect2i(0, 0, tw, th), Vector2i(dest_x, dest_y))
	return out
