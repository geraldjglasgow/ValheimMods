using System;
using OpenKeep.Core;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// How a Mímir's Chest orders its stacks when it packs (<see cref="MimirPack"/>): by name (the item's name in the
    /// game's language, then most stars first), by type (the quick filter categories in their button order, then name)
    /// or by stars (most first, then name). Each chest remembers its own in its ZDO (<see cref="Key"/>, by type until
    /// someone picks), so it reaches every player with the chest; the player with the chest open owns it and writes it.
    /// Picked with the buttons in the title row (<see cref="MimirSortButtons"/>).
    /// </summary>
    public static class MimirSortMode
    {
        public enum Mode
        {
            Name,
            Type,
            Stars,
        }

        public const string Key = "ok_mimir_sort";
        private static readonly int KeyHash = Key.GetStableHashCode();

        public static Mode Of(Container container)
        {
            ZDO zdo = container != null && container.m_nview != null && container.m_nview.IsValid() ? container.m_nview.GetZDO() : null;
            int value = zdo != null ? zdo.GetInt(KeyHash, (int)Mode.Type) : (int)Mode.Type;
            return Enum.IsDefined(typeof(Mode), value) ? (Mode)value : Mode.Type;
        }

        /// <summary>Picks a chest's order and sorts it now; only on its owner.</summary>
        public static void Pick(Container container, Mode mode)
        {
            if (container == null || container.m_nview == null || !container.m_nview.IsValid() || !container.m_nview.IsOwner())
                return;
            if (Of(container) != mode)
                container.m_nview.GetZDO().Set(KeyHash, (int)mode);
            MimirSearch.Repack();
        }

        public static Comparison<ItemDrop.ItemData> Comparer(Mode mode)
        {
            switch (mode)
            {
                case Mode.Name: return (a, b) => Then(ByName(a, b), ByStars(a, b));
                case Mode.Stars: return (a, b) => Then(ByStars(a, b), ByName(a, b));
                default: return (a, b) => Then(ByType(a, b), ByName(a, b));
            }
        }

        private static int Then(int first, int second) => first != 0 ? first : second;

        private static int ByName(ItemDrop.ItemData a, ItemDrop.ItemData b) =>
            string.Compare(ItemNames.DisplayName(a), ItemNames.DisplayName(b), StringComparison.CurrentCultureIgnoreCase);

        private static int ByStars(ItemDrop.ItemData a, ItemDrop.ItemData b) => Stars(b).CompareTo(Stars(a));

        private static int Stars(ItemDrop.ItemData item) => MimirFilters.IsStarred(item) ? item.m_quality - 1 : 0;

        private static int ByType(ItemDrop.ItemData a, ItemDrop.ItemData b) =>
            Array.IndexOf(MimirFilters.Buttons, MimirFilters.Of(a)).CompareTo(Array.IndexOf(MimirFilters.Buttons, MimirFilters.Of(b)));
    }
}
