namespace Broadside
{
    /// <summary>The words of the rig's edits on a launched ship (<see cref="KwSeaRig"/>).</summary>
    public static class KwSeaRigWords
    {
        public const string MastPlace = "Click a deck, a cabin's roof or a keel where the mast should stand";
        public const string PostPlace = "Aim at the top of a stem or stern post";
        public const string Capped = "This post has a cap already";
        public const string Sprit = "This post has a bowsprit already";
        public const string NotOnPost = "It stands on a stem or stern post's top only";
        public const string Moved = "A change keeps the part where it stands";

        /// <summary>A piece's name as the player reads it ("mast", "bowsprit", "post cap").</summary>
        public static string What(string prefab) => prefab == KwSailTool.Made ? "mast" : prefab == KwBowsprit.Made ? "bowsprit" : "post cap";

        /// <summary>A fallen piece asked to change.</summary>
        public static string Down(string what) => $"The {what} is down: put it back with the hammer's repair first, or remove it";

        /// <summary>A piece's window's title on a launched ship.</summary>
        public static string Title(string what) => "Change this " + what + " (launched ship)";

        /// <summary>What the asker reads once a piece is added, changed or removed.</summary>
        public static string Built(string what) => Capital(what) + " built";

        public static string Changed(string what) => Capital(what) + " changed";

        public static string Removed(string what, bool down) => Capital(what) + (down ? " cleared away (it was down: nothing to give back)" : " removed");

        /// <summary>A tool's text that does not read or is past the tool's limits.</summary>
        public static string Unread(string what, string why) => $"That {what} cannot be built: {why}";

        private static string Capital(string what) => what.Length > 0 ? char.ToUpperInvariant(what[0]) + what.Substring(1) : what;
    }
}
