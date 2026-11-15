using System.Collections.Generic;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// A boss by its prefab: the game's bosses and any mod's boss prefab. Another mod can make an ordinary creature a boss
    /// on the spot - Epic Loot's bounty targets set <c>m_boss</c> on the creature, never its prefab - and such a stand-in is
    /// rolled, scaled and paid as a creature of its kind: its biome's stars and mutations, no boss table, aspects, boss
    /// trophies or kill count (Discord, 2026-11-13: a bounty boss took the boss table and lost Epic Loot's health). Every
    /// machine answers the same: the prefab is the object's. The game's own <c>IsBoss</c> still decides what is no one's
    /// prey or call (Devouring, Howling) and the boss's health bar.
    /// </summary>
    internal static class PrefabBoss
    {
        private static readonly Dictionary<string, bool> ByPrefab = new Dictionary<string, bool>();

        public static bool Is(Character creature) =>
            creature != null && creature.IsBoss() && PrefabIsBoss(Utils.GetPrefabName(creature.gameObject));

        // Kept per prefab once the scene knows it; without the scene (or the prefab) the creature's own flag stands.
        private static bool PrefabIsBoss(string prefab)
        {
            if (ByPrefab.TryGetValue(prefab, out bool boss))
            {
                return boss;
            }
            Character? template = ZNetScene.instance != null
                ? ZNetScene.instance.GetPrefab(prefab)?.GetComponent<Character>()
                : null;
            if (template == null)
            {
                return true;
            }
            ByPrefab[prefab] = template.m_boss;
            return template.m_boss;
        }
    }
}
