using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The pages of the game's ranged skills in the info pane: the damage a shot rolls (<see cref="WeaponLines"/>), a
    /// bow's draw time and draw stamina (Humanoid.GetAttackDrawPercentage, ItemData.GetDrawStaminaDrain) and a
    /// crossbow's reload time (ItemData.GetWeaponLoadingTime) at the player's level, with the held weapon's own seconds.
    /// </summary>
    public static class RangedPages
    {
        /// <summary>The cut in the time to a full draw at level 100: the game draws in a fifth of the bow's time.</summary>
        private const float DrawCut = 0.8f;

        /// <summary>The cut in a crossbow's reload time at level 100: the game reloads in half the time.</summary>
        private const float ReloadCut = 0.5f;

        public static void Bows(SkillPage page)
        {
            page.About = "Bows draw faster and hit harder. Trained by hitting creatures with arrows.";
            WeaponLines.Damage(page, "bow and arrow", " At full draw; a partial draw scales it down.");
            string bow = HeldTime(page, a => a.m_bowDraw ? a.m_drawDurationMin : 0f, DrawCut);
            page.Line($"Draw time -{SkillPage.Percent(DrawCut * page.Factor)}", "Draw time",
                $"Time to a full draw: the shot that hits hardest, flies fastest and strays least. At 100: a fifth of the bow's own.{bow}");
            WeaponLines.Cost(page, "Draw stamina",
                "Drained while you hold the draw, half as fast once fully drawn. Any cost of the shot itself drops the same.");
        }

        public static void Crossbows(SkillPage page)
        {
            page.About = "Crossbows reload faster and hit harder. Trained by hitting creatures with bolts.";
            WeaponLines.Damage(page, "crossbow and bolt", "");
            string crossbow = HeldTime(page, a => a.m_requiresReload ? a.m_reloadTime : 0f, ReloadCut);
            page.Line($"Reload time -{SkillPage.Percent(ReloadCut * page.Factor)}", "Reload time",
                $"At 100: half the crossbow's own, and so half the stamina reloading drains.{crossbow}");
        }

        /// <summary>
        /// " Your Arbalest: 2.9 s (from 4 s).": the held weapon's time at the player's level, the game's lerp from its own
        /// time down by <paramref name="cut"/> at level 100; empty with no such weapon in hand.
        /// </summary>
        private static string HeldTime(SkillPage page, System.Func<Attack, float> baseTime, float cut)
        {
            ItemDrop.ItemData weapon = WeaponLines.Held(page);
            float full = weapon == null ? 0f : baseTime(weapon.m_shared.m_attack);
            if (full <= 0f)
                return "";
            float now = Mathf.Lerp(full, full * (1f - cut), page.Factor);
            return $" Your {WeaponLines.Name(weapon.m_shared.m_name)}: {WeaponLines.Seconds(now)} (from {WeaponLines.Seconds(full)}).";
        }
    }
}
