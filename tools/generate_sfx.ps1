#requires -Version 5.1
<#
Procedurally synthesises every SFX cue Karma references and bakes the
results to assets/audio/sfx/ as 22050 Hz mono 16-bit WAV files. All
output is original (synthesised from scratch in this script) so it
ships under the project license — no third-party attribution needed.

Run from the repo root:
    powershell.exe -ExecutionPolicy Bypass -File tools/generate_sfx.ps1
#>

$ErrorActionPreference = 'Stop'
$SampleRate = 22050
$RepoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$OutDir = Join-Path $RepoRoot 'assets/audio/sfx'
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }

# --- WAV writer ---------------------------------------------------------

function Write-Wav([string]$path, [double[]]$samples, [int]$sampleRate = $SampleRate) {
    $byteCount = $samples.Length * 2
    $stream = [System.IO.File]::Create($path)
    try {
        $bw = New-Object System.IO.BinaryWriter($stream)
        try {
            $bw.Write([byte[]][System.Text.Encoding]::ASCII.GetBytes('RIFF'))
            $bw.Write([uint32](36 + $byteCount))
            $bw.Write([byte[]][System.Text.Encoding]::ASCII.GetBytes('WAVE'))
            $bw.Write([byte[]][System.Text.Encoding]::ASCII.GetBytes('fmt '))
            $bw.Write([uint32]16)            # PCM chunk size
            $bw.Write([uint16]1)             # PCM format
            $bw.Write([uint16]1)             # mono
            $bw.Write([uint32]$sampleRate)
            $bw.Write([uint32]($sampleRate * 2))
            $bw.Write([uint16]2)             # block align
            $bw.Write([uint16]16)            # bits per sample
            $bw.Write([byte[]][System.Text.Encoding]::ASCII.GetBytes('data'))
            $bw.Write([uint32]$byteCount)
            for ($i = 0; $i -lt $samples.Length; $i++) {
                $s = $samples[$i]
                if ($s -gt 1.0) { $s = 1.0 } elseif ($s -lt -1.0) { $s = -1.0 }
                $bw.Write([int16][math]::Round($s * 32760.0))
            }
        }
        finally { $bw.Dispose() }
    }
    finally { $stream.Dispose() }
}

function Get-Length([double]$seconds) { return [int]($seconds * $SampleRate) }

# --- Synthesis helpers --------------------------------------------------

function Add-Mix([double[]]$dest, [double[]]$src, [double]$gain = 1.0, [int]$offsetSamples = 0) {
    $n = [math]::Min($dest.Length - $offsetSamples, $src.Length)
    for ($i = 0; $i -lt $n; $i++) { $dest[$offsetSamples + $i] += $src[$i] * $gain }
}

function New-Noise([int]$length, [int]$seed) {
    $rng = New-Object System.Random $seed
    $out = [double[]]::new($length)
    for ($i = 0; $i -lt $length; $i++) { $out[$i] = $rng.NextDouble() * 2.0 - 1.0 }
    return ,$out
}

function Apply-LowPass([double[]]$samples, [double]$cutoffHz) {
    $rc = 1.0 / (2.0 * [math]::PI * $cutoffHz)
    $dt = 1.0 / $SampleRate
    $alpha = $dt / ($rc + $dt)
    $prev = 0.0
    for ($i = 0; $i -lt $samples.Length; $i++) {
        $prev = $prev + $alpha * ($samples[$i] - $prev)
        $samples[$i] = $prev
    }
}

function Apply-HighPass([double[]]$samples, [double]$cutoffHz) {
    $rc = 1.0 / (2.0 * [math]::PI * $cutoffHz)
    $dt = 1.0 / $SampleRate
    $alpha = $rc / ($rc + $dt)
    $prevIn = 0.0
    $prevOut = 0.0
    for ($i = 0; $i -lt $samples.Length; $i++) {
        $cur = $samples[$i]
        $prevOut = $alpha * ($prevOut + $cur - $prevIn)
        $prevIn = $cur
        $samples[$i] = $prevOut
    }
}

