using System.Collections.Generic;
using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// A prop moving on its parent (the lens or a bone) over film seconds, like a first-person swing:
    /// "path": [{"t": 0, "off": [x,y,z], "rot": [x,y,z]}, {"t": 0.25, "off": ..., "rot": ..., "ease": "in"}, ...];
    /// each key eases in from the one before (inout by default), the last pose holds, and "until" (seconds) takes the
    /// prop away.
    /// </summary>
    internal sealed class PropPath : MonoBehaviour
    {
        private sealed class Key
        {
            internal float T;
            internal Vector3 Off;
            internal Quaternion Rot;
            internal string Ease;
        }

        private readonly List<Key> keys = new List<Key>();
        private float age, until = -1f;

        internal static void Put(GameObject prop, JArray path, float until)
        {
            PropPath mover = prop.AddComponent<PropPath>();
            mover.until = until;
            foreach (JObject key in path)
                mover.keys.Add(new Key
                {
                    T = key.Value<float?>("t") ?? 0f, Ease = key.Value<string>("ease") ?? "inout",
                    Off = Spot.Vector(key["off"] ?? throw new BridgeException("prop path key needs off"), "prop path off"),
                    Rot = Quaternion.Euler(key["rot"] != null ? Spot.Vector(key["rot"], "prop path rot") : Vector3.zero),
                });
            mover.Pose(0f);
        }

        private void LateUpdate()
        {
            age += Time.deltaTime;
            if (until >= 0f && age >= until) { Destroy(gameObject); return; }
            Pose(age);
        }

        private void Pose(float t)
        {
            if (keys.Count == 0) return;
            int i = 0;
            while (i < keys.Count - 1 && t >= keys[i + 1].T) i++;
            Key a = keys[i], b = keys[Mathf.Min(i + 1, keys.Count - 1)];
            float u = b.T > a.T ? CameraMove.Eased(Mathf.Clamp01((t - a.T) / (b.T - a.T)), b.Ease) : 1f;
            transform.localPosition = Vector3.LerpUnclamped(a.Off, b.Off, u);
            transform.localRotation = Quaternion.SlerpUnclamped(a.Rot, b.Rot, u);
        }
    }
}
