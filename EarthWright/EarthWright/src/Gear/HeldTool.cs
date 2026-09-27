using EarthWright.Core;

namespace EarthWright.Gear
{
    /// <summary>
    /// What the local player has in the right hand, refreshed once per frame: whether it is a terrain tool (hoe,
    /// cultivator) and its level. The movement patches run every physics step and the
    /// brush asks every frame, so they read this instead of looking the item's prefab name up each time.
    /// </summary>
    public static class HeldTool
    {
        private static ItemDrop.ItemData lastItem;

        /// <summary>The local player holds a terrain tool (build mode and menu state do not matter).</summary>
        public static bool IsTerrainTool { get; private set; }

        /// <summary>The held terrain tool's upgrade level, 0 when none is held.</summary>
        public static int Level => IsTerrainTool && lastItem != null ? lastItem.m_quality : 0;

        /// <summary>A terrain tool is held and EarthWright is on for this player.</summary>
        public static bool Active => IsTerrainTool && GeneralSettings.Active;

        internal static void Update()
        {
            Player player = Player.m_localPlayer;
            ItemDrop.ItemData item = player != null ? player.GetRightItem() : null;
            if (ReferenceEquals(item, lastItem))
                return;
            lastItem = item;
            IsTerrainTool = IsTool(item);
        }

        /// <summary>The item is a terrain tool by its prefab name.</summary>
        public static bool IsTool(ItemDrop.ItemData item)
        {
            return item != null && item.m_dropPrefab != null && LocalTool.IsToolName(item.m_dropPrefab.name);
        }
    }
}
