namespace Broadside
{
    /// <summary>
    /// What the Dock tool says: its description in the hammer, the live lines its panel shows while it is in hand (the
    /// material and width, the keys, what a click would do and cost, or why not), and what a click built. Plain text.
    /// </summary>
    public static class KwPierWords
    {
        public const string Name = "Dock tool";

        public const string AimStart = "Aim at the shore, or at a dock's end or side, to start a dock";
        public const string AimEnd = "Aim at the water or the ground where the dock should end";

        public static readonly string Description =
            "Build a dock: click the shore (or a dock's end or side to carry it on), then the water where it ends. "
            + $"Wheel: wood, core wood or fine wood. Alt + wheel: width, {KwPierTool.MinWidth:0.0} to {KwPierTool.MaxWidth:0} m. "
            + "Costs its material by its size, at a workbench; all of it comes back when it is removed.";

        /// <summary>The panel's lines while the tool is in hand: what it builds, then the click's (note) or the dock planned.</summary>
        public static string Live(KwPierPlan? plan, string note, bool waiting)
        {
            string head = $"{KwPierTool.Kind.Label} dock, {KwPierTool.Width:0.0} m wide (wheel: material, Alt + wheel: width {KwPierTool.MinWidth:0.0} to {KwPierTool.MaxWidth:0} m)";
            if (!waiting || plan == null)
                return head + "\n" + note;
            string line = plan.Why ?? $"{Size(plan)}: {plan.Cost} {Item(plan.Kind)}. Click to build it; right click or Esc cancels.";
            return head + "\n" + line;
        }

        /// <summary>"18.5 m long in 2 sections" (and "the longest" when the aim reached past it).</summary>
        public static string Size(KwPierPlan plan)
        {
            string text = $"{plan.Length:0.#} m long";
            if (plan.Sections.Count > 1)
                text += $" in {plan.Sections.Count} sections";
            if (plan.Crosses > 0)
                text += plan.Crosses == 1 ? ", across a dock" : $", across {plan.Crosses} docks";
            return plan.Cut ? text + $" (the longest, {KwPierPlan.MaxLength:0} m)" : text;
        }

        public static string Built(KwPierPlan plan, int made)
        {
            string text = $"{plan.Kind.Label} dock built, {plan.Length:0.#} m long: {plan.Cost} {Item(plan.Kind)}";
            return made < plan.Sections.Count ? text + $" ({made} of {plan.Sections.Count} sections; the log says why)" : text;
        }

        /// <summary>The material's item as the game names it.</summary>
        public static string Item(KwPierKind kind)
        {
            ItemDrop? drop = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(kind.Item)?.GetComponent<ItemDrop>() : null;
            return drop != null && Localization.instance != null ? Localization.instance.Localize(drop.m_itemData.m_shared.m_name) : kind.Label;
        }
    }
}
