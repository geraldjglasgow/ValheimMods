using System.Runtime.CompilerServices;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// A loaded crossbow stays loaded while it is put away. The game forgets the loaded
    /// weapon whenever one is unequipped - a swap, a swim, sitting down - and reloads on the next draw. This remembers
    /// each weapon that was loaded when this machine's player put it away, and hands the loaded state back when the
    /// same weapon is drawn again; firing still empties it as before. Only the player's owner holds the state and it
    /// writes the game's own loaded flag (which the game replicates), so it works the same on a dedicated server.
    /// Memory only: weakly held, so a dropped or destroyed weapon is simply forgotten, and a relog reloads as before.
    /// </summary>
    internal static class LoadedWeapons
    {
        private static readonly ConditionalWeakTable<ItemDrop.ItemData, object> Kept =
            new ConditionalWeakTable<ItemDrop.ItemData, object>();
        private static readonly object Mark = new object();

        /// <summary>Before the game unequips <paramref name="item"/>: keep it if it is this player's loaded weapon.</summary>
        public static void Unequipping(Humanoid humanoid, ItemDrop.ItemData item)
        {
            if (humanoid is Player player && IsOwnLoaded(player, item))
            {
                Kept.Remove(item);
                Kept.Add(item, Mark);
            }
        }

        /// <summary>After the game equipped <paramref name="item"/>: if it was put away loaded, it is loaded again.</summary>
        public static void Equipped(Humanoid humanoid, ItemDrop.ItemData item)
        {
            if (!(humanoid is Player player) || !Kept.TryGetValue(item, out _))
            {
                return;
            }
            Kept.Remove(item);
            if (player.m_nview.IsOwner() && player.IsItemEquiped(item))
            {
                player.SetWeaponLoaded(item);
            }
        }

        private static bool IsOwnLoaded(Player player, ItemDrop.ItemData item) =>
            item.m_shared.m_attack.m_requiresReload && ReferenceEquals(player.m_weaponLoaded, item)
            && player.m_nview.IsValid() && player.m_nview.IsOwner();
    }
}
