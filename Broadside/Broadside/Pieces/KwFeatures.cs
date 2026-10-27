using System.Collections.Generic;
using System.Linq;

namespace Broadside
{
    /// <summary>
    /// The fittings and the station added on top of the hull kit, each in its own file: decorative post caps, the rudders,
    /// hatches, the anchor windlass, the captain's wheel and the dry dock blueprint table.
    /// </summary>
    public static class KwFeatures
    {
        public static IEnumerable<KwDef> All() =>
            KwCaps.All().Concat(KwNameBoard.All()).Concat(KwRudder.All()).Concat(KwHatch.All()).Concat(KwAnchor.All()).Concat(KwWheel.All()).Concat(KwBlueprintStation.All());
    }
}
