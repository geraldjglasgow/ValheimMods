using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// Sounding a raid at a host, on the host's owner, the same for the Raiders Chest and for `elite raid start`: the
    /// checks (a live host this machine owns, no raid on it already, none within 200 m), the raid drawn from those the
    /// world has unlocked (<see cref="RaidPick"/>), the heat fixed from the strongest player at the raid (or
    /// the command's tier), the state written into the host's ZDO, and the start told to the players within 100 m with
    /// the horn. The coins themselves are the caller's: the chest takes them out of itself, the command names them.
    /// </summary>
    internal static class RaidStart
    {
        /// <summary>Why no raid may be sounded at this host now; null when one may.</summary>
        public static string? Refusal(ZNetView? host)
        {
            RaidRunner? runner = host != null && host.IsValid() ? host.GetComponent<RaidRunner>() : null;
            if (runner == null || runner.Zdo == null)
            {
                return "This cannot hold a raid.";
            }
            if (!runner.IsOwner)
            {
                return "Only the machine that owns it can sound a raid here.";
            }
            if (runner.State.Running)
            {
                return "A raid is already on here.";
            }
            return RaidSpacing.Refusal(runner.Position, runner.HostId);
        }

        /// <summary>Sounds the raid; null on success, else why not (and nothing was written).</summary>
        public static string? Start(ZNetView host, int coins, int tierOverride)
        {
            string? refusal = Refusal(host);
            if (refusal != null)
            {
                return refusal;
            }
            if (coins <= 0)
            {
                return "No gold, no raid.";
            }
            RaidRunner runner = host.GetComponent<RaidRunner>();
            RaidChoice? choice = RaidPick.Draw(runner.Position);
            if (choice == null)
            {
                return "No raid has been unlocked in this world yet.";
            }
            Begin(runner, choice, coins, tierOverride);
            return null;
        }

        private static void Begin(RaidRunner runner, RaidChoice choice, int coins, int tierOverride)
        {
            RaidPresence.Count near = RaidPresence.Of(runner);
            runner.Remember(near);
            RaidHeat heat = RaidHeat.For(coins, tierOverride >= 0 ? tierOverride : near.StrongestTier);
            RaidState state = runner.State;
            state.Begin(heat, choice, coins, NetTime.NowMs());
            RaidPresence.Claim(state, near);
            RaidNet.Tell(runner, RaidText.Started(choice.DisplayName, heat.Band));
            RaidHorn.Sound(runner);
            Log.Info($"raid at {runner.Position:F0}: {choice.DisplayName} ({choice.EventName}), {coins} coins against "
                + $"tier {heat.Tier}, heat {heat.Heat:0.##} - {heat.Band.Name}");
        }
    }
}
