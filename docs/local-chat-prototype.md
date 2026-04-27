# Local Chat Prototype

This is the first implementation step toward player-to-player proximity voice.
It intentionally starts with server-owned local text chat and an audibility model,
without microphone capture or VOIP networking yet.

## What exists now

- New server intent: `SendLocalChat`.
- Payload:
  - `text` — local message text.
- The authoritative server sanitizes whitespace, truncates messages to
  `AuthoritativeWorldServer.LocalChatMaxMessageLength`, stores the message, and
  emits a `local_chat` server event.
- Interest snapshots now include `LocalChatMessages`, filtered by listener
  distance.
- Each local chat snapshot includes:
  - message id/tick,
  - speaker id/name,
  - text,
  - speaker tile position,
  - listener distance in tiles,
  - normalized volume.
- The HUD shows the latest audible local chat line.
- Press `/` or `T` in gameplay to open the local chat entry; press Enter to send
  or Esc to cancel.
- Recent audible messages render as lightweight world-space chat bubbles above
  speakers for a short prototype window.
- The developer overlay Events page lists recent audible local chat with distance
  and volume.

## Current falloff model

Prototype constants:

- Full volume through `LocalChatClearRadiusTiles = 4`.
- Smooth fade until `LocalChatMaxRadiusTiles = 18`.
- Inaudible at or beyond max radius.

This matches the future proximity voice behavior: text first, then voice can reuse
the same server-approved audibility metadata.

## Next steps

1. Add `Local`, `Posse`, and `System` chat tabs or filters.
2. Decide server-side message expiry/pruning instead of keeping the prototype log
   for the whole session.
3. Improve bubble styling with a proper panel/tail and text-safe contrast.
4. Feed the same falloff/audibility model into a fake audio-source prototype.
5. Evaluate real VOIP transport after gameplay-side audibility feels right.
