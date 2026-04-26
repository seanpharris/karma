# Karma Worldbuilding Themes

This directory contains generation schemas and guidelines for creating themed Karma worlds. Each theme defines specific locations, roles, and story rules for LLM-based world generation.

## Available Themes

### Space
- **File**: `space-generation.json`
- **Tileset**: `assets/themes/space/tileset.png`
- **Description**: Cozy, eerie sci-fi stations with isolation, mystery, and system repair themes
- **Core Locations**: Station hub, engineering bay, habitat ring
- **Key Roles**: Commander, medic, engineer, quartermaster, hydroponics keeper
- **Tone**: Cozy, absurd, socially reactive, occasionally dark, isolated, mysterious

### Western
- **File**: `western-generation.json`
- **Tileset**: `assets/themes/western/tileset.png`
- **Description**: Cozy, absurd frontier towns with themes of law, survival, and moral ambiguity
- **Core Locations**: Town center, Red Ridge mine, Sunset homestead
- **Key Roles**: Sheriff, doctor, saloon keeper, ranch owner, miner
- **Tone**: Cozy, absurd, socially reactive, occasionally dark

### Boarding School
- **File**: `boarding_school-generation.json`
- **Tileset**: `assets/themes/boarding_school/tileset.png`
- **Description**: Cozy, whimsical school setting with social tension, secrets, and gossip
- **Core Locations**: Courtyard, dormitory, classroom wing
- **Key Roles**: Headmaster, teacher, top student, troublemaker, caretaker
- **Tone**: Cozy, absurd, socially reactive, occasionally dark, gossipy, emotionally charged

## How to Use for LLM Generation

1. Point the LLM to the appropriate `{theme}-generation.json` file
2. Include the `generation_instruction` field at the end of the schema
3. The LLM should output valid JSON matching the `output_format` structure
4. Generated worlds should respect all `fixed_world_rules` and `story_generation_rules`

## Adding a New Theme

1. Create a new folder in `assets/themes/{theme_name}/`
2. Create `{theme_name}-generation.json` in this directory
3. Add tileset image to `assets/themes/{theme_name}/`
4. Document the theme here with description, locations, and key roles
5. Ensure `generation_instruction` is clear and specific

## Theme Template

Use `space-generation.json` as a template for new themes. Key sections:
- `generation_request`: Theme-specific settings and tone
- `fixed_world_rules`: Karma system, world size, relationship tracking
- `available_locations`: Story-critical locations and buildings
- `required_roles`: NPCs that must exist in the world
- `optional_roles_pool`: NPCs that may be generated
- `story_generation_rules`: Constraints for NPCs, quests, and objects
