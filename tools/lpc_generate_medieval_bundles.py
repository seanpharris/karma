#!/usr/bin/env python3
"""Generate LPC theme-bundle JSONs for the medieval NPC roster.

Reads a compact spec table below and writes one JSON per bundle into
`assets/art/sprites/lpc/themes/`. Re-runnable; will overwrite existing
files. Each bundle resolves to LPC layer paths under
`assets/art/sprites/lpc/spritesheets/`.

Run from repo root:

  python3 tools/lpc_generate_medieval_bundles.py

Bundle paths are best-effort guesses against the LPC layout. The composer
will log a warning and skip any layer whose .png doesn't exist on disk.
Tune the spec or check the LPC `sheet_definitions/<category>/<variant>.json`
file for canonical paths if a layer is missing.
"""

from __future__ import annotations

import json
import random
import shutil
from functools import lru_cache
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
THEMES_DIR = REPO / "assets" / "art" / "sprites" / "lpc" / "themes"
LPC_SPRITESHEETS = REPO / "assets" / "art" / "sprites" / "lpc" / "spritesheets"

# (id, display, tags, body_kind, torso, legs, feet, hair, weapon, head_kind)
# - body_kind:  "male" | "female"
# - head_kind:  "human/male" | "human/female" | "human/male_elderly" | "human/female_elderly" | "human/child"
# - torso/legs/feet/hair: paths under spritesheets/<category>/, the variant's
#   subfolder (we append "/walk.png" automatically — most LPC variants ship
#   walk).
# - weapon: full path under spritesheets/weapon/ that ends in .png, OR
#   empty string for no weapon.
SPECS = [
    # ── Authority / Law ────────────────────────────────────────────────
    ("guard_captain_male",  "Guard Captain (M)",  ["medieval","law","melee"],
        "male",   "torso/armour/plate/male",                 "legs/pants/thin",        "feet/boots/basic/male",
        "hair/buzzcut/adult",       "weapon/sword/arming/universal/fg/walk/steel.png",  "human/male"),
    ("guard_male",          "Guard (M)",           ["medieval","law"],
        "male",   "torso/armour/legion/male",                "legs/pants/thin",        "feet/boots/basic/male",
        "hair/buzzcut/adult",       "weapon/polearm/spear/walk/foreground.png",                       "human/male"),
    ("guard_female",        "Guard (F)",           ["medieval","law"],
        "female", "torso/armour/legion/female",              "legs/pants/thin",        "feet/boots/basic/male",
        "hair/bob/adult",     "weapon/polearm/spear/walk/foreground.png",                       "human/female"),
    ("noble_male",          "Magistrate / Noble",  ["medieval","law"],
        "male",   "torso/clothes/longsleeve/longsleeve_brown/male",  "legs/pants/thin",  "feet/shoes/basic/male",
        "hair/buzzcut/adult",       "",                                                          "human/male"),
    ("executioner_male",    "Executioner",         ["medieval","law"],
        "male",   "torso/armour/leather/male",               "legs/pants/thin",        "feet/boots/basic/male",
        "hair/balding/adult",       "weapon/blunt/waraxe/walk/waraxe.png",                       "human/male"),
    ("ranger_male",         "Ranger",              ["medieval","law","ranged"],
        "male",   "torso/armour/leather/male",               "legs/pants/thin",        "feet/boots/basic/male",
        "hair/messy1/adult",      "weapon/ranged/bow/great/walk/foreground/iron.png",         "human/male"),
    ("knight_male",         "Knight-Errant",       ["medieval","law","melee"],
        "male",   "torso/armour/plate/male",                 "legs/pants/thin",        "feet/boots/basic/male",
        "hair/buzzcut/adult",       "weapon/sword/arming/universal/fg/walk/silver.png", "human/male"),
    ("squire_female",       "Squire (F)",          ["medieval","law"],
        "female", "torso/armour/leather/female",             "legs/pants/thin",        "feet/boots/basic/male",
        "hair/bob/adult",     "",                                                          "human/female"),

    # ── Trade / Craft ──────────────────────────────────────────────────
    ("blacksmith_male",     "Blacksmith",          ["medieval","trade"],
        "male",   "torso/clothes/sleeveless/sleeveless2_vneck/male",  "legs/pants/thin", "feet/boots/basic/male",
        "hair/buzzcut/adult",       "weapon/blunt/mace/walk/mace.png",                           "human/male"),
    ("fletcher_female",     "Fletcher (F)",        ["medieval","trade"],
        "female", "torso/clothes/longsleeve/longsleeve/female",       "legs/pants/thin", "feet/shoes/basic/male",
        "hair/long/adult",       "weapon/ranged/bow/great/walk/foreground/iron.png",         "human/female"),
    ("tanner_male",         "Tanner",              ["medieval","trade"],
        "male",   "torso/clothes/sleeveless/sleeveless2_vneck/male",  "legs/pants/thin", "feet/shoes/basic/male",
        "hair/buzzcut/adult",       "",                                                          "human/male"),
    ("miller_male",         "Miller",              ["medieval","trade"],
        "male",   "torso/clothes/longsleeve/scoop/male",     "legs/pants/thin",        "feet/shoes/basic/male",
        "hair/buzzcut/adult",       "",                                                          "human/male"),
    ("baker_female",        "Baker (F)",           ["medieval","trade"],
        "female", "torso/clothes/longsleeve/longsleeve/female",       "legs/skirts/slit/thin", "feet/shoes/basic/male",
        "hair/bob/adult",     "",                                                          "human/female"),
    ("tailor_female",       "Tailor (F)",          ["medieval","trade"],
        "female", "torso/clothes/longsleeve/longsleeve/female",       "legs/skirts/slit/thin", "feet/shoes/basic/male",
        "hair/long/adult",       "",                                                          "human/female"),
    ("carpenter_male",      "Carpenter",           ["medieval","trade"],
        "male",   "torso/clothes/longsleeve/scoop/male",     "legs/pants/thin",        "feet/boots/basic/male",
        "hair/buzzcut/adult",       "",                                                          "human/male"),
    ("mason_male",          "Mason",               ["medieval","trade"],
        "male",   "torso/clothes/sleeveless/sleeveless2_vneck/male",  "legs/pants/thin", "feet/boots/basic/male",
        "hair/buzzcut/adult",       "",                                                          "human/male"),
    ("brewer_male",         "Brewer",              ["medieval","trade"],
        "male",   "torso/clothes/longsleeve/scoop/male",     "legs/pants/thin",        "feet/shoes/basic/male",
        "hair/buzzcut/adult",       "",                                                          "human/male"),
    ("butcher_male",        "Butcher",             ["medieval","trade"],
        "male",   "torso/clothes/sleeveless/sleeveless2_vneck/male",  "legs/pants/thin", "feet/shoes/basic/male",
        "hair/buzzcut/adult",       "weapon/blunt/club/club.png",                                "human/male"),
    ("herbalist_female",    "Herbalist (F)",       ["medieval","wild"],
        "female", "torso/clothes/longsleeve/longsleeve/female",       "legs/skirts/slit/thin", "feet/shoes/basic/male",
        "hair/long/adult",       "",                                                          "human/female"),
    ("apothecary_male",     "Apothecary",          ["medieval","trade"],
        "male",   "torso/clothes/longsleeve/longsleeve_brown/male",   "legs/pants/thin", "feet/shoes/basic/male",
        "hair/buzzcut/adult",       "",                                                          "human/male"),

    # ── Hospitality / Service ─────────────────────────────────────────
    ("tavernkeeper_female", "Tavernkeeper (F)",    ["medieval","trade"],
        "female", "torso/clothes/longsleeve/longsleeve/female",       "legs/skirts/slit/thin", "feet/shoes/basic/male",
        "hair/long/adult",       "",                                                          "human/female"),
    ("innkeeper_male",      "Innkeeper",           ["medieval","trade"],
        "male",   "torso/clothes/longsleeve/longsleeve_brown/male",   "legs/pants/thin", "feet/shoes/basic/male",
        "hair/buzzcut/adult",       "",                                                          "human/male"),
    ("stablehand_female",   "Stable-Hand (F)",     ["medieval","trade"],
        "female", "torso/clothes/sleeveless/sleeveless2_vneck/female","legs/pants/thin", "feet/boots/basic/male",
        "hair/bob/adult",     "",                                                          "human/female"),
    ("midwife_female",      "Midwife",             ["medieval","wild"],
        "female", "torso/clothes/longsleeve/longsleeve/female",       "legs/skirts/slit/thin", "feet/shoes/basic/male",
        "hair/long/adult",       "",                                                          "human/female_elderly"),
    ("undertaker_female",   "Undertaker",          ["medieval","chapel"],
        "female", "torso/clothes/longsleeve/longsleeve_brown/female","legs/skirts/slit/thin", "feet/shoes/basic/male",
        "hair/long/adult",       "",                                                          "human/female"),
    ("washerwoman_female",  "Washerwoman",         ["medieval","trade"],
        "female", "torso/clothes/longsleeve/longsleeve/female",       "legs/skirts/slit/thin", "feet/shoes/basic/male",
        "hair/bob/adult",     "",                                                          "human/female"),

    # ── Commerce / Wayfarers ──────────────────────────────────────────
    ("merchant_male",       "Travelling Merchant", ["medieval","wayfarer"],
        "male",   "torso/clothes/longsleeve/longsleeve_brown/male",   "legs/pants/thin", "feet/boots/basic/male",
        "hair/buzzcut/adult",       "",                                                          "human/male"),
    ("merchant_female",     "Foreign Merchant",    ["medieval","wayfarer"],
        "female", "torso/clothes/longsleeve/longsleeve/female",       "legs/skirts/slit/thin", "feet/boots/basic/male",
        "hair/long/adult",       "",                                                          "human/female"),
    ("scholar_male",        "Wandering Scholar",   ["medieval","wayfarer"],
        "male",   "torso/clothes/longsleeve/longsleeve_brown/male",   "legs/pants/thin", "feet/shoes/basic/male",
        "hair/messy1/adult",      "",                                                          "human/male_elderly"),
    ("alchemist_male",      "Alchemist",           ["medieval","wayfarer"],
        "male",   "torso/clothes/longsleeve/longsleeve_brown/male",   "legs/pants/thin", "feet/shoes/basic/male",
        "hair/messy1/adult",      "",                                                          "human/male"),
    ("fortune_teller_female","Fortune-Teller",     ["medieval","wayfarer"],
        "female", "torso/clothes/longsleeve/longsleeve/female",       "legs/skirts/slit/thin", "feet/shoes/basic/male",
        "hair/long/adult",       "",                                                          "human/female"),
    ("bard_female",         "Wandering Bard (F)",  ["medieval","wayfarer"],
        "female", "torso/clothes/longsleeve/longsleeve/female",       "legs/pants/thin", "feet/boots/basic/male",
        "hair/long/adult",       "",                                                          "human/female"),
    ("mercenary_male",      "Mercenary (M)",       ["medieval","wayfarer","melee"],
        "male",   "torso/armour/leather/male",               "legs/pants/thin",        "feet/boots/basic/male",
        "hair/buzzcut/adult",       "weapon/sword/arming/universal/fg/walk/iron.png",   "human/male"),
    ("pilgrim_female",      "Pilgrim (F)",         ["medieval","wayfarer","chapel"],
        "female", "torso/clothes/longsleeve/longsleeve_brown/female",  "legs/skirts/slit/thin", "feet/shoes/basic/male",
        "hair/long/adult",       "",                                                          "human/female"),

    # ── Worship ────────────────────────────────────────────────────────
    ("priest_male",         "Priest",              ["medieval","chapel"],
        "male",   "torso/clothes/longsleeve/longsleeve_brown/male",   "legs/pants/thin", "feet/shoes/basic/male",
        "hair/balding/adult",       "",                                                          "human/male"),
    ("monk_male",           "Monk",                ["medieval","chapel"],
        "male",   "torso/clothes/longsleeve/longsleeve_brown/male",   "legs/pants/thin", "feet/shoes/basic/male",
        "hair/balding/adult",       "",                                                          "human/male"),
    ("monk_elder_male",     "Abbot",               ["medieval","chapel"],
        "male",   "torso/clothes/longsleeve/longsleeve_brown/male",   "legs/pants/thin", "feet/shoes/basic/male",
        "hair/balding/adult",       "",                                                          "human/male_elderly"),
    ("acolyte_female",      "Acolyte",             ["medieval","chapel"],
        "female", "torso/clothes/longsleeve/longsleeve_brown/female","legs/skirts/slit/thin", "feet/shoes/basic/male",
        "hair/long/adult",       "",                                                          "human/female"),

    # ── Country folk ──────────────────────────────────────────────────
    ("farmer_female",       "Farmer (F)",          ["medieval","peasant"],
        "female", "torso/clothes/longsleeve/longsleeve/female",       "legs/skirts/slit/thin", "feet/shoes/basic/male",
        "hair/bob/adult",     "",                                                          "human/female"),
    ("farmer_male",         "Farmer (M)",          ["medieval","peasant"],
        "male",   "torso/clothes/longsleeve/scoop/male",     "legs/pants/thin",        "feet/shoes/basic/male",
        "hair/buzzcut/adult",       "",                                                          "human/male"),
    ("shepherd_male",       "Shepherd",            ["medieval","peasant"],
        "male",   "torso/clothes/longsleeve/scoop/male",     "legs/pants/thin",        "feet/boots/basic/male",
        "hair/buzzcut/adult",       "",                                                          "human/male"),
    ("woodcutter_male",     "Woodcutter",          ["medieval","peasant"],
        "male",   "torso/clothes/sleeveless/sleeveless2_vneck/male",  "legs/pants/thin", "feet/boots/basic/male",
        "hair/buzzcut/adult",       "weapon/blunt/waraxe/walk/waraxe.png",                       "human/male"),
    ("hunter_male",         "Hunter",              ["medieval","peasant","ranged"],
        "male",   "torso/armour/leather/male",               "legs/pants/thin",        "feet/boots/basic/male",
        "hair/buzzcut/adult",       "weapon/ranged/bow/great/walk/foreground/iron.png",         "human/male"),
    ("fisher_female",       "Fisher (F)",          ["medieval","peasant"],
        "female", "torso/clothes/longsleeve/longsleeve/female",       "legs/pants/thin", "feet/shoes/basic/male",
        "hair/long/adult",       "",                                                          "human/female"),
    ("miner_male",          "Miner",               ["medieval","peasant"],
        "male",   "torso/clothes/sleeveless/sleeveless2_vneck/male",  "legs/pants/thin", "feet/boots/basic/male",
        "hair/buzzcut/adult",       "weapon/blunt/club/club.png",                                "human/male"),
    ("peasant_male",        "Peasant (M)",         ["medieval","peasant"],
        "male",   "torso/clothes/longsleeve/scoop/male",     "legs/pants/thin",        "feet/shoes/basic/male",
        "hair/buzzcut/adult",       "",                                                          "human/male"),
    ("peasant_female",      "Peasant (F)",         ["medieval","peasant"],
        "female", "torso/clothes/longsleeve/longsleeve/female",       "legs/skirts/slit/thin", "feet/shoes/basic/male",
        "hair/bob/adult",     "",                                                          "human/female"),

    # ── Outcasts / Wild ───────────────────────────────────────────────
    ("beggar_male",         "Beggar",              ["medieval","wild"],
        "male",   "torso/clothes/longsleeve/scoop/male",     "legs/pants/thin",        "feet/shoes/basic/male",
        "hair/messy1/adult",      "",                                                          "human/male_elderly"),
    ("vagabond_male",       "Vagabond / Deserter", ["medieval","wild"],
        "male",   "torso/clothes/longsleeve/scoop/male",     "legs/pants/thin",        "feet/boots/basic/male",
        "hair/messy1/adult",      "weapon/blunt/club/club.png",                                "human/male"),
    ("hermit_male",         "Hermit",              ["medieval","wild"],
        "male",   "torso/clothes/longsleeve/longsleeve_brown/male",   "legs/pants/thin", "feet/shoes/basic/male",
        "hair/long/adult",       "",                                                          "human/male_elderly"),
    ("witch_female",        "Hedge-Witch",         ["medieval","wild"],
        "female", "torso/clothes/longsleeve/longsleeve/female",       "legs/skirts/slit/thin", "feet/shoes/basic/male",
        "hair/long/adult",       "",                                                          "human/female"),

    # ── Shadowed Guild / Rogues ───────────────────────────────────────
    ("cutpurse_male",       "Cutpurse",            ["medieval","outlaw"],
        "male",   "torso/clothes/longsleeve/scoop/male",     "legs/pants/thin",        "feet/boots/basic/male",
        "hair/buzzcut/adult",       "weapon/sword/dagger/walk/dagger.png",                       "human/male"),
    ("fence_female",        "Fence (F)",           ["medieval","outlaw"],
        "female", "torso/clothes/longsleeve/longsleeve/female",       "legs/skirts/slit/thin", "feet/boots/basic/male",
        "hair/long/adult",       "",                                                          "human/female"),
    ("gambler_male",        "Gambler",             ["medieval","outlaw"],
        "male",   "torso/clothes/longsleeve/longsleeve_brown/male",   "legs/pants/thin", "feet/boots/basic/male",
        "hair/buzzcut/adult",       "",                                                          "human/male"),
    ("smuggler_male",       "Smuggler",            ["medieval","outlaw"],
        "male",   "torso/armour/leather/male",               "legs/pants/thin",        "feet/boots/basic/male",
        "hair/buzzcut/adult",       "weapon/sword/dagger/walk/dagger.png",                       "human/male"),
    ("bandit_male",         "Bandit",              ["medieval","outlaw","melee"],
        "male",   "torso/armour/leather/male",               "legs/pants/thin",        "feet/boots/basic/male",
        "hair/messy1/adult",      "weapon/blunt/waraxe/walk/waraxe.png",                       "human/male"),

    # ── Children ──────────────────────────────────────────────────────
    ("child_male",          "Errand-Boy / Child",  ["medieval","peasant"],
        "male",   "torso/clothes/longsleeve/scoop/male",     "legs/pants/thin",        "feet/shoes/basic/male",
        "hair/messy1/adult",      "",                                                          "human/child"),
]


