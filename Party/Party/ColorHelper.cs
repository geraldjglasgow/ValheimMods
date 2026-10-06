using UnityEngine;

namespace Party
{
    public static class ColorHelper
    {
        private static string partyHex;
        private static string leaderHex;
        private static Color party = Color.white;
        private static Color leader = Color.white;

        public static Color Parse(string hex) => ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;

        /// <summary>
        /// The party colour from the config, parsed once per edit: a config entry hands back the same string object
        /// until its value changes, so a reference compare is the whole cache check.
        /// </summary>
        public static Color PartyColor()
        {
            string hex = PartyConfig.PartyColor.Value;
            if (!ReferenceEquals(hex, partyHex))
            {
                partyHex = hex;
                party = Parse(hex);
            }
            return party;
        }

        /// <summary>The leader colour, cached the same way as <see cref="PartyColor"/>.</summary>
        public static Color LeaderColor()
        {
            string hex = PartyConfig.LeaderColor.Value;
            if (!ReferenceEquals(hex, leaderHex))
            {
                leaderHex = hex;
                leader = Parse(hex);
            }
            return leader;
        }

        public static Color MemberColor(bool isLeader) => isLeader ? LeaderColor() : PartyColor();
    }
}
