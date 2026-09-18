namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// The full, commented rule file exactly as it is written on first run: every value at its default and every
    /// comment in place, including the meaning of every star-power line and every mutation-power field. A server
    /// owner always edits a complete, documented file rather than a blank one. This is data, not logic.
    /// </summary>
    internal static class RuleText
    {
        public const string FileName = "creature_rules.yml";

        public const string Default =
@"# Elite Creatures Reborn - creature rules
#
# Percentages are 0-100. Anything a biome does not set falls back to `defaults`.
# Biome names must match the game's own: Meadows, BlackForest, Swamp, Mountain,
# Plains, Mistlands, AshLands, DeepNorth, Ocean.

# When true, connected players use the server's copy and cannot override it locally.
lock to server: true

# Most mutations one creature may carry at once. 0 means no cap. Where more roll
# than the cap allows, the survivors are picked at random.
max mutations: 1

# Turn a mutation off everywhere, regardless of its chance curves below. No biome
# block can turn it back on. Creatures already spawned keep whatever they rolled -
# this only changes what the next one rolls.
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

defaults:
  # How much stronger a mutation is on a large star (worth 5) versus a small one.
  # Multiplies the mutation's bonus, never its cost. A biome may override this.
  large star power: 1

  # What a star is worth, one entry per star count (index 0 = unstarred).
  #   growth, hp, attack   - size, max health and damage multipliers
  #   swing speed, speed   - attack/animation and movement multipliers, kept gentle
  #   drops                - loot quantity multiplier
  star power:
    growth:      [0.06, 0.10, 0.15, 0.20, 0.25, 0.30]
    hp:          [1,    1.4,  1.95, 2.6,  3.3,  4.0]
    attack:      [1,    1.2,  1.45, 1.75, 2.1,  2.5]
    swing speed: [1,    1.02, 1.05, 1.08, 1.12, 1.16]
    speed:       [1,    1,    1.03, 1.06, 1.1,  1.15]
    drops:       [1,    1,    1.5,  2,    2.5,  3]

  # Chance any one mutation appears, by star count. Applies to any mutation with
  # no override below; each mutation rolls separately, so these don't sum to 100.
  mutation chance: [2.5, 3.5, 5, 6, 7.5, 10]

  # Per-mutation overrides of the curve above.
  mutation chances:
    Devouring:   [0.6, 0.9, 1.2, 1.5, 1.8, 2.4]

  # How strong each mutation is, in named fields rather than positional numbers.
  # A field marked (enhanced) in the README is multiplied by `large star power`
  # on a large star; a cost field never is. Full field-by-field meanings are in
  # the mod's README, under Mutation power fields.
  mutation power:
    Mad:         { move: 1.6, attack speed: 1.5, health: 0.5 }
    Bloated:     { health: 2.0, delay: 1.0, damage: 40, radius: 4, blast effect: fx_barrel_destroyed, warning effect: fx_Smoke }
    Cloaked:     { reveal distance: 6, fade time: 0.5, fade margin: 1 }
    Splintering: { damage: 0.6, max generations: 0, max descendants: 0 }
    Leeching:    { regen: 0.5, lifesteal: 10, regen cap: 20, combat cooldown: 5 }
    Warding:     { reflect: 30, knockback: 4 }
    Plated:      { armour: 40, damage: 60, max reduction: 55 }
    Miasmic:     { cloud life: 6, cloud damage: 5, clouds per second: 1, cloud radius: 4, cloud effect: vfx_blob_death, body effect: vfx_blob_death }
    Devouring:   { absorb health: 100, absorb damage: 100, slow per 100 health: 2, player threshold: 0.333, devour cooldown: 10 }
    Thieving:    { max items: 1 }

# Repopulating the world. All off by default, so clearing a camp or dungeon
# stays worth doing. Timers are in world days (one day = 30 real minutes at
# default speed). A spawner the game already timers on its own is left alone; a
# chest refills only once it has been fully emptied.
respawning:
  camps: false
  camp days: 5
  dungeons: false
  dungeon days: 7
  dungeon loot: false
  dungeon loot days: 14

# Bosses scale on their own table, never the creature lines above, and never take
# mutations. `stars: false` leaves every boss exactly as the game ships it.
# `star chances` are weights per star count (index 0 = no stars); 90 in 100 stay
# plain by default. `star power` climbs harder than a creature's on health and
# damage but less on growth, and leaves swing speed/speed at 1 so a boss fight
# stays readable.
bosses:
  stars: true
  star chances: [90, 6, 3, 1]
  star power:
    growth:      [0,   0.05, 0.10, 0.15, 0.20, 0.20]
    hp:          [1,   1.5,  2.25, 3.4,  5.1,  7.6]
    attack:      [1,   1.25, 1.55, 1.9,  2.3,  2.75]
    swing speed: [1]
    speed:       [1]
    drops:       [1,   1.5,  2,    2.5,  3,    3.5]

# Every biome below overrides only what it names; delete a line to fall back to
# `defaults`, or a whole biome to use defaults entirely.
#
# `star chances` is per-biome only, with no global default - it's the main thing
# that tells one biome apart from the next. One entry per star count, summing to
# 100: [73, 10, 10, 5, 1, 1] means 73 in 100 creatures are unstarred, 10 have one
# star, and so on up to 1 in 100 with five. A seventh entry raises the ceiling to
# six stars. An unlisted or modded biome takes the Meadows row.
biomes:
  - match: Meadows
    star chances:    [73, 10, 10, 5, 1, 1]
    mutation chance: [2.5,  3.5,  5,    6,    7.5,  10]

  - match: BlackForest
    star chances:    [62, 15, 12, 6, 3, 2]
    mutation chance: [3.5,  4.5,  6,    8,    10,   12.5]
    mutation chances:
      Thieving:    [6,  8,  11, 14, 17, 21]   # greydwarves already take things that are not theirs

  - match: Swamp
    star chances:    [52, 18, 14, 8, 5, 3]
    mutation chance: [4,    6,    8,    10,   12.5, 16]
    mutation chances:
      Miasmic:     [11, 16, 22, 28, 34, 42]   # rot belongs here
      Leeching:    [8,  12, 16, 21, 25, 31]

  - match: Mountain
    star chances:    [42, 20, 17, 10, 7, 4]
    mutation chance: [5,    7,    9,    12,   15,   18.5]
    mutation chances:
      Plated:      [16, 22, 30, 38, 46, 56]
      Cloaked:     [2,  3,  4,  5,  6,  7]   # little cover up there

  - match: Plains
    star chances:    [32, 22, 20, 13, 8, 5]
    mutation chance: [6,    8,    11,   14,   17,   21.5]
    mutation chances:
      Mad:         [22, 30, 40, 50, 60, 72]
      Devouring:   [1.5, 2, 3, 4, 5, 6]
      Thieving:    [8,  11, 14, 18, 22, 27]  # fulings, and a biome where you are carrying something worth taking

  - match: Mistlands
    star chances:    [22, 22, 22, 16, 11, 7]
    mutation chance: [6.5,  9,    12.5, 16,   20,   25]
    mutation chances:
      Cloaked:     [30, 40, 52, 64, 76, 90]  # the mist hides things already
      Devouring:   [2, 3, 4, 5, 6, 8]
      Thieving:    [7,  10, 13, 17, 21, 26]  # a thief you cannot see is the encounter this mutation is for

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
