using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevBridge.Stage
{
    /// <summary>One thing put on the stage, and what it takes to put it back the same way after its bundle is reloaded.</summary>
    internal sealed class Placement
    {
        internal const string Asset = "asset";
        internal const string Game = "game";
        internal const string Effect = "effect";
        internal const string Sound = "sound";

        internal int Id;
        internal string Kind;
        internal string Name;

        /// <summary>The bundle it came from, or null for the game's own.</summary>
        internal string Bundle;

        internal GameObject Root;

        /// <summary>How an asset was made, to make it again after a reload; null for anything else.</summary>
        internal PlaceSpec Spec;

        /// <summary>The materials each dressable renderer had when placed: what every dress starts from.</summary>
        internal readonly Dictionary<Renderer, Material[]> Originals = new Dictionary<Renderer, Material[]>();

        /// <summary>The dresses applied, in order (one per `only` filter), re-applied after a reload.</summary>
        internal readonly List<DressSpec> Dresses = new List<DressSpec>();

        /// <summary>Materials made for it, destroyed with it.</summary>
        internal readonly List<Material> Made = new List<Material>();

        /// <summary>The animator's own controller before any clip swap, and the swap to re-apply after a reload.</summary>
        internal RuntimeAnimatorController Controller;
        internal string SwapSpec;
        internal readonly HashSet<string> SwapBundles = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        internal bool Alive => Root;

        /// <summary>Remembers the renderers' current materials as the originals every dress starts from.</summary>
        internal void KeepOriginals(IEnumerable<Renderer> renderers)
        {
            Originals.Clear();
            foreach (Renderer renderer in renderers.Where(r => r)) Originals[renderer] = renderer.sharedMaterials;
        }

        /// <summary>Its object and the materials made for it go; the entry itself can be filled again.</summary>
        internal void Destroy(bool now = false)
        {
            if (Root && now) Object.DestroyImmediate(Root);
            else if (Root) Object.Destroy(Root);
            Root = null;
            foreach (Material material in Made.Where(m => m)) Object.Destroy(material);
            Made.Clear();
            Originals.Clear();
        }
    }

    /// <summary>How an asset from a bundle was placed: which asset, worn by which game creature, at what scale.</summary>
    internal sealed class PlaceSpec
    {
        internal string Asset;
        internal string Bundle;
        internal string On;
        internal string Bone;

        /// <summary>The wearing creature's gear: null or 1 its own, 0 none, or a comma list of game items.</summary>
        internal string Gear;

        internal float Scale = 1f;
    }

    /// <summary>A dress: which game material (Creature, Piece, a prefab's), for which placeholder materials, how plain.</summary>
    internal sealed class DressSpec
    {
        internal string With;
        internal string Child;
        internal string Only;

        /// <summary>When set, the game maps laid out for the game model's UVs come off and the gloss is this.</summary>
        internal float? Gloss;

        internal bool Undress => string.Equals(With, "none", System.StringComparison.OrdinalIgnoreCase);
    }
}
