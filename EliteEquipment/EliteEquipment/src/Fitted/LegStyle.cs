using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// One fitted look: which of the game's leggings wear it, whose chest it borrows its material and textures from, how
    /// its texture atlas is copied together from that chest's textures, how the texture lies on the leggings (texels
    /// per metre round the hips and legs, from <see cref="FieldOrigin"/>), the leather on top under the belt and on the
    /// lips, and its bands (belt, straps, trims) and the belt's buckle. Atlas places are texels from the top-left
    /// of the 256 px atlas.
    /// </summary>
    internal sealed class LegStyle
    {
        public LegStyle(string key, string legs, string chest)
        {
            Key = key;
            Legs = legs;
            LegsHash = legs.GetStableHashCode();
            Chest = chest;
        }

        public string Key { get; }

        /// <summary>The game's leggings prefab that wears this look.</summary>
        public string Legs { get; }
        public int LegsHash { get; }

        /// <summary>The game's chest whose worn material and textures (and body paint) this look copies.</summary>
        public string Chest { get; }

        /// <summary>Texels per metre of the texture round the hips and legs.</summary>
        public float Density { get; set; }

        /// <summary>How far the leggings stand off the skin, metres; the bands and buckle lie on top of that.</summary>
        public float Thickness { get; set; } = 0.010f;

        /// <summary>Full length under the boots (they are drawn over it) instead of ending just inside their tops.</summary>
        public bool UnderBoots { get; set; }

        /// <summary>The atlas texel at the seam and the waist, where the texture round the body starts.</summary>
        public Vector2 FieldOrigin { get; set; }

        /// <summary>Leather for the top band under the belt and the lips at the edges.</summary>
        public RectInt Leather { get; set; }

        /// <summary>The round buckle at the front of the belt.</summary>
        public DiscSpec Buckle { get; set; }

        public List<AtlasCopy> Recipe { get; } = new List<AtlasCopy>();
        public List<BandSpec> Bands { get; } = new List<BandSpec>();
    }

    /// <summary>The chest's worn mesh textures, or its body paint (its armour material's chest textures).</summary>
    internal enum AtlasSource { ChestMesh, ChestPaint }

    /// <summary>Copied once, repeated, or repeated with every other copy mirrored (no hard seams in leather).</summary>
    internal enum AtlasFill { Copy, Tile, Mirror }

    /// <summary>One step of making an atlas: a region of a source texture (laid out at <see cref="Base"/> px) into the atlas.</summary>
    internal readonly struct AtlasCopy
    {
        public AtlasCopy(AtlasSource source, int sourceBase, RectInt from, RectInt to, AtlasFill fill)
        {
            Source = source;
            Base = sourceBase;
            From = from;
            To = to;
            Fill = fill;
        }

        public AtlasSource Source { get; }
        public int Base { get; }
        public RectInt From { get; }
        public RectInt To { get; }
        public AtlasFill Fill { get; }
    }

    /// <summary>
    /// A band laid over the leggings: the shell cut between two planes in rest space (where the dot with
    /// <see cref="Normal"/> is between From and To, metres), lifted off it. Round the body it runs along the atlas region's
    /// width, or along its height when <see cref="Rotated"/> (a tall trim strip). An ankle band's planes are measured from
    /// the leggings' lower end and it is only there without boots.
    /// </summary>
    internal readonly struct BandSpec
    {
        public BandSpec(Vector3 normal, float from, float to, float lift, bool hips, RectInt region, bool rotated = false, bool ankle = false)
        {
            Normal = normal.normalized;
            From = from;
            To = to;
            Lift = lift;
            Hips = hips;
            Region = region;
            Rotated = rotated;
            Ankle = ankle;
        }

        public Vector3 Normal { get; }
        public float From { get; }
        public float To { get; }
        public float Lift { get; }

        /// <summary>True for a band on the hips' triangles (the belt), false for one on the legs'.</summary>
        public bool Hips { get; }
        public RectInt Region { get; }
        public bool Rotated { get; }
        public bool Ankle { get; }
    }

    /// <summary>A round piece on top (radius in metres), textured from a disc in the atlas (centre and radius in texels).</summary>
    internal readonly struct DiscSpec
    {
        public DiscSpec(float radius, Vector2 centre, float texels)
        {
            Radius = radius;
            Centre = centre;
            Texels = texels;
        }

        public float Radius { get; }
        public Vector2 Centre { get; }
        public float Texels { get; }
    }
}
