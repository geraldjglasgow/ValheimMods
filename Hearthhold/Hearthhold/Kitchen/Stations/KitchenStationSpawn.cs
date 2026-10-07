using System.Collections.Generic;
using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// Stars for what a kitchen cooking station or oven puts into the world, on its ZDO owner. Taking a dish off sends
    /// RPC_RemoveDoneItem to the owner, which calls SpawnItem(name, slot, ...) once per dish (GrindstoneSkills' extra
    /// food raises that count) for the first done slot, then empties it; SpawnItem instantiates the item prefab, so
    /// ItemDrop.Awake runs inside it. A breaking station's OnDestroyed calls DropAllItems on the owner: fuel first, then
    /// per slot in order the done dish, the burnt item or the raw food, each instantiated, then the slot emptied. Each
    /// patch opens a <see cref="SpawnStars"/> scope with a queue of (prefab, stars) in the order the station creates its
    /// items, and every new star item takes the first entry with its prefab name, so identical dishes from several slots
    /// each get their own slot's stars. A done dish carries its rolled stars, raw food the stars it went on with;
    /// burnt items and fuel are not queued and stay plain.
    /// </summary>
    public static class KitchenStationSpawn
    {
        private struct Entry
        {
            public string Prefab;
            public int Stars;
        }

        private static readonly List<Entry> queue = new List<Entry>();
        private static readonly SpawnStars.Roller roller = Take;
        private static SpawnStars.Roller previous;
        private static bool open;

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.SpawnItem))]
        private static class Spawn
        {
            [HarmonyPrefix]
            private static void Prefix(CookingStation __instance, string name, int slot, out bool __state) =>
                __state = Kitchen.IsKitchen(__instance) && HookGuard.Run("station spawn", () => QueueDish(__instance, name, slot), false);

            [HarmonyFinalizer]
            private static void Finalizer(bool __state)
            {
                if (__state)
                    Close();
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.DropAllItems))]
        private static class Drop
        {
            [HarmonyPrefix]
            private static void Prefix(CookingStation __instance, out bool __state) =>
                __state = Kitchen.IsKitchen(__instance) && HookGuard.Run("station drops", () => QueueAll(__instance), false);

            [HarmonyFinalizer]
            private static void Finalizer(bool __state)
            {
                if (__state)
                    Close();
            }
        }

        /// <summary>The dish SpawnItem is about to create, when the slot is done (not burnt); true when a scope opened.</summary>
        private static bool QueueDish(CookingStation station, string name, int slot)
        {
            ZNetView nview = station.m_nview;
            if (nview == null || !nview.IsValid() || string.IsNullOrEmpty(name))
                return false;
            ZDO zdo = nview.GetZDO();
            if (KitchenSlots.Status(zdo, slot) != CookingStation.Status.Done)
                return false;
            Open();
            queue.Add(new Entry { Prefab = name, Stars = KitchenSlots.DishStars(zdo, slot) });
            return true;
        }

        /// <summary>Every slot's item DropAllItems is about to create, in its order; true when a scope opened.</summary>
        private static bool QueueAll(CookingStation station)
        {
            ZNetView nview = station.m_nview;
            if (nview == null || !nview.IsValid())
                return false;
            Open();
            ZDO zdo = nview.GetZDO();
            for (int slot = 0; slot < station.m_slots.Length; slot++)
            {
                string name = KitchenSlots.Name(zdo, slot);
                CookingStation.Status status = KitchenSlots.Status(zdo, slot);
                if (name.Length == 0 || status == CookingStation.Status.Burnt)
                    continue;
                int stars = status == CookingStation.Status.Done ? KitchenSlots.DishStars(zdo, slot) : KitchenSlots.RawStars(zdo, slot);
                queue.Add(new Entry { Prefab = name, Stars = stars });
            }
            return true;
        }

        private static void Open()
        {
            Close();
            previous = SpawnStars.Open(roller);
            open = true;
        }

        private static void Close()
        {
            if (!open)
                return;
            open = false;
            SpawnStars.Close(previous);
            previous = null;
            queue.Clear();
        }

        /// <summary>The roller: the stars queued first for this item's prefab, removed from the queue; 0 when none are.</summary>
        private static int Take(ItemDrop.ItemData item)
        {
            string prefab = item.m_dropPrefab != null ? item.m_dropPrefab.name : null;
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i].Prefab != prefab)
                    continue;
                int stars = queue[i].Stars;
                queue.RemoveAt(i);
                return stars;
            }
            return 0;
        }
    }
}
