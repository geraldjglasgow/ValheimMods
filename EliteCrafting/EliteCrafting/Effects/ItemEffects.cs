using System.Collections.Generic;
using EliteCrafting.Affixes;
using EliteCrafting.Config;
using EliteCrafting.Items;
using EliteCrafting.Rules;

namespace EliteCrafting.Effects
{
    /// <summary>One active affix on an equipped item: the item, its roll, the live definition and the item's slot.</summary>
    public readonly struct ActiveAffix
    {
        public ActiveAffix(ItemDrop.ItemData item, AffixRoll roll, AffixDef def, ItemSlot slot)
        {
            Item = item;
            Roll = roll;
            Def = def;
            Slot = slot;
        }

        public ItemDrop.ItemData Item { get; }
        public AffixRoll Roll { get; }
        public AffixDef Def { get; }
        public ItemSlot Slot { get; }

        /// <summary>Shortcut to <see cref="AffixDef.ChannelIndex"/>.</summary>
        public int Channel => Def.ChannelIndex;
    }

    /// <summary>
    /// Enumerates what counts for effects (effects-runtime.md section 2): only equipped items (weapons and tools in
    /// hand, shield, armor, cape, utility item; hidden hand items and trinkets never), only active affixes (defined and
    /// enabled; dormant and unreadable ones are skipped), only when the <c>Affix effects</c> switch is on. For event
    /// handlers (equip change, inventory change), never per frame: it walks the equipment slots and reads each item's
    /// cached state, filling caller-owned lists so a rebuild allocates nothing.
    /// </summary>
    public static class ItemEffects
    {
        /// <summary>The <c>Affix effects</c> master switch (synced).</summary>
        public static bool Enabled => ModSettings.AffixEffects == null || ModSettings.AffixEffects.Value;

        /// <summary>Fills <paramref name="into"/> with the equipped items of <paramref name="humanoid"/> that count.</summary>
        public static void EquippedItems(Humanoid? humanoid, List<ItemDrop.ItemData> into)
        {
            into.Clear();
            if (humanoid == null)
            {
                return;
            }
            AddIfPresent(into, humanoid.m_rightItem);
            AddIfPresent(into, humanoid.m_leftItem);
            AddIfPresent(into, humanoid.m_helmetItem);
            AddIfPresent(into, humanoid.m_chestItem);
            AddIfPresent(into, humanoid.m_legItem);
            AddIfPresent(into, humanoid.m_shoulderItem);
            AddIfPresent(into, humanoid.m_utilityItem);
        }

        /// <summary>
        /// Fills <paramref name="into"/> with every active affix on the local player's counted items. Empty when there
        /// is no local player (dedicated server) or the master switch is off.
        /// </summary>
        public static void CollectLocal(List<ActiveAffix> into, List<ItemDrop.ItemData> scratch)
        {
            into.Clear();
            if (!Enabled || Player.m_localPlayer == null)
            {
                return;
            }
            EquippedItems(Player.m_localPlayer, scratch);
            foreach (ItemDrop.ItemData item in scratch)
            {
                CollectItem(item, into);
            }
        }

        /// <summary>Adds the active affixes of one item (for item-local hooks, use <see cref="ItemState.Read"/> directly).</summary>
        public static void CollectItem(ItemDrop.ItemData item, List<ActiveAffix> into)
        {
            ItemState state = ItemState.Read(item);
            if (!state.HasAffixes)
            {
                return;
            }
            ItemSlot slot = ItemSlots.SlotOf(item);
            for (int i = 0; i < state.AffixCount; i++)
            {
                if (state.IsActiveAt(i))
                {
                    into.Add(new ActiveAffix(item, state.Affixes[i], state.DefinitionAt(i)!, slot));
                }
            }
        }

        /// <summary>Whether the item is one the local player has equipped (for <see cref="ItemStateCache.Written"/>).</summary>
        public static bool IsEquippedByLocalPlayer(ItemDrop.ItemData item)
        {
            Player? player = Player.m_localPlayer;
            return player != null && player.IsItemEquiped(item);
        }

        private static void AddIfPresent(List<ItemDrop.ItemData> into, ItemDrop.ItemData? item)
        {
            if (item != null && !into.Contains(item))
            {
                into.Add(item);
            }
        }
    }
}
