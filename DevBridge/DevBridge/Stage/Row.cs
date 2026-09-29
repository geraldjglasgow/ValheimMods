using System.Collections.Generic;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Stage
{
    /// <summary>Where a placed object goes in the row and how it stands: side, gap to its neighbour, height, sitting, turn.</summary>
    internal sealed class Slot
    {
        internal string Side = "right";
        internal float Gap = 0.5f;
        internal float Height;
        internal bool Sit = true;
        internal float Yaw;

        internal static Slot From(BridgeRequest request, string side) => new Slot
        {
            Side = request.Get("side", side).ToLowerInvariant(),
            Gap = request.Float("gap", request.Float("spacing", 0.5f)),
            Height = request.Float("height", 0f),
            Sit = !request.Has("sit") || request.Flag("sit"),
            Yaw = request.Float("yaw", 0f),
        };
    }

    /// <summary>
    /// The row things are lined up in: it starts a few metres in front of the player, runs across their view, and every
    /// object in it faces them (its +Z, the workshop's front, towards the player). The first object stands at the
    /// origin; later ones go to its right, left or alternately, a gap from the row's end measured between their drawn
    /// meshes, each standing on the ground under it.
    /// </summary>
    internal static class Row
    {
        private static bool started;
        private static Vector3 origin;
        private static Vector3 axis;
        private static Quaternion facing;
        private static float rightEdge;
        private static float leftEdge;
        private static int count;

        internal static bool Started => started && StageRoot.Exists;

        internal static Vector3 Origin => origin;

        /// <summary>From the row towards where the player stood when it began: the side the objects face.</summary>
        internal static Vector3 Front => facing * Vector3.forward;

        internal static void Reset()
        {
            started = false;
            count = 0;
        }

        /// <summary>A new row `distance` metres in front of the player, across their view.</summary>
        internal static void Begin(float distance)
        {
            Player player = Player.m_localPlayer ? Player.m_localPlayer : throw new BridgeException("no local player");
            Vector3 forward = Vector3.ProjectOnPlane(player.m_eye.forward, Vector3.up);
            forward = forward.sqrMagnitude < 0.001f ? player.transform.forward : forward.normalized;
            origin = player.transform.position + forward * distance;
            origin.y = Floor(origin);
            axis = Vector3.Cross(Vector3.up, forward).normalized;
            facing = Quaternion.LookRotation(-forward);
            (rightEdge, leftEdge, count, started) = (0f, 0f, 0, true);
        }

        /// <summary>Puts the object into the row at its next place; returns where that is (origin, right, left).</summary>
        internal static string Put(GameObject obj, Slot slot)
        {
            obj.transform.SetPositionAndRotation(origin, facing * Quaternion.Euler(0f, slot.Yaw, 0f));
            Bounds box = AssetInfo.Bounds(obj);
            float centre = Vector3.Dot(box.center - origin, axis);
            float half = Mathf.Abs(axis.x) * box.extents.x + Mathf.Abs(axis.z) * box.extents.z;
            string side = count == 0 ? "origin" : Side(slot.Side);
            float shift = side == "right" ? rightEdge + slot.Gap - (centre - half)
                : side == "left" ? leftEdge - slot.Gap - (centre + half) : 0f;
            if (side != "left") rightEdge = Mathf.Max(rightEdge, shift + centre + half);
            if (side != "right") leftEdge = Mathf.Min(leftEdge, shift + centre - half);
            count++;
            Stand(obj, origin + axis * shift, box, slot);
            return side;
        }

        /// <summary>At an exact point outside the row, facing the player.</summary>
        internal static void PutAt(GameObject obj, Vector3 point, Slot slot)
        {
            Player player = Player.m_localPlayer;
            Vector3 toPlayer = player ? Vector3.ProjectOnPlane(player.transform.position - point, Vector3.up) : Vector3.zero;
            Quaternion turn = toPlayer.sqrMagnitude > 0.001f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity;
            obj.transform.SetPositionAndRotation(point, turn * Quaternion.Euler(0f, slot.Yaw, 0f));
            if (slot.Sit) obj.transform.position += Vector3.up * (point.y - AssetInfo.Bounds(obj).min.y);
            obj.transform.position += Vector3.up * slot.Height;
        }

        private static string Side(string wanted)
        {
            if (wanted == "right" || wanted == "left") return wanted;
            if (wanted != "both") throw new BridgeException("side= takes right, left or both");
            return count % 2 == 1 ? "right" : "left";
        }

        // On the ground under its place: its pivot there, or with sit its lowest drawn point.
        private static void Stand(GameObject obj, Vector3 place, Bounds measured, Slot slot)
        {
            float below = obj.transform.position.y - measured.min.y;
            place.y = Floor(place) + slot.Height + (slot.Sit ? below : 0f);
            obj.transform.position = place;
        }

        /// <summary>The solid ground (terrain, rock, a floor) under a point, looking from a little above it.</summary>
        internal static float Floor(Vector3 point)
        {
            if (ZoneSystem.instance && ZoneSystem.instance.GetSolidHeight(point + Vector3.up * 0.5f, out float height, 2)) return height;
            return point.y;
        }

        internal static Dictionary<string, object> Describe() => new Dictionary<string, object>
        {
            ["origin"] = Fmt.V3(origin),
            ["front"] = Fmt.V3(Front),
            ["length"] = Fmt.R(rightEdge - leftEdge),
            ["count"] = count,
        };
    }
}
