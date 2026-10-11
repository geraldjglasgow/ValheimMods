using System.Globalization;
using EliteCreaturesReborn.Raids;
using UnityEngine;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// <c>elite raid start &lt;coins&gt; [tier]</c> and <c>elite raid stop</c>, admin only like the other elite
    /// subcommands. Start sounds a raid where the admin stands with that stake and no chest: it places the invisible test
    /// marker (<see cref="RaidMarker"/>), which this machine therefore owns, and sounds the raid on it exactly as a Raiders
    /// Chest would, the heat weighed against the tier given (0-6) or else the strongest player within 96 m. It skips the
    /// chest's own rules (coins in a chest, the cooldown, the base test) but not the 200 m between raids. Stop ends the
    /// nearest raid within 200 m of the admin as the chest's stop does: every raider dies and drops nothing.
    /// </summary>
    public static class RaidCommand
    {
        public static void Run(Terminal.ConsoleEventArgs args)
        {
            string verb = args.Length > 2 ? args[2].ToLowerInvariant() : "";
            Player player = Player.m_localPlayer;
            if (verb != "start" && verb != "stop")
            {
                EliteCommands.Reply(args, "usage: elite raid start <coins> [tier 0-6] | elite raid stop");
            }
            else if (player == null)
            {
                EliteCommands.Reply(args, "elite raid: needs your player in the world.");
            }
            else if (verb == "start")
            {
                Start(args, player.transform.position);
            }
            else
            {
                Stop(args, player.transform.position);
            }
        }

        private static void Start(Terminal.ConsoleEventArgs args, Vector3 at)
        {
            if (!TryArgs(args, out int coins, out int tier))
            {
                return;
            }
            ZNetView? marker = RaidMarker.Place(at);
            if (marker == null)
            {
                EliteCommands.Reply(args, "elite raid: the raid marker is not registered; is the world loaded?");
                return;
            }
            string? refusal = Raid.Start(marker, coins, tier);
            if (refusal != null)
            {
                ZNetScene.instance.Destroy(marker.gameObject);
                EliteCommands.Reply(args, $"elite raid: {refusal}");
                return;
            }
            RaidState state = new RaidState(marker.GetZDO());
            EliteCommands.Reply(args, $"elite raid: {state.DisplayName}, {coins} coins against gear tier {state.Tier}: heat "
                + $"{state.Heat.ToString("0.##", CultureInfo.InvariantCulture)}, {RaidTable.Band(state.Band).Name}. "
                + $"First wave in {RaidTable.CountdownSeconds:0} s.");
        }

        // <coins> a whole number above 0; [tier] 0-6, or -1 (the strongest player near) when left out.
        private static bool TryArgs(Terminal.ConsoleEventArgs args, out int coins, out int tier)
        {
            tier = -1;
            if (args.Length < 4 || !int.TryParse(args[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out coins)
                || coins <= 0)
            {
                coins = 0;
                EliteCommands.Reply(args, "usage: elite raid start <coins, more than 0> [tier 0-6]");
                return false;
            }
            if (args.Length > 4 && (!int.TryParse(args[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out tier)
                || tier < 0 || tier > RaidTable.MaxTier))
            {
                EliteCommands.Reply(args, $"elite raid: '{args[4]}' is not a gear tier (0 to {RaidTable.MaxTier}).");
                return false;
            }
            return true;
        }

        // Found by its ZDO, so a raid this machine has not loaded is found too; the stop goes to whoever runs it.
        private static void Stop(Terminal.ConsoleEventArgs args, Vector3 at)
        {
            ZDO? host = RaidZdos.RunningNear(at, RaidTable.RaidSpacing, ZDOID.None);
            if (host == null)
            {
                EliteCommands.Reply(args, $"elite raid: no raid is on within {RaidTable.RaidSpacing:0} m of you.");
                return;
            }
            string name = new RaidState(host).DisplayName;
            float metres = Vector3.Distance(host.GetPosition(), at);
            Raid.Stop(host);
            EliteCommands.Reply(args, $"elite raid: stopping {name}, {metres:0} m away: its raiders fall, dropping nothing.");
        }
    }
}