function Apply-Envelope([double[]]$samples, [double]$attackSec, [double]$releaseSec) {
    $attack = [int]($attackSec * $SampleRate)
    $release = [int]($releaseSec * $SampleRate)
    if ($attack -lt 1) { $attack = 1 }
    if ($release -lt 1) { $release = 1 }
    $n = $samples.Length
    for ($i = 0; $i -lt $n; $i++) {
        $env = 1.0
        if ($i -lt $attack) { $env = $i / [double]$attack }
        $tailStart = $n - $release
        if ($i -gt $tailStart) { $env *= [math]::Max(0.0, ($n - $i) / [double]$release) }
        $samples[$i] *= $env
    }
}

function Apply-ExpDecay([double[]]$samples, [double]$decaySec) {
    $tau = $decaySec * $SampleRate
    for ($i = 0; $i -lt $samples.Length; $i++) {
        $samples[$i] *= [math]::Exp(-$i / $tau)
    }
}

function New-Sine([double]$freq, [int]$length, [double]$phase = 0.0) {
    $out = [double[]]::new($length)
    $step = 2.0 * [math]::PI * $freq / $SampleRate
    for ($i = 0; $i -lt $length; $i++) { $out[$i] = [math]::Sin($phase + $step * $i) }
    return ,$out
}

function New-Triangle([double]$freq, [int]$length) {
    $out = [double[]]::new($length)
    for ($i = 0; $i -lt $length; $i++) {
        $phase = ($i * $freq / $SampleRate) - [math]::Floor($i * $freq / $SampleRate + 0.5)
        $out[$i] = 2.0 * [math]::Abs(2.0 * $phase) - 1.0
    }
    return ,$out
}

function New-Saw([double]$freq, [int]$length) {
    $out = [double[]]::new($length)
    for ($i = 0; $i -lt $length; $i++) {
        $phase = ($i * $freq / $SampleRate) - [math]::Floor($i * $freq / $SampleRate + 0.5)
        $out[$i] = 2.0 * $phase
    }
    return ,$out
}

function New-PitchSweepSine([double]$startHz, [double]$endHz, [int]$length) {
    $out = [double[]]::new($length)
    $phase = 0.0
    for ($i = 0; $i -lt $length; $i++) {
        $t = $i / [double]$length
        $f = $startHz + ($endHz - $startHz) * $t
        $phase += 2.0 * [math]::PI * $f / $SampleRate
        $out[$i] = [math]::Sin($phase)
    }
    return ,$out
}

# --- Cue generators ----------------------------------------------------

function Make-FootstepDirt([int]$seed) {
    $len = Get-Length 0.18
    $samples = New-Noise $len $seed
    Apply-LowPass $samples 1100.0
    Apply-HighPass $samples 80.0
    Apply-Envelope $samples 0.005 0.16
    for ($i = 0; $i -lt $len; $i++) { $samples[$i] *= 0.85 }
    return ,$samples
}

function Make-FootstepStone([int]$seed) {
    $len = Get-Length 0.12
    $samples = New-Noise $len $seed
    Apply-LowPass $samples 4500.0
    Apply-HighPass $samples 800.0
    Apply-Envelope $samples 0.002 0.10
    for ($i = 0; $i -lt $len; $i++) { $samples[$i] *= 0.7 }
    return ,$samples
}

function Make-FootstepWood([int]$seed) {
    $len = Get-Length 0.16
    $samples = New-Noise $len $seed
    Apply-LowPass $samples 2200.0
    Apply-HighPass $samples 200.0
    Apply-Envelope $samples 0.003 0.14
    $tone = New-Sine 230.0 $len
    Apply-ExpDecay $tone 0.05
    Add-Mix $samples $tone 0.4
    return ,$samples
}

function Make-GruntPain {
    $len = Get-Length 0.42
    $out = [double[]]::new($len)
    $phase = 0.0
    for ($i = 0; $i -lt $len; $i++) {
        $t = $i / [double]$len
        $f = 165.0 - 55.0 * $t
        $phase += 2.0 * [math]::PI * $f / $SampleRate
        $core = [math]::Sin($phase) * 0.55 + [math]::Sin($phase * 2.0) * 0.18 + [math]::Sin($phase * 3.0) * 0.10
        $out[$i] = $core
    }
    $breath = New-Noise $len 911
    Apply-LowPass $breath 1200.0
    Add-Mix $out $breath 0.18
    Apply-LowPass $out 1800.0
    Apply-Envelope $out 0.025 0.20
    for ($i = 0; $i -lt $len; $i++) { $out[$i] *= 0.95 }
    return ,$out
}