def to_bundle(spec) -> dict:
    (bid, name, tags, body_kind, torso, legs, feet, hair, weapon, head_kind) = spec
    layers = {
        "body":  f"body/bodies/{body_kind}/walk.png",
        "head":  f"head/heads/{head_kind}/walk.png",
        "eyes":  "eyes/human/adult/default/walk.png",
        "feet":  f"{feet}/walk.png",
        "legs":  f"{legs}/walk.png",
        "torso": f"{torso}/walk.png",
        "hair":  f"{hair}/walk.png",
    }
    if weapon:
        layers["weapon"] = weapon
    return {
        "id": bid,
        "display_name": name,
        "tags": tags,
        "layers": layers,
        "animations": ["walk", "idle"],
    }


# Visual-variant strategy. Each base role now produces a broad deterministic
# random set. The generator scans the local LPC spritesheets so new vendored
# colors/styles are picked up without hand-editing every role.
VARIANTS_PER_BASE = 24

BLOCKED_MEDIEVAL_TOKENS = {
    "alien",
    "baseball",
    "blaster",
    "christmas",
    "cyber",
    "cyborg",
    "firearm",
    "frankenstein",
    "glow",
    "glowsword",
    "gun",
    "hazmat",
    "hoodie",
    "jack",
    "jetpack",
    "lab",
    "laser",
    "modern",
    "pistol",
    "rifle",
    "robot",
    "santa",
    "scifi",
    "shotgun",
    "sneaker",
    "space",
    "spacesuit",
    "tshirt",
    "xeon",
    "zombie",
}

