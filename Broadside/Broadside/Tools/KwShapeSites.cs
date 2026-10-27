using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a made keel or rib goes and what it stands on: a point (the keel line where a keel carries on from another, a
    /// keel rest's top where it lies on the dock, or a rib's foot), its turn, and the thickness of the keel it joins or
    /// stands on (the standard keel's on a dock rest).
    /// </summary>
    public sealed class KwSite
    {
        public KwSite(Vector3 point, Quaternion turn, bool onRest, KwKeelBody body)
        {
            Point = point;
            Turn = turn;
            OnRest = onRest;
            Body = body;
        }

        public Vector3 Point { get; }
        public Quaternion Turn { get; }
        public bool OnRest { get; }
        public KwKeelBody Body { get; }

        /// <summary>A made keel's origin (its underside at its start) for a keel of that depth.</summary>
        public Vector3 KeelOrigin(float depth) => OnRest ? Point : Point - Vector3.up * depth;
    }

    /// <summary>
    /// Where the keel and rib tools put their part from the piece clicked, read from the pieces' own snap points (keel
    /// joints, keel rests, keel tops), so a made part sits exactly on them.
    /// </summary>
    public static class KwShapeSites
    {
        /// <summary>
        /// A keel: on the dock's keel rest nearest the click, running along the dock the way the tool's placeholder points (its
        /// post end; the wheel turns it); or from the
        /// clicked keel's joint nearest the click, carrying on away from that keel at its keel line. Null for anything else.
        /// </summary>
        public static KwSite? Keel(KwClick aim)
        {
            Transform? rest = Nearest(aim.Piece.transform, "Keel rest", aim.Point);
            if (rest != null)
                return new KwSite(rest.position, Facing(aim.Piece.transform.forward, Way()), true, KwKeel.Standard);
            Transform? joint = Nearest(aim.Piece.transform, "Keel joint", aim.Point);
            if (joint == null)
                return null;
            Quaternion turn = Facing(aim.Piece.transform.forward, joint.position - Middle(aim.Piece));
            return new KwSite(joint.position, turn, false, KwKeel.BodyOf(aim.Piece));
        }

        /// <summary>A keel's open end (its "Keel joint") nearest a point; null when it has none.</summary>
        public static Transform? Joint(Piece keel, Vector3 point) => Nearest(keel.transform, "Keel joint", point);

        /// <summary>A rib: on the clicked keel's rib point nearest the click, square to the keel. Null off a keel.</summary>
        public static KwSite? Rib(KwClick aim)
        {
            if (!KwKeel.IsKeel(aim.Piece))
                return null;
            Transform? top = Nearest(aim.Piece.transform, "Keel top", aim.Point);
            Vector3 axis = aim.Piece.transform.forward;
            return top != null ? new KwSite(top.position, Facing(axis, axis), false, KwKeel.BodyOf(aim.Piece)) : null;
        }

        /// <summary>A mast: where the click is on a deck, a cabin's roof or a keel, square to it, facing along it the way the tool's placeholder points.</summary>
        public static KwSite? Mast(KwClick aim)
        {
            bool deck = Utils.GetPrefabName(aim.Piece.gameObject) == KwToolPieces.PartPrefix + "deck";
            if (!deck && !KwKeel.IsKeel(aim.Piece) && !KwCabinRoofs.OnRoof(aim.Piece, aim.Point))
                return null;
            return new KwSite(aim.Point, Facing(aim.Piece.transform.forward, Way()), false, KwKeel.Standard);
        }

        /// <summary>The way the player means: where the tool's placeholder points (the wheel turns it, as any piece), else the look.</summary>
        public static Vector3 Way()
        {
            GameObject? ghost = Player.m_localPlayer != null ? Player.m_localPlayer.m_placementGhost : null;
            return ghost != null && ghost.activeInHierarchy ? ghost.transform.forward : GameCamera.instance.transform.forward;
        }

        /// <summary>The axis, level, turned to the side the hint points to.</summary>
        public static Quaternion Facing(Vector3 axis, Vector3 hint)
        {
            axis.y = 0f;
            if (Vector3.Dot(axis, hint) < 0f)
                axis = -axis;
            return Quaternion.LookRotation(axis.normalized);
        }

        // The snap point of that name on a piece nearest a point (snap points are the piece's inactive children).
        private static Transform? Nearest(Transform piece, string name, Vector3 point)
        {
            Transform? best = null;
            float bestSq = float.MaxValue;
            foreach (Transform child in piece)
            {
                float sq = (child.position - point).sqrMagnitude;
                if (child.name != name || !child.CompareTag("snappoint") || sq >= bestSq)
                    continue;
                best = child;
                bestSq = sq;
            }
            return best;
        }

        /// <summary>The middle of a piece's model, so which side of it a point is on can be told.</summary>
        public static Vector3 Middle(Piece piece)
        {
            MeshFilter? filter = piece.transform.Find("model")?.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null ? filter.transform.TransformPoint(filter.sharedMesh.bounds.center) : piece.transform.position;
        }
    }
}
