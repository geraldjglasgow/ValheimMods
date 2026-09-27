using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// A hint under a starred plant's hover text, for players whose Show Hints is on: when it is best picked ("Best
    /// picked at night"), or, while it is, "At its best now". Only plants whose Forage entry has stars and a best
    /// time; the test is the hovering player's own environment, the same one the pick uses.
    /// </summary>
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
    public static class ForageHover
    {
        private const string Now = "<color=yellow>At its best now</color>";
        private const string Later = "<color=#B0B0B0>Best picked {0}</color>";

        [HarmonyPostfix]
        private static void Postfix(Pickable __instance, ref string __result)
        {
            if (string.IsNullOrEmpty(__result) || !ForagingSettings.ShowHints.Value)
                return;
            ForageEntry entry = Forage.Of(__instance);
            if (entry == null || !entry.Stars || entry.Best == BestTime.None)
                return;
            __result += "\n" + (BestTimes.IsNow(entry.Best) ? Now : string.Format(Later, BestTimes.Describe(entry.Best)));
        }
    }
}
