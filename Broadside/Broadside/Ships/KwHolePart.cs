using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One of a launched ship's hull meshes (<see cref="KwHoleLooks"/>: a chunk of a few parts' looks in one material,
    /// <see cref="KwHoleChunks"/>, or a part drawn on its own), in the look's frame: its renderer's filter, its whole mesh
    /// and its bounds. It is given the holes to wear (<see cref="Want"/>) and gets there a step at a time on the main thread
    /// (<see cref="Step"/>): at its first hole the whole mesh is read into lists (<see cref="KwHoleMeshes"/>); each cut
    /// (<see cref="KwHolePatches"/>, the slow part: tens of milliseconds for a big hole in a big mesh) runs off the main
    /// thread (<see cref="KwOffMain"/>), one at a time for the mesh, and the cut mesh is written on the main thread once it
    /// is done; with no holes it wears the whole mesh again at once. Holes asked for while a cut runs are cut after it, so
    /// it always ends up wearing the last ones asked for, cut exactly as one cut would cut them.
    /// </summary>
    public sealed class KwHolePart
    {
        private static readonly List<KwHoleShape> NoHoles = new List<KwHoleShape>();
        private static readonly List<string> NoKeys = new List<string>();
        private readonly Vector3 low, high;
        private KwHolePatches? patches;
        private Mesh? holed;
        private List<KwHoleShape> holes = NoHoles;
        private List<string> keys = NoKeys;
        private KwOffMainJob<(bool Changed, KwHoleMesh? Cut, string Key)>? job;
        private long away;

        public KwHolePart(MeshFilter filter, Mesh whole)
        {
            (Filter, Whole) = (filter, whole);
            (low, high) = (whole.bounds.min, whole.bounds.max);
        }

        public MeshFilter Filter { get; }
        public Mesh Whole { get; }

        /// <summary>The keys of the holes it wears, joined; "" whole.</summary>
        public string Key { get; private set; } = "";

        /// <summary>The keys of the holes it was last given to wear (<see cref="Want"/>), joined.</summary>
        public string Wanted { get; private set; } = "";

        /// <summary>Whether a hole comes near enough to cut it: within its outline's reach and depth of its bounds.</summary>
        public bool Near(KwHoleShape hole)
        {
            float reach = hole.Reach + KwHoleCut.Inside;
            Vector3 c = hole.Centre;
            return c.x > low.x - reach && c.x < high.x + reach && c.y > low.y - reach && c.y < high.y + reach && c.z > low.z - reach && c.z < high.z + reach;
        }

        /// <summary>The holes to wear (in its frame, each with its key; lists never changed after): worn once its steps get there.</summary>
        public void Want(List<KwHoleShape> holes, List<string> keys) => (this.holes, this.keys, Wanted) = (holes, keys, string.Join(";", keys));

        /// <summary>
        /// One step towards wearing the holes wanted, on the main thread: a finished cut's mesh written, the whole mesh worn
        /// again, or the mesh read (at its first hole) and a cut started (waiting on a cut costs nothing); true once it wears
        /// them.
        /// </summary>
        public bool Step()
        {
            if (job != null)
            {
                if (!job.Done)
                    return false;
                Took();
            }
            if (Key == Wanted)
                return true;
            if (holes.Count == 0 || !Whole.isReadable)
                return Plain();
            patches ??= new KwHolePatches(KwHoleMeshes.Read(Whole));
            if (KwOffMain.Free)
                job = KwOffMain.Start(Cutting(patches, holes, keys, Wanted));
            return false;
        }

        /// <summary>How long its cuts took off the main thread since this was last asked (stopwatch ticks).</summary>
        public long TakeAway()
        {
            long took = away;
            away = 0L;
            return took;
        }

        /// <summary>
        /// A cut that threw (on the main thread or off it): logged, and the part whole again until its holes change (at once,
        /// or with a cut still running, once it ends: its lists are never touched by two threads).
        /// </summary>
        public void Fail(System.Exception e)
        {
            Log.Warn($"Broadside: a hole could not be cut into the hull: {e.Message}");
            (holes, keys, Wanted) = (NoHoles, NoKeys, "");
            if (job == null)
                Plain();
        }

        // The cut as work for the thread pool: whether it changed the mesh, the mesh (null: the whole one), the keys it was for.
        private static System.Func<(bool Changed, KwHoleMesh? Cut, string Key)> Cutting(KwHolePatches patches, List<KwHoleShape> holes, List<string> keys, string key) =>
            () => (patches.Cut(holes, keys, out KwHoleMesh? cut), cut, key);

        // The finished cut worn; a throw logged and the part whole.
        private void Took()
        {
            KwOffMainJob<(bool Changed, KwHoleMesh? Cut, string Key)> done = job!;
            job = null;
            away += done.Ticks;
            if (done.Error != null)
            {
                Fail(done.Error);
                return;
            }
            Key = done.Result.Key;
            if (done.Result.Changed)
                Wear(done.Result.Cut);
        }

        // The whole mesh worn again, its patches let go.
        private bool Plain()
        {
            Filter.sharedMesh = Whole;
            patches?.Cut(holes, keys, out _);
            Key = Wanted;
            return true;
        }

        // A cut mesh written into its own mesh and worn; the whole one worn when the cut leaves none.
        private void Wear(KwHoleMesh? cut)
        {
            if (cut == null)
            {
                Filter.sharedMesh = Whole;
                return;
            }
            holed ??= new Mesh { name = Whole.name + "_holed" };
            KwHoleMeshes.Write(cut, holed);
            Filter.sharedMesh = holed;
        }

        public void Let()
        {
            Object.Destroy(Whole);
            if (holed != null)
                Object.Destroy(holed);
        }
    }
}
