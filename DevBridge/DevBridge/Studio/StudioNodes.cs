using System.Collections.Generic;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// A target's transforms as the studio lists them: each addressed by child indexes from the target's root ("" the
    /// root, "0/2" the root's first child's third child), with its local pose, its position in the root's space, what it
    /// holds and whether the studio moved or attached it. Values to three decimals; rotations as -180..180 angles.
    /// </summary>
    internal static class StudioNodes
    {
        private const int Most = 600;

        internal static Transform At(GameObject root, string path)
        {
            Transform node = root.transform;
            if (string.IsNullOrEmpty(path)) return node;
            foreach (string step in path.Split('/'))
            {
                if (!int.TryParse(step, out int index) || index < 0 || index >= node.childCount)
                    throw new BridgeException($"no node {path} on {root.name} (the tree changed: fetch it again)");
                node = node.GetChild(index);
            }
            return node;
        }

        internal static List<Dictionary<string, object>> Tree(GameObject root)
        {
            var lines = new List<Dictionary<string, object>>();
            Walk(root.transform, root.transform, "", lines);
            return lines;
        }

        private static void Walk(Transform root, Transform node, string path, List<Dictionary<string, object>> lines)
        {
            if (lines.Count >= Most) return;
            lines.Add(Describe(root, node, path));
            for (int i = 0; i < node.childCount; i++)
                Walk(root, node.GetChild(i), path.Length == 0 ? i.ToString() : path + "/" + i, lines);
        }

        internal static Dictionary<string, object> Describe(Transform root, Transform node, string path) => new Dictionary<string, object>
        {
            ["path"] = path,
            ["name"] = node.name,
            ["depth"] = path.Length == 0 ? 0 : path.Split('/').Length,
            ["active"] = node.gameObject.activeInHierarchy,
            ["kinds"] = Kinds(node),
            ["plays"] = StudioPlayer.CanPlay(node),
            ["position"] = Fine(node.localPosition),
            ["rotation"] = Fine(Angles(node.localEulerAngles)),
            ["scale"] = Fine(node.localScale),
            ["fromRoot"] = Fine(root.InverseTransformPoint(node.position)),
            ["moved"] = StudioMoves.IsMoved(node),
            ["attached"] = StudioAttach.IsAttached(node),
        };

        private static List<string> Kinds(Transform node)
        {
            var kinds = new List<string>();
            if (node.GetComponent<MeshRenderer>() || node.GetComponent<SkinnedMeshRenderer>()) kinds.Add("mesh");
            if (node.GetComponent<ParticleSystem>()) kinds.Add("particles");
            if (node.GetComponent<Animator>()) kinds.Add("animator");
            if (node.GetComponent<Animation>()) kinds.Add("animation");
            if (node.GetComponent<Light>()) kinds.Add("light");
            if (node.GetComponent<AudioSource>()) kinds.Add("sound");
            if (node.GetComponent<Collider>()) kinds.Add("collider");
            return kinds;
        }

        private static Vector3 Angles(Vector3 euler) => new Vector3(Signed(euler.x), Signed(euler.y), Signed(euler.z));

        private static float Signed(float angle) => angle > 180f ? angle - 360f : angle;

        private static float[] Fine(Vector3 value) =>
            new[] { Mathf.Round(value.x * 1000f) / 1000f, Mathf.Round(value.y * 1000f) / 1000f, Mathf.Round(value.z * 1000f) / 1000f };
    }
}
