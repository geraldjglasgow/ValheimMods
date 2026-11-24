using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Pages from creatures (features/casters.md, "The spellbook", "Where pages come from"): a creature a player killed
    /// (the attacker of its last hit) drops a page 5% of the time with two stars or more (the game's level, or Elite
    /// Creatures Reborn's own stars from the key it publishes in the creature's ZDO when higher), 0.5% of the time
    /// otherwise; the page from <see cref="PageDrops"/>. Never from a boss (each drops its own, <see cref="BossPages"/>), a
    /// tame or a player. Added to the game's drop list (<c>CharacterDrop.GenerateDropList</c>, made once per death on the
    /// creature's owner: dropped at once, or kept in the ragdoll's ZDO and dropped when it goes), so the page drops with the
    /// game's own loot for every player. Only while the switch is on.
    /// </summary>
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    public static class CreaturePages
    {
        private const float EliteChance = 0.05f;
        private const float AnyChance = 0.005f;
        private const int EliteStars = 2;
        /// <summary>Elite Creatures Reborn's star count in the creature's ZDO (its <c>TraitKeys.Stars</c>), read only.</summary>
        private const string RebornStars = "ecr_stars";

        [HarmonyPostfix]
        private static void Postfix(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result)
        {
            if (CastersSettings.On && __result != null)
                CasterSafe.Run("creature pages", () => Add(__instance.m_character, __result));
        }

        private static void Add(Character creature, List<KeyValuePair<GameObject, int>> drops)
        {
            if (!Counts(creature) || !PageDrops.Roll(Stars(creature) >= EliteStars ? EliteChance : AnyChance))
                return;
            GameObject page = PageDrops.Pick(creature.transform.position);
            if (page != null)
                drops.Add(new KeyValuePair<GameObject, int>(page, 1));
        }

        private static bool Counts(Character creature) =>
            creature != null && !creature.IsPlayer() && !creature.IsBoss() && !creature.IsTamed()
            && creature.m_lastHit?.GetAttacker() is Player;

        private static int Stars(Character creature)
        {
            ZDO zdo = creature.m_nview != null ? creature.m_nview.GetZDO() : null;
            return Mathf.Max(creature.GetLevel() - 1, zdo != null ? zdo.GetInt(RebornStars) : 0);
        }
    }
}
