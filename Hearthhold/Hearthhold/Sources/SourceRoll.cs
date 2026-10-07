namespace Hearthhold
{
    /// <summary>
    /// One spawn's star roll for a source (wild picks, crops, honey and sap, meat), made on the spawning object's owner:
    /// the actor's skill level, what the source brings (a creature's stars) and the profession floor at that level. Only
    /// the source's own items roll (a forage pick's forage items, a crop's crops, a kill's meat); anything else spawned in
    /// the same scope (seeds, trophies, hides) stays plain. <see cref="Open"/> starts a <see cref="SpawnStars"/> scope with
    /// it; the hook keeps the returned <see cref="Scope"/> as its state and closes it in a finalizer, which Harmony runs
    /// after every postfix, so items another mod spawns in its postfix of the same call (GrindstoneSkills' extra honey,
    /// a giant crop's extra crop) roll too.
    /// </summary>
    public sealed class SourceRoll
    {
        private readonly StarSource source;
        private readonly float level;
        private readonly float bonus;
        private readonly int floor;

        private SourceRoll(StarSource source, float level, float bonus)
        {
            this.source = source;
            this.level = StarOdds.Sane(level);
            this.bonus = StarOdds.Sane(bonus);
            floor = Professions.Floor(source, this.level);
        }

        /// <summary>A scope a hook opened, or none; closing one that was never opened does nothing.</summary>
        public struct Scope
        {
            public bool Opened;
            public SpawnStars.Roller Previous;

            public void Close()
            {
                if (Opened)
                    SpawnStars.Close(Previous);
                Opened = false;
            }
        }

        /// <summary>Opens a scope in which every new item of <paramref name="source"/> rolls at this level and bonus.</summary>
        public static Scope Open(StarSource source, float level, float bonus) =>
            new Scope { Opened = true, Previous = SpawnStars.Open(new SourceRoll(source, level, bonus).Roll) };

        /// <summary>
        /// On the owner, as the game's RPC from <paramref name="sender"/> arrives: a scope at the level the sender marked
        /// on this object just before (<see cref="Marks"/>), or at level 0 without a mark (a client without Hearthhold).
        /// </summary>
        public static Scope FromMark(ZNetView nview, long sender, StarSource source)
        {
            float level = Marks.TryTake(nview, sender, out Marks.Mark mark) ? mark.Level : 0f;
            return Open(source, level, 0f);
        }

        private int Roll(ItemDrop.ItemData item) => IsOwnItem(item) ? StarOdds.Roll(level, bonus, floor) : 0;

        private bool IsOwnItem(ItemDrop.ItemData item)
        {
            string name = item?.m_dropPrefab != null ? item.m_dropPrefab.name : null;
            switch (source)
            {
                case StarSource.Forage: return Sources.IsForageItem(name);
                case StarSource.Crop: return Sources.IsCropItem(name);
                case StarSource.Meat: return Sources.IsMeatItem(name);
                default: return false;
            }
        }
    }
}
