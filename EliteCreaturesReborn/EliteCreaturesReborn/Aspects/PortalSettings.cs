using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Portalbound's numbers, read live from the rules at each wind-up, so a rule-file edit retunes the next throw, and
    /// kept inside sane bounds: a portal never hangs absurdly high, never demands a clearance no spot could give, and
    /// never opens beyond <see cref="MaxRange"/> of its target.
    /// </summary>
    internal readonly struct PortalSettings
    {
        /// <summary>The farthest from its target a far portal may open, whatever the rule file says.</summary>
        public const float MaxRange = 60f;

        /// <summary>The fewest metres the far portal hangs above whatever is under it.</summary>
        public readonly float MinHeight;

        /// <summary>The fewest metres between the far portal's centre and any solid thing or creature.</summary>
        public readonly float Clearance;

        /// <summary>The farthest the far portal opens from its target, in metres.</summary>
        public readonly float Range;

        private PortalSettings(float minHeight, float clearance, float range)
        {
            MinHeight = minHeight;
            Clearance = clearance;
            Range = range;
        }

        public static PortalSettings Read() => new PortalSettings(Mathf.Clamp(Power(Fields.MinHeight), 0f, 30f),
            Mathf.Clamp(Power(Fields.Clearance), 0f, 10f), Mathf.Clamp(Power(Fields.Range), 0f, MaxRange));

        private static float Power(string field) => AspectMath.Power(Aspect.Portalbound, field);
    }
}
