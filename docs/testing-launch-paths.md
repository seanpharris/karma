# Testing Launch Paths

Karma has two practical launch paths during prototype development.

## Main menu path

Use this when testing the real player-facing boot flow:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-main-menu.ps1
```

Equivalent direct Godot command:

```powershell
& 'C:\Users\pharr\Downloads\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64.exe' --path 'C:\Users\pharr\code\karma' --scene 'res://scenes/MainMenu.tscn'
```

The project default still boots here via `project.godot`:

```text
application/run/main_scene = res://scenes/MainMenu.tscn
```

## Direct gameplay path

Use this for fast gameplay iteration when the menu is in the way:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-gameplay.ps1
```

Equivalent direct Godot command:

```powershell
& 'C:\Users\pharr\Downloads\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64.exe' --path 'C:\Users\pharr\code\karma' --scene 'res://scenes/Main.tscn'
```

`tools/run-game.ps1` remains as a compatibility alias for direct gameplay.

## Automated checks

Use the headless smoke test path before committing gameplay/UI changes:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test.ps1
```

From WSL, the known-good commands are:

```bash
'/mnt/c/Program Files/dotnet/dotnet.exe' build Karma.csproj
'/mnt/c/Users/pharr/Downloads/Godot_v4.6.2-stable_mono_win64/Godot_v4.6.2-stable_mono_win64/Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path "C:\Users\pharr\code\karma" "res://scenes/TestHarness.tscn"
```

## Windows exports

The player-facing build exports as its own executable:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\export-main-game.ps1
```

Output: `build\windows\main\Karma.exe`

The direct gameplay prototype remains separate:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\export-prototype-game.ps1
```

Output: `build\windows\prototype\KarmaPrototype.exe`

Both scripts create the local, ignored `export_presets.cfg` from
`tools\export_presets.template.cfg` if needed.
The raw LPC source tree is kept out of Godot exports; runtime character bundles
come from `assets\art\generated\lpc_npcs`.
