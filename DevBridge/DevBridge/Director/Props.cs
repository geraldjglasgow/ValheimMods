using System.Collections.Generic;
using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// Props: local, unnetworked copies of a game prefab (or one of its parts) held on the camera or on an actor's bone,
    /// like a first-person torch: {"prop":"Torch","child":"attach","on":"camera","off":[x,y,z],"rot":[x,y,z],"show":["equiped"]},
    /// or moving there along a "path" (<see cref="PropPath"/>), like a swung axe.
    /// Colliders and bodies are stripped; every prop goes when the shot ends.
    /// </summary>
    internal static class Props
    {
        private static readonly List<GameObject> Held = new List<GameObject>();

        internal static void Add(JObject spec)
        {
            Transform parent = Parent(spec.Value<string>("on") ?? "camera", spec.Value<string>("bone"));
            GameObject copy = Copy(spec.Value<string>("prop"), spec.Value<string>("child"), parent);
            Place(copy.transform, parent, spec);
            copy.transform.localScale *= spec.Value<float?>("scale") ?? 1f;
            if (spec["show"] is JArray show) foreach (JToken part in show) Show(copy.transform, part.Value<string>());
            foreach (ParticleSystem particles in copy.GetComponentsInChildren<ParticleSystem>(true)) Hold(particles);
            if (spec["lamp"] is JObject light) foreach (Light lamp in copy.GetComponentsInChildren<Light>(true)) Tune(lamp, light);
            if (spec["path"] is JArray path) PropPath.Put(copy, path, spec.Value<float?>("until") ?? -1f);
            Held.Add(copy);
        }

        /// <summary>
        /// On its parent at off/rot; or, with "pose": an actor, where that actor is and turned as it is, then held there
        /// on the parent (a bolt left sticking in the one it hit), moved by off along its own axes.
        /// </summary>
        private static void Place(Transform prop, Transform parent, JObject spec)
        {
            Vector3 off = spec["off"] != null ? Spot.Vector(spec["off"], "prop off") : Vector3.zero;
            if (spec["pose"] != null)
            {
                Transform from = Cast.Anchor(spec.Value<string>("pose"), null);
                prop.SetPositionAndRotation(from.position, from.rotation);
                prop.SetParent(parent, true);
                prop.Translate(off, Space.Self);
                return;
            }
            prop.SetParent(parent, false);
            prop.localPosition = off;
            prop.localRotation = Quaternion.Euler(spec["rot"] != null ? Spot.Vector(spec["rot"], "prop rot") : Vector3.zero);
        }

        private static GameObject Copy(string prefabName, string child, Transform at)
        {
            GameObject prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(prefabName) : null;
            if (!prefab) throw new BridgeException($"no prefab {prefabName}");
            Transform source = child != null ? prefab.transform.Find(child) ?? throw new BridgeException($"{prefabName} has no part {child}") : prefab.transform;
            ZNetView.m_forceDisableInit = true;
            GameObject copy;
            try { copy = Object.Instantiate(source.gameObject, at.position, at.rotation); }
            finally { ZNetView.m_forceDisableInit = false; }
            copy.SetActive(true);
            foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true)) Object.Destroy(collider);
            foreach (Rigidbody body in copy.GetComponentsInChildren<Rigidbody>(true)) Object.Destroy(body);
            return copy;
        }

        /// <summary>A held prop's particles live in its own space, so moving it never leaves a trail.</summary>
        private static void Hold(ParticleSystem particles)
        {
            ParticleSystem.MainModule main = particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            particles.Clear(false);
        }

        /// <summary>"lamp": {"color": [r,g,b], "intensity": x, "range": m} on every light the prop carries.</summary>
        private static void Tune(Light lamp, JObject spec)
        {
            if (spec["color"] is JArray c) lamp.color = new Color(c[0].Value<float>(), c[1].Value<float>(), c[2].Value<float>());
            lamp.intensity = spec.Value<float?>("intensity") ?? lamp.intensity;
            LightFlicker flicker = lamp.GetComponent<LightFlicker>();
            if (flicker) flicker.m_baseIntensity = lamp.intensity;
            lamp.range = spec.Value<float?>("range") ?? lamp.range;
        }

        private static Transform Parent(string on, string bone)
        {
            if (on != "camera") return Cast.Anchor(on, bone);
            if (!GameCamera.instance) throw new BridgeException("no game camera");
            Transform lens = CameraRig.Lens;
            CameraRig.FollowLens(GameCamera.instance);
            return lens;
        }

        private static void Show(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) t.gameObject.SetActive(true);
        }

        internal static void Clear()
        {
            foreach (GameObject prop in Held) if (prop) Object.Destroy(prop);
            Held.Clear();
        }
    }
}
