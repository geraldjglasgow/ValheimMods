using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Marks a plant with its planter. Player.PlacePiece instantiates the piece on the placing client, which owns the new
    /// ZDO, and calls Piece.SetCreator on it with the player's ID. While the local player places, a plant that gets its
    /// creator is given the planter's ID and Farming level (<see cref="PlantKeys"/>): every planter-level perk reads them,
    /// on any machine. The plant then waits for the seed it is paid with (<see cref="SeedStars"/>), unless
    /// <see cref="Sowing"/> placed it, which pays first and takes the plant from <see cref="TakeSown"/>. Vines are left to
    /// the game, so they get no planter and no growth perks.
    /// </summary>
    public static class PlantPlacing
    {
        private static bool placing;
        private static Plant sown;

        /// <summary>The plant the last <see cref="Sowing"/> placement made, once; null when none.</summary>
        public static Plant TakeSown()
        {
            Plant plant = sown;
            sown = null;
            return plant;
        }

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
                    HookGuard.Run("Farming planter", () => Mark(__instance.GetComponent<Plant>(), uid));
            }
        }

        private static void Mark(Plant plant, long planter)
        {
            ZDO zdo = PlantKeys.Of(plant);
            if (zdo == null || planter == 0L || CropCatalog.GrowsVine(plant))
                return;
            PlantKeys.WritePlanter(zdo, planter, FarmSkill.Local());
            if (Sowing.Active)
                sown = plant;
            else
                SeedStars.Await(plant);
        }
    }
}
