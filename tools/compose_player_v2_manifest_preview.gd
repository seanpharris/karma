extends SceneTree

const MANIFEST := "res://assets/art/sprites/player_v2/player_model_32x64_manifest.json"

func _initialize() -> void:
    var text := FileAccess.get_file_as_string(MANIFEST)
    var data: Dictionary = JSON.parse_string(text)
    if data.is_empty():
        push_error("could not parse manifest")
        quit(1)
        return
    var base_dir := MANIFEST.get_base_dir()
    var by_id := {}
    for layer in data["layers"]:
        by_id[layer["id"]] = layer
    var target: Image = null
    for layer_id in data["previewStack"]:
        var layer: Dictionary = by_id[layer_id]
        var image := Image.load_from_file(base_dir + "/" + String(layer["path"]))
        if image == null or image.is_empty():
            push_error("could not load layer: " + String(layer["path"]))
            quit(1)
            return
        if target == null:
            target = Image.create_empty(image.get_width(), image.get_height(), false, Image.FORMAT_RGBA8)
            target.fill(Color(0, 0, 0, 0))
        target.blend_rect(image, Rect2i(Vector2i.ZERO, image.get_size()), Vector2i.ZERO)
    if target == null:
        target = Image.create_empty(int(data["frameWidth"]) * int(data["columns"]), int(data["frameHeight"]) * int(data["rows"]), false, Image.FORMAT_RGBA8)
        target.fill(Color(0, 0, 0, 0))
    var out_path := base_dir + "/" + String(data["composite"])
    target.save_png(out_path)
    print("wrote manifest preview: ", out_path)
    quit(0)
