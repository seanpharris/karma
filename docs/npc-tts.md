# NPC Voice / Dialogue Prototype

This document is the current handoff note for the Mara NPC voice prototype.
It now covers more than plain TTS: the prototype includes local LLM dialogue,
Voicebox-based TTS, Voicebox-based STT, proximity-driven speech behavior, and
basic interaction logic such as asking Mara to follow the player.

## What We Built Tonight

The prototype moved from a simple event-driven TTS test into a small vertical
slice of NPC interaction:

- Mara can notice the player at range and speak ambient lines.
- Mara's speech volume scales by distance.
- The player can open dialogue near Mara and speak back.
- Player speech is transcribed through Voicebox STT.
- Mara replies are generated through a local `llama.cpp` runtime when
  available, with a local stub fallback.
- Mara's reply is spoken through Voicebox TTS.
- Mara can respond to simple natural-language requests such as `follow me` and
  `wait here`.
- Follow / no / not-yet decisions are now intended to be driven primarily by
  the player's karma state.

## Current Demo Scenes

### `res://scenes/NpcTtsDemo.tscn`
Main end-to-end prototype scene for:

- proximity greeting
- STT -> LLM -> TTS loop
- dialogue open / close behavior
- distance-based speech volume
- Mara follow interaction

### `res://scenes/VoiceboxMinimalDemo.tscn`
Minimal Voicebox playback harness used to isolate TTS issues.

Use this when debugging:

- Voicebox profile issues
- duplicated playback
- playback artifacts
- "is the problem the game, or just the TTS path?"

## Runtime Stack

### TTS

Current TTS path is Voicebox-first.

- `scripts/Audio/VoiceboxSpeechPlayer.cs`
- `scripts/Audio/NpcTextToSpeechController.cs`
- `tools/start-voicebox-server.ps1`
- `tools/run-tts-demo.ps1`

Notes:

- Old OS TTS overlap was removed from the active demo path.
- Playback uses the generator-based path that fixed the earlier artifact issue.
- We added a Voicebox profile resolver so the project does not depend only on
  one exact hardcoded profile name.

### LLM

Current local LLM path:

- `llama.cpp` local server
- `Phi-3.5-mini` GGUF
- prompt assembly from Mara context + runtime state

Main files:

- `scripts/Voice/NpcLlmPromptBuilder.cs`
- `scripts/Voice/LlamaCppNpcDialogueClient.cs`
- `scripts/Voice/NpcDialogueTestBackend.cs`
- `docs/worldbuilding/npc-context/mara.md`
- `tools/start-llama-server.ps1`

Notes:

- The local LLM is preferred when available.
- Stub fallback still exists when the local LLM is unavailable.
- Mara context is no longer just canned dialogue; it is prompt-driven.

### STT

Current STT path:

- Voicebox `/transcribe`
- English language hint
- Windows-native mic recording through `NAudio`

Main files:

- `scripts/Voice/VoiceboxSttClient.cs`
- `scripts/Voice/WindowsMicrophoneRecorder.cs`
- `tools/ensure-voicebox-stt-model.ps1`

Important note:

- Godot microphone capture was not reliable enough for this prototype.
- We replaced live capture with a Windows-native recording path, which is what
  made STT actually work in the demo.

## Mara Demo Interaction Model

### Proximity

Mara currently has two main distance bands in the demo:

- notice range: `300px`
- conversation range: `40px`

Behavior:

- player starts outside the notice range
- when entering notice range, Mara can greet the player
- voice volume ramps smoothly from `0` at the outer range to full volume near
  conversation range
- player can open dialogue anywhere within the notice range

### Ambient Speech

Mara now uses ambient lines instead of repeating one hardcoded opening line.

Current behavior:

- greeting when player enters notice range
- optional follow-up if the player lingers without responding
- greeting memory to avoid immediate repetition
- ambient speech now respects the live dialogue state better than before

### Thinking Cues

If Mara's real reply takes a moment to generate, she may play a short thinking
cue first.

Current cue pool:

- `Hmm.`
- `Uhhh.`
- `Uhmmm.`

This is only meant to soften generation delay and should not overlap with other
NPC speech.

### Player Response

Current flow:

- press `E` near Mara to open dialogue
- hold `F` to record
- release `F` to transcribe and auto-submit the line
- Mara generates a reply and speaks it back

### Follow Interaction

Prototype natural-language interaction exists for:

- `follow me`
- `come with me`
- `walk with me`
- `join me`
- `wait here`
- `stay here`
- `stay put`
- `stop following`

Current implementation:

- this is demo-local behavior, not a full authoritative server companion system
- Mara follows the player locally in the demo scene when she agrees
- stop-following requests clear the local follow state

## Karma-Based Interaction Direction

We changed the intent of follow decisions so they are based primarily on Karma
state, not just vague trust wording.

Current design direction:

- positive karma / strong standing / good Mara faction reputation should make
  Mara more likely to agree