MEDIEVAL_THEME_HINTS = {
    "apron",
    "armour",
    "armor",
    "bandana",
    "belt",
    "boot",
    "bracer",
    "cape",
    "chainmail",
    "cloth",
    "cowl",
    "dress",
    "formal",
    "fur",
    "glove",
    "hair",
    "hat",
    "helmet",
    "hood",
    "hose",
    "kilt",
    "leather",
    "longsleeve",
    "mantal",
    "pants",
    "robe",
    "sandals",
    "sash",
    "shield",
    "shirt",
    "shoe",
    "skirt",
    "sleeveless",
    "surcoat",
    "tunic",
    "vest",
    "weapon",
    "waist",
}

NATURAL_SKIN_TINTS = [
    "#f2c7a5",
    "#e1a979",
    "#c98255",
    "#a86b47",
    "#7b4a32",
    "#5b3829",
]

HAIR_TINTS = [
    "#20150f",
    "#3b2418",
    "#5b3822",
    "#7a4d2d",
    "#9a693b",
    "#c08a4b",
    "#d8b36a",
    "#e6d6ac",
    "#8a8a8a",
    "#c9c0b8",
    "#5f2a1d",
    "#2c2b26",
]

BODY_SLOT_TOKENS = {"male", "female", "thin", "pregnant", "teen", "child", "adult", "muscular", "universal"}


