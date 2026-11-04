using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the layout asks of the world itself, past the read cells (DECORATE.md, "What a decoration needs"): the game's
    /// building rules and what stands in a decoration's way (<see cref="Allows"/>), and where a floor's edge really lies
    /// (<see cref="Line"/>). The only part of the layout that casts rays; the rest is plain arithmetic on the reading.
    /// </summary>
    public static class KwDecorWorld
    {
        /// <summary>The game's rules allow it there and nothing solid is in its way.</summary>
        public static bool Allows(KwDecorGrid grid, KwDecorPlan plan) => KwDecorRules.Allowed(grid, plan) && KwDecorSolids.Clear(plan);

        /// <summary>
        /// Where the floor under a cell's middle really ends one way, to 2 cm (the grid's rows need not meet a floor's edge
        /// at a cell's side): rays down, halving the step, the floor counted while the top is within a tolerance of the
        /// cell's (5 cm: a dock's curb or a wall is its end, for what stands on it; 30 cm: the curb is the deck, for what
        /// hangs on its side). The line's height is the cell's.
        /// </summary>
        public static Vector3 Line(Vector3 on, Vector3 way, float tolerance)
        {
            (float inside, float outside) = (0f, KwDecorGrid.Step);
            for (int i = 0; i < 5; i++)
            {
                float mid = (inside + outside) / 2f;
                (inside, outside) = Floor(on, way, mid, tolerance) ? (mid, outside) : (inside, mid);
            }
            return on + way * inside;
        }

        private static bool Floor(Vector3 on, Vector3 way, float mid, float tolerance)
        {
            Vector3 probe = on + way * mid;
            return !Physics.Raycast(on + Vector3.up * 0.3f, way, mid, KwDecorSurface.RayMask, QueryTriggerInteraction.Ignore)
                && Physics.Raycast(probe + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 0.5f + tolerance, KwDecorSurface.RayMask, QueryTriggerInteraction.Ignore)
                && Mathf.Abs(hit.point.y - on.y) <= tolerance;
        }
    }

    /// <summary>
    /// The game's own building rules for a decoration at its place, as the placement ghost's are: not in a no-build
    /// location, not in a ward the player may not build in (<c>PrivateArea.CheckAccess</c>), not inside a dungeon unless the
    /// piece may be or the world lets build there, its biome when it has one, a fire the game keeps off wood off a wooden
    /// floor, and nothing in water but a piece made for it.
    /// </summary>
    public static class KwDecorRules
    {
        public static bool Allowed(KwDecorGrid grid, KwDecorPlan plan)
        {
            Piece piece = plan.Kind.Piece;
            Vector3 at = plan.Position;
            if (Location.IsInsideNoBuildLocation(at) || !PrivateArea.CheckAccess(at, 0f, false, false))
                return false;
            if (!piece.m_allowedInDungeons && Character.InInterior(at) && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.DungeonBuild))
                return false;
            if (piece.m_onlyInBiome != Heightmap.Biome.None && (Heightmap.FindBiome(at) & piece.m_onlyInBiome) == 0)
                return false;
            if (!grid.CellOf(at, out int k))
                return plan.Kind.Fit.Hangs;
            KwCell cell = grid.Cells[k];
            return !(piece.m_notOnWood && cell.Has(KwCellMark.Wood)) && !(piece.m_noInWater && cell.Kind == KwCellKind.Water);
        }
    }

    /// <summary>
    /// Nothing solid in a decoration's way: each of its colliders' boxes at its pose, 4 cm smaller all round and lifted 6 cm
    /// off what it stands on (a hung piece moved 4 cm off its wall), tested against the world, pieces, vehicles and characters
    /// (the game refuses a piece over a player); without boxes, its footprint to its height. A dock lantern's are its lamp's
    /// and the top of its post beside the deck (its prefab's short post box); a mooring post over a dock's piling is tested
    /// past the dock's own curb (<see cref="KwDecorMoorings"/>).
    /// </summary>
    public static class KwDecorSolids
    {
        private const float Shrink = 0.04f, Lift = 0.06f, Off = 0.04f;
        private static readonly Collider[] found = new Collider[4];
        private static int mask;

        private static int Mask => mask != 0 ? mask : mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "vehicle", "character", "character_net", "character_noenv");

        public static bool Clear(KwDecorPlan plan)
        {
            KwDecorFit fit = plan.Kind.Fit;
            Vector3 nudge = fit.Hangs ? plan.Rotation * Vector3.forward * Off : Vector3.up * Lift;
            Matrix4x4 pose = Matrix4x4.TRS(plan.Position + nudge, plan.Rotation, Vector3.one);
            var solids = plan.Kind.Solids;
            if (solids.Count == 0)
                return Free(pose, new Vector3(0f, fit.Height / 2f, fit.Hangs ? fit.Depth / 2f : 0f), new Vector3(fit.Width, fit.Height, fit.Depth));
            foreach ((Matrix4x4 at, Vector3 centre, Vector3 size) in solids)
            {
                if (!Free(pose * at, centre, size))
                    return false;
            }
            return true;
        }

        private static bool Free(Matrix4x4 pose, Vector3 centre, Vector3 size)
        {
            Vector3 half = Vector3.Max(Vector3.Scale(size * 0.5f, pose.lossyScale) - Vector3.one * Shrink, Vector3.one * 0.01f);
            int hits = Physics.OverlapBoxNonAlloc(pose.MultiplyPoint3x4(centre), half, found, pose.rotation, Mask, QueryTriggerInteraction.Ignore);
            return hits == 0;
        }
    }
}
