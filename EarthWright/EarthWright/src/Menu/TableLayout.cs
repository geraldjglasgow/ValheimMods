using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EarthWright.Menu
{
    /// <summary>
    /// Rearranges one tool's piece list so the result depends only on the current settings, however often it runs:
    /// every managed piece is taken out, the visible ones go back as one block where the first managed piece stood, and
    /// the custom entries follow (at their YAML position when they have one). Pieces other mods added stay where they are.
    /// </summary>
    public static class TableLayout
    {
        /// <summary>Rebuilds the list and returns the index the block was put at.</summary>
        /// <param name="managed">Whether a piece name belongs to the pieces this layout owns.</param>
        /// <param name="fallbackAnchor">Where the block goes when no managed piece is in the list.</param>
        public static int Rebuild(List<GameObject> pieces, Func<string, bool> managed, List<GameObject> block,
            List<(GameObject Prefab, int? Position)> custom, int fallbackAnchor)
        {
            int anchor = pieces.FindIndex(p => p != null && managed(p.name));
            pieces.RemoveAll(p => p == null || managed(p.name));
            anchor = Mathf.Clamp(anchor < 0 ? fallbackAnchor : anchor, 0, pieces.Count);
            pieces.InsertRange(anchor, block);
            InsertCustom(pieces, custom, anchor + block.Count);
            return anchor;
        }

        /// <summary>Entries without a position right after the block in file order, then the others at their index.</summary>
        private static void InsertCustom(List<GameObject> pieces, List<(GameObject Prefab, int? Position)> custom, int after)
        {
            foreach ((GameObject prefab, int? _) in custom.Where(c => !c.Position.HasValue))
                pieces.Insert(Mathf.Clamp(after++, 0, pieces.Count), prefab);
            foreach ((GameObject prefab, int? position) in custom.Where(c => c.Position.HasValue).OrderBy(c => c.Position.Value))
                pieces.Insert(Mathf.Clamp(position.Value, 0, pieces.Count), prefab);
        }
    }
}
