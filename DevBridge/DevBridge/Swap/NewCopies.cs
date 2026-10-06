using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DevBridge.Swap
{
    /// <summary>
    /// Copies of swapped prefabs made since the last sweep, caught as the game makes them, so the once-a-second sweep
    /// reaches them without walking every loaded object: a networked copy registering with ZNetScene (AddInstance, for
    /// objects loaded and created alike), an item visual a VisEquipment attaches (AttachItem, AttachArmor) and an item
    /// stand's visual (SetVisualItem). The patches go on with the first swap and stay, under their own Harmony id; with
    /// nothing swapped each is one set lookup.
    /// </summary>
    internal static class NewCopies
    {
        private static readonly HashSet<int> Hashes = new HashSet<int>();
        private static readonly List<ZNetView> Instances = new List<ZNetView>();
        private static readonly List<(VisEquipment Wearer, GameObject Item, int Hash)> Worn = new List<(VisEquipment, GameObject, int)>();
        private static readonly List<ItemStand> Stands = new List<ItemStand>();
        private static Harmony harmony;

        /// <summary>The prefabs swapped now (by name hash): their copies are caught from here on.</summary>
        internal static void Watch(IEnumerable<int> hashes)
        {
            Hashes.Clear();
            foreach (int hash in hashes) Hashes.Add(hash);
            if (Hashes.Count > 0) Patch();
            else Clear();
        }

        /// <summary>The copies of the entry's prefab caught since the last <see cref="Clear"/> and still there, as swap targets.</summary>
        internal static IEnumerable<Target> Of(SwapEntry entry)
        {
            foreach (ZNetView view in Instances)
            {
                if (view && view.GetZDO() != null && view.GetZDO().GetPrefab() == entry.Hash) yield return Targets.Instance(entry, view);
            }
            foreach ((VisEquipment wearer, GameObject item, int hash) in Worn)
            {
                if (hash == entry.Hash && wearer && item) yield return Targets.WornItem(entry, wearer, item);
            }
            string prefix = Stands.Count > 0 ? Targets.StandPrefix(entry) : null;
            if (prefix == null) yield break;
            foreach (ItemStand stand in Stands)
            {
                if (stand && stand.m_visualHash == entry.Hash && stand.m_visualItem) yield return Targets.OnStand(entry, stand, prefix);
            }
        }

        /// <summary>After a sweep has reached them.</summary>
        internal static void Clear()
        {
            Instances.Clear();
            Worn.Clear();
            Stands.Clear();
        }

        private static void Patch()
        {
            if (harmony != null) return;
            harmony = new Harmony(DevBridgePlugin.PluginGuid + ".swap");
            harmony.Patch(AccessTools.Method(typeof(ZNetScene), nameof(ZNetScene.AddInstance)), postfix: Hook(nameof(Added)));
            harmony.Patch(AccessTools.Method(typeof(VisEquipment), nameof(VisEquipment.AttachItem)), postfix: Hook(nameof(Attached)));
            harmony.Patch(AccessTools.Method(typeof(VisEquipment), nameof(VisEquipment.AttachArmor)), postfix: Hook(nameof(ArmorAttached)));
            harmony.Patch(AccessTools.Method(typeof(ItemStand), nameof(ItemStand.SetVisualItem)), postfix: Hook(nameof(Shown)));
        }

        private static HarmonyMethod Hook(string name) => new HarmonyMethod(typeof(NewCopies), name);

        private static void Added(ZDO zdo, ZNetView nview)
        {
            if (Hashes.Count > 0 && zdo != null && Hashes.Contains(zdo.GetPrefab())) Instances.Add(nview);
        }

        private static void Attached(VisEquipment __instance, int itemHash, GameObject __result)
        {
            if (Hashes.Contains(itemHash) && __result) Worn.Add((__instance, __result, itemHash));
        }

        private static void ArmorAttached(VisEquipment __instance, int itemHash, List<GameObject> __result)
        {
            if (!Hashes.Contains(itemHash) || __result == null) return;
            foreach (GameObject item in __result) Worn.Add((__instance, item, itemHash));
        }

        private static void Shown(ItemStand __instance)
        {
            if (Hashes.Contains(__instance.m_visualHash) && __instance.m_visualItem) Stands.Add(__instance);
        }
    }
}
