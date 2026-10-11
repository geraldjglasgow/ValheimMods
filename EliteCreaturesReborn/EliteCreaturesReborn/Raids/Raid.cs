namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// The raids' front door, shared by the Raiders Chest and the console: sound a raid at a host, stop it, or end it as
    /// robbed. A host is any networked object carrying a <see cref="RaidRunner"/>. Sounding and robbing run on the host's
    /// owner (the chest asks its owner first; the command makes its marker, so it owns it); stopping may be asked from
    /// any machine and goes to the owner. Everything else the raid does, it does on its own from the host's ZDO.
    /// </summary>
    public static class Raid
    {
        /// <summary>Why no raid may be sounded at this host now (not the owner, one already on, another within 200 m);
        /// null when one may. The chest asks before it takes the coins out.</summary>
        public static string? Check(ZNetView host) => RaidStart.Refusal(host);

        /// <summary>
        /// Sounds a raid at the host with <paramref name="coins"/> as the stake, the heat weighed against the strongest
        /// player within 96 m, or against <paramref name="tierOverride"/> (0-6) when it is 0 or more. Host's owner only.
        /// Returns null when the raid is on, else why not; on a refusal nothing was written.
        /// </summary>
        public static string? Start(ZNetView host, int coins, int tierOverride = -1) =>
            RaidStart.Start(host, coins, tierOverride);

        /// <summary>Stops the raid at the host: every raider dies where it stands and drops nothing. Any machine; a machine
        /// that does not own the host asks its owner.</summary>
        public static void Stop(ZNetView host)
        {
            RaidRunner? runner = Runner(host);
            if (runner == null)
            {
                return;
            }
            if (runner.IsOwner)
            {
                runner.End(RaidEnd.Stopped);
            }
            else
            {
                host.InvokeRPC(RaidKeys.StopAskRpc);
            }
        }

        /// <summary>Stops the raid whose host has this ZDO, wherever it is loaded: the stop goes to the host's owner, which
        /// may be this machine. Any machine.</summary>
        public static void Stop(ZDO host)
        {
            if (host != null && ZRoutedRpc.instance != null && host.HasOwner())
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(host.GetOwner(), host.m_uid, RaidKeys.StopAskRpc);
            }
        }

        /// <summary>The Raiders Chest is breaking: the raid ends robbed and its raiders walk off with their coins. Call it
        /// on the chest's owner as it is destroyed, before its ZDO goes.</summary>
        public static void Robbed(ZNetView host) => Runner(host)?.End(RaidEnd.Robbed);

        /// <summary>True while a raid runs at the host (countdown, a wave or a break). Any machine.</summary>
        public static bool IsRunning(ZNetView host) =>
            host != null && host.IsValid() && RaidState.IsRunning(host.GetZDO());

        private static RaidRunner? Runner(ZNetView? host)
        {
            RaidRunner? runner = host != null && host.IsValid() ? host.GetComponent<RaidRunner>() : null;
            return runner != null && runner.Zdo != null ? runner : null;
        }
    }
}
