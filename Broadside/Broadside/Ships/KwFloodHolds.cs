using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The water in a launched ship's hold, compartment by compartment (<see cref="KwBulkheads"/>): each compartment holds
    /// its own volume under its own flat surface (<see cref="KwFloodLevel"/>), all of them lying the same way up. Across
    /// the ship that is level, a little late (<see cref="KwShipFlood"/>), so the water sloshes across and lists the ship;
    /// along it the surface follows only <see cref="Along"/> of the ship's pitch, as the frames, floors and stores of a
    /// hold keep its water from running fore and aft, since water free to run the whole length made a flooded ship dive
    /// by the head. Each hole lets the sea into its own compartment (<see cref="Leak"/>, about an eighth of a real hole's
    /// flow). Through a bulkhead water passes slowly to the neighbour where it stands lower (<see cref="Pass"/>): it
    /// seeps through the frames' gaps under its top (the design waterline) and pours over the top when it stands higher
    /// (<see cref="KwFloodWall.Opening"/>), so a holed compartment fills first and trims the ship by its end, and the
    /// water evens out along the hold over minutes. Pure arithmetic in the ship's frame: the owner runs it, every
    /// screen works out the same surfaces from the volumes the owner writes (<see cref="Text"/>).
    /// </summary>
    public sealed class KwFloodHolds
    {
        /// <summary>Cubic metres: less than this in a compartment is none.</summary>
        public const float Dry = 1e-4f;

        /// <summary>How much of the ship's pitch the water's surface follows along the ship (all of its heel across).</summary>
        public const float Along = 0.2f;

        // the holes' share of a real one's flow; a bulkhead's wet wall's share that lets water seep through and the share
        // of the gap over its top it pours through; how small a head flows in proportion; the step the surface's area is
        // measured over; how much of the way to even a bulkhead passes water at once; the least surface area
        private const float LeakShare = 0.08f, Seep = 0.003f, Over = 0.02f, Linear = 0.02f, Probe = 0.01f, Ease = 0.5f, Least = 0.05f;
        private static readonly CultureInfo Plain = CultureInfo.InvariantCulture;

        private readonly List<KwHullSlice> slices;
        private readonly int[] starts;
        private readonly float[] capacity, volume, level, surface;
        private readonly KwFloodWall[] walls;

        public KwFloodHolds(List<KwHullSlice> slices, int[] starts, float top)
        {
            (this.slices, this.starts) = (slices, starts);
            int n = starts.Length - 1;
            (capacity, volume, level, surface) = (new float[n], new float[n], new float[n], new float[n]);
            (Areas, Middles) = (new float[slices.Count], new Vector2[slices.Count]);
            for (int k = 0; k < n; k++)
                (capacity[k], surface[k]) = (KwFloodLevel.Capacity(slices, starts[k], Count(k)), Least);
            Capacity = Sum(capacity);
            walls = new KwFloodWall[Mathf.Max(0, n - 1)];
            for (int b = 0; b + 1 < n; b++)
                walls[b] = KwFloodWall.Before(slices, starts[b + 1], top);
        }

        /// <summary>How many compartments.</summary>
        public int Holds => capacity.Length;

        /// <summary>How much the whole hold holds up to the sheer, cubic metres.</summary>
        public float Capacity { get; }

        /// <summary>The water in the whole hold, cubic metres.</summary>
        public float Total => Sum(volume);

        /// <summary>The way up every compartment's surface lies, in the ship's frame (up·p = level).</summary>
        public Vector3 Up { get; private set; } = Vector3.up;

        /// <summary>Each slice's water: its area across the slice and the middle of it (x across, y up), zero where dry.</summary>
        public float[] Areas { get; }

        public Vector2[] Middles { get; }

        public List<KwHullSlice> Slices => slices;

        public bool Wet(int k) => volume[k] > Dry;

        public bool AnyWet => Total > Dry;

        public float VolumeOf(int k) => volume[k];

        public float CapacityOf(int k) => capacity[k];

        /// <summary>A wet compartment's surface: up·p = this, in the ship's frame.</summary>
        public float LevelOf(int k) => level[k];

        public int First(int k) => starts[k];

        public int Count(int k) => starts[k + 1] - starts[k];

        /// <summary>Where a compartment begins and ends along the ship.</summary>
        public float Aft(int k) => KwBulkheads.Edge(slices, starts[k]);

        public float Fore(int k) => KwBulkheads.Edge(slices, starts[k + 1]);

        /// <summary>The compartment at a place along the ship (the end ones beyond the hull's ends).</summary>
        public int At(float z)
        {
            for (int k = 0; k + 1 < Holds; k++)
            {
                if (z < Fore(k))
                    return k;
            }
            return Holds - 1;
        }

        /// <summary>Water into a compartment (out, less than nothing), kept within what it holds.</summary>
        public void Add(int k, float water)
        {
            if (k >= 0 && k < Holds)
                volume[k] = Mathf.Clamp(volume[k] + water, 0f, capacity[k]);
        }

        /// <summary>
        /// The water through a hole in a second, into its own compartment: in while the sea outside stands higher over it
        /// (<paramref name="outside"/>, metres) than the water inside does, out the other way round (√(2 g h)).
        /// </summary>
        public float Leak(KwHullHole hole, float outside, float g)
        {
            int k = At(hole.Place.z);
            float inside = Wet(k) ? Mathf.Max(0f, level[k] - Vector3.Dot(Up, hole.Place)) : 0f;
            float head = outside - inside;
            return Mathf.Sign(head) * LeakShare * hole.Area * Mathf.Sqrt(2f * g * Mathf.Abs(head));
        }

        /// <summary>
        /// Every compartment's surface worked out again for its volume, lying level across the ship's frame as
        /// <paramref name="up"/> (the world's up, a little late) does and following <see cref="Along"/> of its pitch;
        /// first, for <paramref name="dt"/> seconds (none on a screen that only draws the owner's volumes), the water
        /// through the bulkheads passed on.
        /// </summary>
        public void Resolve(Vector3 up, float dt, float g)
        {
            if (dt > 0f)
                Pass(dt, g);
            Up = new Vector3(up.x, up.y, Along * up.z).normalized;
            for (int k = 0; k < Holds; k++)
                Settle(k);
        }

        /// <summary>
        /// One volume for the whole hold as the ship saved it before it had compartments: spread at one level, as it lay.
        /// </summary>
        public void Spread(float total)
        {
            total = Mathf.Clamp(total, 0f, Capacity);
            float d = KwFloodLevel.Solve(slices, Vector3.up, total), sum = 0f;
            for (int k = 0; k < Holds; k++)
                sum += volume[k] = KwFloodLevel.Volume(slices, starts[k], Count(k), Vector3.up, d);
            for (int k = 0; k < Holds && sum > Dry; k++)
                volume[k] = Mathf.Min(volume[k] * total / sum, capacity[k]);
        }

        /// <summary>The compartments' volumes as the ship's ZDO keeps them: cubic metres, aft to fore, ','-separated.</summary>
        public string Text()
        {
            StringBuilder text = new StringBuilder();
            for (int k = 0; k < Holds; k++)
                text.Append(k > 0 ? "," : "").Append(volume[k].ToString("0.###", Plain));
            return text.ToString();
        }

        /// <summary>The volumes from the ZDO's text; false (nothing changed) when it is not one number a compartment.</summary>
        public bool Read(string? text)
        {
            string[] fields = (text ?? "").Split(',');
            if (fields.Length != Holds)
                return false;
            float[] read = new float[Holds];
            for (int k = 0; k < Holds; k++)
            {
                if (!float.TryParse(fields[k], NumberStyles.Float, Plain, out read[k]))
                    return false;
            }
            for (int k = 0; k < Holds; k++)
                volume[k] = Mathf.Clamp(read[k], 0f, capacity[k]);
            return true;
        }

        // A compartment's surface for its volume, its slices' water, and how wide its surface is (the volume a little
        // higher level would hold more, over that step); a dry one's slices cleared.
        private void Settle(int k)
        {
            int first = starts[k], count = Count(k);
            if (!Wet(k))
            {
                System.Array.Clear(Areas, first, count);
                surface[k] = Least;
                return;
            }
            level[k] = KwFloodLevel.Solve(slices, first, count, Up, volume[k]);
            KwFloodLevel.Shares(slices, first, count, Up, level[k], Areas, Middles);
            float higher = KwFloodLevel.Volume(slices, first, count, Up, level[k] + Probe);
            surface[k] = Mathf.Max(Least, (higher - volume[k]) / Probe);
        }

        // Through every bulkhead, from the side where the water stands higher at it to the other, by the difference
        // (√(2 g h), in proportion under Linear), through the bulkhead's opening for the higher side's water
        // (KwFloodWall.Opening); never more than half the way to even at once.
        private void Pass(float dt, float g)
        {
            for (int b = 0; b + 1 < Holds; b++)
            {
                float aft = Height(b, b), fore = Height(b, b + 1), head = Mathf.Abs(aft - fore);
                if (head <= 0f)
                    continue;
                int from = aft > fore ? b : b + 1, to = 2 * b + 1 - from;
                float opening = walls[b].Opening(Mathf.Max(aft, fore), Seep, Over);
                float flow = opening * Mathf.Sqrt(2f * g * Mathf.Max(head, Linear)) * Mathf.Min(head / Linear, 1f);
                float even = head / (1f / surface[from] + 1f / surface[to]);
                float moved = Mathf.Min(flow * dt, Mathf.Min(Ease * even, volume[from]));
                volume[from] -= moved;
                volume[to] = Mathf.Min(volume[to] + moved, capacity[to]);
            }
        }

        // How high a compartment's water stands at a bulkhead, over the bulkhead's foot (none when dry or lower).
        private float Height(int b, int k) =>
            Wet(k) ? Mathf.Max(0f, level[k] - (Up.y * walls[b].Foot + Up.z * walls[b].Z)) : 0f;

        private static float Sum(float[] values)
        {
            float sum = 0f;
            foreach (float v in values)
                sum += v;
            return sum;
        }
    }
}
