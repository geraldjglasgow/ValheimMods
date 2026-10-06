using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// The latest estimate of the brush click the local player would make now, shared by the changed-points preview and
    /// the cost line so the planners run once for both. It is reused while the edit is the same to the centimetre and
    /// younger than <see cref="MaxAge"/> (so edits by anyone show up within a second; the player's own edits drop it at
    /// once), and holds as many per-vertex changes as the preview asked for last (<see cref="PreferredChanges"/>),
    /// whoever asks first. The click's own cost check reads it too, when it is younger than <see cref="ClickAge"/>; the
    /// cost line takes the latest one of the same entry and brush wherever it stood (<see cref="Recent"/>).
    /// </summary>
    public static class LiveEstimate
    {
        public const float MaxAge = 1f;

        /// <summary>How old an estimate the click's cost check may use (the ground may change under a held click).</summary>
        public const float ClickAge = 0.25f;

        /// <summary>How many per-vertex changes the preview draws (0 while it shows none); set by the Preview module.</summary>
        public static int PreferredChanges;

        private static readonly EditKey key = new EditKey();
        private static EditEstimate last;
        private static int lastLimit;
        private static float madeAt = -10f;

        /// <summary>The estimate of the edit with at least <paramref name="changeLimit"/> per-vertex changes listed.</summary>
        public static EditEstimate For(TerrainEdit edit, int changeLimit, float maxAge = MaxAge)
        {
            if (last != null && lastLimit >= changeLimit && Time.time - madeAt < maxAge && key.Matches(edit, false))
                return last;
            int limit = Mathf.Max(changeLimit, PreferredChanges);
            last = Engine.Estimate(edit, limit);
            lastLimit = limit;
            madeAt = Time.time;
            key.Set(edit);
            return last;
        }

        /// <summary>
        /// For a line that refreshes on its own (the cost line): the latest estimate of the same entry and brush values
        /// when younger than <paramref name="maxAge"/>, wherever the brush stood then, else a new one. A moving brush is
        /// then planned once for both the preview and the cost line.
        /// </summary>
        public static EditEstimate Recent(TerrainEdit edit, float maxAge)
        {
            if (last != null && Time.time - madeAt < maxAge && key.Matches(edit, true))
                return last;
            return For(edit, 0);
        }

        internal static void Initialize() => EditEvents.Sent += edit => Clear();

        /// <summary>Forgets the kept estimate (the next request plans again).</summary>
        public static void Clear() => last = null;
    }

    /// <summary>
    /// The values of a brush edit that decide its estimate, kept to the centimetre so hand jitter does not count:
    /// source, flags and every stroke value. Compared without allocating.
    /// </summary>
    internal sealed class EditKey
    {
        private const int Count = 23;

        private readonly int[] values = new int[Count];
        private readonly int[] scratch = new int[Count];
        private string source;
        private bool set;

        public void Set(TerrainEdit edit)
        {
            set = Fill(values, edit);
            source = edit?.Source;
        }

        /// <summary>The edit has the same values; <paramref name="anyPlace"/> ignores where it stands (centre and target height).</summary>
        public bool Matches(TerrainEdit edit, bool anyPlace)
        {
            if (!set || !Fill(scratch, edit) || edit.Source != source)
                return false;
            for (int i = 0; i < Count; i++)
            {
                if (scratch[i] != values[i] && !(anyPlace && IsPlace(i)))
                    return false;
            }
            return true;
        }

        private static bool IsPlace(int index) => (index >= 6 && index <= 8) || index == 13;

        /// <summary>False for an edit that is not a brush stroke (never matched).</summary>
        private static bool Fill(int[] v, TerrainEdit edit)
        {
            BrushStroke s = edit?.Stroke;
            if (s == null || edit.Kind != EditKind.Stroke)
                return false;
            // A click's edit is the preview's plus the placement flag, which changes nothing in the plan.
            v[0] = (int)(edit.Flags & ~EditFlags.FromPlacement);
            v[1] = (int)s.Shape;
            v[2] = (int)s.Height;
            v[3] = (int)s.Style;
            v[4] = (int)s.Paint;
            // Every built stroke gets a new seed; it decides only which points a random share picks.
            v[5] = s.RandomShare < 1f ? s.Seed : 0;
            FillSizes(v, s);
            return true;
        }

        private static void FillSizes(int[] v, BrushStroke s)
        {
            v[6] = Cm(s.Center.x);
            v[7] = Cm(s.Center.y);
            v[8] = Cm(s.Center.z);
            v[9] = Cm(s.Radius);
            v[10] = Cm(s.Radius2);
            v[11] = Cm(s.Rotation);
            v[12] = Cm(s.Hardness);
            v[13] = Cm(s.Target);
            v[14] = Cm(s.Amount);
            v[15] = Cm(s.MaxStep);
            v[16] = Cm(s.Strength);
            v[17] = Cm(s.EffectivePaintRadius);
            v[18] = Cm(s.PaintStrength);
            v[19] = Cm(s.Density);
            v[20] = Cm(s.BandMin);
            v[21] = Cm(s.BandMax);
            v[22] = Cm(s.RandomShare);
        }

        private static int Cm(float value) => Mathf.RoundToInt(value * 100f);
    }
}
