using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a rudder is sized to: the post it hangs on, its height from the rudder point (the foot of its outer face) to
    /// its top, half its width and its depth along the keel, how far below the rudder point the blade hangs to reach
    /// down near the keel bottom (<see cref="Drop"/>, <see cref="KwRudderDrop"/>), and where the ship's design waterline
    /// stands on it (<see cref="Water"/>, <see cref="KwRudderWater"/>). The rudder's head and tiller grow with the height
    /// (<see cref="Scale"/>, the standard post is 1), their thickness more slowly (<see cref="Thick"/>), its straps wrap the
    /// post's own width and depth, and its blade reaches from the drop up to the waterline (<see cref="KwRudderSpan"/>). The
    /// post is measured from the keels' own snap points, so a rudder fits every keel's post, a stem's or a flat stern's, on
    /// every peer the same; the waterline is measured from the whole hull (on the dock by the rudder's owner, kept in its ZDO).
    /// </summary>
    public readonly struct KwRudderFit
    {
        /// <summary>The standard post's fit: 3 m over the standard keel, from the keel's underside.</summary>
        public static readonly KwRudderFit Standard = new KwRudderFit(KwKeel.Depth + KwKeel.PostAbove, KwKeel.PostHalf, KwKeel.PostDepth);

        public KwRudderFit(float height, float postHalf, float postDepth, float drop = 0f, float water = float.NaN)
        {
            Height = height;
            PostHalf = postHalf;
            PostDepth = postDepth;
            Drop = drop;
            Water = water;
        }

        public float Height { get; }
        public float PostHalf { get; }
        public float PostDepth { get; }

        /// <summary>How far the blade's foot hangs below the rudder point: to 15 cm over the ship's keel bottom; 0 on a flat stern.</summary>
        public float Drop { get; }

        /// <summary>
        /// The ship's design waterline over the rudder point (negative under it: <see cref="KwRudderWater"/>), the line the
        /// blade's broad part reaches up to; NaN while it is not known (no hull measured yet), when the blade guesses it.
        /// </summary>
        public float Water { get; }

        /// <summary>This fit with the ship's design waterline at a height over the rudder point (NaN: not known).</summary>
        public KwRudderFit WithWater(float water) => new KwRudderFit(Height, PostHalf, PostDepth, Drop, water);

        public float Scale => Height / (KwKeel.Depth + KwKeel.PostAbove);
        public float Thick => Mathf.Clamp(Mathf.Sqrt(Scale), 0.35f, 1.5f);

        public bool Same(KwRudderFit other) =>
            Mathf.Abs(Height - other.Height) < 0.01f && Mathf.Abs(PostHalf - other.PostHalf) < 0.005f && Mathf.Abs(PostDepth - other.PostDepth) < 0.005f
            && Mathf.Abs(Drop - other.Drop) < 0.02f && KwRudderWater.Same(Water, other.Water);

        /// <summary>
        /// The fit of a keel's post whose rudder point is at a point: up to the keel's post top nearest it, down to the keel
        /// bottom of it and the keels in line with it; null without a post top.
        /// </summary>
        public static KwRudderFit? Of(Piece keel, Vector3 point)
        {
            Transform? top = null;
            float best = 1f;
            foreach (Transform child in keel.transform)
            {
                Vector3 off = child.position - point;
                float across = new Vector2(off.x, off.z).magnitude;
                if (child.name != "Post top" || !child.CompareTag("snappoint") || across >= best)
                    continue;
                top = child;
                best = across;
            }
            if (top == null || top.position.y - point.y < 0.3f)
                return null;
            KwKeelBody body = KwKeel.BodyOf(keel);
            return new KwRudderFit(top.position.y - point.y, body.PostHalf, body.PostDepth, KwRudderDrop.Of(point.y, KwRudderDrop.Bottom(keel, point)));
        }

        /// <summary>The fit of the post a rudder at a point hangs on: the keel near it with its rudder point there.</summary>
        public static KwRudderFit? At(Vector3 point)
        {
            System.Collections.Generic.List<Piece> near = new System.Collections.Generic.List<Piece>();
            Piece.GetAllPiecesInRadius(point, 16f, near);
            foreach (Piece piece in near)
            {
                if (!KwKeel.IsKeel(piece))
                    continue;
                foreach (Transform child in piece.transform)
                {
                    if (child.name == "Rudder" && child.CompareTag("snappoint") && (child.position - point).sqrMagnitude < 0.01f)
                        return Of(piece, child.position);
                }
            }
            return null;
        }
    }
}
