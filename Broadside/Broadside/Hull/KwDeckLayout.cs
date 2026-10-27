using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// How a biome tier's deck texture is cut into boards (<see cref="KwDeckBoards"/>), fitted to how it is painted
    /// (<c>ValheimAssets/Assets/Ships/Keelwright/tier_&lt;biome&gt;</c>: 128 texels square, 32 a metre, boards 8 texels wide
    /// running down the image, each with its dark gap on its left). Across the deck every column of the texture stays where
    /// <see cref="KwUv.Deck"/> puts it, so the deck's cuts fall on the texture's own gaps; the columns are split into bands,
    /// a board each, except that the boards something is painted across (a leather scrap, a wolf pelt) go as one, and the
    /// plains' woven mats are a metre square. Along, each band is cut into lengths that each take a place of their own in
    /// the texture: moved by whole steps (16 texels, so the ashlands' bone runner down the centre line still meets itself),
    /// some run backwards (one texel short of a step, for the same reason), and none cuts through what is painted in its band.
    /// The painted boxes were measured from the tiers' paintings (replayed, their shadows included); a repainted deck
    /// whose scraps or pelts move needs them measured again.
    /// </summary>
    public sealed class KwDeckLayout
    {
        private const int Size = 128;

        // Plain boards, a band each: Black Forest, Swamp, Mistlands, Ashlands. Lengths of two to four metres.
        private static readonly KwDeckLayout Boards = new KwDeckLayout(8, 16, 64, 128);

        // Leather scraps nailed across three boards each: their columns left to right, rows down from the image's top.
        private static readonly KwDeckLayout Meadows = new KwDeckLayout(8, 16, 64, 128, (6, 23, 91, 111), (38, 52, 9, 28), (59, 77, 46, 64), (90, 105, 61, 78));

        // Two wolf pelts thrown across six boards each.
        private static readonly KwDeckLayout Mountains = new KwDeckLayout(8, 16, 64, 128, (12, 55, 1, 59), (78, 118, 68, 122));

        // Woven mats a metre square, each whole, a mat a length.
        private static readonly KwDeckLayout Plains = new KwDeckLayout(32, 32, 32, 32);

        private readonly List<int> edges = new List<int>();
        private readonly List<List<(int From, int To)>> kept = new List<List<(int From, int To)>>();

        private KwDeckLayout(int board, int step, int shortest, int longest, params (int Left, int Right, int Top, int Bottom)[] painted)
        {
            (Step, Shortest, Longest) = (step, shortest, longest);
            for (int e = 0; e <= Size; e += board)
            {
                if (!System.Array.Exists(painted, p => p.Left < e && e <= p.Right))
                    edges.Add(e);
            }
            for (int i = 0; i + 1 < edges.Count; i++)
                kept.Add(Kept(painted, edges[i], edges[i + 1]));
        }

        /// <summary>Texels: how far a length's place in the texture moves at a time, and where its cuts fall.</summary>
        public int Step { get; }

        /// <summary>Texels: the shortest and longest lengths a band is cut into.</summary>
        public int Shortest { get; }
        public int Longest { get; }

        /// <summary>How many bands the texture's width is split into.</summary>
        public int Bands => edges.Count - 1;

        /// <summary>A band's first column and the column after its last.</summary>
        public (int From, int To) Band(int i) => (edges[i], edges[i + 1]);

        /// <summary>The deck boards of a tier's texture.</summary>
        public static KwDeckLayout Of(KwBiomeTier tier) => tier.Id switch
        {
            "meadows" => Meadows,
            "mountains" => Mountains,
            "plains" => Plains,
            _ => Boards,
        };

        /// <summary>
        /// A length of band i from at (texels along the deck from its start), from a hash: how long it is, its shift in the
        /// texture (texels) and whether it runs backwards; the first that cuts through nothing painted in the band. A band
        /// with something painted in it is cut a whole texture long, so it shows each thing once a length, as often as painted.
        /// </summary>
        public (int Length, int Shift, bool Flip) Pick(uint hash, int band, int at)
        {
            int shortest = kept[band].Count > 0 ? Longest : Shortest, lengths = (Longest - shortest) / Step + 1, shifts = Size / Step;
            bool flip = (hash & 1u) == 1u;
            int firstLength = (int)((hash >> 1) % (uint)lengths), firstShift = (int)((hash >> 9) % (uint)shifts);
            for (int i = 0; i < lengths * shifts; i++)
            {
                int length = shortest + (firstLength + i / shifts) % lengths * Step;
                int shift = (firstShift + i) % shifts * Step - (flip ? 1 : 0);
                if (Whole(band, Place(at, shift, flip)) && Whole(band, Place(at + length, shift, flip)))
                    return (length, shift, flip);
            }
            return (Longest, 0, false);
        }

        /// <summary>Where a point texels along the deck falls in the texture (texels up from its foot), on a length with this shift.</summary>
        public static int Place(int along, int shift, bool flip) => flip ? shift - along : shift + along;

        // Whether a length may end at this place in the texture: outside everything painted in the band.
        private bool Whole(int band, int place)
        {
            int at = (place % Size + Size) % Size;
            return kept[band].TrueForAll(k => at <= k.From || at >= k.To);
        }

        // What is painted inside a band, as places along the texture (rows run down from its top, places up from its foot).
        private static List<(int From, int To)> Kept((int Left, int Right, int Top, int Bottom)[] painted, int from, int to)
        {
            List<(int From, int To)> rows = new List<(int From, int To)>();
            foreach ((int left, int right, int top, int bottom) in painted)
            {
                if (left >= from && right < to)
                    rows.Add((Size - 1 - bottom, Size - top));
            }
            return rows;
        }
    }
}
