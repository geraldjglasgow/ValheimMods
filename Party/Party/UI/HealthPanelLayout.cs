namespace Party.UI
{
    /// <summary>Shared row-height math between the panel (row spacing) and a row (bar positions).</summary>
    public static class HealthPanelLayout
    {
        public static float RowHeight()
        {
            float height = PartyConfig.FontSize.Value + 6f + PartyConfig.BarHeight.Value + 4f;
            height += PartyConfig.BarHeight.Value * 0.6f + 4f;
            height += PartyConfig.BarHeight.Value * 0.6f + 4f;
            return height;
        }
    }
}
