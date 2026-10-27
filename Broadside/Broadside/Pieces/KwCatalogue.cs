using System.Collections.Generic;
using System.Linq;

namespace Broadside
{
    /// <summary>
    /// Every piece the kit makes: the dry dock set, the tools and the generated parts they build (keels, ribs, planking,
    /// decks, railings, ladders, masts), and the fittings.
    /// </summary>
    public static class KwCatalogue
    {
        public static readonly Dictionary<string, KwDef> Defs = new Dictionary<string, KwDef>();

        public static void Build()
        {
            if (Defs.Count > 0)
                return;
            foreach (KwDef def in KwDock.All.Concat(KwToolPieces.All()).Concat(KwFeatures.All()))
                Defs[def.Name] = def;
        }
    }
}
