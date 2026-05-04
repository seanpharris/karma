param(
    [string]$ModelName = "whisper-small"
)

. "$PSScriptRoot\env.ps1"

& (Join-Path $PSScriptRoot "start-voicebox-server.ps1")
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

function Get-VoiceboxModels {
    Invoke-RestMethod -Method Get -Uri "http://$VoiceboxHost`:$VoiceboxPort/models/status"
}

function Get-ModelStatus {
    param([string]$Name)

    $models = Get-VoiceboxModels
    return $models.models | Where-Object { $_.model_name -eq $Name } | Select-Object -First 1
}

$model = Get-ModelStatus -Name $ModelName
if ($null -eq $model) {
    throw "Voicebox model '$ModelName' was not found in /models/status."
}

if ($model.downloaded) {
    Write-Host "Voicebox STT model '$ModelName' is already downloaded."
    exit 0
}

if (-not $model.downloading) {
    $body = @{ model_name = $ModelName } | ConvertTo-Json
    Invoke-RestMethod -Method Post -Uri "http://$VoiceboxHost`:$VoiceboxPort/models/download" -ContentType "application/json" -Body $body | Out-Null
    Write-Host "Started Voicebox download for '$ModelName'."
}
else {
    Write-Host "Voicebox model '$ModelName' is already downloading."
}

for ($i = 0; $i -lt 720; $i++) {
    Start-Sleep -Seconds 5
    $model = Get-ModelStatus -Name $ModelName
    if ($null -eq $model) {
        throw "Voicebox model '$ModelName' disappeared from status while downloading."
    }

    if ($model.downloaded) {
        Write-Host "Voicebox STT model '$ModelName' download complete."
        exit 0
    }

    Write-Host "Waiting for '$ModelName'... downloading=$($model.downloading) downloaded=$($model.downloaded)"
}

throw "Timed out waiting for Voicebox model '$ModelName' to finish downloading."
