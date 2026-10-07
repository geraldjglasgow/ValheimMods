using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Marks a plant with its planter. Player.PlacePiece instantiates the piece on the placing client, which owns the new
    /// ZDO, and calls Piece.SetCreator on it with the player's ID. While the local player places (by hand, or through
    /// <see cref="Sowing"/>), a plant that gets its creator is given the planter's ID and Farming level
    /// (<see cref="PlantKeys"/>): every planter-level perk reads them, on any machine. Vines are left to the game, so they
    /// get no planter and no growth perks.
    /// </summary>
    public static class PlantPlacing
    {
        private static bool placing;

        [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
        private static class Placing
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance) => placing = __instance == Player.m_localPlayer;

            [HarmonyFinalizer]
            private static void Finalizer() => placing = false;
        }

        [HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
        private static class Creator
        {
            [HarmonyPostfix]
            private static void Postfix(Piece __instance, long uid)
            {
                if (placing && FarmSkill.Active)
                    HookGuard.Run("Farming planter", static planted => Mark(planted.piece.GetComponent<Plant>(), planted.uid), (piece: __instance, uid));
            }
        }

        private static void Mark(Plant plant, long planter)
        {
            ZDO zdo = PlantKeys.Of(plant);
            if (zdo != null && planter != 0L && !CropCatalog.GrowsVine(plant))
                PlantKeys.WritePlanter(zdo, planter, FarmSkill.Local());
        }
    }
}
