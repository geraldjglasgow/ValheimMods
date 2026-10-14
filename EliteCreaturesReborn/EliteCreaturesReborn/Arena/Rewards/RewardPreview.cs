using EliteCreaturesReborn.Arena.Trial;

namespace EliteCreaturesReborn.Arena.Rewards
{
    /// <summary>
    /// "Current rewards: Bronze Battle Idol, 120 coins, 6 runes, 20 essence, +30% skill XP" on Hel's hover while a trial
    /// runs (a player's question, 2026-10-14: how do I know what my reward is?): what the trial would pay this player if it
    /// ended now, worked out from the core's ZDO as the end does (<see cref="RewardPlan"/>), on this client only. Rebuilt
    /// only when the trial, its waves cleared, its bosses beaten or this player's souls change. The runes are drawn only
    /// at the end, so only their count shows, as in the end message.
    /// </summary>
    internal static class RewardPreview
    {
        private static long _trial = -1L;
        private static int _swept = -1;
        private static long _beaten = -1L;
        private static long _souls = -1L;
        private static string _text = "";

        /// <summary>The line for the running trial, in the player's language; "" when none runs here.</summary>
        public static string Line()
        {
            ArenaCore? core = ArenaCore.Current;
            ZDO? zdo = core != null ? core.Zdo : null;
            if (zdo == null || !ArenaState.IsRunning(zdo) || Localization.instance == null)
            {
                return "";
            }
            ArenaState state = new ArenaState(zdo);
            long souls = SoulWallets.Souls(zdo, ZDOMan.GetSessionID());
            if (state.TrialId != _trial || state.Swept != _swept || state.BossesBeaten != _beaten || souls != _souls)
            {
                _trial = state.TrialId;
                _swept = state.Swept;
                _beaten = state.BossesBeaten;
                _souls = souls;
                _text = Build(state, zdo);
            }
            return _text;
        }

        private static string Build(ArenaState state, ZDO zdo)
        {
            Localization words = Localization.instance;
            RewardList list = RewardPlan.CoinsFor(RewardPlan.For(state), zdo, ZDOMan.GetSessionID());
            string items = RewardText.Items(list);
            string shown = items.Length > 0 ? items : words.Localize("$" + ArenaWords.RewardNone);
            return words.Localize("$" + ArenaWords.RewardNow, shown);
        }
    }
}
