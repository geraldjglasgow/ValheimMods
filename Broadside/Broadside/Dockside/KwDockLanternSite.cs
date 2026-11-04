using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a dock lantern would stand (<see cref="KwDockLanternAim"/>): its origin (the post's axis at the deck's top, just
    /// outside the edge), the level way in over the deck (its +Z), the edge's point at the deck's top, the post's length
    /// under the deck's top to its foot, its material (the dock's), the dock section it stands beside (null beside a game
    /// floor), and why it may not be built there (null when it may). Plain data.
    /// </summary>
    public sealed class KwDockLanternSite
    {
        public Vector3 Origin { get; set; }
        public Vector3 Inward { get; set; }
        public Vector3 Edge { get; set; }
        public float Length { get; set; }
        public KwPierKind Kind { get; set; } = KwPierKinds.Wood;
        public KwPier? Pier { get; set; }
        public string? Why { get; set; }

        public bool OnDock => Pier != null;

        public Quaternion Turn => Quaternion.LookRotation(Inward);

        public KwDockLanternSpec Spec(int seed) => new KwDockLanternSpec(Length, Kind, seed, OnDock);

        /// <summary>A site on an edge: its point at the deck's top and the level way out; the post stands off it.</summary>
        public static KwDockLanternSite At(Vector3 edge, Vector3 outward, KwPierKind kind, KwPier? pier) => new KwDockLanternSite
        {
            Edge = edge, Inward = -outward, Kind = kind, Pier = pier, Origin = edge + outward * KwDockLanternShape.Off(kind),
        };

        public static KwDockLanternSite Refused(string why) => new KwDockLanternSite { Why = why };
    }
}
