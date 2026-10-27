using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// The sails' cloth in the sail and bowsprit tools' see-through previews, beside the spars (<see cref="KwPreview"/>, which
    /// draws one mesh in one look): each sail's or jib's cloth in its own material, its pattern painted on
    /// (<see cref="KwSailLooks"/>), see-through, under one object placed where the part would go. Made again only when what
    /// it shows changes, hidden with the window. Local to this player, never networked.
    /// </summary>
    public static class KwSailPreview
    {
        private static GameObject? shown;
        private static string key = "";

        /// <summary>The cloths at a place, made again only when what they show (next) changes; each a mesh, its look and where it is drawn.</summary>
        public static void Show(string next, Vector3 position, Quaternion turn, Func<List<(Mesh Mesh, Material? Look, Matrix4x4 At)>> build)
        {
            GameObject holder = Holder();
            if (holder.transform.parent != null)
                holder.transform.SetParent(null, true);
            holder.transform.SetPositionAndRotation(position, turn);
            Fill(holder, next, build);
        }

        /// <summary>
        /// The same under a parent at a place in its frame, riding with it (a launched ship's visual holder,
        /// <see cref="KwSeaRigGhost"/>): the ship moving under it carries the cloths along.
        /// </summary>
        public static void ShowOn(Transform parent, string next, Vector3 position, Quaternion turn, Func<List<(Mesh Mesh, Material? Look, Matrix4x4 At)>> build)
        {
            GameObject holder = Holder();
            if (holder.transform.parent != parent)
                holder.transform.SetParent(parent, false);
            holder.transform.SetLocalPositionAndRotation(position, turn);
            Fill(holder, next, build);
        }

        // Shown, and the cloths made again only when what they show (next) changed.
        private static void Fill(GameObject holder, string next, Func<List<(Mesh Mesh, Material? Look, Matrix4x4 At)>> build)
        {
            holder.SetActive(true);
            if (next == key)
                return;
            key = next;
            Empty(holder.transform);
            foreach ((Mesh mesh, Material? look, Matrix4x4 at) in build())
                Part(holder.transform, KwSailGhost.Baked(mesh, at), look);
        }

        public static void Hide()
        {
            if (shown != null && shown.activeSelf)
                shown.SetActive(false);
            key = "";
        }

        private static GameObject Holder()
        {
            if (shown == null)
                shown = new GameObject("KwSailPreview");
            return shown;
        }

        // One cloth, see-through, casting no shadow.
        private static void Part(Transform holder, Mesh mesh, Material? look)
        {
            GameObject part = new GameObject("cloth");
            part.transform.SetParent(holder, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = KwSailLooks.Glass(look);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // The cloths shown before, and their meshes, let go.
        private static void Empty(Transform holder)
        {
            for (int i = holder.childCount - 1; i >= 0; i--)
            {
                GameObject part = holder.GetChild(i).gameObject;
                MeshFilter? filter = part.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    Object.Destroy(filter.sharedMesh);
                part.transform.SetParent(null);
                Object.Destroy(part);
            }
        }
    }
}
