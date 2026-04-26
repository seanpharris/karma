# Prototype Model Art Prompts

Paste these prompts into ChatGPT/image tools to generate cleaner art for the
current Karma prototype models. Keep outputs license-safe/original. Treat first
outputs as **reference art** unless they exactly match the runtime contract.

## Shared Style Block

Use this at the top of any prompt below if the tool allows longer prompts:

```text
Game: Karma, a top-down 2D multiplayer life-sim RPG about Ascension/Descension karma choices.
Style: compact readable pixel art, sci-fi frontier colony, cozy but slightly weird, clean silhouettes, crisp nearest-neighbor pixels, no painterly blur.
Camera: top-down RPG / three-quarter top-down object view.
Runtime rules: transparent background preferred; if impossible use flat #00ff00 chroma key. No labels, text, UI panels, metadata, watermarks, grid lines, decorative borders, shadows baked into the background, or prompt notes.
Asset should feel original and license-safe, not copied from an existing game.
```

## Character Sheet Runtime Contract

Use this contract for player/NPC character sheets:

```text
STRICT CHARACTER SHEET FORMAT:
- PNG sprite sheet only.
- Canvas exactly 256x288 px if possible.
- 8 columns x 9 rows.
- Each frame exactly 32x32 px.
- Direction columns left-to-right: front, front-right, right, back-right, back, back-left, left, front-left.
- Row 0: idle pose.
- Rows 1-4: four-frame walking cycle.
- Row 5: run/action-ready pose.
- Row 6: tool/use pose.
- Row 7: melee/impact pose.
- Row 8: interact/reach pose.
- Feet bottom-centered in every frame.
- Character proportions consistent across all frames.
- True 8-direction rotation: diagonals must be real three-quarter views, not copies of side/front/back frames.
```

---

## Characters

### Local Player: Sci-Fi Frontier Engineer

```text
Create a clean 2D pixel-art top-down RPG character runtime sprite sheet for Karma.

Subject: player character, sci-fi frontier engineer / colony repair tech, practical jumpsuit, utility belt, compact backpack or tool harness, readable friendly silhouette, teal/cyan accent lights, rugged boots, no helmet.

Use the Shared Style Block and STRICT CHARACTER SHEET FORMAT.

Important visual notes:
- Should look like a capable frontier mechanic/explorer.
- Outfit should support repair, tool use, and social interactions.
- Avoid bulky armor; keep silhouette readable at 32x32.
- Make diagonal frames visibly distinct.
```

### Mara Venn: Clinic / Repair NPC

```text
Create a clean 2D pixel-art top-down RPG character runtime sprite sheet for Karma.

Subject: Mara Venn, warm but exhausted frontier clinic technician, amber/orange work clothes, teal medical/repair accents, short practical hair or head wrap, tool pouch, small med patch satchel, kind but no-nonsense posture.

Use the Shared Style Block and STRICT CHARACTER SHEET FORMAT.

Important visual notes:
- Must read as a healer/repair-helper NPC at tiny scale.
- Blend medical clinic and repair-yard vibes.
- Avoid fantasy robes; this is sci-fi frontier colony gear.
- Make idle and interact/reach poses feel welcoming/helpful.
```

### Dallen Venn: Tense Civilian / Rival NPC

```text
Create a clean 2D pixel-art top-down RPG character runtime sprite sheet for Karma.

Subject: Dallen Venn, guarded frontier civilian / worried rival NPC, muted blue-gray jacket, tan utility scarf, practical boots, tense posture, slightly suspicious expression, compact silhouette.

Use the Shared Style Block and STRICT CHARACTER SHEET FORMAT.

Important visual notes:
- Should contrast with Mara: cooler colors, more closed-off body language.
- Not a villain; more anxious, protective, distrustful.
- Interact/reach pose should feel like pointing, warning, or reluctant negotiation.
```

### Stranded Peer Player / Generic Multiplayer Stand-In

