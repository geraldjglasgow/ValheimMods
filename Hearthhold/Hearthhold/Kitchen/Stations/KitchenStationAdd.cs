using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// Putting raw food on a kitchen cooking station or the oven. Every way of doing it (Interact, using an item on the
    /// station, the oven's add-food switch) ends in CookingStation.CookItem on the cook's client: it claims an unowned
    /// station, refuses incompatible and unknown items and a full station, removes one item from the inventory and sends
    /// the add to the station's owner, whose RPC_AddItem puts it into the first free slot (GetFreeSlot) with SetSlot.
    /// GrindstoneSkills replaces CookItem at kitchens with its own copy that sends its own add RPC, whose handler on the
    /// owner calls the game's RPC_AddItem with the same sender. So the prefix here runs first of all prefixes, makes the
    /// game's own checks and claim, and sends the mark (<see cref="Marks"/>: the cook's level, the food's stars) before
    /// either mod's add leaves; routed RPCs from one peer arrive in order. On the owner, RPC_AddItem's prefix takes the
    /// mark and notes the free slot; the postfix records the cook in the slot when the game filled it
    /// (<see cref="KitchenSlots"/>). An add without a mark records level 0 and plain food.
    /// </summary>
    public static class KitchenStationAdd
    {
        /// <summary>What the owner's RPC_AddItem prefix saw: the slot it will fill (-1 for none) and the mark.</summary>
        public struct Pending
        {
            public int Slot;
            public float Level;
            public float Stars;
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.CookItem))]
        private static class Cook
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(CookingStation __instance, Humanoid user, ItemDrop.ItemData item)
            {
                if (Kitchen.IsKitchen(__instance))
                    HookGuard.Run("station mark", static args => SendMark(args.Item1, args.Item2, args.Item3), (__instance, user, item));
            }
        }

        /// <summary>On the cook's client: the mark, when the game's CookItem is going to send the add.</summary>
        private static void SendMark(CookingStation station, Humanoid user, ItemDrop.ItemData item)
        {
            ZNetView nview = station.m_nview;
            if (item?.m_dropPrefab == null || user == null || user != Player.m_localPlayer || nview == null || !nview.IsValid())
                return;
            if (!nview.HasOwner())
                nview.ClaimOwnership();
            if (Incompatible(station, item) || !station.IsItemAllowed(item) || station.GetFreeSlot() == -1)
                return;
            Marks.Send(nview, GrindstoneLink.LocalLevel(StarSources.Skill(StarSource.Dish)), Stars.Get(item));
        }

        /// <summary>The game's test for an item this station refuses with a message (and so never adds).</summary>
        private static bool Incompatible(CookingStation station, ItemDrop.ItemData item)
        {
            foreach (CookingStation.ItemMessage incompatible in station.m_incompatibleItems)
            {
                if (incompatible.m_item != null && incompatible.m_item.m_itemData.m_shared.m_name == item.m_shared.m_name)
                    return true;
            }
            return false;
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.RPC_AddItem))]
        private static class Add
        {
            [HarmonyPrefix]
            private static void Prefix(CookingStation __instance, long sender, out Pending __state)
            {
                Pending none = new Pending { Slot = -1 };
                __state = Kitchen.IsKitchen(__instance) ? HookGuard.Run("station add", () => Before(__instance, sender), none) : none;
            }

            [HarmonyPostfix]
            private static void Postfix(CookingStation __instance, string itemName, Pending __state)
            {
                if (__state.Slot >= 0)
                    HookGuard.Run("station add", static args => After(args.Item1, args.Item2, args.Item3), (__instance, itemName, __state));
            }
        }

        private static Pending Before(CookingStation station, long sender)
        {
            ZNetView nview = station.m_nview;
            if (nview == null || !nview.IsValid())
                return new Pending { Slot = -1 };
            if (!Marks.TryTake(nview, sender, out Marks.Mark mark))
                mark = default;
            return new Pending { Slot = station.GetFreeSlot(), Level = mark.Level, Stars = mark.Stars };
        }

        /// <summary>Records the cook when the free slot now holds the added item.</summary>
        private static void After(CookingStation station, string itemName, Pending pending)
        {
            ZNetView nview = station.m_nview;
            if (!nview.IsValid() || string.IsNullOrEmpty(itemName))
                return;
            ZDO zdo = nview.GetZDO();
            if (KitchenSlots.Name(zdo, pending.Slot) == itemName)
                KitchenSlots.Fill(zdo, pending.Slot, pending.Level, pending.Stars);
        }
    }
}
