using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// A scene's setup steps, run in real time before its shot: {"do": "reset"}, {"do": "spawns", "on": false},
    /// {"do": "purge", "kinds": ["creatures"]}, {"do": "teleport", "at": [x, z]}, {"do": "path", "x": 0, "from": -8,
    /// "to": 14}, {"do": "clear", "x": 0, "from": -8, "to": 14, "radius": 2.6}, {"do": "cues", "cues": [...]},
    /// {"do": "wait", "seconds": 1.5}. Places are in the scene's frame (x right, z ahead of its origin and yaw).
    /// </summary>
    internal static class SceneSteps
    {
        internal static IEnumerator Run(JObject step, ShotFrame frame, List<string> problems)
        {
            switch (step?.Value<string>("do"))
            {
                case "reset": ShotRunner.Restore(false); break;
                case "spawns": WorldTidy.Quiet = !(step.Value<bool?>("on") ?? true); break;
                case "purge": WorldTidy.Purge((step["kinds"] as JArray ?? new JArray()).Select(k => k.Value<string>())); break;
                case "teleport": return Teleport(step, frame);
                case "path": return Path(step, frame);
                case "clear": Clear(step, frame); break;
                case "cues": RunCues(step["cues"] as JArray, frame, problems); break;
                case "wait": return Wait(step.Value<float?>("seconds") ?? 1f);
                default: throw new BridgeException($"unknown setup step {step}");
            }
            return Nothing();
        }

        private static IEnumerator Nothing()
        {
            yield break;
        }

        private static IEnumerator Wait(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        /// <summary>Near (60 m): set straight down on the ground. Far: the game's own teleport, then wait for the area.</summary>
        private static IEnumerator Teleport(JObject step, ShotFrame frame)
        {
            Player player = Player.m_localPlayer ? Player.m_localPlayer : throw new BridgeException("no local player");
            Vector3 target = Flat(step["at"], frame);
            Quaternion facing = Quaternion.Euler(0f, frame.Yaw + (step.Value<float?>("yaw") ?? 0f), 0f);
            if (Vector3.Distance(Flat(player.transform.position), Flat(target)) < 60f)
            {
                PlayerSetup.Move(player, new Vector3(target.x, ZoneSystem.instance.GetGroundHeight(target), target.z), facing);
                yield break;
            }
            float y = Mathf.Max(30f, WorldGenerator.instance.GetHeight(target.x, target.z)) + 0.5f;
            player.TeleportTo(new Vector3(target.x, y, target.z), facing, true);
            float deadline = Time.realtimeSinceStartup + 60f;
            yield return new WaitForSecondsRealtime(0.5f);
            while (Time.realtimeSinceStartup < deadline && !ZNetScene.instance.IsAreaReady(target)) yield return null;
            while (Time.realtimeSinceStartup < deadline && player.IsTeleporting() && player.m_teleportTimer < 2.5f) yield return null;
            // Over water or a built floor the game never finds ground and would keep its loading screen up for good.
            player.m_teleporting = false;
            yield return new WaitForSecondsRealtime(step.Value<float?>("settle") ?? 3f);
        }

        /// <summary>The hoe's own path piece every metre along the scene's z axis at x: dirt, no grass.</summary>
        private static IEnumerator Path(JObject step, ShotFrame frame)
        {
            Piece path = ZNetScene.instance.GetPrefab("path")?.GetComponent<Piece>() ?? throw new BridgeException("no path piece");
            float x = step.Value<float?>("x") ?? 0f, each = Mathf.Max(0.25f, step.Value<float?>("step") ?? 1f);
            int placed = 0;
            for (float z = step.Value<float>("from"); z <= step.Value<float>("to"); z += each)
            {
                Vector3 at = frame.ToWorld(new Vector3(x, 0f, z));
                at.y = ZoneSystem.instance.GetGroundHeight(at);
                Player.m_localPlayer.PlacePiece(path, at, Quaternion.Euler(0f, frame.Yaw, 0f), false);
                if (++placed % 6 == 0) yield return null;
            }
        }

        /// <summary>Rocks, bushes, trees, logs and pickables cleared every 2 m along the scene's z axis at x.</summary>
        private static void Clear(JObject step, ShotFrame frame)
        {
            float x = step.Value<float?>("x") ?? 0f, radius = step.Value<float?>("radius") ?? 2.5f;
            for (float z = step.Value<float>("from"); z <= step.Value<float>("to"); z += 2f)
                WorldTidy.ClearAround(frame.ToWorld(new Vector3(x, 0f, z)), radius);
        }

        private static void RunCues(JArray cues, ShotFrame frame, List<string> problems)
        {
            if (cues == null) return;
            for (int i = 0; i < cues.Count; i++)
            {
                try
                {
                    Cue cue = Cue.Parse(cues[i] as JObject ?? throw new BridgeException("not an object"), i);
                    Cues.Run(cue, frame, cue.T);
                }
                catch (BridgeException error)
                {
                    problems.Add($"setup cue {i}: {error.Message}");
                }
            }
        }

        private static Vector3 Flat(JToken at, ShotFrame frame)
        {
            if (!(at is JArray xz) || xz.Count != 2) throw new BridgeException("teleport at= takes [x, z] in the scene's frame");
            return frame.ToWorld(new Vector3(xz[0].Value<float>(), 0f, xz[1].Value<float>()));
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
