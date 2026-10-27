using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A wall of a part the Doorway and Window tools cut into (<see cref="KwDoorwayTool"/>, <see cref="KwWindowTool"/>): the
    /// part (a planking part or a cabin; none in a test), its tool, saved text and tier, the wall (<see cref="KwOpeningSurface"/>:
    /// a planking side, a cabin's own wall, a cabin's raised hull side), the place clicked on it (u along, z up, the part's kit
    /// axes), the part's openings as saved, the deck inside under the place (a cabin's own floor; planking's found by a ray
    /// down inside the hull, none when nothing is there). Which reach along the wall an opening may take: planking's ends, where its
    /// ribs stand, kept 0.1 m clear; a cabin's walls kept a corner post clear of their ends. Pure but for the part it names.
    /// </summary>
    public sealed class KwOpeningSite
    {
        /// <summary>How far an opening's frame keeps clear of a planking part's ends (the ribs stand there).</summary>
        public const float EndClear = 0.1f;

        public KwOpeningSite(string tool, string saved, KwBiomeTier tier, KwOpeningSurface surface, KwCabinWall? own = null, Piece? piece = null)
        {
            (Tool, Saved, Tier, Surface, Own, Piece, Of) = (tool, saved, tier, surface, own, piece, piece);
            Openings = KwOpeningText.Of(saved);
            if (KwHouses.Owns(tool) && KwCabinSpec.TryParse(saved, out KwCabinSpec spec))
                (Spec, Floor) = (spec, spec.Floor);
            else if (KwSpan.TryParse(saved, out KwSpan span))
                Span = span;
        }

        // A copy of a site read before, at another place on the same wall (nothing parsed again).
        private KwOpeningSite(KwOpeningSite of, float u, float z)
        {
            (Tool, Saved, Tier, Surface, Own, Piece, Of) = (of.Tool, of.Saved, of.Tier, of.Surface, of.Own, of.Piece, of.Of);
            (Openings, Spec, Span, U, Z) = (of.Openings, of.Spec, of.Span, u, z);
            if (Spec != null)
                Floor = Spec.Floor;
        }

        /// <summary>The same wall at another place on it (a cabin's floor kept; planking's deck inside not known yet).</summary>
        public KwOpeningSite At(float u, float z) => new KwOpeningSite(this, u, z);

        /// <summary>A kit point's place along this wall: a cabin wall's distance along its line, else the kit x.</summary>
        public float AlongOf(Vector3 kit) => Own != null ? KwOpeningAim.OnPath(Own, kit).U : kit.x;

        public string Tool { get; }
        public string Saved { get; }
        public KwBiomeTier Tier { get; }
        public KwOpeningSurface Surface { get; }

        /// <summary>A cabin's own wall, when the surface is one (its outline, its line across the plan).</summary>
        public KwCabinWall? Own { get; }

        /// <summary>The part in the world; null in a test.</summary>
        public Piece? Piece { get; }

        /// <summary>What tells the part from others: the piece, or in a test anything the test gives.</summary>
        public object? Of { get; set; }

        public KwSpan? Span { get; }
        public KwCabinSpec? Spec { get; }

        /// <summary>The part's openings as saved, on every wall.</summary>
        public List<KwOpening> Openings { get; }

        /// <summary>The place clicked on the wall.</summary>
        public float U { get; set; }
        public float Z { get; set; }

        /// <summary>The deck inside under the place clicked (kit z); null when none was found.</summary>
        public float? Floor { get; set; }

        public string Wall => Surface.Wall;
        public bool Planking => Tool == "planking";
        public bool OwnWall => Own != null;
        public bool Raised => Wall == KwOpening.RaisedLeft || Wall == KwOpening.RaisedRight;

        /// <summary>The tool the edit path builds the part again with: planking, or a cabin (an old castle becomes one).</summary>
        public string EditTool => Planking ? "planking" : KwHouses.Cabin;

        /// <summary>A cabin wall's head: the roof's underside; far above for planking (its sheer is its top, <see cref="KwOpeningSurface.Range"/>).</summary>
        public float Head => Spec != null ? KwCabinWalls.Head(Spec) : float.MaxValue;

        /// <summary>The part's saved text with these openings in place of its own; null when the text does not read.</summary>
        public string? Text(List<KwOpening> openings)
        {
            if (Spec != null)
                return Spec.Opened(openings).ToString();
            if (Span == null || !KwSpan.TryParse(Saved, out KwSpan span))
                return null;
            span.Openings = KwOpeningText.Write(openings);
            return span.ToString();
        }

        /// <summary>
        /// The reach along the wall an opening's middle may take, half as wide as given with a frame so wide, between the
        /// heights its frame takes: planking's ends kept <see cref="EndClear"/> clear; a cabin's raised side and its own walls
        /// kept a corner post (<see cref="KwCabinPosts.Size"/>) clear of their ends (a raised side's end the hull closes
        /// <see cref="EndClear"/>), a wall's ends where its outline reaches at those heights; not clear, the ends themselves.
        /// Empty when lo is over hi.
        /// </summary>
        public (float Lo, float Hi) Allowed(float half, float wide, float z0, float z1, bool clear = true)
        {
            float room = half + wide;
            if (Own != null)
            {
                (float from, float to) = Within(z0, z1);
                float post = clear ? KwCabinPosts.Size : 0f;
                return (from + post + room, to - post - room);
            }
            (float start, float end) = Surface.Reach;
            (float back, float front) = clear ? Clear() : (0f, 0f);
            return (start + back + room, end - front - room);
        }

        // How far the ends are kept clear: planking's and a raised side's the hull closes by EndClear, a cabin's by a post.
        private (float Back, float Front) Clear()
        {
            if (Spec == null)
                return (EndClear, EndClear);
            float back = KwCabinOutline.Closed(Spec, Spec.Back) ? EndClear : KwCabinPosts.Size;
            return (back, KwCabinOutline.Closed(Spec, Spec.Front) ? EndClear : KwCabinPosts.Size);
        }

        /// <summary>How far a cabin's own wall reaches along at its narrowest between two heights (its outline, sampled every 10 cm).</summary>
        public (float From, float To) Within(float z0, float z1)
        {
            float from = float.MinValue, to = float.MaxValue;
            if (Own == null)
                return Surface.Reach;
            (float lo, float hi) = KwCabinWall.Heights(Own.Outline);
            float low = Mathf.Clamp(z0, lo + 0.001f, hi - 0.001f), high = Mathf.Clamp(z1, lo + 0.001f, hi - 0.001f);
            for (float z = low; ; z = Mathf.Min(z + 0.1f, high))
            {
                (float a, float c) = Across(Own.Outline, z);
                (from, to) = (Mathf.Max(from, a), Mathf.Min(to, c));
                if (z >= high)
                    break;
            }
            return (from, to);
        }

        /// <summary>Where a level line at height z crosses an outline (u along): its first and last crossing; (0, 0) off it.</summary>
        public static (float From, float To) Across(List<Vector2> outline, float z)
        {
            float from = float.MaxValue, to = float.MinValue;
            for (int i = 0; i < outline.Count; i++)
            {
                Vector2 a = outline[i], c = outline[(i + 1) % outline.Count];
                if ((a.y - z) * (c.y - z) > 0f || Mathf.Abs(c.y - a.y) < 1e-6f)
                    continue;
                float u = Mathf.Lerp(a.x, c.x, (z - a.y) / (c.y - a.y));
                (from, to) = (Mathf.Min(from, u), Mathf.Max(to, u));
            }
            return from <= to ? (from, to) : (0f, 0f);
        }
    }
}
