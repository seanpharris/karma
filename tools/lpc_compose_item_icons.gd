extends SceneTree

# Compose 32×32 medieval item thumbnails from LPC weapon/armor/shield
# sheets for the inventory + vendor + hotbar UI.
#
# Each entry below names a single LPC sheet and a (row, col) cell within
# it. The script:
#   1. Loads the sheet.
#   2. Extracts the named 64×64 cell.
#   3. Finds the non-transparent bounding box.
#   4. Pads + scales the bbox to fit a 32×32 canvas (nearest-neighbour
#      upscale to keep pixel-art crisp; downscale only if the source is
#      larger than 32×32 in either dim).
#   5. Centers in the canvas + writes
#      assets/art/themes/medieval/items/<item_id>.png
#
# Skipped items use the existing sci-fi PrototypeSpriteCatalog atlas as
# a fallback (no medieval re-skin yet — handle in a follow-up).
#
# Run via:
#   godot --headless --path . --script res://tools/lpc_compose_item_icons.gd

const LPC_ROOT := "res://assets/art/sprites/spritesheets/"
const OUT_DIR := "res://assets/art/themes/medieval/items/"
const CELL := 64
const TARGET := 32
const ICON_PADDING := 1

# Each entry maps a Karma item id to a single LPC sheet + cell coord.
# Naming: most LPC walk sheets are 576×256 = 9 cols × 4 rows of 64×64
# cells. Rows: 0 up, 1 left, 2 down, 3 right. Frame 0 is usually the
# cleanest "idle" pose. Picked left-facing (row 1) for most so weapons
# show in profile.
# Each entry maps a Karma item id to an LPC sheet. The composer scans
# every 64×64 cell in the sheet and picks the one with the most opaque
# pixels — robust to LPC's varying row layouts (some sheets put the
# left-facing pose on row 1, others on row 2). Set "cell" to override
# auto-pick with an explicit (row, col).
const ITEM_SOURCES := [
    # Weapons — direct medieval analogues
    { "id": "stun_baton", "src": "weapon/blunt/club/club.png" },
    { "id": "long_sword", "src": "weapon/sword/longsword/walk/longsword.png" },
    { "id": "short_bow", "src": "weapon/ranged/bow/normal/walk/foreground.png" },
    { "id": "crossbow", "src": "weapon/ranged/crossbow/walk/crossbow.png" },
    # Sci-fi → medieval re-skin (best-effort visual match)
    { "id": "electro_pistol", "src": "weapon/sword/dagger/walk/dagger.png" },
    { "id": "smg_11", "src": "weapon/sword/dagger/walk/dagger.png" },
    { "id": "shotgun_mk1", "src": "weapon/blunt/waraxe/walk/waraxe.png" },
    { "id": "rifle_27", "src": "weapon/ranged/crossbow/walk/crossbow.png" },
    { "id": "sniper_x9", "src": "weapon/ranged/bow/normal/walk/foreground.png" },
    { "id": "plasma_cutter", "src": "weapon/sword/longsword/walk/longsword.png" },
    { "id": "flamethrower", "src": "weapon/blunt/club/club.png" },
    { "id": "grenade_launcher", "src": "weapon/blunt/waraxe/walk/waraxe.png" },
    { "id": "railgun", "src": "weapon/ranged/crossbow/walk/crossbow.png" },
    { "id": "impact_mine", "src": "weapon/blunt/waraxe/walk/waraxe.png" },
    { "id": "emp_grenade", "src": "weapon/blunt/club/club.png" },
    # Shields — re-skinned to medieval shields. work_vest is "armor"
    # but a heater shield is a clean iconic match for "you carry
    # something protective".
    { "id": "work_vest", "src": "shield/heater/original/wood/fg/walk/oak.png" },
    { "id": "portable_shield", "src": "shield/spartan/fg/walk/spartan.png" },

    # Tools — re-skinned to medieval working implements
    { "id": "repair_kit", "src": "tools/smash/foreground/hammer.png" },
    { "id": "welding_torch", "src": "weapon/magic/wand/male/slash/wand.png" },
    { "id": "multi_tool", "src": "tools/smash/foreground/pickaxe.png" },
    { "id": "medi_patch", "src": "backpack/basket_contents/ore/fg/walk/silver.png" },
    { "id": "lockpick_set", "src": "weapon/sword/dagger/walk/dagger.png" },
    { "id": "flashlight", "src": "weapon/magic/wand/male/slash/wand.png" },
    { "id": "hacking_device", "src": "backpack/basket_contents/wood/fg/walk/3_logs.png" },
    { "id": "scanner", "src": "backpack/squarepack/male/walk/leather.png" },
    { "id": "grappling_hook", "src": "tools/rod/foreground/rod.png" },
    { "id": "chem_injector", "src": "weapon/magic/wand/male/slash/wand.png" },
    { "id": "power_cell", "src": "backpack/basket_contents/ore/fg/walk/copper.png" },
    { "id": "bolt_cutters", "src": "tools/smash/foreground/axe.png" },
    { "id": "magnetic_grabber", "src": "tools/rod/foreground/rod.png" },

    # Consumables / ammo — re-skinned to medieval food, arrows, potions
    { "id": "ration_pack", "src": "backpack/basket_contents/wood/fg/walk/3_logs.png" },
    { "id": "ballistic_round", "src": "weapon/ranged/bow/arrow/shoot/arrow.png" },
    { "id": "energy_cell", "src": "backpack/basket_contents/ore/fg/walk/gold.png" },
    { "id": "stim_spike", "src": "weapon/magic/wand/male/slash/wand.png" },
    { "id": "downer_haze", "src": "weapon/magic/wand/male/slash/wand.png" },
    { "id": "tremor_tab", "src": "weapon/magic/wand/male/slash/wand.png" },

    # InteractibleObject — re-skinned to medieval relics + parcels
    { "id": "data_chip", "src": "backpack/basket_contents/ore/fg/walk/silver.png" },
    { "id": "filter_core", "src": "backpack/basket_contents/ore/fg/walk/iron.png" },
    { "id": "contraband_package", "src": "backpack/squarepack/male/walk/maroon.png" },
    { "id": "portable_terminal", "src": "tools/smash/foreground/hammer.png" },

    # Cosmetic / oddity — quirky placeholders. Re-skinned to misc
    # objects until bespoke art lands.
    { "id": "whoopie_cushion", "src": "cape/solid/male/walk/red.png" },
    { "id": "deflated_balloon", "src": "cape/solid/male/walk/lavender.png" },
    { "id": "apology_flower", "src": "weapon/magic/wand/male/slash/wand.png" },

    # Misc
    { "id": "backpack_brown", "src": "backpack/backpack/male/walk/walnut.png" },
]

