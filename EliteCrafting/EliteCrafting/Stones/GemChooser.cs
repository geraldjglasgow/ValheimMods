using EliteCrafting.Affixes;
using EliteCrafting.Display;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// A gem onto an item whose sockets are all full (sockets.md section 4, user decision 2026-10-07): the player picks
    /// the socket whose gem it replaces. The game's own yes/no popup asks once per filled socket, in order ("Socket 2 holds
    /// Thor's Gem: +6 lightning damage. Replace it?"); Yes replaces that one (the old gem is lost), No asks about the next,
    /// No on the last keeps everything. Yes re-runs every check first, aimed at the chosen socket. Local player only.
    /// </summary>
    internal static class GemChooser
    {
        public static void Ask(StoneJob job)
        {
            if (!UnifiedPopup.IsAvailable() || job.State.Gems.Count == 0)
            {
                StoneFeedback.Show(job.Player, StoneResult.Refuse("sockets_full").Refusal!);
                return;
            }
            AskAt(job, 0);
        }

        private static void AskAt(StoneJob job, int index)
        {
            if (index >= job.State.Gems.Count)
            {
                return;
            }
            int socket = job.State.GemSocketAt(index);
            Push(job, index, () => Chosen(job, socket), () => Next(job, index));
        }

        /// <summary>
        /// A gem aimed at a socket the player picked (the Rune Table's socket row): asks once whether to replace the gem
        /// there, then sets it. A socket whose entry is unreadable is replaced without asking (there is nothing to name).
        /// </summary>
        public static void Confirm(StoneJob job, int socket)
        {
            int index = -1;
            for (int i = 0; i < job.State.Gems.Count; i++)
            {
                index = job.State.GemSocketAt(i) == socket ? i : index;
            }
            if (index < 0 || !UnifiedPopup.IsAvailable())
            {
                Set(job, socket);
                return;
            }
            Push(job, index, () => Chosen(job, socket), UnifiedPopup.Pop);
        }

        private static void Push(StoneJob job, int index, PopupButtonCallback yes, PopupButtonCallback no)
        {
            string socket = (job.State.GemSocketAt(index) + 1).ToString();
            string title = Words.Localize("$ecf_ui_gem_replace_title", job.StoneName);
            string body = Words.Localize("$ecf_ui_gem_replace_body", socket, Holding(job.State, index), job.StoneName);
            UnifiedPopup.Push(new YesNoPopup(title, body, yes, no, localizeText: false));
        }

        /// <summary>"Thor's Gem: +6 lightning damage" for gem <paramref name="index"/>.</summary>
        private static string Holding(ItemState state, int index)
        {
            GemRoll gem = state.Gems[index];
            AffixDef? def = state.GemDefinitionAt(index);
            StoneDef? stone = ActiveRules.Current.Economy.Stone(gem.GemId);
            string name = Words.Localize(stone?.Name ?? "$ecf_stone_" + gem.GemId);
            return name + ": " + AffixLines.Sentence(gem.Roll.Id, gem.Roll.Value, def);
        }

        private static void Next(StoneJob job, int index)
        {
            UnifiedPopup.Pop();
            AskAt(job, index + 1);
        }

        private static void Chosen(StoneJob asked, int socket)
        {
            UnifiedPopup.Pop();
            Set(asked, socket);
        }

        // Every check again (the inventory may have changed while the popup was open), aimed at the socket, then the commit.
        private static void Set(StoneJob asked, int socket)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }
            StoneJob job = asked.AtSocket(player, socket);
            StoneResult result = StonePipeline.Evaluate(job);
            if (result.Refused)
            {
                StoneFeedback.Show(player, result.Refusal!);
                return;
            }
            StoneCommit.Commit(job, result);
        }
    }
}
