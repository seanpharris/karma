param(
    [switch]$IncludeUi
)

. "$PSScriptRoot\env.ps1"

$stopped = $false
Get-Process -Name "voicebox-server" -ErrorAction SilentlyContinue | ForEach-Object {
    $_ | Stop-Process -Force
    $stopped = $true
}

if ($IncludeUi) {
    Get-Process -Name "voicebox" -ErrorAction SilentlyContinue | ForEach-Object {
        $_ | Stop-Process -Force
        $stopped = $true
    }
}

if ($stopped) {
    Write-Host "Stopped Voicebox process(es)."
} else {
    Write-Host "No Voicebox processes were running."
}
