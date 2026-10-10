using System.Collections.Generic;
using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>One camera key: when (seconds into the move), where, what it looks at, the lens, and the ease into it.</summary>
    internal sealed class CameraKey
    {
        internal float T;
        internal Spot Pos;
        internal Spot Look;
        internal Vector2? Dir;
        internal float Fov;
        internal float Roll;
        internal string Ease;

        internal static CameraKey Parse(JObject spec, CameraKey previous, int index)
        {
            string what = $"camera key {index}";
            var key = new CameraKey
            {
                T = spec.Value<float?>("t") ?? (previous?.T ?? 0f), Fov = spec.Value<float?>("fov") ?? previous?.Fov ?? 50f,
                Roll = spec.Value<float?>("roll") ?? 0f, Ease = spec.Value<string>("ease") ?? "inout",
                Pos = Spot.Parse(spec["pos"], what + " pos") ?? previous?.Pos ?? throw new BridgeException(what + ": needs pos"),
                Look = Spot.Parse(spec["look"], what + " look"),
            };
            if (spec["dir"] is JArray dir && dir.Count == 2) key.Dir = new Vector2(dir[0].Value<float>(), dir[1].Value<float>());
            else if (key.Look == null) (key.Look, key.Dir) = (previous?.Look, previous?.Dir);
            return key;
        }
    }

    /// <summary>
    /// A camera move: keys joined by a Catmull-Rom curve through their positions and their look points, each segment
    /// eased (inout, in, out, linear), plus handheld sway. Keys on actors follow them as they move.
    /// </summary>
    internal sealed class CameraMove
    {
        private readonly List<CameraKey> keys = new List<CameraKey>();
        private readonly ShotFrame frame;

        internal float HandAmp { get; private set; }
        internal float HandFreq { get; private set; } = 0.6f;
        internal float Near { get; private set; } = 0.05f;
        internal float Aperture { get; private set; } = 0.6f;
        internal float Blur { get; private set; } = 2f;
        private Spot focus;

        /// <summary>The point held in focus (depth of field on), or null for the game's own depth of field.</summary>
        internal Vector3? FocusPoint() => focus?.Resolve(frame);

        internal bool HasFocus => focus != null;

        private CameraMove(ShotFrame frame) => this.frame = frame;

        internal static CameraMove Parse(JObject spec, ShotFrame frame)
        {
            var move = new CameraMove(frame);
            JArray keys = FromHere(spec, spec["orbit"] is JObject orbit ? Orbit(orbit) : spec["keys"] as JArray ?? new JArray(spec));
            CameraKey previous = null;
            for (int i = 0; i < keys.Count; i++)
            {
                previous = CameraKey.Parse((JObject)keys[i], previous, i);
                move.keys.Add(previous);
            }
            if (spec["hand"] is JArray hand && hand.Count == 2) (move.HandAmp, move.HandFreq) = (hand[0].Value<float>(), hand[1].Value<float>());
            else move.HandAmp = spec.Value<float?>("hand") ?? 0f;
            move.Near = spec.Value<float?>("near") ?? 0.05f;
            move.focus = Spot.Parse(spec["focus"], "camera focus");
            move.Aperture = spec.Value<float?>("aperture") ?? 0.6f;
            move.Blur = spec.Value<float?>("blur") ?? 2f;
            return move;
        }

        internal float Length => keys[keys.Count - 1].T;

        /// <summary>
        /// With "from": "here" the move starts where the camera is now (no cut): that pose is its first key, and the given
        /// keys follow "lead" seconds later (default: when the first given key is at 0, 1.5 s; otherwise its own time).
        /// </summary>
        private static JArray FromHere(JObject spec, JArray keys)
        {
            if (spec.Value<string>("from") != "here" || keys.Count == 0) return keys;
            float first = keys[0].Value<float?>("t") ?? 0f, lead = spec.Value<float?>("lead") ?? (first > 0f ? 0f : 1.5f);
            var shifted = new JArray(CameraRig.Here(0f));
            foreach (JObject key in keys)
            {
                var copy = (JObject)key.DeepClone();
                copy["t"] = (key.Value<float?>("t") ?? 0f) + lead;
                shifted.Add(copy);
            }
            return shifted;
        }

        /// <summary>
        /// An orbit as keys: round a centre (a point or an actor) from one bearing to another (degrees, in the shot frame)
        /// at a radius and height, over time seconds, easing in and out; it looks at "look" or the centre lifted by "lift".
        /// {"orbit": {"center": [x,y,z] | {"cast":...}, "radius": 9, "height": 6, "from": 200, "to": 290, "time": 14,
        ///  "lift": 1.2, "fov": 45}}
        /// </summary>
        private static JArray Orbit(JObject o)
        {
            JToken center = o["center"] ?? throw new BridgeException("orbit needs center=");
            float from = o.Value<float?>("from") ?? 0f, to = o.Value<float?>("to") ?? 90f, time = o.Value<float?>("time") ?? 10f;
            float radius = o.Value<float?>("radius") ?? 8f, height = o.Value<float?>("height") ?? 4f, lift = o.Value<float?>("lift") ?? 1f;
            int steps = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(to - from) / 8f));
            JToken look = o["look"] ?? Offset(center, new Vector3(0f, lift, 0f));
            var keys = new JArray();
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)steps, a = Mathf.Lerp(from, to, u) * Mathf.Deg2Rad;
                var key = new JObject { ["t"] = time * u, ["pos"] = Offset(center, new Vector3(Mathf.Sin(a) * radius, height, Mathf.Cos(a) * radius)), ["ease"] = "linear" };
                if (i == 0) (key["look"], key["fov"]) = (look, o.Value<float?>("fov") ?? 50f);
                keys.Add(key);
            }
            return keys;
        }

        /// <summary>A spot's JSON moved by an offset in the shot frame: a point's coordinates, or an actor spot's "off".</summary>
        private static JToken Offset(JToken spot, Vector3 by)
        {
            if (spot is JArray p) return new JArray(p[0].Value<float>() + by.x, p[1].Value<float>() + by.y, p[2].Value<float>() + by.z);
            if (!(spot is JObject o)) return new JObject { ["cast"] = spot.Value<string>(), ["off"] = new JArray(by.x, by.y, by.z) };
            var copy = (JObject)o.DeepClone();
            Vector3 off = copy["off"] != null ? Spot.Vector(copy["off"], "orbit centre") : copy["p"] != null ? Spot.Vector(copy["p"], "orbit centre") : Vector3.zero;
            copy[copy["p"] != null ? "p" : "off"] = new JArray(off.x + by.x, off.y + by.y, off.z + by.z);
            if (copy["cast"] != null) copy["local"] = false;
            return copy;
        }

        /// <summary>Reads every key once now, so snap spots keep where things are at the moment the move is set.</summary>
        internal void Prime()
        {
            for (int i = 0; i < keys.Count; i++)
            {
                Position(i);
                LookAt(i);
            }
            focus?.Resolve(frame);
        }

        internal void Evaluate(float t, out Vector3 position, out Quaternion rotation, out float fov)
        {
            int i = Segment(t, out float u);
            position = CatmullRom(Position(i - 1), Position(i), Position(i + 1), Position(i + 2), u);
            Vector3 look = CatmullRom(LookAt(i - 1), LookAt(i), LookAt(i + 1), LookAt(i + 2), u);
            CameraKey a = Key(i), b = Key(i + 1);
            fov = Mathf.Lerp(a.Fov, b.Fov, u);
            Vector3 forward = look - position;
            if (forward.sqrMagnitude < 0.0001f) forward = frame.Turn * Vector3.forward;
            rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 0f, Mathf.Lerp(a.Roll, b.Roll, u));
        }

        /// <summary>The key a segment starts at and how far along it t is, eased by the key it runs into.</summary>
        private int Segment(float t, out float u)
        {
            u = 0f;
            if (t <= keys[0].T) return 0;
            for (int i = 0; i < keys.Count - 1; i++)
            {
                CameraKey a = keys[i], b = keys[i + 1];
                if (t >= b.T) continue;
                u = Eased(Mathf.Clamp01((t - a.T) / Mathf.Max(0.0001f, b.T - a.T)), b.Ease);
                return i;
            }
            return keys.Count - 1;
        }

        private CameraKey Key(int i) => keys[Mathf.Clamp(i, 0, keys.Count - 1)];

        private Vector3 Position(int i) => Key(i).Pos.Resolve(frame);

        private Vector3 LookAt(int i)
        {
            CameraKey key = Key(i);
            if (key.Look != null) return key.Look.Resolve(frame);
            Vector2 dir = key.Dir ?? Vector2.zero;
            return Position(i) + frame.Turn * (Quaternion.Euler(dir.y, dir.x, 0f) * Vector3.forward) * 20f;
        }

        internal static float Eased(float u, string ease)
        {
            switch (ease)
            {
                case "linear": return u;
                case "in": return 1f - Mathf.Cos(u * Mathf.PI * 0.5f);
                case "out": return Mathf.Sin(u * Mathf.PI * 0.5f);
                default: return 0.5f - 0.5f * Mathf.Cos(u * Mathf.PI);
            }
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float u)
        {
            float u2 = u * u, u3 = u2 * u;
            return 0.5f * (2f * p1 + (p2 - p0) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2 + (3f * p1 - p0 - 3f * p2 + p3) * u3);
        }
    }
}
