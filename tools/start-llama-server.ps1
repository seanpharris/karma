param(
    [switch]$ForceRestart,
    [switch]$SkipWarmup,
    [string]$WarmupPrompt = "Reply in one short sentence: hello",
    [string]$ModelPath = "",
    [string]$ModelAlias = ""
)

. "$PSScriptRoot\env.ps1"

function Get-LlamaListener {
    Get-NetTCPConnection -LocalPort $LlamaPort -State Listen -ErrorAction SilentlyContinue |
        Select-Object -First 1
}

function Get-LlamaHealth {
    try {
        return Invoke-RestMethod -Method Get -Uri "http://$LlamaHost`:$LlamaPort/health" -ErrorAction Stop
    }
    catch {
        return $null
    }
}

function Resolve-ModelPath {
    param([string]$ExplicitPath)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        return $ExplicitPath
    }

    return $PhiMiniModelPath
}

function Warmup-Llama {
    param(
        [Parameter(Mandatory = $true)][string]$BaseUrl,
        [Parameter(Mandatory = $true)][string]$Alias,
        [Parameter(Mandatory = $true)][string]$Prompt
    )

    $body = @{
        model = $Alias
        messages = @(
            @{ role = "system"; content = "You are a concise NPC dialogue model." },
            @{ role = "user"; content = $Prompt }
        )
        temperature = 0.4
        max_tokens = 32
        stream = $false
    } | ConvertTo-Json -Depth 6

    for ($i = 0; $i -lt 120; $i++) {
        try {
            $response = Invoke-RestMethod -Method Post -Uri "$BaseUrl/v1/chat/completions" -ContentType "application/json" -Body $body -ErrorAction Stop
            if ($response.choices -and $response.choices.Count -gt 0) {
                return
            }
        }
        catch {
            $message = $_.Exception.Message
            if ($message -notmatch "503" -and $message -notmatch "Loading model") {
                throw
            }
        }

        Start-Sleep -Milliseconds 1000
    }

    throw "llama.cpp warmup timed out waiting for the model to finish loading."
}

$resolvedModelPath = Resolve-ModelPath -ExplicitPath $ModelPath
$resolvedAlias = if ([string]::IsNullOrWhiteSpace($ModelAlias)) { $PhiMiniModelAlias } else { $ModelAlias }

if (-not (Test-Path $LlamaServerExe)) {
    throw "llama-server executable not found at $LlamaServerExe"
}

if (-not (Test-Path $resolvedModelPath)) {
    throw "Phi-3.5 mini GGUF model not found at $resolvedModelPath"
}

if ($ForceRestart) {
    Get-Process -Name "llama-server" -ErrorAction SilentlyContinue | Stop-Process -Force
}

$listener = Get-LlamaListener
if ($listener) {
    Write-Host "llama.cpp server already listening on $LlamaHost`:$LlamaPort (PID $($listener.OwningProcess))."
    if (-not $SkipWarmup) {
        $baseUrl = "http://$LlamaHost`:$LlamaPort"
        Write-Host "Waiting for local Phi-3.5 mini model to finish loading..."
        Warmup-Llama -BaseUrl $baseUrl -Alias $resolvedAlias -Prompt $WarmupPrompt
        Write-Host "llama.cpp warmup complete."
    }

    exit 0
}

$existingServer = Get-Process -Name "llama-server" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $existingServer) {
    $arguments = @(
        "-m", $resolvedModelPath,
        "--host", $LlamaHost,
        "--port", "$LlamaPort",
        "--alias", $resolvedAlias,
        "-ngl", "$LlamaGpuLayers",
        "-c", "$LlamaContextSize"
    )

    Start-Process -FilePath $LlamaServerExe -ArgumentList $arguments -WindowStyle Hidden | Out-Null
}

for ($i = 0; $i -lt 120; $i++) {
    Start-Sleep -Milliseconds 500
    $listener = Get-LlamaListener
    if ($listener) {
        Write-Host "llama.cpp server ready on $LlamaHost`:$LlamaPort (PID $($listener.OwningProcess))."
        break
    }
}

if (-not (Get-LlamaListener)) {
    throw "llama.cpp server did not start listening on $LlamaHost`:$LlamaPort."
}

if (-not $SkipWarmup) {
    $baseUrl = "http://$LlamaHost`:$LlamaPort"
    Write-Host "Warming up local Phi-3.5 mini model..."
    Warmup-Llama -BaseUrl $baseUrl -Alias $resolvedAlias -Prompt $WarmupPrompt
    Write-Host "llama.cpp warmup complete."
}
