# Cleaned Gemini Static Props — 2026-04-27

Extracted from `assets/art/reference/gemini_prototypes/2026-04-27/karma_static_props_ref.jpg` during the old Gemini reference-sheet cleanup pass.

## Output

- 16 individual transparent PNG sprites
- 128x128 normalized canvases
- `gemini_static_props_cleaned_atlas.png` — 4x4 atlas, 128x128 cells
- `gemini_static_props_cleaned_contact.png` — dark-background review sheet
- `manifest.json` — sprite names, source cells, canvas size

## Usability

Good enough for prototype use after automated cleanup. Best immediate candidates:

- `airlock_door_front.png`
- `power_cell_canister.png`
- `portable_terminal.png`
- `compact_kiosk_terminal.png`
- `cargo_crate.png`
- `maintenance_hatch.png`
- `utility_junction_box.png`
- `station_wall_segment.png`

Keep but manually clean before prominent use:

- `repair_kit_case.png`
- `hydroponics_planter.png`
- `oxygen_tank_rack.png`
- `solar_panel.png`
- `pipe_cluster.png`
- `landing_beacon.png`
- `medical_supply_box.png`

Weakest extraction:

- `exterior_lamp_post.png` — thin pole/glow artifacts; likely needs redraw or manual cleanup.

## Known issues

The source was a JPG with a flat gray background. Automated cleanup removed the background, but many sprites still have dark edge halos, jagged alpha, JPG noise, and fragile thin details. Some mechanical details are AI-imprecise. These are useful prototype assets and references, not final production art.
