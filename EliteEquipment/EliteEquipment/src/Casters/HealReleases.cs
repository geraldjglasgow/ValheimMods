using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The release effects of Eir's Spring, Chain Mend, Second Wind and Revive (features/casters.md, "Nine more spells"):
    /// their names in <c>ee_casterfx_meadows</c>, and their drawing on each machine from <see cref="CasterFx"/>'s message
    /// (<see cref="SpellFx"/> hands them here before the shared flash path, which would tint them by the spell's colour).
    /// The character in <see cref="Draw"/> is the message's: the one healed for a chain's arc, the caster for Second
    /// Wind's ring, the one revived for Revive's pillar on them (none for the pillar at the grave).
    /// </summary>
    public static class HealReleases
    {
        public const string Spring = "ee_fx_cast_spring";
        public const string SecondWind = "ee_fx_cast_secondwind";
        public const string ChainArc = "ee_fx_chainmend_arc";
        public const string Revive = "ee_fx_cast_revive";

        /// <summary>True when the effect is one of these four, drawn here.</summary>
        public static bool Draw(string effect, Vector3 at, Character character)
        {
            switch (effect)
            {
                case Spring:
                    Casters.Spring.Show(at);
                    return true;
                case SecondWind:
                    Casters.SecondWind.Show(at, character);
                    return true;
                case ChainArc:
                    ChainMend.ShowArc(at, character);
                    return true;
                case Revive:
                    Casters.Revive.Show(at, character);
                    return true;
                default:
                    return false;
            }
        }
    }
}
