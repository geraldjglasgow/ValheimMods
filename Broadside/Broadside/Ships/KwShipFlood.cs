using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Water in a launched ship's hold. The hold is split at bulkheads into compartments (<see cref="KwFloodHolds"/>,
    /// <see cref="KwBulkheads"/>). On the owner, every physics step (<see cref="Step"/>, from the ship's buoyancy,
    /// <see cref="KwShipHydro.Float"/>): the sea comes in through every hole not patched (<see cref="KwHullHoles.Leaking"/>), and through every
    /// opening left open low in the hull's sides (<see cref="KwShipOpenings.Leaks"/>, as holes of its size in bands up it,
    /// at most <see cref="KwShipOpeningPlan.Most"/>), into the hole's own compartment, and runs back out of a hole the water inside stands over while the sea outside is lower; a few times a
    /// second water passes slowly through the bulkheads to the neighbours where it stands lower, and every compartment's
    /// surface is worked out again: flat, following the ship's heel about <see cref="Lag"/> seconds late (it sloshes) and
    /// a share of its pitch (<see cref="KwFloodHolds.Along"/>). Its weight presses down on each slice where that slice's
    /// water lies, so the ship sits lower, trims by the flooded end and lists to the side the water runs to. Bailing takes it out of the bailer's compartment
    /// (<see cref="KwShipBail"/>). Filled past what the hull lifts, the ship sinks; once its lowest deck (its sheer
    /// midships, with none) has been under the sea for <see cref="Settle"/> seconds with its hold at least half full, it
    /// loses a fifth of its full health a second through the game's own wear, which destroys it as the game destroys a
    /// ship. The ship's ZDO keeps each compartment's volume (<see cref="PartsKey"/>) and the whole hold's
    /// (<see cref="Key"/>); a ship saved with the whole hold's alone has it spread at one level. Every peer works out the
    /// same surfaces from them and draws them (<see cref="KwFloodSurface"/>).
    /// </summary>
    public sealed class KwShipFlood : MonoBehaviour
    {
        public const string Key = "kw_flood", PartsKey = "kw_flood_parts";
        private static readonly int hash = Key.GetStableHashCode(), partsHash = PartsKey.GetStableHashCode();
        private const float Density = 1000f, Lag = 1.2f, Every = 0.1f, Settle = 2f, SinkShare = 0.2f, NoWater = -1000f;
        private const float WriteEvery = 0.25f;
        private static readonly List<KwHullSlice> NoSlices = new List<KwHullSlice>();
        private static readonly List<KwHullHole> NoHoles = new List<KwHullHole>();

        private ZNetView? view;
        private Rigidbody? body;
        private KwHullHoles? holes;
        private WearNTear? wear;
        private KwFloodHolds? holds;
        private float writtenAt = -99f, solvedAt, under, sunkAt = -99f, deck, middle;
        private float[] written = new float[0];
        private string read = "";
        private float spread = -1f;
        private bool owned;
        private readonly WaterVolume?[] seas = new WaterVolume?[KwHullDamage.Most + 1];
        private KwShipOpenings? openings;
        private readonly WaterVolume?[] openSeas = new WaterVolume?[KwShipOpeningPlan.Most];

        /// <summary>The compartments and their water (null until the ship is built).</summary>
        public KwFloodHolds? Holds => holds;

        /// <summary>The water in the hold, in cubic metres.</summary>
        public float Volume => holds != null ? holds.Total : 0f;

        /// <summary>How much the hold holds up to the sheer, in cubic metres.</summary>
        public float Capacity => holds != null ? holds.Capacity : 0f;

        /// <summary>How full the hold is, 0 to 1.</summary>
        public float Share => Capacity > 0f ? Volume / Capacity : 0f;

        /// <summary>Whether any compartment had water under its surface when it was last worked out.</summary>
        public bool Solved { get; private set; }

        /// <summary>The way up the water's surfaces lie, in the ship's frame.</summary>
        public Vector3 Up => holds != null ? holds.Up : Vector3.up;

        public List<KwHullSlice> Slices => holds != null ? holds.Slices : NoSlices;

        /// <summary>The point whose going under the sea sinks the ship: its lowest deck (its sheer, with none) midships, ship's frame.</summary>
        public Vector3 Top => new Vector3(0f, deck, middle);

        public void Set(KwShipHydro hydro, KwHullHoles holes, KwBpHull hull)
        {
            (view, body, wear, this.holes) = (GetComponent<ZNetView>(), GetComponent<Rigidbody>(), GetComponent<WearNTear>(), holes);
            openings = GetComponent<KwShipOpenings>();
            holds = new KwFloodHolds(hydro.Slices, KwBulkheads.Starts(holes.Blueprint, hydro.Slices, hydro.Waterline), hydro.Waterline);
            written = new float[holds.Holds];
            (deck, middle) = (hull.DeckLow < float.MaxValue ? hull.DeckLow : hull.Section[hull.Section.Length - 1].y, hull.Origin.z);
            Load(true);
            Remember();
            Resolve(0f, false);
        }

        // Not the owner: the owner's volumes, the surfaces worked out again a few times a second where they are drawn.
        private void Update()
        {
            if (view == null || !view.IsValid() || view.IsOwner())
                return;
            owned = false;
            if (!KwShipBody.Drawn || Time.time - solvedAt < 2f * Every)
                return;
            Load(false);
            Resolve(Time.time - solvedAt, false);
        }

        /// <summary>On the owner, in the ship's physics step: water in and out, over the bulkheads, its weight, sinking.</summary>
        public void Step()
        {
            if (view == null || body == null || holds == null || !view.IsValid() || !view.IsOwner() || holds.Capacity <= 0f)
                return;
            if (!owned)
            {
                Load(true);
                (owned, writtenAt) = (true, Time.time);
                Remember();
            }
            Inflow(Time.fixedDeltaTime);
            if (Time.time - solvedAt >= Every)
                Resolve(Time.time - solvedAt, true);
            Press();
            Sink(Time.fixedDeltaTime);
            Write(false);
        }

        /// <summary>On the owner: water taken out of a compartment (a bucket bailed there).</summary>
        public void Bail(float water, int hold)
        {
            if (view == null || holds == null || !view.IsValid() || !view.IsOwner())
                return;
            holds.Add(hold, -water);
            Write(true);
        }

        // The ZDO's volumes, read again only when they changed (or always, forced); a ship saved with the whole hold's
        // volume alone has it spread at one level.
        private void Load(bool force)
        {
            ZDO? zdo = view != null && view.IsValid() ? view.GetZDO() : null;
            if (holds == null || zdo == null)
                return;
            string text = zdo.GetString(partsHash);
            float total = text.Length == 0 ? zdo.GetFloat(hash, 0f) : 0f;
            if (!force && text == read && (text.Length > 0 || total == spread))
                return;
            (read, spread) = (text, total);
            if (!holds.Read(text))
                holds.Spread(zdo.GetFloat(hash, 0f));
        }

        // The surfaces turned a share of the way to lie level, the water over the bulkheads passed on (on the owner), and
        // every compartment's surface for its volume.
        private void Resolve(float dt, bool spill)
        {
            Vector3 level = transform.InverseTransformDirection(Vector3.up);
            Vector3 up = dt > 0f ? Vector3.Slerp(holds!.Up, level, 1f - Mathf.Exp(-dt / Lag)).normalized : level;
            holds!.Resolve(up, spill ? dt : 0f, -Physics.gravity.y);
            solvedAt = Time.time;
            Solved = holds.AnyWet;
        }

        // Every slice's water weighing on it where its water lies.
        private void Press()
        {
            float g = -Physics.gravity.y;
            float[] areas = holds!.Areas;
            Vector2[] middles = holds.Middles;
            List<KwHullSlice> slices = holds.Slices;
            for (int i = 0; i < areas.Length && i < slices.Count; i++)
            {
                if (areas[i] <= 0f)
                    continue;
                Vector3 at = transform.TransformPoint(new Vector3(middles[i].x, middles[i].y, slices[i].Z));
                body!.AddForceAtPosition(Vector3.down * (Density * g * areas[i] * slices[i].Length), at, ForceMode.Force);
            }
        }

        // The sea through every hole this step, into the hole's own compartment (out of it, where the water inside is higher);
        // then the same through the open openings' bands (KwShipOpenings), each with its own cache of the sea.
        private void Inflow(float dt)
        {
            float g = -Physics.gravity.y;
            List<KwHullHole> list = holes != null ? holes.Leaking : NoHoles;
            for (int i = 0; i < list.Count && i < seas.Length - 1; i++)
                Through(list[i], ref seas[i], g, dt);
            List<KwHullHole> open = openings != null ? openings.Leaks : NoHoles;
            for (int i = 0; i < open.Count && i < openSeas.Length; i++)
                Through(open[i], ref openSeas[i], g, dt);
        }

        // The sea through one hole this step.
        private void Through(KwHullHole hole, ref WaterVolume? cache, float g, float dt)
        {
            Vector3 at = transform.TransformPoint(hole.Place);
            float sea = Floating.GetWaterLevel(new Vector3(at.x, SeaLevel, at.z), ref cache);
            if (sea >= NoWater)
                holds!.Add(holds.At(hole.Place.z), holds.Leak(hole, Mathf.Max(0f, sea - at.y), g) * dt);
        }

        // Under for Settle seconds with the hold half full: a fifth of the ship's full health gone every second.
        private void Sink(float dt)
        {
            if (wear == null || Share < 0.5f)
            {
                under = 0f;
                return;
            }
            Vector3 top = transform.TransformPoint(new Vector3(0f, deck, middle));
            under = Sea(seas.Length - 1, top) > top.y ? under + dt : 0f;
            if (under < Settle || Time.time - sunkAt < 1f)
                return;
            sunkAt = Time.time;
            wear.ApplyDamage(wear.m_health * SinkShare);
        }

        // The volumes into the ZDO when a compartment's has changed enough (or one ran dry), at most every WriteEvery
        // unless forced: each compartment's and the whole hold's.
        private void Write(bool now)
        {
            if (!Changed() || (!now && Time.time - writtenAt < WriteEvery))
                return;
            ZDO zdo = view!.GetZDO();
            string text = holds!.Text();
            zdo.Set(partsHash, text);
            zdo.Set(hash, holds.Total);
            (read, writtenAt) = (text, Time.time);
            Remember();
        }

        private bool Changed()
        {
            for (int k = 0; k < written.Length; k++)
            {
                float now = holds!.VolumeOf(k);
                if (Mathf.Abs(now - written[k]) >= Mathf.Max(0.01f, 0.004f * holds.CapacityOf(k)) || (now <= 0f && written[k] > 0f))
                    return true;
            }
            return false;
        }

        // The volumes as last written (or read), to tell when they have changed enough to write again.
        private void Remember()
        {
            for (int k = 0; k < written.Length; k++)
                written[k] = holds!.VolumeOf(k);
        }

        private static float SeaLevel => ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;

        private float Sea(int i, Vector3 at) => Floating.GetWaterLevel(new Vector3(at.x, SeaLevel, at.z), ref seas[i]);
    }
}
