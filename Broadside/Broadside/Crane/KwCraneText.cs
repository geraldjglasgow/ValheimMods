namespace Broadside
{
    /// <summary>The crane's and the crate's words (English, as the rest of Broadside; keys shown with the game's bindings).</summary>
    public static class KwCraneText
    {
        public const string CraneDescription =
            "A crane for loading and unloading ships. Work it with E like a ship's helm, one person at a time; it lifts cargo and storage crates with everything inside, off ships and onto them.";
        public const string CrateDescription = "A crate for cargo, 24 slots. Only a cargo crane can lift it, with everything inside.";

        /// <summary>The keys box's lines (<see cref="KwCraneKeys"/>), the game's bindings in its key colour.</summary>
        public const string KeyLines = "[<color=yellow><b>$KEY_Left</b></color>] [<color=yellow><b>$KEY_Right</b></color>]  Turn\n"
            + "[<color=yellow><b>$KEY_Forward</b></color>] [<color=yellow><b>$KEY_Backward</b></color>]  Boom out / in\n"
            + "[<color=yellow><b>$KEY_Jump</b></color>]  Raise the hook\n"
            + "[<color=yellow><b>$KEY_Crouch</b></color>]  Lower the hook\n"
            + "[<color=yellow><b>$KEY_Attack</b></color>]  Pick up a crate\n"
            + "[<color=yellow><b>$KEY_Block</b></color>]  Set the crate down\n"
            + "[<color=yellow><b>$KEY_Run</b></color>]  Faster\n"
            + "[<color=yellow><b>$KEY_Use</b></color>]  Leave the crane";

        public const string NotReady = "The crane is not ready yet";
        public const string Holding = "The hook already holds a crate";
        public const string NoCrate = "Lower the hook onto a crate";
        public const string InUse = "Someone has that crate open";
        public const string NoAnswer = "The crate did not answer";
        public const string Empty = "Nothing hangs from the hook";
        public const string Hanging = "The crate hangs from a crane";

        /// <summary>The crane's hover: who works it, how to start or stop, and what hangs from its hook.</summary>
        public static string Hover(KwCrane crane)
        {
            Player? me = Player.m_localPlayer;
            ZDOID who = crane.Operator;
            string use = crane.Operating ? "[<color=yellow><b>$KEY_Use</b></color>] Leave the crane"
                : !who.IsNone() && (me == null || who != me.GetZDOID()) ? "<color=#888888>Someone is working the crane</color>"
                : "[<color=yellow><b>$KEY_Use</b></color>] Work the crane";
            string load = crane.Crate.IsNone() ? "" : "\nA crate hangs from the hook";
            return KwCrane.Label + "\n" + use + load;
        }
    }
}
