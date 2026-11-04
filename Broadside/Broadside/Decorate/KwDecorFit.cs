using System;

namespace Broadside
{
    /// <summary>
    /// The kinds of place a decoration stands in (<see cref="KwDecorFit.Places"/>, any mix), as the auto-decorate tool reads an
    /// area (DECORATE.md, "Reading the area"). Ground and Deck pieces have their origin at the footprint's centre; Wall and Edge
    /// pieces hang: their origin at the foot of the face behind them, their back on it (z = 0), the piece in front (+Z).
    /// A scene's member stands in one of its piece's kinds of place (<see cref="KwSceneMember.Place"/>).
    /// </summary>
    [Flags]
    public enum KwDecorPlace
    {
        None = 0,

        /// <summary>Open ground (and a wide floor's open middle), not too steep, dry.</summary>
        Ground = 1,

        /// <summary>A floor or dock along its edges (its back to the edge or a wall on it), never in its lane or at its ends' access.</summary>
        Deck = 2,

        /// <summary>Hung on a floor's or dock's side face at a drop (the fenders): its back on the side, over the water or the drop.</summary>
        Edge = 4,

        /// <summary>Hung on a building wall that faces open space, its back on the wall.</summary>
        Wall = 8,

        /// <summary>The water's edge: ground within 1.5 m of water, the waterline itself, or a dock's water-facing edge.</summary>
        Shore = 16,

        /// <summary>Standing in shallow water, on the bottom (<see cref="KwDecorFit.MaxDepth"/> at most).</summary>
        Shallows = 32,
    }

    /// <summary>
    /// A dockside decoration's placement metadata for the auto-decorate tool (<see cref="KwDockDef.Decor"/>; DECORATE.md,
    /// "Decoration metadata"): its footprint on the ground (metres along its own x and z, the workshop BRIEF's W and D), its
    /// height, the kinds of place it may stand in, the gap it keeps from decorations of other scenes, whether it may lie in a
    /// path or lane, and whether it may stand in water (also when placed by hand: the piece's <c>m_noInWater</c> is
    /// cleared, <see cref="KwDockside"/>). Where it goes and with what is the scenes' business (<see cref="KwDecorScenes"/>,
    /// <see cref="KwDecorQuay"/>). Plain data.
    /// </summary>
    public sealed class KwDecorFit
    {
        public KwDecorFit(float width, float depth, float height, KwDecorPlace places)
        {
            (Width, Depth, Height, Places) = (width, depth, height, places);
        }

        /// <summary>Footprint along the piece's own x (left to right as the player faces its front), metres.</summary>
        public float Width { get; }

        /// <summary>Footprint along the piece's own z (back to front), metres.</summary>
        public float Depth { get; }

        /// <summary>Height, metres: a wall piece needs a wall this high above <see cref="Lift"/>; an edge piece hangs this far down.</summary>
        public float Height { get; }

        public KwDecorPlace Places { get; }

        /// <summary>Metres kept clear round its footprint from decorations of other scenes and standing ones (the larger of two pieces' counts).</summary>
        public float Spacing { get; set; } = 0.6f;

        /// <summary>May lie in a path or a floor's lane (a flat floor scatter that nobody trips over); false for anything standing.</summary>
        public bool InPath { get; set; }

        /// <summary>
        /// Made for the water: may stand at the waterline and in shallow water. The piece's <c>m_noInWater</c> is cleared,
        /// so it is placed there by hand too (the game's table, which the decorations copy, refuses water).
        /// </summary>
        public bool Wet { get; set; }

        /// <summary>A <see cref="KwDecorPlace.Shallows"/> piece's deepest water, metres over the bottom.</summary>
        public float MaxDepth { get; set; } = 1.6f;

        /// <summary>A <see cref="KwDecorPlace.Wall"/> piece's origin this high over the floor at the wall's foot, metres.</summary>
        public float Lift { get; set; } = 0.4f;

        /// <summary>Whether it stands in a place of this kind.</summary>
        public bool In(KwDecorPlace place) => (Places & place) != 0;

        /// <summary>Whether it hangs (its origin at its back): a Wall or Edge piece.</summary>
        public bool Hangs => (Places & (KwDecorPlace.Wall | KwDecorPlace.Edge)) != 0;
    }
}
