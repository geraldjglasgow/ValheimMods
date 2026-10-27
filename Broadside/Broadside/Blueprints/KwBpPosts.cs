using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The keel posts of a blueprint and the fittings that stand on them, for a ship scaled as a whole
    /// (<see cref="KwBpResize.Scale"/>): a post cap sits on a post's top, a rudder on the rudder point at the foot of its
    /// post's outer face (as <see cref="KwKeel.Custom"/> places both). A keel keeps its timber's depth, so those points do
    /// not scale with the rest of the ship about the keel line; each fitting goes where its own post's point now is.
    /// </summary>
    internal sealed class KwBpPosts
    {
        private const float Reach = 1.5f;

        private readonly List<(Vector3 Top, Vector3 Rudder, Vector3 NewTop, Vector3 NewRudder)> posts =
            new List<(Vector3 Top, Vector3 Rudder, Vector3 NewTop, Vector3 NewRudder)>();

        /// <summary>A keel's post as it was and as it is now scaled (its piece's new place, its new saved text).</summary>
        public void Add(KwBpPiece keel, Vector3 at, string text)
        {
            if (!KwKeelSpec.TryParse(KwShipParts.Saved(keel), out KwKeelSpec was) || !KwKeelSpec.TryParse(text, out KwKeelSpec now))
                return;
            if (Points(keel.Position, keel.Rotation, was, out Vector3 top, out Vector3 rudder)
                && Points(at, keel.Rotation, now, out Vector3 newTop, out Vector3 newRudder))
                posts.Add((top, rudder, newTop, newRudder));
        }

        /// <summary>Where a cap (on a post's top) or a rudder (at a post's rudder point) goes now; null when no post is near it.</summary>
        public Vector3? Moved(KwBpPiece fitting)
        {
            bool rudder = KwRudder.Is(fitting.Prefab);  // a cap or a bowsprit sits on the post's top
            float nearest = Reach;
            Vector3? moved = null;
            foreach ((Vector3 top, Vector3 point, Vector3 newTop, Vector3 newPoint) in posts)
            {
                Vector3 was = rudder ? point : top;
                float distance = Vector3.Distance(was, fitting.Position);
                if (distance < nearest)
                    (nearest, moved) = (distance, fitting.Position + (rudder ? newPoint : newTop) - was);
            }
            return moved;
        }

        // A keel's post top and rudder point in the blueprint's frame: the piece stands at the keel's foot, its top (the
        // keel line) its timber's depth over it, the post a half post past its flat end or a half post back from a stem's.
        private static bool Points(Vector3 at, Quaternion turn, KwKeelSpec spec, out Vector3 top, out Vector3 rudder)
        {
            (top, rudder) = (Vector3.zero, Vector3.zero);
            if (!spec.Post)
                return false;
            float half = spec.Body.PostDepth / 2f, x = spec.Length + (spec.Flat ? half : -half);
            top = at + turn * new Vector3(0f, spec.Body.Depth + spec.RiseAt(x) + spec.PostAbove, x);
            rudder = at + turn * new Vector3(0f, spec.RiseAt(spec.Length), x + half);
            return true;
        }
    }
}
