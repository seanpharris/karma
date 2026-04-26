# True 8-Direction Engineer Sprite Prompt

Use this in ChatGPT/image generation when OpenClaw image generation is unavailable.

```text
Create a clean 2D pixel-art top-down RPG character runtime sprite sheet for the game Karma.

Character: sci-fi frontier engineer, compact readable 32x32 pixel-art proportions, brown hair, rugged utility jumpsuit, boots, small toolbelt/backpack details.

STRICT OUTPUT CONTRACT:
- PNG sprite sheet only.
- Exact clean runtime grid, no surrounding presentation UI.
- 8 columns x 9 rows.
- Each frame is exactly 32x32 px.
- Total sheet should be 256x288 px if possible.
- Transparent background. If transparency is impossible, use flat #00ff00 chroma key.
- No labels, no text, no grid lines, no side panels, no metadata, no preview boxes, no watermark, no decorative border.

Columns left-to-right:
1 front
2 front-right true three-quarter view
3 right side view
4 back-right true three-quarter rear view
5 back view
6 back-left true three-quarter rear view
7 left side view
8 front-left true three-quarter view

Rows top-to-bottom:
1 idle
2 walk frame 1
3 walk frame 2
4 walk frame 3
5 walk frame 4
6 run/action-ready
7 tool/use or shoot-ready
8 melee/impact
9 interact/reach

TRUE 8-DIRECTION REQUIREMENT:
- Do not duplicate side-facing sprites for diagonal directions.
- Front-right/front-left must show face and chest partly turned.
- Back-right/back-left must show back and shoulder partly turned.
- Side views should be profile only.
- Back view should clearly show rear of head/body.
- Diagonal poses should have distinct head, torso, shoulder, arm, leg, and foot angles.
- Keep feet aligned bottom-center in every frame.
- Keep silhouette size consistent so animation does not pop or jitter.
- Crisp nearest-neighbor pixel art, readable dark outlines, no painterly blur.
```

After generating, place the output somewhere local and run:

```bash
python3 tools/prepare_character_sheet.py validate path/to/generated.png
python3 tools/prepare_character_sheet.py normalize path/to/generated.png assets/art/sprites/scifi_engineer_player_8dir.png --chroma
python3 tools/prepare_character_sheet.py validate assets/art/sprites/scifi_engineer_player_8dir.png
```
