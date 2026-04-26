. "$PSScriptRoot\env.ps1"
& $DotnetExe build (Join-Path $ProjectRoot "Karma.csproj")
exit $LASTEXITCODE
