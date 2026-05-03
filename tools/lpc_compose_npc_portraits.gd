extends SceneTree

# Crop a 32×32 head/shoulders portrait from each LPC NPC bundle atlas.
#
# Source atlases under assets/art/sprites/themes/medieval/generated/<bundle_id>_32x64_8dir_4row.png
# are 256×256: 8 cols × 4 rows of 32×64 cells. Col 0 row 0 = south-
# facing standing frame. The top 32 pixels of that cell is the head +
# shoulders, which is the cleanest portrait crop.
#
# Output: assets/art/themes/medieval/npc_portraits/<bundle_id>.png
#
# Run via:
#   godot --headless --path . --script res://tools/lpc_compose_npc_portraits.gd

const SOURCE_DIR := "res://assets/art/sprites/themes/medieval/generated/"
const OUT_DIR := "res://assets/art/themes/medieval/npc_portraits/"
const BUNDLE_SUFFIX := "_32x64_8dir_4row.png"
const CELL_W := 32
const CELL_H := 64
const PORTRAIT_SIZE := 32

func _init() -> void:
    var dir := DirAccess.open(SOURCE_DIR)
    if dir == null:
        push_error("lpc_compose_npc_portraits: cannot open %s" % SOURCE_DIR)
        quit(1)
        return

    var out_dir := DirAccess.open("res://")
    out_dir.make_dir_recursive(OUT_DIR.replace("res://", ""))

    var ok := 0
    var skipped := 0
    var failed := 0

    dir.list_dir_begin()
    while true:
        var name := dir.get_next()
        if name == "":
            break
        if dir.current_is_dir():
            continue
        if not name.ends_with(BUNDLE_SUFFIX):
            continue

        var bundle_id := name.substr(0, name.length() - BUNDLE_SUFFIX.length())
        var result := _compose(SOURCE_DIR + name, bundle_id)
        match result:
            "ok": ok += 1
            "skipped": skipped += 1
            "failed": failed += 1
    dir.list_dir_end()

    print("lpc_compose_npc_portraits: ok=%d skipped=%d failed=%d" % [ok, skipped, failed])
    quit(0)

func _compose(sheet_path: String, bundle_id: String) -> String:
    var sheet: Image = Image.load_from_file(sheet_path)
    if sheet == null:
        push_warning("lpc_compose_npc_portraits: cannot load %s" % sheet_path)
        return "skipped"
    if sheet.get_format() != Image.FORMAT_RGBA8:
        sheet.convert(Image.FORMAT_RGBA8)

    # Col 0 row 0 = south-facing standing frame. The head sits in the
    # top half of the 32×64 cell; cropping the top 32 rows gives a
    # square head + shoulders portrait.
    var src_rect := Rect2i(0, 0, CELL_W, PORTRAIT_SIZE)
    if sheet.get_width() < src_rect.size.x or sheet.get_height() < src_rect.size.y:
        push_warning("lpc_compose_npc_portraits: sheet too small for %s" % bundle_id)
        return "skipped"

    var portrait := sheet.get_region(src_rect)
    var out_path := OUT_DIR + bundle_id + ".png"
    var save_err := portrait.save_png(out_path)
    if save_err != OK:
        push_error("lpc_compose_npc_portraits: save_png failed for %s (%d)" % [bundle_id, save_err])
        return "failed"
    return "ok"
