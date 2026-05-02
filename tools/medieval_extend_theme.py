#!/usr/bin/env python3
"""Augment medieval theme.json with per-NPC appearance variants, relationship
graphs, and shared interaction pools.

Inputs (read in place):
- assets/themes/medieval/theme.json                       (NPC roster)
- assets/art/sprites/lpc/themes/_appearance_variants.json (variant map)

Output (written in place):
- assets/themes/medieval/theme.json (with the new fields merged)

Re-runnable. Existing fields are preserved; only the augmented keys are
overwritten so per-NPC edits to canonical fields (name/role/secret) survive.
"""

from __future__ import annotations

import json
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
THEME_PATH = REPO / "assets" / "themes" / "medieval" / "theme.json"
VARIANTS_PATH = REPO / "assets" / "art" / "sprites" / "lpc" / "themes" / "_appearance_variants.json"

# Hard-coded inter-NPC relationship graph, drawn from the personality /
# need / secret text in the existing roster. Format:
#   from_npc_id: [(to_npc_id, type, intensity 0..3)]
# Where `type` is one of: friend, rival, family, creditor, debtor, lover,
# distrusts, fears, mentors, employs, knows_secret.
RELATIONSHIPS: dict[str, list[tuple[str, str, int]]] = {
    "blacksmith_garrick": [
        ("tavernkeep_meri", "friend", 2),
        ("miller_aenwyn", "rival", 2),
        ("acolyte_nesta", "family", 1),  # smith's daughter implied
    ],
    "tavernkeep_meri": [
        ("blacksmith_garrick", "friend", 2),
        ("drunkard_perrin", "creditor", 2),
        ("bard_seraphine", "employs", 2),
        ("baker_jonna", "friend", 1),
    ],
    "priest_calden": [
        ("abbot_ethelmar", "mentors", 2),
        ("scribe_velmont", "employs", 1),
        ("fence_morwen", "family", 3),  # estranged sister
        ("acolyte_nesta", "mentors", 1),
        ("witch_thorne", "distrusts", 2),
        ("bard_seraphine", "friend", 1),
    ],
    "captain_wace": [
        ("guard_juno", "employs", 1),
        ("guard_brida", "employs", 1),
        ("ranger_ivor", "employs", 1),
        ("smuggler_weyland", "knows_secret", 3),
        ("bandit_garth", "rival", 3),
    ],
    "magistrate_oland": [
        ("captain_wace", "employs", 1),
        ("nobleman", "rival", 1),
        ("midwife_alenia", "knows_secret", 3),  # bastard
        ("gambler_nox", "debtor", 3),
    ],
    "guard_juno": [
        ("captain_wace", "employs", 1),
        ("vagabond_oren", "family", 3),  # brother
        ("blacksmith_garrick", "lover", 2),  # smith's daughter (unrequited)
    ],
    "guard_brida": [
        ("captain_wace", "employs", 1),
        ("tavernkeep_meri", "friend", 1),
    ],
    "executioner_corvus": [
        ("blacksmith_garrick", "employs", 1),
        ("magistrate_oland", "distrusts", 2),
    ],
    "fletcher_essa": [
        ("captain_wace", "creditor", 2),
        ("hunter_riven", "friend", 1),
    ],
    "tanner_pell": [
        ("priest_calden", "rival", 2),  # smell complaints
        ("smuggler_weyland", "knows_secret", 2),
    ],
    "miller_aenwyn": [
        ("baker_jonna", "rival", 2),
        ("priest_calden", "distrusts", 1),
    ],
    "baker_jonna": [
        ("miller_aenwyn", "rival", 2),
        ("beggar_lorne", "friend", 2),
        ("tavernkeep_meri", "friend", 1),
    ],
    "tailor_corra": [
        ("magistrate_oland", "creditor", 2),
        ("scribe_velmont", "knows_secret", 1),
    ],
    "carpenter_orlin": [
        ("tavernkeep_meri", "friend", 1),
        ("undertaker_morgaine", "employs", 1),
    ],
    "weaver_isolde": [
        ("priest_calden", "knows_secret", 2),  # heretical patterns
    ],
    "cooper_braddock": [
        ("smuggler_weyland", "family", 2),
        ("brewer_haldis", "employs", 1),
    ],
    "mason_dorrick": [
        ("priest_calden", "creditor", 2),
        ("gambler_nox", "debtor", 3),
    ],
    "brewer_haldis": [
        ("cooper_braddock", "employs", 1),
        ("priest_calden", "friend", 1),
    ],
    "butcher_orven": [
        ("shepherd_caelin", "employs", 1),
        ("scribe_velmont", "knows_secret", 1),  # love poetry
    ],
    "herbalist_ysolt": [
        ("priest_calden", "rival", 2),
        ("apothecary_winnoc", "rival", 3),
        ("midwife_alenia", "friend", 2),
        ("witch_thorne", "friend", 1),
    ],
    "apothecary_winnoc": [
        ("herbalist_ysolt", "rival", 3),
        ("magistrate_oland", "knows_secret", 2),  # mixing for his wife
    ],
    "innkeeper_otto": [
        ("smuggler_weyland", "knows_secret", 3),  # daughter
    ],
    "stablehand_pia": [
        ("captain_wace", "employs", 1),
        ("knight_aldric", "friend", 1),
    ],
    "merchant_godric": [
        ("captain_wace", "rival", 1),  # tolls
        ("merchant_zara", "friend", 1),
        ("mercenary_kael", "employs", 1),
    ],
    "merchant_zara": [
        ("magistrate_oland", "rival", 1),
        ("smuggler_weyland", "employs", 1),
    ],
    "scribe_velmont": [
        ("priest_calden", "employs", 1),
        ("butcher_orven", "knows_secret", 1),
        ("bard_seraphine", "friend", 2),
    ],
    "abbot_ethelmar": [
        ("priest_calden", "mentors", 2),
        ("hermit_vey", "friend", 2),
        ("fence_morwen", "knows_secret", 3),
    ],
    "acolyte_nesta": [
        ("priest_calden", "mentors", 1),
        ("abbot_ethelmar", "fears", 1),
        ("blacksmith_garrick", "family", 1),
    ],
    "farmer_bess": [
        ("priest_calden", "friend", 1),
        ("bandit_garth", "family", 3),  # son
    ],
    "farmer_ulric": [
        ("miller_aenwyn", "rival", 2),
    ],
    "shepherd_caelin": [
        ("butcher_orven", "employs", 1),
        ("captain_wace", "knows_secret", 2),  # bandits in meadow
    ],
    "woodcutter_havel": [
        ("carpenter_orlin", "employs", 1),
    ],
    "hunter_riven": [
        ("captain_wace", "employs", 1),
        ("fletcher_essa", "friend", 1),
    ],
    "fisher_marra": [
        ("smuggler_weyland", "family", 2),  # brother
    ],
    "miner_dolf": [
        ("priest_calden", "friend", 1),
        ("mason_dorrick", "employs", 1),
    ],
    "beggar_lorne": [
        ("baker_jonna", "friend", 2),
        ("priest_calden", "friend", 1),
        ("magistrate_oland", "knows_secret", 2),
    ],
    "drunkard_perrin": [
        ("tavernkeep_meri", "debtor", 2),
        ("magistrate_oland", "knows_secret", 3),  # his son
    ],
    "cutpurse_dolf": [
        ("magistrate_oland", "knows_secret", 3),  # signet
        ("fence_morwen", "employs", 2),
    ],
    "fence_morwen": [
        ("priest_calden", "family", 3),
        ("cutpurse_dolf", "employs", 2),
        ("smuggler_weyland", "friend", 2),
    ],
    "gambler_nox": [
        ("magistrate_oland", "creditor", 3),
        ("mason_dorrick", "creditor", 3),
    ],
    "smuggler_weyland": [
        ("captain_wace", "knows_secret", 3),  # IOU
        ("fence_morwen", "friend", 2),
        ("innkeeper_otto", "knows_secret", 3),
        ("fisher_marra", "family", 2),
    ],
    "bandit_garth": [
        ("captain_wace", "rival", 3),
        ("farmer_bess", "family", 3),
    ],
    "vagabond_oren": [
        ("guard_juno", "family", 3),  # brother
        ("captain_wace", "fears", 3),
    ],
    "bard_seraphine": [
        ("tavernkeep_meri", "employs", 2),
        ("priest_calden", "employs", 1),
        ("scribe_velmont", "friend", 2),
    ],
    "hermit_vey": [
        ("abbot_ethelmar", "friend", 2),
    ],
    "witch_thorne": [
        ("priest_calden", "rival", 2),
        ("herbalist_ysolt", "friend", 1),
    ],
    "alchemist_corwin": [
        ("apothecary_winnoc", "rival", 1),
        ("innkeeper_otto", "employs", 1),  # rents room
    ],
    "fortune_teller_solvi": [
        ("priest_calden", "rival", 1),
    ],
    "scholar_thorvald": [
        ("scribe_velmont", "employs", 1),
        ("priest_calden", "rival", 1),
    ],
    "mercenary_kael": [
        ("merchant_godric", "employs", 1),
    ],
    "ranger_ivor": [
        ("captain_wace", "employs", 1),
        ("hunter_riven", "friend", 1),
    ],
    "knight_aldric": [
        ("squire_petra", "employs", 2),
        ("stablehand_pia", "friend", 1),
    ],
    "squire_petra": [
        ("knight_aldric", "employs", 2),
    ],
    "pilgrim_temperance": [
        ("priest_calden", "friend", 1),
    ],
    "midwife_alenia": [
        ("herbalist_ysolt", "friend", 2),
        ("magistrate_oland", "knows_secret", 3),
    ],
    "undertaker_morgaine": [
        ("carpenter_orlin", "employs", 1),
        ("priest_calden", "employs", 1),
    ],
    "errand_boy_finn": [
        ("blacksmith_garrick", "employs", 1),
        ("captain_wace", "employs", 1),
        ("baker_jonna", "friend", 2),
    ],
    "washerwoman_griselda": [
        ("captain_wace", "knows_secret", 3),  # blood on tunic
        ("stablehand_pia", "friend", 1),
    ],
}

