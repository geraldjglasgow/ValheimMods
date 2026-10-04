namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// The full rule file exactly as it is written on first run: every value at its default, with a short comment
    /// where a value needs one to be set right (units, what 0 means, what a list is indexed by). What each block
    /// does is in the reference (the mod's CLAUDE.md), not here. A server owner always edits a complete file rather
    /// than a blank one. This is data, not logic.
    /// </summary>
    internal static class RuleText
    {
        public const string FileName = "creature_rules.yml";

        public const string Default =
@"# Elite Creatures Reborn - creature rules. The reference explains every block:
# https://github.com/geraldjglasgow/ValheimMods/blob/main/EliteCreaturesReborn/CLAUDE.md
# Percentages are 0-100. Lists by star count start at 0 stars; past the end the
# last entry repeats. Biome names: Meadows, BlackForest, Swamp, Mountain, Plains,
# Mistlands, AshLands, DeepNorth, Ocean.

# How hard creatures get as the world's bosses fall: Easy, Medium, Hard, Very Hard
# or Extreme. Custom uses the biomes' star and mutation rows below with the world
# tier boosts instead; a file without this line is Custom.
difficulty: Medium

# Connected players use the server's copy of this file.
lock to server: true

# Most mutations one creature can carry. 0 = no cap.
max mutations: 1

# false turns a mutation off everywhere.
mutations enabled:
  Mad: true
  Bloated: true
  Cloaked: true
  Splintering: true
  Leeching: true
  Warding: true
  Plated: true
  Miasmic: true
  Devouring: true
  Thieving: true
  Gilded: true
  Blinking: true
  Relentless: true
  Juggernaut: true
  Screecher: true

defaults:
  # How much stronger a mutation is on a large star (worth 5).
  large star power: 1

  # Multipliers by star count: size, health, damage, attack speed, move speed, loot.
  # 6 to 8 stars happen only on Extreme.
  star power:
    growth:      [0.06, 0.10, 0.15, 0.20, 0.25, 0.30, 0.35, 0.40, 0.45]
    hp:          [1,    1.4,  1.95, 2.6,  3.3,  4.0,  4.7,  5.4,  6.1]
    attack:      [1,    1.2,  1.45, 1.75, 2.1,  2.5,  2.9,  3.3,  3.7]
    swing speed: [1,    1.02, 1.05, 1.08, 1.12, 1.16, 1.19, 1.22, 1.25]
    speed:       [1,    1,    1.03, 1.06, 1.1,  1.15, 1.18, 1.21, 1.24]
    drops:       [1,    1,    1.5,  2,    2.5,  3,    3.5,  4,    4.5]

  # Chance of each mutation by star count. On Custom every mutation rolls separately;
  # on a difficulty these only decide which one a mutated creature gets.
  mutation chance: [2.5, 3.5, 5, 6, 7.5, 10]

  # Mutations with a curve of their own instead of the one above.
  mutation chances:
    Devouring:   [0.6, 0.9, 1.2, 1.5, 1.8, 2.4]
    Gilded:      [0.2, 0.25, 0.35, 0.45, 0.55, 0.7]

  # Field meanings: reference, Mutation power fields.
  mutation power:
    Mad:         { move: 1.6, attack speed: 1.5, health: 0.5 }
    Bloated:     { health: 2.0, delay: 1.5, damage: 40, radius: 4, blast effect: fx_dynamite_explosion, blast sound: sfx_bombdynamite_explosion, warning effect: vfx_Smoked }
    Cloaked:     { reveal distance: 10, fade time: 0.5, fade margin: 1 }
    Splintering: { damage: 0.6, max generations: 0, max descendants: 0 }
    Leeching:    { regen: 0.5, lifesteal: 10, regen cap: 20, combat cooldown: 5 }
    Warding:     { reflect: 30, knockback: 4, max reflect: 7.5 }
    Plated:      { armour: 40, damage: 60, max reduction: 55 }
    Miasmic:     { cloud life: 6, cloud damage: 5, clouds per second: 1, cloud radius: 4, cloud effect: vfx_blob_death, body effect: vfx_blob_death }
    Devouring:   { move: 1, absorb health: 50, absorb damage: 25, slow per 100 health: 2, player threshold: 0.333, devour cooldown: 60, max prey health: 100, min meals: 1 }
    Thieving:    { max items: 1 }
    Gilded:      { loot: 3, bonus item: Coins, bonus amount: 20, flee distance: 30, glitter effect: vfx_Potion_stamina_medium }
    Blinking:    { health: 0.75, every: 30, distance: 4, tell time: 0.5, blink effect: vfx_ghost_spawn, tell effect: vfx_WishbonePing, tell sound: sfx_WishbonePing_near }
    Relentless:  { chase distance: 150 }
    Screecher:   { threshold: 15, radius: 20, mute time: 4, cooldown: 15, shriek sound: sfx_fallenvalkyrie_screech }

# Refilling cleared camps, dungeons and dungeon chests. Days are world days (30 real minutes).
respawning:
  camps: false
  camp days: 5
  dungeons: false
  dungeon days: 7
  dungeon loot: false
  dungeon loot days: 14

" + WorldRuleText.Block + BossRuleText.Block + @"# mode: Vanilla (drops untouched), Scaled (quantities times the star drops line),
# Rolled (the drop table rolled again per star) or Curated (only the creatures: rules).
loot:
  mode: Rolled
  # Chance of each extra roll, by star count.
  extra roll chance: [0, 100]
  # 0 = no cap.
  max extra rolls: 5
  # Multiply every dropped quantity after the mode. Bosses take both.
  global multiplier: 1
  boss multiplier: 1
  multiply trophies: false

# Per creature, by prefab name. `elite reference` writes every creature's name and
# drop table to creature_reference.yml. Keys: reference, Per-creature rules.
creatures:
  - match: Troll
    mutation power:
      Cloaked:     { reveal distance: 15 }
  - match: Lox
    mutation power:
      Cloaked:     { reveal distance: 15 }
  # Drakes are never Cloaked.
  - match: Hatchling
    mutation chances:
      Cloaked:     [0]
#  - match: Troll
#    drops: [1, 1.5, 2, 3, 4, 5]
#    multiply trophies: true
#    drop overrides:
#      - item: TrollHide
#        amount: [2, 5]
#        chance: 100
#      - item: Coins
#        remove: true
#    extra drops:
#      - item: Ruby
#        chance: 10
#        amount: [1, 1]
#        per star: true

# A biome overrides only what it names; the rest comes from defaults. star chances
# (Custom only): the chance of each star count, summing to 100; six entries allow
# up to five stars.
# An unlisted or modded biome takes the Meadows row.
biomes:
  - match: Meadows
    star chances:    [73, 10, 10, 5, 1, 1]
    mutation chance: [2.5,  3.5,  5,    6,    7.5,  10]

  - match: BlackForest
    star chances:    [62, 15, 12, 6, 3, 2]
    mutation chance: [3.5,  4.5,  6,    8,    10,   12.5]
    mutation chances:
      Thieving:    [6,  8,  11, 14, 17, 21]

  - match: Swamp
    star chances:    [52, 18, 14, 8, 5, 3]
    mutation chance: [4,    6,    8,    10,   12.5, 16]
    mutation chances:
      Miasmic:     [11, 16, 22, 28, 34, 42]
      Leeching:    [8,  12, 16, 21, 25, 31]

  - match: Mountain
    star chances:    [42, 20, 17, 10, 7, 4]
    mutation chance: [5,    7,    9,    12,   15,   18.5]
    mutation chances:
      Plated:      [16, 22, 30, 38, 46, 56]
      Cloaked:     [2,  3,  4,  5,  6,  7]

  - match: Plains
    star chances:    [32, 22, 20, 13, 8, 5]
    mutation chance: [6,    8,    11,   14,   17,   21.5]
    mutation chances:
      Mad:         [22, 30, 40, 50, 60, 72]
      Devouring:   [1.5, 2, 3, 4, 5, 6]
      Thieving:    [8,  11, 14, 18, 22, 27]

  - match: Mistlands
    star chances:    [22, 22, 22, 16, 11, 7]
    mutation chance: [6.5,  9,    12.5, 16,   20,   25]
    mutation chances:
      Cloaked:     [30, 40, 52, 64, 76, 90]
      Devouring:   [2, 3, 4, 5, 6, 8]
      Thieving:    [7,  10, 13, 17, 21, 26]

  - match: AshLands
    star chances:    [16, 22, 26, 21, 11, 4]
    mutation chance: [7.5,  10.5, 14,   18,   22,   28]
    mutation chances:
      Bloated:     [38, 50, 64, 78, 92, 100]
      Splintering: [28, 37, 48, 58, 69, 82]

  - match: DeepNorth
    star chances:    [16, 22, 26, 21, 11, 4]
    mutation chance: [7.5,  10.5, 14,   18,   22,   28]
    mutation chances:
      Plated:      [38, 50, 64, 78, 92, 100]

  - match: Ocean
    star chances:    [68, 12, 10, 6, 3, 1]
    mutation chance: [3,    4,    5.5,  7,    8.5,  11]
";
    }
}
