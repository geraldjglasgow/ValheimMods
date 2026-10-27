using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A sail's cloth raised to a share f of its height (1 fully set, 0 furled), as a real sail is: never stretched or
    /// shrunk. A square sail's foot is hauled up toward the yard: the cloth from the yard down to the foot hangs flat and
    /// filled, its texture cut off where the foot is, and the cloth hauled up hangs in bags under it
    /// (<see cref="KwSailDrape"/>); furled, the whole sail hangs in bags under the yard. A sail along the ship comes down
    /// the mast instead: its head drops toward the boom, the cloth above the boom stands filled, and the rest lies in
    /// flakes on the boom. In the cloth's own
    /// coordinates (origin on the edge it is fastened by: under the yard, or on the boom), kit axes as
    /// <see cref="KwSailLayout"/>; one face, as the game's cloth shader draws both sides. The texture stays on the cloth as
    /// it is raised: what stands shows the upper part of the cloth's texture and the bags or flakes the rest, so a pattern
    /// painted on it (<see cref="KwSailLooks"/>) lies where it would on real cloth; a plain triangle sail keeps its texture
    /// as wide as it stands instead (the upper part drawn as wide as the whole cloth would stretch its panels). A jib's
    /// cloth takes its texture from its own triangle in it (<see cref="KwSailLayout.Uv"/>, <see cref="KwJibUv"/>).
    /// </summary>
    public static class KwSailCanvas
    {
        public static Mesh Cloth(KwSailLayout s, float f)
        {
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            Lay(b, s, f);
            return b.ToMesh(KwSailTool.Made + "_cloth");
        }

        // The cloth's faces at hoist f.
        private static void Lay(MeshBuilder b, KwSailLayout s, float f)
        {
            float shown = f * s.Height, gathered = s.Bundle(f);
            if (s.Square)
            {
                if (shown > 0.01f)
                    Grid(b, s, (u, v) => Square(s, u, v, shown), (u, v) => new Vector2(u, 1f - (1f - v) * f));
                KwSailDrape.Square(b, s, f);
                return;
            }
            if (shown > 0.01f)
                Grid(b, s, (u, v) => Along(s, u, v, shown, gathered), (u, v) => AlongUv(s, u, v, f));
            KwSailDrape.Boom(b, s, f);
        }

        // How many faces the flat part has along its fastened edge and away from it: about one every half metre, from
        // ten to sixteen, so the wind's waves over the cloth stay smooth on a big sail.
        private static int Across(KwSailLayout s) => Mathf.Clamp(Mathf.CeilToInt(s.Width / 0.5f), 10, 16);

        private static int Up(KwSailLayout s) => Mathf.Clamp(Mathf.CeilToInt(s.Height / 0.5f), 10, 16);

        // A square sail's flat part: v is 1 at the yard and 0 at the foot; fullest a third of the way down, the foot still
        // bellied, the sides less, the corners held; the sides drawn in a little toward the foot.
        private static Vector3 Square(KwSailLayout s, float u, float v, float shown)
        {
            float down = 1f - v, across = 2f * u - 1f;
            float fill = down < 0.35f ? Mathf.Sin(Mathf.PI / 2f * down / 0.35f) : 1f - 0.55f * Mathf.Pow((down - 0.35f) / 0.65f, 2f);
            float x = s.BellyAt(shown) * fill * (1f - 0.88f * across * across);
            return new Vector3(x, across * s.Width / 2f * (1f - 0.06f * Mathf.Sin(Mathf.PI * down)), -down * shown);
        }

        // A sail along the ship's flat part: u aft along the foot, v up from the folds to the head; deepest two fifths back.
        private static Vector3 Along(KwSailLayout s, float u, float v, float shown, float gathered)
        {
            float draft = Mathf.Sin(Mathf.PI * Mathf.Pow(Mathf.Clamp01(u), 0.75f)) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(v));
            float x = s.Spec.Shape == 1 ? -u * s.Width * (1f - v) : -u * s.Width;
            float rise = s.Spec.Shape == 1 ? v * shown : v * shown * Gaff(u);
            return new Vector3(x, s.BellyAt(shown) * 0.8f * draft, gathered + rise);
        }

        // A sail along the ship's standing cloth's texture place: the upper share f of the cloth's texture, its rows under
        // the gaff (a triangle's under its diagonal, the after edge on it while patterned).
        private static Vector2 AlongUv(KwSailLayout s, float u, float v, float f)
        {
            if (s.Uv != null)
                return s.Uv.Value.Standing(u, v, f);
            float row = 1f - (1f - v) * f;
            if (s.Spec.Shape == 2)
                return new Vector2(u, row * Gaff(u));
            return new Vector2(u * (1f - v) * (Patterned(s) ? f : 1f), row);
        }

        /// <summary>A sail along the ship's flaked cloth's texture place: u along the boom, row y of the cloth's texture under the set part.</summary>
        public static Vector2 FlakeUv(KwSailLayout s, float u, float y) =>
            s.Uv != null ? s.Uv.Value.Flaked(u, y) : s.Spec.Shape == 2 ? new Vector2(u, y * Gaff(u)) : new Vector2(Patterned(s) ? u * (1f - y) : u, y);

        // Painted to the cloth's own outline: a pattern, or a worn cloth's wear (KwSailWear), which tears its leech there.
        private static bool Patterned(KwSailLayout s) => !s.Spec.Pattern.IsPlain || KwSailWear.Worn(s.Spec.Cloth);

        /// <summary>A gaff sail's height share along its foot: its leading edge three quarters of its after edge's.</summary>
        public static float Gaff(float u) => 0.75f + 0.25f * u;

        // A grid of quads over the flat part: u along its fastened edge from 0 to 1, v away from it to 1.
        private static void Grid(MeshBuilder b, KwSailLayout s, System.Func<float, float, Vector3> at, System.Func<float, float, Vector2> uv)
        {
            int across = Across(s), up = Up(s);
            for (int i = 0; i < across; i++)
            {
                for (int j = 0; j < up; j++)
                {
                    float u0 = (float)i / across, u1 = (float)(i + 1) / across, v0 = (float)j / up, v1 = (float)(j + 1) / up;
                    b.Face(new List<Vector3> { at(u0, v0), at(u1, v0), at(u1, v1), at(u0, v1) },
                        new List<Vector2> { uv(u0, v0), uv(u1, v0), uv(u1, v1), uv(u0, v1) });
                }
            }
        }

        /// <summary>
        /// The cloth's free edges the bolt rope runs along, at that hoist, through the cloth's own corners there so that it
        /// moves with them: a square sail's sides down from the yard and its foot; a sail along the ship's after edge up
        /// from the boom's end.
        /// </summary>
        public static List<Vector3> Edge(KwSailLayout s, float f)
        {
            float shown = f * s.Height, gathered = s.Bundle(f);
            List<Vector3> edge = new List<Vector3>();
            if (!s.Square)
            {
                edge.Add(new Vector3(-s.Width, 0f, 0f));
                for (int j = 0; j <= Up(s); j++)
                    edge.Add(Along(s, 1f, (float)j / Up(s), shown, gathered));
                return edge;
            }
            edge.AddRange(Side(s, f, 0f, false));
            edge.AddRange(KwSailDrape.Foot(s, f));
            edge.AddRange(Side(s, f, 1f, true));
            return edge;
        }

        // A square sail's side at u (0 or 1), down from the yard: the set cloth's, then the hauled cloth's down to the
        // clew (the foot's end, not repeated); up toward the yard when rising.
        private static List<Vector3> Side(KwSailLayout s, float f, float u, bool rising)
        {
            List<Vector3> side = new List<Vector3>();
            float shown = f * s.Height;
            for (int j = Up(s); j >= 1 && shown > 0.01f; j--)
                side.Add(Square(s, u, (float)j / Up(s), shown));
            side.AddRange(KwSailDrape.Side(s, f, u));
            if (rising)
                side.Reverse();
            return side;
        }

        /// <summary>The square sail's lower corners (its clews, at the feet of its hanging sides) at that hoist and the layout's fill.</summary>
        public static Vector3[] Corners(KwSailLayout s, float f)
        {
            List<Vector3> foot = KwSailDrape.Foot(s, f);
            return new[] { foot[0], foot[foot.Count - 1] };
        }

        /// <summary>
        /// The clews where the sheets hold them at that hoist, whatever the wind: as the cloth lies slack. The sheets hold the
        /// cloth there, so the wind does not move them (<see cref="KwSailPoints"/>), and the sheets end there.
        /// </summary>
        public static Vector3[] Clews(KwSailLayout s, float f)
        {
            float fill = s.Fill;
            s.Fill = 0f;
            Vector3[] clews = Corners(s, f);
            s.Fill = fill;
            return clews;
        }

        /// <summary>A sail along the ship's head (the gaff's throat for a gaff sail) and the gaff's peak, at that hoist.</summary>
        public static (Vector3 Head, Vector3 Peak) Top(KwSailLayout s, float f)
        {
            float shown = f * s.Height, gathered = s.Bundle(f);
            float head = s.Spec.Shape == 2 ? gathered + shown * Gaff(0f) : gathered + shown;
            return (new Vector3(0f, 0f, head), new Vector3(-s.Width, 0f, gathered + shown));
        }
    }
}