# Shared interaction pools keyed by the role-tag NPCs use in their
# spawn entry. The server picks lines from the pool whose tag matches
# at least one of the NPC's tags. NPC entries can override or add via a
# `personal_greetings` list (kept flat for now).
INTERACTIONS = {
    "greetings_pool": {
        "law": [
            "Move along. Or don't. Your call, but I'm watching.",
            "State your business at the gate.",
            "By order of the crown, mind the curfew bell tonight.",
            "Keep that hand off your weapon while you talk to me.",
        ],
        "outlaw": [
            "Looking for something the gates don't sell?",
            "Quiet voice, friend. The walls have ears, and most are armed.",
            "Coin first, conversation second.",
            "I don't know you. Convince me that's a problem worth fixing.",
        ],
        "chapel": [
            "Peace be on your road, traveller.",
            "The morning bell rings for all of us. Even you.",
            "What burden have you brought into the chapel today?",
            "Light a candle. The order takes coppers or honest work.",
        ],
        "trade": [
            "Buying or browsing? Either's fine, but one pays my rent.",
            "Fair price is whatever I say it is, and I'm in a fair mood.",
            "Mind the apprentice — he's still learning where to stand.",
            "Anything you don't see, I might still have. Ask.",
        ],
        "wayfarer": [
            "Just passing through. The road north was quiet, if you're going.",
            "Stranger to stranger — what news from your side of the gate?",
            "I keep my coin close and my conversation closer.",
            "If you're heading my way, two travellers walk safer than one.",
        ],
        "wild": [
            "I'd offer you a seat but my hut wouldn't survive it.",
            "The wood listens. Speak softly while you're inside it.",
            "Whatever you came to ask, ask plainly. I'm short on patience.",
            "A copper in the bowl is a fair tithe for an honest answer.",
        ],
        "peasant": [
            "Bless you, friend. Mind the cart.",
            "Was that you near my fence last night, or am I imagining things?",
            "Long day. Long week. Long life.",
            "If the priest sent you, I've already paid the tithe this fortnight.",
        ],
    },

    "reactions_pool": {
        "approached": [
            "Mm.",
            "What now.",
            "Speak quick — I've a kettle on.",
            "Yes?",
        ],
        "complimented": [
            "Hmph. Easy words.",
            "Generous of you. I'll remember it.",
            "Was there something you wanted with that?",
        ],
        "insulted": [
            "Repeat that, slowly, where the captain can hear.",
            "I'll forget you said that. Once.",
            "Keep walking before I make you regret your tongue.",
        ],
        "witnessed_crime": [
            "I saw nothing. I see less every day.",
            "That'll be a copper for the silence.",
            "The captain hears about that before sundown.",
            "Run, fool. Before someone less merciful saw you.",
        ],
        "given_gift": [
            "Kind. Strange, but kind.",
            "What do you want for it?",
            "I'll add this to the kindness ledger.",
        ],
    },

    "gossip_templates": {
        "trade": [
            "{relation_name} owes me three coppers and a debt of honour.",
            "Don't tell {relation_name} I said this, but the work has been sloppy.",
            "Heard {relation_name} was seen with a stranger after dark.",
        ],
        "law": [
            "{relation_name} draws a long shadow lately. Watch your back.",
            "If {relation_name} asks where you slept last night, lie politely.",
        ],
        "chapel": [
            "I will pray for {relation_name}. They need it.",
            "{relation_name} has not been at confession in too long.",
        ],
        "outlaw": [
            "{relation_name} is on the wrong list lately. Wouldn't borrow there.",
            "Heard {relation_name} is one bad week from a price on their head.",
        ],
        "wayfarer": [
            "{relation_name} was telling stories at the inn. Half were even true.",
            "Saw {relation_name} on the south road. Carrying something they shouldn't.",
        ],
        "peasant": [
            "{relation_name} short-weighted me last market. Not forgetting it.",
            "{relation_name} buried something past the field stones. I saw the spade.",
        ],
        "wild": [
            "{relation_name} brought me herbs that grew where they shouldn't.",
            "Don't repeat this, but {relation_name} watches the chapel at night.",
        ],
    },
}


def main() -> int:
    theme = json.loads(THEME_PATH.read_text())
    variants = json.loads(VARIANTS_PATH.read_text())

    augmented = 0
    for npc in theme.get("npc_roster", []):
        bundle_id = npc.get("lpc_bundle")
        npc["appearance_options"] = variants.get(bundle_id, [bundle_id])
        npc["relationships"] = [
            {"target": target, "type": rel_type, "intensity": intensity}
            for (target, rel_type, intensity) in RELATIONSHIPS.get(npc["id"], [])
        ]
        augmented += 1

    theme["interactions"] = INTERACTIONS
    theme["schema_version"] = max(theme.get("schema_version", 1), 2)

    THEME_PATH.write_text(json.dumps(theme, indent=2) + "\n")
    print(f"updated {augmented} NPCs in {THEME_PATH}")
    print(f"interaction pools: greetings={len(INTERACTIONS['greetings_pool'])} role tags, "
          f"reactions={len(INTERACTIONS['reactions_pool'])} contexts, "
          f"gossip_templates={len(INTERACTIONS['gossip_templates'])} role tags")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
