using System;
using System.Collections.Generic;
using System.Linq;
using PlateColumn;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The painted hammer icons, in the GrindstoneSkills skill icons' style, painted in the asset workshop
    /// (<c>ValheimAssets/Assets/Ships/Keelwright</c>: <c>ToolIcons</c> for the seven tools, <c>PieceIcons</c> for the
    /// blueprint table, the keel block and its guide, the three hatches, the five post caps and the seven rudders) and
    /// embedded from <c>Broadside/assets</c> at 64 px, each named for its piece: the prefab name in lower case
    /// (<c>kw_tool_keel.png</c>, <c>kw_rudder_swamp.png</c>). Decoded once, mipmapped and smoothly filtered, as those skill
    /// icons are. Any piece without a file (a saved ship's hammer entry) gets an icon rendered from its
    /// own meshes (<see cref="KwIcons"/>); a tool without one is reported, since all seven should have theirs.
    /// </summary>
    public static class KwPaintedIcons
    {
        private const string Folder = "Broadside.assets.";
        private static readonly Dictionary<string, Sprite?> loaded = new Dictionary<string, Sprite?>();
        private static HashSet<string>? files;

        /// <summary>A piece's painted icon, by its prefab name; null when it has none.</summary>
        public static Sprite? For(string prefab)
        {
            string resource = Folder + prefab.ToLowerInvariant() + ".png";
            if (!Files().Contains(resource))
            {
                if (prefab.StartsWith(KwToolPieces.ToolPrefix, StringComparison.Ordinal))
                    Log.Warn($"Broadside: no painted icon {resource}; the tool's icon is rendered from its placeholder.");
                return null;
            }
            if (!loaded.TryGetValue(prefab, out Sprite? sprite))
                loaded[prefab] = sprite = Load(resource, prefab);
            return sprite;
        }

        // The embedded resources' names, read once.
        private static HashSet<string> Files() =>
            files ??= new HashSet<string>(typeof(KwPaintedIcons).Assembly.GetManifestResourceNames().Where(n => n.StartsWith(Folder, StringComparison.Ordinal)));

        private static Sprite? Load(string resource, string prefab)
        {
            Sprite? sprite = EmbeddedSprite.Load(typeof(KwPaintedIcons).Assembly, resource, prefab + "_icon");
            if (sprite == null)
                Log.Warn($"Broadside: the painted icon {resource} would not decode; {prefab}'s icon is rendered instead.");
            return sprite;
        }
    }
}