func _init() -> void:
    var dir := DirAccess.open("res://")
    if dir == null:
        push_error("lpc_compose_item_icons: cannot open project root")
        quit(1)
        return
    dir.make_dir_recursive(OUT_DIR.replace("res://", ""))

    var ok := 0
    var skipped := 0
    var failed := 0
    for entry in ITEM_SOURCES:
        var item_id: String = entry["id"]
        var src: String = entry["src"]
        var override_row: int = entry.get("row", -1)
        var override_col: int = entry.get("col", -1)
        var result := _compose(item_id, src, override_row, override_col)
        match result:
            "ok": ok += 1
            "skipped": skipped += 1
            "failed": failed += 1

    print("lpc_compose_item_icons: ok=%d skipped=%d failed=%d" % [ok, skipped, failed])
    quit(0)

func _compose(item_id: String, src: String, override_row: int, override_col: int) -> String:
    var sheet_path := LPC_ROOT + src
    if not FileAccess.file_exists(sheet_path):
        push_warning("lpc_compose_item_icons: source missing for %s at %s" % [item_id, src])
        return "skipped"

    var sheet: Image = Image.load_from_file(sheet_path)
    if sheet == null:
        push_warning("lpc_compose_item_icons: failed to load %s" % sheet_path)
        return "skipped"
    if sheet.get_format() != Image.FORMAT_RGBA8:
        sheet.convert(Image.FORMAT_RGBA8)

    var pick: Vector2i
    if override_row >= 0 and override_col >= 0:
        pick = Vector2i(override_col, override_row)
    else:
        pick = _pick_densest_cell(sheet)
    if pick.x < 0:
        push_warning("lpc_compose_item_icons: no opaque cell found for %s in %s" % [item_id, src])
        return "skipped"

    var src_rect := Rect2i(pick.x * CELL, pick.y * CELL, CELL, CELL)
    if not _src_in_bounds(sheet, src_rect):
        push_warning("lpc_compose_item_icons: cell out of bounds for %s (%d,%d) in %dx%d sheet" %
            [item_id, pick.y, pick.x, sheet.get_width(), sheet.get_height()])
        return "skipped"

    var cell := Image.create(CELL, CELL, false, Image.FORMAT_RGBA8)
    cell.fill(Color(0, 0, 0, 0))
    cell.blend_rect(sheet, src_rect, Vector2i(0, 0))

    var bbox := _opaque_bbox(cell)
    if bbox.size.x == 0 or bbox.size.y == 0:
        push_warning("lpc_compose_item_icons: empty cell for %s in %s row=%d col=%d" %
            [item_id, src, pick.y, pick.x])
        return "skipped"

    bbox = _expand_with_padding(bbox, ICON_PADDING, cell.get_size())
    var cropped := cell.get_region(bbox)
    var canvas := _scale_into_target(cropped)
    var out_path := OUT_DIR + item_id + ".png"
    var save_err := canvas.save_png(out_path)
    if save_err != OK:
        push_error("lpc_compose_item_icons: save_png failed for %s (%d)" % [item_id, save_err])
        return "failed"
    return "ok"

