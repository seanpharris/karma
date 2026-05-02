# NPC Conversational AI — STT + LLM + TTS Plan

User-flagged 2026-05-02:
- "the audio agent could also work on the tts from the npcs"
- "we will also need to come up with a lot of voices for the tts for the npcs"
- "we will need to work on speech to text as well so the npcs can 'hear' the players"
- "aka the llm"

So the full loop is:

```
Player mic
  → STT (transcribe player speech to text)
  → LLM (NPC identity + transcript + context → dialogue response)
  → TTS (NPC voice id + response text → audio clip)
  → Audio playback (positional, NPC-anchored)
```

This is a five-piece system. Each piece can be a separate agent task
because they communicate through narrow seams (transcript text,
response text, voice id, audio path).

## Architecture sketch

A new `Karma.Voice` namespace under `scripts/Voice/` holds the runtime.

```
scripts/Voice/
  PlayerMicCapture.cs          // capture push-to-talk audio (Godot 4 microphone API)
  SpeechRecognizer.cs          // STT seam — Whisper.cpp local, OpenAI Whisper API, or stub
  NpcDialogueLLM.cs            // LLM seam — Anthropic API or local llama or stub
  NpcVoiceSynthesizer.cs       // TTS seam — Piper local, ElevenLabs API, or stub
  VoiceCatalog.cs              // many-voices registry, NPC → voice id mapping
  ConversationOrchestrator.cs  // wires the above into a single push-to-talk flow
```

Each seam ships with a **stub backend** that returns deterministic
placeholder data so the loop can be developed end-to-end before any
external service is wired in. This keeps the prototype offline-friendly
and tests deterministic.

## Stage 1 — Speech-to-Text (STT)

**Trigger:** push-to-talk hold key (default `V`). Capture starts on
press, stops on release, transcribes on release.

**Options:**
- **Whisper.cpp** (offline, MIT license) — best for privacy + offline
  builds. ~250 MB for the small model; runs on CPU. Native binding via
  Godot's `OS.Execute` calling a `whisper-cli` binary, or a C# wrapper.
- **OpenAI Whisper API** — high quality, online, ~$0.006/min.
  Requires an `OPENAI_API_KEY` env var; the audio agent should keep
  the API call in a single class so it's easy to swap.
- **Stub** — fixed-string mapping from clip duration buckets to canned
  test phrases. Used in unit tests + when no backend configured.

**Recommended default:** stub for the prototype, with the
Whisper.cpp path stubbed-out behind a feature flag in the same shape
so the local-offline mode can be turned on later.

## Stage 2 — LLM dialogue

**Inputs to the prompt:**
- NPC identity from `ThemeData.NpcRoster[npcId]` (name, role,
  faction, alignment, personality, secret, likes, dislikes).
- Relationship context (gossip targets, intensity).
- Recent server events the NPC witnessed (witness propagation
  already records this).
- Player's transcribed message.
- A short history of the conversation so far (last 3 turns).

**System prompt template:**
```
You are {npc.name}, a {npc.role} in the {theme.display_name}
setting. {npc.personality}. You {npc.likes} and you don't trust
{npc.dislikes}. Recent events: {witness_summary}. Stay in character.
Reply in 1-3 short sentences. Don't break the fourth wall.
```

**Options:**
- **Anthropic API (Claude)** — recommended. The codebase has the
  `userEmail` set, suggesting a developer with API access. Use
  `claude-haiku-4-5-20251001` for cost; fall back to Sonnet if
  responses feel flat. Keep request body in one helper.
- **OpenAI API** — fine alternative; same shape.
- **Local llama** (via llama.cpp + a small model like Qwen 2.5
  3B-Instruct quantized) — offline, free, slower. Useful for soak
  testing without burning API budget.
- **Stub** — pulls from `theme.json` `gossip_templates` /
  `greetings_pool` and rotates them deterministically. Used in tests
  + when no API key is configured.

**Cost guard:** cap LLM calls per minute per player; cache responses
keyed by `(npc_id, player_intent_summary)`. Prototype budget: 50¢ /
session if Anthropic.

## Stage 3 — Text-to-Speech (TTS)