function Make-GruntAttack {
    $len = Get-Length 0.28
    $out = [double[]]::new($len)
    $phase = 0.0
    for ($i = 0; $i -lt $len; $i++) {
        $t = $i / [double]$len
        $f = 130.0 - 25.0 * $t
        $phase += 2.0 * [math]::PI * $f / $SampleRate
        $out[$i] = [math]::Sin($phase) * 0.55 + [math]::Sin($phase * 2.0) * 0.22
    }
    $breath = New-Noise $len 9120
    Apply-LowPass $breath 1500.0
    Add-Mix $out $breath 0.22
    Apply-LowPass $out 2000.0
    Apply-Envelope $out 0.008 0.10
    return ,$out
}

function Make-SwordSwing {
    $len = Get-Length 0.26
    $samples = New-Noise $len 7331
    Apply-HighPass $samples 600.0
    Apply-LowPass $samples 4000.0
    # Sweep volume: ramp up, peak around 35%, decay
    for ($i = 0; $i -lt $len; $i++) {
        $t = $i / [double]$len
        $env = if ($t -lt 0.35) { $t / 0.35 } else { (1.0 - $t) / 0.65 }
        $samples[$i] *= $env * 0.85
    }
    return ,$samples
}

function Make-SwordHit {
    $len = Get-Length 0.55
    $out = [double[]]::new($len)
    # Inharmonic metallic partials
    $partials = @(880.0, 1320.0, 1980.0, 2670.0, 3950.0)
    $weights  = @(0.45,  0.30,   0.22,   0.15,   0.10)
    for ($p = 0; $p -lt $partials.Length; $p++) {
        $tone = New-Sine $partials[$p] $len
        Apply-ExpDecay $tone (0.18 - $p * 0.02)
        Add-Mix $out $tone $weights[$p]
    }
    # Initial impact noise transient
    $transient = New-Noise (Get-Length 0.04) 4242
    Apply-HighPass $transient 1200.0
    Add-Mix $out $transient 0.6
    Apply-Envelope $out 0.001 0.02
    return ,$out
}

function Make-HitThud {
    $len = Get-Length 0.22
    $out = [double[]]::new($len)
    $tone = New-Sine 95.0 $len
    Apply-ExpDecay $tone 0.06
    Add-Mix $out $tone 0.7
    $transient = New-Noise (Get-Length 0.05) 7777
    Apply-LowPass $transient 800.0
    Add-Mix $out $transient 0.45
    Apply-Envelope $out 0.002 0.04
    return ,$out
}

function Make-DoorOpen {
    $len = Get-Length 0.85
    $samples = New-Noise $len 13579
    Apply-LowPass $samples 2200.0
    Apply-HighPass $samples 150.0
    # Slow LFO modulation = "creak"
    for ($i = 0; $i -lt $len; $i++) {
        $t = $i / [double]$SampleRate
        $lfo = 0.55 + 0.45 * [math]::Sin(2.0 * [math]::PI * 3.5 * $t)
        $samples[$i] *= $lfo
    }
    Apply-Envelope $samples 0.04 0.30
    for ($i = 0; $i -lt $len; $i++) { $samples[$i] *= 0.7 }
    return ,$samples
}

function Make-LatchClick {
    $len = Get-Length 0.07
    $samples = New-Noise $len 3030
    Apply-HighPass $samples 1500.0
    Apply-LowPass $samples 5500.0
    Apply-Envelope $samples 0.001 0.05
    return ,$samples
}

function New-BellSamples([double]$rootHz, [double]$decaySec, [int]$length) {
    $out = [double[]]::new($length)
    # Tubular-bell-like inharmonic ratios.
    $ratios  = @(0.5,  1.0,  2.0,  2.76, 5.40, 8.93)
    $weights = @(0.35, 0.55, 0.30, 0.22, 0.14, 0.08)
    $decays  = @(1.4,  1.0,  0.65, 0.40, 0.22, 0.14)
    for ($p = 0; $p -lt $ratios.Length; $p++) {
        $tone = New-Sine ($rootHz * $ratios[$p]) $length
        Apply-ExpDecay $tone ($decaySec * $decays[$p])
        Add-Mix $out $tone $weights[$p]
    }
    return ,$out
}

