using System;
using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// The hammer's repair on a launched ship (the user, 2026-10-26: "when a ship gets back to port and they start repairing
    /// it with the hammer, these wooden repair kits should just go away ... as they sit there repairing their ship. It would
    /// also be nice, as they repair the ship with the hammer, if things like sails and anything that was shot and fell off
    /// the ship incrementally start generating back"), on the repairing player's game: the one postfix on
    /// <c>WearNTear.Repair</c> for a launched ship (the game's own checks first: a workbench in range, wards). Each click
    /// mends one thing (<see cref="KwShipRepairWork"/>): what the hammer is aimed at, else the next of
    /// <see cref="KwShipRepairPlan"/>'s steps not asked just now; the game's own repair of the ship's health, to full, goes on
    /// alongside, and a click that mends something counts as a repair even with the health full, so the game's swing,
    /// stamina and message go with it.
    /// </summary>
    [HarmonyPatch]
    public static class KwShipRepair
    {
        private const float Every = 0.15f;
        private static float last = -99f;

        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Repair)), HarmonyPostfix]
        private static void Repaired(WearNTear __instance, ref bool __result)
        {
            Player? me = Player.m_localPlayer;
            if (me == null || !me.InPlaceMode() || GameCamera.instance == null || Time.time - last < Every || !__instance.TryGetComponent(out KwHullHoles holes))
                return;
            try
            {
                if (!new KwShipRepairWork(holes, me).Next())
                    return;
                last = Time.time;
                __result = true;
            }
            catch (Exception e)
            {
                Guard.Report(e, "Broadside ship repair");
            }
        }
    }

    /// <summary>
    /// The steps the local player's hammer has just asked of a ship's owner (<see cref="KwShipRepairWork"/>), so the next
    /// click goes on to the next one while the owner's answer is on its way; a step is asked again only after
    /// <see cref="Wait"/> s, when the owner has not done it.
    /// </summary>
    internal static class KwShipRepairAsked
    {
        public const float Wait = 1.5f;
        private static readonly Dictionary<string, float> asked = new Dictionary<string, float>();

        public static bool Waiting(Object ship, KwRepairStep step) => asked.TryGetValue(Key(ship, step), out float at) && Time.time - at < Wait;

        public static void Mark(Object ship, KwRepairStep step)
        {
            if (asked.Count > 64)
                asked.Clear();
            asked[Key(ship, step)] = Time.time;
        }

        private static string Key(Object ship, KwRepairStep step) => ship.GetInstanceID() + "/" + step.Key;
    }
}
