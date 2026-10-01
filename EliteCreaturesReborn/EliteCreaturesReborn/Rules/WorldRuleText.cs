namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// The `world tiers:` and `breeding:` sections of the default rule file, exactly as they are written on first run.
    /// Kept apart from <see cref="RuleText"/> only for length; the pieces are joined into one file at compile time.
    /// This is data, not logic.
    /// </summary>
    internal static class WorldRuleText
    {
        public const string Block =
@"# Each boss's first defeat raises the world tier by one. `elite tier` shows it.
world tiers:
  enabled: true
  # The world key each boss sets when it dies. `elite tier` lists every boss's key.
  bosses: [defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader]
  # By tier, from tier 0. star boost multiplies each star count's weight once per
  # star; mutation boost multiplies every mutation chance.
  star boost:     [1, 1.1,  1.2, 1.3,  1.4, 1.5,  1.6, 1.7]
  mutation boost: [1, 1.15, 1.3, 1.45, 1.6, 1.75, 1.9, 2]

breeding:
  enabled: true
  # Chance a newborn takes one of its parents' mutations when either has one.
  mutation chance: 100

";
    }
}
