using System.Collections.Generic;
using EarthWright.Core;

namespace EarthWright.Terrain
{
    /// <summary>
    /// The terrain math. <see cref="Apply"/> runs on the owner of a compiler and writes its arrays (the caller saves);
    /// <see cref="Estimate"/> runs on the sender against this machine's copy of the terrain, with the same planners
    /// (<see cref="EnginePlanner"/>), for the preview and the costs. Both never throw: a failure is logged and changes nothing.
    /// </summary>
    public static class Engine
    {
        /// <summary>Owner: applies the edit to the compiler's height and paint arrays. Does not save or rebuild.</summary>
        public static EditResult Apply(TerrainComp comp, TerrainEdit edit)
        {
            return Safe.Call("EarthWright engine (apply)", () => EngineApply.Run(comp, edit), default(EditResult));
        }

        /// <summary>Sender: what the edit would change, read from the local terrain (compilers and heightmaps). Writes nothing. <paramref name="withChanges"/> fills the per-vertex list.</summary>
        public static EditEstimate Estimate(TerrainEdit edit, bool withChanges = false)
        {
            return Safe.Call("EarthWright engine (estimate)", () => EngineEstimate.Run(edit, withChanges), new EditEstimate());
        }

        /// <summary>
        /// Metres beyond the edit's own area (<see cref="TerrainEdit.GetArea"/>) the owner may also change: the gentle
        /// slopes radius when that setting is on and the edit reshapes the ground, else 0. Undo snapshots and ward checks
        /// that cover the edit's area should widen it by this.
        /// </summary>
        public static float ExtraReach(TerrainEdit edit) => SlopeRelax.Applies(edit) ? SlopeSettings.RadiusValue : 0f;
    }

    /// <summary>What an applied edit changed on one compiler.</summary>
    public struct EditResult
    {
        public bool HeightChanged;
        public bool PaintChanged;

        /// <summary>At least one vertex was held back by a height limit.</summary>
        public bool HitLimit;

        public bool Changed => HeightChanged || PaintChanged;
    }

    /// <summary>What an edit would change, summed over every compiler it touches.</summary>
    public sealed class EditEstimate
    {
        /// <summary>Vertices whose height would change.</summary>
        public int Vertices;

        /// <summary>Paint cells that would change.</summary>
        public int PaintCells;

        /// <summary>Paint cells that would newly become paved (for a stone cost per square metre paved).</summary>
        public int PavedCells;

        /// <summary>Cubic metres added (each vertex stands for one square metre).</summary>
        public float Raised;

        /// <summary>Cubic metres removed.</summary>
        public float Lowered;

        /// <summary>At least one vertex would be held back by a height limit.</summary>
        public bool HitLimit;

        /// <summary>Per-vertex changes, for the grid preview (only filled when asked for).</summary>
        public readonly List<VertexChange> Changes = new List<VertexChange>();
    }

    /// <summary>One vertex of an estimate: world position (X, Z), height before and after.</summary>
    public struct VertexChange
    {
        public float X;
        public float Z;
        public float Before;
        public float After;
        public bool Limited;
    }
}
