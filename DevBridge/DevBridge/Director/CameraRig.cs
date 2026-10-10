using System.Collections.Generic;
using DevBridge.Server;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// The director's camera: while a move is set it places the game camera after the game has (GameCamera.LateUpdate),
    /// with its lens, near plane and the listener at the camera, so the film hears what it sees. The player stays where
    /// they are; nothing is carried along as with the free-fly camera.
    /// </summary>
    internal static class CameraRig
    {
        private static CameraMove move;
        private static float elapsed, clock, savedNear = -1f;
        private static int setFrame = -1;

        internal static bool Active => move != null;
        internal static float Elapsed => elapsed;
        internal static float Clock => clock;

        internal static void Set(CameraMove next, float startAt)
        {
            next.Prime();
            move = next;
            elapsed = startAt;
            if (!next.HasFocus) Focus.Release();
            setFrame = Time.frameCount;
        }

        /// <summary>The camera held where it is now, looking where it looks now, with the lens it has now.</summary>
        internal static void Freeze() => Set(CameraMove.Parse(Here(0f), ShotFrame.World), 0f);

        /// <summary>A camera key at t for where the camera is now, looking where it looks now, with the lens it has now.</summary>
        internal static JObject Here(float t)
        {
            GameCamera camera = GameCamera.instance ? GameCamera.instance : throw new BridgeException("no game camera");
            Transform at = camera.transform;
            return new JObject
            {
                ["t"] = t,
                ["pos"] = new JObject { ["world"] = Point(at.position) },
                ["look"] = new JObject { ["world"] = Point(at.position + at.forward * 10f) },
                ["fov"] = camera.m_camera.fieldOfView,
            };
        }

        private static JArray Point(Vector3 p) => new JArray(p.x, p.y, p.z);

        internal static void Off()
        {
            move = null;
            Focus.Release();
            if (savedNear > 0f && GameCamera.instance) GameCamera.instance.m_camera.nearClipPlane = savedNear;
            savedNear = -1f;
        }

        internal static void Tick(float delta)
        {
            clock += delta;
            if (move != null && Time.frameCount != setFrame) elapsed += delta;
        }

        internal static void Apply(GameCamera camera)
        {
            if (move == null) return;
            Vector3 position;
            Quaternion rotation;
            float fov;
            Vector3? focus;
            try
            {
                move.Evaluate(elapsed, out position, out rotation, out fov);
                focus = move.FocusPoint();
            }
            catch (BridgeException error)
            {
                ShotRunner.Report("camera: " + error.Message);
                Off();
                return;
            }
            rotation *= Shake.Offset(clock, move.HandAmp, move.HandFreq);
            Place(camera, position, rotation, fov);
            if (focus.HasValue) Focus.Hold(focus.Value, move.Aperture, move.Blur);
        }

        private static Transform lens;

        /// <summary>
        /// Where the film's lens is, moved only here, after the game has placed its camera: props held "on the camera"
        /// hang from it, so the game's own camera moves within a frame never drag their particles about.
        /// </summary>
        internal static Transform Lens
        {
            get
            {
                if (!lens) lens = new GameObject("DirectorLens").transform;
                return lens;
            }
        }

        /// <summary>After the game's camera update (and the move, if one is set): the lens follows the camera.</summary>
        internal static void FollowLens(GameCamera camera)
        {
            if (lens) lens.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
        }

        private static void Place(GameCamera camera, Vector3 position, Quaternion rotation, float fov)
        {
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.m_camera.fieldOfView = fov;
            camera.m_skyCamera.fieldOfView = fov;
            if (savedNear < 0f) savedNear = camera.m_camera.nearClipPlane;
            camera.m_camera.nearClipPlane = move.Near;
            if (camera.m_listner) camera.m_listner.transform.position = position;
        }

        internal static Dictionary<string, object> Describe()
        {
            GameCamera camera = GameCamera.instance;
            return new Dictionary<string, object>
            {
                ["active"] = Active,
                ["elapsed"] = Fmt.R(elapsed),
                ["length"] = move != null ? Fmt.R(move.Length) : (object)null,
                ["position"] = camera ? Fmt.V3(camera.transform.position) : null,
                ["forward"] = camera ? Fmt.V3(camera.transform.forward) : null,
                ["fov"] = camera ? Fmt.R(camera.m_camera.fieldOfView) : (object)null,
            };
        }
    }

    [HarmonyPatch(typeof(GameCamera), "LateUpdate")]
    internal static class CameraRigPatch
    {
        private static void Postfix(GameCamera __instance)
        {
            CameraRig.Apply(__instance);
            CameraRig.FollowLens(__instance);
        }
    }

    /// <summary>Handheld sway (smooth noise) and impact kicks that die away, as a small turn of the camera.</summary>
    internal static class Shake
    {
        private static float kickAmp, kickDecay = 0.3f, kickAt;

        internal static void Kick(float amp, float decay, float now)
        {
            kickAmp = amp;
            kickDecay = Mathf.Max(0.02f, decay);
            kickAt = now;
        }

        internal static Quaternion Offset(float time, float handAmp, float handFreq)
        {
            float yaw = Noise(time * handFreq, 0.1f) * handAmp, pitch = Noise(time * handFreq, 7.3f) * handAmp;
            float roll = Noise(time * handFreq, 13.7f) * handAmp * 0.5f;
            float kick = kickAmp * Mathf.Exp(-(time - kickAt) / kickDecay);
            if (kick > 0.001f)
            {
                yaw += Noise(time * 17f, 3.1f) * kick;
                pitch += Noise(time * 19f, 5.9f) * kick;
            }
            return Quaternion.Euler(pitch, yaw, roll);
        }

        private static float Noise(float x, float seed) => (Mathf.PerlinNoise(x, seed) - 0.5f) * 2f;
    }
}
