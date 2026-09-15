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

# When true, players on a server use the server's copy of this file and cannot
# override it. Their own file is ignored while connected and restored when they
# leave. Display preferences in the .cfg (colours, nameplate distance) are never
# locked - they change only what that player sees.
lock to server: true

# The most mutations one creature may carry. 1 by default: a creature is Mad, or
# it is Cloaked, but not both. Raise it for stacked monsters; 0 means no cap at
# all. Where more roll than the cap allows, the survivors are picked at random.
max mutations: 1

defaults:
  # How much stronger a mutation is when it sits on a large star (worth 5 stars)
  # rather than a small one. Multiplies the mutation's BONUS, never its cost: a
  # Mad creature on a large star is much faster but still has half health. 5 -
  # matching the star's worth - is available but produces absurdities like a
  # creature outrunning the player, so the default is deliberately lower. A biome
  # may override it, so Ash Lands large stars can be ferocious and the Meadows not.
  large star power: 2

  # What a star is worth. One entry per star count, so index 0 is an unstarred
  # creature and index 5 a five-star one.
  #   growth      - Size bonus, added to 1. 0.20 at 3 stars means 20% larger.
  #   hp          - Max health multiplier. 2.75 at 3 stars means 2.75x.
  #   attack      - Damage multiplier, applied to everything it deals.
  #   swing speed - Attack and animation speed multiplier. Deliberately gentle.
  #   speed       - Movement speed multiplier. Also gentle.
  #   drops       - Loot quantity multiplier. Stars do not pay until the second.
  star power:
    growth:      [0.06, 0.10, 0.15, 0.20, 0.25, 0.30]
    hp:          [1,    1.4,  1.95, 2.75, 3.85, 5.4]
    attack:      [1,    1.2,  1.45, 1.75, 2.1,  2.5]
    swing speed: [1,    1.02, 1.05, 1.08, 1.12, 1.16]
    speed:       [1,    1,    1.03, 1.06, 1.1,  1.15]
    drops:       [1,    1,    1.5,  2,    2.5,  3]

  # The chance that ANY ONE mutation appears, indexed by the creature's star
  # count. Applied to every mutation that has no entry of its own below. Each
  # mutation is rolled separately, so these do not sum to anything.
  mutation chance: [2.5, 3.5, 5, 6, 7.5, 10]

  # Per-mutation overrides. Anything not listed here uses `mutation chance` above.
  # Devouring is deliberately rarer everywhere: one of them changes a whole area.
  mutation chances:
    Devouring:   [0.6, 0.9, 1.2, 1.5, 1.8, 2.4]

  # How strong each mutation is. Named fields, not positional numbers. A field
  # marked (enhanced) is multiplied by `large star power` when the mutation sits
  # on a large star; a cost is never multiplied. Meaning:
  #   Mad         move          - Movement speed multiplier. 1.6 = 60% faster. (enhanced)
  #   Mad         attack speed  - Attack and animation speed multiplier. (enhanced)
  #   Mad         health        - Max health multiplier. 0.5 = half. Its cost.
  #   Bloated     health        - Max health multiplier. (enhanced)
  #   Bloated     delay         - Seconds between death and the blast. The blast
  #                               goes off at the corpse's resting place, not the
  #                               spot of death; the warning rides the corpse.
  #   Bloated     damage        - Blunt damage at 0 stars, times (1 + stars). (enhanced)
  #   Bloated     radius        - Blast radius in metres. (enhanced)
  #   Bloated     blast effect  - Vanilla prefab cloned for the explosion.
  #   Bloated     warning effect - Vanilla prefab played on the corpse during the delay.
  #   Cloaked     reveal distance - Metres at which it becomes visible. (enhanced)
  #   Cloaked     fade time     - Seconds to phase in or out. 0 snaps.
  #   Cloaked     fade margin   - Extra metres before it fades back out (stops strobing).
  #   Splintering damage        - Damage multiplier. 0.6 = 40% weaker.
  #   Splintering max generations - Cascade-depth cap. 0 = unlimited.
  #   Splintering max descendants - Live-descendant cap. 0 = unlimited.
  #   Leeching    regen         - Percent of max health regained per second. (enhanced)
  #   Leeching    lifesteal     - Percent of damage dealt returned as health. (enhanced)
  #   Warding     reflect       - Percent of incoming damage returned. (enhanced)
  #   Warding     knockback     - Force applied to a melee attacker. (enhanced)
  #   Plated      armour        - Percent armour bonus at full health, to 0 hurt. (enhanced)
  #   Plated      damage        - Percent damage bonus at zero health, to 0 full. (enhanced)
  #   Miasmic     cloud life    - Seconds a dropped cloud lasts.
  #   Miasmic     cloud damage  - Strength of the vanilla Poison applied to a player
  #                               it hits or who stands in a cloud - not direct
  #                               damage; the status effect does the harming. (enhanced)
  #   Miasmic     clouds per second - How often it drops one while moving. (enhanced)
  #   Miasmic     cloud radius  - Metres. The visible cloud matches this exactly.
  #   Miasmic     cloud effect  - Vanilla prefab cloned for a trail cloud.
  #   Miasmic     body effect   - Vanilla poison visual worn on the creature at all
  #                               times. It looks poisoned; it is not, and takes no harm.
  #   Devouring   absorb health - Percent of a victim's max health it keeps, added to
  #                               its own permanently. An instant kill always lands the
  #                               killing blow, so the whole amount is kept. (enhanced)
  #   Devouring   absorb damage - Percent of a victim's damage it keeps, permanently. (enhanced)
  #   Devouring   slow per 100 health - Percent speed lost per 100 eaten health. Its cost.
  #   Devouring   player threshold - Fraction of a player's max health its per-hit
  #                                  damage must pass before it hunts players for good.
  #   Devouring   devour cooldown - Seconds it must wait after a meal before it can eat
  #                                 again. During it the creature fights as an ordinary one.
  mutation power:
    Mad:         { move: 1.6, attack speed: 1.5, health: 0.5 }
    Bloated:     { health: 2.0, delay: 1.0, damage: 40, radius: 4, blast effect: fx_barrel_destroyed, warning effect: fx_Smoke }
    Cloaked:     { reveal distance: 6, fade time: 0.5, fade margin: 1 }
    Splintering: { damage: 0.6, max generations: 0, max descendants: 0 }
    Leeching:    { regen: 2, lifesteal: 30 }
    Warding:     { reflect: 30, knockback: 4 }
    Plated:      { armour: 100, damage: 60 }
    Miasmic:     { cloud life: 6, cloud damage: 5, clouds per second: 1, cloud radius: 4, cloud effect: vfx_blob_death, body effect: vfx_blob_death }
    Devouring:   { absorb health: 100, absorb damage: 100, slow per 100 health: 2, player threshold: 0.333, devour cooldown: 10 }

