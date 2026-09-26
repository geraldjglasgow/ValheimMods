namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// The `world tiers:` and `breeding:` sections of the default rule file, exactly as they are written on first run,
    /// comments and all. Kept apart from <see cref="RuleText"/> only for length; the pieces are joined into one file at
    /// compile time. This is data, not logic.
    /// </summary>
    internal static class WorldRuleText
    {
        public const string Block =
@"# World tiers. The world starts at tier 0 and goes up one tier the first time
# each boss below is defeated - by anyone, in any order. Killing a boss again
# changes nothing, so the seven bosses take the world from tier 0 to tier 7.
# Everyone is told when the tier rises. The tier makes stars and mutations more
# common in every biome; bosses roll on their own table and are unaffected.
# Creatures already alive keep what they rolled - the tier only changes what
# the next one rolls. `elite tier` shows the tier and which bosses count.
world tiers:
  enabled: true
  # The world key each boss sets when it dies. A modded boss counts once its
  # key is listed here; `elite tier` lists every boss the game knows with its
  # key. Admins can try a tier out with the game's setkey and removekey.
  bosses: [defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader]
  # One entry per tier (index 0 = no boss down yet); past the end the last
  # entry repeats.
  #   star boost     - each star count's weight in a biome's `star chances` is
  #                    multiplied by this once per star, then the row is scaled
  #                    back to 100. At 1.5 a one-star creature becomes 1.5 times
  #                    as likely against an unstarred one, a two-star 2.25 times,
  #                    a five-star 7.6 times. 1 leaves stars alone.
  #   mutation boost - multiplies every mutation chance, capped at 100.
  star boost:     [1, 1.1,  1.2, 1.3,  1.4, 1.5,  1.6, 1.7]
  mutation boost: [1, 1.15, 1.3, 1.45, 1.6, 1.75, 1.9, 2]

# Breeding tamed creatures. A newborn - a pup, a piglet, a calf, or an egg and
# the chick that hatches from it - inherits from its two parents instead of
# rolling like a wild creature. It takes one of their mutations, picked at
# random, when either parent has one, and a star count drawn with equal odds
# from 0 up to the stronger parent's: a 4-star and a 1-star can have a pup of
# 0 to 4 stars. Two plain parents have plain young. A mutation switched off in
# `mutations enabled` is never passed on. An egg keeps its traits when carried;
# its stars travel as its quality, so eggs only stack with eggs of the same
# stars, and a stack keeps a single mutation. Young creatures keep their traits
# when they grow up, whatever this block says. `enabled: false` lets newborns
# roll like wild creatures instead.
breeding:
  enabled: true
  # Chance a newborn takes one of its parents' mutations when either has one.
  mutation chance: 100

";
    }
}