**Engine options:**
- **Piper** (offline, MIT license, CC0 voices) — recommended default.
  Dozens of voices in many accents; ~25 MB per voice. Runs as a
  subprocess: `piper --model en_GB-alba-medium.onnx --output_file
  out.wav < text`. Voice list at
  <https://github.com/rhasspy/piper/blob/master/VOICES.md>.
- **Coqui TTS** — also offline; deprecated upstream but still works.
- **ElevenLabs API** — best quality, ~$0.18/1k chars; hundreds of
  voices including custom. Online only.
- **Azure / Google Cloud TTS** — similar shape.
- **System TTS** (Windows SAPI / macOS speech) — terrible quality,
  but free + always available. Useful as a stub.
- **Stub** — silent .wav of the right duration for transcript length;
  tests assert the file exists, not its content.

**Recommended default:** Piper offline + a curated set of ~30 voices
covering adult male / adult female / older male / older female /
gruff / soft / accented variants.

## Stage 4 — Voice catalog (many voices)

Each NPC needs a stable voice. Mirror the LPC bundle pick pattern:

```csharp
// New: ThemeData.PickVoiceId(string worldId, string npcId)
// Returns a deterministic voice id from npc.voice_options[] (new
// theme.json field), or falls back to a faction default.
```

**theme.json schema additions:**
```json
"voice_pools": {
  "law":      ["en_GB-alba-medium",  "en_US-ryan-high"],
  "outlaw":   ["en_US-libritts_r-medium-23",  "en_US-amy-low"],
  "chapel":   ["en_GB-aru-medium",  "en_GB-northern_english_male-medium"],
  "wayfarer": ["en_US-arctic-medium",  "en_US-l2arctic-medium"]
}
```

Each NPC's voice pool is its `RoleTags`; pick one deterministically
via `HashCode.Combine(worldId, npcId)`.

**Variety target:** ~30 distinct voices across the medieval roster of
60 NPCs. Some NPCs may share a voice — that's fine for a prototype.

**Voice-curation work for the audio agent:**
1. Download Piper voices (CC0 / public domain — check each model card).
2. Audition each voice with a stock medieval line (e.g.
   "By the king's order, mind the gate"). Keep voices that feel
   medieval-appropriate.
3. Commit voices under
   `assets/audio/voice/piper/<voice_id>.onnx` (+ `.json` config).
4. Populate `voice_pools` in
   `assets/themes/medieval/theme.json` (NEW — main session keeps the
   conflict zone updated; agent 2 must coordinate before editing).
5. Stretch: record/source extra distinctive voices for the named NPCs
   that should sound unique (Headmaster Braydon, Captain Wace, etc.).

## Stage 5 — Conversation orchestrator

A `ConversationOrchestrator` Node attached to the gameplay scene
listens for push-to-talk + the active dialogue NPC. On each
end-of-utterance:

1. Get `PlayerMicCapture.LastClipPath`.
2. Call `SpeechRecognizer.Transcribe(clip)` → text.
3. Call `NpcDialogueLLM.GenerateReply(npcId, text, history)` → text.
4. Call `NpcVoiceSynthesizer.Synthesize(voiceId, text)` → wav path.
5. Play through `PositionalAudioPlayer` anchored at NPC position.

Latency target: <2s end-to-end with stubs; <5s with full
local-offline backends; <3s with API backends.

## Privacy + opt-in

- **Push-to-talk only.** No always-on mic. Always-on is a non-starter
  for a multiplayer game.
- **Local-first when possible.** Whisper.cpp + Piper means no audio
  leaves the machine.
- **Settings panel toggle** to disable the conversational AI feature
  entirely (falls back to existing typed dialogue).
- Microphone permissions handled per-OS at first use.

## License rules

- Whisper.cpp = MIT.
- Piper = MIT; voices vary (most are public domain or CC0).
- ElevenLabs = proprietary; stock voices are licensed for game audio
  but check each one.
- Anthropic / OpenAI = service ToS; no licensing concern for output.
- Same audit hygiene: track per-voice license in
  `assets/audio/voice/CREDITS.md`.

## Out of scope (for now)

- Multi-NPC conversations (overlapping speakers).
- Voice cloning.
- Realtime streaming TTS (clip-by-clip is fine for the prototype).
- Spatial 3D-audio reverb processing.
