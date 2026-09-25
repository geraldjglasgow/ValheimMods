using EliteCrafting.Config;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Commands
{
    /// <summary>
    /// <c>ecraft reload</c> (console-commands.md section 3, configuration.md section 5): re-reads both YAML families,
    /// the translation files and the <c>.cfg</c> now instead of on the next poll. Admin, author side only (checked by
    /// <see cref="CommandAccess"/>): single player, the host, a dedicated server's console, or a player the server does
    /// not bind. On a server a successful family build is re-published to every connected player by the rules code.
    /// Whether a family applied comes from <see cref="ActiveRules.ReloadLocal"/>'s per-family result.
    /// </summary>
    internal static class ReloadCommand
    {
        public static void Run(CommandCall call)
        {
            (FamilyReload affixes, FamilyReload economy) = ActiveRules.ReloadLocal();
            Words.InstallNow();
            ModSettings.AffixEffects?.ConfigFile.Reload();
            RuleSet now = ActiveRules.Current;
            call.Reply($"rules generation {now.Generation}");
            call.Detail("affixes: " + Describe(affixes, $"reloaded, {now.Affixes.Affixes.Count} affixes"));
            call.Detail("economy: " + Describe(economy, $"reloaded, {now.Economy.Rarities.Count} rarities, {now.Economy.Stones.Count} stones"));
            call.Detail("translations and com.EliteCrafting.cfg: re-read");
        }

        private static string Describe(FamilyReload result, string applied)
        {
            switch (result)
            {
                case FamilyReload.Applied: return applied;
                case FamilyReload.Bound: return "the server's rules stay in force";
                case FamilyReload.NoFiles: return "no files found, the loaded rules stay";
                default: return "errors, previous configuration kept, see log";
            }
        }
    }
}
