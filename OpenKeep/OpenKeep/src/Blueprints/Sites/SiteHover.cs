using System.Collections.Generic;
using OpenKeep.Core;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// A construction site's hover text in the game's own style: its name and who placed it, how many pieces stand,
    /// what it still lacks (the five biggest), how it builds, and the keys - E to hand over materials, Shift + E (shown
    /// as the game shows its alternative use) to take it down, the latter only for its creator and admins.
    /// </summary>
    public static class SiteHover
    {
        private const string Key = "[<color=yellow><b>$1</b></color>] ";

        public static string Text(SiteMarker site)
        {
            SiteState s = site.State;
            Blueprint bp = s?.Blueprint;
            if (bp == null)
                return Language.Localize(SiteWords.MarkerName);
            List<string> lines = new List<string>
            {
                "<b>" + BlueprintWords.Format(SiteWords.Title, s.Name, s.CreatorName) + "</b>",
                BlueprintWords.Format(SiteWords.BuiltCount, s.BuiltCount, bp.Pieces.Count),
            };
            if (!BlueprintSettings.FreeMaterials)
                lines.Add(Needs(SiteNeeds.Missing(site)));
            lines.Add(Language.Localize(HowItBuilds()));
            lines.Add(Language.Localize(Key.Replace("$1", "$KEY_Use") + SiteWords.Deliver));
            Player player = Player.m_localPlayer;
            if (player != null && SiteTakeDown.MayAsk(site, player))
                lines.Add(Language.Localize(Key.Replace("$1", AltUse()) + SiteWords.TakeDown));
            return string.Join("\n", lines);
        }

        private static string Needs(Dictionary<string, int> missing)
        {
            string text = SiteCosts.Describe(missing, 5);
            return text == null ? Language.Localize(SiteWords.HasAll) : BlueprintWords.Format(SiteWords.Needed, text);
        }

        private static string HowItBuilds()
        {
            if (BlueprintSettings.FreeMaterials)
                return SiteWords.FreeBuild;
            return SiteSettings.PieceByPiece ? SiteWords.PieceByPiece : SiteWords.AllAtOnce;
        }

        /// <summary>The alternative use as the game names it: Shift + E, or the gamepad's own keys.</summary>
        private static string AltUse()
        {
            return ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive() ? "$KEY_AltKeys + $KEY_Use" : "$KEY_AltPlace + $KEY_Use";
        }
    }
}