def is_medieval_safe(rel_path: str) -> bool:
    lowered = rel_path.lower()
    return not any(token in lowered for token in BLOCKED_MEDIEVAL_TOKENS)


def walk_path(rel_path: str) -> str:
    return rel_path if rel_path.endswith(".png") else f"{rel_path}/walk.png"


def existing_layer(rel_path: str) -> str | None:
    rel = walk_path(rel_path)
    if not is_medieval_safe(rel):
        return None
    return rel if (LPC_SPRITESHEETS / rel).exists() else None


def is_usable_walk_asset(path: Path) -> bool:
    rel = path.relative_to(LPC_SPRITESHEETS).as_posix()
    if not is_medieval_safe(rel):
        return False
    if path.name == "walk.png":
        return True
    return path.parent.name == "walk" and path.suffix.lower() == ".png"


@lru_cache(maxsize=None)
def walk_assets(category: str) -> tuple[str, ...]:
    root = LPC_SPRITESHEETS / category
    if not root.exists():
        return ()
    assets = [
        path.relative_to(LPC_SPRITESHEETS).as_posix()
        for path in root.rglob("*.png")
        if is_usable_walk_asset(path)
    ]
    return tuple(sorted(set(assets)))


def existing_pool(candidates: list[str]) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for candidate in candidates:
        rel = existing_layer(candidate)
        if rel is not None and rel not in seen:
            seen.add(rel)
            result.append(rel)
    return result


