using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The layout of a dock material's texture atlas, <c>bs_pier_&lt;material&gt;.png</c> (1024 px square, painted in the asset
    /// workshop: <c>ValheimAssets/Assets/Ships/Keelwright/PierPlanks</c>, whose <c>atlas.py</c> this mirrors; change both
    /// together). Pixel rows count from the image's top. The top 16 px are a row of 64 end-grain tiles, 16 px square; under
    /// them the square timbers (8 px across, 0.2 m) or, for core wood, the bark round a log (32 px, periodic across); for
    /// wood (the rough tier) then round poles with bark patches (24 px round, periodic), twisted rope (8 px round) and
    /// narrow boards (8 px, 0.186 m); then the deck boards, a board's width across. Every strip is 512 px long with the
    /// grain along u, at <see cref="Density"/> px a metre, two side by side in a row; every strip's top and height a
    /// multiple of four texels, so no compressed block or near mip level mixes two of them.
    /// </summary>
    public sealed class KwPierAtlas
    {
        public const int Size = 1024, Length = 512, Tile = 16, Ends = 64;

        /// <summary>Texels a metre along the grain (Broadside's pieces).</summary>
        public const float Density = 43f;

        // wood: 40 timbers, 16 poles, 4 ropes, 64 narrow and 64 wide boards; core wood: 24 barks, 104 boards; fine wood:
        // 64 timbers, 188 boards.
        private static readonly KwPierAtlas Wood = new KwPierAtlas(new KwPierStrips(16, 8, 20, 0.2f), new KwPierStrips(640, 12, 32, 0.28f))
        {
            Poles = new KwPierStrips(176, 24, 8, 1f), Ropes = new KwPierStrips(368, 8, 2, 1f), Narrow = new KwPierStrips(384, 8, 32, 0.186f),
        };

        private static readonly KwPierAtlas CoreWood = new KwPierAtlas(new KwPierStrips(16, 32, 12, 0.75f), new KwPierStrips(400, 12, 52, 0.3f));
        private static readonly KwPierAtlas FineWood = new KwPierAtlas(new KwPierStrips(16, 8, 32, 0.2f), new KwPierStrips(272, 8, 94, 0.18f));

        private KwPierAtlas(KwPierStrips timbers, KwPierStrips boards)
        {
            (Timbers, Boards) = (timbers, boards);
        }

        /// <summary>The square timbers' strips, or core wood's bark.</summary>
        public KwPierStrips Timbers { get; }

        /// <summary>The deck boards' strips (wood's wider boards).</summary>
        public KwPierStrips Boards { get; }

        /// <summary>Wood's narrow boards, round poles and rope; null for the other materials.</summary>
        public KwPierStrips? Narrow { get; private set; }
        public KwPierStrips? Poles { get; private set; }
        public KwPierStrips? Ropes { get; private set; }

        public static KwPierAtlas Of(KwPierKind kind) => kind == KwPierKinds.CoreWood ? CoreWood : kind == KwPierKinds.FineWood ? FineWood : Wood;

        /// <summary>A family's strips (<see cref="KwPierUv.Boards"/> ...): one the atlas lacks falls back to the boards or the timbers.</summary>
        public KwPierStrips Family(int family) => family switch
        {
            KwPierUv.Narrow => Narrow ?? Boards,
            KwPierUv.Poles => Poles ?? Timbers,
            KwPierUv.Ropes => Ropes ?? Timbers,
            KwPierUv.Timbers => Timbers,
            _ => Boards,
        };

        /// <summary>Whether the atlas has strips of its own for that family (wood's narrow boards, poles and rope).</summary>
        public bool Has(int family) => family switch
        {
            KwPierUv.Narrow => Narrow != null,
            KwPierUv.Poles => Poles != null,
            KwPierUv.Ropes => Ropes != null,
            _ => true,
        };

        /// <summary>The texture coordinate of a point in the image, in pixels right and down from its top left corner.</summary>
        public static Vector2 At(float x, float y) => new Vector2(x / Size, 1f - y / Size);

        /// <summary>The middle of an end-grain tile, in pixels.</summary>
        public static Vector2 TileMiddle(int tile) => new Vector2(tile * Tile + Tile * 0.5f, Tile * 0.5f);
    }

    /// <summary>One kind of strip in an atlas: rows of two strips from pixel row <see cref="Top"/>, each <see cref="Height"/> px, standing for <see cref="Across"/> metres across the grain.</summary>
    public sealed class KwPierStrips
    {
        public KwPierStrips(int top, int height, int rows, float across)
        {
            (Top, Height, Rows, Across) = (top, height, rows, across);
        }

        public int Top { get; }
        public int Height { get; }
        public int Rows { get; }
        public float Across { get; }

        public int Count => Rows * 2;

        /// <summary>A strip's top left corner in pixels (the first two side by side in the first row, and so on down).</summary>
        public Vector2 Corner(int i) => new Vector2(i % 2 * KwPierAtlas.Length, Top + i / 2 * Height);
    }
}
