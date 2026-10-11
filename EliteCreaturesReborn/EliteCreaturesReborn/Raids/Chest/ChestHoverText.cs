using System.Text;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// Builds the Raiders Chest's hover (features/raids.md section 2), not yet localized, for <see cref="ChestHover"/>:
    /// <code>
    /// Raiders Chest
    /// 1,000 coins - Deadly raid (coins back x2, drops x5)
    /// [E] Open   [Shift + E] Sound the raid
    /// </code>
    /// The difficulty is worked out for the players near the chest now, on this machine (<see cref="RaidHeat.Measure"/>),
    /// so a group sees what it would get before it starts. When the chest cannot sound a raid the line under the coins says
    /// why (empty, not in a base, the horn resting, another raid near) and Shift + E is left out. During a raid it shows
    /// the raid as the HUD line does, and how to stop it, with the hold's progress. With the setting off it reads as a
    /// coin chest. Behind a ward the player is not on, the game's "no access".
    /// </summary>
    internal static class ChestHoverText
    {
        private static readonly StringBuilder Text = new StringBuilder(256);

        public static string Build(RaidChest chest, int holdStep)
        {
            Text.Clear().Append(ChestPrefab.ContainerName);
            if (chest.Container.m_checkGuardStone && !PrivateArea.CheckAccess(chest.transform.position, 0f, flash: false))
            {
                Text.Append("\n$piece_noaccess");
                return Text.ToString();
            }
            RaidState state = new RaidState(chest.View.GetZDO());
            if (state.Running)
            {
                Raiding(state, holdStep);
            }
            else
            {
                Idle(chest);
            }
            return Text.ToString();
        }

        private static void Raiding(RaidState state, int holdStep)
        {
            long now = NetTime.NowMs();
            string line = RaidText.Line(state.DisplayName, RaidTable.Band(state.Band), state.Phase, state.Wave, state.Left,
                Seconds(state.Deadline - now), Seconds(state.PhaseUntil - now));
            Text.Append('\n').Append(line);
            Text.Append('\n').Append(holdStep >= 0 ? ChestText.Stopping(holdStep) : ChestText.HoldToStop);
        }

        private static void Idle(RaidChest chest)
        {
            int coins = ChestCoins.Count(chest.Container.GetInventory());
            if (!RaidSettings.ChestEnabled)
            {
                Text.Append('\n').Append(coins > 0 ? ChestText.Coins(coins) : "( $piece_container_empty )");
                Text.Append('\n').Append(ChestText.Keys(false, false));
                return;
            }
            string? refusal = ChestRules.Refusal(chest);
            if (coins > 0)
            {
                Text.Append('\n').Append(ChestText.Stake(coins, RaidHeat.Measure(chest.transform.position, coins)));
            }
            refusal ??= coins > 0 ? null : ChestText.Empty;
            if (refusal != null)
            {
                Text.Append('\n').Append(refusal);
            }
            Text.Append('\n').Append(ChestText.Keys(refusal == null, GamepadKeys()));
        }

        // The pair the game reads as "alt" with E: Shift, or on a gamepad in the newer layout its own alt key.
        private static bool GamepadKeys() => ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive();

        private static int Seconds(long ms) => ms <= 0L ? 0 : (int)((ms + 999L) / 1000L);
    }
}
