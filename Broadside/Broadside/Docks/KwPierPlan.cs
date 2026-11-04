using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dock as the Dock tool would build it, on the placing player's game: from a start (<see cref="KwPierStart"/>)
    /// toward an end, at a width and in a material. Its length is the level distance to the end (along the way a start on a
    /// dock fixes), to the half-metre, <see cref="MinLength"/> to <see cref="MaxLength"/> (a longer aim is cut there); its
    /// deck is level at the start's height. Where its line runs over other docks (<see cref="KwPierMeet"/>) it is laid
    /// around them: a run up to each one's near edge and the next from its far edge, and an end aimed on (or just short of)
    /// a dock stops exactly at its near edge, not rounded (user, 2026-11-03: "we should allow docks to connect"). Each run
    /// is laid as equal sections of at most <see cref="MaxSection"/>, each a piece of its own at its own place along the
    /// line (<see cref="Offsets"/>), the first joined when it starts on a dock and the others joined to the one before. Each section's
    /// pilings are found by casts straight down to the ground or rock (never the water or a piece) and sunk
    /// <see cref="Embed"/> into it. It is refused, and why is kept (<see cref="Why"/>), when its deck would be less than
    /// <see cref="KwPierAim.MinOverWater"/> over the sea, more than <see cref="MaxHigh"/> over the water or ground at a
    /// station, a piling longer than <see cref="MaxPiling"/>, the ground would rise through it, another dock is in the way,
    /// or it would reach into a no-build place or a ward the player may not use (<see cref="KwPierChecks"/>). Its cost is
    /// its sections' (<see cref="KwPierCost"/>). Nothing is sent or built here.
    /// </summary>
    public sealed class KwPierPlan
    {
        public const float Embed = 0.3f, MinLength = 2f, MaxLength = 40f, MaxSection = 12f, MaxPiling = 20f, MaxHigh = 8f, Step = 0.5f, Degree = 1f;
        private const float MinRun = 1f;
        private const float Above = 1.5f;
        private static readonly int Ground = LayerMask.GetMask("terrain", "static_solid", "Default", "Default_small");

        private KwPierPlan(KwPierStart start, Vector3 way, float width, KwPierKind kind)
        {
            (At, Way, Width, Kind, Joined) = (start.At, way, width, kind, start.OnDock);
        }

        public Vector3 At { get; }
        public Vector3 Way { get; }
        public Quaternion Turn => Quaternion.LookRotation(Way);
        public Vector3 Right => Vector3.Cross(Vector3.up, Way);
        public float Width { get; }
        public KwPierKind Kind { get; }
        public bool Joined { get; }
        public float Length { get; private set; }

        /// <summary>Whether the aim reached past <see cref="MaxLength"/> and the dock was cut there.</summary>
        public bool Cut { get; private set; }

        public List<KwPierSpec> Sections { get; } = new List<KwPierSpec>();

        /// <summary>Each section's start, in metres along the line from <see cref="At"/>.</summary>
        public List<float> Offsets { get; } = new List<float>();

        /// <summary>How many other docks it crosses on the way (it joins each on both sides).</summary>
        public int Crosses { get; private set; }

        /// <summary>Where section <paramref name="index"/> starts, at the deck's top.</summary>
        public Vector3 SectionAt(int index) => At + Way * Offsets[index];
        public int Cost { get; private set; }

        /// <summary>Why it may not be built (the first reason found); null when it may.</summary>
        public string? Why { get; private set; }

        public void Refuse(string why) => Why ??= why;

        /// <summary>
        /// The dock from a start to an end; flash: the click (a ward refusing it flashes, as the game's do); stub: the short
        /// dock shown before the first click, refused only for its start (what lies ahead of it is the second click's).
        /// </summary>
        public static KwPierPlan Make(KwPierStart start, Vector3 end, float width, KwPierKind kind, int seed, bool flash, bool stub = false)
        {
            Vector3 way = start.Way ?? Flat(end - start.At);
            KwPierPlan plan = new KwPierPlan(start, way.sqrMagnitude > 1e-6f ? way : Vector3.forward, width, kind);
            float length = start.Way != null ? Vector3.Dot(end - start.At, plan.Way) : new Vector2(end.x - start.At.x, end.z - start.At.z).magnitude;
            List<Vector2> crossed = KwPierMeet.Crossings(start, plan.Way, width, Mathf.Min(length, MaxLength) + KwPierMeet.Reach);
            plan.Measure(length, crossed);
            if (start.Why != null)
                plan.Refuse(start.Why);
            plan.Lay(seed, crossed);
            KwPierChecks.Run(plan, flash);
            plan.Cost = KwPierCost.Amount(plan.Sections);
            if (stub)
                plan.Why = start.Why;
            return plan;
        }

        /// <summary>
        /// The end a dock is built to from an aimed one: its length to the half-metre and, from a start on the ground, its
        /// way to the degree (the preview and the click alike, so the preview is made again only at each step).
        /// </summary>
        public static Vector3 Snap(KwPierStart start, Vector3 end)
        {
            Vector3 d = end - start.At;
            if (start.Way != null)
                return start.At + start.Way.Value * (Mathf.Round(Vector3.Dot(d, start.Way.Value) / Step) * Step);
            float angle = Mathf.Round(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg / Degree) * Degree;
            float length = Mathf.Round(new Vector2(d.x, d.z).magnitude / Step) * Step;
            return start.At + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * length;
        }

        // The length to the half-metre, cut at the longest, or to the near edge of a dock its end is on or just short of;
        // one too short is laid at the shortest, refused (an end on a dock: any run of at least MinRun).
        private void Measure(float length, List<Vector2> crossed)
        {
            length = Mathf.Round(length / Step) * Step;
            Cut = length > MaxLength;
            Length = Mathf.Min(length, MaxLength);
            Vector2 last = crossed.Count > 0 ? crossed[crossed.Count - 1] : Vector2.zero;
            bool onDock = crossed.Count > 0 && last.y > Length && last.x <= Length + KwPierMeet.Reach;
            if (onDock)
                (Length, Cut) = (last.x, false);
            Crosses = crossed.FindAll(s => s.x > 0f && s.y < Length).Count;
            if (!onDock && Length < MinLength)
                Refuse($"Too short: a dock is at least {MinLength:0} m long");
            Length = onDock ? Length : Mathf.Max(Length, MinLength);
            float over = At.y - KwPierAim.Sea;
            if (over < KwPierAim.MinOverWater)
                Refuse($"Too low: the deck would be {over:0.0} m over the sea ({KwPierAim.MinOverWater:0.0} m at least)");
        }

        // The runs between the docks it crosses, from the start to its length; none at all: the docks already meet there.
        private void Lay(int seed, List<Vector2> crossed)
        {
            float from = 0f;
            foreach (Vector2 span in crossed)
            {
                if (span.x >= Length)
                    break;
                Run(from, span.x, seed);
                from = Mathf.Max(from, span.y);
            }
            Run(from, Length, seed);
            if (Sections.Count == 0)
                Refuse("The docks already meet here");
        }

        // One run as equal sections, the first joined when it starts on a dock (the start's, or one it crossed).
        private void Run(float from, float to, int seed)
        {
            float length = to - from;
            if (length < MinRun)
                return;
            int count = Mathf.CeilToInt(length / MaxSection - 0.001f);
            float each = length / count;
            for (int i = 0; i < count; i++)
            {
                bool joined = i > 0 || from > 0f || Joined;
                Offsets.Add(from + each * i);
                Sections.Add(new KwPierSpec(each, Width, joined, seed + Sections.Count * 7919, Depths(from + each * i, each, joined)));
            }
        }

        // A section's piling feet, station by station, found by casts at the places its pilings stand.
        private Vector2[] Depths(float from, float length, bool joined)
        {
            int stations = KwPierSpec.StationsFor(length);
            KwPierSpec frame = new KwPierSpec(length, Width, joined, 0, new Vector2[stations]);
            float x = KwPierShape.PostX(frame, Kind);
            Vector2[] depths = new Vector2[stations];
            for (int j = joined ? 1 : 0; j < stations; j++)
            {
                float along = from + KwPierShape.PostZ(frame, Kind, j);
                Vector3 middle = At + Way * along;
                depths[j] = new Vector2(Depth(middle - Right * x, along, j), Depth(middle + Right * x, along, j));
            }
            return depths;
        }

        // How far under the deck's top a piling's foot is: the ground's depth and the sinking; refused where it cannot stand.
        private float Depth(Vector3 point, float along, int station)
        {
            float top = At.y;
            if (!Physics.Raycast(new Vector3(point.x, top + Above, point.z), Vector3.down, out RaycastHit hit, Above + MaxPiling, Ground))
            {
                Refuse($"Too deep for pilings {along:0} m out ({MaxPiling:0} m at most)");
                return MaxPiling;
            }
            float ground = hit.point.y, surface = Mathf.Max(ground, KwPierAim.Sea);
            if (ground > top + 0.02f && station > 0)
                Refuse($"The ground rises through the dock {along:0} m out");
            else if (top - surface > MaxHigh)
                Refuse($"Too high: the deck would stand {top - surface:0.0} m over the {(ground >= KwPierAim.Sea ? "ground" : "water")} {along:0} m out ({MaxHigh:0} m at most)");
            return Mathf.Max(0f, top - ground + Embed);
        }

        /// <summary>What the preview shows, to know when to make it again.</summary>
        public string Key()
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            string head = string.Join("|", At.ToString("F2"), Way.ToString("F3"), Kind.Id, Why != null ? "x" : "ok");
            return head + "|" + string.Join(";", Sections.ConvertAll(s => s.ToString())) + "|" + Length.ToString("F1", inv);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z).normalized;
    }
}
