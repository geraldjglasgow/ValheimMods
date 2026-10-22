using System.Globalization;
using Translations;

namespace Wayfare.SeaGates
{
    /// <summary>The verdict for a pillar at a position: the nearest unpaired pillar it would pair with (if any) and
    /// whether it may. <see cref="ReasonKey"/> is a <see cref="SeaGateWords"/> key (PairOk or why not); for a measured
    /// problem <see cref="Value"/> is what was found and <see cref="Limit"/> what the rule wants (metres), which the word's
    /// $1 and $2 show, so a player knows how far off it is.</summary>
    public readonly struct PairCheck
    {
        public readonly SeaGatePillar Candidate;
        public readonly bool Ok;
        public readonly int Sides;
        public readonly string ReasonKey;
        public readonly float Value;
        public readonly float Limit;

        public PairCheck(SeaGatePillar candidate, bool ok, int sides, string reasonKey, float value = 0f, float limit = 0f)
        {
            Candidate = candidate;
            Ok = ok;
            Sides = sides;
            ReasonKey = reasonKey;
            Value = value;
            Limit = limit;
        }

        /// <summary>The reason as a player reads it, numbers filled in (a reason without numbers leaves them out).</summary>
        public string Describe()
        {
            if (string.IsNullOrEmpty(ReasonKey))
                return "";
            return Lang.Format(ReasonKey, Metres(Value), Metres(Limit));
        }

        private static string Metres(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
