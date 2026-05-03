extends SceneTree

# Slice karma_priority_player_interactions_atlas (1024x1024, 6x6 = 36 cells,
# no labels). Duel signs, posse banners, contracts, treasure chests, etc.

const COLS := 6
const ROWS := 6
const ICON_PORTION := 0.93

const SOURCE := "res://assets/art/generated/priority_static_atlases/karma_priority_player_interactions_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/player_interactions"

const NAMES := [
	["duel_post_sign", "parade_flags", "gambling_table", "handshake_statue", "manuscript_scroll", "wanted_paper"],
	["gift_box", "handshake_accept", "handshake_decline", "handshake_prohibited", "money_pile", "posse_banner_pole"],
	["posse_banner_hanging", "contracts_pinboard", "chest_open_jewels", "chest_open_weapons", "chest_open_misc", "chest_open_candy"],
	["chest_closed", "treasure_map_a", "treasure_map_b", "gold_medallion", "danger_sign_yellow", "danger_sign_yellow_alt"],
	["ballot_box_wood", "sealed_parchment", "money_envelope", "magic_ritual_circle", "torch_standing", "torch_standing_alt"],
	["torch_on_stick", "hunter_recruit_sign", "potion_chess_table", "casino_chips_dice", "money_sack_tag", "mailbox_lit_red"],
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
			var x: int = col * cell_w + x_inset
			var y: int = row * cell_h + y_inset
			var icon := atlas.get_region(Rect2i(x, y, icon_w, icon_h))
			var out_path := OUT_DIR + "/" + name + ".png"
			var save_err := icon.save_png(out_path)
			if save_err != OK:
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
		quit(1)
		return
	manifest_file.store_string(JSON.stringify(manifest_entries, "  "))
	manifest_file.close()
	print("Wrote %d sliced player-interactions to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
