"""Which codex category each of the game's creatures falls in, and the reference creatures of each category.

Sorted by body plan first (what rig and clips a new creature of that kind needs), then by size: bipeds under 1.7 m
are small, over 3 m large (the Player is 1.9 m); legged animals under 1.2 m tall are small, over 2.5 m large.
Bosses (Character.m_boss) and the talking NPCs are their own categories whatever their body. A creature missing
from the table is reported by creatures.py, so a game update that adds one shows up.
"""

CATEGORIES = {
    "creature.critter": ("critters: ambient birds and fish, hares and chickens (no attack, small or no rig)",
                         ["Hare", "Chicken", "Hen", "Crow", "Seagal", "AshCrow", "Fish1", "Fish2", "Fish3",
                          "Fish4_cave", "Fish5", "Fish6", "Fish7", "Fish8", "Fish9", "Fish10", "Fish11", "Fish12"]),
    "creature.humanoid_small": ("small bipeds, under 1.7 m",
                                ["Goblin", "GoblinArcher", "GoblinDeepNorth", "Goblin_Gem", "GoblinShaman",
                                 "GoblinShaman_Hildir", "GoblinShaman_Hildir_nochest", "DvergerTest", "Dverger",
                                 "DvergerAshlands", "DvergerDeepNorth", "DvergerMage", "DvergerMageFire",
                                 "DvergerMageIce", "DvergerMageSupport", "Greyling", "Surtling", "Elaking",
                                 "ElakingLantern", "Neck"]),
    "creature.humanoid": ("bipeds of the player's size, 1.7 to 3 m",
                          ["Player", "FallenWarrior", "ShadowPerson", "Greydwarf", "Greydwarf_Frozen",
                           "Greydwarf_Shaman", "Greydwarf_Shaman_Frozen", "Draugr", "Draugr_sleeping", "Draugr_Elite",
                           "Draugr_Elite_sleeping", "Draugr_Ranged", "Draugr_Ranged_sleeping", "TrainingDummy",
                           "Skeleton", "Skeleton_NoArcher", "Skeleton_Friendly", "Skeleton_Meadows",
                           "Skeleton_Meadows_noarcher", "Skeleton_Swamps", "Skeleton_Swamps_noarcher",
                           "Skeleton_Mountains", "Skeleton_Mountains_noarcher", "Skeleton_DeepNorth", "Skeleton_Poison",
                           "Skeleton_Hildir", "Skeleton_Hildir_nochest", "Skeleton_aspect", "Ghost", "Ghost_sleeping",
                           "Ghost_old", "Ghost_Void", "Wraith", "Charred_Melee", "Charred_Melee_Dyrnwyn",
                           "Charred_Melee_Fader", "Charred_Archer", "Charred_Archer_Fader", "Charred_Mage",
                           "Charred_Twitcher", "Charred_Twitcher_Summoned", "Fenring_Cultist",
                           "Fenring_Cultist_Hildir", "Fenring_Cultist_Hildir_nochest", "Frysling"]),
    "creature.humanoid_large": ("large bipeds, over 3 m",
                                ["Greydwarf_Elite", "Fenring", "GoblinBrute", "GoblinBrute_Hildir", "GoblinBruteBros",
                                 "GoblinBruteBros_nochest", "JotunWarrior", "JotunWarriorDualWield", "JotunWitch",
                                 "Troll", "Troll_sleeping", "Troll_Summoned", "TrollFrost", "StoneGolem", "Barka",
                                 "Morgen", "Morgen_NonSleeping", "Abomination", "Writhan"]),
    "creature.quadruped_small": ("small legged animals, under 1.2 m tall",
                                 ["Boar", "Boar_piggy", "Boar_spiritcaller", "Tick"]),
    "creature.quadruped": ("legged animals 1.2 to 2.5 m tall (four legs, or six and eight for the insects)",
                           ["Wolf", "Wolf_cub", "Wolf_spiritcaller", "Deer", "Deer_White", "Asksvin",
                            "Asksvin_hatchling", "Ulv", "Seal", "Seal_Pup", "Seeker", "SeekerBrood", "SeekerBrute"]),
    "creature.quadruped_large": ("large legged animals, over 2.5 m tall",
                                 ["Lox", "Lox_Calf", "Moose", "Moose_calf", "Moose_spiritcaller", "Bjorn",
                                  "Bjorn_sleeping", "Bjorn_spiritcaller", "Unbjorn", "ElakingMole"]),
    "creature.flyer": ("flyers: bats, insects, drakes, birds of prey, floating things",
                       ["Bat", "Bat_Swamp", "Deathsquito", "Mistile", "Hatchling", "Volture", "Gjall",
                        "FallenValkyrie"]),
    "creature.swimmer": ("swimmers: serpents, leeches and the leviathans",
                         ["Serpent", "BonemawSerpent", "Leech", "Leech_cave", "Leviathan", "LeviathanLava"]),
    "creature.amorphous": ("amorphous: blobs, roots and tendrils, spirits and haunted things",
                           ["Blob", "BlobAspect", "BlobElite", "BlobFrost", "BlobLava", "BlobMork", "BlobMorkMini",
                            "BlobTar", "TentaRoot", "TentaRoot_wild", "Aspect_TentaRoot", "Tendril", "Tendril_back",
                            "BogWitchKvastur", "FrostWisp"]),
    "creature.boss": ("bosses and their summoned aspects",
                      ["Eikthyr", "Aspect_Eikthyr", "gd_king", "Aspect_Elder", "Bonemass", "Aspect_Bonemass", "Hive",
                       "Dragon", "Aspect_Moder", "GoblinKing", "Aspect_Yagluth", "SeekerQueen", "Aspect_SeekerQueen",
                       "TheHive", "Fader", "Aspect_Fader", "FrozenKing_0", "FrozenKing_p2", "FrozenKing_p3"]),
    "creature.npc": ("talking NPCs: traders, Odin, the Valkyrie, the ravens",
                     ["Haldor", "Hildir", "BogWitch", "odin", "Hugin", "Munin", "Valkyrie"]),
}

REFERENCES = {
    "creature.critter": ["Hare", "Hen", "Crow", "Fish1"],
    "creature.humanoid_small": ["Goblin", "GoblinShaman", "Dverger", "Surtling", "Greyling"],
    "creature.humanoid": ["Player", "Greydwarf", "Draugr", "Skeleton", "Charred_Melee", "Fenring_Cultist"],
    "creature.humanoid_large": ["Troll", "Greydwarf_Elite", "GoblinBrute", "JotunWarrior", "StoneGolem"],
    "creature.quadruped_small": ["Boar", "Tick"],
    "creature.quadruped": ["Wolf", "Deer", "Seeker", "Asksvin", "Ulv"],
    "creature.quadruped_large": ["Lox", "Moose", "Bjorn", "Unbjorn"],
    "creature.flyer": ["Bat", "Deathsquito", "Hatchling", "Volture", "Gjall"],
    "creature.swimmer": ["Serpent", "BonemawSerpent", "Leech"],
    "creature.amorphous": ["Blob", "BlobElite", "TentaRoot", "BogWitchKvastur"],
    "creature.boss": ["Eikthyr", "gd_king", "Bonemass", "Dragon", "GoblinKing", "Fader"],
    "creature.npc": ["Haldor", "Hildir", "BogWitch", "odin"],
}


def category_of(name):
    """The category key a creature prefab name falls in, or None when the table does not know it."""
    return next((key for key, (_, names) in CATEGORIES.items() if name in names), None)
