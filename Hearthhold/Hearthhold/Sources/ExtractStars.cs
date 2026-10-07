using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// Stars on honey and sap (Foraging, as wild picks). Beehive.Interact and SapCollector.Interact run on the taker's
    /// client: a first press (not a held repeat) on a hive or collector holding something sends RPC_Extract to its
    /// owner, who instantiates one item per level held at the spawn point and empties it. So:
    /// <list type="bullet">
    /// <item>a prefix on Interact, first of all prefixes, sends the taker's Foraging level as a mark
    /// (<see cref="Marks"/>) just before the game's RPC whenever there is something to take; when the ward refuses the
    /// taker no RPC follows and the mark simply expires;</item>
    /// <item>on the owner, a prefix on RPC_Extract takes the caller's mark (level 0 without one) and opens a scope
    /// (<see cref="SourceRoll"/>); a finalizer closes it after every postfix, so the extra honey GrindstoneSkills
    /// instantiates in its RPC_Extract postfix (Husbandry's Extra Honey) rolls too.</item>
    /// </list>
    /// </summary>
    public static class ExtractStars
    {
        private const StarSource Source = StarSource.Forage;

        [HarmonyPatch(typeof(Beehive), nameof(Beehive.Interact))]
        private static class HiveInteract
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Beehive __instance, Humanoid character, bool repeat)
            {
                if (!repeat && IsLocal(character) && __instance.GetHoneyLevel() > 0)
                    HookGuard.Run("honey mark", static view => SendMark(view), __instance.m_nview);
            }
        }

        [HarmonyPatch(typeof(Beehive), nameof(Beehive.RPC_Extract))]
        private static class HiveExtract
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Beehive __instance, long caller, out SourceRoll.Scope __state) =>
                __state = IsOwner(__instance.m_nview) && __instance.GetHoneyLevel() > 0 ? OpenScope(__instance.m_nview, caller) : default;

            [HarmonyFinalizer]
            private static void Finalizer(SourceRoll.Scope __state) => __state.Close();
        }

        [HarmonyPatch(typeof(SapCollector), nameof(SapCollector.Interact))]
        private static class SapInteract
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(SapCollector __instance, Humanoid character, bool repeat)
            {
                if (!repeat && IsLocal(character) && __instance.GetLevel() > 0)
                    HookGuard.Run("sap mark", static view => SendMark(view), __instance.m_nview);
            }
        }

        [HarmonyPatch(typeof(SapCollector), nameof(SapCollector.RPC_Extract))]
        private static class SapExtract
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(SapCollector __instance, long caller, out SourceRoll.Scope __state) =>
                __state = IsOwner(__instance.m_nview) && __instance.GetLevel() > 0 ? OpenScope(__instance.m_nview, caller) : default;

            [HarmonyFinalizer]
            private static void Finalizer(SourceRoll.Scope __state) => __state.Close();
        }

        private static bool IsLocal(Humanoid character) => character != null && character == Player.m_localPlayer;

        private static bool IsOwner(ZNetView nview) => nview != null && nview.IsValid() && nview.IsOwner();

        private static void SendMark(ZNetView nview) => Marks.Send(nview, GrindstoneLink.LocalLevel(StarSources.Skill(Source)), 0f);

        private static SourceRoll.Scope OpenScope(ZNetView nview, long caller) =>
            HookGuard.Run("extract stars", () => SourceRoll.FromMark(nview, caller, Source), default(SourceRoll.Scope));
    }
}
