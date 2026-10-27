using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// An opening's outline in its wall's own measures (u along, z up), anticlockwise seen from outside, always convex: a
    /// doorway, a square or a rectangle its four corners; a round porthole a circle of <see cref="RoundSides"/> sides; an
    /// arched top a rectangle under a half circle as wide. Grown by a margin, each grows evenly all round (the frame's
    /// outer edge, the gap kept to a neighbour). The tools' limits for each shape, and which covers each takes. Pure.
    /// </summary>
    public static class KwOpeningShape
    {
        /// <summary>A round port's sides, and the arch's half circle's.</summary>
        public const int RoundSides = 20, ArchSides = 10;

        /// <summary>A doorway's width and height limits (the Doorway tool), and its first sizes.</summary>
        public const float DoorMinWide = 1.2f, DoorMaxWide = 2.4f, DoorMinHigh = 1.6f, DoorMaxHigh = 2.4f, DoorWide = 1.3f, DoorHigh = 1.9f;

        /// <summary>A window's or port's least and most width, height or diameter (the Window tool).</summary>
        public const float Least = 0.25f, Most = 1.5f;

        public static bool Known(string shape) =>
            shape == KwOpening.Door || shape == KwOpening.Round || shape == KwOpening.Square || shape == KwOpening.Rect || shape == KwOpening.Arch;

        /// <summary>Whether a shape takes a cover: a doorway none, a door or a double door; a round port none or its lid; the others none, a lid up or down, or shutters.</summary>
        public static bool Covers(string shape, string cover) => shape switch
        {
            KwOpening.Door => cover == KwOpening.None || cover == KwOpening.Leaf || cover == KwOpening.DoubleDoor,
            KwOpening.Round => cover == KwOpening.None || cover == KwOpening.Lid,
            _ => cover == KwOpening.None || cover == KwOpening.Up || cover == KwOpening.Down || cover == KwOpening.Shutters,
        };

        /// <summary>
        /// The outline grown by a margin all round: its own (<see cref="Own"/>) set onto its wall's face at its true size
        /// when the opening is fitted to a wall (<see cref="KwOpening.Surface"/>, <see cref="KwOpeningSurface.Fit"/>).
        /// </summary>
        public static List<Vector2> Outline(KwOpening o, float grow)
        {
            List<Vector2> own = Own(o, grow);
            return o.Surface is KwOpeningSurface wall ? own.ConvertAll(p => wall.Fit(o, p)) : own;
        }

        /// <summary>The outline in true metres, laid about the opening's u and sill as if its wall were flat.</summary>
        public static List<Vector2> Own(KwOpening o, float grow)
        {
            float u0 = o.U0 - grow, u1 = o.U1 + grow, z0 = o.Sill - grow, z1 = o.Top + grow;
            if (o.Shape == KwOpening.Round)
                return Circle(new Vector2(o.U, o.Sill + o.Width / 2f), o.Width / 2f + grow, RoundSides, 0f, 360f);
            if (o.Shape != KwOpening.Arch || o.Height < o.Width / 2f + 0.01f)
                return new List<Vector2> { new Vector2(u0, z0), new Vector2(u1, z0), new Vector2(u1, z1), new Vector2(u0, z1) };
            float r = o.Width / 2f + grow;
            Vector2 centre = new Vector2(o.U, o.Top - o.Width / 2f);
            List<Vector2> outline = new List<Vector2> { new Vector2(u0, z0), new Vector2(u1, z0) };
            outline.AddRange(Circle(centre, r, ArchSides, 0f, 180f, true));
            return outline;
        }

        // Points round a circle from one angle to another (degrees, anticlockwise from +u), the last one left out unless asked
        // (a whole circle's last point is its first).
        private static List<Vector2> Circle(Vector2 centre, float r, int sides, float from, float to, bool last = false)
        {
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i < sides + (last ? 1 : 0); i++)
            {
                float a = (from + (to - from) * i / sides) * Mathf.Deg2Rad;
                points.Add(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            return points;
        }

        /// <summary>The opening's area in m² (for the water it lets in).</summary>
        public static float Area(KwOpening o) => Mathf.Abs(Signed(Own(o, 0f)));

        /// <summary>A polygon's signed area (anticlockwise positive).</summary>
        public static float Signed(List<Vector2> ring)
        {
            float twice = 0f;
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], c = ring[(i + 1) % ring.Count];
                twice += a.x * c.y - c.x * a.y;
            }
            return twice / 2f;
        }

        /// <summary>The outline's reach along (u0, u1) and up (z0, z1).</summary>
        public static Rect Bounds(List<Vector2> ring)
        {
            float u0 = float.MaxValue, u1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
            foreach (Vector2 p in ring)
                (u0, u1, z0, z1) = (Mathf.Min(u0, p.x), Mathf.Max(u1, p.x), Mathf.Min(z0, p.y), Mathf.Max(z1, p.y));
            return Rect.MinMaxRect(u0, z0, u1, z1);
        }
    }
}
