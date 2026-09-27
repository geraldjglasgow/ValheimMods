using System.Runtime.CompilerServices;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Tapping, on the barrel's ZDO owner. The game's RPC_Tap (owner only, when Ready) keeps the content hash in a field,
    /// clears the content and start time, and Invokes DelayedTap after m_tapDelay; DelayedTap then spawns
    /// m_producedItems meads on the same machine. When RPC_Tap did tap (the content went from set to 0), the base's
    /// stars are kept for that barrel instance, the way the game keeps the content, and the barrel's keys are cleared.
    /// DelayedTap takes them and spawns inside a <see cref="FermenterSpawnStars"/> scope, so every mead of the batch,
    /// FeastMaster's changed yield included, carries the base's stars. A DelayedTap with nothing kept gives 0 stars.
    /// </summary>
    internal static class FermenterTap
    {
        private sealed class Batch
        {
            public int StarCount;
        }

        /// <summary>Stars waiting for DelayedTap, per barrel; weak, so a barrel unloaded before it runs is not kept alive.</summary>
        private static readonly ConditionalWeakTable<Fermenter, Batch> batches = new ConditionalWeakTable<Fermenter, Batch>();

        private static int Take(Fermenter fermenter)
        {
            if (!batches.TryGetValue(fermenter, out Batch batch))
                return 0;
            batches.Remove(fermenter);
            return batch.StarCount;
        }

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.RPC_Tap))]
        private static class Tap
        {
            [HarmonyPrefix]
            private static void Prefix(Fermenter __instance, out int __state) => __state = __instance.GetContent();

            [HarmonyPostfix]
            private static void Postfix(Fermenter __instance, int __state)
            {
                if (__state == 0 || __instance.GetContent() != 0)
                    return;
                ZDO zdo = __instance.m_nview.GetZDO();
                batches.GetOrCreateValue(__instance).StarCount = FermenterBase.GetStars(zdo);
                FermenterBase.Clear(zdo);
            }
        }

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.DelayedTap))]
        private static class Spawn
        {
            [HarmonyPrefix]
            private static void Prefix(Fermenter __instance, out int? __state) => __state = FermenterSpawnStars.Begin(Take(__instance));

            [HarmonyFinalizer]
            private static void Finalizer(int? __state) => FermenterSpawnStars.End(__state);
        }
    }
}
