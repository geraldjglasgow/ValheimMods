using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>Something that bends a sail's cloth after the wind has placed it (<see cref="KwSailMotion.Bend"/>), point by point.</summary>
    public interface IKwClothBend
    {
        void Bend(KwSailPoint[] points, Vector3[] placed);
    }

    /// <summary>
    /// A launched ship's sail drooping where it is no longer fastened (the user, 2026-10-26: "if you break off a piece of
    /// wood and a ring or two or three are missing, the sail with the rings still on that side looks normal, but the rest
    /// droops down"), on the screens that draw it, after the wind (<see cref="KwSailMotion"/>), from its fastenings
    /// (<see cref="KwSailHurt"/>). A square sail's head between two lashings still holding sags; past the last one holding
    /// it hangs down from that lashing, its corner fallen; with none holding it slumps toward its foot, which its sheets
    /// still hold. A sail along the ship's luff sags aft between hoops still holding, its foot below the boom between
    /// lacing still holding, and past a broken gaff its peak hangs down. How far a point moves is its head's (or luff's,
    /// foot's) move, less toward what still holds it; the slack cloth folds a little. Each point's move is worked out once
    /// a hoist (<see cref="Move"/>, plain arithmetic in the cloth's kit axes) and swayed a little as it is drawn.
    /// </summary>
    public sealed class KwSailDamageDroop : IKwClothBend
    {
        private const float Hang = 72f * Mathf.Deg2Rad, Sag = 0.35f, Fold = 0.18f;
        private readonly KwSailLayout s;
        private readonly KwSailHurt hurt;
        private readonly Dictionary<KwSailPoint[], Vector3[]> moves = new Dictionary<KwSailPoint[], Vector3[]>();

        public KwSailDamageDroop(KwSailLayout s, KwSailHurt hurt) => (this.s, this.hurt) = (s, hurt);

        public void Bend(KwSailPoint[] points, Vector3[] placed)
        {
            if (!moves.TryGetValue(points, out Vector3[] move))
            {
                if (moves.Count > 4)
                    moves.Clear();
                moves[points] = move = Moves(points);
            }
            float sway = 1f + 0.05f * Mathf.Sin(Time.time * 1.7f);
            for (int k = 0; k < placed.Length && k < move.Length; k++)
                placed[k] += move[k] * sway;
        }

        private Vector3[] Moves(KwSailPoint[] points)
        {
            float shown = 0.2f;
            foreach (KwSailPoint p in points)
                shown = Mathf.Max(shown, s.Square ? -KwMesh.ToKit(p.Rest).z : KwMesh.ToKit(p.Rest).z);
            Vector3[] move = new Vector3[points.Length];
            for (int k = 0; k < points.Length; k++)
                move[k] = KwMesh.ToUnity(Move(s, hurt, KwMesh.ToKit(points[k].Rest), shown));
            return move;
        }

        /// <summary>How far a point of the cloth (its kit axes, at rest) droops, the cloth shown that deep (or high).</summary>
        public static Vector3 Move(KwSailLayout s, KwSailHurt hurt, Vector3 kit, float shown)
        {
            if (!hurt.Bent)
                return Vector3.zero;
            return s.Square ? Square(s, hurt, kit, shown) : KwSailDroopAlong.Move(s, hurt, kit, shown);
        }

        // A square sail: its head's move over the point, less toward the foot, which the sheets hold; folds in the slack.
        private static Vector3 Square(KwSailLayout s, KwSailHurt hurt, Vector3 kit, float shown)
        {
            Vector2 head = Head(s, hurt.Lashings, kit.y, shown);
            float down = Mathf.Max(0f, -kit.z), w = Mathf.Clamp01(1f - down / Mathf.Max(shown, 0.2f));
            float fold = head.magnitude * Fold * Mathf.Sin(3f * Mathf.PI * down / Mathf.Max(shown, 0.2f));
            return new Vector3(fold * w, head.x * w, head.y * w);
        }

        /// <summary>
        /// Where a square sail's head moves at a place across it (kit y), as (across, down): sagging between lashings that
        /// hold, hanging from the last one holding to a free corner (the cloth past an end lashing that holds is held by it),
        /// slumping with none holding.
        /// </summary>
        public static Vector2 Head(KwSailLayout s, bool[] gone, float y, float shown)
        {
            int n = gone.Length;
            if (n == 0 || System.Array.IndexOf(gone, true) < 0)
                return Vector2.zero;
            if (System.Array.IndexOf(gone, false) < 0)
                return new Vector2(0f, -0.75f * shown);
            float spacing = s.Width / n, first = -s.Width / 2f + 0.5f * spacing, last = s.Width / 2f - 0.5f * spacing;
            if ((y <= first && !gone[0]) || (y >= last && !gone[n - 1]))
                return Vector2.zero;
            float? left = null, right = null;
            for (int k = 0; k < n; k++)
            {
                float at = first + k * spacing;
                if (!gone[k] && at <= y)
                    left = at;
                if (!gone[k] && at >= y && right == null)
                    right = at;
            }
            return Between(left, right, y, spacing);
        }

        // The head between what holds it on either side: a sag between two, a hang past one, nothing at a lashing itself.
        private static Vector2 Between(float? left, float? right, float y, float spacing)
        {
            if (left.HasValue && right.HasValue)
            {
                float span = right.Value - left.Value;
                if (span <= spacing * 1.01f)
                    return Vector2.zero;
                return new Vector2(0f, -Sag * span * Mathf.Sin(Mathf.PI * (y - left.Value) / span));
            }
            float hinge = left ?? right!.Value, a = Mathf.Abs(y - hinge), way = left.HasValue ? -1f : 1f;
            return new Vector2(way * a * (1f - Mathf.Cos(Hang)), -a * Mathf.Sin(Hang));
        }
    }

    /// <summary>
    /// A sail along the ship drooping (<see cref="KwSailDamageDroop"/>), in its cloth's kit axes (x aft is negative, y its
    /// belly, z up from the boom): its luff sagging aft between the hoops that still hold it (the boom and its head hold its
    /// ends), its foot sagging below the boom between the lacing still holding (the tack and clew hold its ends; past a
    /// broken boom the clew is free), its peak hanging down past a broken gaff.
    /// </summary>
    internal static class KwSailDroopAlong
    {
        private const float Hang = 65f * Mathf.Deg2Rad, Sag = 0.3f;

        public static Vector3 Move(KwSailLayout s, KwSailHurt hurt, Vector3 kit, float shown)
        {
            float aft = Mathf.Max(0f, -kit.x), high = Mathf.Max(shown, 0.2f), u = Mathf.Clamp01(aft / Mathf.Max(s.Width, 0.1f));
            float luff = Run(Hoops(s), hurt.Hoops, kit.z, 0f, high) * Mathf.Pow(1f - u, 2f);
            float foot = Run(Lacing(s), hurt.Lacing, aft, 0f, hurt.BoomBreak >= 0f ? float.NaN : s.Width) * Mathf.Pow(1f - Mathf.Clamp01(kit.z / high), 2f);
            Vector3 move = new Vector3(-luff, 0f, -0.3f * luff - foot);
            if (hurt.GaffBreak >= 0f && u > hurt.GaffBreak)
            {
                float a = (u - hurt.GaffBreak) * s.Width, w = Mathf.Clamp01(kit.z / high);
                move += new Vector3(a * (1f - Mathf.Cos(Hang)), 0f, -a * Mathf.Sin(Hang)) * w;
            }
            return move;
        }

        // The hoops' heights in the cloth's axes (over the boom).
        private static List<float> Hoops(KwSailLayout s) => KwRigPiece.Hoops(s).ConvertAll(at => at - 0.1f - s.ClothOrigin.z);

        // The lacing's places aft along the cloth's foot.
        private static List<float> Lacing(KwSailLayout s) => KwRigPiece.Lacing(s).ConvertAll(at => at + s.Radius + s.ClothOrigin.x);

        /// <summary>
        /// How far a run of broken fastenings lets the cloth sag at a place: between the nearest holding on each side (or
        /// the run's ends: low and high, a NaN end free), a sag a third of their span at its middle; nothing where none is broken.
        /// </summary>
        private static float Run(List<float> at, bool[] gone, float x, float low, float high)
        {
            float before = low, after = high;
            for (int k = 0; k < at.Count && k < gone.Length; k++)
            {
                if (gone[k])
                    continue;
                if (at[k] <= x)
                    before = Mathf.Max(before, at[k]);
                else if (float.IsNaN(after) || at[k] < after)
                    after = at[k];
            }
            if (!Broken(at, gone, before, after))
                return 0f;
            if (float.IsNaN(after))
                return Mathf.Max(0f, x - before) * Mathf.Sin(Hang);
            float span = Mathf.Max(after - before, 0.01f);
            return Sag * span * Mathf.Sin(Mathf.PI * Mathf.Clamp01((x - before) / span));
        }

        // Whether a fastening between two places (the second NaN: to the free end) is broken.
        private static bool Broken(List<float> at, bool[] gone, float before, float after)
        {
            for (int k = 0; k < at.Count && k < gone.Length; k++)
            {
                if (gone[k] && at[k] > before && (float.IsNaN(after) || at[k] < after))
                    return true;
            }
            return false;
        }
    }
}
