extends SceneTree

# Batch-compose every theme bundle JSON under
# assets/art/sprites/lpc/themes/ into Karma atlases under
# assets/art/generated/lpc_npcs/.
#
# Re-runnable. Skips bundles whose Karma atlas already exists unless
# FORCE_REBUILD is true. Emits a summary at the end with how many bundles
# composed successfully vs. failed (most failures are missing LPC layer
# paths in the JSON — fix the path and re-run).
#
# Run:
#   godot --headless --path . --script res://tools/lpc_materialize_theme_bundles.gd
#
# Optional CLI flags:
#   --force        force rebuild every bundle even if its atlas exists
#   --filter <substr>  only process bundles whose id contains <substr>

const THEMES_DIR := "res://assets/art/sprites/lpc/themes/"
const OUT_DIR := "res://assets/art/generated/lpc_npcs/"

const LPC_ROOT := "res://assets/art/sprites/lpc/spritesheets/"
const LPC_FRAME := 64
const LPC_COLS := 9
const LPC_ROWS := 4
const LPC_W := LPC_FRAME * LPC_COLS
const LPC_H := LPC_FRAME * LPC_ROWS

const TARGET_CELL_W := 32
const TARGET_CELL_H := 64
const KARMA_W := TARGET_CELL_W * 8
const KARMA_H := TARGET_CELL_H * 4

const COLUMN_LPC_ROW := [2, 2, 3, 0, 0, 0, 1, 1]
const ROW_LPC_FRAME := [0, 1, 4, 7]
const LAYER_ORDER := [
    "body", "legs", "feet", "torso",
    "waist", "neck",
    "head", "eyes", "facial", "beard", "hair", "hat",
    "shoulders", "arms", "cape", "shield", "weapon", "backpack", "quiver"
]


func _initialize() -> void:
    var force_rebuild := false
    var filter_substr := ""
    var args := OS.get_cmdline_args()
    args.append_array(OS.get_cmdline_user_args())
    for i in range(args.size()):
        if args[i] == "--force":
            force_rebuild = true
        elif args[i] == "--filter" and i + 1 < args.size():
            filter_substr = args[i + 1]

    DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))

    var dir := DirAccess.open(THEMES_DIR)
    if dir == null:
        push_error("Could not open themes directory: %s" % THEMES_DIR)
        quit(1)
        return

    var bundle_files: Array = []
    dir.list_dir_begin()
    while true:
        var entry := dir.get_next()
        if entry == "":
            break
        if entry.begins_with("_") or not entry.ends_with(".json"):
            continue
        var bundle_id := entry.get_basename()
        if filter_substr != "" and not bundle_id.contains(filter_substr):
            continue
        bundle_files.append(bundle_id)
    dir.list_dir_end()
    bundle_files.sort()

    var stats := {"composed": 0, "skipped_existing": 0, "failed": 0, "total": bundle_files.size()}
    var failed_bundles: Array[String] = []

    for bundle_id in bundle_files:
        var karma_out: String = OUT_DIR + bundle_id + "_32x64_8dir_4row.png"
        if not force_rebuild and FileAccess.file_exists(karma_out):
            stats.skipped_existing += 1
            continue
        var ok := compose_bundle(bundle_id)
        if ok:
            stats.composed += 1
        else:
            stats.failed += 1
            failed_bundles.append(bundle_id)

    print("=" .repeat(60))
    print("Bundle materialization summary:")
    print("  total:   %d" % stats.total)
    print("  composed:%d" % stats.composed)
    print("  skipped: %d (already exist; pass --force to rebuild)" % stats.skipped_existing)
    print("  failed:  %d" % stats.failed)
    if not failed_bundles.is_empty():
        print("  failed bundles:")
        for b in failed_bundles:
            print("    - %s" % b)
    quit(0 if stats.failed == 0 else 2)


func compose_bundle(bundle_id: String) -> bool:
    var bundle_path := THEMES_DIR + bundle_id + ".json"
    var bundle_text := FileAccess.get_file_as_string(bundle_path)
    var json := JSON.new()
    if json.parse(bundle_text) != OK:
        push_error("Failed to parse bundle JSON: %s" % bundle_path)
        return false
    var bundle: Dictionary = json.data
    var ordered := order_layers(bundle.get("layers", {}))
    var tints: Dictionary = bundle.get("tints", {})
    if ordered.is_empty():
        push_error("Bundle has no layers: %s" % bundle_id)
        return false
    var lpc_sheet := composite_layers(ordered, tints)
    if lpc_sheet == null:
        push_error("No usable layers loaded: %s" % bundle_id)
        return false

    var lpc_out := OUT_DIR + bundle_id + "_lpc_walk.png"
    if lpc_sheet.save_png(lpc_out) != OK:
        push_error("Could not save LPC sheet: %s" % lpc_out)
        return false

    var karma_sheet := build_karma_sheet(lpc_sheet)
    var karma_out := OUT_DIR + bundle_id + "_32x64_8dir_4row.png"
    if karma_sheet.save_png(karma_out) != OK:
        push_error("Could not save Karma atlas: %s" % karma_out)
        return false

    print("✓ %s" % bundle_id)
    return true


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


func composite_layers(layers: Array, tints: Dictionary) -> Image:
    var base: Image = null
    for entry in layers:
        var layer_key: String = entry[0]
        var rel_png_path: String = entry[1]
        if rel_png_path.is_empty():
            continue
        var sheet_path: String = LPC_ROOT + rel_png_path
        if not FileAccess.file_exists(sheet_path):
            continue
        var img := Image.load_from_file(sheet_path)
        if img == null:
            continue
        if img.get_width() != LPC_W or img.get_height() != LPC_H:
            continue
        if img.get_format() != Image.FORMAT_RGBA8:
            img.convert(Image.FORMAT_RGBA8)
        if tints.has(layer_key):
            colorize_layer(img, Color.html(str(tints[layer_key])))
        if base == null:
            base = Image.create_empty(LPC_W, LPC_H, false, Image.FORMAT_RGBA8)
            base.fill(Color(0, 0, 0, 0))
        base.blend_rect(img, Rect2i(0, 0, LPC_W, LPC_H), Vector2i.ZERO)
    return base


func colorize_layer(img: Image, tint: Color) -> void:
    for y in range(img.get_height()):
        for x in range(img.get_width()):
            var pixel := img.get_pixel(x, y)
            if pixel.a <= 0.04:
                continue
            var luminance: float = pixel.r * 0.299 + pixel.g * 0.587 + pixel.b * 0.114
            var shade: float = clamp(0.42 + luminance * 0.95, 0.0, 1.25)
            img.set_pixel(
                x,
                y,
                Color(
                    min(tint.r * shade, 1.0),
                    min(tint.g * shade, 1.0),
                    min(tint.b * shade, 1.0),
                    pixel.a
                )
            )


func build_karma_sheet(lpc_sheet: Image) -> Image:
    var out := Image.create_empty(KARMA_W, KARMA_H, false, Image.FORMAT_RGBA8)
    out.fill(Color(0, 0, 0, 0))
    for column in range(8):
        var lpc_row: int = COLUMN_LPC_ROW[column]
        for row in range(4):
            var frame_index: int = ROW_LPC_FRAME[row]
            var src := Rect2i(frame_index * LPC_FRAME, lpc_row * LPC_FRAME, LPC_FRAME, LPC_FRAME)
            var fitted := fit_lpc_cell(lpc_sheet, src)
            out.blend_rect(fitted, Rect2i(0, 0, TARGET_CELL_W, TARGET_CELL_H), Vector2i(column * TARGET_CELL_W, row * TARGET_CELL_H))
    return out


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
