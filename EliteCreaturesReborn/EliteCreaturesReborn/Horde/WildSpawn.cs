namespace EliteCreaturesReborn.Horde
{
    /// <summary>
    /// Tags the creatures that wake while a wild spawn is under way: the game's <c>SpawnSystem.Spawn</c> outside an event
    /// (<see cref="HordeSpawnPatch"/>), or a wild Splintering creature's split (<see cref="Patches.DeathPatch"/>). Both
    /// instantiate the creature inside the call, so its <c>Character.Awake</c> runs there, with its ZDO already made and
    /// owned here (the game reads it in the same Awake); the creature attach patch hands each waking creature to
    /// <see cref="Notice"/>. Begin returns what it replaced and End puts it back, so a call inside another cannot leave
    /// the flag on.
    /// </summary>
    internal static class WildSpawn
    {
        private static bool _wild;

        /// <summary>Starts a spawn whose creatures are wild (or not); returns the state to hand back to <see cref="End"/>.</summary>
        public static bool Begin(bool wild)
        {
            bool was = _wild;
            _wild = wild;
            return was;
        }

        public static void End(bool was) => _wild = was;

        /// <summary>A creature waking: tagged when a wild spawn is under way and this machine made it.</summary>
        public static void Notice(Character character)
        {
            if (!_wild)
            {
                return;
            }
            ZNetView? nview = character.m_nview;
            ZDO? zdo = nview != null ? nview.GetZDO() : null;
            if (zdo != null && nview!.IsOwner())
            {
                WildTag.Mark(zdo);
            }
        }
    }
}
