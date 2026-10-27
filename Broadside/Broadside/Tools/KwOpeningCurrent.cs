namespace Broadside
{
    /// <summary>
    /// The opening type a Doorway or Window tool places now (the user's request, 2026-10-25: "it needs to show the last
    /// object that you selected for that tool, and it should have some default that is shown if nothing was selected
    /// before"): one of the tool's fixed types (<see cref="KwOpeningTypes"/>), or one of the player's own, the last opening
    /// edited that is none of them; the wheel steps through the fixed types and the player's own last
    /// (<see cref="Step"/>). The last opening placed, edited or picked by the wheel stays current for the session (the
    /// tools keep their last values only for the session, as every tool's window does). The window tool starts on the
    /// square gunport with its lid flapping down, the doorway tool on the single door. How many a row has (Shift held,
    /// <see cref="KwOpeningRow"/>) is kept the same way once the wheel sets it. Local to this player.
    /// </summary>
    public sealed class KwOpeningCurrent
    {
        public static readonly KwOpeningCurrent Window = new KwOpeningCurrent(KwOpeningTypes.Windows);
        public static readonly KwOpeningCurrent Door = new KwOpeningCurrent(KwOpeningTypes.Doors);

        private readonly KwOpeningType[] fixedTypes;
        private KwOpeningType? own;
        private int index;

        public KwOpeningCurrent(KwOpeningType[] fixedTypes) => this.fixedTypes = fixedTypes;

        /// <summary>The tool's current type: "doorway" the Doorway tool's, anything else the Window tool's.</summary>
        public static KwOpeningCurrent Of(string tool) => tool == KwDoorwayTool.Tool ? Door : Window;

        public KwOpeningType Type => index < fixedTypes.Length ? fixedTypes[index] : own!;

        /// <summary>How many a row has, as the wheel last set it with Shift held; none yet: as many as the side's spacing fits.</summary>
        public int? Row { get; set; }

        /// <summary>How many types the wheel steps through: the fixed ones and the player's own when there is one.</summary>
        public int Count => fixedTypes.Length + (own != null ? 1 : 0);

        /// <summary>The type and where it stands in the list: "Square gunport, lid flaps down (2/8)".</summary>
        public string Label => $"{Type.Name} ({index + 1}/{Count})";

        /// <summary>Steps through the list (the wheel's clicks, either way), round from the last to the first.</summary>
        public void Step(int by)
        {
            int count = Count;
            index = ((index + by) % count + count) % count;
        }

        /// <summary>An opening placed or edited becomes the current type: a fixed type it matches, else the player's own.</summary>
        public void Keep(KwOpeningAsk ask)
        {
            int found = System.Array.FindIndex(fixedTypes, t => KwOpeningTypes.Same(t.Ask, ask));
            if (found >= 0)
            {
                index = found;
                return;
            }
            KwOpeningAsk kept = ask.Copy();
            kept.Filled = false;
            own = KwOpeningTypes.Own(kept);
            index = fixedTypes.Length;
        }

        /// <summary>Back to the first type, the player's own and the row's count forgotten (offline checks).</summary>
        public void Reset() => (own, index, Row) = (null, 0, null);
    }
}
