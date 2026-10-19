using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The pages of the game's movement skills in the info pane: Run, Swim, Jump, Sneak, Dodge and Ride, each with what
    /// the game's code does with the level (Player.CheckRun and GetRunSpeedFactor, OnSwimming, Character.Jump,
    /// Player.OnSneaking and UpdateStealth, GetDodgeStaminaUse, Sadle.UpdateRiding and Character.GetRunSpeedFactor).
    /// Stamina costs come from the player's own prefab fields and the world's movement stamina rate where the game
    /// applies it, before gear and status effects. What GrindstoneSkills adds follows on each skill's own page writer
    /// (Running, Jumping, Sneaking, Swimming, Dodging and Riding).
    /// </summary>
    public static class MovementPages
    {
        /// <summary>The game's scales at level 100.</summary>
        private const float RunStaminaAt100 = 0.5f;
        private const float RunSpeedAt100 = 0.25f;
        private const float JumpAt100 = 0.4f;
        private const float SneakStaminaAt100 = 0.75f;
        private const float DodgeStaminaAt100 = 0.5f;
        private const float MountSpeedAt100 = 0.25f;
        private const float MountStaminaAt100 = 0.5f;

        /// <summary>How far enemies see a crouching player, as a share of their range: untrained and at 100, dark and lit.</summary>
        private const float SeenDark = 0.5f;
        private const float SeenDarkAt100 = 0.2f;
        private const float SeenLit = 1f;
        private const float SeenLitAt100 = 0.6f;

        public static void Run(SkillPage page)
        {
            Player player = page.Player;
            float drain = player.m_runStaminaDrain * Game.m_moveStaminaRate;
            float speed = player.m_runSpeed;
            page.About = Lang.Get("gs_page_movement_run_about");
            Term(page, "gs_page_movement_run_stamina", "gs_page_movement_run_stamina_line",
                Lang.Format("gs_page_movement_run_stamina_tip", Rate(drain * (1f - RunStaminaAt100 * page.Factor)), Rate(drain), SkillPage.Percent(RunStaminaAt100)),
                SkillPage.Percent(RunStaminaAt100 * page.Factor));
            Term(page, "gs_page_movement_run_speed", "gs_page_movement_run_speed_line",
                Lang.Format("gs_page_movement_run_speed_tip", SkillPage.Number(speed * (1f + RunSpeedAt100 * page.Factor)), SkillPage.Number(speed), SkillPage.Percent(RunSpeedAt100)),
                SkillPage.Percent(RunSpeedAt100 * page.Factor));
        }

        public static void Swim(SkillPage page)
        {
            Player player = page.Player;
            float untrained = player.m_swimStaminaDrainMinSkill * Game.m_moveStaminaRate;
            float master = player.m_swimStaminaDrainMaxSkill * Game.m_moveStaminaRate;
            float now = Mathf.Lerp(untrained, master, page.Factor);
            float saving = untrained > 0f ? 1f - now / untrained : 0f;
            page.About = Lang.Get("gs_page_movement_swim_about");
            Term(page, "gs_page_movement_swim_stamina", "gs_page_movement_swim_stamina_line",
                Lang.Format("gs_page_movement_swim_stamina_tip", Rate(now), Rate(untrained), Rate(master)), SkillPage.Percent(saving));
        }

        public static void Jump(SkillPage page)
        {
            page.About = Lang.Get("gs_page_movement_jump_about");
            Term(page, "gs_page_movement_jump_force", "gs_page_movement_jump_force_line",
                Lang.Format("gs_page_movement_jump_force_tip", SkillPage.Percent(JumpAt100)), SkillPage.Percent(JumpAt100 * page.Factor));
        }

        public static void Sneak(SkillPage page)
        {
            Player player = page.Player;
            float share = 1f - SneakStaminaAt100 * Mathf.Sqrt(page.Factor);
            page.About = Lang.Get("gs_page_movement_sneak_about");
            Term(page, "gs_page_movement_sneak_stamina", "gs_page_movement_sneak_stamina_line",
                Lang.Format("gs_page_movement_sneak_stamina_tip", Rate(player.m_sneakStaminaDrain * share), Rate(player.m_sneakStaminaDrain), SkillPage.Percent(SneakStaminaAt100)),
                SkillPage.Percent(1f - share));
            float dark = Mathf.Lerp(SeenDark, SeenDarkAt100, page.Factor);
            float lit = Mathf.Lerp(SeenLit, SeenLitAt100, page.Factor);
            Term(page, "gs_page_movement_sneak_sight", "gs_page_movement_sneak_sight_line",
                Lang.Format("gs_page_movement_sneak_sight_tip", SkillPage.Percent(SeenDarkAt100), SkillPage.Percent(SeenLitAt100)),
                SkillPage.Percent(dark), SkillPage.Percent(lit));
        }

        public static void Dodge(SkillPage page)
        {
            Player player = page.Player;
            float cost = player.m_dodgeStaminaUsage * (1f - DodgeStaminaAt100 * page.Factor);
            page.About = Lang.Get("gs_page_movement_dodge_about");
            Term(page, "gs_page_movement_dodge_stamina", "gs_page_movement_dodge_stamina_line",
                Lang.Format("gs_page_movement_dodge_stamina_tip", SkillPage.Number(cost), SkillPage.Number(player.m_dodgeStaminaUsage), SkillPage.Percent(DodgeStaminaAt100)),
                SkillPage.Percent(DodgeStaminaAt100 * page.Factor));
            float refund = player.m_perfectDodgeStaminaReturnMultiplier;
            if (refund > 0f)
                Term(page, "gs_page_movement_dodge_perfect", "gs_page_movement_dodge_perfect_line",
                    Lang.Format("gs_page_movement_dodge_perfect_tip", SkillPage.Percent(refund), SkillPage.Number(player.m_perfectDodgeAdrenaline)),
                    SkillPage.Number(cost * refund));
        }

        public static void Ride(SkillPage page)
        {
            page.About = Lang.Get("gs_page_movement_ride_about");
            Term(page, "gs_page_movement_ride_gallop", "gs_page_movement_ride_gallop_line",
                Lang.Format("gs_page_movement_ride_gallop_tip", SkillPage.Percent(MountSpeedAt100)), SkillPage.Percent(MountSpeedAt100 * page.Factor));
            Term(page, "gs_page_movement_ride_stamina", "gs_page_movement_ride_stamina_line",
                Lang.Format("gs_page_movement_ride_stamina_tip", SkillPage.Percent(MountStaminaAt100)), SkillPage.Percent(MountStaminaAt100 * page.Factor));
        }

        /// <summary>
        /// A line about one term: the term's own word (<paramref name="termKey"/>) goes into the line's sentence
        /// (<paramref name="lineKey"/>) as $1, the values after it as $2, $3 ..., so the hovered term is in the line in
        /// every language.
        /// </summary>
        private static void Term(SkillPage page, string termKey, string lineKey, string tip, params string[] values)
        {
            string term = Lang.Get(termKey);
            object[] all = new object[values.Length + 1];
            all[0] = term;
            values.CopyTo(all, 1);
            page.Line(Lang.Format(lineKey, all), term, tip);
        }

        private static string Rate(float perSecond) => Lang.Format("gs_page_movement_rate", SkillPage.Number(perSecond));
    }
}
