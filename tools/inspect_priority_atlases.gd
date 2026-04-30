extends SceneTree

# One-shot diagnostic: load each priority + key static-event atlas and print
# its pixel dimensions so the slicing script can use accurate cell sizes.

const ATLASES := [
	"res://assets/art/generated/priority_static_atlases/karma_priority_wanted_bounty_law_atlas.jpg",
	"res://assets/art/generated/priority_static_atlases/karma_priority_clinic_rescue_revive_atlas.jpg",
	"res://assets/art/generated/priority_static_atlases/karma_priority_supply_shop_loot_atlas.jpg",
	"res://assets/art/generated/priority_static_atlases/karma_priority_player_interactions_atlas.jpg",
	"res://assets/art/generated/priority_static_atlases/karma_priority_structure_world_state_atlas.jpg",
	"res://assets/art/generated/priority_static_atlases/karma_priority_prototype_ui_icons_atlas.jpg",
	"res://assets/art/generated/priority_static_atlases/karma_priority_location_exteriors_atlas.jpg",
	"res://assets/art/generated/priority_static_atlases/karma_priority_theme_variant_matrix_atlas.jpg",
	"res://assets/art/generated/static_event_atlases/karma_static_ui_status_icons_atlas.jpg",
	"res://assets/art/generated/static_event_atlases/karma_static_event_markers_atlas.jpg",
	"res://assets/art/generated/static_event_atlases/karma_static_modular_walls_doors_atlas.jpg",
	"res://assets/art/generated/static_event_atlases/karma_static_interior_furniture_atlas.jpg",
	"res://assets/art/generated/static_event_atlases/karma_static_crafting_stations_atlas.jpg",
	"res://assets/art/generated/static_event_atlases/karma_static_containers_loot_atlas.jpg",
	"res://assets/art/generated/static_event_atlases/karma_static_evidence_clues_atlas.jpg",
	"res://assets/art/generated/static_event_atlases/karma_static_hazards_disasters_atlas.jpg",
	"res://assets/art/generated/static_event_atlases/karma_static_player_interaction_props_atlas.jpg",
	"res://assets/art/generated/static_event_atlases/karma_static_faction_reputation_symbols_atlas.jpg",
	"res://assets/art/generated/static_event_atlases/karma_static_mission_boards_atlas.jpg",
]

func _initialize() -> void:
	for path in ATLASES:
		var img := Image.load_from_file(path)
		if img == null:
			push_warning("Could not load: " + path)
			continue
		var file_name: String = path.get_file()
		print("%s | %dx%d" % [file_name, img.get_width(), img.get_height()])
	quit(0)
