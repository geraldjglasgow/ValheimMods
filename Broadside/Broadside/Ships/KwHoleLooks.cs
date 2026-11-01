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
    /// (<see cref="KwHullHoles.Version"/>) every chunk a hole comes near is given the holes near it to wear
    /// (<see cref="KwHolePart"/>: only the group of holes that changed is cut again), so a hole at a seam goes through both
    /// parts there; a chunk whose holes are all mended wears its whole mesh again. The chunks given new holes get there a
    /// step at a time while the frame's share lasts (<see cref="KwFrameShare.Looks"/>), each cut itself off the main thread
    /// (<see cref="KwOffMain"/>): a ship loaded with its holes (every peer that loads it cuts them all, as when many players
    /// arrive and the server loads their areas) or a volley's holes never hold a frame up. Each hole's ragged outline is
    /// <see cref="KwHoleShape"/>'s, the same on every peer. Only where there is a screen; the colliders are
    /// <see cref="KwHoleBoxes"/>'.
    /// </summary>
    public sealed class KwHoleLooks : MonoBehaviour
    {
        private readonly List<KwHolePart> chunks = new List<KwHolePart>();

        // The parts drawn on their own by their blueprint index (planking and cabins, KwSeaOpeningLooks), so an edit at sea
        // draws one part again and its ghost hides just that part.
        private readonly Dictionary<int, List<KwHolePart>> own = new Dictionary<int, List<KwHolePart>>();
        private readonly List<KwHolePart> waiting = new List<KwHolePart>();
        private readonly KwHoleCutLog log = new KwHoleCutLog();
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
        /// holes cut into it from the next frame. Nothing where the ship has no hull looks (no screen).
        /// </summary>
        public static void Own(Transform visual, int index, List<KwHolePart> made)
        {
            KwHoleLooks? looks = visual.GetComponent<KwHoleLooks>();
            if (looks == null)
                return;
            if (looks.own.TryGetValue(index, out List<KwHolePart> old))
                old.ForEach(looks.Gone);
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

        // A part's mesh let go with its object, waiting no more (a cut still running for it is dropped when it ends).
        private void Gone(KwHolePart part)
        {
            waiting.Remove(part);
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
            if (holes == null || ship == null)
                return;
            if (holes.Version != drawn)
                Plan();
            if (waiting.Count > 0)
                Work();
        }

        // Every chunk whose holes near it are not the ones it was last given, given them and waiting for its turn.
        private void Plan()
        {
            drawn = holes!.Version;
            List<KwHullHole> now = holes.Holes;
            Matrix4x4 into = transform.worldToLocalMatrix * ship!.localToWorldMatrix;
            List<KwHoleShape> shapes = now.ConvertAll(h => KwHoleShape.Of(h).Moved(into.MultiplyPoint3x4, into.MultiplyVector));
            if (waiting.Count == 0)
                log.Start();
            foreach (KwHolePart chunk in chunks.Concat(own.Values.SelectMany(p => p)))
            {
                List<int> near = Enumerable.Range(0, shapes.Count).Where(i => chunk.Near(shapes[i])).ToList();
                List<string> keys = near.ConvertAll(i => now[i].Key);
                if (string.Join(";", keys) == chunk.Wanted)
                    continue;
                chunk.Want(near.ConvertAll(i => shapes[i]), keys);
                if (!waiting.Contains(chunk))
                    waiting.Add(chunk);
            }
        }

        // The waiting chunks stepped on in turn while the frame's share lasts; one that wears its holes waits no more.
        private void Work()
        {
            for (int i = 0; i < waiting.Count && KwFrameShare.Looks.Open(); i++)
            {
                KwHolePart chunk = waiting[i];
                long start = Stopwatch.GetTimestamp();
                bool done = Stepped(chunk);
                long took = Stopwatch.GetTimestamp() - start;
                KwFrameShare.Looks.Spend(took);
                log.Step(took);
                if (!done)
                    continue;
                log.Cut(chunk.TakeAway());
                waiting.RemoveAt(i--);
            }
            log.Frame(waiting.Count == 0);
        }

        // One chunk's step; a throw logged and the chunk left whole.
        private static bool Stepped(KwHolePart chunk)
        {
            try
            {
                return chunk.Step();
            }
            catch (System.Exception e)
            {
                chunk.Fail(e);
                return true;
            }
        }

        private void OnDestroy()
        {
            waiting.Clear();
            chunks.ForEach(c => c.Let());
            foreach (List<KwHolePart> parts in own.Values)
                parts.ForEach(p => p.Let());
        }
    }

    /// <summary>
    /// What cutting a ship's holes into its hull cost (<see cref="KwHoleLooks"/>), told in the log once a run of cuts is
    /// done, when it took more than <see cref="Slow"/> ms in all: how many meshes, the time off the main thread and on it,
    /// over how many frames, and the most of it in one frame (which the frame's share, <see cref="KwFrameShare"/>, keeps small).
    /// </summary>
    internal sealed class KwHoleCutLog
    {
        private const double Slow = 8.0;
        private int meshes, frames;
        private long away, main, frame, most;

        /// <summary>A run of cuts begins: counted afresh.</summary>
        public void Start() => (meshes, frames, away, main, frame, most) = (0, 0, 0L, 0L, 0L, 0L);

        /// <summary>A step's time on the main thread (stopwatch ticks).</summary>
        public void Step(long ticks) => (main, frame) = (main + ticks, frame + ticks);

        /// <summary>A mesh done, its cuts' time off the main thread (stopwatch ticks).</summary>
        public void Cut(long ticks) => (meshes, away) = (meshes + 1, away + ticks);

        /// <summary>A frame's steps done; at the end of a run, told when it was slow.</summary>
        public void Frame(bool end)
        {
            (most, frames, frame) = (System.Math.Max(most, frame), frames + 1, 0L);
            double off = Ms(away), on = Ms(main);
            if (end && off + on > Slow)
                Log.Info($"Broadside: {meshes} of a ship's hull meshes cut again for its holes in {off:0.0} ms off the main thread and {on:0.0} ms on it, over {frames} frames (at most {Ms(most):0.0} ms in one)");
        }

        private static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
    }
}
