. "$PSScriptRoot\env.ps1"
& (Join-Path $PSScriptRoot "start-voicebox-server.ps1") `
  -WarmupProfile $VoiceboxProfile `
  -WarmupText "Well hello there, traveler."
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $PSScriptRoot "start-llama-server.ps1")
if ($LASTEXITCODE -ne 0) {
  Write-Warning "llama.cpp did not start. The demo will fall back to the local stub dialogue backend."
}
& $DotnetExe build (Join-Path $ProjectRoot "Karma.csproj")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $GodotExe --path $ProjectRoot "res://scenes/NpcTtsDemo.tscn"
exit $LASTEXITCODE
