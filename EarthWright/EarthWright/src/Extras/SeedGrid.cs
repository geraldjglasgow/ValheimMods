using System.Globalization;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Extras
{
    /// <summary>
    /// Seeds and saplings snap to a world grid while they are placed ("Seed Grid", switched with the Seed Grid Key while a
    /// seed or sapling is selected). The placement ghost is moved to the nearest grid point on the ground after the game
    /// placed it, and the game's cultivated-ground, dungeon, no-build and ward checks are redone for the new point, so
    /// what the ghost shows is what the click plants. Planting itself stays the game's (the plant is placed where the
    /// ghost stands), so it replicates as usual. The brush is not active for seeds, so the key is free for this then.
    /// </summary>
    public static class SeedGrid
    {
        private const string HudKey = "seedgrid";
        private static bool hudShown;
        private static float hudSpacing;

        /// <summary>The selected piece is a seed or sapling (it grows into something).</summary>
        public static Plant SelectedPlant()
        {
            Piece piece = LocalTool.SelectedPiece;
            return piece != null ? piece.GetComponent<Plant>() : null;
        }

        /// <summary>Per frame: the toggle key and the HUD line.</summary>
        internal static void Update()
        {
            Plant plant = GeneralSettings.Active && LocalTool.InTerrainTool ? SelectedPlant() : null;
            if (plant != null && !GameUiOpen() && Keys.Pressed(ExtrasSettings.SeedGridKey))
            {
                ExtrasSettings.SeedGrid.Value = !ExtrasSettings.SeedGrid.Value;
                Messages.Center(ExtrasSettings.SeedGrid.Value ? ExtrasWords.SeedGridOn : ExtrasWords.SeedGridOff);
            }
            UpdateHud(plant != null && ExtrasSettings.SeedGrid.Value ? plant : null);
        }

        /// <summary>A game window that owns the keyboard is open.</summary>
        private static bool GameUiOpen()
        {
            return InventoryGui.IsVisible() || Minimap.IsOpen() || global::Menu.IsVisible() || StoreGui.IsVisible() || Hud.InRadial();
        }

        /// <summary>Shows "Seed grid: 1 m" while the grid applies to the selected plant; the text is only rebuilt when it changes.</summary>
        private static void UpdateHud(Plant plant)
        {
            float spacing = plant != null ? Spacing(plant) : 0f;
            if (plant != null && (!hudShown || spacing != hudSpacing))
                HudText.Set(HudKey, ExtrasWords.SeedGridHud + " " + spacing.ToString("0.##", CultureInfo.InvariantCulture) + " m", 450);
            else if (plant == null && hudShown)
                HudText.Clear(HudKey);
            hudShown = plant != null;
            hudSpacing = spacing;
        }

        /// <summary>After the game placed the local player's ghost: snaps a seed or sapling to the grid.</summary>
        internal static void Apply(Player player, bool flash)
        {
            if (!ExtrasSettings.SeedGrid.Value || !GeneralSettings.Active)
                return;
            GameObject ghost = player.m_placementGhost;
            Plant plant = SelectedPlant();
            if (ghost == null || !ghost.activeSelf || plant == null)
                return;
            float spacing = Spacing(plant);
            Vector3 p = ghost.transform.position;
            Vector3 snapped = new Vector3(Mathf.Round(p.x / spacing) * spacing, p.y, Mathf.Round(p.z / spacing) * spacing);
            if (ZoneSystem.instance != null)
                snapped.y = ZoneSystem.instance.GetGroundHeight(snapped);
            ghost.transform.position = snapped;
            Recheck(player, plant.GetComponent<Piece>(), snapped, flash);
        }

        /// <summary>The grid spacing for this plant: the setting, or twice the plant's grow radius when the setting is 0.</summary>
        public static float Spacing(Plant plant)
        {
            float setting = ExtrasSettings.SeedGridSpacing.Value;
            float spacing = setting > 0f ? setting : 2f * plant.m_growRadius;
            return Mathf.Max(0.25f, spacing);
        }

        /// <summary>
        /// Judges the snapped point when the game's verdict was valid or only "needs cultivated ground" (every other
        /// refusal of the game stands): cultivated ground first, then the area checks.
        /// </summary>
        private static void Recheck(Player player, Piece piece, Vector3 point, bool flash)
        {
            Player.PlacementStatus status = player.m_placementStatus;
            if (piece == null || (status != Player.PlacementStatus.Valid && status != Player.PlacementStatus.NeedCultivated))
                return;
            Heightmap map = Heightmap.FindHeightmap(point);
            if (piece.m_cultivatedGroundOnly && (map == null || !map.IsCultivated(point)))
                GhostStatus.Set(player, Player.PlacementStatus.NeedCultivated);
            else
                GhostStatus.Set(player, GhostStatus.Checked(player, piece, point, flash));
        }
    }
}
