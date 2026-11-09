using System;
using HarmonyLib;
using PatchGuard;

namespace Broadside
{
    /// <summary>
    /// Broadside's one patch on <c>ItemDrop.Awake</c> (one patch per hot method): a dropped item that wakes with one of
    /// Broadside's keys in its ZDO gets what it needs, on every peer: a trawl's catch riding a ship's deck
    /// (<see cref="KwTrawlDeck"/>, by <see cref="KwTrawlDeck.ShipHash"/>; dead, <see cref="KwTrawlDead"/>, by <see cref="KwTrawlDead.DeadHash"/>) and a broken crate's spilled stack drawn as a
    /// heap (<see cref="KwCrateHeap"/>, by <see cref="KwCrateHeap.HeapHash"/>). An item without a ZDO (a ghost, a prefab
    /// copy) or without the keys is left alone. A throw is reported, never passed into the game's wake.
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop), "Awake")]
    public static class KwItemDropWake
    {
        [HarmonyPostfix]
        private static void Postfix(ItemDrop __instance)
        {
            ZDO? zdo = __instance.m_nview != null ? __instance.m_nview.GetZDO() : null;
            if (zdo == null)
                return;
            if (zdo.GetLong(KwTrawlDeck.ShipHash, 0L) != 0L || zdo.GetBool(KwTrawlDead.DeadHash))
                Catch(__instance, zdo);
            if (zdo.GetBool(KwCrateHeap.HeapHash))
                Heap(__instance);
        }

        private static void Catch(ItemDrop item, ZDO zdo)
        {
            try
            {
                // both keys only ever come from the trawl: a catch landed before it was marked dead is dead too
                KwTrawlDead.Make(item.gameObject);
                if (zdo.GetLong(KwTrawlDeck.ShipHash, 0L) != 0L && item.GetComponent<KwTrawlDeck>() == null)
                    item.gameObject.AddComponent<KwTrawlDeck>();
            }
            catch (Exception e)
            {
                Guard.Report(e, "Broadside catch aboard");
            }
        }

        private static void Heap(ItemDrop item)
        {
            try
            {
                KwCrateHeap.Add(item);
            }
            catch (Exception e)
            {
                Guard.Report(e, "Broadside crate heap");
            }
        }
    }
}
