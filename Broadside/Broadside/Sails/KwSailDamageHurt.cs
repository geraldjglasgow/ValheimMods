using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's sail as its rig's damage leaves it (<see cref="KwRigCondition.Sail"/>): which of its fastenings are
    /// broken (a square sail's lashings to its yard, a sail along the ship's hoops up the mast and lacing to its boom), where
    /// its yard, boom or gaff is broken through and which end of it is gone, the share of it that droops for that
    /// (<see cref="Droop"/>) and the share its holes take (<see cref="Holed"/>).
    /// </summary>
    public sealed class KwSailHurt
    {
        public static readonly KwSailHurt Whole = new KwSailHurt();

        /// <summary>Broken fastenings, each true where it is gone, in <see cref="KwRigPiece.Lashings"/>' (or hoops', lacing's) order.</summary>
        public bool[] Lashings { get; set; } = new bool[0];
        public bool[] Hoops { get; set; } = new bool[0];
        public bool[] Lacing { get; set; } = new bool[0];

        /// <summary>Where along its yard a square sail's yard is broken through (-1 whole), and which end went: 1 its far end (larger along), -1 its near one.</summary>
        public float YardBreak { get; set; } = -1f;
        public int YardSide { get; set; } = 1;

        /// <summary>Where along its boom a sail along the ship's boom is broken through, its after part gone (-1 whole).</summary>
        public float BoomBreak { get; set; } = -1f;

        /// <summary>The share along a gaff sail's gaff where it is broken through, its outer part gone (-1 whole).</summary>
        public float GaffBreak { get; set; } = -1f;

        /// <summary>The share of the sail drooping, slack, giving no drive (0 to 1).</summary>
        public float Droop { get; set; }

        /// <summary>The share of the sail's area its holes take.</summary>
        public float Holed { get; set; }

        /// <summary>Whether anything holding it is broken.</summary>
        public bool Bent => YardBreak >= 0f || BoomBreak >= 0f || GaffBreak >= 0f || Any(Lashings) || Any(Hoops) || Any(Lacing);

        /// <summary>What its droop is drawn from, for knowing when to draw it again.</summary>
        public string Key
        {
            get
            {
                StringBuilder key = new StringBuilder();
                foreach (bool[] row in new[] { Lashings, Hoops, Lacing })
                {
                    foreach (bool gone in row)
                        key.Append(gone ? '1' : '0');
                    key.Append('|');
                }
                return key.Append(YardBreak.ToString("0.00")).Append(YardSide).Append(BoomBreak.ToString("0.00")).Append(GaffBreak.ToString("0.00")).ToString();
            }
        }

        private static bool Any(bool[] row) => System.Array.IndexOf(row, true) >= 0;
    }

    /// <summary>
    /// How a sail's damage is read (<see cref="KwSailHurt"/>): a fastening is broken when a notch that cut at least
    /// <see cref="Least"/> of its spar's section lies within the notch's half-length and <see cref="Reach"/> of it, or when its
    /// spar is broken through on its side (a yard's end away from the break's nearer middle, a boom's and gaff's outer
    /// part). Droop: a square sail's runs of broken lashings, the whole share at an end (its corner falls), half between
    /// lashings still holding (it sags); a sail along the ship by its broken hoops, lacing and spars.
    /// </summary>
    internal static class KwSailHurtRead
    {
        public const float Reach = 0.1f, Least = 0.12f;

        public static KwSailHurt Of(KwRigCondition c, KwRigPiece mast, int i)
        {
            KwSailLayout s = mast.Sails[i];
            KwSailHurt hurt = new KwSailHurt();
            if (s.Square)
                Square(c, mast.Index, i, s, hurt);
            else
                Along(c, mast.Index, i, s, hurt);
            hurt.Holed = Holed(c.Holes(mast.Index, i), s.Spec.Area);
            hurt.Droop = s.Square ? SquareDroop(hurt.Lashings) : AlongDroop(hurt);
            return hurt;
        }

        private static void Square(KwRigCondition c, int piece, int i, KwSailLayout s, KwSailHurt hurt)
        {
            float? broken = c.BrokenAt(piece, i);
            KwRigSpar yard = KwRigPiece.Yard(s, i);
            hurt.YardBreak = broken ?? -1f;
            hurt.YardSide = broken.HasValue && broken.Value < yard.Length / 2f ? -1 : 1;
            hurt.Lashings = Broken(KwRigPiece.Lashings(s), c.Notches(piece, i), broken, hurt.YardSide);
        }

        private static void Along(KwRigCondition c, int piece, int i, KwSailLayout s, KwSailHurt hurt)
        {
            float? boom = c.BrokenAt(piece, KwRigSpar.Boom + i), gaff = c.BrokenAt(piece, KwRigSpar.Gaff + i);
            hurt.Hoops = Broken(KwRigPiece.Hoops(s), c.Notches(piece, KwRigSpar.Column), null, 0);
            hurt.Lacing = Broken(KwRigPiece.Lacing(s), c.Notches(piece, KwRigSpar.Boom + i), boom, 1);
            hurt.BoomBreak = boom ?? -1f;
            if (gaff.HasValue && s.Spec.Shape == 2)
                hurt.GaffBreak = Mathf.Clamp01(gaff.Value / Mathf.Max(KwRigPiece.GaffOf(s, i, 1f).Length, 0.1f));
        }

        // Each fastening at its place along its spar: broken near a deep enough notch, or past where the spar broke through.
        private static bool[] Broken(List<float> at, List<KwRigMark> cuts, float? through, int side)
        {
            bool[] gone = new bool[at.Count];
            for (int k = 0; k < at.Count; k++)
                gone[k] = Near(at[k], cuts) || (through.HasValue && side * (at[k] - through.Value) > 0f);
            return gone;
        }

        private static bool Near(float s, List<KwRigMark> cuts) => cuts.Exists(n => n.C >= Least && Mathf.Abs(s - n.A) < n.D + Reach);

        // A square sail's runs of broken lashings: at an end the whole run's share, between holding lashings half of it.
        private static float SquareDroop(bool[] gone)
        {
            float droop = 0f;
            int n = gone.Length;
            for (int k = 0; k < n;)
            {
                if (!gone[k])
                {
                    k++;
                    continue;
                }
                int start = k;
                while (k < n && gone[k])
                    k++;
                droop += (k - start) / (float)n * (start == 0 || k == n ? 1f : 0.5f);
            }
            return Mathf.Clamp01(droop);
        }

        // A sail along the ship: half the share of its hoops broken, three tenths of its lacing's, and what is outside a broken gaff or boom.
        private static float AlongDroop(KwSailHurt hurt)
        {
            float droop = 0.5f * Share(hurt.Hoops) + 0.3f * Share(hurt.Lacing);
            droop += hurt.GaffBreak >= 0f ? 0.6f * (1f - hurt.GaffBreak) : 0f;
            return Mathf.Clamp01(droop + (hurt.BoomBreak >= 0f ? 0.25f : 0f));
        }

        private static float Share(bool[] gone)
        {
            int count = 0;
            foreach (bool g in gone)
                count += g ? 1 : 0;
            return gone.Length > 0 ? count / (float)gone.Length : 0f;
        }

        // The holes' area against the sail's, at most nine tenths of it.
        private static float Holed(List<KwRigMark> holes, float area)
        {
            float lost = 0f;
            foreach (KwRigMark hole in holes)
                lost += Mathf.PI * hole.C * hole.C;
            return Mathf.Min(0.9f, lost / Mathf.Max(area, 0.5f));
        }
    }
}
