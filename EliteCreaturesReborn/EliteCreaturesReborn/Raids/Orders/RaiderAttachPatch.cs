using EliteCreaturesReborn.Patches;
using HarmonyLib;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// Puts <see cref="RaiderSteering"/> on a raider as its monster AI starts, on every machine that loads it: the one
    /// that spawned it, one it is handed to, one that loads it later from a saved world. A raider is tagged in the
    /// frame it is instantiated, after its Awake and before its Start, and a ZDO arrives on another machine whole, tag
    /// and all, before the object is made from it - so the tag is always there by now. Every monster that starts comes
    /// through here once; one ZDO read tells a raider from the rest, which cost nothing more.
    /// </summary>
    [HarmonyPatch(typeof(MonsterAI), "Start")]
    internal static class RaiderAttachPatch
    {
        private static void Postfix(MonsterAI __instance)
        {
            ZNetView nview = __instance.m_nview;
            if (nview != null && RaiderTag.IsRaider(nview.GetZDO()))
            {
                SafeCall.Run("MonsterAI.Start raider", static ai => Attach(ai), __instance);
            }
        }

        private static void Attach(MonsterAI ai)
        {
            if (ai.GetComponent<RaiderSteering>() == null)
            {
                ai.gameObject.AddComponent<RaiderSteering>();
            }
        }
    }
}
