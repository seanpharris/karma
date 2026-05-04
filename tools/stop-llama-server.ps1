. "$PSScriptRoot\env.ps1"

$stopped = $false
Get-Process -Name "llama-server" -ErrorAction SilentlyContinue | ForEach-Object {
    $_ | Stop-Process -Force
    $stopped = $true
}

if ($stopped) {
    Write-Host "Stopped llama.cpp process(es)."
}
else {
    Write-Host "No llama.cpp processes were running."
}
