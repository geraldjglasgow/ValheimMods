using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// The game's breeding check, Procreation.Procreate: private, every 30 s (m_updateInterval) on every breeding
    /// creature, doing work only on the creature's ZDO owner (often a dedicated server) and only when it is tamed. It
    /// either gives birth (pregnant and due) or runs a love check. While Husbandry is on, on that same machine:
    /// <list type="bullet">
    /// <item>The prefix paces the check (<see cref="BreedingPace"/>: shorter pregnancy, fewer skipped love checks, more
    /// room in the herd), then, for a birth, rolls Twins and Better offspring (<see cref="BirthRolls"/>). It runs first
    /// (Priority.First) so Elite Creatures Reborn's own prefix sees the same "due" and the star-up key.</item>
    /// <item>The postfix books the birth: the birth counter, a twin pregnancy, the callouts.</item>
    /// <item>The finalizer puts the game's values back and closes the birth scope, even when the game's code threw.</item>
    /// </list>
    /// </summary>
    public static class BreedingCheck
    {
        [HarmonyPatch(typeof(Procreation), nameof(Procreation.Procreate))]
        private static class Procreate
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Procreation __instance, out BreedingCall __state)
            {
                Procreation parent = __instance;
                __state = Breeds(parent) ? HookGuard.Run("breeding pace", static p => BreedingPace.Apply(p), parent, (BreedingCall)null) : null;
                BreedingCall call = __state;
                if (call != null)
                    HookGuard.Run("birth rolls", static birth => BirthRolls.Before(birth.parent, birth.call), (parent, call));
            }

            [HarmonyPostfix]
            private static void Postfix(Procreation __instance, BreedingCall __state)
            {
                if (__state != null)
                    HookGuard.Run("birth", static birth => BirthRolls.After(birth.parent, birth.call), (parent: __instance, call: __state));
            }

            [HarmonyFinalizer]
            private static void Finalizer(Procreation __instance, BreedingCall __state)
            {
                if (__state == null)
                    return;
                BreedingPace.Restore(__instance, __state);
                HookGuard.Run("birth close", () => OffspringStar.End(__instance, __state.Star));
            }
        }

        /// <summary>The check does the game's work here: Husbandry on, this machine owns the creature, and it is tamed.</summary>
        private static bool Breeds(Procreation procreation)
        {
            ZNetView nview = procreation.m_nview;
            Tameable tameable = procreation.m_tameable;
            return HusbandrySkill.Active && nview != null && nview.IsValid() && nview.IsOwner()
                && tameable != null && tameable.IsTamed();
        }
    }
}
