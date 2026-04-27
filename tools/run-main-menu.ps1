. "$PSScriptRoot\env.ps1"
& $GodotExe --path $ProjectRoot --scene "res://scenes/MainMenu.tscn"
exit $LASTEXITCODE
