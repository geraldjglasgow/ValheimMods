using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// A star roll's inputs, from the actor's client to the owner of the object that will spawn or hold the result. The
    /// game decides on the actor's client (a pick, putting food on a station, a mead base into a barrel, taking honey) and
    /// sends its own RPC to the object's owner, who spawns the items; skill levels live only on the actor's client. So
    /// just before the game's RPC the actor sends <see cref="Keys.RpcMark"/> on the same object's ZNetView with two
    /// numbers: the actor's skill level and the stars of what went in. Routed RPCs from one peer arrive in order, so the
    /// owner holds the mark when the game's RPC comes and the feature takes it there (<see cref="TryTake"/>). Marks live
    /// in the owner's memory per object and sender for 10 s. Without one (a client without Hearthhold, a lost mark) the
    /// result is plain or rolled at level 0.
    /// <para>The RPC is registered on an object's view when the first mark arrives for it, not on every view that wakes
    /// (thousands of pickables wake on every machine): a prefix on ZNetView.HandleRoutedRPC, which every routed RPC for an
    /// object passes on the machine it is addressed to, registers it before the game's own lookup.</para>
    /// </summary>
    public static class Marks
    {
        private const float Lifetime = 10f;
        private static readonly int RpcHash = Keys.RpcMark.GetStableHashCode();

        public struct Mark
        {
            public float Level;
            public float Stars;
            public float Time;
        }

        private static readonly Dictionary<(ZDOID, long), Mark> marks = new Dictionary<(ZDOID, long), Mark>();
        private static readonly List<(ZDOID, long)> stale = new List<(ZDOID, long)>();

        /// <summary>Sends a mark to the owner of <paramref name="nview"/>, from the actor's client.</summary>
        public static void Send(ZNetView nview, float level, float stars)
        {
            if (nview != null && nview.IsValid())
                nview.InvokeRPC(Keys.RpcMark, level, stars);
        }

        /// <summary>On the owner: the sender's latest mark on this object, removed; false when there is none or it is too old.</summary>
        public static bool TryTake(ZNetView nview, long sender, out Mark mark)
        {
            mark = default;
            ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
            if (zdo == null || !marks.TryGetValue((zdo.m_uid, sender), out mark))
                return false;
            marks.Remove((zdo.m_uid, sender));
            return Time.time - mark.Time <= Lifetime;
        }

        private static void Receive(ZNetView nview, long sender, float level, float stars)
        {
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
                return;
            Prune();
            Mark mark = new Mark { Level = StarOdds.Sane(level), Stars = Mathf.Clamp(StarOdds.Sane(stars), 0f, Stars.Max), Time = Time.time };
            marks[(nview.GetZDO().m_uid, sender)] = mark;
        }

        private static void Prune()
        {
            foreach (KeyValuePair<(ZDOID, long), Mark> pair in marks)
            {
                if (Time.time - pair.Value.Time > Lifetime)
                    stale.Add(pair.Key);
            }
            foreach ((ZDOID, long) key in stale)
                marks.Remove(key);
            stale.Clear();
        }

        [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.HandleRoutedRPC))]
        private static class Arrival
        {
            [HarmonyPrefix]
            private static void Prefix(ZNetView __instance, ZRoutedRpc.RoutedRPCData rpcData)
            {
                if (rpcData.m_methodHash != RpcHash || __instance.m_functions.ContainsKey(RpcHash))
                    return;
                ZNetView view = __instance;
                view.Register<float, float>(Keys.RpcMark, (sender, level, stars) => Receive(view, sender, level, stars));
            }
        }
    }
}
