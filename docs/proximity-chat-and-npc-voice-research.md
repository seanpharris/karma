# Proximity Chat and NPC Voice Research

This note captures the likely communication direction for Karma: local text chat,
proximity voice chat between players, and longer-term spoken NPC conversations.

Current priority decision: **stick with player-to-player proximity voice/text as
the practical feature path**. NPC speech-to-text / LLM / text-to-speech remains a
research backlog item until the player communication stack is stable.

## Goals

- Let nearby players communicate naturally without global voice chaos.
- Make distance matter: voices get quieter as players move apart.
- Preserve server authority for who can hear whom, while keeping voice transport
  efficient enough for a multiplayer prototype.
- Leave room for NPCs that can listen to a player and speak back with spatial
  volume, even if that stays research/prototype-only for a while.

## Player text chat

Text chat is the safest first communication feature.

Recommended channels:

- `Local` — visible only within proximity range.
- `Party/Posse` — visible to temporary team/posse members.
- `Faction` — optional later.
- `System` — server messages, world events, clinic delivery notices, etc.

For local text, the server should decide recipients by distance, not the client.
The client submits a chat intent; the authoritative server snapshots/events route
it only to eligible players.

Potential karma/social hooks:

- Rumorcraft can distort or amplify overheard local text.
- NPCs/factions can react to shouted/nearby public chat later.
- Moderation/report tooling is easier with text than raw voice.

## Player proximity voice chat

Proximity voice is feasible, but it is a separate networking/audio system from
normal server-authoritative gameplay messages.

Godot-relevant options found during research:

- Godot has `AudioStreamMicrophone` and audio buses for microphone capture.
- Godot supports WebRTC classes; native desktop builds require an external
  `webrtc-native` GDExtension, while browser exports have built-in WebRTC support.
- Community Godot VOIP projects such as `one-voip-godot-4` use microphone capture,
  Godot bus effects/audio streams, Opus compression, and packet push/playback.
  That project points to `two-voip-godot-4` as a more active successor.
- GodotSteam has Steam voice APIs for Steam-specific builds, including recording,
  compressed voice data, decompression, and playback.

### Recommended architecture

Use the gameplay server for **voice permissions and proximity metadata**, not raw
high-bandwidth audio mixing at first.

1. Client captures microphone with push-to-talk or voice activation.
2. Audio is encoded with Opus or platform voice API.
3. Voice packets are sent peer-to-peer, via a voice relay, or through a dedicated
   voice service depending on the final networking stack.
4. The authoritative game server periodically tells each client which speakers are
   audible and each speaker's relative distance/falloff.
5. Each receiving client plays one audio stream per remote speaker with volume and
   optional stereo pan based on server-approved proximity.

### Distance falloff

Use a clamped falloff curve instead of pure inverse square, because readable voice
in a top-down game should feel designed rather than physically exact.

Suggested prototype values:

- `clearRadius`: within 3-4 tiles, voice is full volume.
- `fadeRadius`: from 4-14 tiles, voice smoothly fades.
- `maxRadius`: beyond 14-18 tiles, voice is inaudible.
- Optional `shout` mode later can increase radius with social/stealth tradeoffs.

Example normalized volume:

```text
if distance <= clearRadius: volume = 1.0
if distance >= maxRadius: volume = 0.0
otherwise:
  t = (distance - clearRadius) / (maxRadius - clearRadius)
  volume = (1 - smoothstep(t)) * speakerVolume * listenerVoiceVolume
```

For 2D presentation:

- Volume communicates distance.
- Stereo pan can lightly reflect left/right position.
- Avoid aggressive panning; players may use mono speakers/headsets.
- Occlusion through walls/structures can be added later as a multiplier, not a
  first-pass requirement.

### UX and safety requirements

Voice needs more safety/settings than most features:

- Push-to-talk default for early builds.
- Mute self / mute player / block player.
- Per-player voice volume.
- Global voice volume slider.
- Visual speaking indicator over nearby players.
- Accessibility: text chat remains available; optional speech-to-text later.
- Abuse reporting strategy before public playtests.
- Clear mic permission flow and no recording without explicit consent.

