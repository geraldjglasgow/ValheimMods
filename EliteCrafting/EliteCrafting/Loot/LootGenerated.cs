using System;
using EliteCrafting.Core;

namespace EliteCrafting.Loot
{
    /// <summary>
    /// The API's loot-generated listeners (api.md section 5): raised by <see cref="GearFactory.Build"/> after a
    /// pre-rolled magic item was made for a drop (a creature's loot or a world container), on the peer that rolls it,
    /// before it reaches the world. Each listener runs guarded on its own. Main thread only.
    /// </summary>
    internal static class LootGenerated
    {
        public static readonly Callbacks<Action<ItemDrop.ItemData>> Listeners =
            new Callbacks<Action<ItemDrop.ItemData>>("loot-generated listener");

        public static void Raise(ItemDrop.ItemData item)
        {
            foreach (Callbacks<Action<ItemDrop.ItemData>>.Entry listener in Listeners.Snapshot)
            {
                try
                {
                    listener.Callback(item);
                }
                catch (Exception e)
                {
                    Listeners.Failed(listener, e);
                }
            }
        }
    }
}
