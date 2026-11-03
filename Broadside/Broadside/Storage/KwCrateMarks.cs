using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A slatted crate's albedo from its seed, the workshop's marks contract (ValheimAssets
    /// <c>Assets/Ships/Keelwright/SlatCrates/HANDOFF.md</c>, its <c>seed.py</c>), in plain integers so it matches
    /// <c>seed.py</c> byte for byte: the clean atlas multiplied by the crate's window of one seamless 1024 px pattern of
    /// knots, spots and stains (200 leaves the wood as it is, the multiplier value / 200), as far as the atlas's marks
    /// mask lets it, and by a tone of 60 to 67 / 64. Every row counts from the bottom (Unity's <c>GetPixels32</c> order).
    /// The seed: bits 0-9 the window's x, 10-19 its y, 20-21 the flips (u, v; never a turn, so knots stay along the
    /// grain, which runs along u on every board), 22-24 the tone.
    /// </summary>
    public static class KwCrateMarks
    {
        public const int Tile = 1024;
        /// <summary>The seeds a crate rolls: 25 bits, never 0 (a crate whose seed is 0 has none yet).</summary>
        public const int SeedMask = (1 << 25) - 1;
        private const int Unchanged = 200, Full = Unchanged * 255, Scale = Full * 64, Half = Scale / 2;

        /// <summary>The composite: <paramref name="clean"/> and <paramref name="mask"/> atlas sized, <paramref name="pattern"/> <see cref="Tile"/> square.</summary>
        public static Color32[] Albedo(Color32[] clean, byte[] mask, byte[] pattern, int width, int seed)
        {
            Color32[] made = new Color32[clean.Length];
            int ox = seed & 1023, oy = (seed >> 10) & 1023, flips = (seed >> 20) & 3, tone = 60 + ((seed >> 22) & 7);
            for (int y = 0, height = clean.Length / width; y < height; y++)
            {
                int row = Wrap(((flips & 2) != 0 ? -y : y) + oy) * Tile;
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x, p = pattern[row + Wrap(((flips & 1) != 0 ? -x : x) + ox)];
                    int factor = (Full + (p - Unchanged) * mask[i]) * tone;
                    Color32 c = clean[i];
                    made[i] = new Color32(Channel(c.r, factor), Channel(c.g, factor), Channel(c.b, factor), c.a);
                }
            }
            return made;
        }

        private static int Wrap(int value) => ((value % Tile) + Tile) % Tile;

        // At most 255 * 65025 * 67 before the division: within an int.
        private static byte Channel(byte value, int factor) => (byte)Mathf.Min(255, (value * factor + Half) / Scale);
    }
}
