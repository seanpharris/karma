extends SceneTree

# Slice karma_priority_supply_shop_loot_atlas (1024x1024, 5x5 = 25 cells,
# no labels, uniform grid). Step 30 supply drop + shop UX prop set.
#
# Some atlas rows have 4 visible items + 1 empty leftmost cell; we slice
# uniformly and the empty cells produce mostly-blank PNGs that can be
# pruned manually.

const COLS := 5
const ROWS := 5
const ICON_PORTION := 0.93

const SOURCE := "res://assets/art/generated/priority_static_atlases/karma_priority_supply_shop_loot_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/supply_shop_loot"

const NAMES := [
	["supply_drop_parachute", "barrel_metal", "bedroll_blanket_pack", "trade_table_scales", "_blank_0_4"],
	["shop_kiosk", "shop_shelves", "supply_tent", "locked_metal_chest", "_blank_1_4"],
	["wood_chest_open", "loot_crate_stamped", "backpack_brown", "wrapped_parcel", "_blank_2_4"],
	["ammo_crate_metal", "medical_crate_red_cross", "weapon_case_long", "tool_box_open", "_blank_3_4"],
	["money_sack", "cluttered_loot_pile", "shop_signpost_blank", "discount_tag", "scrap_metal_pile"],
]

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
	var atlas := Image.load_from_file(SOURCE)
	if atlas == null:
		push_error("Could not load atlas: " + SOURCE)
		quit(1)
		return
	var cell_w: int = atlas.get_width() / COLS
	var cell_h: int = atlas.get_height() / ROWS
	var icon_w: int = int(cell_w * ICON_PORTION)
	var icon_h: int = int(cell_h * ICON_PORTION)
	var x_inset: int = (cell_w - icon_w) / 2
	var y_inset: int = (cell_h - icon_h) / 2

	var manifest_entries: Array = []
	for row in range(ROWS):
		for col in range(COLS):
			var name: String = NAMES[row][col]
			if name.begins_with("_blank_"):
				continue
			var x: int = col * cell_w + x_inset
			var y: int = row * cell_h + y_inset
			var icon := atlas.get_region(Rect2i(x, y, icon_w, icon_h))
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
