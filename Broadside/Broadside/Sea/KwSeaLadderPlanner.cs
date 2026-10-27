using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ladder tool's one click on a launched ship, worked out as on the dock (<see cref="KwLadderTool"/>) from the
    /// ship's blueprint (<see cref="KwSeaLadderShip"/>), in its local frame: aimed at a hatch (or through its opening), the
    /// spot of the opening's twenty nearest the aim, facing in (<see cref="KwLadderSpots"/>); aimed at a deck near an end no
    /// other deck carries on from, or at a cabin, that open edge (<see cref="KwSeaLadderEdges"/>), snapped by the same
    /// centring (<see cref="KwLadderSpans"/> over <see cref="KwSeaLadderNear"/>'s bounds); and the ladder's length down to
    /// what it stands on (<see cref="KwSeaLadderDrop"/>). A ladder aimed at is found by its box. Plain maths, so every
    /// answer can be checked away from the game.
    /// </summary>
    public static class KwSeaLadderPlanner
    {
        public const string Hint = "Aim at a hatch, or at the open edge of a deck or a cabin's roof";

        // How near a deck's or a roof's top a hit must be to be on it; how far off a hatch a hit still means it; a ladder's box.
        private const float OnTop = 0.15f, NearHatch = 0.3f, LadderHalf = 0.32f;

        /// <summary>The ladder the aim (local ray, the point it hit if any) would build; null and why, and what the hover tells.</summary>
        public static KwLadderPlan? Plan(KwSeaLadderShip ship, Vector3 from, Vector3 way, Vector3? hit, out string why, out string tell)
        {
            tell = "";
            float before = hit.HasValue ? Vector3.Distance(from, hit.Value) : 50f;
            int hatch = Hatch(ship, from, way, hit, before, out Vector3 at);
            if (hatch >= 0)
                return OnHatch(ship, hatch, at, out why);
            why = Hint;
            int part = hit.HasValue ? Part(ship, hit.Value) : -1;
            KwLadderEdge? edge = part >= 0 ? KwSeaLadderEdges.Find(ship, part, hit!.Value, out why) : null;
            return edge != null ? OnEdge(ship, edge, hit!.Value, out why, out tell) : null;
        }

        /// <summary>The place in the blueprint of the ladder whose box a point of the ship's frame is in or on; -1 for none.</summary>
        public static int Ladder(KwSeaLadderShip ship, Vector3 point)
        {
            foreach ((int index, KwBpPiece piece, float length) in ship.Ladders)
            {
                if (Off(piece, length, point) <= 0f)
                    return index;
            }
            return -1;
        }

        /// <summary>How far a point of the ship's frame is from a ladder's box (its rails and rungs, a little over its top; 0 inside).</summary>
        public static float Off(KwBpPiece ladder, float length, Vector3 point)
        {
            Vector3 k = KwMesh.ToKit(KwAnchorPose.Back(ladder.Rotation) * (point - ladder.Position));
            Vector3 near = new Vector3(Mathf.Clamp(k.x, -0.08f, 0.3f), Mathf.Clamp(k.y, -LadderHalf, LadderHalf), Mathf.Clamp(k.z, -length - 0.1f, 0.2f));
            return (k - near).magnitude;
        }

        /// <summary>A level way as the turn of a piece facing it (yaw only; plain maths).</summary>
        public static Quaternion Turn(Vector3 facing)
        {
            float half = Mathf.Atan2(facing.x, facing.z) / 2f;
            return new Quaternion(0f, Mathf.Sin(half), 0f, Mathf.Cos(half));
        }

        // The hatch whose opening and rim the aim crosses at its top before what it hits, or the one it hits; -1 for none.
        private static int Hatch(KwSeaLadderShip ship, Vector3 from, Vector3 way, Vector3? hit, float before, out Vector3 at)
        {
            (int best, at) = (-1, Vector3.zero);
            foreach ((int index, KwBpPiece piece, Vector2 opening) in ship.Hatches)
            {
                Vector3 up = piece.Rotation * Vector3.up;
                float facing = Vector3.Dot(up, way), along = Mathf.Abs(facing) > 1e-4f ? Vector3.Dot(up, piece.Position - from) / facing : -1f;
                if (along > 0f && along < before && Inside(piece, opening, from + way * along, KwHatch.Rim))
                    (best, at, before) = (index, from + way * along, along);
                else if (best < 0 && hit.HasValue && Inside(piece, opening, hit.Value, KwHatch.Rim + NearHatch) && Mathf.Abs(KwSeaLadderShip.In(piece, hit.Value).z) < 1.5f)
                    (best, at) = (index, hit.Value);
            }
            return best;
        }

        private static bool Inside(KwBpPiece hatch, Vector2 opening, Vector3 point, float rim)
        {
            Vector3 kit = KwSeaLadderShip.In(hatch, point);
            return Mathf.Abs(kit.x) <= opening.x / 2f + rim && Mathf.Abs(kit.y) <= opening.y / 2f + rim;
        }

        // The cabin a hit is on (its roof, walls or raised sides), else the deck whose top it is on; -1 for neither.
        private static int Part(KwSeaLadderShip ship, Vector3 hit)
        {
            Vector3 k = KwMesh.ToKit(hit);
            foreach ((int index, KwBpPiece _, KwCabinSpec _, KwRigCabin cabin) in ship.Cabins)
            {
                if (cabin.Blocks(k, OnTop, false))
                    return index;
            }
            foreach ((int index, KwBpPiece _, KwDef _, KwAnchorDeck deck) in ship.Decks)
            {
                if (deck.Top(k.x, k.y) is float top && Mathf.Abs(top - k.z) < OnTop)
                    return index;
            }
            return -1;
        }

        // A hatch's spot nearest the aim, facing into the opening, down to whatever is under it.
        private static KwLadderPlan? OnHatch(KwSeaLadderShip ship, int index, Vector3 point, out string why)
        {
            KwBpPiece hatch = ship.Blueprint.Pieces[index];
            Vector2 opening = KwHatch.Opening(hatch.Prefab) ?? Vector2.one;
            Vector3 kit = KwSeaLadderShip.In(hatch, point);
            KwLadderSpot spot = KwLadderSpots.Nearest(KwLadderSpots.Hatch(opening), new Vector2(kit.x, kit.y));
            Vector3 top = KwSeaLadderShip.Out(hatch, new Vector3(spot.At.x, spot.At.y, 0f));
            Vector3 facing = hatch.Rotation * KwMesh.ToUnity(new Vector3(spot.Facing.x, spot.Facing.y, 0f));
            float length = KwSeaLadderDrop.Drop(ship, top, facing, index, false);
            why = length < KwLadder.MinLength ? $"Nothing to stand a ladder on within {KwLadder.MaxLength:0} m below the hatch" : "";
            return why.Length == 0 ? new KwLadderPlan(top, Turn(facing), length) : null;
        }

        // An open edge's spot the aim snaps to, facing out over it, down to a floor of the ship.
        private static KwLadderPlan? OnEdge(KwSeaLadderShip ship, KwLadderEdge edge, Vector3 aim, out string why, out string tell)
        {
            KwLadderPick pick = KwLadderSpans.Pick(edge, KwSeaLadderNear.Of(ship, edge), edge.Along(aim));
            (why, tell) = (pick.Why, pick.Tell);
            if (pick.At == null)
                return null;
            Vector3 top = edge.At(pick.At.Value), outward = edge.OutwardAt(pick.At.Value);
            why = KwSeaLadderDrop.Clear(ship, top, outward, edge.Part) ?? "";
            float length = why.Length == 0 ? KwSeaLadderDrop.Drop(ship, top, outward, edge.Part, true) : 0f;
            if (length >= KwLadder.MinLength)
                return new KwLadderPlan(top, Turn(outward), length, pick.Note);
            if (why.Length == 0)
                why = $"Nothing to stand a ladder on within {KwLadder.MaxLength:0} m below that edge (a deck, a cabin or the hold's floor)";
            tell = "";
            return null;
        }
    }
}
