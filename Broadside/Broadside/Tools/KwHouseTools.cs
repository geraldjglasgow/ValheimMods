namespace Broadside
{
    /// <summary>
    /// The Cabin, Doorway and Window tools' clicks and previews handed on from the other tools' (<see cref="KwTools"/>,
    /// <see cref="KwPreview"/>) to <see cref="KwCabinTool"/>, <see cref="KwDoorwayTool"/> and <see cref="KwWindowTool"/>.
    /// </summary>
    public static class KwHouseTools
    {
        /// <summary>A click with the Cabin, Doorway or Window tool taken; false for any other tool.</summary>
        public static bool Click(Player player, string tool)
        {
            if (tool == KwDoorwayTool.Tool)
                KwDoorwayTool.Click(player);
            else if (tool == KwWindowTool.Tool)
                KwWindowTool.Click(player);
            else if (tool == KwCabinTool.Tool)
                KwCabinTool.Click(player);
            else
                return false;
            return true;
        }

        /// <summary>The Cabin, Doorway or Window tool's preview shown (or hidden); false for any other tool.</summary>
        public static bool Preview(string? tool)
        {
            if (tool == KwDoorwayTool.Tool || tool == KwWindowTool.Tool)
                KwOpeningHover.Frame(Player.m_localPlayer, tool);
            else if (tool == KwCabinTool.Tool)
                KwCabinTool.Preview();
            else
                return false;
            return true;
        }
    }
}
