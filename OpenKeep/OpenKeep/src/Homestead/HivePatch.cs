using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Beehive.UpdateBees</c> (every 10 s on every client that has the hive loaded) adds honey only on the hive's ZDO
    /// owner, and it is the only reader of <c>m_secPerUnit</c>. The prefix sets that field on the owner's instance each
    /// tick, to this mod's rate or back to the prefab's, and converts the stored progress; the postfix keeps the seconds
    /// the game drops after a finished honey.
    /// </summary>
    [HarmonyPatch(typeof(Beehive), nameof(Beehive.UpdateBees))]
    public static class HivePatch
    {
        [HarmonyPrefix]
        public static void Prefix(Beehive __instance, out HiveProgress.Tick __state)
        {
            __state = null;
            ZNetView view = __instance.m_nview;
            if (view == null || !view.IsValid() || !view.IsOwner())
                return;
            float vanilla = HiveRate.VanillaSeconds(__instance);
            if (vanilla <= 0f)
                return;
            float seconds = HiveRate.SecondsPerHoney(__instance);
            bool active = seconds > 0f;
            __instance.m_secPerUnit = active ? seconds : vanilla;
            HiveProgress.Rescale(view.GetZDO(), __instance.m_secPerUnit, vanilla, active);
            if (active)
                __state = HiveProgress.Before(view.GetZDO(), seconds);
        }

        [HarmonyPostfix]
        public static void Postfix(Beehive __instance, HiveProgress.Tick __state)
        {
            ZNetView view = __instance.m_nview;
            if (__state != null && view != null && view.IsValid() && view.IsOwner())
                HiveProgress.KeepRemainder(view.GetZDO(), __state);
        }
    }
}