def body_slots(body_kind: str, head_kind: str) -> dict[str, str]:
    torso_body = body_kind if body_kind in {"female", "male", "teen"} else "male"
    thin_or_male = "thin" if body_kind in {"female", "pregnant"} else "male"
    if body_kind == "teen":
        thin_or_male = "thin"
    adult_or_child = "child" if head_kind.endswith("/child") else "adult"
    return {
        "torso": torso_body,
        "legs": thin_or_male,
        "feet": thin_or_male,
        "arms": thin_or_male,
        "shoulders": "thin" if body_kind in {"female", "pregnant", "teen"} else "male",
        "neck": "female" if body_kind in {"female", "pregnant"} else "male",
        "hair_age": adult_or_child,
    }


def format_candidates(candidates: list[str], slots: dict[str, str]) -> list[str]:
    return [candidate.format(**slots) for candidate in candidates]


def path_parts(rel_path: str) -> set[str]:
    return {part.lower() for part in Path(rel_path).parts}


def has_body_slot(rel_path: str, slot: str) -> bool:
    parts = path_parts(rel_path)
    if slot in parts or "universal" in parts:
        return True
    body_tokens = parts & BODY_SLOT_TOKENS
    return not body_tokens


def excludes_split_layer(rel_path: str) -> bool:
    parts = path_parts(rel_path)
    return "bg" in parts or "fg" in parts or "background" in parts or "foreground" in parts


