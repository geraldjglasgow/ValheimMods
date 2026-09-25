using System;
using EliteCrafting.Core;
using EliteCrafting.Effects;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// The running rules, and the one place they change. Every reader takes <see cref="Current"/> (an immutable
    /// snapshot of both YAML families) and, if it precomputes anything from it, listens to <see cref="RulesChanged"/>.
    /// Whether the rules come from this machine's files or from the server is decided by <see cref="RuleFamily{T}"/>.
    /// </summary>
    public static class ActiveRules
    {
        private static RuleFamily<AffixRules>? _affixes;
        private static RuleFamily<EconomyRules>? _economy;
        private static int _generation;

        /// <summary>The active snapshot. <see cref="RuleSet.Empty"/> only before plugin Awake has loaded the rules.</summary>
        public static RuleSet Current { get; private set; } = RuleSet.Empty;

        public static int Generation => Current.Generation;

        /// <summary>Raised on the main thread after <see cref="Current"/> was replaced, on every peer.</summary>
        public static event Action? RulesChanged;

        /// <summary>Plugin Awake: write missing main files, read the local files, bind to the server via Charter.</summary>
        internal static void Initialise(Charter.Charter charter)
        {
            EffectRegistry.Freeze();
            _affixes = new RuleFamily<AffixRules>(FamilySpec.Affixes, AffixFamilyParser.Parse);
            _economy = new RuleFamily<EconomyRules>(FamilySpec.Economy, EconomyParser.Parse);
            _affixes.Adopted += Compose;
            _economy.Adopted += Compose;
            _affixes.Setup(charter);
            _economy.Setup(charter);
            RuleReload.Attach(Poll);
        }

        /// <summary>
        /// Re-reads this machine's files now (<c>ecraft reload</c>) and says per family what happened; a bound player
        /// keeps the server's rules. An applied family raises <see cref="RulesChanged"/> as usual.
        /// </summary>
        public static (FamilyReload Affixes, FamilyReload Economy) ReloadLocal()
        {
            FamilyReload affixes = _affixes?.ReloadLocal() ?? FamilyReload.NoFiles;
            FamilyReload economy = _economy?.ReloadLocal() ?? FamilyReload.NoFiles;
            return (affixes, economy);
        }

        /// <summary>
        /// The texts the active rules of a family were built from (<see cref="FamilySpec.Affixes"/> or
        /// <see cref="FamilySpec.Economy"/>): the server's files on a bound player, this machine's otherwise.
        /// </summary>
        internal static RuleSources SourcesInForce(FamilySpec family) =>
            (family == FamilySpec.Affixes ? _affixes?.InForce : _economy?.InForce) ?? RuleSources.None;

        private static void Poll()
        {
            _affixes?.PollDisk();
            _economy?.PollDisk();
        }

        private static void Compose()
        {
            if (_affixes?.Active == null || _economy?.Active == null)
            {
                return;
            }
            Current = new RuleSet(_affixes.Active, _economy.Active, ++_generation);
            Log.Info($"rules applied (generation {_generation}): {Current.Affixes.Affixes.Count} affixes, "
                + $"{Current.Economy.Rarities.Count} rarities, {Current.Economy.Stones.Count} stones");
            EssenceMemberChecks.Run(Current);
            Raise();
        }

        // Each handler runs on its own: one feature's exception must not keep the others on stale rules.
        private static void Raise()
        {
            Delegate[] handlers = RulesChanged?.GetInvocationList() ?? Array.Empty<Delegate>();
            foreach (Delegate handler in handlers)
            {
                try
                {
                    ((Action)handler)();
                }
                catch (Exception e)
                {
                    Log.Error($"a RulesChanged handler threw: {e}");
                }
            }
        }
    }
}