```text
Create a clean 2D pixel-art top-down RPG character runtime sprite sheet for Karma.

Subject: stranded peer player stand-in, sci-fi frontier traveler, purple/gray improvised clothes, dusty survival pack, patched sleeves, readable neutral multiplayer silhouette.

Use the Shared Style Block and STRICT CHARACTER SHEET FORMAT.

Important visual notes:
- Should be visually distinct from the local engineer but same scale/proportions.
- Good as a generic other-player model before customization exists.
- Keep outfit modular-looking for future paper-doll layers.
```

---

## Item Atlas Prompt

Use this when generating small pickup/quest item art. The current prototype can
use individual icons or sliced atlases; clean transparent icons are easiest to
curate.

```text
Create a single clean pixel-art item icon sheet for Karma.

Canvas: square or compact atlas, transparent background, no labels/text/grid.
Style: top-down/three-quarter pixel-art items, sci-fi frontier colony, readable at 16-32 px in game.
Include each object separated with generous transparent padding.
Objects to include:
1. ration pack — compact tan survival food packet
2. data chip — cyan glowing memory wafer
3. filter core — small cylindrical air/water filter cartridge with teal center
4. contraband package — suspicious dark wrapped parcel with red warning ties
5. apology flower — small bright flower in a rough sci-fi planter/pot
6. portable terminal — chunky handheld screen device with cyan display
7. scrip — small brass/credit coin or token stack

Rules: original license-safe art, crisp pixels, transparent background, no words, no logos, no labels, no watermark.
```

### Individual Item Prompts

```text
Create one transparent pixel-art game item icon for Karma: a tan sci-fi frontier ration pack, compact wrapped survival food packet, readable at 24x24, crisp pixels, no text, no label, no background.
```

```text
Create one transparent pixel-art game item icon for Karma: a cyan glowing data chip / memory wafer, tiny sci-fi circuit detail, readable at 18x18 to 24x24, crisp pixels, no text, no background.
```

```text
Create one transparent pixel-art game item icon for Karma: a small cylindrical filter core cartridge, gray metal shell with teal filter glow, readable at 18x22, crisp pixels, no text, no background.
```

```text
Create one transparent pixel-art game item icon for Karma: a suspicious contraband package, dark wrapped parcel with red hazard ties, readable at 22x18, crisp pixels, no text, no background.
```

```text
Create one transparent pixel-art game item icon for Karma: an apology flower in a small rugged sci-fi planter, pink/yellow flower, readable at 20x24, crisp pixels, no text, no background.
```

```text
Create one transparent pixel-art game item icon for Karma: a chunky portable terminal, dark handheld device with cyan screen and small amber buttons, readable at 24x22, crisp pixels, no text, no background.
```

---

## Utility / Joke / Support Item Atlas Prompt

```text
Create a clean transparent pixel-art item icon sheet for Karma's weird utility/support items.

Style: sci-fi frontier colony, cozy but absurd, readable 16-32 px icons, crisp nearest-neighbor pixels, no labels/text/grid/watermark.
Objects to include, separated with transparent padding:
1. whoopie cushion — red prank cushion with small nozzle
2. deflated balloon — limp pink/purple balloon scrap
3. repair kit — teal compact repair/med-style kit with cross-like tool mark but no text
4. practice stick — simple wooden training baton/stick
5. work vest — orange utility safety vest folded or icon-ready
6. scrip token — brass sci-fi currency token

Transparent background. Original license-safe art only.
```

---

## Weapon Atlas Prompt

```text
Create a clean transparent pixel-art weapon icon atlas for Karma.

Style: sci-fi frontier improvised weapons, readable at 24-40 px, top-down/side three-quarter item icons, crisp pixels, no labels/text/grid/watermark.
Objects to include, separated with transparent padding:
1. stun baton — short black/metal baton with blue electric tip
2. electro pistol — compact pistol with cyan coil accents
3. SMG-11 — small sci-fi submachine gun, dark metal, cyan accent
4. shotgun mk1 — chunky frontier shotgun, worn metal and grip
5. rifle-27 — long practical colony rifle
6. sniper X9 — long precision rifle with small scope
7. plasma cutter — industrial tool-weapon with glowing cutting head
8. flamethrower — compact tank-and-nozzle weapon, orange accent
9. grenade launcher — stubby launcher with drum/chamber
10. railgun — sleek long electromagnetic rifle with blue rails
11. impact mine — small disk mine with warning color accents but no symbols/text
12. EMP grenade — small sci-fi grenade with blue pulse core

Transparent background. Original license-safe art only.
```

