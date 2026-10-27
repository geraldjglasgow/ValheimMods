using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The stern a rudder hangs on as its lay is picked (<see cref="KwRudderLayFind"/>), in the rudder's frame (kit axes):
    /// whether a round bar would pass through any of its pieces, the deck's top under a point, and whether a cap stands on
    /// the post. In the game the pieces' colliders answer (<see cref="KwRudderSweep"/>).
    /// </summary>
    public interface IKwRudderStern
    {
        /// <summary>Whether a round bar of radius r from a to b touches no piece but the rudder.</summary>
        bool Clear(Vector3 a, Vector3 b, float r);

        /// <summary>The highest deck's top under a point (x, y), as a height; null where there is none.</summary>
        float? DeckAt(float x, float y, float top);

        /// <summary>Whether a post cap stands at a point (the post's top).</summary>
        bool Capped(Vector3 kit);
    }

    /// <summary>
    /// The stern a rudder hangs on, felt through its pieces' own colliders on the dock, in the rudder's frame (kit axes: x
    /// forward from the post's after face, y left, z up from the rudder point): whether a round bar between two points
    /// would touch any piece but the rudder itself (the keel and its post, a post cap, the planking, the railings, the
    /// decks and whatever else stands on them), the deck's top under a point (the generated decks and the hatches on them),
    /// and whether a post cap stands on the post's top. The pieces are built from their ZDOs, so this is the same on every
    /// peer that has them loaded; only the rudder's owner and the placing player's ghost ask.
    /// </summary>
    public sealed class KwRudderSweep : IKwRudderStern
    {
        private const float Above = 3f, Below = 6f, CapNear = 0.2f;
        private static readonly RaycastHit[] Hits = new RaycastHit[64];
        private static readonly Collider[] Touching = new Collider[32];
        private static readonly List<Piece> Near = new List<Piece>();
        private static int mask;
        private readonly Transform frame;

        public KwRudderSweep(Transform frame) => this.frame = frame;

        private static int Mask => mask != 0 ? mask : mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid");

        public Vector3 World(Vector3 kit) => frame.TransformPoint(KwMesh.ToUnity(kit));

        /// <summary>Whether a round bar of radius r from a to b (kit) touches no piece but the rudder.</summary>
        public bool Clear(Vector3 a, Vector3 b, float r)
        {
            Vector3 from = World(a), along = World(b) - from;
            float length = along.magnitude;
            if (length < 0.01f)
                return Free(Physics.OverlapSphereNonAlloc(from, r, Touching, Mask, QueryTriggerInteraction.Ignore));
            int count = Physics.SphereCastNonAlloc(from, r, along / length, Hits, length, Mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (Blocks(Hits[i].collider))
                    return false;
            }
            return Free(Physics.OverlapSphereNonAlloc(from, r, Touching, Mask, QueryTriggerInteraction.Ignore));
        }

        private bool Free(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (Blocks(Touching[i]))
                    return false;
            }
            return true;
        }

        // Any piece's collider but the rudder's own (a placed piece or the ghost is a scene root).
        private bool Blocks(Collider collider) =>
            collider != null && collider.transform.root != frame.root && collider.GetComponentInParent<Piece>() != null;

        /// <summary>The highest deck's top under a point (kit x, y), as a kit height; null where there is none.</summary>
        public float? DeckAt(float x, float y, float top)
        {
            Vector3 from = World(new Vector3(x, y, top + Above));
            int count = Physics.RaycastNonAlloc(from, -frame.up, Hits, top + Above + Below, Mask, QueryTriggerInteraction.Ignore);
            float? best = null;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                if (Vector3.Dot(hit.normal, frame.up) < 0.6f || !IsDeck(hit.collider))
                    continue;
                float z = KwMesh.ToKit(frame.InverseTransformPoint(hit.point)).z;
                best = Mathf.Max(best ?? float.MinValue, z);
            }
            return best;
        }

        private static bool IsDeck(Collider collider)
        {
            Piece? piece = collider.GetComponentInParent<Piece>();
            string name = piece != null ? Utils.GetPrefabName(piece.gameObject) : "";
            return name == KwToolPieces.PartPrefix + "deck" || name.StartsWith(KwHatch.Prefix, System.StringComparison.Ordinal);
        }

        /// <summary>Whether a post cap stands at a point (kit): the post's top.</summary>
        public bool Capped(Vector3 kit)
        {
            Vector3 at = World(kit);
            Near.Clear();
            Piece.GetAllPiecesInRadius(at, 1f, Near);
            foreach (Piece piece in Near)
            {
                if (KwCaps.Is(Utils.GetPrefabName(piece.gameObject))
                    && (piece.transform.position - at).sqrMagnitude < CapNear * CapNear)
                    return true;
            }
            return false;
        }
    }
}
