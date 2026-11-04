using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a surface found from above is (<see cref="KwDecorRays"/>): a floor (any piece's flat, thin top that supports,
    /// the game's floors, other mods' and Broadside's docks alike, but nothing that works or is sat at), something to keep
    /// clear of, the water over it by the game's own water level, painted paths. Each collider's answer kept for one reading.
    /// </summary>
    public static class KwDecorSurface
    {
        private const float FlatUp = 0.9f, Slab = 0.8f;
        private static readonly Dictionary<int, bool> floors = new Dictionary<int, bool>();
        private static int rayMask, pieceMask;

        /// <summary>What the reading's rays meet: the world, pieces and terrain; never water, ghosts, characters or vehicles.</summary>
        public static int RayMask => rayMask != 0 ? rayMask : rayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain");

        /// <summary>Building pieces only, for walls.</summary>
        public static int PieceMask => pieceMask != 0 ? pieceMask : pieceMask = LayerMask.GetMask("piece");

        /// <summary>Each collider's answer dropped (a new reading).</summary>
        public static void Forget() => floors.Clear();

        /// <summary>Whether a piece's hit top is a floor: a dock section's deck, or flat, of a thin slab, a piece that supports and is no obstacle.</summary>
        public static bool Floor(Piece piece, RaycastHit hit)
        {
            if (hit.normal.y < FlatUp)
                return false;
            int id = hit.collider.GetInstanceID();
            if (!floors.TryGetValue(id, out bool floor))
                floors[id] = floor = FloorPiece(piece, hit.collider);
            return floor;
        }

        // A Broadside dock section's deck (Docks/DOCKS.md, "Reading a dock") always is, however deep its deck box.
        private static bool FloorPiece(Piece piece, Collider collider) =>
            KwPierDeck.Of(piece) != null
            || (collider.bounds.size.y <= Slab && piece.m_category != Piece.PieceCategory.Furniture && piece.m_comfort == 0
                && !Obstacle(piece) && !Decoration(piece) && !KwDecorKeepClear.DryDock(piece.name) && Supports(piece));

        private static bool Supports(Piece piece)
        {
            WearNTear wear = piece.GetComponent<WearNTear>();
            return wear == null || wear.m_supports;
        }

        /// <summary>A piece that works, is used or is sat at, so never a floor and never decorated against.</summary>
        public static bool Obstacle(Piece piece) => Works(piece) || Used(piece);

        private static bool Works(Piece piece) =>
            Has<CraftingStation>(piece) || Has<StationExtension>(piece) || Has<Fireplace>(piece) || Has<Smelter>(piece)
            || Has<Fermenter>(piece) || Has<CookingStation>(piece) || Has<Beehive>(piece) || Has<Ship>(piece) || Has<Vagon>(piece);

        private static bool Used(Piece piece) =>
            Has<Container>(piece) || Has<Bed>(piece) || Has<Door>(piece) || Has<TeleportWorld>(piece) || Has<Sign>(piece)
            || Has<ItemStand>(piece) || piece.GetComponentInChildren<Ladder>() != null;

        private static bool Has<T>(Piece piece) where T : Component => piece.GetComponent<T>() != null;

        /// <summary>A dockside decoration (its spacing is kept, it is never stood on).</summary>
        public static bool Decoration(Piece piece) => piece.name.StartsWith(KwDockside.Prefix, System.StringComparison.Ordinal);

        /// <summary>A wood or hardwood piece (the game keeps some fires off wood).</summary>
        public static bool Wood(Piece piece)
        {
            WearNTear wear = piece.GetComponent<WearNTear>();
            return wear != null && (wear.m_materialType == WearNTear.MaterialType.Wood || wear.m_materialType == WearNTear.MaterialType.HardWood);
        }

        /// <summary>Liquid over a point by the game's own water level, calm (waves left out), metres; 0 dry.</summary>
        public static float Depth(Vector3 at)
        {
            float level = Floating.GetLiquidLevel(at, 0f);
            return level > at.y ? level - at.y : 0f;
        }

        /// <summary>Ground painted dirt or paved (a path or road, or a levelled yard: its width tells, <see cref="KwDecorGround"/>).</summary>
        public static bool Painted(Heightmap ground, Vector3 at)
        {
            Color paint = ground.GetPaintMask(at);
            return paint.r > 0.5f || paint.b > 0.5f;
        }
    }
}
