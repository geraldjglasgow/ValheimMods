using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// What a keeper's client last saw of one creature, all from its replicated ZDO: taming time left, tamed, the feeding
    /// stamp and the birth counter (<see cref="Keys.Births"/>). Comparing two snapshots tells what happened in between:
    /// taming progress, a tame, a meal, births. The experience for it is in <see cref="HerdExperience"/>.
    /// </summary>
    public struct HerdSnapshot
    {
        public float TamingLeft;
        public float TamingTime;
        public bool Tamed;
        public long LastFed;
        public int Births;

        public static HerdSnapshot Of(Tameable tameable)
        {
            ZDO zdo = tameable.m_nview.GetZDO();
            bool tamed = tameable.IsTamed();
            return new HerdSnapshot
            {
                TamingTime = Mathf.Max(1f, tameable.m_tamingTime),
                Tamed = tamed,
                TamingLeft = tamed ? 0f : zdo.GetFloat(ZDOVars.s_tameTimeLeft, tameable.m_tamingTime),
                LastFed = zdo.GetLong(ZDOVars.s_tameLastFeeding),
                Births = zdo.GetInt(Keys.Births),
            };
        }

        /// <summary>Untamed with taming under way (tameness above 0%).</summary>
        public bool BeingTamed => !Tamed && TamingLeft < TamingTime;
    }
}
