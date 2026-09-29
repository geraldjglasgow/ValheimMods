using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The pages of the game's movement skills in the info pane: Run, Swim, Jump, Sneak, Dodge and Ride, each with what
    /// the game's code does with the level (Player.CheckRun and GetRunSpeedFactor, OnSwimming, Character.Jump,
    /// Player.OnSneaking and UpdateStealth, GetDodgeStaminaUse, Sadle.UpdateRiding and Character.GetRunSpeedFactor).
    /// Stamina costs come from the player's own prefab fields and the world's movement stamina rate where the game
    /// applies it, before gear and status effects. GrindstoneSkills changes none of these skills.
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
            page.About = "Run faster and longer. Trained by running.";
            page.Line($"Run stamina -{SkillPage.Percent(RunStaminaAt100 * page.Factor)}", "Run stamina",
                $"{Rate(drain * (1f - RunStaminaAt100 * page.Factor))} at your level, {Rate(drain)} untrained, before gear. -{SkillPage.Percent(RunStaminaAt100)} at level 100.");
            page.Line($"Run speed +{SkillPage.Percent(RunSpeedAt100 * page.Factor)}", "Run speed",
                $"{SkillPage.Number(speed * (1f + RunSpeedAt100 * page.Factor))} m/s at your level, {SkillPage.Number(speed)} untrained, before gear. +{SkillPage.Percent(RunSpeedAt100)} at level 100.");
        }

        public static void Swim(SkillPage page)
        {
            Player player = page.Player;
            float untrained = player.m_swimStaminaDrainMinSkill * Game.m_moveStaminaRate;
            float master = player.m_swimStaminaDrainMaxSkill * Game.m_moveStaminaRate;
            float now = Mathf.Lerp(untrained, master, page.Factor);
            float saving = untrained > 0f ? 1f - now / untrained : 0f;
            page.About = "Swimming costs less stamina. Trained by swimming.";
            page.Line($"Swim stamina -{SkillPage.Percent(saving)}", "Swim stamina",
                $"{Rate(now)} at your level, {Rate(untrained)} untrained, {Rate(master)} at level 100, before gear. Out of stamina you drown: 5% of max health a second.");
        }

        public static void Jump(SkillPage page)
        {
            page.About = "Jump higher and farther. Trained by jumping.";
            page.Line($"Jump force +{SkillPage.Percent(JumpAt100 * page.Factor)}", "Jump force",
                $"Both the upward and the forward push of a jump. +{SkillPage.Percent(JumpAt100)} at level 100. A jump costs the same stamina.");
        }

        public static void Sneak(SkillPage page)
        {
            Player player = page.Player;
            float share = 1f - SneakStaminaAt100 * Mathf.Sqrt(page.Factor);
            page.About = "Sneak unseen and for longer. Trained by sneaking near enemies that have not noticed you.";
            page.Line($"Sneak stamina -{SkillPage.Percent(1f - share)}", "Sneak stamina",
                $"{Rate(player.m_sneakStaminaDrain * share)} at your level, {Rate(player.m_sneakStaminaDrain)} untrained, before gear. -{SkillPage.Percent(SneakStaminaAt100)} at level 100; the first levels help most.");
            float dark = Mathf.Lerp(SeenDark, SeenDarkAt100, page.Factor);
            float lit = Mathf.Lerp(SeenLit, SeenLitAt100, page.Factor);
            page.Line($"Enemy sight {SkillPage.Percent(dark)} to {SkillPage.Percent(lit)}", "Enemy sight",
                $"While you crouch, enemies see you and are alerted by you only within that share of their range: the low end in the dark, the high end in full light. {SkillPage.Percent(SeenDarkAt100)} to {SkillPage.Percent(SeenLitAt100)} at level 100.");
        }

        public static void Dodge(SkillPage page)
        {
            Player player = page.Player;
            float cost = player.m_dodgeStaminaUsage * (1f - DodgeStaminaAt100 * page.Factor);
            page.About = "Dodging costs less stamina. Trained by dodging, most by dodging through a hit.";
            page.Line($"Dodge stamina -{SkillPage.Percent(DodgeStaminaAt100 * page.Factor)}", "Dodge stamina",
                $"{SkillPage.Number(cost)} a dodge at your level, {SkillPage.Number(player.m_dodgeStaminaUsage)} untrained, before gear. -{SkillPage.Percent(DodgeStaminaAt100)} at level 100.");
            float refund = player.m_perfectDodgeStaminaReturnMultiplier;
            if (refund > 0f)
                page.Line($"Perfect dodge refund {SkillPage.Number(cost * refund)} stamina", "Perfect dodge",
                    $"Dodging through an attack gives back {SkillPage.Percent(refund)} of the dodge's stamina and {SkillPage.Number(player.m_perfectDodgeAdrenaline)} adrenaline.");
        }

        public static void Ride(SkillPage page)
        {
            page.About = "Your mount gallops faster and longer. Trained by riding at a gallop.";
            page.Line($"Gallop speed +{SkillPage.Percent(MountSpeedAt100 * page.Factor)}", "Gallop speed",
                $"How much faster a mount you ride runs. +{SkillPage.Percent(MountSpeedAt100)} at level 100.");
            page.Line($"Mount stamina -{SkillPage.Percent(MountStaminaAt100 * page.Factor)}", "Mount stamina",
                $"Your mount spends that much less stamina galloping and swimming. -{SkillPage.Percent(MountStaminaAt100)} at level 100.");
        }

        private static string Rate(float perSecond) => $"{SkillPage.Number(perSecond)} stamina/s";
    }
}