function Make-KarmaBreakStinger {
    $len = Get-Length 1.4
    $out = [double[]]::new($len)
    # Cracked low bell: detuned partials.
    $bell = New-BellSamples 220.0 1.2 $len
    Add-Mix $out $bell 0.8
    $bellOff = New-BellSamples 233.5 1.0 $len
    Add-Mix $out $bellOff 0.45
    # Chain rattle: rapid noise impulses for the first ~600ms.
    $rattleLen = Get-Length 0.6
    $rattle = New-Noise $rattleLen 5151
    Apply-HighPass $rattle 2000.0
    for ($i = 0; $i -lt $rattleLen; $i++) {
        $tick = if (($i % 1100) -lt 220) { 1.0 } else { 0.15 }
        $rattle[$i] *= $tick
    }
    Add-Mix $out $rattle 0.35 (Get-Length 0.05)
    Apply-Envelope $out 0.005 0.30
    return ,$out
}

function Make-ContrabandAlarm {
    $len = Get-Length 1.6
    $out = [double[]]::new($len)
    # Three watchtower bell strikes, ~430ms apart.
    $strikeLen = Get-Length 0.7
    foreach ($i in 0..2) {
        $bell = New-BellSamples 660.0 0.55 $strikeLen
        Add-Mix $out $bell 0.55 (Get-Length (0.05 + $i * 0.42))
    }
    Apply-Envelope $out 0.005 0.15
    return ,$out
}

function Make-PurchaseChime {
    $len = Get-Length 0.65
    $out = [double[]]::new($len)
    $bell = New-BellSamples 1320.0 0.45 $len
    Add-Mix $out $bell 0.6
    # Coin clink first
    $clink = New-Sine 3200.0 (Get-Length 0.10)
    Apply-ExpDecay $clink 0.04
    Add-Mix $out $clink 0.4
    $clink2 = New-Sine 4100.0 (Get-Length 0.08)
    Apply-ExpDecay $clink2 0.03
    Add-Mix $out $clink2 0.3 (Get-Length 0.04)
    Apply-Envelope $out 0.002 0.10
    return ,$out
}

function Make-ReloadClick {
    $len = Get-Length 0.50
    $out = [double[]]::new($len)
    # Crossbow draw: noise sweeping up in pitch.
    $drawLen = Get-Length 0.35
    $draw = New-Noise $drawLen 8888
    Apply-HighPass $draw 250.0
    Apply-LowPass $draw 1800.0
    for ($i = 0; $i -lt $drawLen; $i++) {
        $t = $i / [double]$drawLen
        $draw[$i] *= (0.3 + 0.5 * $t)
    }
    Add-Mix $out $draw 0.55
    # Sharp click at the end.
    $click = New-Noise (Get-Length 0.04) 1212
    Apply-HighPass $click 2500.0
    Add-Mix $out $click 0.7 (Get-Length 0.40)
    Apply-Envelope $out 0.002 0.04
    return ,$out
}

function Make-SupplyDropHorn {
    $len = Get-Length 1.4
    $out = [double[]]::new($len)
    # Two-note horn fanfare: G3 (196 Hz) -> C4 (262 Hz).
    function Add-Note([double[]]$dest, [double]$freq, [int]$noteLen, [int]$offset) {
        $tri = New-Triangle $freq $noteLen
        $saw = New-Saw ($freq * 1.005) $noteLen
        for ($i = 0; $i -lt $noteLen; $i++) { $tri[$i] = ($tri[$i] * 0.7) + ($saw[$i] * 0.3) }
        # Vibrato
        for ($i = 0; $i -lt $noteLen; $i++) {
            $t = $i / [double]$SampleRate
            $tri[$i] *= (0.85 + 0.15 * [math]::Sin(2.0 * [math]::PI * 5.5 * $t))
        }
        Apply-Envelope $tri 0.04 0.10
        for ($i = 0; $i -lt $noteLen; $i++) {
            if ($offset + $i -lt $dest.Length) { $dest[$offset + $i] += $tri[$i] * 0.55 }
        }
    }
    Add-Note $out 196.0 (Get-Length 0.55) (Get-Length 0.05)
    Add-Note $out 262.0 (Get-Length 0.70) (Get-Length 0.65)
    Apply-Envelope $out 0.005 0.05
    return ,$out
}

