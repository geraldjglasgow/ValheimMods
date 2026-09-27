using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Scales the game's own Farming experience: 1 per placement from the cultivator (Player.UpdatePlacement) and 1 per
    /// crop picked (Pickable.Interact), both raised on the player's own client. While a scope is open (planting with a
    /// plant selected, see <see cref="PlantingScope"/>; picking a crop, <see cref="CropPick"/>) the raise is multiplied
    /// by Experience Multiplier and the crop's tier, and a pick also by the giant and discovery bonuses. Every raise goes
    /// Player.RaiseSkill → Skills.RaiseSkill, so the prefix sits on the latter and only touches the local player.
    /// </summary>
    public static class FarmXp
    {
        private sealed class Scope
        {
            public CropPlant Crop;
            public string Discoverable;
            public bool Giant;
        }

        // One instance, reused: planting opens a scope every frame the placement ghost shows a plant.
        private static readonly Scope current = new Scope();
        private static Scope scope;

        /// <summary>Opens a scope for this crop (null: tier 1). Returns false when one is already open.</summary>
        public static bool Begin(CropPlant crop, string discoverablePickable, bool giant)
        {
            if (scope != null)
                return false;
            current.Crop = crop;
            current.Discoverable = discoverablePickable;
            current.Giant = giant;
            scope = current;
            return true;
        }

        /// <summary>Closes the scope when <paramref name="opened"/> says the caller's Begin opened it.</summary>
        public static void End(bool opened)
        {
            if (opened)
                scope = null;
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        private static class RaisePatch
        {
            [HarmonyPrefix]
            private static void Prefix(Skills __instance, Skills.SkillType skillType, ref float factor)
            {
                Player player = Player.m_localPlayer;
                if (skillType == FarmSkill.Skill && scope != null && player != null && __instance.m_player == player && FarmSkill.Active)
                    factor *= Scale(player, scope);
            }
        }

        public static float Multiplier => Mathf.Max(0f, FarmingExperienceSettings.Multiplier.Value);

        /// <summary>The crop's tier: its value over the reference value, clamped to 1..maximum; 1 with Tier Scaling off.</summary>
        public static float Tier(CropPlant crop)
        {
            if (!FarmingExperienceSettings.TierScaling.Value)
                return 1f;
            float reference = Mathf.Max(1f, FarmingExperienceSettings.TierReferenceValue.Value);
            float maximum = Mathf.Max(1f, FarmingExperienceSettings.TierMaximum.Value);
            return Mathf.Clamp(CropValues.Of(crop) / reference, 1f, maximum);
        }

        /// <summary>Raises the local player's Farming by an amount that is already scaled, bypassing any open scope.</summary>
        public static void RaiseScaled(Player player, float amount)
        {
            if (player == null || amount <= 0f)
                return;
            Scope open = scope;
            scope = null;
            try
            {
                player.RaiseSkill(FarmSkill.Skill, amount);
            }
            finally
            {
                scope = open;
            }
        }

        private static float Scale(Player player, Scope open)
        {
            float scale = Multiplier * Tier(open.Crop);
            if (open.Giant)
                scale *= Mathf.Max(1f, FarmingExperienceSettings.GiantMultiplier.Value);
            string kind = open.Discoverable;
            open.Discoverable = null;
            if (kind != null && FarmDiscovery.TryRecord(player, kind))
                scale *= Mathf.Max(1f, FarmingExperienceSettings.DiscoveryMultiplier.Value);
            return scale;
        }
    }
}
