using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// Local poses the studio set on this machine only (nothing is saved or sent): each node's pose from before its first
    /// change is kept, so a reset puts it back. A node that is gone (unloaded, destroyed) is forgotten.
    /// </summary>
    internal static class StudioMoves
    {
        private struct Pose
        {
            internal Vector3 Position;
            internal Quaternion Rotation;
            internal Vector3 Scale;
        }

        private static readonly Dictionary<Transform, Pose> Originals = new Dictionary<Transform, Pose>();

        internal static bool IsMoved(Transform node) => Originals.ContainsKey(node);

        internal static void Set(Transform node, Vector3? position, Vector3? rotation, Vector3? scale)
        {
            if (!Originals.ContainsKey(node))
                Originals[node] = new Pose { Position = node.localPosition, Rotation = node.localRotation, Scale = node.localScale };
            if (position.HasValue) node.localPosition = position.Value;
            if (rotation.HasValue) node.localRotation = Quaternion.Euler(rotation.Value);
            if (scale.HasValue) node.localScale = scale.Value;
        }

        /// <summary>Puts back every moved node at or under this one; returns how many.</summary>
        internal static int Reset(Transform under)
        {
            Forget();
            List<Transform> moved = Originals.Keys.Where(node => node == under || node.IsChildOf(under)).ToList();
            foreach (Transform node in moved)
            {
                Pose pose = Originals[node];
                node.localPosition = pose.Position;
                node.localRotation = pose.Rotation;
                node.localScale = pose.Scale;
                Originals.Remove(node);
            }
            return moved.Count;
        }

        private static void Forget()
        {
            foreach (Transform gone in Originals.Keys.Where(node => !node).ToList()) Originals.Remove(gone);
        }
    }
}
