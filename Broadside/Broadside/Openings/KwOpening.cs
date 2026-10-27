using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One opening cut into a part's wall (the user's request, 2026-10-25: doorways "in the material instead of just auto
    /// putting doorways in", and windows, gunports and portholes, "None of them should have any glass: just a framed piece
    /// that exposes a hole in the ship"): which wall of the part it is in (<see cref="Wall"/>), its shape, where its middle
    /// lies along the wall (<see cref="U"/>: a planking part's or a cabin's own x along its frame; a cabin wall's distance
    /// along its line), its sill's height over the keel line (<see cref="Sill"/>, the part's z), its width and height (a
    /// round port's diameter twice), its frame and its cover (a doorway's door is its cover). Saved in the part's text
    /// (<see cref="KwOpeningText"/>), so the part, a blueprint and a launched ship all build the same opening. Pure.
    /// </summary>
    public sealed class KwOpening
    {
        /// <summary>The shapes: a doorway (from the floor up), a round porthole, a square, a rectangle, an arched top.</summary>
        public const string Door = "door", Round = "round", Square = "square", Rect = "rect", Arch = "arch";

        /// <summary>The frames: plain timber, heavy timber, iron-banded.</summary>
        public const string Plain = "plain", Heavy = "heavy", Iron = "iron";

        /// <summary>
        /// The covers: none; a gunport lid hinged at its top (flapping up and out); a lid hinged at its bottom (flapping down);
        /// a pair of side shutters; a round port's hinged lid; a doorway's door; a doorway's double door (two leaves meeting
        /// in the middle, each hinged at its own jamb, one state).
        /// </summary>
        public const string None = "none", Up = "up", Down = "down", Shutters = "shutters", Lid = "lid", Leaf = "door", DoubleDoor = "door2";

        /// <summary>
        /// The walls: a planking part's port and starboard sides; a cabin's own walls by their names
        /// (<see cref="KwCabinWall.Name"/>); a cabin's raised hull sides, left and right.
        /// </summary>
        public const string Port = "p", Starboard = "s", RaisedLeft = "hl", RaisedRight = "hr";

        public KwOpening(int id, string wall, string shape, float u, float sill, float width, float height, string frame, string cover)
        {
            (Id, Wall, Shape, U, Sill) = (id, wall, shape, u, sill);
            (Width, Height, Frame, Cover) = (width, shape == Round ? width : height, frame, cover);
        }

        /// <summary>The opening's number on its part, kept while it is changed (its door's or cover's state goes by it).</summary>
        public int Id { get; }
        public string Wall { get; }
        public string Shape { get; }
        public float U { get; }
        public float Sill { get; }
        public float Width { get; }
        public float Height { get; }
        public string Frame { get; }
        public string Cover { get; }

        public float Top => Sill + Height;
        public float U0 => U - Width / 2f;
        public float U1 => U + Width / 2f;

        public bool IsDoorway => Shape == Door;

        /// <summary>Whether it has a leaf that opens and shuts (a door, a lid, shutters).</summary>
        public bool Covered => Cover != None;

        /// <summary>Whether its cover is a door, single or double.</summary>
        public bool HasDoor => Cover == Leaf || Cover == DoubleDoor;

        /// <summary>Whether a planking part's opening is on its port side (else starboard); a raised side's on the cabin's left.</summary>
        public bool OnPort => Wall == Port || Wall == RaisedLeft;

        /// <summary>The same opening moved, resized or given another frame or cover; its number and wall kept.</summary>
        public KwOpening With(string shape, float u, float sill, float width, float height, string frame, string cover) =>
            new KwOpening(Id, Wall, shape, u, sill, width, height, frame, cover);

        /// <summary>The same opening numbered again (a part's openings numbered from 1).</summary>
        public KwOpening Numbered(int id) => new KwOpening(id, Wall, Shape, U, Sill, Width, Height, Frame, Cover);

        /// <summary>The same opening on a part made again bigger or smaller: its place along by a, its heights by s, its sizes by s.</summary>
        public KwOpening Scaled(float a, float s) =>
            new KwOpening(Id, Wall, Shape, U * a, Sill * s, Width * s, Height * s, Frame, Cover);

        /// <summary>
        /// The wall it was fitted to (<see cref="KwOpeningSurfaces"/>, <see cref="On"/>), whose face its outline follows at
        /// its true size; none for an opening read from a text alone, whose outline is its own (true metres about its u and sill).
        /// </summary>
        public KwOpeningSurface? Surface { get; private set; }

        /// <summary>The same opening fitted to its wall, so its outline is in the wall's (u, z) at its true size on the face.</summary>
        public KwOpening On(KwOpeningSurface surface) => new KwOpening(Id, Wall, Shape, U, Sill, Width, Height, Frame, Cover) { Surface = surface };

        /// <summary>
        /// Its outline in (along, up), grown by a margin all round (0: the cut itself), anticlockwise: on its wall's face at
        /// its true size when fitted to one (<see cref="Surface"/>; worked out once a margin, a copy handed out), else its own.
        /// </summary>
        public List<Vector2> Outline(float grow = 0f)
        {
            if (Surface == null)
                return KwOpeningShape.Outline(this, grow);
            fitted ??= new Dictionary<float, List<Vector2>>();
            if (!fitted.TryGetValue(grow, out List<Vector2> ring))
                fitted[grow] = ring = KwOpeningShape.Outline(this, grow);
            return new List<Vector2>(ring);
        }

        private Dictionary<float, List<Vector2>>? fitted;

        /// <summary>The next free number among a part's openings.</summary>
        public static int Next(IEnumerable<KwOpening> openings)
        {
            int most = 0;
            foreach (KwOpening opening in openings)
                most = Mathf.Max(most, opening.Id);
            return most + 1;
        }
    }
}
