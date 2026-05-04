. "$PSScriptRoot\env.ps1"
& (Join-Path $PSScriptRoot "start-voicebox-server.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $DotnetExe build (Join-Path $ProjectRoot "Karma.csproj")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $GodotExe --path $ProjectRoot "res://scenes/VoiceboxMinimalDemo.tscn"
exit $LASTEXITCODE
