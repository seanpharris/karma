extends SceneTree

# Slice the karma_priority_prototype_ui_icons_atlas (1024x1024, 6x6 = 36
# labeled event icons) into individual transparent-bg PNGs.
#
# The atlas was NOT laid out on a strict uniform grid: row 0 has a 2-line
# label ("INTERACT KEY PROMPT", "SCENARIO COMPLETE", "SERVER EVENT LOG",
# "MATCH SUMMARY") that shifts row 1's icon down. Rows 2 and 3 also have
# multi-line labels ("SUPPLY CLAIMED", "CONTRABAND DETECTED", "DUEL
# REQUESTED", "DUEL ACCEPTED", "PLAYER DOWNED", "PLAYER RESCUED",
# "STRUCTURE INTERACTED", "POSSE INVITE", "POSSE ACCEPTED", "QUEST
# COMPLETED").
#
# Empirically-measured icon-area Y offsets from a vertical strip render:
const ROW_Y_TOPS := [10, 200, 365, 545, 705, 880]
const ICON_HEIGHT := 115
const ICON_WIDTH := 115
const COLS := 6

const SOURCE := "res://assets/art/generated/priority_static_atlases/karma_priority_prototype_ui_icons_atlas.jpg"
const OUT_DIR := "res://assets/art/generated/sliced/prototype_ui_icons"

const NAMES := [
	["interact_key_prompt", "objective_arrow", "scenario_start", "scenario_complete", "server_event_log", "match_summary"],
	["ready_up", "match_started", "wanted", "bounty_claimed", "contraband_detected", "supply_spawned"],
	["supply_claimed", "clinic_revive", "duel_requested", "duel_accepted", "player_downed", "player_rescued"],
	["karma_break", "item_purchased", "item_used", "structure_interacted", "posse_invite", "posse_accepted"],
	["local_chat", "mount", "dismount", "quest_started", "quest_completed", "dialogue"],
	["entanglement", "rumor", "witness", "evidence", "danger_heat", "restart"],
]

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
	var atlas := Image.load_from_file(SOURCE)
	if atlas == null:
		push_error("Could not load atlas: " + SOURCE)
		quit(1)
		return
	var col_w: int = atlas.get_width() / COLS
	# Centre the icon crop horizontally inside each column.
	var x_inset: int = (col_w - ICON_WIDTH) / 2

	var manifest_entries: Array = []
	for row in range(ROW_Y_TOPS.size()):
		var y_top: int = ROW_Y_TOPS[row]
		for col in range(COLS):
			var name: String = NAMES[row][col]
			var x: int = col * col_w + x_inset
			var icon := atlas.get_region(Rect2i(x, y_top, ICON_WIDTH, ICON_HEIGHT))
			var out_path := OUT_DIR + "/" + name + ".png"
			var save_err := icon.save_png(out_path)
			if save_err != OK:
				push_warning("Failed to save: " + out_path)
				continue
			manifest_entries.append({
				"name": name,
				"path": out_path,
				"source_atlas": SOURCE,
				"source_region": [x, y_top, ICON_WIDTH, ICON_HEIGHT]
			})

	var manifest_path := OUT_DIR + "/manifest.json"
	var manifest_file := FileAccess.open(manifest_path, FileAccess.WRITE)
	if manifest_file == null:
		push_error("Could not write manifest")
		quit(1)
		return
	manifest_file.store_string(JSON.stringify(manifest_entries, "  "))
	manifest_file.close()
	print("Wrote %d sliced icons + manifest to %s" % [manifest_entries.size(), OUT_DIR])
	quit(0)