def role_rank(rel_path: str, tags: list[str]) -> int:
    lowered = rel_path.lower()
    score = 0
    if "law" in tags or "melee" in tags:
        score += 5 if any(token in lowered for token in ("armour", "armor", "chainmail", "helmet", "shield", "sword", "spear", "mace")) else 0
    if "ranged" in tags:
        score += 5 if any(token in lowered for token in ("bow", "quiver", "arrow", "sling")) else 0
    if "trade" in tags:
        score += 4 if any(token in lowered for token in ("apron", "suspend", "tool", "belt", "sleeve")) else 0
    if "peasant" in tags:
        score += 4 if any(token in lowered for token in ("plain", "scoop", "fur", "sandals", "basket", "pack")) else 0
    if "chapel" in tags:
        score += 4 if any(token in lowered for token in ("robe", "hood", "cowl", "magic", "staff", "simple")) else 0
    if "outlaw" in tags:
        score += 4 if any(token in lowered for token in ("bandana", "hood", "leather", "dagger", "club", "pirate")) else 0
    if "wayfarer" in tags:
        score += 3 if any(token in lowered for token in ("cape", "pack", "hood", "hat", "boot")) else 0
    score += 1 if any(token in lowered for token in MEDIEVAL_THEME_HINTS) else 0
    return score


