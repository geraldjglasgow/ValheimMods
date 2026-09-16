using EliteCreaturesReborn.Rules;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Makes cleared camps and dungeons repopulate, by giving a one-shot spawner the respawn timer it was built
    /// without. The game's own spawner already knows how to respawn - it keeps the time of its last live creature in
    /// its ZDO and waits <c>m_respawnTimeMinuts</c> world minutes - but dungeon and camp spawners ship with that set to
    /// zero, which is what makes a cleared landmark stay dead. Writing a positive timer hands the work back to the
    /// game, so nothing here has to track a creature or spawn one.
    ///
    /// A spawner whose timer the game already set is never touched: that one respawns as its designer intended.
    /// Runs on every machine, before the spawner's own first update; the value is a local field the owner reads, so no
    /// state is written and there is nothing to replicate.
    /// </summary>
    [HarmonyPatch(typeof(CreatureSpawner), "Awake")]
    public static class SpawnerRespawnPatch
    {
        private static void Postfix(CreatureSpawner __instance) =>
            Guard.Run("CreatureSpawner.Awake respawn", () => Arm(__instance));

        private static void Arm(CreatureSpawner spawner)
        {
            if (spawner == null || spawner.m_respawnTimeMinuts > 0f)
            {
                return;
            }
            float minutes = RuleState.Active.Respawn.MinutesFor(InDungeon(spawner));
            if (minutes > 0f)
            {
                spawner.m_respawnTimeMinuts = minutes;
            }
        }

        /// <summary>A dungeon spawner sits inside a generated room; a camp's sits in the world.</summary>
        internal static bool InDungeon(Component spawner) => spawner.GetComponentInParent<Room>() != null;
    }
}
