using System.Collections.Generic;
using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// The lens's focus pulled over film seconds, whatever the camera does: {"focus": {"keys": [[0, 0.4], [0.9, 40],
    /// [1.8, 0.4]], "aperture": 1.0, "blur": 7}} sets the focus that many metres ahead of the lens at each time (near:
    /// all that is far melts away; far: sharp again), eased between keys, the last held; {"focus": "off"} lets go.
    /// </summary>
    internal sealed class FocusPull : MonoBehaviour
    {
        private static FocusPull current;
        private readonly List<Vector2> keys = new List<Vector2>();
        private float age, aperture, blur;

        internal static void Cue(JToken spec)
        {
            Stop();
            if (spec.Type == JTokenType.String && spec.Value<string>() == "off") return;
            JObject s = spec as JObject ?? throw new BridgeException("focus takes an object or \"off\"");
            current = new GameObject("DirectorFocusPull").AddComponent<FocusPull>();
            (current.aperture, current.blur) = (s.Value<float?>("aperture") ?? 1f, s.Value<float?>("blur") ?? 6f);
            foreach (JToken key in s["keys"] as JArray ?? throw new BridgeException("focus needs keys=[[t, metres], ...]"))
                current.keys.Add(new Vector2(key[0].Value<float>(), key[1].Value<float>()));
            if (current.keys.Count == 0) throw new BridgeException("focus needs at least one key");
        }

        internal static void Stop()
        {
            if (current) Destroy(current.gameObject);
            current = null;
            Focus.Release();
        }

        private void LateUpdate()
        {
            age += Time.deltaTime;
            GameCamera camera = GameCamera.instance;
            if (!camera) return;
            Transform lens = camera.transform;
            Focus.Hold(lens.position + lens.forward * Distance(age), aperture, blur);
        }

        private float Distance(float t)
        {
            int i = 0;
            while (i < keys.Count - 1 && t >= keys[i + 1].x) i++;
            Vector2 a = keys[i], b = keys[Mathf.Min(i + 1, keys.Count - 1)];
            float u = b.x > a.x ? CameraMove.Eased(Mathf.Clamp01((t - a.x) / (b.x - a.x)), "inout") : 1f;
            return Mathf.Lerp(a.y, b.y, u);
        }
    }
}