# Scan every 64×64 cell in the sheet, return the (col, row) of the cell
# with the most opaque pixels. Returns Vector2i(-1, -1) if the entire
# sheet is transparent. Cells with width or height that don't fit a
# whole CELL count are skipped.
func _pick_densest_cell(sheet: Image) -> Vector2i:
    var cols := int(sheet.get_width() / CELL)
    var rows := int(sheet.get_height() / CELL)
    var best := Vector2i(-1, -1)
    var best_count := 0
    for r in range(rows):
        for c in range(cols):
            var count := _count_opaque(sheet, c * CELL, r * CELL, CELL, CELL)
            if count > best_count:
                best_count = count
                best = Vector2i(c, r)
    return best

func _count_opaque(image: Image, x0: int, y0: int, w: int, h: int) -> int:
    var count := 0
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            if image.get_pixel(x, y).a > 0.05:
                count += 1
    return count

func _src_in_bounds(image: Image, rect: Rect2i) -> bool:
    return rect.position.x >= 0 and rect.position.y >= 0 \
        and rect.position.x + rect.size.x <= image.get_width() \
        and rect.position.y + rect.size.y <= image.get_height()

func _opaque_bbox(image: Image) -> Rect2i:
    var width := image.get_width()
    var height := image.get_height()
    var min_x := width
    var min_y := height
    var max_x := -1
    var max_y := -1
    for y in range(height):
        for x in range(width):
            var c := image.get_pixel(x, y)
            if c.a > 0.05:
                if x < min_x: min_x = x
                if y < min_y: min_y = y
                if x > max_x: max_x = x
                if y > max_y: max_y = y
    if max_x < 0:
        return Rect2i(0, 0, 0, 0)
    return Rect2i(min_x, min_y, max_x - min_x + 1, max_y - min_y + 1)

func _expand_with_padding(rect: Rect2i, padding: int, max_size: Vector2i) -> Rect2i:
    var x: int = max(0, rect.position.x - padding)
    var y: int = max(0, rect.position.y - padding)
    var w_max: int = min(max_size.x - x, rect.size.x + 2 * padding)
    var h_max: int = min(max_size.y - y, rect.size.y + 2 * padding)
    return Rect2i(x, y, w_max, h_max)

# Scale the cropped bbox to fit a 32×32 canvas. If the source is smaller
# than TARGET in both dims, upscale (nearest-neighbour, integer factor
# when possible). If it's larger, downscale to fit. Center the result.
func _scale_into_target(cropped: Image) -> Image:
    var w := cropped.get_width()
    var h := cropped.get_height()
    var max_dim: int = max(w, h)
    var scaled: Image = cropped.duplicate()

    if max_dim != TARGET:
        var scale_factor: float = float(TARGET) / float(max_dim)
        var new_w: int = max(1, int(round(w * scale_factor)))
        var new_h: int = max(1, int(round(h * scale_factor)))
        # Clamp to TARGET in each dim (rounding can push slightly over).
        new_w = min(new_w, TARGET)
        new_h = min(new_h, TARGET)
        scaled.resize(new_w, new_h, Image.INTERPOLATE_NEAREST)

    var canvas := Image.create(TARGET, TARGET, false, Image.FORMAT_RGBA8)
    canvas.fill(Color(0, 0, 0, 0))
    var sw := scaled.get_width()
    var sh := scaled.get_height()
    var ox: int = int((TARGET - sw) / 2)
    var oy: int = int((TARGET - sh) / 2)
    canvas.blend_rect(scaled, Rect2i(0, 0, sw, sh), Vector2i(ox, oy))
    return canvas
