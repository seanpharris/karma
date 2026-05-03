. "$PSScriptRoot\env.ps1"
& $GodotExe --path $ProjectRoot --scene "res://scenes/Main.tscn"
exit $LASTEXITCODE
