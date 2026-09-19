namespace Party.UI
{
    /// <summary>Shared row-layout math between the panel (heights, spacing) and a row (bar positions).</summary>
    public static class HealthPanelLayout
    {
        public const float BarGap = 4f;

        public static float NameHeight() => PartyConfig.FontSize.Value + 6f;

        public static float SubBarHeight() => PartyConfig.BarHeight.Value * 0.6f;

        /// <summary>Only the bars actually shown take space; the panel rebuilds its rows when the toggles change.</summary>
        public static float RowHeight()
        {
            float height = NameHeight() + PartyConfig.BarHeight.Value + BarGap;
            if (PartyConfig.ShowStamina.Value)
                height += SubBarHeight() + BarGap;
            if (PartyConfig.ShowEitr.Value)
                height += SubBarHeight() + BarGap;
            return height;
        }
    }
}
