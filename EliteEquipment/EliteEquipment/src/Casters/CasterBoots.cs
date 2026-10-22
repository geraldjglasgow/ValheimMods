using EliteEquipment.Boots;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Every set's trousers split like every leggings while EliteEquipment's <c>1. Boots / Separate Boots</c> is on (user,
    /// 2026-10-11: "all equipment made works with EliteEquipment, so the boots and pants need to be separated"): each set's
    /// trousers are registered with its boots as a pair that brings its own parts (<see cref="BootSets.AddOwn"/>): the boots,
    /// named by the word <c>ee_boots_seer&lt;tier&gt;</c>, are the trousers' own <c>boots</c> mesh, with the set's workshop
    /// icons and the trousers' paint cut where the boots begin. The boots system then makes the boots item
    /// (<c>EE_Boots_Seer&lt;Tier&gt;</c>, PackPanel's feet slot takes the prefix), hides them on the trousers, splits stats,
    /// weight and recipe 80/20, and draws the worn pair. Without the split the trousers keep them. The trousers' eitr follows the split here
    /// (<see cref="EitrShare"/>).
    /// </summary>
    public static class CasterBoots
    {
        private const float BootsShare = 0.2f;

        private static bool registered;

        /// <summary>
        /// Joins the boots once, from the first item database the game wakes, just before the casters add the trousers there
        /// (their patches run ahead of the boots', which then find the trousers).
        /// </summary>
        public static void Register()
        {
            if (registered)
                return;
            registered = true;
            foreach (RobeSet set in RobeSets.All)
                Register(set);
        }

        private static void Register(RobeSet set)
        {
            string bundle = set.Trousers.Bundle;
            string asset = "ee_seer_" + set.Tier;
            BootSets.AddOwn(set.BootsKey, set.Trousers.Prefab,
                CasterBundles.Sprite(bundle, asset + "_boots_icon"),
                CasterBundles.Sprite(bundle, asset + "_trousers_split_icon"),
                CasterBundles.Texture(bundle, asset + "_trousers_legs_split"),
                CasterBundles.Texture(bundle, asset + "_trousers_legs_split_bump"));
        }

        /// <summary>Separate Boots is on (the server's value once synced).</summary>
        public static bool SplitOn => BootsSettings.On;

        /// <summary>The share of the trousers' eitr the trousers keep: four fifths while split, all otherwise.</summary>
        public static float EitrShare => SplitOn ? 1f - BootsShare : 1f;

        /// <summary>The eitr worn seer's boots give: the fifth their set's trousers gave up.</summary>
        public static float BootsEitr(Humanoid humanoid)
        {
            if (!SplitOn || humanoid == null)
                return 0f;
            foreach (ItemDrop.ItemData item in humanoid.GetInventory().GetAllItems())
            {
                RobeSet set = item.m_equipped ? SetOfBoots(item.m_shared.m_name) : null;
                if (set != null)
                    return set.Trousers.Eitr * BootsShare;
            }
            return 0f;
        }

        /// <summary>The set whose boots carry this name token (EliteEquipment's <c>$ee_boots_&lt;key&gt;</c>), or null.</summary>
        private static RobeSet SetOfBoots(string token)
        {
            foreach (RobeSet set in RobeSets.All)
            {
                if (token == "$ee_boots_" + set.BootsKey.ToLowerInvariant())
                    return set;
            }
            return null;
        }
    }
}
