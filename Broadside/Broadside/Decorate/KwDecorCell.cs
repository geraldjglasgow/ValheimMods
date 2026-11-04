using System;

namespace Broadside
{
    /// <summary>What a cell of the read area is, by the surface found from above (<see cref="KwDecorRays"/>).</summary>
    public enum KwCellKind : byte
    {
        /// <summary>Nothing found in the height band (a drop off a high pier, open air).</summary>
        None,

        /// <summary>The game's terrain, dry.</summary>
        Ground,

        /// <summary>A floor's or dock's walkable top (any piece's flat, thin, supporting top; <see cref="KwDecorSurface.Floor"/>).</summary>
        Deck,

        /// <summary>The terrain under water (<see cref="KwCell.Depth"/> over it).</summary>
        Water,

        /// <summary>Something standing there: a chest, a table, a rock, a wall's top, a roof.</summary>
        Blocked,
    }

    /// <summary>What the reading marks on a cell (<see cref="KwDecorDecks"/>, <see cref="KwDecorKeepClear"/>, <see cref="KwDecorGround"/>).</summary>
    [Flags]
    public enum KwCellMark : ushort
    {
        None = 0,

        /// <summary>A painted path or road strip (dirt or paved, at most 4.5 m across): kept clear.</summary>
        Path = 1,

        /// <summary>In a keep-clear zone: a doorway, ladder, stairs, station's working side, chest's front, bed, portal, ship, the dry dock, the way between them.</summary>
        Clear = 2,

        /// <summary>A floor's or dock's walking lane: kept clear.</summary>
        Lane = 4,

        /// <summary>Near where one steps on or off a floor (its access): kept clear.</summary>
        Access = 8,

        /// <summary>A floor's outermost row at a drop or against a wall, with <see cref="KwCell.Out"/> the way to it.</summary>
        Edge = 16,

        /// <summary>That drop faces water.</summary>
        WaterEdge = 32,

        /// <summary>The water's edge: walkable within 1.5 m of water, or the waterline itself.</summary>
        Shore = 64,

        /// <summary>A wall face stands beside it (<see cref="KwDecorWalls"/>).</summary>
        Wall = 128,

        /// <summary>A wide floor's open middle: decorated as ground.</summary>
        Open = 256,

        /// <summary>A floor's edge band, where deck pieces stand.</summary>
        Band = 512,

        /// <summary>Blocked by something standing on a floor (the floor under it counts for the floor's width).</summary>
        Cover = 1024,

        /// <summary>The floor is wood (a fire piece the game keeps off wood stays off).</summary>
        Wood = 2048,

        /// <summary>Painted dirt or paved (path or not, worked out from its width).</summary>
        Paint = 4096,

        /// <summary>A dock's cell nearer its end than its side (its <see cref="KwCell.Out"/> points along the dock, to a pier head).</summary>
        Tip = 8192,
    }

    /// <summary>One 0.5 m cell of the read area (<see cref="KwDecorGrid"/>).</summary>
    public struct KwCell
    {
        public KwCellKind Kind;
        public KwCellMark Marks;

        /// <summary>The surface's height; a covered cell's, the floor's under what stands on it.</summary>
        public float Y;

        /// <summary>The surface normal's up part (1 flat).</summary>
        public float Up;

        /// <summary>Liquid over the surface, metres (0 dry), by the game's own water level.</summary>
        public float Depth;

        /// <summary>The floor region (<see cref="KwDecorDecks"/>), -1 for none.</summary>
        public short Region;

        /// <summary>The way (0 +x, 1 +z, 2 -x, 3 -z in the grid's frame) to the floor's edge, a wall, or the water; -1 for none.</summary>
        public sbyte Out;

        /// <summary>Metres to the nearest water, up to 16 (16 for none near).</summary>
        public float Water;

        /// <summary>The floor piece under the cell, for a deck cell.</summary>
        public Piece? Floor;

        public bool Has(KwCellMark mark) => (Marks & mark) != 0;

        /// <summary>Dry ground or a floor's top: somewhere a person walks.</summary>
        public bool Walkable => Kind == KwCellKind.Ground || Kind == KwCellKind.Deck;
    }
}
