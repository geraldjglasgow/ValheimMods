using System.Collections.Generic;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Who may build the Animal Feeder: a player whose Husbandry level reaches Feeder Level, while Husbandry is on. On
    /// the building player's own client:
    /// <list type="bullet">
    /// <item><c>Player.HaveRequirements(Piece, RequirementMode)</c> answers false for the feeder otherwise. The game asks
    /// it in IsKnown mode to decide whether a hammer piece is learned (only then does it enter the build menu, with the
    /// "new piece" message) and in CanBuild mode before placing one, so a player below the level neither learns nor
    /// places it.</item>
    /// <item><c>PieceTable.UpdateAvailable</c> fills the build menu from every learned piece and does not ask again; a
    /// postfix takes the feeder back out, so a player who learned it and then fell below the level (skill loss on death,
    /// a higher Feeder Level, Husbandry turned off) no longer sees it.</item>
    /// </list>
    /// Feeders already built keep working for everyone.
    /// </summary>
    public static class FeederGate
    {
        public static bool Allowed(Player player) =>
            HusbandrySkill.Active && player != null
            && HusbandrySkill.Reaches(HusbandrySkill.Of(player), HusbandryCompanionSettings.FeederLevel.Value);

        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
        private static class Requirements
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance, Piece piece, ref bool __result)
            {
                if (__result && FeederPrefab.Is(piece) && !HookGuard.Run("feeder gate", () => Allowed(__instance), true))
                    __result = false;
            }
        }

        [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable))]
        private static class Menu
        {
            [HarmonyPostfix]
            private static void Postfix(PieceTable __instance, Player player)
            {
                Piece feeder = FeederPrefab.Piece;
                if (feeder != null && __instance.m_availablePieces.Contains(feeder)
                    && !HookGuard.Run("feeder gate", () => Allowed(player), true))
                    Hide(__instance, feeder);
            }
        }

        private static void Hide(PieceTable table, Piece feeder)
        {
            table.m_availablePieces.Remove(feeder);
            foreach (List<Piece> category in table.m_availablePiecesByCategory)
                category.Remove(feeder);
        }
    }
}
