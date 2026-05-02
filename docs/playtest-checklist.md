# Playtest Checklist

1. Launch the project and start the main playable prototype from the main menu.
2. Confirm the match begins fresh: the local player has 25 scrip, 0 karma, no completed quest progress, and the HUD is responsive.
3. Move around the starting area for at least 30 seconds and confirm movement, camera, minimap, stamina, hunger, and status strip updates feel stable.
4. Attack an NPC or valid combat target once, then confirm health, ammo or stamina, combat log, latest event text, and event icon update.
5. Talk to a nearby NPC and confirm the dialogue panel opens, choices render, and selecting a choice writes a local chat or dialogue event.
6. Open a vendor, confirm every vendor row has an item icon, buy one affordable item, and verify scrip decreases while the inventory row appears with an icon.
7. Open inventory, drag an item into a hotbar slot, and confirm the inventory row and hotbar slot both show item icons.
8. Pick up a world drop and verify it appears in inventory, respects capacity, and can be bound to the hotbar.
9. Play until the match summary appears or advance a test build to match end, then press the match summary's Return to Main Menu button.
10. Start a second match from the main menu and confirm the player starts fresh again with 25 scrip, 0 karma, no completed quest progress, and no leftover temporary status effects.
11. Spend five minutes in the default small world and watch density: the prototype should show 5 social stations and about 10 NPC profiles, enough to feel occupied without crowding the 80x72 map.
12. Keep `tools/test.ps1` green after any gameplay-loop change before doing a manual pass.
