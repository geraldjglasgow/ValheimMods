using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// Stars for items the game is about to create in the world. Every way the game spawns a pick, a kill's drops, honey,
    /// sap or a station's product instantiates the item prefab, and ItemDrop.Awake runs inside that Instantiate on the
    /// machine doing it (always the spawning object's owner); the item saves itself to its ZDO in Start, a frame later.
    /// A feature opens a scope around the game's spawning call (<see cref="Open"/> to <see cref="Close"/>) with a roller
    /// that decides each new star item's stars; the one postfix on ItemDrop.Awake asks it and sets them at once
    /// (<see cref="Stars.SetOnDrop"/>). Outside a scope every waking item costs one null test. Scopes nest: closing
    /// restores the outer one.
    /// </summary>
    public static class SpawnStars
    {
        /// <summary>Decides the stars of one new star item from its data (its prefab is m_dropPrefab); 0 keeps it plain.</summary>
        public delegate int Roller(ItemDrop.ItemData item);

        private static Roller current;

        /// <summary>Opens a scope; hand the result to <see cref="Close"/> in a finalizer.</summary>
        public static Roller Open(Roller roller)
        {
            Roller previous = current;
            current = roller;
            return previous;
        }

        public static void Close(Roller previous) => current = previous;

        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
        private static class Woke
        {
            [HarmonyPostfix]
            private static void Postfix(ItemDrop __instance)
            {
                if (current != null)
                    HookGuard.Run("spawn stars", static item => Apply(item), __instance);
            }
        }

        private static void Apply(ItemDrop item)
        {
            if (item.m_nview == null || !item.m_nview.IsValid() || !item.m_nview.IsOwner() || !Stars.IsStarItem(item.m_itemData))
                return;
            int stars = current(item.m_itemData);
            if (stars > 0)
                Stars.SetOnDrop(item, stars);
        }
    }
}