function Make-StructureInteractPop { return ,(Make-LatchClick) }

function Make-ClinicReviveChime {
    $len = Get-Length 1.3
    $out = [double[]]::new($len)
    $bell = New-BellSamples 880.0 1.0 $len
    Add-Mix $out $bell 0.55
    # Soft choir-like pad swell using detuned sines.
    $padLen = $len
    $pad1 = New-Sine 440.0 $padLen
    $pad2 = New-Sine 442.2 $padLen
    $pad3 = New-Sine 660.5 $padLen
    for ($i = 0; $i -lt $padLen; $i++) {
        $t = $i / [double]$padLen
        $env = [math]::Sin([math]::PI * $t) * 0.4
        $out[$i] += ($pad1[$i] + $pad2[$i] + $pad3[$i] * 0.6) * $env * 0.18
    }
    Apply-Envelope $out 0.02 0.20
    return ,$out
}

function Make-BountyPaid {
    $len = Get-Length 1.0
    $out = [double[]]::new($len)
    # Stream of randomly-pitched coin clinks for ~750 ms.
    $rng = New-Object System.Random 22222
    for ($n = 0; $n -lt 18; $n++) {
        $offsetSec = 0.02 + $rng.NextDouble() * 0.78
        $freq = 2400.0 + $rng.NextDouble() * 2400.0
        $clinkLen = Get-Length 0.10
        $clink = New-Sine $freq $clinkLen
        Apply-ExpDecay $clink (0.018 + $rng.NextDouble() * 0.02)
        Add-Mix $out $clink (0.18 + $rng.NextDouble() * 0.18) (Get-Length $offsetSec)
    }
    Apply-Envelope $out 0.005 0.20
    return ,$out
}

# --- Bake everything ---------------------------------------------------

$cues = @(
    @{ Name = 'footstep_dirt.wav';            Make = { Make-FootstepDirt 1001 } }
    @{ Name = 'footstep_dirt_b.wav';          Make = { Make-FootstepDirt 1002 } }
    @{ Name = 'footstep_stone.wav';           Make = { Make-FootstepStone 2001 } }
    @{ Name = 'footstep_stone_b.wav';         Make = { Make-FootstepStone 2002 } }
    @{ Name = 'footstep_wood.wav';            Make = { Make-FootstepWood 3001 } }
    @{ Name = 'footstep_wood_b.wav';          Make = { Make-FootstepWood 3002 } }
    @{ Name = 'grunt_pain.wav';               Make = { Make-GruntPain } }
    @{ Name = 'grunt_attack.wav';             Make = { Make-GruntAttack } }
    @{ Name = 'sword_swing.wav';              Make = { Make-SwordSwing } }
    @{ Name = 'sword_hit.wav';                Make = { Make-SwordHit } }
    @{ Name = 'hit_thud.wav';                 Make = { Make-HitThud } }
    @{ Name = 'door_open.wav';                Make = { Make-DoorOpen } }
    @{ Name = 'interact_pop.wav';             Make = { Make-StructureInteractPop } }
    @{ Name = 'karma_break_stinger.wav';      Make = { Make-KarmaBreakStinger } }
    @{ Name = 'contraband_alarm.wav';         Make = { Make-ContrabandAlarm } }
    @{ Name = 'purchase_chime.wav';           Make = { Make-PurchaseChime } }
    @{ Name = 'reload_click.wav';             Make = { Make-ReloadClick } }
    @{ Name = 'supply_drop_horn.wav';         Make = { Make-SupplyDropHorn } }
    @{ Name = 'clinic_revive_chime.wav';      Make = { Make-ClinicReviveChime } }
    @{ Name = 'bounty_paid.wav';              Make = { Make-BountyPaid } }
)

foreach ($cue in $cues) {
    $path = Join-Path $OutDir $cue.Name
    $samples = & $cue.Make
    Write-Wav $path $samples
    $size = (Get-Item $path).Length
    Write-Output ("baked {0}  ({1:N0} bytes, {2:N0} samples)" -f $cue.Name, $size, $samples.Length)
}

Write-Output ("done. {0} cues written under {1}" -f $cues.Length, $OutDir)
