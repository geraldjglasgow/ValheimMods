using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A part shown before it is built in its real looks, one object a material, solid, at a place: the Cabin tool's cabin
    /// (its walls and raised sides in the hull's planking, its roof in the deck boards, its posts in the plank wood), which
    /// drawn whole in the hull's material came out squashed. Made again only when what it shows changes; hidden with the
    /// tools' preview (<see cref="KwPreview.Hide"/>). Local to this player, never networked.
    /// </summary>
    public static class KwLookPreview
    {
        private static readonly List<Mesh> meshes = new List<Mesh>();
        private static GameObject? shown;
        private static string key = "";

        /// <summary>The looks at a place, made again only when next, what they are, changes.</summary>
        public static void Show(string next, Vector3 position, Quaternion turn, Func<List<(Material Material, Mesh Mesh)>> looks)
        {
            if (next != key || shown == null)
            {
                Hide();
                key = next;
                shown = new GameObject("KwLookPreview");
                foreach ((Material material, Mesh mesh) in looks())
                    Part(material, mesh);
            }
            shown.transform.SetPositionAndRotation(position, turn);
        }

        /// <summary>The preview gone, its meshes let go.</summary>
        public static void Hide()
        {
            key = "";
            if (shown != null)
                UnityEngine.Object.Destroy(shown);
            shown = null;
            foreach (Mesh mesh in meshes)
            {
                if (mesh != null)
                    UnityEngine.Object.Destroy(mesh);
            }
            meshes.Clear();
        }

        private static void Part(Material material, Mesh mesh)
        {
            meshes.Add(mesh);
            GameObject part = new GameObject("look");
            part.transform.SetParent(shown!.transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
