using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// Smart select: the building a blueprint piece belongs to, so a village is built one house at a time. A pure
    /// function of the blueprint's frame, worked out like this:
    /// 1. The clicked piece's storey is the lowest floor near it (the ground floor for a bedroom or a roof alike).
    /// 2. A quarter-metre plan of that storey marks what stands in it at head height (walls, doors, gates, windows,
    ///    fences, furniture) and what is roofed (roof pieces above it, their drip edge trimmed).
    /// 3. The outside is flooded in from the plan's edge over open ground; it stops at walls and under roofs. Each
    ///    roofed stretch is a building (open fronts included); so is an enclosed open area the outside cannot reach (a
    ///    pen, a courtyard), joined to the roofed part it borders most, unless it is as big as a field or a walled
    ///    village. Each building then takes the walls round it, up to 3 m thick.
    /// 4. A building's pieces: everything whose footprint centre lies on it, at any height from its floor up (props,
    ///    walls, upper storeys, the roof), pieces just outside it (posts, sills), and pieces touching those (steps up
    ///    to the door, a ridge dragon).
    /// 5. A piece in no building (a field fence, a quay tile, the town wall) selects the boxes it touches, one
    ///    cluster, capped at <see cref="ClusterCap"/>.
    /// The work is kept per blueprint instance and storey: the first click on a 10,000 piece village takes a few
    /// tens of milliseconds, later clicks next to nothing.
    /// </summary>
    public static class SmartSelect
    {
        /// <summary>The most pieces a touching cluster (a piece in no building) selects.</summary>
        public const int ClusterCap = 200;

        private const int MaxModels = 8;
        private static readonly Dictionary<Blueprint, SmartModel> models = new Dictionary<Blueprint, SmartModel>();

        /// <summary>The piece indices (into <paramref name="bp"/>.Pieces, blueprint order) of the building around piece <paramref name="index"/>.</summary>
        public static List<int> Building(Blueprint bp, int index) => Building(bp, index, GameBounds);

        /// <summary>The same with the pieces' drawn bounds at yaw 0 (pivot at the origin) from <paramref name="sizes"/>, null for unknown pieces.</summary>
        public static List<int> Building(Blueprint bp, int index, Func<string, Bounds?> sizes)
        {
            if (bp == null || index < 0 || index >= bp.Pieces.Count)
                return new List<int>();
            SmartModel model = Model(bp, sizes);
            SmartLevel level = model.LevelOf(index);
            return level.Building(index) ?? SmartCluster.Around(model, level, index, ClusterCap);
        }

        /// <summary>The pieces of the same type joined to piece <paramref name="index"/> (a wall run, a roof slope), blueprint order.</summary>
        public static List<int> SameType(Blueprint bp, int index)
        {
            if (bp == null || index < 0 || index >= bp.Pieces.Count)
                return new List<int>();
            return SmartSameType.Around(Model(bp, GameBounds), bp, index, ClusterCap * 2);
        }

        /// <summary>The blueprint's boxes and buckets with the game's drawn bounds (or <paramref name="sizes"/>, offline), for the other site classes (support).</summary>
        internal static SmartModel ModelOf(Blueprint bp, Func<string, Bounds?> sizes = null) => Model(bp, sizes ?? GameBounds);

        private static SmartModel Model(Blueprint bp, Func<string, Bounds?> sizes)
        {
            if (models.TryGetValue(bp, out SmartModel known) && known.PieceCount == bp.Pieces.Count)
                return known;
            if (models.Count >= MaxModels)
                models.Clear();
            SmartModel model = new SmartModel(bp, sizes);
            models[bp] = model;
            return model;
        }

        /// <summary>The game's drawn bounds of a piece prefab, or null when this game has no such piece.</summary>
        private static Bounds? GameBounds(string prefab)
        {
            PieceShape shape = PieceShapes.Of(prefab);
            return shape != null && shape.Template != null ? shape.Bounds : (Bounds?)null;
        }
    }
}