## NPC voice conversations

NPC voice conversations are intentionally **research/to-do**, not the first
implementation target. The current NPC interaction model remains: walk up to an
NPC, inspect their prompt, and choose from a few server-generated options.

The long-term idea is to make that feel more organic without losing game-state
authority:

- NPCs can greet the player with a contextual line or exclamation when approached.
- The player can still choose options, type a freeform line, or eventually speak
  a line through push-to-talk speech-to-text.
- An LLM-style dialogue layer can respond more naturally using bounded context:
  NPC personality, station state, quest state, faction, relationship, and player
  karma.
- The response text can later be voiced through TTS and played spatially from the
  NPC's world position.

The player-to-NPC voice idea is plausible, but it is a larger research track than
player proximity voice. It has four separate hard problems:

1. **Speech-to-text** — convert player speech to text.
2. **Dialogue brain** — decide what the NPC says using scripted dialogue,
   generated quest state, faction/karma context, or an LLM-like service.
3. **Text-to-speech** — synthesize NPC spoken audio.
4. **Spatial playback** — play the NPC response from the NPC position with the
   same distance falloff as proximity voice.

The last part is very doable in Godot: once we have an audio clip/stream, play it
through an NPC-attached audio player and apply the same 2D volume falloff model.
The harder parts are live STT/TTS latency, cost, privacy, moderation, and keeping
NPC dialogue grounded in server-owned game state.

### Recommended NPC approach

Start with a more natural **text-first interaction shell**, then voice-enable later:

1. Player approaches NPC.
2. NPC may emit a short contextual greeting/exclamation, e.g. station stabilized,
   station compromised, faction-friendly, suspicious, quest-ready, etc.
3. Player opens NPC interaction.
4. Player can choose from generated choices, type a freeform line, or eventually
   speak via STT.
5. Server sends compact NPC context:
   - NPC id/personality/faction
   - station state
   - quest state
   - player karma/faction/relationship context
   - recent safe conversation summary, if any
6. Dialogue system returns text constrained to allowed intents/options.
7. Server-owned systems decide any real quest/reward/karma/item changes.
8. Optional later: TTS turns the returned text into audio.
9. NPC audio plays spatially with proximity falloff and subtitles.

For spoken input later:

1. Player holds an `Ask NPC` push-to-talk key while near the NPC.
2. Client captures mic audio.
3. STT returns text.
4. The normal text NPC pipeline handles the response.
5. TTS response plays from the NPC's world position.

### NPC voice constraints

- NPCs must not invent game authority. Generated speech can suggest flavor, but
  quests/rewards/items/karma changes must come from server-owned systems.
- Avoid always-on NPC listening. Use explicit interaction/push-to-talk.
- Cache common NPC lines and TTS outputs to reduce cost/latency.
- Provide subtitles for every NPC voice line.
- Keep a non-voice fallback for accessibility and offline/dev builds.

## Open research questions

- Which target multiplayer stack do we prefer for Karma long term: Steam,
  WebRTC, ENet plus a separate voice relay, or a hosted voice service?
- Is a community Godot VOIP extension mature enough for Godot 4.6 desktop builds?
- How much voice traffic should pass through our servers vs peer-to-peer?
- What moderation tools are required before wider playtesting?
- Which STT/TTS provider is acceptable for prototype NPC voice, if any?
- Can we support local-only/offline generated NPC text without external services?

## Prototype order

1. Implement local text chat routed by server proximity.
2. Add chat bubbles and/or compact local chat log.
3. Add server-owned audibility model: who can hear whom and at what falloff.
4. Prototype proximity voice locally with fake/generated audio sources first.
5. Evaluate a Godot 4 VOIP extension or Steam voice path.
6. Add push-to-talk proximity voice for player-to-player only.
7. Add NPC text conversation backed by existing generated NPC/quest/state systems.
8. Research STT/TTS and play NPC TTS spatially as an optional experiment.
