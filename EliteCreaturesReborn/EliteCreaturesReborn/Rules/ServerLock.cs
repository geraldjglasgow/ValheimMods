using BepInEx.Configuration;
using Charter;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Util;
using HarmonyLib;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// Binds every connected player to the server's rule file through Charter. The .cfg's 'Lock to server' is Charter's
    /// binding switch; the server pushes the file's text as an article, a bound player adopts that text and ignores its
    /// own, and gets its own back the moment it disconnects. Charter also refuses a player whose mod version does not
    /// match. This class owns the decision of which rule set is active; it never parses YAML itself. The .cfg's gameplay
    /// settings ride the same binding as clauses, so while the server binds their values hold for every player: the
    /// rules' switches (<see cref="RuleSettings"/>, <see cref="MutationSettings"/>, <see cref="RespawnSettings"/>), the
    /// raids' Raiders Chest (<see cref="RaidSettings"/>), the Arena (<see cref="ArenaSettings"/>), the Altar Choice
    /// (<see cref="AltarSettings"/>), the horde's four (<see cref="HordeSettings"/>), what tamed creatures keep
    /// (<see cref="TamedSettings"/>) and the Rendering Table (<see cref="TableSettings"/>).
    /// </summary>
    internal static class ServerLock
    {
        private const string ArticleName = "creature_rules";
        private static Charter.Charter _charter = null!;
        private static Article<string> _rules = null!;

        public static void Setup(string guid, string title, string version)
        {
            _charter = new Charter.Charter(guid, title, version, oldestAccepted: null, mandatory: true);
            _charter.Binding(RuleSettings.LockToServer);
            RuleClauses();
            _charter.Clause(RaidSettings.RaidersChest);
            _charter.Clause(ArenaSettings.Arena);
            _charter.Clause(AltarSettings.Choice);
            _charter.Clause(HordeSettings.Horde);
            _charter.Clause(HordeSettings.SpawnMultiplier);
            _charter.Clause(HordeSettings.HealthMultiplier);
            _charter.Clause(HordeSettings.AttackMultiplier);
            _charter.Clause(TamedSettings.Stars);
            _charter.Clause(TamedSettings.Mutations);
            _charter.Clause(TableSettings.RenderingTable);
            _rules = new Article<string>(_charter, ArticleName, RuleFile.LocalText);
            _rules.Changed += () => Guard("rule push", Recompute);
            _charter.BindingChanged += () => Guard("binding change", OnBindingChanged);
            Publish();
            Recompute();
        }

        // The rule file's old single switches, now in the .cfg.
        private static void RuleClauses()
        {
            _charter.Clause(RuleSettings.DifficultyChoice);
            _charter.Clause(RuleSettings.CreatureStars);
            _charter.Clause(RuleSettings.MaxMutations);
            foreach (ConfigEntry<bool> entry in MutationSettings.All)
            {
                _charter.Clause(entry);
            }
            _charter.Clause(RespawnSettings.Camps);
            _charter.Clause(RespawnSettings.CampDays);
            _charter.Clause(RespawnSettings.Dungeons);
            _charter.Clause(RespawnSettings.DungeonDays);
            _charter.Clause(RespawnSettings.DungeonLoot);
            _charter.Clause(RespawnSettings.DungeonLootDays);
        }

        public static void Install(Harmony harmony) => Charter.Charter.Install(harmony);

        /// <summary>
        /// The server's rules are in (always, on the server and in single player). A player that just joined holds its own
        /// file until the first push, which on a slow connection can take minutes: nothing is rolled by it before then.
        /// </summary>
        public static bool RulesArrived => _charter == null || _charter.Received;

        /// <summary>Called after this machine's own file reloads: the server re-pushes the text.</summary>
        public static void OnLocalReloaded()
        {
            Publish();
            Recompute();
        }

        /// <summary>
        /// A .cfg setting the rules copy changed (an edit, a reload, a steward's amendment, the server's push): the rules
        /// are read again with it. On a bound player the push carries the server's value, so the server's text is.
        /// </summary>
        public static void OnSettingsChanged()
        {
            if (_charter == null)
            {
                return;
            }
            if (_charter.IsAuthor)
            {
                RuleFile.Reparse();
            }
            Guard("setting change", Recompute);
        }

        private static void OnBindingChanged()
        {
            if (_charter.IsAuthor)
            {
                RuleFile.Load();
            }
            Recompute();
        }

        private static void Publish()
        {
            if (_charter.IsAuthor)
            {
                _rules.Assign(RuleFile.LocalText);
            }
        }

        private static void Recompute()
        {
            if (_charter.IsAuthor)
            {
                RuleState.Adopt(RuleFile.Local);
                return;
            }
            RuleParser.Result parsed = RuleParser.Parse(_rules.Value);
            if (parsed.Rules != null && parsed.Errors.Count == 0)
            {
                RuleState.Adopt(parsed.Rules);
            }
        }

        private static void Guard(string what, System.Action action)
        {
            try
            {
                action();
            }
            catch (System.Exception e)
            {
                Log.Error($"server lock ({what}) threw: {e}");
            }
        }
    }
}
