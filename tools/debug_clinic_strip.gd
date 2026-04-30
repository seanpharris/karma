extends SceneTree

const SOURCE := "res://assets/art/generated/priority_static_atlases/karma_priority_clinic_rescue_revive_atlas.jpg"
const OUT := "res://assets/art/generated/sliced/_debug_clinic_left_column.png"

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT.get_base_dir()))
	var atlas := Image.load_from_file(SOURCE)
	if atlas == null:
		quit(1)
		return
	var cell_w: int = atlas.get_width() / 5
	var strip := atlas.get_region(Rect2i(0, 0, cell_w, atlas.get_height()))
	strip.save_png(OUT)
	print("Saved clinic strip %dx%d (assuming 5 cols)" % [strip.get_width(), strip.get_height()])
	quit(0)
