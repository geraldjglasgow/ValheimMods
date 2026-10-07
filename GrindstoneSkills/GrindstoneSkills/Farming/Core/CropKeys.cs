namespace GrindstoneSkills
{
    /// <summary>
    /// What Farming keeps on a ripe crop's ZDO, written by the plant's owner when it grew (<see cref="CropRipening"/>), who
    /// owns the new crop: whether it is a giant, and the prefab hash of the plant it grew from. A crop without them (wild,
    /// or ripened before Farming) is no giant.
    /// </summary>
    public static class CropKeys
    {
        private static readonly int GiantHash = Keys.FarmGiant.GetStableHashCode();
        private static readonly int FromHash = Keys.FarmFrom.GetStableHashCode();

        public static bool Giant(ZNetView nview) => Valid(nview) && nview.GetZDO().GetBool(GiantHash);

        /// <summary>The prefab hash of the plant the crop grew from; 0 when unknown.</summary>
        public static int From(ZNetView nview) => Valid(nview) ? nview.GetZDO().GetInt(FromHash) : 0;

        public static void Write(ZDO zdo, bool giant, int fromHash)
        {
            zdo.Set(GiantHash, giant);
            zdo.Set(FromHash, fromHash);
        }

        private static bool Valid(ZNetView nview) => nview != null && nview.IsValid();
    }
}
