using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// An actor's animation driven outright: {"anim": "giant", "speed": 2.5, "for": 1.2} plays it that much faster (or
    /// slower) for that many film seconds, then at its own speed again; "play": "state" cross-fades it into one of its
    /// animator's states now ("fade" seconds, default 0.15), "trigger": "name" and "bool": {"name": true} set its parameters.
    /// </summary>
    internal sealed class Animate : MonoBehaviour
    {
        private Animator animator;
        private float left;

        internal static void Cue(JObject s)
        {
            Actor actor = Cast.Get(s.Value<string>("anim"));
            Animator animator = actor.Go.GetComponentInChildren<Animator>() ?? throw new BridgeException($"actor {actor.Name} has no animator");
            if (s["play"] != null) animator.CrossFadeInFixedTime(s.Value<string>("play"), s.Value<float?>("fade") ?? 0.15f);
            if (s["trigger"] != null) animator.SetTrigger(s.Value<string>("trigger"));
            if (s["bool"] is JObject flags) foreach (JProperty flag in flags.Properties()) animator.SetBool(flag.Name, flag.Value.Value<bool>());
            if (s["speed"] != null) Pace(actor.Go, animator, s.Value<float>("speed"), s.Value<float?>("for") ?? -1f);
        }

        private static void Pace(GameObject go, Animator animator, float speed, float seconds)
        {
            Animate pacer = go.GetComponent<Animate>() ?? go.AddComponent<Animate>();
            (pacer.animator, pacer.left, animator.speed) = (animator, seconds, speed);
        }

        private void Update()
        {
            if (left < 0f) return;
            left -= Time.deltaTime;
            if (left > 0f) return;
            if (animator) animator.speed = 1f;
            Destroy(this);
        }
    }
}
