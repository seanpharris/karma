extends SceneTree

# Strip the leftmost column of the prototype_ui_icons atlas as a single
# vertical PNG so we can visually measure row boundaries.

const SOURCE := "res://assets/art/generated/priority_static_atlases/karma_priority_prototype_ui_icons_atlas.jpg"
const OUT := "res://assets/art/generated/sliced/_debug_left_column.png"

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT.get_base_dir()))
	var atlas := Image.load_from_file(SOURCE)
	if atlas == null:
		quit(1)
		return
	var cell_w := atlas.get_width() / 6
	var strip := atlas.get_region(Rect2i(0, 0, cell_w, atlas.get_height()))
	strip.save_png(OUT)
	print("Saved strip %dx%d to %s" % [strip.get_width(), strip.get_height(), OUT])
	quit(0)
