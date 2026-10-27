using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ladder ready to build: its top on the edge it hangs from, its turn (facing out over the drop), its length, and what
    /// the placement message says of where it stands (centred between two things, in front of a window; "" for nothing).
    /// </summary>
    public sealed class KwLadderPlan
    {
        public KwLadderPlan(Vector3 top, Quaternion turn, float length, string note = "")
        {
            Top = top;
            Turn = turn;
            Length = length;
            Note = note;
        }

        public Vector3 Top { get; }
        public Quaternion Turn { get; }
        public float Length { get; }
        public string Note { get; }

        public override string ToString() => $"{Top:F2}|{Turn.eulerAngles.y:F0}|{Length:F2}";
    }

    /// <summary>
    /// The ladder tool, one click. Aimed at a hatch (or down through its open opening), the ladder snaps to the spot of the
    /// opening's twenty nearest the aim, five on each edge (<see cref="KwLadderSpots"/>), facing into the opening; aimed at
    /// a deck near an end no other deck carries on from, or at a cabin's roof, it stands against that open edge
    /// (<see cref="KwLadderEdges"/>), facing out over the drop: snapped to the middle of a free span between what bounds it
    /// (the edge's ends, doorways and windows in the wall under it, other ladders, corner posts, things in the way below,
    /// <see cref="KwLadderSpans"/>) or a long span's quarter points, else every 0.25 m along it; never in front of a
    /// doorway. The hover says where it snapped ("Centred between the hull side and the doorway") or why not, once each
    /// time that changes. <see cref="KwPreview"/> shows it see-through there, hanging straight down to what it would stand
    /// on (<see cref="KwLadderDrop"/>); a click builds
    /// it (a networked generated part, <see cref="KwGenerated"/>, free) with that length, and where its top comes up on a
    /// roof railing that railing gets a gap, in the same undo step (<see cref="KwLadderGaps"/>). The same climbable ladder
    /// either way (<see cref="KwLadderClimb"/>), on the dock and on a launched ship. A click on a ladder edits its length
    /// (<see cref="KwLadderEdit"/>). Runs on the clicking player's game. Aimed at a launched ship the same click, hover and
    /// edit are worked out from the ship's blueprint and asked of its owner (<see cref="KwSeaLadderTool"/>).
    /// </summary>
    public static class KwLadderTool
    {
        public const string Tool = "ladder";
        private const string Hint = "Aim at a hatch, or at the open edge of a deck or a cabin's roof";
        private static readonly string Made = KwToolPieces.PartPrefix + Tool;

        // What the hover last told the player (said again only when it changes); quiet for the look after a click, whose
        // message stands.
        private static string told = "";
        private static bool quiet;

        public static void Click(Player player)
        {
            KwLadderPlan? plan = Plan(KwLadderAim.Of(player), out string why, out _);
            if (plan == null)
            {
                Say(player, why);
                return;
            }
            int cut = KwLadderGaps.Build(player, plan);
            KwLadderEdges.Forget();
            Quiet();
            string where = plan.Note.Length > 0 ? ", " + char.ToLowerInvariant(plan.Note[0]) + plan.Note.Substring(1) : "";
            Say(player, $"Ladder built ({plan.Length:0.0} m){where}" + (cut > 0 ? "; the railing above it has a gap for it" : ""));
        }

        /// <summary>The ladder the aim would build now, for the preview (null when there is none); where it snapped, or why not, told when that changes.</summary>
        public static KwLadderPlan? Hover(Player player)
        {
            KwLadderPlan? plan = Plan(KwLadderAim.Of(player), out _, out string tell);
            Told(tell);
            return plan;
        }

        /// <summary>What the hover tells of the ladder (where it snapped, or a refusal), said only when it changes (the dock's and a launched ship's, <see cref="KwSeaLadderTool"/>).</summary>
        public static void Told(string tell)
        {
            if (tell != told && tell.Length > 0 && !quiet)
                Tell(tell);
            (told, quiet) = (tell, false);
        }

        /// <summary>The look after a click keeps quiet, so the click's own message stands.</summary>
        public static void Quiet() => quiet = true;

        /// <summary>The ladder at the spot the aim snaps to, its length down to what it stands on; null and why for none.</summary>
        public static KwLadderPlan? Plan(KwLadderAim aim, out string why) => Plan(aim, out why, out _);

        /// <summary>The same, and what the hover tells the player of it: where it snapped, or a refusal by name ("" for neither).</summary>
        public static KwLadderPlan? Plan(KwLadderAim aim, out string why, out string tell)
        {
            tell = "";
            if (aim.Hatch != null)
                return Hatch(aim.Hatch, aim.Point, out why);
            why = Hint;
            if (aim.Piece == null || IsLadder(aim.Piece))
                return null;
            KwLadderEdge? edge = KwLadderEdges.Find(aim.Piece, aim.Point, out why);
            return edge != null ? Edge(edge, aim.Point, out why, out tell) : null;
        }

        /// <summary>Whether a piece is a ladder the tool made.</summary>
        public static bool IsLadder(Piece piece) => Utils.GetPrefabName(piece.gameObject) == Made;

        // A hatch's spot nearest the aim, facing into the opening, down to whatever is under it.
        private static KwLadderPlan? Hatch(KwHatchCover hatch, Vector3 point, out string why)
        {
            Transform at = hatch.transform;
            Vector3 kit = KwMesh.ToKit(at.InverseTransformPoint(point));
            KwLadderSpot spot = KwLadderSpots.Nearest(KwLadderSpots.Hatch(hatch.Opening), new Vector2(kit.x, kit.y));
            Vector3 top = at.TransformPoint(KwMesh.ToUnity(new Vector3(spot.At.x, spot.At.y, 0f)));
            Vector3 facing = at.TransformDirection(KwMesh.ToUnity(new Vector3(spot.Facing.x, spot.Facing.y, 0f)));
            float length = KwLadderDrop.Drop(top, facing, at, false);
            why = length < KwLadder.MinLength ? $"Nothing to stand a ladder on within {KwLadder.MaxLength:0} m below the hatch" : "";
            return why.Length == 0 ? new KwLadderPlan(top, Quaternion.LookRotation(facing), length) : null;
        }

        // An open edge's spot the aim snaps to (KwLadderSpans), facing out over it, down to a floor of the ship.
        private static KwLadderPlan? Edge(KwLadderEdge edge, Vector3 aim, out string why, out string tell)
        {
            KwLadderPick pick = KwLadderSpans.Pick(edge, KwLadderNear.Of(edge), edge.Along(aim));
            (why, tell) = (pick.Why, pick.Tell);
            if (pick.At == null)
                return null;
            Vector3 top = edge.At(pick.At.Value), outward = edge.OutwardAt(pick.At.Value);
            why = KwLadderDrop.Clear(top, outward, edge.Piece!.transform) ?? "";
            float length = why.Length == 0 ? KwLadderDrop.Drop(top, outward, edge.Piece!.transform, true) : 0f;
            if (length >= KwLadder.MinLength)
                return new KwLadderPlan(top, Quaternion.LookRotation(outward), length, pick.Note);
            if (why.Length == 0)
                why = $"Nothing to stand a ladder on within {KwLadder.MaxLength:0} m below that edge (a deck, a cabin or the hold's floor)";
            tell = "";
            return null;
        }

        private static void Say(Player player, string text) => player.Message(MessageHud.MessageType.Center, text);

        // The hover's word in the middle of the screen, kept out of the message log (it changes as the aim moves).
        private static void Tell(string text)
        {
            if (MessageHud.instance != null)
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text, 0, null, false, false);
        }
    }
}
