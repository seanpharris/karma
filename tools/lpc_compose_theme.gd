extends SceneTree

# Compose a single LPC theme bundle (e.g. medieval_warrior_male) into Karma's
# player atlas format.
#
# Reads the JSON bundle at:
#   assets/art/sprites/lpc/themes/<bundle_id>.json
#
# Produces two outputs under assets/art/sprites/themes/medieval/generated/:
#   <bundle_id>_lpc_walk.png             — native 576x256 LPC sheet (9 cols x 4 rows)
#   <bundle_id>_32x64_8dir_4row.png      — Karma 256x256 atlas (8 cols x 4 rows)
#
# Diagonals reuse the closest cardinal verbatim (LPC is body-relative
# 4-direction). Mirrors the pipeline established by tools/lpc_compose_random.gd.
#
# Run via the runner:
#   godot --headless --path . --script res://tools/lpc_compose_theme.gd -- --bundle medieval_warrior_male
#
# Or set BUNDLE_ID below and run the script directly. Useful for one-offs;
# tools/lpc_materialize_theme_bundles.gd batches over every JSON in the
# themes folder.

const LPC_ROOT := "res://assets/art/sprites/spritesheets/"
const THEMES_DIR := "res://assets/art/sprites/themes/"
const OUT_DIR := "res://assets/art/sprites/themes/medieval/generated/"

# Default bundle to compose when no --bundle CLI arg is provided.
const BUNDLE_ID := "medieval_warrior_male"

# LPC z-order — lower priority drawn first, higher on top.
const LAYER_ORDER := [
    "body", "legs", "feet", "torso",
    "head", "eyes", "facial", "beard", "hair", "hat",
    "shoulders", "arms", "cape", "shield", "weapon", "backpack", "quiver"
]

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
const ROW_LPC_FRAME := [0, 1, 4, 7]


func _initialize() -> void:
    var bundle_id := BUNDLE_ID
    var args := OS.get_cmdline_args()
    for i in range(args.size() - 1):
        if args[i] == "--bundle":
            bundle_id = args[i + 1]
            break

    var bundle_path := THEMES_DIR + bundle_id + ".json"
    if not FileAccess.file_exists(bundle_path):
        push_error("Bundle JSON not found: %s" % bundle_path)
        quit(1)
        return

    print("composing bundle: %s" % bundle_id)
    var bundle_text := FileAccess.get_file_as_string(bundle_path)
    var json := JSON.new()
    if json.parse(bundle_text) != OK:
        push_error("Failed to parse bundle JSON: %s" % bundle_path)
        quit(1)
        return
    var bundle: Dictionary = json.data

    var ordered_layers := order_layers(bundle.get("layers", {}))
    if ordered_layers.is_empty():
        push_error("Bundle has no layers: %s" % bundle_id)
        quit(1)
        return
    print("layers in z-order:")
    for entry in ordered_layers:
        print("  %-9s %s" % [entry[0], entry[1]])

    var lpc_sheet := composite_layers(ordered_layers)
    if lpc_sheet == null:
        push_error("No usable layers loaded for bundle: %s" % bundle_id)
        quit(1)
        return

    DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
    var lpc_out := OUT_DIR + bundle_id + "_lpc_walk.png"
    if lpc_sheet.save_png(lpc_out) != OK:
        push_error("Could not save LPC sheet: %s" % lpc_out)
        quit(1)
        return
    print("wrote %s" % lpc_out)

    var karma_sheet := build_karma_sheet(lpc_sheet)
    var karma_out := OUT_DIR + bundle_id + "_32x64_8dir_4row.png"
    if karma_sheet.save_png(karma_out) != OK:
        push_error("Could not save Karma atlas: %s" % karma_out)
        quit(1)
        return
    print("wrote %s" % karma_out)

    quit(0)


# Walk the bundle's layers dict and produce an array of [label, full_path]
# in canonical LPC z-order. Unknown / unordered keys are appended at the end.
func order_layers(layers: Dictionary) -> Array:
    var result: Array = []
    var seen := {}
    for key in LAYER_ORDER:
        if layers.has(key):
            result.append([key, layers[key]])
            seen[key] = true
    for key in layers:
        if not seen.has(key):
            result.append([key, layers[key]])
    return result


func composite_layers(layers: Array) -> Image:
    var base: Image = null
    for entry in layers:
        var label: String = entry[0]
        var rel_png_path: String = entry[1]
        if rel_png_path.is_empty():
            continue
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


# Fit a 64x64 LPC frame into a 32x64 Karma cell by bbox-cropping the body
# and scaling to fit, anchoring the body at the bottom-centre.
func fit_lpc_cell(lpc_sheet: Image, src: Rect2i) -> Image:
    var sub := Image.create_empty(LPC_FRAME, LPC_FRAME, false, Image.FORMAT_RGBA8)
    sub.fill(Color(0, 0, 0, 0))
    sub.blit_rect(lpc_sheet, src, Vector2i.ZERO)

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
    var body_w: int = max_x - min_x + 1
    var body_h: int = max_y - min_y + 1
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
