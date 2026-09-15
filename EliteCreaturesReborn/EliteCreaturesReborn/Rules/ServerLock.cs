using BepInEx.Configuration;
using Charter;
using EliteCreaturesReborn.Util;
using HarmonyLib;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// Binds every connected player to the server's rule file through Charter. The server mirrors the file's `lock to
    /// server` into Charter's binding switch and pushes the file's text as an article; a bound player adopts that text
    /// and ignores its own, and gets its own back the moment it disconnects. Charter also refuses a player whose mod
    /// version does not match. This class owns the decision of which rule set is active; it never parses YAML itself.
    /// </summary>
    internal static class ServerLock
    {
        private const string ArticleName = "creature_rules";
        private static Charter.Charter _charter = null!;
        private static Article<string> _rules = null!;
        private static ConfigEntry<bool> _binding = null!;

        public static void Setup(ConfigFile config, string guid, string title, string version)
        {
            _charter = new Charter.Charter(guid, title, version, oldestAccepted: null, mandatory: true);
            _binding = config.Bind("1 - General", "Lock to server", true,
                "Mirrors the rule file's 'lock to server'. The server governs this; a player's own copy is ignored while bound.");
            _charter.Binding(_binding);
            _rules = new Article<string>(_charter, ArticleName, RuleFile.LocalText);
            _rules.Changed += () => Guard("rule push", Recompute);
            _charter.BindingChanged += () => Guard("binding change", OnBindingChanged);
            MirrorBinding();
            Publish();
            Recompute();
        }

        public static void Install(Harmony harmony) => Charter.Charter.Install(harmony);

        /// <summary>Called after this machine's own file reloads: the server re-mirrors the lock and re-pushes the text.</summary>
        public static void OnLocalReloaded()
        {
            MirrorBinding();
            Publish();
            Recompute();
        }

        private static void OnBindingChanged()
        {
            if (_charter.IsAuthor)
            {
                RuleFile.Load();
            }
            Recompute();
        }

        private static void MirrorBinding()
        {
            if (_charter.IsAuthor)
            {
                _binding.Value = RuleFile.Local.LockToServer;
            }
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
