using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Loot
{
    /// <summary>
    /// The level other mods' drops read for a creature this mod's stars keep at the game's level 1: its stars + 1, so a
    /// drop mod with a table per level (Epic Loot's) pays a five-star creature as level 6 and not as a plain one. Shown
    /// only after the game has built its own drop list, so the game's level bonus never comes on top of this mod's star
    /// loot, and only to the postfixes that roll the other mods' drops:
    /// <list type="bullet">
    /// <item>a corpse that drops the loot: the owner keeps the level on the corpse as the game sets it up
    /// (<see cref="Mark"/>), and it becomes the corpse's game level as the corpse spawns its loot and is removed
    /// (<see cref="Raise"/>). Not sooner: every machine draws a waking corpse's star looks from its game level.</item>
    /// <item>a creature that drops at once: its level is raised for the postfixes on CharacterDrop.OnDeath
    /// (<see cref="Show"/>) and put back after them (<see cref="Hide"/>).</item>
    /// </list>
    /// A creature with no stars of this mod, or that keeps the game's level, is left alone. Owner only: the game runs a
    /// death, and a corpse's loot, on the owner.
    /// </summary>
    internal static class LootLevel
    {
        private static readonly int Hash = TraitKeys.LootLevel.GetStableHashCode();

        private static Character? _shown;
        private static int _gameLevel;

        /// <summary>The level other mods' drops read: this mod's stars + 1, or the game's level when that is higher.</summary>
        public static int Of(Character creature)
        {
            EliteController? controller = creature.GetComponent<EliteController>();
            int stars = controller != null && controller.Ready ? controller.Traits.Stars : 0;
            return Mathf.Max(creature.GetLevel(), stars + 1);
        }

        /// <summary>OWNER, as the game sets up a dying creature's corpse: the level kept on it, when above the game's.</summary>
        public static void Mark(Ragdoll corpse, CharacterDrop? drop)
        {
            Character? creature = drop != null ? drop.m_character : null;
            ZDO? zdo = CorpseZdo(corpse);
            if (creature == null || zdo == null)
            {
                return;
            }
            int level = Of(creature);
            if (level > creature.GetLevel())
            {
                zdo.Set(Hash, level);
            }
        }

        /// <summary>CORPSE OWNER, as the corpse spawns its loot, before the other mods' postfixes read its game level.</summary>
        public static void Raise(Ragdoll corpse)
        {
            ZDO? zdo = CorpseZdo(corpse);
            int level = zdo != null ? zdo.GetInt(Hash) : 0;
            if (zdo != null && level > zdo.GetInt(ZDOVars.s_level, 1))
            {
                zdo.Set(ZDOVars.s_level, level);
            }
        }

        /// <summary>OWNER, after the game has dropped a creature's loot at once: its level raised for the postfixes.</summary>
        public static void Show(CharacterDrop drop)
        {
            Character creature = drop.m_character;
            if (creature == null || creature.IsPlayer())
            {
                return;
            }
            int level = Of(creature);
            if (level > creature.GetLevel())
            {
                _shown = creature;
                _gameLevel = creature.GetLevel();
                creature.m_level = level;
            }
        }

        /// <summary>After every postfix on CharacterDrop.OnDeath, also when one threw: the game's level back.</summary>
        public static void Hide()
        {
            if (_shown != null)
            {
                _shown.m_level = _gameLevel;
            }
            _shown = null;
        }

        private static ZDO? CorpseZdo(Ragdoll corpse) =>
            corpse.m_nview != null && corpse.m_nview.IsValid() && corpse.m_nview.IsOwner() ? corpse.m_nview.GetZDO() : null;
    }
}
