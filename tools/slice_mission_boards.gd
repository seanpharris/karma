extends SceneTree

# Slice karma_static_mission_boards_atlas (1024x1024, 4x5 = 20 cells).
# Top 2 rows unlabeled; bottom 3 rows have labels under icons. Use a tight
# icon crop to skip labels.

const COLS := 4
const ROWS := 5
const ICON_HEIGHT := 145
const ICON_WIDTH := 200
# Row Y_TOPS: rows 0-1 unlabeled (icon at full cell top); rows 2-4 labeled
# (icon at top of cell, label below).
const ROW_Y_TOPS := [10, 215, 410, 615, 820]

const SOURCE := "res://assets/art/generated/static_event_atlases/karma_static_mission_boards_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/mission_boards"

const NAMES := [
	["adventure_quest_board", "computer_data_board", "danger_skull_gear_board", "ornate_faction_board"],
	["bandage_health_board", "wanted_poster_board", "supply_check_board", "repair_tool_board"],
	["lost_and_found_board", "rumor_board", "posse_recruitment_board", "faction_notice_board"],
	["black_market_coded_board", "law_bulletin_board", "community_vote_board", "delivery_board"],
	["salvage_claim_board", "duel_challenge_board", "apology_board", "warning_board"],
]

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
	var atlas := Image.load_from_file(SOURCE)
	if atlas == null:
		quit(1)
		return
	var col_w: int = atlas.get_width() / COLS
	var x_inset: int = (col_w - ICON_WIDTH) / 2

	var manifest_entries: Array = []
	for row in range(ROWS):
		var y_top: int = ROW_Y_TOPS[row]
		for col in range(COLS):
			var name: String = NAMES[row][col]
			var x: int = col * col_w + x_inset
			var icon := atlas.get_region(Rect2i(x, y_top, ICON_WIDTH, ICON_HEIGHT))
			var out_path := OUT_DIR + "/" + name + ".png"
			if icon.save_png(out_path) != OK:
				continue
			manifest_entries.append({
				"name": name, "path": out_path, "source_atlas": SOURCE,
				"source_region": [x, y_top, ICON_WIDTH, ICON_HEIGHT]
			})
	var manifest_path := OUT_DIR + "/manifest.json"
	var manifest_file := FileAccess.open(manifest_path, FileAccess.WRITE)
	if manifest_file != null:
		manifest_file.store_string(JSON.stringify(manifest_entries, "  "))
		manifest_file.close()
	print("Wrote %d sliced mission boards to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
