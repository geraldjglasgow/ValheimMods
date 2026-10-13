using EliteCreaturesReborn.Patches;
using HarmonyLib;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Puts an <see cref="ArenaFighter"/> on a creature as its monster AI starts, on every machine that loads it, when it
    /// is an arena creature or starts inside the built hall: the one that spawned it, one it is handed to, one that loads
    /// it later from a saved world. A tag is written in the frame a creature is made, before its Start, and a ZDO arrives
    /// whole before its object is made, so the tag is there by now; an untagged creature inside the hall gets one too, to
    /// be adopted while a trial runs. Every monster that starts comes through here once: one ZDO read and, with the hall
    /// built, one distance test tell the rest apart, as the raids' own attach does (<see cref="Raids.RaiderAttachPatch"/>).
    /// </summary>
    [HarmonyPatch(typeof(MonsterAI), "Start")]
    internal static class ArenaAttachPatch
    {
        private static void Postfix(MonsterAI __instance)
        {
            ZNetView nview = __instance.m_nview;
            ZDO? zdo = nview != null ? nview.GetZDO() : null;
            if (zdo != null && (ArenaTag.IsArena(zdo) || (ArenaHall.Built && ArenaHall.Contains(zdo.GetPosition()))))
            {
                SafeCall.Run("MonsterAI.Start arena", static ai => Attach(ai), __instance);
            }
        }

        private static void Attach(MonsterAI ai)
        {
            if (ai.GetComponent<ArenaFighter>() == null)
            {
                ai.gameObject.AddComponent<ArenaFighter>();
            }
        }
    }
}