def sorted_for_role(pool: list[str], tags: list[str]) -> list[str]:
    return sorted(pool, key=lambda path: (-role_rank(path, tags), path))


def category_pool(category: str, tags: list[str], slot: str | None = None, allow_split: bool = False) -> list[str]:
    result = []
    for rel in walk_assets(category):
        if slot is not None and not has_body_slot(rel, slot):
            continue
        if not allow_split and excludes_split_layer(rel):
            continue
        result.append(rel)
    return sorted_for_role(result, tags)


def hair_pool(head_kind: str) -> list[str]:
    age = "child" if head_kind.endswith("/child") else "adult"
    result = [
        rel for rel in walk_assets("hair")
        if age in path_parts(rel) and not excludes_split_layer(rel)
    ]
    return sorted(set(result))


def optional_layers(tags: list[str], body_kind: str, slots: dict[str, str]) -> dict[str, list[str]]:
    pools = {
        "arms": category_pool("arms", tags, slots["arms"]),
        "shoulders": category_pool("shoulders", tags, slots["shoulders"]),
        "waist": category_pool("torso/waist", tags, slots["neck"]),
        "neck": category_pool("neck", tags, slots["neck"]),
        "hat": category_pool("hat", tags, "adult"),
        "facial": category_pool("facial", tags, "adult"),
        "cape": category_pool("cape", tags, "adult"),
        "shield": category_pool("shield", tags, None, allow_split=True),
        "quiver": category_pool("quiver", tags, "adult"),
        "backpack": category_pool("backpack", tags, body_kind, allow_split=False),
    }
    if body_kind == "male":
        pools["beard"] = category_pool("beards", tags)
    return pools


def weapon_pool(tags: list[str], base_weapon: str) -> list[str]:
    pool = list(walk_assets("weapon"))
    if "ranged" not in tags:
        pool = [path for path in pool if "ranged/" not in path or any(tag in tags for tag in ("law", "outlaw", "wild"))]
    if not any(tag in tags for tag in ("law", "melee", "ranged", "outlaw", "wild", "chapel")):
        pool = []
    if base_weapon:
        pool.insert(0, base_weapon)
    return sorted_for_role(sorted(set(path for path in pool if is_medieval_safe(path))), tags)


def choose(rng: random.Random, pool: list[str], fallback: str | None = None, favored_window: int = 80) -> str | None:
    if not pool:
        return fallback
    window = min(len(pool), max(1, favored_window))
    return rng.choice(pool[:window])


def roll(rng: random.Random, probability: float) -> bool:
    return rng.random() < probability


def layer_signature(layers: dict[str, str]) -> tuple[tuple[str, str], ...]:
    return tuple(sorted((key, value) for key, value in layers.items() if value))


