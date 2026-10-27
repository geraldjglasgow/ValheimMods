using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// What a tool at sea would build, or an edit's window would make, shown on the launched ship itself: blueprint pieces
    /// in the ship's frame, gathered as a ship's look gathers them (<see cref="KwBpMeshSet"/>: seated on the ship's keels,
    /// its decks' hatch openings, covers shut), combined one mesh a material under the ship's visual holder, so the ghost
    /// rides the moving ship as drawn. Solid in the parts' real looks (an edit's window, as <see cref="KwEditPreview"/>) or
    /// see-through (a tool's hover, as <see cref="KwPreview"/>). Made again only when its key changes; gone when it is not
    /// shown for a few frames (the hammer put away). Local to this screen, never networked; nothing on a dedicated server.
    /// </summary>
    public sealed class KwSeaGhost : MonoBehaviour
    {
        private const int Frames = 3;
        private static KwSeaGhost? shown;
        private readonly List<Mesh> meshes = new List<Mesh>();
        private string key = "";
        private int touched;

        /// <summary>Held: kept while not shown each frame (an open window's preview).</summary>
        public static bool Held { get; set; }

        public static void Show(KwSeaShip ship, string key, IReadOnlyList<KwBpPiece> pieces, bool solid)
        {
            if (!KwShipBody.Drawn)
                return;
            if (shown != null && shown.key == key && shown.transform.parent == ship.Visual)
            {
                shown.touched = Time.frameCount;
                return;
            }
            Clear();
            GameObject root = new GameObject("KwSeaGhost") { layer = ship.Visual.gameObject.layer };
            root.transform.SetParent(ship.Visual, false);
            shown = root.AddComponent<KwSeaGhost>();
            (shown.key, shown.touched) = (key, Time.frameCount);
            shown.Build(ship, pieces, solid);
        }

        public static void Clear()
        {
            if (shown != null)
                Destroy(shown.gameObject);
            shown = null;
        }

        // Every piece gathered in the ship's frame and drawn, a mesh a material.
        private void Build(KwSeaShip ship, IReadOnlyList<KwBpPiece> pieces, bool solid)
        {
            KwBlueprint blueprint = ship.Blueprint;
            KwBpMeshSet set = new KwBpMeshSet
            {
                Shut = true, Keels = ship.Keels, Decks = KwSole.Decks(blueprint),
                DeckHoles = (deck, def) => KwShipDeckHoles.For(blueprint, deck, def),
            };
            foreach (KwBpPiece piece in pieces)
                set.Add(piece, Vector3.zero);
            foreach ((Material material, Mesh mesh) in set.Combine("KW_sea_ghost"))
            {
                meshes.Add(mesh);
                Part(mesh, new[] { material }, Matrix4x4.identity, solid);
            }
            foreach ((Mesh mesh, Material[] materials, Matrix4x4 at) in set.Loose)
                Part(mesh, materials, at, solid);
        }

        private void Part(Mesh mesh, Material[] materials, Matrix4x4 at, bool solid)
        {
            GameObject part = new GameObject("ghost") { layer = gameObject.layer };
            part.transform.SetParent(transform, false);
            part.transform.SetLocalPositionAndRotation(at.GetColumn(3), at.rotation);
            part.transform.localScale = at.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = solid ? materials : System.Array.ConvertAll(materials, m => KwPreview.Glass(m) ?? m);
            if (!solid)
                (renderer.shadowCastingMode, renderer.receiveShadows) = (ShadowCastingMode.Off, false);
        }

        private void LateUpdate()
        {
            if (!Held && Time.frameCount - touched > Frames)
                Clear();
        }

        private void OnDestroy()
        {
            meshes.ForEach(Destroy);
            if (shown == this)
                shown = null;
        }
    }

    /// <summary>
    /// A launched ship's parts hidden on this screen only while an edit's window shows their new version
    /// (<see cref="KwSeaWindow"/>), by their renderers' <c>forceRenderingOff</c>; shown again when it ends.
    /// </summary>
    public static class KwSeaHide
    {
        private static readonly List<Renderer> hidden = new List<Renderer>();

        public static void Hide(IEnumerable<Renderer> renderers)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && !renderer.forceRenderingOff)
                {
                    renderer.forceRenderingOff = true;
                    hidden.Add(renderer);
                }
            }
        }

        private static KwSeaShip? waitFor;
        private static int waitVersion;
        private static float waitUntil;

        /// <summary>The hidden parts kept hidden until the ship redraws an edit (the one just asked) or 3 s pass, then shown.</summary>
        public static void Until(KwSeaShip ship)
        {
            (waitFor, waitVersion, waitUntil) = (ship, ship.Version, Time.time + 3f);
        }

        /// <summary>Each frame on each ship: the parts kept hidden for it shown again once it redrew or the wait is over.</summary>
        public static void Tick(KwSeaShip ship)
        {
            if (waitFor == ship && (ship.Version != waitVersion || Time.time > waitUntil))
                Show();
        }

        public static void Show()
        {
            waitFor = null;
            foreach (Renderer renderer in hidden)
            {
                if (renderer != null)
                    renderer.forceRenderingOff = false;
            }
            hidden.Clear();
        }
    }
}
