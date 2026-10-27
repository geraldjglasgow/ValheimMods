using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where the planking's faces sit on its biome tier's planking texture (<see cref="KwBiomeLooks"/>: 2048 by 512 px, 64 m
    /// of board long, repeating along u): the outside of a hull on the upper half, sixteen rows of planks 16 px tall, the
    /// inside on the lower half, sixteen rows the same, every row painted on its own along its whole length (its own board
    /// ends, knots, nails and wear), so no stretch of wood comes round again on a ship. The texture goes on at its own
    /// density, 32 px a metre (the game's building pieces'): a row is a strake, <see cref="StrakeWidth"/> (0.5 m) of the
    /// hull's measure, its height on the sides and its girth on the bottom (<see cref="KwGirth"/>: strake k level at k widths
    /// over the keel line on the sides), on row k % 16 with its lower edge at the row's foot, so the painted planks follow
    /// the real strakes; the sheer cuts the top strake's row where it falls, the keel or a stem the lowest. A side leaning
    /// from upright widens its strakes a little, and the row with them (a quarter at most, where the bottom turns into the
    /// side). A strake's lip takes its row's lower edge and the gunwale's cap the plank just under its head. Along the hull
    /// u is each strake's own length from the part's start; <see cref="KwPlankShift"/> then lays the part's planks where the
    /// part stands along the ship, so neighbouring parts carry the same boards on, and starts each strake at its own place
    /// in the texture (<see cref="RowStart"/>). A flat stern's transom board is planked across in level rows at the side
    /// strakes' heights. Decks keep their own 128 px texture, repeating every 4 m both ways, the boards running along the
    /// ship (<see cref="Deck"/>: a deck's edges, and the whole of a deck kept whole for its collider); a deck's top and
    /// underside are laid as boards, each moved along the texture by a shift of its own (<see cref="Board"/>,
    /// <see cref="KwDeckBoards"/>).
    /// </summary>
    public static class KwUv
    {
        /// <summary>Texels a metre, the deck texture's size, and how far along a deck it repeats (4 m).</summary>
        public const float Density = 32f, Size = 128f, Tile = Size / Density;

        /// <summary>The planking texture's length along its boards (2048 px: 64 m) and its height (512 px), in texels.</summary>
        public const float PlankLength = 2048f, PlankHeight = 512f;

        /// <summary>How far along a strake the planking texture comes round again, in metres (64).</summary>
        public const float PlankRepeat = PlankLength / Density;

        // The rows a face of the planking texture holds, each a strake: sixteen outside on the upper half, sixteen inside.
        private const int Rows = 16;
        private const float RowPx = 16f, Band = Rows * RowPx;

        /// <summary>
        /// The steps a strake's start into the planking texture comes in, in metres: 4, the round of the straps and bands
        /// some tiers paint up the hull, so they stay in one line from strake to strake whatever each starts at.
        /// </summary>
        public const float RowStep = 4f;

        /// <summary>How wide a strake is in the hull's measure, in metres (0.5): one row of the texture at its density.</summary>
        public const float StrakeWidth = RowPx / Density;

        /// <summary>u for a distance along a deck or across it (the deck texture, 4 m a round).</summary>
        public static float Along(float metres) => metres / Tile;

        /// <summary>u for a distance along a strake (the planking texture, 64 m a round).</summary>
        public static float Plank(float metres) => metres / PlankRepeat;

        /// <summary>A point of strake k (<see cref="KwGirth"/>) at metres along it and up metres over its lower edge (0 to <see cref="StrakeWidth"/>).</summary>
        public static Vector2 Strake(bool outside, float along, int k, float up) =>
            new Vector2(Plank(along), V(Top(outside, k) + RowPx - 0.5f - Mathf.Clamp01(up / StrakeWidth) * (RowPx - 1f)));

        /// <summary>A strake's lower lip (the face under its proud edge): its row's last two texel rows, outside at the foot.</summary>
        public static Vector2 Lip(float along, int i, bool outer) => new Vector2(Plank(along), V(Top(true, i) + (outer ? RowPx - 0.5f : RowPx - 2f)));

        /// <summary>The gunwale's cap on the top strake: two texel rows of plank just under its row's head (whose seam is paint).</summary>
        public static Vector2 Cap(float along, int i, bool outer) => new Vector2(Plank(along), V(Top(true, i) + (outer ? 2.5f : 4.5f)));

        /// <summary>A transom board point: across it, and up metres over the foot of its row r, the rows the hull's strakes.</summary>
        public static Vector2 Transom(float across, int row, float up) => Strake(true, across + Tile / 2f, row, up);

        /// <summary>
        /// Where strake k starts into the planking texture (as u), on one side of the ship (1 its port, -1 its starboard, 0 a
        /// row across it, the transom board's): a whole number of <see cref="RowStep"/>s picked from the strake's number
        /// and side alone, so the same strake starts at the same place on every part and every peer, and no two strakes
        /// over each other run the same stretch of their rows.
        /// </summary>
        public static float RowStart(int k, int side)
        {
            const int starts = (int)(PlankRepeat / RowStep);
            return (int)(Mix(k, side) % starts) * RowStep / PlankRepeat;
        }

        /// <summary>A deck point (kit x along, y across, the deck's start at x0): u across, v along, 4 m a texture.</summary>
        public static Vector2 Deck(Vector3 p, float x0) => new Vector2(Along(p.y), Along(p.x - x0));

        /// <summary>
        /// A deck board's point (kit x along, y across): u as on <see cref="Deck"/>, v along from the deck's start moved by
        /// the board's own shift (texels), or run backwards from it on a flipped board, as <see cref="KwDeckLayout.Place"/> counts.
        /// </summary>
        public static Vector2 Board(Vector2 p, float x0, int shift, bool flip) =>
            new Vector2(Along(p.y), shift / Size + (flip ? -1f : 1f) * Along(p.x - x0));

        // The texture row (from the top) a strake's row starts at; the bottom's strakes under the keel line (below 0) too.
        private static float Top(bool outside, int i) => (outside ? 0f : Band) + (((i % Rows) + Rows) % Rows) * RowPx;

        private static float V(float row) => 1f - row / PlankHeight;

        // A strake's number and side stirred into a well spread whole number, the same on every machine.
        private static uint Mix(int k, int side)
        {
            unchecked
            {
                uint h = (uint)k * 0x9E3779B1u ^ (uint)(side + 2) * 0x85EBCA77u;
                h ^= h >> 15;
                h *= 0x2C1B3C6Du;
                h ^= h >> 12;
                h *= 0x297A2D39u;
                return h ^ (h >> 15);
            }
        }
    }
}
