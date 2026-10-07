using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Auto-replant: from Auto Replant Level, picking a crop puts the plant it grew from back in its spot, paid with a seed
    /// from the picker's inventory. Nothing happens without a seed, or where the plant could not grow
    /// (<see cref="PlantSpot"/>; the crop being picked is not in its way). Each player can turn it off. Runs on the
    /// picker's client after the pick (<see cref="CropPick"/>).
    /// </summary>
    public static class AutoReplant
    {
        public static void After(Player player, Pickable pickable, CropPlant crop)
        {
            float level = FarmSkill.Level(player);
            if (!FarmingSettings.AutoReplant.Value || !FarmSkill.Reached(FarmingPerkSettings.AutoReplantLevel.Value, level))
                return;
            if (crop.Piece == null || crop.Seed == null || !Sowing.CanPayAnother(player, crop.Piece, owed: 0))
                return;
            Vector3 spot = PlantSpot.Ground(pickable.transform.position);
            if (!PlantSpot.CanSow(crop, spot, level, pickable.gameObject))
                return;
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Sowing.Sow(player, crop.Piece, spot, rotation);
        }
    }
}
