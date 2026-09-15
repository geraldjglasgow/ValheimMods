using HarmonyLib;

namespace Lockstep
{
    /// <summary>
    /// The gate: a boss altar refuses to summon while its stage is closed. The visible gate is on the two
    /// altar entry points (offering item, item stands); the safety net is on the spawn RPC the altar owner runs.
    /// </summary>
    public static class AltarGate
    {
        /// <summary>True when the altar's boss is in the chain and its stage is closed. Shows the message to the user when given.</summary>
        public static bool IsClosed(OfferingBowl bowl, Humanoid user)
        {
            if (bowl.m_bossPrefab == null)
                return false;
            ProgressState.StageStatus status = ProgressState.ForBoss(bowl.m_bossPrefab.name);
            if (status == null || status.Open)
                return false;
            user?.Message(MessageHud.MessageType.Center, ProgressState.ClosedMessage(status));
            return true;
        }
    }

    [HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.UseItem))]
    public static class OfferingBowlUseItemPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(OfferingBowl __instance, Humanoid user, ref bool __result)
        {
            if (!AltarGate.IsClosed(__instance, user))
                return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.Interact))]
    public static class OfferingBowlInteractPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(OfferingBowl __instance, Humanoid user, ref bool __result)
        {
            if (!__instance.m_useItemStands || !AltarGate.IsClosed(__instance, user))
                return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(OfferingBowl), "RPC_SpawnBoss")]
    public static class OfferingBowlSpawnGuardPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(OfferingBowl __instance)
        {
            if (!LockstepConfiguration.SpawnGuard.Value || !AltarGate.IsClosed(__instance, null))
                return true;
            Lockstep.Log.LogWarning($"Blocked a spawn of {__instance.m_bossPrefab.name}: its stage is closed. The summoning client may have stale state.");
            return false;
        }
    }
}