- mid-range karma should bias toward `not yet`
- bad karma / hostile standing / poor faction reputation should bias toward `no`
- urgency should mostly decide `yes` versus `not yet`, not override obvious bad
  karma into a yes

Relevant files:

- `scripts/UI/NpcTtsDemoController.cs`
- `scripts/Voice/NpcLlmPromptBuilder.cs`
- `scripts/Voice/NpcDialogueTestBackend.cs`
- `docs/worldbuilding/npc-context/mara.md`

This still needs more playtesting and tuning.

## Important Files

### Core demo / interaction

- `scripts/UI/NpcTtsDemoController.cs`
- `scenes/NpcTtsDemo.tscn`

### Voice playback

- `scripts/Audio/VoiceboxSpeechPlayer.cs`
- `scripts/Audio/NpcTextToSpeechController.cs`
- `scripts/Voice/VoiceboxProfileResolver.cs`

### LLM / prompting

- `scripts/Voice/NpcLlmPromptBuilder.cs`
- `scripts/Voice/LlamaCppNpcDialogueClient.cs`
- `scripts/Voice/NpcDialogueTestBackend.cs`
- `docs/worldbuilding/npc-context/mara.md`

### STT / mic capture

- `scripts/Voice/VoiceboxSttClient.cs`
- `scripts/Voice/WindowsMicrophoneRecorder.cs`

### Runtime scripts

- `tools/run-tts-demo.ps1`
- `tools/run-voicebox-minimal-demo.ps1`
- `tools/start-voicebox-server.ps1`
- `tools/stop-voicebox-server.ps1`
- `tools/start-llama-server.ps1`
- `tools/stop-llama-server.ps1`
- `tools/ensure-voicebox-stt-model.ps1`
- `tools/env.ps1`

## Known Working Pieces

These parts were working in the prototype tonight:

- local LLM-backed Mara replies
- Voicebox TTS playback in the minimal demo
- Windows-native STT recording path
- STT -> LLM -> TTS loop in the Mara demo
- distance-based voice volume
- dialogue open mode that prevents accidental movement while speaking
- basic `follow me` / `wait here` interaction loop

## Known Problems / Current Blockers

### 1. Voicebox profile state is fragile

Current blocker at end of night:

- Voicebox API can come up with zero loaded profiles
- when that happens, the demo cannot speak
- the project no longer hardcodes only `test1`, but Voicebox still needs at
  least one actual profile loaded in the running API

Current symptom:

- `http://127.0.0.1:17493/profiles` may return an empty array
- TTS will fail until a profile exists again

### 2. Voicebox process duplication can happen

We saw duplicate `voicebox-server.exe` processes at points during testing.
That can confuse debugging and should be kept under control.

### 3. This is still prototype-local behavior

The follow system, ambient speech timing, and proximity logic are still living
in the demo controller. They are not yet generalized into the real game NPC
systems.

### 4. Local LLM shipping is still a prototype decision

The local `Phi-3.5-mini` path works as a prototype, but it is not yet the final
shipping strategy for all player machines.

## Launch Commands

### Mara prototype demo

From repo root:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-tts-demo.ps1
```

### Minimal Voicebox demo

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-voicebox-minimal-demo.ps1
```

## To Do

### Immediate / next session

- get Voicebox back into a stable state with at least one loaded profile
- make the launcher surface the active resolved Voicebox profile more clearly
- verify Mara demo voice after profile recovery
- clean up duplicate Voicebox server startup behavior
- test karma-based follow decisions with low / mid / high karma characters

### Interaction improvements

- move follow logic out of demo-local behavior and into a more durable NPC
  interaction system
- add more natural interaction verbs beyond follow / stop-following
- make Mara's yes / no / not-yet decisions feel less binary and more situational
- let the LLM reference what Mara is doing in a more grounded way
- improve silent-player handling so ambient nudges never feel spammy

### STT improvements

- add a cleaner in-world recording indicator
- improve transcript review / correction flow
- optionally allow manual confirm-before-send mode
- improve fallback behavior when transcription is weak

### TTS improvements

- make Voicebox profile discovery and selection easier
- add a clearer project-level voice configuration story
- eventually support bundling / launching the voice runtime more cleanly with
  the game

### LLM improvements

- keep pushing Mara away from sounding like a generic NPC
- add more contextual memory and better scene grounding
- test whether follow / trust / karma decisions need explicit structured output
  instead of only reply parsing

### Shipping / architecture

- decide long-term local-vs-remote LLM strategy
- define a hardware fallback matrix for dialogue generation
- decide how Voicebox or an alternative TTS runtime should ship with the game
- decide whether STT should remain Windows-native in prototype form or be moved
  to a cross-platform capture path later

## Summary

The NPC voice prototype is no longer just a TTS test.
It is now a working conversation slice with:

- proximity awareness
- STT input
- local LLM reply generation
- Voicebox speech output
- early natural-language interaction handling
- early karma-aware NPC decision framing

The biggest unfinished problem at the end of the night is not the interaction
logic itself. It is runtime stability around Voicebox profile availability and
service startup.
