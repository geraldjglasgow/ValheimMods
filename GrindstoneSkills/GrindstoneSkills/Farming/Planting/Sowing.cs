using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Plants one more of a piece for the local player, for row planting and auto-replant: pays its seed first (recording
    /// the seed's stars, as <see cref="SeedStars"/> does for the game's own placement), then places it with the game's
    /// Player.PlacePiece (no attack animation), so it gets a creator, a planter (<see cref="PlantPlacing"/>) and a ZDO
    /// every client sees. Costs no stamina or tool wear, and raises planting experience as a placement does.
    /// </summary>
    public static class Sowing
    {
        public static bool Active { get; private set; }

        /// <summary>Whether placing costs nothing: the player's no-cost mode or the world's free-build key for the piece.</summary>
        public static bool IsFree(Player player, Piece piece) =>
            player.m_noPlacementCost || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()));

        /// <summary>Whether the player can pay for one more of the piece while <paramref name="owed"/> placed ones are still unpaid.</summary>
        public static bool CanPayAnother(Player player, Piece piece, int owed)
        {
            if (IsFree(player, piece))
                return true;
            foreach (Piece.Requirement requirement in piece.m_resources)
            {
                if (requirement?.m_resItem == null)
                    continue;
                int need = requirement.GetAmount(0) * (owed + 1);
                if (player.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name) < need)
                    return false;
            }
            return true;
        }

        /// <summary>Pays, places and credits one plant. Returns the new plant, or null when none was made.</summary>
        public static Plant Sow(Player player, Piece piece, Vector3 position, Quaternion rotation)
        {
            int stars = Pay(player, piece);
            Active = true;
            try
            {
                player.PlacePiece(piece, position, rotation, doAttack: false);
            }
            finally
            {
                Active = false;
            }
            Plant plant = PlantPlacing.TakeSown();
            ZDO zdo = PlantKeys.Of(plant);
            if (zdo != null)
                PlantKeys.WriteSeed(zdo, stars);
            FarmXp.RaiseScaled(player, FarmXp.Multiplier * FarmXp.Tier(CropCatalog.OfPlant(piece.name)));
            return plant;
        }

        /// <summary>Takes the piece's cost and returns the stars of the seed taken; 0 when free.</summary>
        private static int Pay(Player player, Piece piece)
        {
            if (IsFree(player, piece))
                return 0;
            bool record = !CraftRecord.Recording;
            if (record)
                CraftRecord.Start();
            try
            {
                player.ConsumeResources(piece.m_resources, 0);
                return record ? SeedStars.Recorded() : 0;
            }
            finally
            {
                if (record)
                    CraftRecord.Clear();
            }
        }
    }
}
