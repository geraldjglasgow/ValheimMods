using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's railings drawn apart from its combined look (kept apart by <see cref="KwHoleLooks.Takes"/>), each
    /// its own renderer, so a broken stretch (<see cref="KwShipRails"/>) redraws one railing: whole as it was built while
    /// none of its stretches is broken, else broken where they are (<see cref="KwRailBroken"/>). Only where there is a screen.
    /// </summary>
    public sealed class KwShipRailLooks : MonoBehaviour
    {
        private readonly List<(KwBpPiece Piece, MeshFilter Filter, Mesh Whole)> looks = new List<(KwBpPiece, MeshFilter, Mesh)>();
        private readonly Dictionary<MeshFilter, string> drawn = new Dictionary<MeshFilter, string>();
        private readonly List<Mesh> made = new List<Mesh>();
        private KwShipRails? rails;
        private int version = -1;

        /// <summary>The railings' own looks under the look's holder, each with its piece; the meshes given are kept and let go with it.</summary>
        public static void Keep(Transform holder, List<(KwBpPiece Piece, Material Material, Mesh Mesh, Matrix4x4 At)> railings)
        {
            KwShipRailLooks looks = holder.gameObject.AddComponent<KwShipRailLooks>();
            foreach ((KwBpPiece piece, Material material, Mesh mesh, Matrix4x4 at) in railings)
                looks.looks.Add((piece, Look(holder, material, mesh, at), mesh));
        }

        /// <summary>Draws the ship's broken railings from now on (the ship's look made, <see cref="Keep"/>).</summary>
        public static void Watch(Transform ship, KwShipRails rails)
        {
            KwShipRailLooks? looks = ship.Find(KwShipPrefab.Visual)?.GetComponent<KwShipRailLooks>();
            if (looks != null)
                looks.rails = rails;
        }

        private static MeshFilter Look(Transform holder, Material material, Mesh mesh, Matrix4x4 at)
        {
            GameObject look = new GameObject("look_rail") { layer = holder.gameObject.layer };
            look.transform.SetParent(holder, false);
            look.transform.SetLocalPositionAndRotation(at.GetColumn(3), at.rotation);
            MeshFilter filter = look.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            look.AddComponent<MeshRenderer>().sharedMaterial = material;
            return filter;
        }

        private void Update()
        {
            if (rails == null || rails.Version == version || rails.Blueprint == null)
                return;
            version = rails.Version;
            for (int i = 0; i < looks.Count; i++)
                Redraw(i, rails.Blueprint.Pieces.IndexOf(looks[i].Piece));
        }

        // A railing drawn again when its broken stretches are not those it was last drawn with.
        private void Redraw(int look, int piece)
        {
            List<int> parts = new List<int>();
            foreach ((int p, int part) in rails!.Broken)
            {
                if (p == piece)
                    parts.Add(part);
            }
            parts.Sort();
            string key = string.Join(",", parts);
            if (drawn.TryGetValue(looks[look].Filter, out string was) ? was == key : key.Length == 0)
                return;
            drawn[looks[look].Filter] = key;
            Mesh? broken = parts.Count > 0 ? KwRailBroken.Build(looks[look].Piece, rails.Keels!, parts) : null;
            if (broken != null)
                made.Add(broken);
            Mesh? before = looks[look].Filter.sharedMesh;
            looks[look].Filter.sharedMesh = broken ?? looks[look].Whole;
            if (before != null && before != looks[look].Whole && made.Remove(before))
                Destroy(before);
        }

        /// <summary>
        /// A sea edit's railing drawn anew (<see cref="KwSeaRails"/>): the old piece's look let go (its broken mesh too), the
        /// new piece's added in the ship's materials, casting no shadow (the ship's boxes cast it); the broken stretches drawn
        /// again on the next frame.
        /// </summary>
        public void Swap(KwBpPiece? old, KwBpPiece? now, Material? material, Mesh? mesh, Matrix4x4 at)
        {
            int i = old != null ? looks.FindIndex(l => l.Piece == old) : -1;
            if (i >= 0)
            {
                MeshFilter filter = looks[i].Filter;
                if (filter != null && filter.sharedMesh != looks[i].Whole && made.Remove(filter.sharedMesh))
                    Destroy(filter.sharedMesh);
                Destroy(looks[i].Whole);
                drawn.Remove(filter!);
                if (filter != null)
                    Destroy(filter.gameObject);
                looks.RemoveAt(i);
            }
            if (now != null && material != null && mesh != null)
                looks.Add((now, Dressed(Look(transform, material, mesh, at)), mesh));
            version = -1;
        }

        // A new look in the ship's own materials, its shadow left to the ship's boxes.
        private static MeshFilter Dressed(MeshFilter look)
        {
            KwShipMaterials.Apply(look.transform);
            look.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return look;
        }

        /// <summary>A railing piece's look (its renderer), if it has one.</summary>
        public Renderer? Of(KwBpPiece piece)
        {
            int i = looks.FindIndex(l => l.Piece == piece);
            return i >= 0 && looks[i].Filter != null ? looks[i].Filter.GetComponent<Renderer>() : null;
        }

        private void OnDestroy()
        {
            foreach ((KwBpPiece _, MeshFilter _, Mesh whole) in looks)
                Destroy(whole);
            made.ForEach(Destroy);
        }
    }
}
