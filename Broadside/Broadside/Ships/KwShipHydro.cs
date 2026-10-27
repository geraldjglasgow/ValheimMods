using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How a launched ship floats: its own hull in the water (<see cref="KwShipDrive"/> runs it in the game's ship step).
    /// The hull is cut into slices along its length (<see cref="KwHullSlices"/>, the cross-sections its planking is drawn
    /// from); every physics step each slice's wet part is worked out under the real sea surface across it (the water's
    /// height at both its sides), and the water lifts it by the weight of the water it pushes aside, at the middle of that
    /// wet part (Archimedes). So the ship floats at its waterline, rides waves longer than itself and averages out shorter
    /// ones, heels as wind and crew push it and comes back as the wet part moves to the low side (a wide hull stiff, a
    /// narrow one tender), and loses its lift if heeled past its rail. Its weight is the water its hull displaces to the
    /// design waterline (<see cref="KwShipTuning.Draught"/>), so it floats exactly there; its weight's centre straight
    /// over the middle of that water and low in the hull (<see cref="KwShipWeight"/>), so it floats level; as hard to
    /// turn as a solid hull of its size. Each slice's sides are damped as they move up and down.
    /// The water is looked up at sea level over each point (the game finds water only inside the sea's volume, which does
    /// not reach a sunken hull). While the world round it loads and until it finds water (or 15 s), the ship is held.
    /// </summary>
    public sealed class KwShipHydro : MonoBehaviour
    {
        private const float Density = KwShipWeight.Density, Damping = 0.3f, Waiting = 15f, NoWater = -1000f;

        private Rigidbody? body;
        private ZNetView? view;
        private WaterVolume?[] volumes = new WaterVolume?[0];
        private float damp, waterplane, since = -1f;
        private bool wet, released;

        public List<KwHullSlice> Slices { get; private set; } = new List<KwHullSlice>();
        public float Waterline { get; private set; }

        /// <summary>Whether any of the hull was in the water this step.</summary>
        public bool Afloat { get; private set; }

        public void Set(KwBlueprint blueprint, KwBpHull hull)
        {
            body = GetComponent<Rigidbody>();
            view = GetComponent<ZNetView>();
            Waterline = KwShipTuning.Draught(hull);
            Slices = KwHullSlices.Of(blueprint, hull, Waterline);
            volumes = new WaterVolume?[Slices.Count * 2];
            Weigh(hull);
        }

        // As heavy as the water the hull displaces to the waterline, its weight's centre over that water's middle
        // (KwShipWeight, the dock waterline's reckoning too); turned as a solid hull.
        private void Weigh(KwBpHull hull)
        {
            KwShipWeight weight = KwShipWeight.Of(Slices, Waterline);
            float mass = weight.Mass, length = hull.Length, beam = hull.Beam, depth = hull.Depth;
            waterplane = weight.Waterplane;
            body!.mass = mass;
            body.centerOfMass = weight.Centre;
            body.inertiaTensor = new Vector3(length * length + depth * depth, length * length + beam * beam, beam * beam + depth * depth) * (mass / 12f);
            body.inertiaTensorRotation = Quaternion.identity;
            damp = 2f * Damping * Mathf.Sqrt(Mathf.Max(Density * -Physics.gravity.y * waterplane, 1f) * mass);
        }

        /// <summary>The water's lift and its damping on every slice, this step. False while the ship is held.</summary>
        public bool Float()
        {
            Afloat = false;
            if (body == null || !Hold())
                return false;
            float g = -Physics.gravity.y;
            Vector3 up = transform.InverseTransformDirection(Vector3.up);
            for (int i = 0; i < Slices.Count; i++)
                Slice(i, g, up);
            if (TryGetComponent(out KwShipFlood flood))
                flood.Step(); // the water in the hold weighing on its slices, and the sea let in (KwShipFlood)
            return true;
        }

        // One slice: the sea's height at its two sides, its wet part under the line between them, lifted and damped.
        private void Slice(int i, float g, Vector3 up)
        {
            KwHullSlice s = Slices[i];
            float half = Mathf.Max(s.Breadth, 0.4f) / 2f;
            Vector3 port = transform.TransformPoint(new Vector3(-half, Waterline, s.Z)), starboard = transform.TransformPoint(new Vector3(half, Waterline, s.Z));
            float left = Level(2 * i, port), right = Level(2 * i + 1, starboard);
            if (left < NoWater || right < NoWater)
                return;
            // wet where the point's height in the world is under the sea's, which runs straight from one side to the other
            float slope = (right - left) / (2f * half), mid = (left + right) / 2f;
            float area = KwSectionMath.Wet(s.Shape, up.x - slope, up.y, mid - transform.position.y - up.z * s.Z, out Vector2 at);
            if (area <= 0f)
                return;
            Afloat = true;
            body!.AddForceAtPosition(Vector3.up * (Density * g * area * s.Length), transform.TransformPoint(new Vector3(at.x, at.y, s.Z)), ForceMode.Force);
            float c = damp * (s.Breadth * s.Length / Mathf.Max(waterplane, 0.01f)) / 2f;
            body.AddForceAtPosition(Vector3.up * (-c * body.GetPointVelocity(port).y), port, ForceMode.Force);
            body.AddForceAtPosition(Vector3.up * (-c * body.GetPointVelocity(starboard).y), starboard, ForceMode.Force);
        }

        private static float Sea => ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;

        private float Level(int i, Vector3 at) => Floating.GetWaterLevel(new Vector3(at.x, Sea, at.z), ref volumes[i]);

        // Held still, with no gravity, until the area round the ship has loaded and water is found under it (or for a
        // while after the area loaded, on land): false while it is held. Once released never held again while it stays
        // loaded: the game's area check fails for a moment whenever an object in the sectors round the ship is not made
        // yet, as it is each time the ship sails into new sectors, and holding then stopped a ship dead at sea (the user,
        // 2026-10-26: "my boat just stopped dead in the water for no reason").
        private bool Hold()
        {
            if (released)
                return true;
            bool area = ZNetScene.instance == null || ZNetScene.instance.IsAreaReady(transform.position);
            if (area && since < 0f)
                since = Time.time;
            Vector3 at = transform.position;
            wet = wet || (area && volumes.Length > 0 && Floating.GetWaterLevel(new Vector3(at.x, Sea, at.z), ref volumes[0]) > NoWater);
            bool ready = area && (wet || Time.time - since > Waiting);
            body!.useGravity = ready;
            if (!ready)
                (body.linearVelocity, body.angularVelocity) = (Vector3.zero, Vector3.zero);
            released = ready;
            return ready;
        }
    }
}
