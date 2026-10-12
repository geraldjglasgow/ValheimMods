using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Bows' milestone, Recover (section 43): at Bows level 50 a bow shot has a 25% chance to use no arrow. Runs on the
    /// shooter's own client, which owns their character and runs their attacks. Read from the game code 2026-10-11:
    /// Attack.OnAttackTrigger calls Attack.UseAmmo once per shot, and UseAmmo takes the arrow from the shooter's inventory
    /// with Inventory.RemoveItem(item, 1); ammunition of the Consumable kind is eaten through Player.ConsumeItem
    /// (Inventory.RemoveOneItem) instead and is never spared. While a lucky shot's UseAmmo runs, the first such removal is
    /// skipped, so the arrow stays in its stack and slot and the shot still flies with it, as the game's own. The roll is
    /// made only for the local player's own attack with a weapon that trains Bows. Off, or below level 50, every shot takes
    /// its arrow.
    /// </summary>
    public static class BowRecover
    {
        /// <summary>The chance a bow shot uses no arrow.</summary>
        private const float FreeShotChance = 0.25f;

        /// <summary>A lucky shot's UseAmmo is running and has not taken its arrow yet.</summary>
        private static bool sparing;

        /// <summary>The perk's tip on the Bows page.</summary>
        public static string Tip => $"Each bow shot has a {SkillPage.Percent(FreeShotChance)} chance to use no arrow: it stays in your inventory.";

        [HarmonyPatch(typeof(Attack), nameof(Attack.UseAmmo))]
        private static class Shot
        {
            [HarmonyPrefix]
            private static void Prefix(Attack __instance)
            {
                if (ReferenceEquals(__instance.m_character, Player.m_localPlayer))
                    sparing = HookGuard.Run("bow recover", static attack => Lucky(attack), __instance, false);
            }

            [HarmonyFinalizer]
            private static void Finalizer() => sparing = false;
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new[] { typeof(ItemDrop.ItemData), typeof(int) })]
        private static class Taken
        {
            [HarmonyPrefix]
            private static bool Prefix(ref bool __result)
            {
                if (!sparing)
                    return true;
                sparing = false;
                __result = true;
                return false;
            }
        }

        private static bool Lucky(Attack attack) =>
            RangedMastery.Trains(attack.m_weapon, Skills.SkillType.Bows) && RangedMastery.Active(Skills.SkillType.Bows)
            && Random.value < FreeShotChance;
    }
}
