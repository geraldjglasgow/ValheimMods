using HarmonyLib;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// WearNTear.Highlight prefix: while the Copy entry is selected the game's own hover tint (the support colours of the
    /// piece under the crosshair) is held back, so the Copy glow is the only tint and the game never takes it off.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Highlight))]
    public static class CopyHighlightPatch
    {
        [HarmonyPrefix]
        public static bool Prefix() => !BlueprintSafe.Call("OpenKeep copy hover tint", () => CopySession.Selected, false);
    }

    /// <summary>
    /// WearNTear.ResetHighlight postfix (private; 0.2 s after the game's hover tint, and when a piece breaks): a piece the
    /// Copy tool lights gets its glow back, for a tint the game set just before the entry was selected.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.ResetHighlight))]
    public static class CopyHighlightResetPatch
    {
        [HarmonyPostfix]
        public static void Postfix(WearNTear __instance) => BlueprintSafe.Run("OpenKeep copy glow", () => CopyGlow.Restore(__instance));
    }
}
