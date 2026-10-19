using Translations;
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
            page.About = Lang.Get("gs_page_bows_about");
            WeaponLines.Damage(page, "gs_page_bows_damage_line", "gs_page_bows_damage_tip");
            string term = Lang.Get("gs_page_bows_draw");
            string tip = WithHeldTime(page, Lang.Get("gs_page_bows_draw_tip"), a => a.m_bowDraw ? a.m_drawDurationMin : 0f, DrawCut);
            page.Line(Lang.Format("gs_page_bows_draw_line", term, SkillPage.Percent(DrawCut * page.Factor)), term, tip);
            WeaponLines.Cost(page, "gs_page_bows_stamina", "gs_page_bows_stamina_tip");
        }

        public static void Crossbows(SkillPage page)
        {
            page.About = Lang.Get("gs_page_crossbows_about");
            WeaponLines.Damage(page, "gs_page_crossbows_damage_line");
            string term = Lang.Get("gs_page_crossbows_reload");
            string tip = WithHeldTime(page, Lang.Get("gs_page_crossbows_reload_tip"), a => a.m_requiresReload ? a.m_reloadTime : 0f, ReloadCut);
            page.Line(Lang.Format("gs_page_crossbows_reload_line", term, SkillPage.Percent(ReloadCut * page.Factor)), term, tip);
        }

        /// <summary>
        /// The tip and then " Your Arbalest: 2.9 s (from 4 s).": the held weapon's time at the player's level, the game's
        /// lerp from its own time down by <paramref name="cut"/> at level 100; the tip alone with no such weapon in hand.
        /// </summary>
        private static string WithHeldTime(SkillPage page, string tip, System.Func<Attack, float> baseTime, float cut)
        {
            ItemDrop.ItemData weapon = WeaponLines.Held(page);
            float full = weapon == null ? 0f : baseTime(weapon.m_shared.m_attack);
            if (full <= 0f)
                return tip;
            float now = Mathf.Lerp(full, full * (1f - cut), page.Factor);
            return Lang.Format("gs_page_ranged_held", tip, WeaponLines.Name(weapon.m_shared.m_name), WeaponLines.Seconds(now),
                WeaponLines.Seconds(full));
        }
    }
}
