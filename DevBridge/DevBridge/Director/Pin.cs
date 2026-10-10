using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// An actor held in something's grip: {"pin": "ulf", "to": spot, "off": [x,y,z], "for": 0.6, "vel": [x,y,z],
    /// "of": "ship"} puts it on the spot (a creature's bone, say; off is added in world space) every physics step for
    /// that many film seconds, then lets go with that velocity (turned by "of"'s facing at that moment, else the shot's),
    /// and the game's physics carries it on: thrown.
    /// </summary>
    internal sealed class Pin : MonoBehaviour
    {
        private Character who;
        private Spot to;
        private ShotFrame frame;
        private Vector3 off;
        private Vector3? vel;
        private string of;
        private float left;

        internal static void Cue(JObject s, ShotFrame frame)
        {
            Actor actor = Cast.Get(s.Value<string>("pin"));
            Pin pin = actor.Go.GetComponent<Pin>() ?? actor.Go.AddComponent<Pin>();
            pin.who = actor.Character ? actor.Character : throw new BridgeException($"actor {actor.Name} is not a character");
            pin.to = Spot.Parse(s["to"], "pin to") ?? throw new BridgeException("pin needs to=");
            (pin.frame, pin.left, pin.of) = (frame, s.Value<float?>("for") ?? 1f, s.Value<string>("of"));
            pin.off = s["off"] != null ? Spot.Vector(s["off"], "pin off") : Vector3.zero;
            pin.vel = s["vel"] != null ? Spot.Vector(s["vel"], "pin vel") : (Vector3?)null;
        }

        internal static void StopAll()
        {
            foreach (Pin pin in FindObjectsByType<Pin>(FindObjectsSortMode.None)) Destroy(pin);
        }

        private void FixedUpdate()
        {
            if (!who) { Destroy(this); return; }
            left -= Time.fixedDeltaTime;
            if (left > 0f) { Hold(); return; }
            if (vel.HasValue) who.ForceJump((of != null ? Cast.Anchor(of, null).rotation : frame.Turn) * vel.Value, false);
            Destroy(this);
        }

        private void Hold()
        {
            Vector3 at = to.Resolve(frame) + off;
            who.transform.position = at;
            who.m_body.position = at;
            who.m_body.linearVelocity = Vector3.zero;
        }
    }
}
