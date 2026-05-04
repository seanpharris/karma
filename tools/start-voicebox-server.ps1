param(
    [switch]$ForceRestart,
    [switch]$KeepUi,
    [string]$WarmupProfile = "",
    [string]$WarmupText = ""
)

. "$PSScriptRoot\env.ps1"

function Get-VoiceboxListener {
    Get-NetTCPConnection -LocalPort $VoiceboxPort -State Listen -ErrorAction SilentlyContinue |
        Select-Object -First 1
}

function Get-VoiceboxHealth {
    try {
        return Invoke-RestMethod -Method Get -Uri "http://$VoiceboxHost`:$VoiceboxPort/health" -ErrorAction Stop
    }
    catch {
        return $null
    }
}

function Get-VoiceboxProfiles {
    try {
        return @(Invoke-RestMethod -Method Get -Uri "http://$VoiceboxHost`:$VoiceboxPort/profiles" -ErrorAction Stop)
    }
    catch {
        return @()
    }
}

function Resolve-VoiceboxProfile {
    param(
        [string]$RequestedProfile
    )

    $profiles = Get-VoiceboxProfiles
    if ($profiles.Count -eq 0) {
        throw "Voicebox has no profiles loaded. Open Voicebox and create or import a voice profile, then rerun the demo."
    }

    if (-not [string]::IsNullOrWhiteSpace($RequestedProfile)) {
        $match = $profiles | Where-Object {
            $_.id -eq $RequestedProfile -or $_.name -eq $RequestedProfile
        } | Select-Object -First 1
        if ($match) {
            return $match
        }

        $first = $profiles | Select-Object -First 1
        Write-Warning "Configured Voicebox profile '$RequestedProfile' was not found. Using '$($first.name)' instead."
        return $first
    }

    return $profiles | Select-Object -First 1
}

function Warmup-Voicebox {
    param(
        [Parameter(Mandatory = $true)][string]$Profile,
        [Parameter(Mandatory = $true)][string]$Text
    )

    $body = @{ text = $Text; profile = $Profile } | ConvertTo-Json
    $speak = Invoke-RestMethod -Method Post -Uri "http://$VoiceboxHost`:$VoiceboxPort/speak" -ContentType "application/json" -Body $body
    if (-not $speak.id) {
        throw "Voicebox warmup did not return a generation id."
    }

    for ($i = 0; $i -lt 120; $i++) {
        Start-Sleep -Milliseconds 250
        $statusRaw = Invoke-WebRequest -UseBasicParsing -Uri "http://$VoiceboxHost`:$VoiceboxPort/generate/$($speak.id)/status"
        $textPayload = $statusRaw.Content.Trim()
        if ($textPayload.StartsWith("data:")) {
            $dataLines = $textPayload -split "(\r?\n)+" |
                Where-Object { $_ -and $_.Trim().StartsWith("data:") }
            $textPayload = ($dataLines | Select-Object -Last 1).Trim()
            $textPayload = $textPayload.Substring($textPayload.IndexOf('{'))
        }
        $status = ($textPayload | ConvertFrom-Json).status
        if ($status -eq "completed") {
            return
        }
        if ($status -eq "failed") {
            throw "Voicebox warmup generation failed."
        }
    }

    throw "Voicebox warmup timed out."
}

if (-not (Test-Path $VoiceboxServerExe)) {
    throw "Voicebox server executable not found at $VoiceboxServerExe"
}

if ($ForceRestart) {
    Get-Process -Name "voicebox-server" -ErrorAction SilentlyContinue | Stop-Process -Force
}

if (-not $KeepUi) {
    Get-Process -Name "voicebox" -ErrorAction SilentlyContinue | Stop-Process -Force
}

$listener = Get-VoiceboxListener
if ($listener) {
    Write-Host "Voicebox server already listening on $VoiceboxHost`:$VoiceboxPort (PID $($listener.OwningProcess))."
    exit 0
}

$existingServer = Get-Process -Name "voicebox-server" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $existingServer) {
    Start-Process -FilePath $VoiceboxServerExe `
        -ArgumentList "--host", $VoiceboxHost, "--port", "$VoiceboxPort" `
        -WindowStyle Hidden | Out-Null
}

for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Milliseconds 250
    $listener = Get-VoiceboxListener
    if ($listener) {
        Write-Host "Voicebox server ready on $VoiceboxHost`:$VoiceboxPort (PID $($listener.OwningProcess))."
        break
    }
}

if (-not (Get-VoiceboxListener)) {
    throw "Voicebox server did not start listening on $VoiceboxHost`:$VoiceboxPort."
}

if (-not [string]::IsNullOrWhiteSpace($WarmupProfile) -and -not [string]::IsNullOrWhiteSpace($WarmupText)) {
    $health = Get-VoiceboxHealth
    if ($null -eq $health -or -not $health.model_loaded) {
        $resolvedProfile = Resolve-VoiceboxProfile -RequestedProfile $WarmupProfile
        Write-Host "Warming up Voicebox model for profile '$($resolvedProfile.name)'..."
        Warmup-Voicebox -Profile $resolvedProfile.id -Text $WarmupText
        Write-Host "Voicebox warmup complete."
    }
}
