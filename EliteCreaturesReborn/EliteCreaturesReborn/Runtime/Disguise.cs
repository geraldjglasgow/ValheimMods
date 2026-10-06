namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// A creature another mod disguises as something else while it sleeps: Elite Creatures Pack's crypt mimic, a chest
    /// until it wakes. That mod sets the ZDO bool <see cref="Key"/> on such a creature. While it is set and the creature
    /// sleeps (the game's own sleep state, which reaches every client), nothing of this mod may show on it - no size,
    /// star look, mutation effect or decorated name - so the disguise holds; its numbers still apply at once. Only the key
    /// name is shared: neither mod references the other, and without Elite Creatures Pack the key is never set.
    /// </summary>
    internal static class Disguise
    {
        public const string Key = "ecp_disguised";

        private static readonly int KeyHash = Key.GetStableHashCode();

        public static bool Holds(Character character)
        {
            ZNetView? nview = character != null ? character.m_nview : null;
            ZDO? zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
            return zdo != null && zdo.GetBool(KeyHash) && character!.GetBaseAI() is MonsterAI ai && ai.IsSleeping();
        }
    }
}
