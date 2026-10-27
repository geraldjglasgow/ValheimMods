using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A point of a sail's cloth (or of its bolt rope) as the wind moves it (<see cref="KwSailNow.At"/>): where it sits
    /// slack and how far it goes from there to full, how much it billows, flaps and swings, and where it is on the sail.
    /// </summary>
    public struct KwSailPoint
    {
        /// <summary>Where it is slack, and how far it goes from there to full (the cloth's own axes, Unity's).</summary>
        public Vector3 Rest, Delta;

        /// <summary>
        /// How much it billows (with the belly: none on the yard or the boom, all of it where the belly is deepest), how
        /// much it flaps (most at the free edges, none where the cloth is held), and how far it swings with the ship per
        /// unit of the cloth's lean (a square sail's: the metres it hangs below its yard).
        /// </summary>
        public float Belly, Flap, Hang;

        /// <summary>
        /// Where it is on the sail, in metres: across it (aft from the mast for a sail along the ship), out from its
        /// middle either way (rounded off at the middle), and down from its head.
        /// </summary>
        public float Across, Wide, Down;
    }

    /// <summary>
    /// The points of a sail's cloth at a hoist, from where they sit slack and full (<see cref="KwSailCanvas"/>). A square
    /// sail is held by its yard along its head and by its sheets at its clews (which the wind does not move at all, however
    /// full it blows the cloth: they stay where the sheets end, <see cref="KwSailCanvas.Clews"/>); its sides and its foot
    /// are free, and the cloth hauled up under it hangs free too. A sail along the ship is held by the mast along its leading edge, by the
    /// boom along its foot and by its halyard (and gaff) at its head; its after edge is free, and the cloth lowered onto
    /// its boom is held there. Kit axes as <see cref="KwSailLayout"/>.
    /// </summary>
    public static class KwSailPoints
    {
        /// <summary>The points at hoist f, against the deepest any of the cloth's points goes from slack to full (most).</summary>
        public static KwSailPoint[] Of(KwSailLayout s, float f, Vector3[] slack, Vector3[] full, float most)
        {
            KwSailPoint[] points = new KwSailPoint[Mathf.Min(slack.Length, full.Length)];
            Vector3[] clews = s.Square ? KwSailCanvas.Corners(s, f) : new Vector3[0];
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 kit = KwMesh.ToKit(slack[i]), delta = full[i] - slack[i];
                float free = s.Square ? 1f - Held(s, kit, clews) : 1f;
                KwSailPoint p = s.Square ? Square(s, f, kit, free) : Along(s, f, kit);
                p.Rest = slack[i];
                p.Delta = delta * free;
                p.Belly = (most > 0.001f ? Mathf.Clamp01(delta.magnitude / most) : 0f) * free;
                p.Wide = Mathf.Sqrt(p.Across * p.Across + 0.25f) - 0.5f;
                points[i] = p;
            }
            return points;
        }

        /// <summary>The deepest any point goes from slack to full.</summary>
        public static float Most(Vector3[] slack, Vector3[] full)
        {
            float most = 0f;
            for (int i = 0; i < Mathf.Min(slack.Length, full.Length); i++)
                most = Mathf.Max(most, (full[i] - slack[i]).magnitude);
            return most;
        }

        // A square sail's point: it flaps more the further down it is and the nearer a side; the foot of the cloth set
        // flaps most, the cloth hauled up under it less the deeper it hangs; it swings with the depth it hangs from its yard.
        private static KwSailPoint Square(KwSailLayout s, float f, Vector3 kit, float free)
        {
            float shown = Mathf.Max(f * s.Height, 0.1f), down = Mathf.Max(0f, -kit.z), d = down / shown;
            float a = Mathf.Clamp01(Mathf.Abs(kit.y) / Mathf.Max(s.Width / 2f, 0.1f));
            float flap = d <= 1f
                ? Mathf.Pow(d, 1.3f) * (0.45f + 0.55f * Mathf.Max(a * a * a * a, d * d * d))
                : 1f - 0.6f * Mathf.Clamp01((down - shown) / 0.8f);
            return new KwSailPoint { Flap = flap * free, Hang = down * free, Across = kit.y, Down = down };
        }

        // How firmly the sheets hold a point: wholly at a clew, hardly at all a little way off.
        private static float Held(KwSailLayout s, Vector3 kit, Vector3[] clews)
        {
            float reach = 0.12f * Mathf.Min(s.Width, s.Height) + 0.25f, held = 0f;
            foreach (Vector3 clew in clews)
                held = Mathf.Max(held, Mathf.Exp(-new Vector2(kit.y - clew.y, kit.z - clew.z).sqrMagnitude / (reach * reach)));
            return held;
        }

        // A sail along the ship's point: free most at its after edge halfway up, not at all on the mast, the boom or the
        // head; what lies on the boom is held.
        private static KwSailPoint Along(KwSailLayout s, float f, Vector3 kit)
        {
            float shown = f * s.Height, gathered = s.Bundle(f), aft = Mathf.Max(0f, -kit.x);
            if (shown <= 0.01f || kit.z < gathered - 0.005f)
                return new KwSailPoint { Across = aft };
            bool gaff = s.Spec.Shape == 2;
            float rise = kit.z - gathered, u = Mathf.Clamp01(aft / Mathf.Max(s.Width, 0.1f));
            float v = Mathf.Clamp01(gaff ? rise / (shown * KwSailCanvas.Gaff(u)) : rise / shown);
            u = gaff ? u : Mathf.Clamp01(aft / Mathf.Max(s.Width * (1f - v), 0.05f));
            float free = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * v)), 0.6f) * Mathf.Pow(u, 1.3f);
            float head = gathered + (gaff ? shown * KwSailCanvas.Gaff(u) : shown);
            return new KwSailPoint { Flap = free, Hang = free * 0.5f * Mathf.Min(s.Width, shown), Across = aft, Down = Mathf.Max(0f, head - kit.z) };
        }
    }
}
