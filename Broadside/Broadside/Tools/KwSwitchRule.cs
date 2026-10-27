namespace Broadside
{
    /// <summary>
    /// The tool switch's rules (<see cref="KwToolSwitch"/>), plain data only: which tool a part is edited with, which tool an
    /// opening is, which tool a click goes to, and what the hint says.
    /// </summary>
    public static class KwSwitchRule
    {
        /// <summary>The kind of part a prefab is, its tool the one that edits it (an old castle the Cabin tool's, an old hammer cap the Post cap tool's); null for a piece no tool edits.</summary>
        public static KwEditKind? KindOf(string prefab)
        {
            foreach (KwEditKind kind in KwEditKinds.All)
            {
                if (kind.Edits(prefab))
                    return kind;
            }
            return null;
        }

        /// <summary>Whether a tool is the Doorway or the Window tool.</summary>
        public static bool Opens(string tool) => tool == KwDoorwayTool.Tool || tool == KwWindowTool.Tool;

        /// <summary>The tool an opening is changed with.</summary>
        public static string OpeningTool(bool doorway) => doorway ? KwDoorwayTool.Tool : KwWindowTool.Tool;

        /// <summary>
        /// The tool a click goes to with the held tool in hand: none while its first click waits; an opening's tool when
        /// one is aimed at (none when that is the held tool); else the part's own tool, unless that is the held tool or the
        /// held tool takes the click (acts, asked only then).
        /// </summary>
        public static string? Choose(string held, bool waits, string? opening, string? own, System.Func<bool> acts)
        {
            if (waits)
                return null;
            if (opening != null)
                return opening == held ? null : opening;
            return own == null || own == held || acts() ? null : own;
        }

        /// <summary>The hint for a part: "Click: change this mast (switches to the Sail tool)".</summary>
        public static string Hint(string one, string tool) => $"Click: change this {one} (switches to the {KwToolWords.Name(tool)})";
    }
}
