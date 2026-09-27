using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Row planting: from Row Of Three Level a placed crop comes with one more on each side, from Row Of Five Level two
    /// more. After Player.TryPlacePiece placed the crop the player aimed at (the game pays for it right after), the extra
    /// plants go on the ground along the row: across the player's view, snapped to the nearest world axis so rows line
    /// up, spaced twice the planter's grow radius plus 0.1 m. Each needs its own seed (the aimed crop's still owed) and a
    /// spot where it can grow (<see cref="PlantSpot"/>); a spot that fails is skipped. Holding the alternative place key
    /// plants one, and each player can turn rows off. Crops only; tree saplings are planted one at a time.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class RowPlanting
    {
        private const float Gap = 0.1f;

        [HarmonyPostfix]
        private static void Postfix(Player __instance, Piece piece, bool __result)
        {
            if (!__result || __instance != Player.m_localPlayer || !FarmSkill.Active || !FarmingSettings.RowPlanting.Value || piece == null)
                return;
            CropPlant crop = CropCatalog.OfPlant(piece.name);
            int width = Width(FarmSkill.Local());
            if (crop?.Piece == null || width <= 1 || AltHeld() || __instance.m_placementGhost == null)
                return;
            HookGuard.Run("Farming rows", () => Plant(__instance, crop, width));
        }

        /// <summary>How many plants a placement makes at this level: 1, 3 or 5.</summary>
        public static int Width(float level)
        {
            if (FarmSkill.Reached(FarmingPerkSettings.RowOfFiveLevel.Value, level))
                return 5;
            return FarmSkill.Reached(FarmingPerkSettings.RowOfThreeLevel.Value, level) ? 3 : 1;
        }

        private static bool AltHeld() => ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace");

        private static void Plant(Player player, CropPlant crop, int width)
        {
            Transform ghost = player.m_placementGhost.transform;
            float level = FarmSkill.Local();
            float spacing = 2f * PlantTraits.Radius(crop, level) + Gap;
            Vector3 axis = Axis(player);
            for (int step = 1; step <= width / 2; step++)
            {
                foreach (int side in new[] { 1, -1 })
                {
                    Vector3 spot = PlantSpot.Ground(ghost.position + axis * (side * step * spacing));
                    if (Sowing.CanPayAnother(player, crop.Piece, owed: 1) && PlantSpot.CanSow(crop, spot, level, null))
                        Sowing.Sow(player, crop.Piece, spot, ghost.rotation);
                }
            }
        }

        /// <summary>The row's direction: across the camera's view, snapped to the nearest world axis.</summary>
        private static Vector3 Axis(Player player)
        {
            Camera camera = Utils.GetMainCamera();
            Vector3 right = camera != null ? camera.transform.right : player.transform.right;
            return Mathf.Abs(right.x) >= Mathf.Abs(right.z) ? Vector3.right : Vector3.forward;
        }
    }
}
