using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Growth Speed: tamed young animals grow up and warm eggs hatch sooner with a keeper near. Both run on the ZDO
    /// owner of the young animal or egg, where the game decides:
    /// <list type="bullet">
    /// <item>Growup.GrowUpdate (private, every 10 s on piglets, cubs, calves, chicks, hatchlings) replaces the young
    /// with an adult once its age (BaseAI.GetTimeSinceSpawned) passes m_growTime. Only a tamed young is sped up.</item>
    /// <item>EggGrow.GrowUpdate (private, every 5 s) hatches an egg once world time passes its warm start plus m_growTime.
    /// Every egg is sped up: eggs are the players' own.</item>
    /// </list>
    /// For the one call m_growTime is divided by <see cref="Factor"/>, then put back in a finalizer. The speed-up
    /// follows the keeper near it now, so a young that was left alone catches up as soon as a keeper returns.
    /// </summary>
    public static class GrowingUp
    {
        private const float Untouched = -1f;

        [HarmonyPatch(typeof(Growup), nameof(Growup.GrowUpdate))]
        private static class Young
        {
            [HarmonyPrefix]
            private static void Prefix(Growup __instance, out float __state)
            {
                Growup young = __instance;
                float factor = HookGuard.Run("growing up", () => YoungFactor(young), 1f);
                __state = factor > 1f ? young.m_growTime : Untouched;
                if (factor > 1f)
                    young.m_growTime /= factor;
            }

            [HarmonyFinalizer]
            private static void Finalizer(Growup __instance, float __state)
            {
                if (__state >= 0f)
                    __instance.m_growTime = __state;
            }
        }

        [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.GrowUpdate))]
        private static class Egg
        {
            [HarmonyPrefix]
            private static void Prefix(EggGrow __instance, out float __state)
            {
                EggGrow egg = __instance;
                float factor = HookGuard.Run("hatching", () => Tends(egg.m_nview) ? Factor(egg.transform.position) : 1f, 1f);
                __state = factor > 1f ? egg.m_growTime : Untouched;
                if (factor > 1f)
                    egg.m_growTime /= factor;
            }

            [HarmonyFinalizer]
            private static void Finalizer(EggGrow __instance, float __state)
            {
                if (__state >= 0f)
                    __instance.m_growTime = __state;
            }
        }

        /// <summary>1 + Growth Speed's share for the best keeper within Keeper Range of <paramref name="position"/>; 1 while Husbandry is off.</summary>
        public static float Factor(Vector3 position) =>
            1f + Keeper.Share(HusbandryBreedingSettings.GrowthSpeed.Value, position);

        private static float YoungFactor(Growup young)
        {
            if (!Tends(young.m_nview))
                return 1f;
            Character character = young.GetComponent<Character>();
            return character != null && character.IsTamed() ? Factor(young.transform.position) : 1f;
        }

        /// <summary>This machine decides for the object, and Husbandry is on.</summary>
        private static bool Tends(ZNetView nview) =>
            HusbandrySkill.Active && nview != null && nview.IsValid() && nview.IsOwner();
    }
}