# Every biome below overrides only what it names. Delete a line to fall back to
# `defaults`; delete a whole biome to make it behave like the defaults entirely.
#
# `star chances` is per-biome ONLY - there is deliberately no global default,
# because how starry a biome is is the main thing that separates one from the
# next. One entry per star count, and they should add up to 100. This is a
# straight distribution, not a chain: [73, 10, 10, 5, 1, 1] means 73 creatures in
# 100 have no stars, 10 have one, 10 have two, 5 have three, 1 has four and 1 has
# five. Even the gentlest biome keeps a sliver at the top - that is the rare
# large-star creature, and it should exist everywhere, just barely. Adding a
# seventh entry raises that biome's ceiling to 6 stars, and so on. An unlisted or
# modded biome takes the Meadows row for stars.
biomes:
  - match: Meadows
    star chances:    [73, 10, 10, 5, 1, 1]
    mutation chance: [2.5,  3.5,  5,    6,    7.5,  10]

  - match: BlackForest
    star chances:    [62, 15, 12, 6, 3, 2]
    mutation chance: [3.5,  4.5,  6,    8,    10,   12.5]

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

  - match: Mistlands
    star chances:    [22, 22, 22, 16, 11, 7]
    mutation chance: [6.5,  9,    12.5, 16,   20,   25]
    mutation chances:
      Cloaked:     [30, 40, 52, 64, 76, 90]  # the mist hides things already
      Devouring:   [2, 3, 4, 5, 6, 8]

  - match: AshLands
    star chances:    [12, 20, 24, 20, 15, 9]
    mutation chance: [7.5,  10.5, 14,   18,   22,   28]
    mutation chances:
      Bloated:     [38, 50, 64, 78, 92, 100]
      Splintering: [28, 37, 48, 58, 69, 82]

  - match: DeepNorth
    star chances:    [12, 20, 24, 20, 15, 9]
    mutation chance: [7.5,  10.5, 14,   18,   22,   28]
    mutation chances:
      Plated:      [38, 50, 64, 78, 92, 100]

  - match: Ocean
    star chances:    [68, 12, 10, 6, 3, 1]
    mutation chance: [3,    4,    5.5,  7,    8.5,  11]
";
    }
}
