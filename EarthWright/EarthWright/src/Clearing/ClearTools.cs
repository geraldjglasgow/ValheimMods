using System;

namespace EarthWright.Clearing
{
    /// <summary>
    /// The best axe and pickaxe a player carries, for survival clearing: wood (trees, logs, stumps) needs something that
    /// chops, stone (rocks, ore) something that mines, each of a tool tier at least the object's own minimum. A broken
    /// tool does not count. Admin commands use <see cref="Unlimited"/>, which can do everything.
    /// </summary>
    public sealed class ClearTools
    {
        public int AxeTier = -1;
        public int PickaxeTier = -1;
        public int AxeWorldLevel;
        public int PickaxeWorldLevel;

        public static ClearTools Of(Player player)
        {
            ClearTools tools = new ClearTools();
            Inventory inventory = player != null ? player.GetInventory() : null;
            if (inventory == null)
                return tools;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
                tools.Consider(item);
            return tools;
        }

        public static ClearTools Unlimited()
        {
            int world = Game.m_worldLevel;
            return new ClearTools { AxeTier = 1000, PickaxeTier = 1000, AxeWorldLevel = world, PickaxeWorldLevel = world };
        }

        private void Consider(ItemDrop.ItemData item)
        {
            ItemDrop.ItemData.SharedData shared = item?.m_shared;
            if (shared == null || (shared.m_useDurability && item.m_durability <= 0f) || LockedByWorldLevel(item))
                return;
            if (shared.m_damages.m_chop > 0f && shared.m_toolTier > AxeTier)
            {
                AxeTier = shared.m_toolTier;
                AxeWorldLevel = item.m_worldLevel;
            }
            if (shared.m_damages.m_pickaxe > 0f && shared.m_toolTier > PickaxeTier)
            {
                PickaxeTier = shared.m_toolTier;
                PickaxeWorldLevel = item.m_worldLevel;
            }
        }

        /// <summary>The game's world-level rule: with locked tools, a tool made below the world level cannot mine or chop.</summary>
        private static bool LockedByWorldLevel(ItemDrop.ItemData item)
        {
            ZoneSystem zones = ZoneSystem.instance;
            return zones != null && item.m_worldLevel < Game.m_worldLevel && zones.GetGlobalKey(GlobalKeys.WorldLevelLockedTools);
        }

        /// <summary>Wood needs an axe, stone a pickaxe; shrubs and pickables need no tool.</summary>
        public static bool NeedsAxe(ClearCategory kind) => (kind & (ClearCategory.Trees | ClearCategory.Logs | ClearCategory.Stumps)) != 0;

        public static bool NeedsPickaxe(ClearCategory kind) => (kind & (ClearCategory.Rocks | ClearCategory.Ores)) != 0;

        /// <summary>The tier the player hits this kind with; -1 when the needed tool is missing.</summary>
        public int TierFor(ClearCategory kind)
        {
            if (NeedsAxe(kind))
                return AxeTier;
            if (NeedsPickaxe(kind))
                return PickaxeTier;
            return Math.Max(AxeTier, 0);
        }

        public int WorldLevelFor(ClearCategory kind) => NeedsPickaxe(kind) ? PickaxeWorldLevel : AxeTier >= 0 ? AxeWorldLevel : Game.m_worldLevel;

        /// <summary>The player carries a tool good enough for an object of this kind and minimum tier.</summary>
        public bool CanClear(ClearCategory kind, int minToolTier)
        {
            if ((kind & (ClearCategory.Pickables | ClearCategory.Debris)) != 0)
                return true;
            int tier = TierFor(kind);
            return tier >= 0 && tier >= minToolTier;
        }
    }
}
