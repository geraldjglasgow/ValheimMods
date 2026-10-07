using HarmonyLib;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// The stars of the mead base in a fermenter (<see cref="KitchenKeys.BaseStars"/>, in the barrel's ZDO). The game's
    /// Fermenter.AddItem, on the adder's client, checks that the barrel is empty and the item allowed, removes one from
    /// the inventory and sends "RPC_AddItem"(name hash, cheated) to the owner, whose RPC_AddItem fills the barrel only
    /// when it is the owner and the barrel is empty. GrindstoneSkills swaps that one send, inside ZNetView.InvokeRPC, for
    /// its own add RPC carrying the cook's level, and its handler on the owner calls the game's RPC_AddItem with the same
    /// sender. The prefix here runs first of all prefixes and sends the mark (<see cref="Marks"/>: the adder's Cooking
    /// level and the base's stars) under its own RPC name, which GrindstoneSkills' swap leaves alone, before either add
    /// leaves. On the owner, RPC_AddItem's prefix takes the mark while the barrel is empty and the postfix writes the
    /// base's stars when the barrel now holds the item. The key is written only when it changes, so plain bases leave a
    /// barrel without data; tapping and dropping clear it (<see cref="KitchenBarrelTap"/>).
    /// </summary>
    public static class KitchenBarrel
    {
        private static readonly int StarsKey = KitchenKeys.BaseStars.GetStableHashCode();

        /// <summary>What the owner's RPC_AddItem prefix saw: whether the barrel was empty, and the base's stars.</summary>
        public struct Pending
        {
            public bool Empty;
            public int Stars;
        }

        public static int Get(ZDO zdo) => zdo == null ? 0 : Mathf.Clamp(zdo.GetInt(StarsKey), 0, Stars.Max);

        public static void Write(ZDO zdo, int stars)
        {
            stars = Mathf.Clamp(stars, 0, Stars.Max);
            if (zdo != null && zdo.GetInt(StarsKey) != stars)
                zdo.Set(StarsKey, stars);
        }

        public static void Clear(ZDO zdo) => Write(zdo, 0);

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.AddItem))]
        private static class Adding
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Fermenter __instance, Humanoid user, ItemDrop.ItemData item) =>
                HookGuard.Run("barrel mark", static args => SendMark(args.Item1, args.Item2, args.Item3), (__instance, user, item));
        }

        /// <summary>On the adder's client: the mark, when the game's AddItem is going to send the add.</summary>
        private static void SendMark(Fermenter fermenter, Humanoid user, ItemDrop.ItemData item)
        {
            ZNetView nview = fermenter.m_nview;
            if (item?.m_dropPrefab == null || user == null || user != Player.m_localPlayer || nview == null || !nview.IsValid())
                return;
            if (fermenter.GetStatus() != Fermenter.Status.Empty || !fermenter.IsItemAllowed(item))
                return;
            Marks.Send(nview, GrindstoneLink.LocalLevel(StarSources.Skill(StarSource.Dish)), Stars.Get(item));
        }

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.RPC_AddItem))]
        private static class Added
        {
            [HarmonyPrefix]
            private static void Prefix(Fermenter __instance, long sender, out Pending __state) =>
                __state = HookGuard.Run("barrel add", () => Before(__instance, sender), default(Pending));

            [HarmonyPostfix]
            private static void Postfix(Fermenter __instance, int nameHash, Pending __state)
            {
                if (__state.Empty)
                    HookGuard.Run("barrel add", static args => After(args.Item1, args.Item2, args.Item3), (__instance, nameHash, __state.Stars));
            }
        }

        private static Pending Before(Fermenter fermenter, long sender)
        {
            ZNetView nview = fermenter.m_nview;
            if (nview == null || !nview.IsValid() || !nview.IsOwner() || fermenter.GetContent() != 0)
                return default;
            int stars = Marks.TryTake(nview, sender, out Marks.Mark mark) ? Mathf.RoundToInt(mark.Stars) : 0;
            return new Pending { Empty = true, Stars = stars };
        }

        private static void After(Fermenter fermenter, int nameHash, int stars)
        {
            if (nameHash != 0 && fermenter.m_nview.IsValid() && fermenter.GetContent() == nameHash)
                Write(fermenter.m_nview.GetZDO(), stars);
        }
    }
}
