using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// What Farming keeps on a ripe crop's ZDO, written by the plant's owner when it grew (<see cref="CropRipening"/>), who
    /// owns the new crop: its stars, whether it is a giant, and the prefab hash of the plant it grew from. A crop without
    /// them (wild, or ripened before Farming) has 0 stars and is no giant.
    /// </summary>
    public static class CropKeys
    {
        private static readonly int StarsHash = Keys.FarmStars.GetStableHashCode();
        private static readonly int GiantHash = Keys.FarmGiant.GetStableHashCode();
        private static readonly int FromHash = Keys.FarmFrom.GetStableHashCode();

        public static int Stars(ZNetView nview) => Valid(nview) ? Mathf.Clamp(nview.GetZDO().GetInt(StarsHash), 0, global::GrindstoneSkills.Stars.Max) : 0;

        public static bool Giant(ZNetView nview) => Valid(nview) && nview.GetZDO().GetBool(GiantHash);

        /// <summary>The prefab hash of the plant the crop grew from; 0 when unknown.</summary>
        public static int From(ZNetView nview) => Valid(nview) ? nview.GetZDO().GetInt(FromHash) : 0;

        public static void Write(ZDO zdo, int stars, bool giant, int fromHash)
        {
            zdo.Set(StarsHash, Mathf.Clamp(stars, 0, global::GrindstoneSkills.Stars.Max));
            zdo.Set(GiantHash, giant);
            zdo.Set(FromHash, fromHash);
        }

        private static bool Valid(ZNetView nview) => nview != null && nview.IsValid();
    }
}
