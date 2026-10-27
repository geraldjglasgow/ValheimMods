using System;
using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A piece whose ZDO says it rides a launched ship (<see cref="KwShipRider.ShipKey"/>) rides it from the moment it
    /// wakes, on every peer (loaded from the world, sent from another game, or loaded again after a restart): the game's
    /// <c>Piece.Awake</c>, after its ZNetView has its ZDO, gets the rider added.
    /// </summary>
    [HarmonyPatch(typeof(Piece), "Awake")]
    public static class KwShipRiderWake
    {
        [HarmonyPostfix]
        private static void Postfix(Piece __instance)
        {
            ZNetView? view = __instance.m_nview;
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (zdo == null || zdo.GetLong(KwShipRider.ShipHash, 0L) == 0L || __instance.GetComponent<KwShipRider>() != null)
                return;
            try
            {
                __instance.gameObject.AddComponent<KwShipRider>();
            }
            catch (Exception e)
            {
                Guard.Report(e, "Broadside ship rider");
            }
        }
    }

    /// <summary>
    /// A launched ship's riders let go before the game destroys the ship's object, so they are never destroyed with it as
    /// its children (the game would then keep them as objects it has, and never make them again). The game lets go of an
    /// object three ways, each before Unity destroys it at the frame's end: <c>ZNetScene.Destroy</c> (removed, broken up,
    /// sunk, cleared: its ZDO destroyed when this game owns it), <c>ZNetScene.OnZDODestroyed</c> (destroyed by another
    /// game) and <c>ZNetScene.RemoveObjects</c> (out of range: unloaded, its ZDO kept). The riders stay where they are
    /// (parked) and come aboard again when the ship is loaded; when it was destroyed, each rider's owner makes it an ordinary
    /// piece (<see cref="KwShipRider.Release"/>). Riders sunk or broken up with the ship are taken first
    /// (<see cref="KwShipRiderBreak"/>).
    /// </summary>
    [HarmonyPatch]
    public static class KwShipRiderScene
    {
        private static readonly int ShipPrefab = KwShipPrefab.Name.GetStableHashCode();

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Destroy), new[] { typeof(GameObject) }), HarmonyPrefix]
        private static void Destroying(GameObject go)
        {
            if (go == null || KwShipRiders.Loaded.Count == 0 || !go.TryGetComponent(out KwShipRiders ship))
                return;
            ZDO? zdo = ship.Zdo;
            Run(ship, zdo != null && zdo.IsOwner());
        }

        [HarmonyPatch(typeof(ZNetScene), "OnZDODestroyed"), HarmonyPrefix]
        private static void Gone(ZNetScene __instance, ZDO zdo)
        {
            if (zdo == null || zdo.GetPrefab() != ShipPrefab)
                return;
            KwShipRiders.Destroyed(zdo.GetLong(KwShipRiders.IdHash, 0L));
            if (__instance.m_instances.TryGetValue(zdo, out ZNetView view) && view != null && view.TryGetComponent(out KwShipRiders ship))
                Run(ship, true);
        }

        // After the game let go of the ships out of range (their ZDOs reset, their objects destroyed at the frame's end).
        [HarmonyPatch(typeof(ZNetScene), "RemoveObjects"), HarmonyPostfix]
        private static void Unloaded()
        {
            IReadOnlyList<KwShipRiders> ships = KwShipRiders.Loaded;
            for (int i = 0; i < ships.Count; i++)
            {
                KwShipRiders ship = ships[i];
                if (ship != null && ship.Riders.Count > 0 && ship.Zdo == null)
                    Run(ship, false);
            }
        }

        private static void Run(KwShipRiders ship, bool destroyed)
        {
            try
            {
                ship.LetGo(destroyed);
            }
            catch (Exception e)
            {
                Guard.Report(e, "Broadside ship riders let go");
            }
        }
    }
}
