using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The lantern pole seated on a dock's piling (DOCKS.md, "Piling seats") drawn and made solid without its stone cairn:
    /// over the piling its hewn post rises from the deck as the piling carried on up, and the cairn would lie on the deck's
    /// edge and hang over its side. Every triangle with a corner under 0.45 m and more than 0.13 m from the post's axis (the
    /// stones; the post itself is at most 0.124 m round) is left out of a trimmed copy of each mesh, made once for all poles,
    /// and its boxes wholly under 0.45 m (the cairn's) are switched off. A placed pole decides it itself on every peer from
    /// where it and the dock sections stand (<see cref="KwDockMooring.Seated"/>), looking again every second for 20 s while
    /// the sections round it load; nothing is saved or sent. The hammer's ghost and the Auto-decorate tool's previews are set
    /// by their placing code (<see cref="Set"/>). Meshes only where there is a screen.
    /// </summary>
    public sealed class KwDockTrim : MonoBehaviour
    {
        private const float Low = 0.45f, Post = 0.13f, Every = 1f, Looks = 20f;
        private static readonly Dictionary<Mesh, Mesh> trimmed = new Dictionary<Mesh, Mesh>();
        private readonly List<(MeshFilter Filter, Mesh Source)> meshes = new List<(MeshFilter, Mesh)>();
        private readonly List<BoxCollider> feet = new List<BoxCollider>();
        private bool read, on;
        private float wait, looked;

        /// <summary>Put on the lantern pole's piece (<see cref="KwDockLantern.Fit"/>).</summary>
        public static void Fit(GameObject piece) => piece.AddComponent<KwDockTrim>();

        /// <summary>A ghost or preview trimmed or drawn as made; nothing is added to one never trimmed.</summary>
        public static void Set(GameObject root, bool trim)
        {
            KwDockTrim? made = root.GetComponent<KwDockTrim>();
            if (made == null && !trim)
                return;
            (made ??= root.AddComponent<KwDockTrim>()).Apply(trim);
        }

        // Only a placed piece looks for itself; a ghost (no valid network view) or a preview is set from outside.
        private void Start()
        {
            ZNetView? view = GetComponent<ZNetView>();
            if (view == null || !view.IsValid())
                enabled = false;
            else
                Update();
        }

        private void Update()
        {
            wait -= Time.deltaTime;
            if (wait > 0f)
                return;
            (wait, looked) = (Every, looked + Every);
            bool seated = KwDockMooring.Seated(transform.position);
            if (seated)
                Apply(true);
            if (seated || looked > Looks)
                enabled = false;
        }

        private void Apply(bool trim)
        {
            if (!read)
                Read();
            if (trim == on)
                return;
            on = trim;
            foreach ((MeshFilter filter, Mesh source) in meshes)
                filter.sharedMesh = trim ? Trimmed(source, At(filter.transform)) : source;
            foreach (BoxCollider foot in feet)
                foot.enabled = !trim;
            KwPierBody.Resupport(GetComponent<WearNTear>());
        }

        // Its readable meshes where there is a screen, and its boxes wholly under the cut but the placing box.
        private void Read()
        {
            read = true;
            foreach (MeshFilter filter in KwIcons.CanRender ? GetComponentsInChildren<MeshFilter>(true) : new MeshFilter[0])
            {
                if (filter.sharedMesh != null && filter.sharedMesh.isReadable)
                    meshes.Add((filter, filter.sharedMesh));
            }
            foreach (BoxCollider box in GetComponentsInChildren<BoxCollider>(true))
            {
                Vector3 top = At(box.transform).MultiplyPoint3x4(box.center + Vector3.up * (box.size.y * 0.5f));
                if (box.gameObject.name != KwPrefabs.PlaceName && top.y <= Low)
                    feet.Add(box);
            }
        }

        // A part's pose from this root (the pole's own frame, as the prefab's and a preview's parts all share).
        private Matrix4x4 At(Transform part) => transform.worldToLocalMatrix * part.localToWorldMatrix;

        private static Mesh Trimmed(Mesh source, Matrix4x4 at)
        {
            if (trimmed.TryGetValue(source, out Mesh made) && made != null)
                return made;
            Vector3[] corners = source.vertices;
            made = Instantiate(source);
            made.name = source.name + "_trimmed";
            for (int s = 0; s < source.subMeshCount; s++)
                made.SetTriangles(Kept(source.GetTriangles(s), corners, at), s);
            made.RecalculateBounds();
            return trimmed[source] = made;
        }

        private static int[] Kept(int[] triangles, Vector3[] corners, Matrix4x4 at)
        {
            List<int> kept = new List<int>(triangles.Length);
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                if (Cut(corners[triangles[i]], at) || Cut(corners[triangles[i + 1]], at) || Cut(corners[triangles[i + 2]], at))
                    continue;
                kept.Add(triangles[i]);
                kept.Add(triangles[i + 1]);
                kept.Add(triangles[i + 2]);
            }
            return kept.ToArray();
        }

        // A corner of the cairn: low, and farther from the post's axis than the post's own faces.
        private static bool Cut(Vector3 corner, Matrix4x4 at)
        {
            Vector3 p = at.MultiplyPoint3x4(corner);
            return p.y < Low && new Vector2(p.x, p.z).magnitude > Post;
        }
    }
}
