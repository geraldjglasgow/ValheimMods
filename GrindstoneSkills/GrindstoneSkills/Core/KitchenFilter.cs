using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A kitchen's trash filter: the minimum stars a dish needs to be kept, stored in the kitchen's ZDO under
    /// <see cref="Keys.MinStars"/>. 0 (the default) keeps everything, and so does every kitchen while the server's
    /// Trash Filter setting is off. Setting it (key, RPC, hover text) lives in the Filter folder.
    /// </summary>
    public static class KitchenFilter
    {
        /// <summary>The minimum stars this kitchen keeps, 0..3.</summary>
        public static int MinStars(ZNetView nview)
        {
            if (!KitchenSettings.TrashFilter.Value || nview == null || !nview.IsValid())
                return 0;
            return Mathf.Clamp(nview.GetZDO().GetInt(Keys.MinStars), 0, Stars.Max);
        }

        /// <summary>Whether a dish with these stars is thrown away at this kitchen.</summary>
        public static bool Trashes(ZNetView nview, int stars) => stars < MinStars(nview);
    }
}
