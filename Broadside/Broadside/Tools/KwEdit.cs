using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// Editing parts already built (the user's request, 2026-10-25: "A tool click on an existing piece should allow editing
    /// that specific piece, and shift click should edit all pieces of that type"). A tool's click on a part that tool makes
    /// (<see cref="KwEditKinds"/>: a keel, a rib, planking, a deck, a ladder, a mast, a bowsprit, a railing, a cabin) opens the tool's window
    /// filled with that part's values; with Shift held (read as <see cref="KwTools.Slant"/> reads it) every part of that
    /// kind over the same dry dock is edited (<see cref="KwBpDock.Ship"/>), each given only the values the player changed
    /// (<see cref="KwEditSession"/>); OK builds them again in place (<see cref="KwEditWork"/>), one undo step. A click the
    /// tool still takes its own way (a waiting first click, near a keel's open end or a deck's end,
    /// <see cref="KwEditKind.Keeps"/>) is left to it, and so is any click on a part of another tool's. A click on something a
    /// part stands over edits that part as if it were clicked (<see cref="KwEditKind.Over"/>: the railing tool's click on a
    /// roof railed already, or on the hull under a railing). While the window is open the parts are hidden on this screen and
    /// only the preview of the new values shows (<see cref="KwEditHide"/>, <see cref="KwEditPreview"/>). Only parts over a dry
    /// dock are edited (designing, free); a part under a ward the player has no access to is left out. Runs on the clicking
    /// player's game; the parts go through the game's networked removal and placement like every tool build.
    /// </summary>
    public static class KwEdit
    {
        private static KwEditSession? open;

        /// <summary>Whether an edit's window is open.</summary>
        public static bool IsOpen => open != null;

        /// <summary>A tool's click taken as an edit; false for every other click (the tool takes it as before).</summary>
        public static bool Click(Player player, string tool)
        {
            KwEditKind? kind = KwEditKinds.For(tool);
            KwClick? aim = kind != null && !KwForm.IsOpen ? KwTools.Aim(player) : null;
            if (aim == null)
                return false;
            Piece clicked = aim.Value.Piece;
            Piece? part = kind!.Edits(Utils.GetPrefabName(clicked.gameObject)) ? clicked : kind.Over(aim.Value);
            if (part == null || kind.Keeps(aim.Value))
                return false;
            Start(player, kind, part, KwTools.Slant());
            return true;
        }

        /// <summary>
        /// A part's edit opened as its tool's click on it opens one, whatever that tool would take near it its own way: the
        /// part was clicked with another tool in hand, now switched to its own (<see cref="KwToolSwitch"/>). Shift as for a click.
        /// </summary>
        public static void Open(Player player, KwEditKind kind, Piece part) => Start(player, kind, part, KwTools.Slant());

        /// <summary>
        /// Whether an open edit draws the preview (its parts as OK would build them, in place, <see cref="KwEditPreview"/>,
        /// made as the window changes): the tools' see-through previews stay off; false while none is open, or while its
        /// tool's own window draws it (the sail and bowsprit tools').
        /// </summary>
        public static bool Preview()
        {
            KwEditSession? session = open;
            if (session == null || !KwForm.IsOpen || session.Kind.OwnWindow)
                return false;
            KwPreview.Hide();
            return true;
        }

        /// <summary>An edit's window closed.</summary>
        internal static void Ended(KwEditSession session)
        {
            if (open == session)
                open = null;
        }

        /// <summary>Whatever edit is open forgotten (its window gone without its end, <see cref="KwEditHide"/>).</summary>
        internal static void Drop() => open = null;

        // The part clicked, or with Shift every part of its kind on its dock, opened in the window; or why not.
        private static void Start(Player player, KwEditKind kind, Piece clicked, bool all)
        {
            List<Piece>? ship = OnDock(clicked);
            if (ship == null)
            {
                Say(player, "Only parts on a dry dock can be changed");
                return;
            }
            if (!KwUndoWork.Removable(clicked))
            {
                Say(player, $"A ward you have no access to keeps this {kind.One} as it is");
                return;
            }
            List<KwEditTarget> targets = Targets(kind, clicked, all ? ship : new List<Piece>(), out int warded);
            if (targets.Count == 0)
            {
                Say(player, $"This {kind.One} cannot be read to change it");
                return;
            }
            open = new KwEditSession(player, kind, targets, warded);
            open.Open();
        }

        // Every ship piece over the dock the part stands on, the part among them; null when it stands on none.
        private static List<Piece>? OnDock(Piece part)
        {
            KwBpDockInfo? dock = KwBpDock.Find(part.transform.position);
            List<Piece>? ship = dock != null ? KwBpDock.Ship(dock) : null;
            return ship != null && ship.Contains(part) ? ship : null;
        }

        // The part clicked first, then every other part of its kind among the others that reads and that the player may
        // replace (those a ward keeps counted).
        private static List<KwEditTarget> Targets(KwEditKind kind, Piece clicked, List<Piece> others, out int warded)
        {
            List<KwEditTarget> targets = new List<KwEditTarget>();
            warded = 0;
            Add(targets, kind, clicked);
            if (targets.Count == 0)
                return targets;
            foreach (Piece piece in others)
            {
                if (piece == clicked || piece.m_nview == null || !piece.m_nview.IsValid() || !kind.Edits(Utils.GetPrefabName(piece.gameObject)))
                    continue;
                if (KwUndoWork.Removable(piece))
                    Add(targets, kind, piece);
                else
                    warded++;
            }
            return targets;
        }

        private static void Add(List<KwEditTarget> targets, KwEditKind kind, Piece piece)
        {
            ZDO? zdo = piece.m_nview != null && piece.m_nview.IsValid() ? piece.m_nview.GetZDO() : null;
            string saved = zdo?.GetString(KwGenerated.Key) ?? "";
            KwBiomeTier tier = KwBiomeTiers.Of(zdo);
            KwPartValues? values = zdo != null ? kind.Read(piece, saved, tier, KwRailLooks.Tiered(zdo)) : null;
            if (values != null)
                targets.Add(new KwEditTarget(piece, saved, tier, values));
        }

        private static void Say(Player player, string text) => player.Message(MessageHud.MessageType.Center, text);
    }

    /// <summary>A part an edit may change: the piece, its saved text and tier as they were, and its values read from them.</summary>
    public sealed class KwEditTarget
    {
        public KwEditTarget(Piece piece, string saved, KwBiomeTier tier, KwPartValues values)
        {
            Piece = piece;
            Saved = saved;
            Tier = tier;
            Values = values;
        }

        public Piece Piece { get; }
        public string Saved { get; }
        public KwBiomeTier Tier { get; }
        public KwPartValues Values { get; }
    }

    /// <summary>
    /// A part as the edit would build it again: its new saved text and its tier (null for a part the tiers do not dress); how
    /// far from where it stands (a cabin carried on an edited cabin's roof, and what stands on it); or only taken away.
    /// </summary>
    public sealed class KwEditPlan
    {
        public KwEditPlan(KwEditTarget target, string text, KwBiomeTier? tier, UnityEngine.Vector3 shift = default, bool take = false)
        {
            Target = target;
            Text = text;
            Tier = tier;
            Shift = shift;
            Take = take;
        }

        public KwEditTarget Target { get; }
        public string Text { get; }
        public KwBiomeTier? Tier { get; }

        /// <summary>How far the new part stands from the old one; none for a part edited in place.</summary>
        public UnityEngine.Vector3 Shift { get; }

        /// <summary>Whether the part is only taken away, nothing built in its place (a ladder no longer at its roof's edge).</summary>
        public bool Take { get; }

        /// <summary>
        /// Whether the part is not one of the edit's own but moves with them (<see cref="KwEditFollow"/>): built again with the
        /// tool that made it (<see cref="Tool"/>), left out of the edit's count.
        /// </summary>
        public bool Follows { get; set; }

        /// <summary>The tool a part that follows is built again with ("" for a hammer piece, built from its prefab); null: the edit's.</summary>
        public string? Tool { get; set; }

        /// <summary>Whether it would be built just as it is, so it is left standing.</summary>
        public bool Same => !Take && Shift == UnityEngine.Vector3.zero && Text == Target.Saved && (Tier == null || Tier.Id == Target.Tier.Id);

        /// <summary>The tool a generated part is built with (an old castle's as a cabin), "" for any other prefab.</summary>
        public static string ToolOf(string prefab)
        {
            if (!prefab.StartsWith(KwToolPieces.PartPrefix, System.StringComparison.Ordinal))
                return "";
            string tool = prefab.Substring(KwToolPieces.PartPrefix.Length);
            return KwHouses.Owns(tool) ? KwHouses.Cabin : tool;
        }
    }
}