## Tool Atlas Prompt

```text
Create a clean transparent pixel-art tool icon atlas for Karma.

Style: sci-fi frontier repair/survival tools, readable at 20-36 px, crisp pixels, no labels/text/grid/watermark.
Objects to include, separated with transparent padding:
1. multi-tool — compact folding sci-fi utility tool
2. welding torch — handheld repair torch with blue flame/nozzle
3. medi patch — small medical patch injector/packet
4. lockpick set — compact electronic lockpick kit
5. flashlight — rugged frontier flashlight with blue-white lens
6. portable shield — folded shield generator puck/bracelet
7. hacking device — small black/cyan cracking module
8. scanner — handheld scanner with glowing display
9. grappling hook — compact launcher/hook device
10. chem injector — small injector vial tool
11. power cell — glowing battery cell
12. bolt cutters — compact heavy cutters
13. magnetic grabber — telescoping magnet grabber tool

Transparent background. Original license-safe art only.
```

---

## Structure / Station Prompt

Use this for replacing placeholder station/fixture visuals.

```text
Create a clean transparent pixel-art top-down/three-quarter prop and structure atlas for Karma, sci-fi frontier colony style.

Canvas: compact atlas with transparent background, objects separated with padding, no labels/text/grid/watermark.
Objects to include:
1. clinic marker sign — small medical/repair clinic sign, no text, cross-like icon allowed if abstract
2. market stall marker — small barter kiosk/stall
3. repair yard fixture — filter stack / machine console that can be repaired or sabotaged
4. rumor board — public notice board with papers but no readable writing
5. saloon/social hub sign — neon-ish social station prop, no text
6. restricted shed marker — locked storage shed/door prop
7. oddity yard marker — strange fenced relic display
8. duel ring marker — floor circle/marker prop
9. farm plot marker — compact hydroponic/farm bed
10. black market marker — shady tarp-covered kiosk
11. apology engine — weird machine with heart/gear motif, no text
12. broadcast tower base — small antenna console
13. war memorial marker — abstract memorial slab, no text
14. witness court marker — small civic podium/marker

Style: readable at small game scale, cozy sci-fi frontier, original license-safe pixel art, transparent background.
```

---

## Generated NPC Role Prompt Template

Use this when making a batch of generated NPC role variants.

```text
Create a clean 2D pixel-art top-down RPG character runtime sprite sheet for Karma.

Subject: <NPC role>, from a <station type> in a sci-fi frontier colony.
Personality: <friendly / suspicious / exhausted / flashy / nervous / stern>.
Outfit: <short outfit description>.
Color identity: <2-3 key colors>.
Gameplay read: should immediately communicate <medic / trader / repair worker / rumor broker / farmer / guard / black-market dealer / witness clerk>.

Use the Shared Style Block and STRICT CHARACTER SHEET FORMAT.

Important:
- True 8-direction rotation.
- Compact readable silhouette at 32x32.
- No labels, no background, no text, no grid.
```

Example subjects:

- clinic medic with teal/white utility coat
- market trader with yellow scarf and cargo apron
- repair yard mechanic with orange work vest and welding mask pushed up
- rumor broker with purple coat and portable radio headset
- saloon host with warm red jacket and neon pin
- restricted shed guard with gray armor vest
- hydroponic farmer with green utility overalls
- black-market dealer with dark coat and hidden satchel
- witness court clerk with blue civic sash and tablet

## Validation Reminder

After saving generated art into the repo:

```bash
python3 tools/audit_art_library.py
python3 tools/prepare_character_sheet.py validate assets/art/sprites/<character_sheet>.png
```

Then run the gameplay checks before wiring anything into code:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test.ps1
```
