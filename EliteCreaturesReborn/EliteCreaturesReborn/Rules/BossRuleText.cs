namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// The `bosses:` section of the default rule file - the boss star table and the boss aspects - exactly as it is
    /// written on first run. Kept apart from <see cref="RuleText"/> only for length; the two are joined into one file
    /// at compile time. This is data, not logic.
    /// </summary>
    internal static class BossRuleText
    {
        public const string Block =
@"# Bosses roll on their own table and take an aspect instead of mutations.
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

  aspects:
    enabled: true
    # In-game hours between altar shifts (one hour = 75 real seconds). 0 fixes each altar.
    shift hours: 1

    # Weights, not percentages. none is the fight as the game ships it.
    chances:
      none: 30
      Reflective: 10
      Shielded: 10
      Mending: 10
      Summoner: 10
      Elementalist: 10
      Enraged: 10
      Twin: 10
      Phantom: 10
      Adaptive: 10
      Fixated: 10
      Stormbound: 10
      Gravitic: 10
      Colossal: 10

    # Multiplies everything the boss drops, even in Vanilla loot mode.
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
      Stormbound: 1.2
      Colossal: 1.2
      Adaptive: 1.3
      Fixated: 1.3
      Gravitic: 1.3

    # Field meanings: reference, Boss aspect power fields.
    power:
      Reflective:   { reflect: 15 }
      Shielded:     { arrow reduction: 30 }
      Mending:      { regen: 0.3 }
      Summoner:     { every: 33, count: 2, stars: 2 }
      Elementalist: { elemental bonus: 20 }
      Enraged:      { physical bonus: 20 }
      Twin:         { less health: 25, less damage: 25 }
      Phantom:      { split at: [66, 33], per player: 1, health per tier: 25, less damage: 50 }
      Adaptive:     { resist: 50, window: 15 }
      Fixated:      { marked bonus: 50, others less: 30, every: 30 }
      Stormbound:   { every: 20, tell time: 2, radius: 2.5, damage: 8, range: 40 }
      Gravitic:     { every: 20, range: 30, pull time: 1.5, pull speed: 6, slam radius: 6, slam damage: 10 }
      Colossal:     { bigger: 40, more health: 15, slower: 15, shockwave radius: 8 }

    # Per boss, by prefab name: summons is what Summoner calls (no list, no Summoner);
    # aspects, if set, limits which aspects it rolls.
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
      # Elite Creatures Pack's kraken holds one ship: never Twin or Phantom.
      - match: ECP_Kraken
        aspects: [none, Reflective, Shielded, Mending, Summoner, Elementalist, Enraged, Adaptive, Fixated, Stormbound, Gravitic, Colossal]

";
    }
}
