using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>Carries out one cue now, in a shot's frame; `now` is the film time it runs at (for moves started late).</summary>
    internal static class Cues
    {
        private delegate void Handler(Cue cue, JObject s, ShotFrame frame, float now);

        /// <summary>What each kind of cue does; kinds not listed run the endpoint of that name (call, eval, console, light, sound, effect).</summary>
        private static readonly Dictionary<string, Handler> Handlers = new Dictionary<string, Handler>
        {
            ["camera"] = (c, s, f, now) => CameraCue(s["camera"], f, now - c.T),
            ["spawn"] = (c, s, f, now) => Spawn(s, f),
            ["adopt"] = (c, s, f, now) => Adopt(s, f),
            ["ai"] = (c, s, f, now) => Ai(s),
            ["anim"] = (c, s, f, now) => Animate.Cue(s),
            ["pin"] = (c, s, f, now) => Pin.Cue(s, f),
            ["stand"] = (c, s, f, now) => Stands.Cue(s, f),
            ["focus"] = (c, s, f, now) => FocusPull.Cue(s["focus"]),
            ["blink"] = (c, s, f, now) => Eyelids.Cue(s["blink"]),
            ["attack"] = (c, s, f, now) => AttackCue(s),
            ["hero"] = (c, s, f, now) => Puppet.Set(Part(s, "hero")),
            ["extra"] = (c, s, f, now) => Extras.Spawn(s, f),
            ["act"] = (c, s, f, now) => Extras.PilotOf(Cast.Get(s.Value<string>("act"))).Set(s),
            ["fling"] = (c, s, f, now) => Fling(s, f),
            ["lights"] = (c, s, f, now) => SetLights.Apply(Part(s, "lights"), f),
            ["player"] = (c, s, f, now) => PlayerSetup.Apply(Part(s, "player"), f),
            ["time"] = (c, s, f, now) => TimeCue(s["time"]),
            ["shake"] = (c, s, f, now) => Shake.Kick(s.Value<float>("shake"), s.Value<float?>("decay") ?? 0.35f, CameraRig.Clock),
            ["hide"] = (c, s, f, now) => Hider.Set(Cast.Get(s.Value<string>("hide")).Go, s.Value<bool?>("on") ?? true),
            ["remove"] = (c, s, f, now) => Cast.Remove(s.Value<string>("remove"), true),
            ["mark"] = (c, s, f, now) => { },
            ["clean"] = (c, s, f, now) => Clean.Set(s.Value<bool>("clean")),
            ["end"] = (c, s, f, now) => ShotRunner.EndAfterThisFrame(),
            ["sfx"] = (c, s, f, now) => Fx(s, "sfx", f),
            ["vfx"] = (c, s, f, now) => Fx(s, "vfx", f),
            ["prop"] = (c, s, f, now) => Props.Add(s),
            ["world"] = (c, s, f, now) => World(Part(s, "world")),
            ["env"] = (c, s, f, now) => Env(Part(s, "env")),
            ["title"] = (c, s, f, now) => TitleCard.Title(s["title"]),
            ["fade"] = (c, s, f, now) => TitleCard.Fade(s["fade"]),
        };

        internal static void Run(Cue cue, ShotFrame frame, float now)
        {
            if (Handlers.TryGetValue(cue.Kind, out Handler handler)) handler(cue, cue.Spec, frame, now);
            else Call(cue.Kind, cue.Spec);
        }

        private static JObject Part(JObject s, string key) => s[key] as JObject ?? throw new BridgeException($"{key} takes an object");

        /// <summary>
        /// A character thrown: its body launched off the ground at "vel" ([x, y, z] m/s, in the frame of the "of" actor
        /// when given, else the shot's), the game's own jump launch, so it flies and falls as the game's physics carry it.
        /// </summary>
        private static void Fling(JObject s, ShotFrame frame)
        {
            Character who = Cast.Get(s.Value<string>("fling")).Character ?? throw new BridgeException("fling takes a character");
            Vector3 vel = Spot.Vector(s["vel"] ?? throw new BridgeException("fling needs vel=[x,y,z]"), "fling vel");
            Quaternion turn = s["of"] != null ? Cast.Anchor(s.Value<string>("of"), null).rotation : frame.Turn;
            who.ForceJump(turn * vel, s.Value<bool?>("effects") ?? false);
        }

        /// <summary>An attack now, on a target; skip= starts its animation that many seconds in.</summary>
        private static void AttackCue(JObject s)
        {
            Actor actor = Cast.Get(s.Value<string>("attack"));
            DevBridgePlugin.Instance.StartCoroutine(ActorControl.AttackSoon(actor, s.Value<string>("item"), Target(s.Value<string>("target")),
                s["skip"] != null ? s.Value<float>("skip") : -1f));
        }

        private static void CameraCue(JToken spec, ShotFrame frame, float late)
        {
            if (spec.Type == JTokenType.String && spec.Value<string>() == "off") CameraRig.Off();
            else if (spec.Type == JTokenType.String && spec.Value<string>() == "freeze") CameraRig.Freeze();
            else CameraRig.Set(CameraMove.Parse(spec as JObject ?? throw new BridgeException("camera takes an object or \"off\""), frame), late);
        }

        private static void Spawn(JObject s, ShotFrame frame)
        {
            Vector3 at = Spot.Parse(s["at"], "spawn at")?.Resolve(frame) ?? throw new BridgeException("spawn needs at=");
            Quaternion rotation = Facing(s, frame, at);
            Actor actor = Cast.Spawn(s.Value<string>("spawn"), s.Value<string>("as") ?? s.Value<string>("spawn"), at, rotation, s.Value<int?>("level") ?? 1);
            actor.Held = s.Value<bool?>("hold") ?? true;
            actor.Face = Spot.Parse(s["face"], "spawn face");
            if (s["zdo"] is JObject values) Stamp(actor, values);
        }

        /// <summary>
        /// Values written into a new actor's ZDO the frame it spawns, before other mods' Start reads them: "zdo":
        /// {"key": true | 3 | 0.5 | "text"} (a mod that rolls traits on spawn can be told they are rolled already).
        /// </summary>
        private static void Stamp(Actor actor, JObject values)
        {
            ZDO zdo = actor.Go.GetComponent<ZNetView>()?.GetZDO() ?? throw new BridgeException($"actor {actor.Name} has no ZDO");
            foreach (JProperty p in values.Properties())
            {
                switch (p.Value.Type)
                {
                    case JTokenType.Boolean: zdo.Set(p.Name, p.Value.Value<bool>()); break;
                    case JTokenType.Integer: zdo.Set(p.Name, p.Value.Value<int>()); break;
                    case JTokenType.Float: zdo.Set(p.Name, p.Value.Value<float>()); break;
                    default: zdo.Set(p.Name, p.Value.Value<string>()); break;
                }
            }
        }

        private static void Adopt(JObject s, ShotFrame frame)
        {
            Vector3 near = Spot.Parse(s["near"], "adopt near")?.Resolve(frame) ?? Player.m_localPlayer.transform.position;
            Actor actor = Cast.Adopt(s.Value<string>("adopt"), s.Value<string>("as") ?? s.Value<string>("adopt"), near, s.Value<float?>("radius") ?? 60f,
                s.Value<bool?>("newest") ?? false);
            actor.Held = s.Value<bool?>("hold") ?? false;
        }

        /// <summary>A game sound or effect prefab played here, at a spot (on an actor it stays where it started).</summary>
        private static void Fx(JObject s, string kind, ShotFrame frame)
        {
            string name = s.Value<string>(kind);
            GameObject prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(name) : null;
            if (!prefab) throw new BridgeException($"no prefab {name}");
            Vector3 at = Spot.Parse(s["at"], kind + " at")?.Resolve(frame) ?? GameCamera.instance.transform.position;
            GameObject played = Object.Instantiate(prefab, at, Quaternion.identity);
            float scale = s.Value<float?>("scale") ?? 1f;
            if (scale != 1f) played.transform.localScale *= scale;
        }

        /// <summary>spawns=false stops natural spawning; purge=[tombstones, items, creatures] clears them world-wide.</summary>
        private static void World(JObject spec)
        {
            if (spec["spawns"] != null) WorldTidy.Quiet = !spec.Value<bool>("spawns");
            if (spec["purge"] is JArray kinds) WorldTidy.Purge(kinds.Select(k => k.Value<string>()));
        }

        /// <summary>Makes a film environment and, unless use=false, switches the light to it.</summary>
        private static void Env(JObject spec)
        {
            string name = FilmEnv.Make(spec);
            if (spec.Value<bool?>("use") ?? true) Calls.Invoke("/light", new Dictionary<string, string> { ["env"] = name });
        }

        /// <summary>yaw= in the shot frame, or face= a spot.</summary>
        internal static Quaternion Facing(JObject s, ShotFrame frame, Vector3 at)
        {
            Spot face = Spot.Parse(s["face"], "face");
            if (face == null) return Quaternion.Euler(0f, frame.Yaw + (s.Value<float?>("yaw") ?? 0f), 0f);
            Vector3 flat = Vector3.ProjectOnPlane(face.Resolve(frame) - at, Vector3.up);
            return flat.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(flat) : Quaternion.identity;
        }

        private static void Ai(JObject s)
        {
            Actor actor = Cast.Get(s.Value<string>("ai"));
            if (s["moveTo"] != null) (actor.Held, actor.MoveTo) = (true, Spot.Parse(s["moveTo"], "ai moveTo"));
            if (s["face"] != null) (actor.Held, actor.Face) = (true, Spot.Parse(s["face"], "ai face"));
            actor.Run = s.Value<bool?>("run") ?? actor.Run;
            actor.Straight = s.Value<bool?>("straight") ?? actor.Straight;
            actor.Arrive = s.Value<float?>("arrive") ?? actor.Arrive;
            if (s.Value<bool?>("hold") == true) actor.Held = true;
            if (s.Value<bool?>("wake") == true && actor.Ai is MonsterAI sleeper) sleeper.Wakeup();
            if (s.Value<bool?>("hold") == false || s["target"] != null) ActorControl.Release(actor, Target(s.Value<string>("target")));
        }

        internal static Character Target(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Actor actor = Cast.Get(name);
            return actor.Character ? actor.Character : throw new BridgeException($"actor {name} is not a character");
        }

        private static void TimeCue(JToken spec)
        {
            if (spec.Type == JTokenType.String && spec.Value<string>() == "reset") TimeRamp.Reset("the shot asked");
            else if (spec is JObject o) TimeRamp.Start(o.Value<float?>("scale") ?? 1f, o.Value<float?>("over") ?? 0f);
            else TimeRamp.Start(spec.Value<float>(), 0f);
        }

        /// <summary>call, eval, console, light, sound and effect: an endpoint run here and now.</summary>
        private static void Call(string kind, JObject s)
        {
            switch (kind)
            {
                case "call": Calls.Invoke(s.Value<string>("call"), Args(s["args"] as JObject)); break;
                case "eval": Calls.Invoke("/eval", new Dictionary<string, string> { ["expr"] = s.Value<string>("eval") }); break;
                case "console": Calls.Invoke("/console", new Dictionary<string, string> { ["cmd"] = s.Value<string>("console") }); break;
                default: Calls.Invoke("/" + kind, Args(s[kind] as JObject)); break;
            }
        }

        /// <summary>A JSON object as endpoint arguments: numbers in invariant form, arrays joined with commas.</summary>
        internal static Dictionary<string, string> Args(JObject spec)
        {
            var args = new Dictionary<string, string>();
            if (spec == null) return args;
            foreach (JProperty p in spec.Properties()) args[p.Name] = Text(p.Value);
            return args;
        }

        private static string Text(JToken value)
        {
            if (value is JArray array) return string.Join(",", array.Select(Text));
            if (value.Type == JTokenType.Float) return value.Value<double>().ToString(CultureInfo.InvariantCulture);
            if (value.Type == JTokenType.Boolean) return value.Value<bool>() ? "1" : "0";
            return value.ToString();
        }
    }
}
