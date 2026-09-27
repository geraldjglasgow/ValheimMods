using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A pick's star level, from the picker's client to the plant's owner. The game picks on the picker's client and
    /// sends RPC_Pick to the owner, who spawns the items; the picker's Foraging level lives only on the picker's client.
    /// So just before the game's RPC the picker sends <see cref="Keys.RpcForageMark"/> on the plant's ZNetView to its
    /// owner, with the effective level: the Foraging level, plus Best Time Levels when the plant is at its best where
    /// the picker stands. Routed RPCs from one peer arrive in order, so the owner has the mark when RPC_Pick comes
    /// (<see cref="ForageSpawn"/> takes it). Marks live in the owner's memory, per plant and sender, for 10 s.
    /// </summary>
    public static class ForageMarks
    {
        private const float Lifetime = 10f;

        private struct Mark
        {
            public float Level;
            public float Time;
        }

        private static readonly Dictionary<(ZDOID, long), Mark> marks = new Dictionary<(ZDOID, long), Mark>();

        public static void Send(Pickable pickable, Player player, ForageEntry entry)
        {
            float level = ForagingSkill.Level(player);
            if (BestTimes.IsNow(entry.Best))
                level += Mathf.Max(0f, ForagePerkSettings.BestTimeLevels.Value);
            pickable.m_nview.InvokeRPC(Keys.RpcForageMark, level);
        }

        /// <summary>The level the sender's latest mark on this plant carries, removed; false when there is none.</summary>
        public static bool TryTake(Pickable pickable, long sender, out float level)
        {
            level = 0f;
            ZDO zdo = pickable.m_nview.GetZDO();
            if (zdo == null || !marks.TryGetValue((zdo.m_uid, sender), out Mark mark))
                return false;
            marks.Remove((zdo.m_uid, sender));
            level = mark.Level;
            return Time.time - mark.Time <= Lifetime;
        }

        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Awake))]
        private static class RegisterMark
        {
            [HarmonyPostfix]
            private static void Postfix(Pickable __instance)
            {
                ZNetView nview = __instance.m_nview;
                if (nview != null && nview.GetZDO() != null)
                    nview.Register<float>(Keys.RpcForageMark, (sender, level) => Receive(__instance, sender, level));
            }
        }

        private static void Receive(Pickable pickable, long sender, float level)
        {
            if (pickable == null || !pickable.m_nview.IsValid() || !pickable.m_nview.IsOwner())
                return;
            Prune();
            marks[(pickable.m_nview.GetZDO().m_uid, sender)] = new Mark { Level = level, Time = Time.time };
        }

        private static void Prune()
        {
            List<(ZDOID, long)> stale = null;
            foreach (KeyValuePair<(ZDOID, long), Mark> pair in marks)
            {
                if (Time.time - pair.Value.Time > Lifetime)
                    (stale ??= new List<(ZDOID, long)>()).Add(pair.Key);
            }
            if (stale != null)
                stale.ForEach(key => marks.Remove(key));
        }
    }
}
