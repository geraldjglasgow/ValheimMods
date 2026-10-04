using System.Globalization;

namespace Wayfare.SeaGates
{
    /// <summary>The verdict for a pillar at a position: the nearest unpaired pillar it would pair with (if any) and
    /// whether it may. <see cref="ReasonToken"/> is a SeaGateWords token (PairOk or why not); for a measured problem
    /// <see cref="Value"/> is what was found and <see cref="Limit"/> what the rule wants (metres), which the token's
    /// {0} and {1} show, so a player knows how far off it is.</summary>
    public readonly struct PairCheck
    {
        public readonly SeaGatePillar Candidate;
        public readonly bool Ok;
        public readonly int Sides;
        public readonly string ReasonToken;
        public readonly float Value;
        public readonly float Limit;

        public PairCheck(SeaGatePillar candidate, bool ok, int sides, string reasonToken, float value = 0f, float limit = 0f)
        {
            Candidate = candidate;
            Ok = ok;
            Sides = sides;
            ReasonToken = reasonToken;
            Value = value;
            Limit = limit;
        }

        /// <summary>The reason as a player reads it, numbers filled in.</summary>
        public string Describe()
        {
            if (Localization.instance == null || string.IsNullOrEmpty(ReasonToken))
                return ReasonToken ?? "";
            string text = Localization.instance.Localize(ReasonToken);
            if (!text.Contains("{0}"))
                return text;
            return string.Format(text, Metres(Value), Metres(Limit));
        }

        private static string Metres(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
