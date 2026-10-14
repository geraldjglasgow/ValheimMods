using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The server's watch over a trial nobody is at (features/arena.md section 4). The arena lies far out where only those
    /// who jump there load it, so the core is owned by a player there, never by the server; when the last of them leaves -
    /// out through the way out or `elite arena`, or logged out - the core is left with no owner and its trial with
    /// nobody to end it. Every few seconds (<see cref="ArenaCoreKeeper"/>) the server, which holds every ZDO, asks
    /// whether any machine has the arena in its area (its own player's, on a hosting player's game, and every peer's);
    /// when none has for ten seconds while a trial runs, it takes the core and ends the trial as an empty arena does -
    /// the end told and the reward paid to every player of it - and removes the trial's creatures (no death, no drops).
    /// A player dying in the arena does not count as leaving: their machine stays there until they respawn, and the
    /// core's owner ends the trial itself.
    /// </summary>
    internal static class ArenaOrphanWatch
    {
        /// <summary>Seconds with no machine at the arena before the server ends its trial: well past the game's two
        /// seconds for handing the core to another player who is still there.</summary>
        private const float GraceSeconds = 10f;

        /// <summary>Zones round the core searched for the trial's creatures: the arena spans three by three.</summary>
        private const int ArenaZones = 1;

        private static float _emptySince = -1f;

        /// <summary>The keeper's check, on the server, of the world's core.</summary>
        public static void Check(ZDO core, float now)
        {
            if (!ArenaState.IsRunning(core) || AnyoneThere(core.GetPosition()))
            {
                _emptySince = -1f;
                return;
            }
            if (_emptySince < 0f)
            {
                _emptySince = now;
            }
            else if (now - _emptySince >= GraceSeconds)
            {
                _emptySince = -1f;
                Close(core);
            }
        }

        private static void Close(ZDO core)
        {
            long trial = new ArenaState(core).TrialId;
            core.SetOwner(ZDOMan.GetSessionID()); // nobody holds it: the server writes the end
            ArenaEnd.Close(core, stopped: false);
            int gone = ArenaZdos.Remove(core.GetPosition(), ArenaZones, zdo => ArenaTag.Trial(zdo) == trial);
            Log.Info($"Hel's Trial: nobody is at the arena; the server ended the trial and removed {gone} creature(s)");
        }

        // A machine with the arena in its area would own the core, or soon will: the hosting player's game itself, and
        // every connected peer.
        private static bool AnyoneThere(Vector3 at)
        {
            ZNet net = ZNet.instance;
            if (!net.IsDedicated() && ZNetScene.InActiveArea(at, net.GetReferencePosition()))
            {
                return true;
            }
            foreach (ZNetPeer peer in net.GetPeers())
            {
                if (peer.IsReady() && ZNetScene.InActiveArea(at, peer.GetRefPos()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
