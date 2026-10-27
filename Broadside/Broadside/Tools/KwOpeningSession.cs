using System;
using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// One Doorway or Window tool window open (<see cref="KwDoorwayTool"/>, <see cref="KwWindowTool"/>; one window at a
    /// time, <see cref="KwForm"/>): the player, the wall clicked, what the world says (decks inside, the waterline, worked
    /// out once as the window opens), and how the tool plans its answers. At every change the plan is made again (a tool may
    /// first build the window again with other rows, <see cref="Start"/>'s reshape) and its note said
    /// (<see cref="KwOpeningNotes"/>); the plan's parts show as the real ghost (<see cref="KwOpeningGhost"/>), the parts' own
    /// models hidden, the leaves hung part open so a cover's style shows, made again only when what it shows changes; while
    /// the answers change nothing (or what they ask is refused), the openings as they are show so (the tool's still plan). OK makes the plan once more from the final answers and builds it
    /// (<see cref="KwOpeningWork"/>), unless it would change nothing, or a ward keeps a part it changes; Cancel changes
    /// nothing. Only parts over a dry dock are cut, a ward respected, as edits are (<see cref="OnDock"/>), or a launched
    /// ship's, the game's building rules there, OK sending the plan to its owner (<see cref="May"/>, <see cref="KwSeaOpeningSend"/>). Local to this
    /// player until OK.
    /// </summary>
    public sealed class KwOpeningSession
    {
        private static KwOpeningSession? open;

        private readonly Player player;
        private readonly KwOpeningSite site;
        private readonly KwOpeningWorld world;
        private readonly Func<KwFormValues, KwOpeningPlan> planner;
        private readonly Func<KwFormValues, string> lead;
        private readonly Action<KwFormValues>? keep;
        private readonly (string One, string Many) words;
        private Func<KwFormValues, bool>? reshape;
        private KwOpeningPlan? plan, still;

        private KwOpeningSession(Player player, KwOpeningSite site, KwOpeningWorld world, Func<KwFormValues, KwOpeningPlan> planner,
            Func<KwFormValues, string> lead, (string One, string Many) words, Action<KwFormValues>? keep)
        {
            (this.player, this.site, this.world, this.planner) = (player, site, world, planner);
            (this.lead, this.words, this.keep) = (lead, words, keep);
        }

        /// <summary>
        /// Opens the tool's window on a wall: its rows, how a change is planned, the words first on its note, what is said of
        /// one opening and several ("Window", "windows"), what OK keeps for next time, and what builds the window again with
        /// other rows for some answers (true when it did: the window then reports its new values itself), and the openings as
        /// they are, to show while nothing changes (<see cref="Still"/>).
        /// </summary>
        public static void Start(Player player, string title, List<KwField> fields, KwOpeningSite site, KwOpeningWorld world,
            Func<KwFormValues, KwOpeningPlan> planner, Func<KwFormValues, string> lead, (string One, string Many) words, Action<KwFormValues>? keep = null,
            Func<KwFormValues, bool>? reshape = null, KwOpeningPlan? still = null)
        {
            KwOpeningSession session = new KwOpeningSession(player, site, world, planner, lead, words, keep) { reshape = reshape, still = still };
            open = session;
            KwForm.Open(title, fields, session.Changed, session.Done, true);
        }

        /// <summary>The world as a plan asks it at this wall: rays for decks inside, and the ship's waterline worked out now.</summary>
        public static KwOpeningWorld World(KwOpeningSite site)
        {
            if (KwSeaOpeningPart.Of(site) != null && KwOpeningReads.Sea != null)
                return KwOpeningReads.Sea.World();
            KwOpeningWater? water = site.Piece != null ? KwOpeningWater.Of(site.Piece) : null;
            return new KwOpeningWorld { Crosses = KwOpeningRays.Crosses, Below = water != null ? water.Below : null };
        }

        /// <summary>Whether the player may change a site's part: on a dry dock (<see cref="OnDock"/>), or on a launched ship (<see cref="KwSeaOpeningSend.May"/>); told why not.</summary>
        public static bool May(Player player, KwOpeningSite site) =>
            KwSeaOpeningPart.Of(site) != null ? KwSeaOpeningSend.May(player, site) : OnDock(player, site) != null;

        /// <summary>The ship on the dry dock a site's part stands on, while the player may change that part; null, and the player told why, when not.</summary>
        public static List<Piece>? OnDock(Player player, KwOpeningSite site)
        {
            List<Piece>? ship = site.Piece != null ? KwOpeningHull.Ship(site.Piece) : null;
            if (ship == null)
                Say(player, "Only parts on a dry dock can be cut");
            else if (!KwUndoWork.Removable(site.Piece!))
                Say(player, "A ward you have no access to keeps this part as it is");
            else
                return ship;
            return null;
        }

        // The window's answers planned, the note said (unless the window was built again for them, which says its own).
        private void Changed(KwFormValues values)
        {
            if (reshape != null && reshape(values))
                return;
            plan = planner(values);
            KwForm.Say(KwOpeningNotes.Of(plan, world, lead(values), words.One, words.Many));
        }

        // OK: planned once more and built; Cancel (null): nothing.
        private void Done(KwFormValues? values)
        {
            if (open == this)
                open = null;
            KwOpeningGhost.Hide();
            if (values == null || player == null)
                return;
            Changed(values);
            KwOpeningPlan made = plan!;
            if (!made.Any)
                Say(player, made.Refused > 0 ? "Cannot: " + made.Why : made.Skipped > 0 ? "None fit: " + made.SkipWhy : "Nothing changed");
            else if (KwOpeningWork.Warded(made))
                Say(player, "A ward you have no access to keeps a part this would change");
            else
            {
                keep?.Invoke(values);
                if (!KwSeaOpeningSend.Sent(player, made, words.One, words.Many))
                    Say(player, KwOpeningNotes.Done(made, words.One, words.Many, KwOpeningWork.Build(player, made)));
                KwOpeningReads.Forget();
            }
        }

        /// <summary>The open window's plan shown as the ghost, its leaves part open; the openings as they are while it changes nothing (none while no window is open).</summary>
        public static void Preview()
        {
            KwOpeningSession? session = open;
            KwOpeningPlan? shown = session != null && KwForm.IsOpen ? session.plan : null;
            KwOpeningGhost.Show(shown != null && shown.Any ? shown : session != null && KwForm.IsOpen ? session.still : null, KwOpeningGhostLooks.Open);
        }

        /// <summary>A plan showing openings as they are (each put on its part unchanged): what an edit's ghost shows before a change.</summary>
        public static KwOpeningPlan Still(IEnumerable<(KwOpeningSite Site, KwOpening Opening)> openings)
        {
            KwOpeningPlan plan = new KwOpeningPlan();
            foreach ((KwOpeningSite site, KwOpening o) in openings)
                plan.Part(site).Put(site, o);
            return plan;
        }

        /// <summary>The tools' words in the middle of the screen; the placement message waits a moment after them.</summary>
        public static void Say(Player player, string text)
        {
            KwOpeningWords.Hold();
            player.Message(MessageHud.MessageType.Center, text);
        }
    }
}
