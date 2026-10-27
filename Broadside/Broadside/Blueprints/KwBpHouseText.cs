namespace Broadside
{
    /// <summary>
    /// A cabin's saved text (<see cref="KwCabinSpec"/>, an old castle's too) made again for <see cref="KwBpReshape"/> by its
    /// own <c>Reshaped(w, s)</c>: w the beam's factor, s the whole ship's scale. Wider, its sections and its sides' insets
    /// go out with the beam (w, 1); scaled, every measure (1, s). A cabin is never stretched: the stretch amidships keeps
    /// clear of it (<see cref="KwBpStretchZone"/>), and it moves with its part of the ship. A cabin whose height would leave
    /// the Cabin tool's limits, or whose shape would no longer build, is refused with the reason, as any other part is.
    /// </summary>
    internal static class KwBpHouseText
    {
        private const string Tall = "a cabin would be {0} m high; cabins are {1} to {2} m";

        public static bool Is(string tool) => KwHouses.Owns(tool);

        /// <summary>A cabin's length along the ship from its frame's origin (the deck sections it was built on); 0 for anything else.</summary>
        public static float CabinLength(string tool, string saved) =>
            KwHouses.Owns(tool) && KwCabinSpec.TryParse(saved, out KwCabinSpec cabin) ? cabin.Length : 0f;

        public static string? Widened(string tool, string saved, float w, out string? error) => Reshaped(tool, saved, w, 1f, out error);

        public static string? Scaled(string tool, string saved, float s, out string? error) => Reshaped(tool, saved, 1f, s, out error);

        private static string? Reshaped(string tool, string saved, float w, float s, out string? error)
        {
            error = null;
            if (!KwHouses.Owns(tool) || !KwCabinSpec.TryParse(saved, out KwCabinSpec cabin))
                return saved;
            if (!KwBpPartText.Within(cabin.Height * s, KwCabinSpec.MinHeight, KwCabinSpec.MaxHeight, Tall, out error))
                return null;
            KwCabinSpec made = cabin.Reshaped(w, s);
            string? refused = KwCabinOutline.Refused(made);
            if (refused != null)
                error = "a cabin would not build: " + char.ToLowerInvariant(refused[0]) + refused.Substring(1);
            return refused == null ? made.ToString() : null;
        }
    }
}
