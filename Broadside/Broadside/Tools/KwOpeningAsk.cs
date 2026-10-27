using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the Doorway or Window tool's window asks (<see cref="KwOpeningForm"/>): fill the opening in (wall again), or its
    /// shape, width and height (a round port's and a square's height its width), its sill's height over the deck inside,
    /// its frame and its cover (a doorway's door, single or double). As
    /// the tools' current type (<see cref="KwOpeningCurrent"/>) its sill is the height over the deck inside the ghost
    /// snaps to. As an edit's values (<see cref="KwPartValues"/>, <see cref="Values"/>) a window's sill is
    /// its height over the deck inside, so that a change of it given to every window on a ship puts each that high over
    /// its own deck, and its height the height row's (a square's and a round port's follow their width when made, so a
    /// square's new width never becomes a rectangle's height). Pure.
    /// </summary>
    public sealed class KwOpeningAsk
    {
        public const string Fill = "fill", Shape = "shape", Width = "width", Height = "height", Sill = "sill", Frame = "frame", Cover = "cover";

        /// <summary>A new window's sill over the deck inside: a gunport's in planking, a window's in a cabin's wall.</summary>
        public const float PortSill = 0.5f, CabinSill = 1f;

        public bool Filled { get; set; }
        public string Kind { get; set; } = KwOpening.Square;
        public float Wide { get; set; } = 0.6f;
        public float Tall { get; set; } = 0.6f;

        /// <summary>A window's sill over the deck inside (over the keel line with none).</summary>
        public float Above { get; set; } = PortSill;
        public string Framed { get; set; } = KwOpening.Plain;
        public string Covered { get; set; } = KwOpening.None;

        /// <summary>The height it is cut: a round port's and a square's their width.</summary>
        public float High => Kind == KwOpening.Round || Kind == KwOpening.Square ? Wide : Tall;

        public KwOpeningAsk Copy() => (KwOpeningAsk)MemberwiseClone();

        /// <summary>An opening's own answers (its sill over the deck inside, when there is one; else over the keel line).</summary>
        public static KwOpeningAsk Of(KwOpening o, float? floor) => new KwOpeningAsk
        {
            Kind = o.Shape, Wide = o.Width, Tall = o.Height, Above = o.Sill - (floor ?? 0f), Framed = o.Frame, Covered = o.Cover,
        };

        /// <summary>As an edit's values, the sill given (over the deck inside).</summary>
        public KwPartValues Values(float sill)
        {
            KwPartValues values = new KwPartValues();
            values[Fill] = Filled ? "1" : "0";
            values[Shape] = Kind;
            values.Set(Width, Wide);
            values.Set(Height, Tall);
            values.Set(Sill, sill);
            values[Frame] = Framed;
            values[Cover] = Covered;
            return values;
        }

        /// <summary>
        /// An opening as an edit's values make it again, its number, wall and place along kept: its sill that far over its
        /// deck inside (over the keel line without one), or kept where it was when keep says so.
        /// </summary>
        public static KwOpening Made(KwOpening was, KwPartValues values, float? floor, bool keepSill)
        {
            string shape = values[Shape];
            float width = values.Number(Width, was.Width), height = shape == KwOpening.Square ? width : values.Number(Height, was.Height);
            float sill = keepSill ? was.Sill : (floor ?? 0f) + values.Number(Sill, was.Sill - (floor ?? 0f));
            return was.With(shape, was.U, sill, width, height, values[Frame], values[Cover]);
        }

        /// <summary>Whether an edit's values fill the opening in.</summary>
        public static bool Fills(KwPartValues values) => values[Fill] == "1";

        public override string ToString() =>
            $"{(Filled ? "fill" : "keep")} {Kind} {Wide:0.##}x{High:0.##} sill+{Mathf.Round(Above * 100f) / 100f} {Framed} {Covered}";
    }
}
