using HarmonyLib;
using Translations;

namespace Lockstep
{
    /// <summary>
    /// The altar's hover names its gate: open, or who the group still waits for. The line is built once per altar, state
    /// and language, the joined text once per vanilla text, so a hovered altar costs a string compare per frame.
    /// </summary>
    public static class AltarHover
    {
        private const string Open = "<color=#8fd16a>";
        private const string Sealed = "<color=#ff8060>";
        private const string Warning = "<color=orange>";
        private const string End = "</color>";

        private static OfferingBowl lastBowl;
        private static int lastRevision = -1, lastWords = -1;
        private static bool lastNamed, lastSilent;
        private static string line, lastVanilla, lastText;

        public static string Append(OfferingBowl bowl, string vanilla)
        {
            Refresh(bowl);
            if (line == null)
                return vanilla;
            if (vanilla != lastVanilla)
            {
                lastVanilla = vanilla;
                lastText = vanilla + "\n" + line;
            }
            return lastText;
        }

        /// <summary>Rebuilds the line when the altar, the server's summary, a setting it shows or the language has changed.</summary>
        private static void Refresh(OfferingBowl bowl)
        {
            bool named = LockstepConfiguration.NameMissingPlayers.Value, silent = ServerCheck.Silent;
            if (ReferenceEquals(bowl, lastBowl) && lastRevision == ProgressState.Revision && lastWords == Lang.Version
                && lastNamed == named && lastSilent == silent)
                return;
            lastBowl = bowl;
            lastRevision = ProgressState.Revision;
            lastWords = Lang.Version;
            lastNamed = named;
            lastSilent = silent;
            line = Line(bowl);
            lastVanilla = null;
        }

        private static string Line(OfferingBowl bowl)
        {
            if (bowl.m_bossPrefab == null)
                return null;
            string boss = bowl.m_bossPrefab.name;
            if (ServerCheck.Silent)
                return Chain.ByBoss(boss) != null ? Warning + Lang.Get("lockstep_hover_not_running") + End : null;
            ProgressState.StageStatus status = ProgressState.ForBoss(boss);
            if (status == null)
                return null;
            if (!status.Open)
                return Sealed + SealedText(status) + End;
            if (status.PreviousName.Length == 0)
                return Open + Lang.Get("lockstep_hover_open_first") + End;
            return Open + Lang.Format("lockstep_hover_open", ProgressState.Shown(status.PreviousName)) + End;
        }

        /// <summary>Who a sealed altar waits for: the missing players by name, or the group when names are hidden.</summary>
        private static string SealedText(ProgressState.StageStatus status) =>
            LockstepConfiguration.NameMissingPlayers.Value
                ? Lang.Format("lockstep_hover_sealed", status.Waiting, ProgressState.Shown(status.PreviousName))
                : Lang.Format("lockstep_hover_sealed_group", ProgressState.Shown(status.PreviousName));
    }

    [HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.GetHoverText))]
    public static class OfferingBowlHoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(OfferingBowl __instance, ref string __result) => __result = AltarHover.Append(__instance, __result);
    }
}
