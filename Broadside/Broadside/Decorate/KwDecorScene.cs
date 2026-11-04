using System;
using System.Collections.Generic;

namespace Broadside
{
    /// <summary>Which way a scene's front (its +Z, the side people work from) is turned where it stands.</summary>
    public enum KwFacing
    {
        /// <summary>Toward the most open walkable ground round it (its working space), ties toward the yard's heart.</summary>
        Open,

        /// <summary>Toward the nearest water.</summary>
        Water,

        /// <summary>Toward the nearest path or path end (a door, a dock's entry); with none near, as <see cref="Open"/>.</summary>
        Path,

        /// <summary>Toward the yard's heart (the dry dock, else a ship or workbench, else the open ground's middle).</summary>
        Heart,

        /// <summary>Toward the water when it is within 8 m, else toward the dry dock (a ship slip).</summary>
        Slip,
    }

    /// <summary>What a scene must have where it stands (any mix); a place without them is never tried.</summary>
    [Flags]
    public enum KwNeed
    {
        None = 0,

        /// <summary>Its front within 1.5 m of the water (at the water's edge).</summary>
        WaterFront = 1,

        /// <summary>Its front within 3 m of the water, or within 6 m of the dry dock (a slip beside either).</summary>
        Slip = 2,

        /// <summary>Its middle within 14 m of the water (near the docks).</summary>
        NearWater = 4,

        /// <summary>Its middle within 7 m of where a dock is stepped onto (by the quay).</summary>
        Quay = 8,

        /// <summary>Its front within 2.5 m of a path or a path end (facing it).</summary>
        PathFront = 16,

        /// <summary>Its core on the land itself, never on a floor (a shed, a slip, a fire, a heap, a vane stand on the ground).</summary>
        Land = 32,
    }

    /// <summary>What a scene likes about a place (any mix): each adds to the place's score; the best that fits whole wins.</summary>
    [Flags]
    public enum KwLike
    {
        None = 0,

        /// <summary>Near the dry dock, a ship or a workbench.</summary>
        Yard = 1,

        /// <summary>Its back against a building's wall.</summary>
        BackToWall = 2,

        /// <summary>Back from the water's edge (4 m or more).</summary>
        Dry = 4,

        /// <summary>Near the yard's heart.</summary>
        Central = 8,

        /// <summary>Out of the way: far from the heart, near the area's side.</summary>
        Outskirts = 16,

        /// <summary>High ground.</summary>
        High = 32,

        /// <summary>At a way into the yard (a dock's entry, a door, a path at the area's side).</summary>
        Entrance = 64,

        /// <summary>Beside a path.</summary>
        Path = 128,

        /// <summary>Near where a dock is stepped onto.</summary>
        Quay = 256,

        /// <summary>Near the water.</summary>
        Wet = 512,
    }

    /// <summary>
    /// One piece of a scene (DECORATE.md, "Scenes"): which decoration (its id, the prefab without <c>BS_dock_</c>), where in
    /// the scene's frame (metres right and toward the front, +Z, from its middle), turned how far from the scene's front
    /// (degrees, as a piece's own turn: 90 puts its front to the scene's right), in what kind of place, and how many
    /// densities after its scene it first shows (0: one of the scene's core, without which the scene is not placed;
    /// more: a filler, left out when it does not fit). <see cref="Ends"/>: its two ends differ (the steam box's kettle,
    /// the rope walk's jack), so a mirrored scene turns it round instead of mirroring its turn. A wall piece is hung on a
    /// building's wall facing the scene within 7 m, its place in the frame ignored. A piece not in the game yet (the pit
    /// saw, the figurehead bench) is left out until it is.
    /// </summary>
    public sealed class KwSceneMember
    {
        public KwSceneMember(string piece, float x, float z, float yaw = 0f, int extra = 0, KwDecorPlace place = KwDecorPlace.Ground, bool ends = false)
        {
            (Piece, X, Z, Yaw, Extra, Place, Ends) = (piece, x, z, yaw, extra, place, ends);
        }

        public string Piece { get; }
        public float X { get; }
        public float Z { get; }
        public float Yaw { get; }
        public int Extra { get; }
        public KwDecorPlace Place { get; }
        public bool Ends { get; }
    }

    /// <summary>
    /// A scene (a vignette, DECORATE.md "Scenes"): a small hand-made arrangement of decorations that belong together,
    /// placed whole or not at all. Its name (the player's words), the density that first brings it in, which way its front
    /// turns, what it must have and what it likes where it stands, its pieces, its aprons (the working space in front, in
    /// its frame: kept walkable, or walkable or water, and free of other scenes), the clear margin round it, and how
    /// many a large area may hold (a second, if any, coming in <see cref="Later"/> densities after the first and at least
    /// <see cref="Apart"/> metres from it). Plain data.
    /// </summary>
    public sealed class KwDecorScene
    {
        public KwDecorScene(string id, string name, int level, KwFacing facing)
        {
            (Id, Name, Level, Facing) = (id, name, level, facing);
        }

        public string Id { get; }

        /// <summary>The player's words, as the lines list what shows ("a carpentry yard").</summary>
        public string Name { get; }

        public int Level { get; }
        public KwFacing Facing { get; }
        public KwNeed Needs { get; set; }
        public KwLike Likes { get; set; }

        /// <summary>Another scene it likes to be near (by id), placed before it.</summary>
        public string? Beside { get; set; }

        public List<KwSceneMember> Members { get; set; } = new List<KwSceneMember>();

        /// <summary>
        /// Its working spaces, each (min x, min z, max x, max z) in its frame: the space in front where people work, a
        /// kettle's end kept clear; walkable (or water, with <see cref="ApronWet"/>) and free of other scenes.
        /// </summary>
        public List<(float MinX, float MinZ, float MaxX, float MaxZ)> Aprons { get; set; } = new List<(float, float, float, float)>();

        /// <summary>Its aprons may be water as well as walkable (it faces the water).</summary>
        public bool ApronWet { get; set; }

        /// <summary>Metres kept clear round it from other scenes.</summary>
        public float Margin { get; set; } = 1.2f;

        public int Copies { get; set; } = 1;
        public int Later { get; set; } = 3;
        public float Apart { get; set; } = 18f;
    }
}
