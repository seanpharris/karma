# Local LLM Runtime

This project can run NPC dialogue through a local GPU-backed `llama.cpp` server.

## Expected bundle layout

- `third_party/llama.cpp/bin/llama-server.exe`
- `third_party/models/phi-3.5-mini-instruct/phi-3.5-mini-instruct-q4.gguf`

These paths are configured in:
- `tools/env.ps1`

## Demo startup

The Mara TTS demo now attempts to start both services:
- Voicebox TTS server
- local `llama.cpp` server for dialogue replies

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-tts-demo.ps1
```

If the local Phi model is unavailable, the demo falls back to the existing stub dialogue backend.

## Runtime behavior

- `tools/start-llama-server.ps1` starts `llama-server.exe` hidden on `127.0.0.1:18080`
- the server is launched with:
  - model alias `phi-3.5-mini-instruct`
  - GPU layer offload enabled via `-ngl 999`
  - context size `4096`
- `tools/stop-llama-server.ps1` stops the local server

## API shape

The in-game client calls the OpenAI-compatible endpoint:
- `POST /v1/chat/completions`

This lets us swap in any future compatible local server with minimal code changes.
