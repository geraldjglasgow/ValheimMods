using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;

namespace EliteCreaturesReborn.Compendium
{
    /// <summary>
    /// The mod's pages in the game's compendium (the inventory's Compendium button): world difficulty, stars, mutations
    /// and boss aspects, written each time the compendium opens from the rules this machine holds - the server's, synced
    /// - and the world's own keys, so they always show this server's numbers right now. They go in after the game's own
    /// Active effects and Logs pages, in a fixed order, ahead of the lore.
    /// </summary>
    internal static class CompendiumPages
    {
        /// <summary>The game puts Active effects and Logs at the top; ours follow them.</summary>
        private const int After = 2;

        public static void AddTo(List<TextsDialog.TextInfo> texts)
        {
            if (!Configuration.ShowCompendium.Value || Player.m_localPlayer == null)
            {
                return;
            }
            RuleSet set = RuleState.Active;
            int tier = WorldTier.Current();
            Heightmap.Biome here = PageText.Here();
            List<TextsDialog.TextInfo> pages = new List<TextsDialog.TextInfo>
            {
                new TextsDialog.TextInfo(DifficultyPage.Topic, DifficultyPage.Text(set, tier, here)),
                new TextsDialog.TextInfo(StarsPage.Topic, StarsPage.Text(set, here)),
                new TextsDialog.TextInfo(MutationsPage.Topic, MutationsPage.Text(set, here)),
                new TextsDialog.TextInfo(AspectsPage.Topic, AspectsPage.Text(set)),
            };
            texts.InsertRange(System.Math.Min(After, texts.Count), pages);
        }
    }
}
