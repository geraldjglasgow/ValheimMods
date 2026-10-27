using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where the sea stands in a launched ship's hull, worked out <see cref="Every"/> seconds on every screen: at each of
    /// the hull's cross-sections (<see cref="KwHullSlices.Rings"/>) the sea's surface across it, as a line in the ship's
    /// frame through the sea's height at its two sides (<see cref="Line"/>), however low the ship rides, heels or pitches;
    /// and, compartment by compartment (<see cref="KwFloodHolds"/>), whether the sea itself is the water there
    /// (<see cref="KwFloodHandover"/>: its own water within a few centimetres of the sea, or the ship sinking). The water
    /// mask (<see cref="KwShipWaterMask"/>) hides the sea only round that line, only where the hold is dry, and nowhere the
    /// sea has taken over; the hold's water (<see cref="KwFloodSurface"/>) is drawn only where it has not. Read from the
    /// ship's own synced place and its synced water, so every screen agrees. Only where there is a screen.
    /// </summary>
    public sealed class KwShipSeaInside : MonoBehaviour
    {
        private const float Every = 0.1f, NoWater = -1000f, Narrowest = 0.4f, Far = 150f, NoGap = 99f;
        private readonly KwFloodHandover handover = new KwFloodHandover();
        private List<(float Z, Vector2[] Shape)> rings = new List<(float, Vector2[])>();
        private float waterline, at = -99f;
        private float[] halves = new float[0];
        private WaterVolume?[] seas = new WaterVolume?[0];
        private KwShipFlood? flood;

        /// <summary>Each cross-section's sea line: Normals[r]·(x, y) = Levels[r], the normal pointing up; where Found[r].</summary>
        public Vector2[] Normals { get; private set; } = new Vector2[0];

        public float[] Levels { get; private set; } = new float[0];

        public bool[] Found { get; private set; } = new bool[0];

        /// <summary>Counts up each time the lines and the sea's compartments are worked out again.</summary>
        public int Version { get; private set; }

        public List<(float Z, Vector2[] Shape)> Rings => rings;

        /// <summary>The hold's compartments and their water (null before the ship's flooding is made, or without it).</summary>
        public KwFloodHolds? Holds => Flood != null ? Flood.Holds : null;

        /// <summary>Whether the ship is going down: the sea is the water everywhere in it.</summary>
        public bool Sinking => handover.Sinking;

        /// <summary>Whether the sea is the water in compartment k.</summary>
        public bool Sea(int k) => handover.Sea(k);

        private KwShipFlood? Flood => flood != null ? flood : flood = GetComponent<KwShipFlood>();

        public void Set(List<(float Z, Vector2[] Shape)> sections, float line)
        {
            (rings, waterline) = (sections, line);
            int n = sections.Count;
            (halves, seas) = (new float[n], new WaterVolume?[2 * n + 1]);
            (Normals, Levels, Found) = (new Vector2[n], new float[n], new bool[n]);
            for (int r = 0; r < n; r++)
                halves[r] = Mathf.Max(KwSectionMath.Breadth(sections[r].Shape, line), Narrowest) / 2f;
        }

        /// <summary>Whether the sea is the water in a ship's hull at a point of the world (in a compartment it has taken over).</summary>
        public static bool SeaAt(Component ship, Vector3 world)
        {
            KwShipSeaInside? inside = ship.GetComponent<KwShipSeaInside>();
            KwFloodHolds? holds = inside != null ? inside.Holds : null;
            return holds != null && inside!.Sea(holds.At(ship.transform.InverseTransformPoint(world).z));
        }

        private void Update()
        {
            if (Time.time - at < Every || Distant())
                return;
            float dt = Mathf.Min(Time.time - at, 1f);
            at = Time.time;
            Vector3 up = transform.InverseTransformDirection(Vector3.up);
            Vector3 slide = Vector3.forward - up.z * up; // along the ship, keeping a point's height in the world
            for (int r = 0; r < rings.Count; r++)
                Found[r] = Line(r, up, slide.z > 0.1f ? slide / slide.z : Vector3.forward);
            Decide(dt);
            Version++;
        }

        // A cross-section's sea line: through the sea's surface over its two sides at the design waterline, each brought
        // along the ship to the section, level in the world, so the line lies in the section's own plane.
        private bool Line(int r, Vector3 up, Vector3 slide)
        {
            float z = rings[r].Z, half = halves[r];
            if (!Surface(2 * r, new Vector3(-half, waterline, z), slide, z, out Vector2 port)
                || !Surface(2 * r + 1, new Vector3(half, waterline, z), slide, z, out Vector2 starboard))
                return false;
            Vector2 across = starboard - port;
            Vector2 normal = across.sqrMagnitude > 1e-6f ? new Vector2(-across.y, across.x).normalized : new Vector2(up.x, up.y).normalized;
            if (normal.x * up.x + normal.y * up.y < 0f)
                normal = -normal;
            (Normals[r], Levels[r]) = (normal, Vector2.Dot(normal, port));
            return true;
        }

        // The sea's surface over a point of the ship's frame, brought along the ship to z: false where there is no sea.
        private bool Surface(int i, Vector3 local, Vector3 slide, float z, out Vector2 point)
        {
            Vector3 world = transform.TransformPoint(local);
            float sea = Floating.GetWaterLevel(new Vector3(world.x, SeaLevel, world.z), ref seas[i]);
            Vector3 on = transform.InverseTransformPoint(new Vector3(world.x, sea, world.z));
            on += slide * (z - on.z);
            point = new Vector2(on.x, on.y);
            return sea > NoWater;
        }

        // Each compartment's water against the sea there, and the ship sinking or not.
        private void Decide(float dt)
        {
            KwFloodHolds? holds = Holds;
            if (holds == null || Flood == null)
                return;
            handover.Sink(DeckUnder(Flood), Flood.Share, dt);
            for (int k = 0; k < holds.Holds; k++)
                handover.Step(k, holds.Holds, holds.Wet(k), holds.VolumeOf(k), holds.Wet(k) ? Gap(holds, k) : 0f, dt);
        }

        // How far the sea stands over a compartment's water on its centre line, the mean over its cross-sections.
        private float Gap(KwFloodHolds holds, int k)
        {
            Vector3 up = holds.Up;
            float aft = holds.Aft(k) - 0.01f, fore = holds.Fore(k) + 0.01f, level = holds.LevelOf(k), sum = 0f;
            int count = 0;
            for (int r = 0; r < rings.Count; r++)
            {
                float z = rings[r].Z;
                if (z < aft || z > fore || !Found[r] || Mathf.Abs(Normals[r].y) < 0.1f || Mathf.Abs(up.y) < 0.1f)
                    continue;
                sum += Levels[r] / Normals[r].y - (level - up.z * z) / up.y;
                count++;
            }
            return count > 0 ? sum / count : NoGap;
        }

        // The ship's lowest deck (its sheer midships, with none) under the sea.
        private bool DeckUnder(KwShipFlood flooding)
        {
            Vector3 top = transform.TransformPoint(flooding.Top);
            return Floating.GetWaterLevel(new Vector3(top.x, SeaLevel, top.z), ref seas[seas.Length - 1]) > top.y;
        }

        // Too far from the camera for its hold to be seen: left as it was last worked out.
        private bool Distant() =>
            GameCamera.instance != null && (GameCamera.instance.transform.position - transform.position).sqrMagnitude > Far * Far;

        private static float SeaLevel => ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;
    }
}
