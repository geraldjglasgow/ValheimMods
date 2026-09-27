namespace GrindstoneSkills
{
    /// <summary>
    /// Where the trash filter is set and shown: kitchens with a valid ZNetView, while the server's Trash Filter
    /// setting is on. A cooking station with an add-food switch (the oven) takes food through that Switch child, and
    /// its own collider shows no hover text and ignores Use, so its filter lives on the switch; a plain cooking
    /// station has no switch and takes it directly. The fuel switch is left alone. Each lookup returns the kitchen's
    /// ZNetView, or null where the filter does not apply.
    /// </summary>
    public static class FilterStations
    {
        public static bool Enabled => KitchenSettings.TrashFilter != null && KitchenSettings.TrashFilter.Value;

        /// <summary>A kitchen cooking station without an add-food switch.</summary>
        public static ZNetView Of(CookingStation station)
        {
            if (!Enabled || !Kitchen.IsKitchen(station) || station.m_addFoodSwitch != null)
                return null;
            return Valid(station.m_nview);
        }

        /// <summary>A kitchen crafting station (cauldron, mead cauldron, prep table).</summary>
        public static ZNetView Of(CraftingStation station) => Enabled && Kitchen.IsKitchen(station) ? Valid(station.m_nview) : null;

        /// <summary>The kitchen cooking station whose add-food switch this is.</summary>
        public static ZNetView Of(Switch sw)
        {
            if (!Enabled || sw == null)
                return null;
            CookingStation station = sw.GetComponentInParent<CookingStation>();
            if (!Kitchen.IsKitchen(station) || station.m_addFoodSwitch != sw)
                return null;
            return Valid(station.m_nview);
        }

        private static ZNetView Valid(ZNetView nview) => nview != null && nview.IsValid() ? nview : null;
    }
}
