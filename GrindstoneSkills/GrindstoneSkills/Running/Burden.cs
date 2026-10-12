using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Burden, Running's milestone at <see cref="UnlockLevel"/>: the stamina an over-encumbered player spends while
    /// moving is cut to <see cref="DrainShare"/>. The game spends it in Player.UpdateStats(float), once a physics tick,
    /// as m_encumberedStaminaDrain times the tick while the player is encumbered and has move input. For that one call
    /// the local player's field is lowered, then put back by a finalizer, so the value the prefab or another mod set is
    /// kept and only this one formula changes.
    /// <para>Runs on the player's own client, which owns the player and spends their stamina; every other player returns
    /// at the first test. Off, or below the level, the field is not touched.</para>
    /// The prefix runs last and the finalizer first, so this save and restore sits inside any other mod's save and
    /// restore of the same field (FeastMaster's, for one): Harmony runs finalizers in prefix order, not reversed.
    /// </summary>
    public static class Burden
    {
        private const float UnlockLevel = 50f;
        private const float DrainShare = 0.5f;

        /// <summary>The Run level that unlocks Burden.</summary>
        public static float Level => UnlockLevel;

        /// <summary>The encumbered drain per second with Burden.</summary>
        public static float Drain(Player player) => player.m_encumberedStaminaDrain * DrainShare;

        [HarmonyPatch(typeof(Player), nameof(Player.UpdateStats), typeof(float))]
        private static class Stats
        {
            [HarmonyPrefix, HarmonyPriority(Priority.Last)]
            private static void Prefix(Player __instance, out float __state) =>
                __state = HookGuard.Run("burden", static player => Lower(player), __instance, float.NaN);

            [HarmonyFinalizer, HarmonyPriority(Priority.First)]
            private static void Finalizer(Player __instance, float __state)
            {
                if (!float.IsNaN(__state))
                    __instance.m_encumberedStaminaDrain = __state;
            }
        }

        /// <summary>Lowers the local player's drain for this call; returns the value to restore, or NaN when untouched.</summary>
        private static float Lower(Player player)
        {
            if (!LocalSkill.IsLocal(player) || !GameSkillSettings.Running.Value || !LocalSkill.Reached(Skills.SkillType.Run, UnlockLevel))
                return float.NaN;
            float original = player.m_encumberedStaminaDrain;
            player.m_encumberedStaminaDrain = original * DrainShare;
            return original;
        }
    }
}
