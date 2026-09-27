using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Scales Cooking experience raised inside an <see cref="XpScope"/>: the game's own amount times the Experience
    /// Multiplier, the dish's tier and, the first time the character makes the dish, the Discovery Multiplier. Every
    /// kitchen raise goes Player.RaiseSkill (status-effect modifiers) → Skills.RaiseSkill, so the prefix sits on the
    /// latter and only touches the local player's own skills.
    /// </summary>
    public static class XpScaling
    {
        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        private static class RaisePatch
        {
            [HarmonyPrefix]
            private static void Prefix(Skills __instance, Skills.SkillType skillType, ref float factor)
            {
                Player player = Player.m_localPlayer;
                if (skillType == CookLevel.Skill && XpScope.Active && player != null && __instance.m_player == player)
                    factor *= Scale(player);
            }
        }

        public static float Multiplier => Mathf.Max(0f, ExperienceSettings.Multiplier.Value);

        /// <summary>
        /// The dish's tier: its food value (health + stamina + eitr, or what an intermediate becomes) over the reference
        /// value, clamped to 1..maximum. Always 1 while Tier Scaling is off, and for an unknown dish.
        /// </summary>
        public static float Tier(ItemDrop.ItemData dish)
        {
            if (!ExperienceSettings.TierScaling.Value)
                return 1f;
            float reference = Mathf.Max(1f, ExperienceSettings.TierReferenceValue.Value);
            float maximum = Mathf.Max(1f, ExperienceSettings.TierMaximum.Value);
            return Mathf.Clamp(Kitchen.FoodValue(dish) / reference, 1f, maximum);
        }

        /// <summary>The tier of a dish by its prefab name.</summary>
        public static float Tier(string dishPrefab)
        {
            ItemDrop drop = Kitchen.ItemPrefab(dishPrefab);
            return Tier(drop == null ? null : drop.m_itemData);
        }

        /// <summary>Raises the local player's Cooking by an amount that is already scaled, bypassing any open scope.</summary>
        public static void RaiseScaled(Player player, float factor)
        {
            if (player == null || factor <= 0f)
                return;
            bool paused = XpScope.Pause();
            try
            {
                player.RaiseSkill(CookLevel.Skill, factor);
            }
            finally
            {
                XpScope.Resume(paused);
            }
        }

        /// <summary>
        /// The factor for the open scope. The discovery bonus covers one dish: a multi-craft of n earns n + (D - 1)
        /// dishes' worth, so a station take-off or a single craft is simply multiplied by D.
        /// </summary>
        private static float Scale(Player player)
        {
            float scale = Multiplier * Tier(XpScope.Dish);
            string dish = XpScope.TakeDiscoverable();
            if (dish != null && DishDiscovery.TryRecord(player, dish))
            {
                float discovery = Mathf.Max(1f, ExperienceSettings.DiscoveryMultiplier.Value);
                scale *= 1f + (discovery - 1f) / XpScope.Units;
            }
            return scale;
        }
    }
}
