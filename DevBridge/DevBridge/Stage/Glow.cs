using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevBridge.Stage
{
    /// <summary>
    /// The glow parts a dress leaves as the bundle made them unless glow=1: a material with emission on in a colour that
    /// is not black, or any material on a renderer under a child named glow* (the workshop's GLOW parts, which may start
    /// dark for a mod to light). The mods keep the bundle's own emissive material there, because its Standard shader
    /// draws correctly in game.
    /// </summary>
    internal static class Glow
    {
        internal static bool Is(Renderer renderer, Material placeholder, Transform root) =>
            Emits(placeholder) || Named(renderer.transform, root);

        private static bool Emits(Material material) =>
            material.IsKeywordEnabled("_EMISSION") && material.HasProperty("_EmissionColor") &&
            material.GetColor("_EmissionColor").maxColorComponent > 0.001f;

        // The renderer's own object or a parent below the root (an asset named glow_* is not all glow).
        private static bool Named(Transform part, Transform root)
        {
            for (; part && part != root; part = part.parent)
                if (part.name.StartsWith("glow", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>The glow materials left as built: ones a dress would have taken, but none with glow=1 did.</summary>
        internal static List<string> Left(Placement placement)
        {
            List<DressSpec> worn = placement.Dresses.Where(d => !d.Undress).ToList();
            if (worn.Count == 0) return new List<string>();
            Transform root = placement.Root.transform;
            return placement.Originals.Where(p => p.Key)
                .SelectMany(p => p.Value.Where(m => m && Is(p.Key, m, root))).Distinct()
                .Where(m => worn.Any(d => Dress.Matches(m, d.Only)) && !worn.Any(d => d.Glow && Dress.Matches(m, d.Only)))
                .Select(m => m.name).Distinct().ToList();
        }
    }
}
