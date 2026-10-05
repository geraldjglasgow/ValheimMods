using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Ship speed from the helmsman's Sailing level. The game moves a ship in Ship.CustomFixedUpdate on its ZDO owner
    /// only, and the owner is not always the helmsman: ownership moves only when the owner leaves the ship
    /// (Ship.UpdateOwner). So the owner reads the helmsman's level, its own skill when it steers, otherwise the level
    /// the helmsman's client publishes on their player ZDO (<see cref="SailingXp"/>).
    /// For that one call the sail force factor and the oar force are raised, then put back, so ShipConfig's per-ship
    /// values and anything else that sets them are kept. The game's forward drag grows with the square of the speed,
    /// so top speed grows with the square root of the force: the force is raised by the square of the speed bonus.
    /// </summary>
    public static class HelmSpeed
    {
        public struct Forces
        {
            public bool Raised;
            public float Sail;
            public float Oars;
        }

        [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
        private static class Physics
        {
            [HarmonyPrefix]
            private static void Prefix(Ship __instance, out Forces __state)
            {
                __state = default;
                float factor = ForceFactor(__instance);
                if (factor <= 1f)
                    return;
                __state = new Forces { Raised = true, Sail = __instance.m_sailForceFactor, Oars = __instance.m_backwardForce };
                __instance.m_sailForceFactor *= factor;
                __instance.m_backwardForce *= factor;
            }

            [HarmonyFinalizer]
            private static void Finalizer(Ship __instance, Forces __state)
            {
                if (!__state.Raised)
                    return;
                __instance.m_sailForceFactor = __state.Sail;
                __instance.m_backwardForce = __state.Oars;
            }
        }

        /// <summary>The force multiplier on the machine that moves the ship; 1 anywhere else or without a helmsman.</summary>
        private static float ForceFactor(Ship ship)
        {
            ZNetView nview = ship.m_nview;
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
                return 1f;
            float speed = SpeedFactor(ship);
            return speed * speed;
        }

        /// <summary>The top speed factor the helmsman's level gives the ship: 1.2 is 20% faster; 1 without a helmsman or with Sailing off.</summary>
        public static float SpeedFactor(Ship ship)
        {
            Player helmsman = SailingSkill.Active ? Helm.Helmsman(ship) : null;
            return helmsman != null ? 1f + SailingSkill.Share(SailingSettings.ShipSpeed.Value, SailingSkill.Of(helmsman)) : 1f;
        }
    }
}
