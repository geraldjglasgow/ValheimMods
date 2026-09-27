using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Tells a rock's owner that a miner's next hit on a chunk is a clean strike, so the break of that chunk can give the
    /// clean strike's extra roll there.
    /// <list type="bullet">
    /// <item><see cref="Send"/> runs on the miner's client, before the clean strike's hit is sent: an RPC on the rock's
    /// ZNetView to its owner (<see cref="Keys.RpcCleanStrike"/>: int area, ZDOID the miner's player). Both RPCs go the
    /// same route from the same sender, so the mark arrives first; when the miner owns the rock it is handled at once.</item>
    /// <item>The owner keeps each mark in memory for <see cref="Lifetime"/> seconds (a clean strike may need more hits
    /// to break its chunk). Marks are lost if the rock changes owner in between.</item>
    /// <item><see cref="Take"/> runs on the owner when a chunk breaks: true once per mark of that miner on that chunk.</item>
    /// </list>
    /// The RPC is registered on every MineRock5 in a postfix on MineRock5.Awake, where the game registers its own
    /// RPC_Damage (only for an instance with a ZDO).
    /// </summary>
    public static class CleanStrikeMarks
    {
        /// <summary>Seconds the owner keeps a mark.</summary>
        public const float Lifetime = 30f;

        private const int MaxMarks = 512;

        private static readonly List<Mark> Marks = new List<Mark>();

        [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Awake))]
        private static class Register
        {
            [HarmonyPostfix]
            private static void Postfix(MineRock5 __instance)
            {
                ZNetView view = __instance.m_nview;
                if (view != null && view.GetZDO() != null)
                    view.Register<int, ZDOID>(Keys.RpcCleanStrike, (sender, area, miner) => Receive(__instance, area, miner));
            }
        }

        /// <summary>On the miner's client: marks the chunk <paramref name="area"/> of <paramref name="rock"/> for the local player.</summary>
        public static void Send(Rock rock, int area)
        {
            Player player = Player.m_localPlayer;
            if (player != null && rock != null && rock.IsValid && area >= 0)
                rock.View.InvokeRPC(Keys.RpcCleanStrike, area, player.GetZDOID());
        }

        /// <summary>On the rock's owner: takes the mark of this miner on this chunk; true when there was one.</summary>
        public static bool Take(ZDOID rock, int area, ZDOID miner)
        {
            Prune();
            int index = Marks.FindIndex(mark => mark.Rock == rock && mark.Area == area && mark.Miner == miner);
            if (index < 0)
                return false;
            Marks.RemoveAt(index);
            return true;
        }

        /// <summary>On the rock's owner: takes the breaking miner's mark on the broken chunk; false for a single piece.</summary>
        public static bool Take(RockBreak broken) =>
            broken != null && broken.Miner != null && broken.Chunk >= 0 && Take(broken.RockId, broken.Chunk, broken.Miner.Attacker);

        private static void Receive(MineRock5 chunks, int area, ZDOID miner) =>
            HookGuard.Run("clean strike mark", () => Keep(chunks, area, miner));

        private static void Keep(MineRock5 chunks, int area, ZDOID miner)
        {
            ZNetView view = chunks != null ? chunks.m_nview : null;
            if (!PickSkill.Active || view == null || !view.IsValid() || !view.IsOwner() || miner.IsNone())
                return;
            Prune();
            if (Marks.Count >= MaxMarks)
                Marks.RemoveAt(0);
            Marks.Add(new Mark(view.GetZDO().m_uid, area, miner, Time.time));
        }

        private static void Prune()
        {
            float oldest = Time.time - Lifetime;
            Marks.RemoveAll(mark => mark.Made < oldest);
        }

        private readonly struct Mark
        {
            public Mark(ZDOID rock, int area, ZDOID miner, float time)
            {
                Rock = rock;
                Area = area;
                Miner = miner;
                Made = time;
            }

            public ZDOID Rock { get; }
            public int Area { get; }
            public ZDOID Miner { get; }
            public float Made { get; }
        }
    }
}
