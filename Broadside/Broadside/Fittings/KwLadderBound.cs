namespace Broadside
{
    /// <summary>
    /// What bounds the free stretches of an edge a ladder may stand against: an end of the edge, a doorway (never covered),
    /// a window (a ladder may stand in front of it, never snapped there), another ladder, something solid in the way (a
    /// corner post, a mast, a chest), or no floor below to climb down to.
    /// </summary>
    public enum KwLadderBoundKind
    {
        End,
        Doorway,
        Window,
        Ladder,
        Solid,
        NoFloor,
    }

    /// <summary>
    /// One thing along an edge a ladder may stand against (<see cref="KwLadderEdge"/>): from and to along it (metres from its
    /// start), what kind of thing it is and its name in the player's words, for the ladder tool's centring
    /// (<see cref="KwLadderSpans"/>). Pure.
    /// </summary>
    public readonly struct KwLadderBound
    {
        /// <summary>The names the placement message uses.</summary>
        public const string HullSide = "hull side", Corner = "corner", DeckEnd = "deck end", Obstacle = "obstacle";

        public KwLadderBound(float from, float to, KwLadderBoundKind kind, string name)
        {
            From = from;
            To = to;
            Kind = kind;
            Name = name;
        }

        public float From { get; }
        public float To { get; }
        public KwLadderBoundKind Kind { get; }
        public string Name { get; }

        /// <summary>A kind's own word: doorway, window, ladder; a deck end where the floor below ends; else an obstacle.</summary>
        public static string Word(KwLadderBoundKind kind) => kind switch
        {
            KwLadderBoundKind.Doorway => "doorway",
            KwLadderBoundKind.Window => "window",
            KwLadderBoundKind.Ladder => "ladder",
            KwLadderBoundKind.NoFloor => DeckEnd,
            _ => Obstacle,
        };

        public override string ToString() => $"{Name} {From:0.00}..{To:0.00}";
    }
}
