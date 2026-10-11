using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;

namespace EliteCreaturesPack.Custom.Nature
{
    /// <summary>
    /// `senses:` and `movement:` on the shell's mind, the game's <see cref="BaseAI"/> (a MonsterAI or an AnimalAI):
    /// sight <c>m_viewRange</c> and <c>m_viewAngle</c>, hearing <c>m_hearRange</c>, wandering <c>m_randomMoveInterval</c>
    /// and <c>m_randomMoveRange</c>, idle circling <c>m_randomCircleInterval</c>; a flyer's take-off and landing
    /// <c>m_chanceToTakeoff</c>, <c>m_chanceToLand</c>, <c>m_groundDuration</c>, <c>m_airDuration</c> (the game uses them
    /// only on a creature that takes off and lands at random, <c>m_randomFly</c>) and its height above the ground
    /// <c>m_flyAltitudeMin</c>/<c>Max</c>. `alert range` is a monster's alone (<c>MonsterAI.m_alertRange</c>: a target seen
    /// closer than this alerts it, and a tame one fights only within it of where it follows or guards).
    /// </summary>
    internal static class SensesFields
    {
        public static void Apply(CreatureBuild build, Character character)
        {
            SensesBlock? senses = build.Definition.Senses;
            MovementBlock? movement = build.Definition.Movement;
            if (senses == null && movement == null)
            {
                return;
            }
            BaseAI ai = build.Shell.GetComponent<BaseAI>();
            if (ai == null)
            {
                build.Report.Warn("it has no mind of the game's (BaseAI), so its senses and movement are ignored", senses != null ? "senses" : "movement");
                return;
            }
            if (senses != null)
            {
                ApplySenses(build, ai, senses);
            }
            if (movement != null)
            {
                ApplyMovement(build, ai, character, movement);
            }
        }

        private static void ApplySenses(CreatureBuild build, BaseAI ai, SensesBlock senses)
        {
            Assign.Set(ref ai.m_viewRange, senses.SightRange);
            Assign.Set(ref ai.m_viewAngle, senses.SightAngle);
            Assign.Set(ref ai.m_hearRange, senses.HearingRange);
            if (senses.AlertRange == null)
            {
                return;
            }
            if (ai is MonsterAI monster)
            {
                monster.m_alertRange = senses.AlertRange.Value;
            }
            else
            {
                build.Report.Warn($"only a monster's mind has an alert range, and its base's is an {ai.GetType().Name}; ignored", "senses.alert range");
            }
        }

        private static void ApplyMovement(CreatureBuild build, BaseAI ai, Character character, MovementBlock movement)
        {
            Assign.Set(ref ai.m_randomMoveInterval, movement.WanderEvery);
            Assign.Set(ref ai.m_randomMoveRange, movement.WanderRange);
            Assign.Set(ref ai.m_randomCircleInterval, movement.CircleEvery);
            Assign.Set(ref ai.m_chanceToTakeoff, movement.TakeOffChance);
            Assign.Set(ref ai.m_chanceToLand, movement.LandChance);
            Assign.Set(ref ai.m_groundDuration, movement.TimeOnGround);
            Assign.Set(ref ai.m_airDuration, movement.TimeInAir);
            if (movement.FlyHeight != null)
            {
                ai.m_flyAltitudeMin = movement.FlyHeight.Value.Min;
                ai.m_flyAltitudeMax = movement.FlyHeight.Value.Max;
            }
            WarnGrounded(build, ai, character, movement);
        }

        /// <summary>Flyer values on a creature that never takes off are kept (harmless) but named, since they do nothing.</summary>
        private static void WarnGrounded(CreatureBuild build, BaseAI ai, Character character, MovementBlock movement)
        {
            bool landing = movement.TakeOffChance != null || movement.LandChance != null
                || movement.TimeOnGround != null || movement.TimeInAir != null;
            if (landing && !ai.m_randomFly)
            {
                build.Report.Warn("its base never takes off and lands by itself, so take off chance, land chance, time on ground and time in air do nothing", "movement");
            }
            if (movement.FlyHeight != null && !ai.m_randomFly && !character.m_flying)
            {
                build.Report.Warn("its base never flies, so fly height does nothing", "movement.fly height");
            }
        }
    }
}