def variants_for(spec):
    """Yield generated bundle dictionaries for a given base spec."""
    bid, name, tags, body_kind, torso, legs, feet, hair, weapon, head_kind = spec
    slots = body_slots(body_kind, head_kind)
    torso_options = category_pool("torso", tags, slots["torso"])
    legs_options = category_pool("legs", tags, slots["legs"])
    feet_options = category_pool("feet", tags, slots["feet"])
    hair_options = hair_pool(head_kind)
    weapons = weapon_pool(tags, weapon)
    extras = optional_layers(tags, body_kind, slots)

    seen: set[tuple[tuple[str, str], ...]] = set()
    for variant_number in range(1, VARIANTS_PER_BASE + 1):
        rng = random.Random(f"{bid}:{variant_number}:medieval-lpc-v4")
        skin_tint = rng.choice(NATURAL_SKIN_TINTS)
        hair_tint = rng.choice(HAIR_TINTS)
        layers = {
            "body": f"body/bodies/{body_kind}/walk.png",
            "head": f"head/heads/{head_kind}/walk.png",
            "eyes": "eyes/human/adult/default/walk.png",
            "feet": choose(rng, feet_options, walk_path(feet)),
            "legs": choose(rng, legs_options, walk_path(legs)),
            "torso": choose(rng, torso_options, walk_path(torso)),
            "hair": choose(rng, hair_options, walk_path(hair), favored_window=200),
        }
        if weapons and roll(rng, 0.55 if any(tag in tags for tag in ("law", "melee", "outlaw", "ranged")) else 0.18):
            layers["weapon"] = choose(rng, weapons, weapon, favored_window=100)

        optional_chances = {
            "waist": 0.72,
            "neck": 0.26 if "wayfarer" not in tags else 0.50,
            "hat": 0.38 if not any(tag in tags for tag in ("law", "outlaw")) else 0.72,
            "facial": 0.20,
            "beard": 0.58 if body_kind == "male" else 0.0,
            "arms": 0.34 if any(tag in tags for tag in ("law", "melee", "outlaw")) else 0.16,
            "shoulders": 0.34 if any(tag in tags for tag in ("law", "melee")) else 0.10,
            "cape": 0.18 if not any(tag in tags for tag in ("law", "wayfarer", "chapel")) else 0.36,
            "shield": 0.24 if any(tag in tags for tag in ("law", "melee", "outlaw", "ranged", "wild")) else 0.0,
            "quiver": 0.70 if "ranged" in tags else 0.06,
            "backpack": 0.34 if any(tag in tags for tag in ("peasant", "trade", "wayfarer")) else 0.12,
        }
        for slot_name, probability in optional_chances.items():
            pool = extras.get(slot_name, [])
            if pool and roll(rng, probability):
                layers[slot_name] = choose(rng, pool, favored_window=100)

        layers = {key: value for key, value in layers.items() if value}
        bundle = {
            "id": bid if variant_number == 1 else f"{bid}_v{variant_number}",
            "display_name": name if variant_number == 1 else f"{name} (variant {variant_number})",
            "tags": tags,
            "layers": layers,
            "tints": {
                "body": skin_tint,
                "head": skin_tint,
                "hair": hair_tint,
                "beard": hair_tint,
            },
            "animations": ["walk", "idle"],
        }
        signature = layer_signature(bundle["layers"])
        if signature in seen:
            continue
        seen.add(signature)
        yield bundle


def main() -> int:
    THEMES_DIR.mkdir(parents=True, exist_ok=True)
    for old_theme in THEMES_DIR.glob("*.json"):
        old_theme.unlink()
    generated_dir = REPO / "assets" / "art" / "generated" / "lpc_npcs"
    if generated_dir.exists():
        shutil.rmtree(generated_dir)
    generated_dir.mkdir(parents=True, exist_ok=True)

    written = 0
    base_to_variants: dict[str, list[str]] = {}
    for spec in SPECS:
        base_id = spec[0]
        base_to_variants[base_id] = []
        for bundle in variants_for(spec):
            path = THEMES_DIR / f"{bundle['id']}.json"
            path.write_text(json.dumps(bundle, indent=2))
            base_to_variants[base_id].append(bundle["id"])
            written += 1
    # Write the bundle ↔ variant map so theme.json / engine can read which
    # variants are available for each base bundle id.
    map_path = THEMES_DIR / "_appearance_variants.json"
    map_path.write_text(json.dumps(base_to_variants, indent=2))
    print(f"wrote {written} bundle(s) ({len(base_to_variants)} bases) to {THEMES_DIR}")
    print(f"wrote variant map to {map_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
