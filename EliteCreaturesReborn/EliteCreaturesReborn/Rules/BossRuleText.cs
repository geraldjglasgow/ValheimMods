namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// The `bosses:` section of the default rule file - the boss star table and the boss aspects - exactly as it is
    /// written on first run, comments and all. Kept apart from <see cref="RuleText"/> only for length; the two are
    /// joined into one file at compile time. This is data, not logic.
    /// </summary>
    internal static class BossRuleText
    {
        public const string Block =
@"# Bosses scale on their own table, never the creature lines above, and never take
# mutations - they take an aspect instead. `stars: false` turns boss stars off;
# with aspects off as well, every boss is exactly as the game ships it.
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

  # An aspect is one modifier that changes what kind of fight a boss is. The
  # altar shows the current one before you summon, and shifts it every
  # `shift hours` in-game hours (one hour is 1/24 of the game's day - 75 real
  # seconds by default); 0 fixes each altar's aspect for good. The aspect on the
  # bowl when you make the offering is the one you fight. A boss with no altar
  # (the Queen, a console spawn) rolls its aspect when it first appears.
  aspects:
    enabled: true
    shift hours: 1

    # Relative weights, not percentages. `none` is a plain vanilla fight. An
    # altar never shifts to the aspect it already shows.
    chances:
      none: 20
      Reflective: 10
      Shielded: 10
      Mending: 10
      Summoner: 10
      Elementalist: 10
      Enraged: 10
      Twin: 10
      Phantom: 10

    # Multiplies everything the boss drops, on top of the star `drops` line and
    # `boss multiplier` - and even in Vanilla loot mode. Trophies follow the
    # `multiply trophies` switch. Both Twins drop full loot, so Twin pays double
    # at 1. Phantom copies never drop anything.
    loot:
      none: 1
      Twin: 1
      Shielded: 1.1
      Enraged: 1.2
      Elementalist: 1.2
      Mending: 1.3
      Phantom: 1.3
      Reflective: 1.4
      Summoner: 1.5

    # Percentages are of the boss as its stars left it.
    #   Reflective   reflect - % of each hit you land that comes back to you, as
    #                true damage armour does not reduce (burn/poison ticks never)
    #   Shielded     arrow reduction - % less damage from bows and crossbows
    #   Mending      regen - % of max health healed every second, in combat too
    #   Summoner     every - % of max health lost per wave; count - creatures per
    #                wave; stars - stars each summoned creature has
    #   Elementalist elemental bonus - % more fire/frost/lightning/poison/spirit
    #   Enraged      physical bonus - % more blunt/slash/pierce
    #   Twin         less health, less damage - % less for each of the two; they
    #                share one health pool and die together
    #   Phantom      copies - how many; health - each copy's max health;
    #                less damage - % less than the boss deals
    power:
      Reflective:   { reflect: 15 }
      Shielded:     { arrow reduction: 30 }
      Mending:      { regen: 0.3 }
      Summoner:     { every: 33, count: 2, stars: 2 }
      Elementalist: { elemental bonus: 20 }
      Enraged:      { physical bonus: 20 }
      Twin:         { less health: 25, less damage: 25 }
      Phantom:      { copies: 4, health: 100, less damage: 50 }

    # Per boss, by prefab name. `summons` is what Summoner calls (a boss with no
    # list never rolls Summoner); `aspects: [none, Twin, ...]` narrows that boss's
    # rotation. `elite reference` lists every boss's exact prefab name.
    per boss:
      - match: Eikthyr
        summons: [Boar, Neck]
      - match: gd_king
        summons: [Greydwarf_Elite, Greydwarf_Shaman]
      - match: Bonemass
        summons: [Draugr_Elite, BlobElite]
      - match: Dragon
        summons: [Hatchling]
      - match: GoblinKing
        summons: [GoblinBrute, GoblinShaman]
      - match: SeekerQueen
        summons: [SeekerBrute, Seeker]
      - match: Fader
        summons: [Charred_Melee, Charred_Archer]

";
    }
}
