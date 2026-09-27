using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Planting experience. Player.UpdatePlacement places the selected piece and raises the piece table's skill (Farming
    /// for the cultivator) in the same call, on the local player's client. While it runs with a plant selected, a
    /// <see cref="FarmXp"/> scope is open with that plant's crop (null for tree saplings: tier 1), so the raise is
    /// scaled; the extra plants of a row (<see cref="RowPlanting"/>) raise inside it too.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
    public static class PlantingScope
    {
        [HarmonyPrefix]
        private static void Prefix(Player __instance, out bool __state)
        {
            __state = false;
            if (__instance != Player.m_localPlayer || !FarmSkill.Active || __instance.m_placementGhost == null)
                return;
            Piece piece = __instance.GetSelectedPiece();
            Plant plant = piece != null ? piece.GetComponent<Plant>() : null;
            if (plant != null)
                __state = FarmXp.Begin(CropCatalog.OfPlant(piece.name), null, giant: false);
        }

        [HarmonyFinalizer]
        private static void Finalizer(bool __state) => FarmXp.End(__state);
    }
}
