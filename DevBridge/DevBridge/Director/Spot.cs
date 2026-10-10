using System;
using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// A shot's own frame: an origin and a yaw. Points in a shot are written in it (x right, y up, z ahead), so a
    /// whole scene moves or turns by changing two numbers.
    /// </summary>
    internal sealed class ShotFrame
    {
        internal static readonly ShotFrame World = new ShotFrame(Vector3.zero, 0f);

        internal readonly Vector3 Origin;
        internal readonly Quaternion Turn;
        internal readonly float Yaw;

        internal ShotFrame(Vector3 origin, float yaw)
        {
            Origin = origin;
            Yaw = yaw;
            Turn = Quaternion.Euler(0f, yaw, 0f);
        }

        internal Vector3 ToWorld(Vector3 local) => Origin + Turn * local;

        internal static ShotFrame From(JObject spec)
        {
            if (spec?["origin"] == null) return World;
            return new ShotFrame(Spot.Vector(spec["origin"], "origin"), spec.Value<float?>("yaw") ?? 0f);
        }
    }

    /// <summary>
    /// A place a cue or camera key names, resolved every frame: [x,y,z] in the shot frame; {"p":[x,y,z],"ground":true}
    /// with y above the ground there; {"world":[x,y,z]}; or {"cast":"name","bone":"Head","off":[x,y,z],"local":true}
    /// on an actor (or "player"), the offset in the actor's own frame when local, else in the shot frame; {"lens":[x,y,z]}
    /// before the camera (x right, y up, z ahead, as the camera sits now); "height": spot takes its height from another
    /// spot (a camera riding a falling body down). "snap": true
    /// reads it once, when its cue runs, and keeps that place.
    /// </summary>
    internal sealed class Spot
    {
        private Vector3 offset;
        private bool ground, world, local, snap, lens;
        private Vector3? snapped;
        private string cast, bone;
        private Spot height;

        internal bool OnActor => cast != null;
        internal string Actor => cast;

        internal static Spot Parse(JToken token, string what)
        {
            if (token == null || token.Type == JTokenType.Null) return null;
            if (token is JArray) return new Spot { offset = Vector(token, what) };
            if (token.Type == JTokenType.String) return new Spot { cast = token.Value<string>() };
            if (!(token is JObject spec)) throw new BridgeException($"{what}: a point is [x,y,z] or an object");
            var spot = new Spot { ground = spec.Value<bool?>("ground") ?? false, local = spec.Value<bool?>("local") ?? false, snap = spec.Value<bool?>("snap") ?? false };
            spot.cast = spec.Value<string>("cast");
            spot.bone = spec.Value<string>("bone");
            spot.height = Parse(spec["height"], what + " height");
            JToken at = spec["p"] ?? spec["off"] ?? spec["world"] ?? spec["lens"];
            (spot.world, spot.lens) = (spec["world"] != null, spec["lens"] != null);
            spot.offset = at != null ? Vector(at, what) : Vector3.zero;
            return spot;
        }

        /// <summary>Where the spot is now; a snap spot ("snap": true) keeps where it was the first time it was read.</summary>
        internal Vector3 Resolve(ShotFrame frame)
        {
            if (snapped.HasValue) return snapped.Value;
            Vector3 point = Locate(frame);
            if (height != null) point.y = height.Resolve(frame).y;
            if (snap) snapped = point;
            return point;
        }

        private Vector3 Locate(ShotFrame frame)
        {
            if (cast != null) return OnCast(frame);
            if (lens) return GameCamera.instance ? GameCamera.instance.transform.TransformPoint(offset) : throw new BridgeException("no game camera");
            Vector3 point = world ? offset : frame.ToWorld(offset);
            if (!ground) return point;
            float height = ZoneSystem.instance ? ZoneSystem.instance.GetGroundHeight(point) : 0f;
            return new Vector3(point.x, height + offset.y, point.z);
        }

        private Vector3 OnCast(ShotFrame frame)
        {
            Transform anchor = Cast.Anchor(cast, bone);
            if (local) return anchor.TransformPoint(offset);
            return anchor.position + frame.Turn * offset;
        }

        /// <summary>The actor's facing when the spot is on one, for keys that follow its turn.</summary>
        internal Quaternion Facing() => cast != null ? Cast.Anchor(cast, null).rotation : Quaternion.identity;

        internal static Vector3 Vector(JToken token, string what)
        {
            if (!(token is JArray array) || array.Count != 3) throw new BridgeException($"{what}: expected [x,y,z]");
            try
            {
                return new Vector3(array[0].Value<float>(), array[1].Value<float>(), array[2].Value<float>());
            }
            catch (FormatException)
            {
                throw new BridgeException($"{what}: [x,y,z] takes numbers");
            }
        }
    }
}
