using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The holes cut through a launched ship's look (the user, 2026-10-26: "Make it look like there's a hole in the ship,
    /// and let objects pass through it"). The hull's own looks (every planking part's strakes and painted bands, every deck,
    /// every cabin) are kept apart from the ship's combined meshes (<see cref="KwShipLook"/>, through
    /// <see cref="KwBpMeshSet.Apart"/>) and packed into a few meshes of their own a material, neighbours along the ship
    /// together (<see cref="KwHoleChunks"/>), each its own renderer. When the ship's holes change
    /// (<see cref="KwHullHoles.Version"/>) every chunk a hole comes near is cut with the holes near it (<see cref="KwHolePart"/>:
    /// only the group of holes that changed is cut again), so a hole at a seam goes through both parts there; a chunk
    /// whose holes are all mended wears its whole mesh again. Each hole's ragged outline is <see cref="KwHoleShape"/>'s,
    /// the same on every peer. Only where there is a screen; the colliders are <see cref="KwHoleBoxes"/>'.
    /// </summary>
    public sealed class KwHoleLooks : MonoBehaviour
    {
        private const double Slow = 8.0;
        private readonly List<KwHolePart> chunks = new List<KwHolePart>();

        // The parts drawn on their own by their blueprint index (planking and cabins, KwSeaOpeningLooks), so an edit at sea
        // draws one part again and its ghost hides just that part.
        private readonly Dictionary<int, List<KwHolePart>> own = new Dictionary<int, List<KwHolePart>>();
        private KwHullHoles? holes;
        private Transform? ship;
        private int drawn = -1;

        /// <summary>
        /// Whether a blueprint piece's look is kept apart from the ship's combined look: a hull part holes are cut into
        /// (planking, a deck or a cabin), or a railing, which shots break (<see cref="KwShipRailLooks"/>).
        /// </summary>
        public static bool Takes(string prefab) => prefab == KwToolPieces.PartPrefix + "planking" || prefab == KwToolPieces.PartPrefix + "deck"
            || KwHouses.IsPart(prefab) || KwRailStretches.IsRail(prefab);

        /// <summary>
        /// The hull parts' own looks, packed and drawn under the look's holder, and the railings' each drawn on its own
        /// (<see cref="KwShipRailLooks"/>); the looks given are let go.
        /// </summary>
        public static void Keep(Transform holder, List<(KwBpPiece Piece, Material Material, Mesh Mesh, Matrix4x4 At)> made)
        {
            KwShipRailLooks.Keep(holder, made.FindAll(m => KwRailStretches.IsRail(m.Piece.Prefab)));
            KwHoleLooks looks = holder.gameObject.AddComponent<KwHoleLooks>();
            foreach ((Material material, Mesh mesh) in KwHoleChunks.Combine(made.FindAll(m => !KwRailStretches.IsRail(m.Piece.Prefab))))
                looks.chunks.Add(looks.Chunk(holder, material, mesh));
        }

        /// <summary>Cuts the ship's holes into its look from now on (the ship's look made, <see cref="Keep"/>).</summary>
        public static void Watch(Transform ship, KwHullHoles holes)
        {
            KwHoleLooks? looks = ship.Find(KwShipPrefab.Visual)?.GetComponent<KwHoleLooks>();
            if (looks != null)
                (looks.ship, looks.holes) = (ship, holes);
        }

        /// <summary>
        /// A part drawn on its own (by its blueprint index; its meshes' renderers made by the caller under the look's holder)
        /// taken as hull meshes holes are cut into, in place of the ones it had, which are let go with their objects; the
        /// holes cut into it on the next frame. Nothing where the ship has no hull looks (no screen).
        /// </summary>
        public static void Own(Transform visual, int index, List<KwHolePart> made)
        {
            KwHoleLooks? looks = visual.GetComponent<KwHoleLooks>();
            if (looks == null)
                return;
            if (looks.own.TryGetValue(index, out List<KwHolePart> old))
                old.ForEach(Gone);
            looks.own[index] = made;
            looks.drawn = -1;
        }

        /// <summary>The renderers of a part drawn on its own (none for one that is not).</summary>
        public static List<Renderer> Renderers(Transform visual, int index)
        {
            KwHoleLooks? looks = visual.GetComponent<KwHoleLooks>();
            List<Renderer> renderers = new List<Renderer>();
            if (looks != null && looks.own.TryGetValue(index, out List<KwHolePart> parts))
                parts.ForEach(p => renderers.AddRange(p.Filter.GetComponents<Renderer>()));
            return renderers;
        }

        // A part's mesh let go with its object.
        private static void Gone(KwHolePart part)
        {
            part.Let();
            if (part.Filter != null)
                Destroy(part.Filter.gameObject);
        }

        private KwHolePart Chunk(Transform holder, Material material, Mesh mesh)
        {
            GameObject look = new GameObject("look_hull") { layer = holder.gameObject.layer };
            look.transform.SetParent(holder, false);
            MeshFilter filter = look.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            look.AddComponent<MeshRenderer>().sharedMaterial = material;
            return new KwHolePart(filter, mesh);
        }

        private void Update()
        {
            if (holes == null || ship == null || holes.Version == drawn)
                return;
            drawn = holes.Version;
            Stopwatch watch = Stopwatch.StartNew();
            int changed = Refresh(holes.Holes);
            if (watch.Elapsed.TotalMilliseconds > Slow)
                Log.Info($"Broadside: {changed} of a ship's hull meshes cut again for its holes in {watch.Elapsed.TotalMilliseconds:0.0} ms");
        }

        // Every chunk whose holes near it are not those it was last cut with, cut again; how many.
        private int Refresh(List<KwHullHole> now)
        {
            Matrix4x4 into = transform.worldToLocalMatrix * ship!.localToWorldMatrix;
            List<KwHoleShape> shapes = now.ConvertAll(h => KwHoleShape.Of(h).Moved(into.MultiplyPoint3x4, into.MultiplyVector));
            int changed = 0;
            foreach (KwHolePart chunk in chunks.Concat(own.Values.SelectMany(p => p)))
            {
                List<int> near = Enumerable.Range(0, shapes.Count).Where(i => chunk.Near(shapes[i])).ToList();
                List<string> keys = near.ConvertAll(i => now[i].Key);
                if (string.Join(";", keys) == chunk.Key)
                    continue;
                Guarded(chunk, near.ConvertAll(i => shapes[i]), keys);
                changed++;
            }
            return changed;
        }

        // One chunk cut; a throw logged and the chunk left whole.
        private static void Guarded(KwHolePart chunk, List<KwHoleShape> shapes, List<string> keys)
        {
            try
            {
                chunk.Cut(shapes, keys);
            }
            catch (System.Exception e)
            {
                Log.Warn($"Broadside: a hole could not be cut into the hull: {e.Message}");
                chunk.Cut(new List<KwHoleShape>(), new List<string>());
            }
        }

        private void OnDestroy()
        {
            chunks.ForEach(c => c.Let());
            foreach (List<KwHolePart> parts in own.Values)
                parts.ForEach(p => p.Let());
        }
    }
}
