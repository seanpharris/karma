param(
    [ValidateSet("Main", "Prototype")]
    [string]$Target = "Main",
    [switch]$Debug,
    [switch]$SkipBuild,
    [switch]$RefreshPresets
)

. "$PSScriptRoot\env.ps1"

$PresetTemplatePath = Join-Path $PSScriptRoot "export_presets.template.cfg"
$PresetPath = Join-Path $ProjectRoot "export_presets.cfg"

if ($RefreshPresets -or -not (Test-Path $PresetPath)) {
    if (-not (Test-Path $PresetTemplatePath)) {
        Write-Error "Missing export preset template at $PresetTemplatePath"
        exit 1
    }

    Copy-Item $PresetTemplatePath $PresetPath -Force
    Write-Host "Created/refreshed local Godot export presets from tools\export_presets.template.cfg"
}

if ($Target -eq "Prototype") {
    $PresetName = "Windows Prototype Game"
    $ExportPath = Join-Path $ProjectRoot "build\windows\prototype\KarmaPrototype.exe"
} else {
    $PresetName = "Windows Main Game"
    $ExportPath = Join-Path $ProjectRoot "build\windows\main\Karma.exe"
}

$ExportDirectory = Split-Path -Parent $ExportPath
New-Item -ItemType Directory -Force -Path $ExportDirectory | Out-Null

if (-not $SkipBuild) {
    & $DotnetExe build (Join-Path $ProjectRoot "Karma.csproj")
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

$ExportMode = if ($Debug) { "--export-debug" } else { "--export-release" }
Write-Host "Exporting $PresetName to $ExportPath"
& $GodotConsoleExe --headless --path $ProjectRoot $ExportMode $PresetName $ExportPath
$ExportExitCode = $LASTEXITCODE

if ($ExportExitCode -ne 0) {
    exit $ExportExitCode
}

if (-not (Test-Path $ExportPath)) {
    Write-Error "Godot export finished without creating $ExportPath. Check that Windows export templates are installed for this Godot version."
    exit 1
}

Write-Host "Exported $ExportPath"
exit 0
