using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A birth, on the parent's owner, inside one breeding check (<see cref="BreedingCheck"/>).
    /// <list type="bullet">
    /// <item><b>Before the game's code</b> (<see cref="Before"/>): the check is a birth when the parent is pregnant and
    /// due, by the game's own IsPregnant and IsDue after <see cref="BreedingPace"/> shortened the pregnancy. Twins are
    /// rolled (not for a twin's own birth) and Better offspring is rolled; a won star is set up by
    /// <see cref="OffspringStar"/>. Both chances are their setting's share at the keeper's level.</item>
    /// <item><b>After it</b> (<see cref="After"/>), only when the game did end the pregnancy (so a check another mod
    /// skipped counts nothing): the parent's birth counter (<see cref="Keys.Births"/>) goes up for nearby keepers'
    /// clients to see, a twin's mark is cleared, a won twin roll starts the second pregnancy
    /// (<see cref="TwinPregnancy"/>), and "Strong offspring!" and "Twins!" float above the parent.</item>
    /// </list>
    /// The game's own birth (the tamed flag, the passed-on level, Elite Creatures Reborn's traits) is left to the game.
    /// </summary>
    public static class BirthRolls
    {
        public const string StrongText = "Strong offspring!";
        public const string TwinsText = "Twins!";

        private static readonly int BirthsHash = Keys.Births.GetStableHashCode();

        public static void Before(Procreation parent, BreedingCall call)
        {
            if (!parent.IsPregnant() || !parent.IsDue())
                return;
            call.Birth = true;
            call.WasTwin = TwinPregnancy.IsTwin(parent.m_nview.GetZDO());
            call.Twins = !call.WasTwin && Rolls(HusbandryBreedingSettings.Twins.Value, call.Level);
            if (Rolls(HusbandryBreedingSettings.BetterOffspring.Value, call.Level))
                call.Star = OffspringStar.Begin(parent);
        }

        public static void After(Procreation parent, BreedingCall call)
        {
            ZNetView nview = parent.m_nview;
            if (!call.Birth || nview == null || !nview.IsValid() || parent.IsPregnant())
                return;
            ZDO zdo = nview.GetZDO();
            zdo.Set(BirthsHash, zdo.GetInt(BirthsHash) + 1);
            if (call.WasTwin)
                TwinPregnancy.Clear(zdo);
            if (OffspringStar.Added(parent, call.Star))
                HerdCallout.Send(Above(parent, 0f), StrongText);
            if (call.Twins && TwinPregnancy.Conceive(parent, call.PregnancyDuration))
                HerdCallout.Send(Above(parent, 0.6f), TwinsText);
        }

        /// <summary>The parent's birth count so far (<see cref="Keys.Births"/>), readable on any machine.</summary>
        public static int Births(ZDO zdo) => zdo != null ? zdo.GetInt(BirthsHash) : 0;

        /// <summary>A perk's roll: its share at the keeper's level, as a chance; a share of 0 never wins.</summary>
        private static bool Rolls(float percentAt100, float level)
        {
            float chance = HusbandrySkill.Share(percentAt100, level);
            return chance > 0f && Random.value < chance;
        }

        /// <summary>Just above the parent's head, raised by <paramref name="lift"/> metres so two callouts do not overlap.</summary>
        private static Vector3 Above(Procreation parent, float lift)
        {
            Character character = parent.m_character;
            Vector3 top = character != null ? character.GetTopPoint() : parent.transform.position + Vector3.up;
            return top + Vector3.up * (0.3f + lift);
        }
    }
}
