using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The mead base in a fermenter as GrindstoneSkills records it: its stars (<see cref="Keys.BaseStars"/>) and the Cooking
    /// level of the player who put it in (<see cref="Keys.BaseLevel"/>), in the barrel's own ZDO. The game stores only
    /// the content's prefab hash and the start time there, so these keys travel with the same ZDO to every peer and
    /// outlive the owner leaving. Written by the owner when a base goes in, cleared when the batch is tapped or dropped.
    /// Both read 0 when unset, which is what a vanilla add means.
    /// </summary>
    internal static class FermenterBase
    {
        private static readonly int StarsHash = Keys.BaseStars.GetStableHashCode();
        private static readonly int LevelHash = Keys.BaseLevel.GetStableHashCode();

        public static int GetStars(ZDO zdo) => zdo == null ? 0 : Mathf.Clamp(zdo.GetInt(StarsHash), 0, Stars.Max);

        public static float GetLevel(ZDO zdo) => zdo == null ? 0f : Sanitize(zdo.GetFloat(LevelHash));

        /// <summary>Writes both keys, touching the ZDO only for a value that changes, so a vanilla barrel gains no data.</summary>
        public static void Write(ZDO zdo, int stars, float level)
        {
            if (zdo == null)
                return;
            stars = Mathf.Clamp(stars, 0, Stars.Max);
            level = Sanitize(level);
            if (zdo.GetInt(StarsHash) != stars)
                zdo.Set(StarsHash, stars);
            if (zdo.GetFloat(LevelHash) != level)
                zdo.Set(LevelHash, level);
        }

        public static void Clear(ZDO zdo) => Write(zdo, 0, 0f);

        /// <summary>A level that came over the network: never negative, NaN or infinite.</summary>
        public static float Sanitize(float level) => float.IsNaN(level) || float.IsInfinity(level) ? 0f : Mathf.Max(0f, level);
    }
}
