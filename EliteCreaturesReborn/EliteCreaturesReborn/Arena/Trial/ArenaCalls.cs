using System.Collections.Generic;
using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Calling the next wave early (features/arena.md section 3, "Calling the next wave"; the user, 2026-10-13: "make it
    /// so pressing E on Hel will spawn in the next wave during this wave"). During a wave, once all its creatures have come
    /// out (none still queued), a player of the trial alive in the arena may press E at Hel: the next wave begins at once
    /// on top of the one fighting, and every player of the trial reads "Ana calls the next wave!". One press is enough. A
    /// wave can be called past once - the called wave is then the one fighting, and can be called past in its turn once
    /// its own creatures are out. A boss wave may be called early; a wave past a living boss may not ("Hel waits until
    /// Moder falls."). The alive cap holds: the called wave's creatures wait their turn. Each wave fighting is cleared by
    /// its own creatures, with no break between them; the break follows the last (<see cref="ArenaClears"/>). The client
    /// asks the core's owner by RPC, and the owner checks it all again.
    /// </summary>
    internal static class ArenaCalls
    {
        /// <summary>Any machine: true when the local player may call the next wave now, as this machine reads the core's
        /// ZDO (Hel's hover).</summary>
        public static bool CanCall(ZDO? zdo)
        {
            if (zdo == null || (ArenaPhase)zdo.GetInt(ArenaKeys.PhaseHash) != ArenaPhase.Fighting)
            {
                return false;
            }
            return HallPlayers.LocalInHall() && AllOut(new ArenaState(zdo));
        }

        /// <summary>Any machine: asks the core's owner to call the next wave, with the local player's name.</summary>
        public static void Ask(ArenaCore core)
        {
            string name = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerName() : "";
            if (core.IsOwner)
            {
                Call(core, ZNet.GetUID(), name); // this machine runs the trial: no round trip
            }
            else
            {
                core.View.InvokeRPC(ArenaKeys.CallRpc, name);
            }
        }

        /// <summary>The core's owner: a player called the next wave. Checked again here: a wave on, the caller alive in
        /// the arena, every creature of the wave out, no boss of the trial alive.</summary>
        public static void Call(ArenaCore core, long caller, string name)
        {
            ArenaState state = core.State;
            List<long> inHall = HallPlayers.PeersNow();
            if (state.Phase != ArenaPhase.Fighting || !inHall.Contains(caller) || !AllOut(state))
            {
                return;
            }
            string? boss = ArenaFighters.BossName(state.TrialId);
            if (boss != null)
            {
                ArenaNet.TellOne(caller, ArenaWords.BossWaits, boss);
                return;
            }
            ArenaNet.Tell(core.Zdo!, ArenaWords.Called, name);
            ArenaWaves.Call(core, inHall.Count, NetTime.NowMs());
        }

        // Every creature of the wave fighting has come out, and another wave has room.
        private static bool AllOut(ArenaState state) =>
            ArenaQueue.Read(state.Zdo).Of(state.Wave) == 0 && state.Wave - state.Swept < ArenaTable.MaxWavesAtOnce;
    }
}
