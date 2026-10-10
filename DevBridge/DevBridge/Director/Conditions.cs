using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// When a waiting cue may run: "near": [a, b, metres] (actors' roots at most that far apart), "far": [a, b, metres],
    /// "attacking": name (in an attack), "dead": name, "landed": name (on the ground), "cam": [name, metres, bone?] (that
    /// close to the camera), "pushed": [name, m/s] (moving at least that fast: knocked back, thrown), "lower": [name, spot] (below the spot's height). All given must hold.
    /// </summary>
    internal static class Conditions
    {
        internal static bool Hold(JObject when)
        {
            if (when["near"] is JArray near && Distance(near) > near[2].Value<float>()) return false;
            if (when["far"] is JArray far && Distance(far) < far[2].Value<float>()) return false;
            if (when["attacking"] != null && !Character(when.Value<string>("attacking")).InAttack()) return false;
            if (when["dead"] != null && Cast.Get(when.Value<string>("dead")).Alive) return false;
            if (when["exists"] != null && !Exists(when.Value<string>("exists"))) return false;
            if (when["cam"] is JArray cam && Camera(cam) > cam[1].Value<float>()) return false;
            if (when["landed"] != null && !Character(when.Value<string>("landed")).IsOnGround()) return false;
            if (when["lower"] is JArray lower && Cast.Anchor(lower[0].Value<string>(), null).position.y >= Spot.Parse(lower[1], "lower").Resolve(Cast.Frame).y) return false;
            if (when["pushed"] is JArray pushed && Speed(pushed[0].Value<string>()) < pushed[1].Value<float>()) return false;
            return true;
        }

        private static float Distance(JArray pair)
        {
            if (pair.Count != 3) throw new BridgeException("near/far take [actor, actor, metres]");
            Vector3 a = Cast.Anchor(pair[0].Value<string>(), null).position, b = Cast.Anchor(pair[1].Value<string>(), null).position;
            return Vector3.Distance(a, b);
        }

        /// <summary>"cam": [actor, metres, bone?]: how far in front of the lens the actor (or its part) is, along the view.</summary>
        private static float Camera(JArray spec)
        {
            Transform anchor = Cast.Anchor(spec[0].Value<string>(), spec.Count > 2 ? spec[2].Value<string>() : null);
            Transform lens = GameCamera.instance ? GameCamera.instance.transform : null;
            return lens ? Vector3.Dot(anchor.position - lens.position, lens.forward) : float.MaxValue;
        }

        /// <summary>"exists": prefab: a loaded instance of it that is not in the cast yet (a projectile just fired).</summary>
        private static bool Exists(string prefab)
        {
            foreach (ZNetView view in ZNetScene.instance.m_instances.Values)
                if (view && Utils.GetPrefabName(view.gameObject) == prefab && !Cast.IsActor(view.gameObject)) return true;
            return false;
        }

        private static float Speed(string name)
        {
            Rigidbody body = Character(name).m_body;
            return body ? body.linearVelocity.magnitude : 0f;
        }

        private static Character Character(string name)
        {
            Actor actor = Cast.Get(name);
            return actor.Character ? actor.Character : throw new BridgeException($"actor {name} is not a character");
        }
    }
}
