using System.Collections.Generic;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.History
{
    /// <summary>
    /// Remembers when this machine last sent an edit that is applied somewhere else, later: to a compiler another
    /// machine owns, or a privileged edit, which travels through the server even when this machine owns the compiler.
    /// Until such an edit has been applied and the saved data came back, the local copy of that compiler is out of date:
    /// a restore then sends every recorded value instead of only those that differ from the local copy, and the recorded
    /// values are not pruned yet. A normal edit to a compiler this machine owns is applied at once (the game handles a
    /// routed call to itself immediately), so it never unsettles anything.
    /// </summary>
    internal static class SendTracker
    {
        /// <summary>Generous time for an edit to reach its owner and the saved data to come back.</summary>
        private const float SettleSeconds = 3f;

        private static readonly Dictionary<Vector2Int, float> lastRemote = new Dictionary<Vector2Int, float>();

        public static void Note(TerrainComp comp, TerrainEdit edit)
        {
            if (comp == null || comp.m_nview == null || !comp.m_nview.IsValid())
                return;
            bool relayed = edit.Has(EditFlags.Privileged) && !Side.IsServer;
            if (comp.m_nview.IsOwner() && !relayed)
                return;
            lastRemote[RawAccess.KeyOf(comp.transform.position)] = Time.time;
        }

        /// <summary>The local copy of the compiler at this key can be trusted: no edit of ours is still on its way.</summary>
        public static bool Settled(Vector2Int key)
        {
            return !lastRemote.TryGetValue(key, out float sent) || Time.time - sent > SettleSeconds;
        }

        public static void Clear() => lastRemote.Clear();
    }
}
