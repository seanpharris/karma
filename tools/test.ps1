. "$PSScriptRoot\env.ps1"
& $DotnetExe build (Join-Path $ProjectRoot "Karma.csproj")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $GodotConsoleExe --headless --path $ProjectRoot "res://scenes/TestHarness.tscn"
exit $LASTEXITCODE
