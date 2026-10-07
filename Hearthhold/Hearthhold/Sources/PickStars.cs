using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// Stars on wild picks (Foraging) and crops (Farming). Pickable.Interact runs on the picker's client (the use key, a
    /// scythe, GrindstoneSkills' sweep, which calls Interact for every plant it picks) and sends RPC_Pick to the plant's
    /// owner on every call, even while the plant only reads unpicked until the owner answers; the owner spawns the pick
    /// there (Pickable.Drop: Instantiate, the game's bonus yield, the extra drops) and ignores the RPC once the plant is
    /// picked. So:
    /// <list type="bullet">
    /// <item>a prefix on Interact, first of all prefixes, sends the picker's level for the plant's source as a mark
    /// (<see cref="Marks"/>) whenever the plant can still be picked, just before the game's RPC_Pick;</item>
    /// <item>on the owner, a prefix on RPC_Pick takes the sender's mark (level 0 without one) and opens a scope
    /// (<see cref="SourceRoll"/>) for an unpicked plant; a finalizer closes it, after GrindstoneSkills' postfix that
    /// drops a giant crop's extra crop, which so rolls too.</item>
    /// </list>
    /// A picker who owns the plant runs RPC_Pick inside Interact, after the mark, which is handled at once.
    /// </summary>
    public static class PickStars
    {
        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
        private static class Interact
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Pickable __instance, Humanoid character)
            {
                if (character != null && character == Player.m_localPlayer && CanPick(__instance))
                    HookGuard.Run("pick mark", static pickable => SendMark(pickable), __instance);
            }
        }

        [HarmonyPatch(typeof(Pickable), nameof(Pickable.RPC_Pick))]
        private static class Pick
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Pickable __instance, long sender, out SourceRoll.Scope __state)
            {
                __state = default;
                if (CanPick(__instance) && __instance.m_nview.IsOwner())
                    __state = HookGuard.Run("pick stars", () => OpenScope(__instance, sender), default(SourceRoll.Scope));
            }

            [HarmonyFinalizer]
            private static void Finalizer(SourceRoll.Scope __state) => __state.Close();
        }

        /// <summary>A plant the game would pick now: valid, enabled and not picked (cheap: no strings, no lookups).</summary>
        private static bool CanPick(Pickable pickable) =>
            pickable.m_nview != null && pickable.m_nview.IsValid() && !pickable.m_picked && pickable.m_enabled != 0;

        private static void SendMark(Pickable pickable)
        {
            if (Sources.TryPickSource(pickable, out StarSource source))
                Marks.Send(pickable.m_nview, GrindstoneLink.LocalLevel(StarSources.Skill(source)), 0f);
        }

        private static SourceRoll.Scope OpenScope(Pickable pickable, long sender) =>
            Sources.TryPickSource(pickable, out StarSource source) ? SourceRoll.FromMark(pickable.m_nview, sender, source) : default;
    }
}
