using System;
using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The game's cart in the scene, on every peer: as it wakes it gets its bed (<see cref="KwCartBed"/>, only the game's own
    /// <see cref="KwCargoCart.Prefab"/>); its owner's mass, every 5 s as the game sets it, counts the crates on it too
    /// (<c>Vagon.UpdateMass</c>: its base mass and, at the game's factor, what its own storage and its crates hold, so a
    /// loaded cart pulls as heavily as it did when the goods were in its storage); and its crates are let go before the game
    /// destroys its object, so they are never destroyed with it as its children (the game would then keep them as objects it
    /// has and never make them again). The game lets go of an object three ways, each before Unity destroys it at the frame's
    /// end: <c>ZNetScene.Destroy</c> (removed or broken: its ZDO destroyed when this game owns it), <c>ZNetScene.OnZDODestroyed</c>
    /// (destroyed by another game) and <c>ZNetScene.RemoveObjects</c> (out of range: unloaded, its ZDO kept). The crates stay
    /// where they are (parked) and board again when the cart is loaded; when it was destroyed, each crate's owner makes it a
    /// free crate, which comes down where it is (<see cref="KwCartRider.Release"/>).
    /// </summary>
    [HarmonyPatch]
    public static class KwCartScene
    {
        [HarmonyPatch(typeof(Vagon), "Awake"), HarmonyPostfix]
        private static void Woke(Vagon __instance)
        {
            ZDO? zdo = __instance.m_nview != null ? __instance.m_nview.GetZDO() : null;
            if (zdo == null || zdo.GetPrefab() != KwCargoCart.PrefabHash || __instance.GetComponent<KwCartBed>() != null)
                return;
            try
            {
                __instance.gameObject.AddComponent<KwCartBed>();
            }
            catch (Exception e)
            {
                Guard.Report(e, "Broadside cart bed");
            }
        }

        [HarmonyPatch(typeof(Vagon), "UpdateMass"), HarmonyPostfix]
        private static void Mass(Vagon __instance)
        {
            if (__instance.m_container == null || !__instance.m_nview.IsOwner() || !__instance.TryGetComponent(out KwCartBed bed) || bed.Riders.Count == 0)
                return;
            float weight = __instance.m_container.GetInventory().GetTotalWeight() + bed.Weight();
            __instance.SetMass(__instance.m_baseMass + weight * __instance.m_itemWeightMassFactor);
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Destroy), new[] { typeof(GameObject) }), HarmonyPrefix]
        private static void Destroying(GameObject go)
        {
            if (go == null || KwCartBed.Carried == 0 || !go.TryGetComponent(out KwCartBed bed))
                return;
            ZDO? zdo = bed.Zdo;
            Run(bed, zdo != null && zdo.IsOwner());
        }

        [HarmonyPatch(typeof(ZNetScene), "OnZDODestroyed"), HarmonyPrefix]
        private static void Gone(ZNetScene __instance, ZDO zdo)
        {
            if (zdo == null || zdo.GetPrefab() != KwCargoCart.PrefabHash)
                return;
            KwCartBed.Destroyed(zdo.GetLong(KwCargoCart.IdHash, 0L));
            if (__instance.m_instances.TryGetValue(zdo, out ZNetView view) && view != null && view.TryGetComponent(out KwCartBed bed))
                Run(bed, true);
        }

        // After the game let go of the carts out of range (their ZDOs reset, their objects destroyed at the frame's end).
        [HarmonyPatch(typeof(ZNetScene), "RemoveObjects"), HarmonyPostfix]
        private static void Unloaded()
        {
            if (KwCartBed.Carried == 0)
                return;
            IReadOnlyList<KwCartBed> beds = KwCartBed.Loaded;
            for (int i = beds.Count - 1; i >= 0; i--)
            {
                KwCartBed bed = beds[i];
                if (bed != null && bed.Riders.Count > 0 && bed.Zdo == null)
                    Run(bed, false);
            }
        }

        private static void Run(KwCartBed bed, bool destroyed)
        {
            try
            {
                bed.LetGo(destroyed);
            }
            catch (Exception e)
            {
                Guard.Report(e, "Broadside cart crates let go");
            }
        }
    }
}
