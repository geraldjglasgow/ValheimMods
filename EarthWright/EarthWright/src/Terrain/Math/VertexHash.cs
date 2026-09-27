namespace EarthWright.Terrain
{
    /// <summary>
    /// A deterministic random number per world vertex and seed. The random-share filter uses it instead of a random
    /// generator so that both compilers of a shared edge, on different machines, pick exactly the same vertices.
    /// </summary>
    public static class VertexHash
    {
        /// <summary>A number in [0, 1) that depends only on the whole-metre world coordinates and the seed.</summary>
        public static float Unit(int x, int z, int seed)
        {
            unchecked
            {
                uint h = (uint)x * 0x8DA6B343u ^ (uint)z * 0xD8163841u ^ (uint)seed * 0xCB1AB31Fu;
                h ^= h >> 13;
                h *= 0x5BD1E995u;
                h ^= h >> 15;
                h *= 0x27D4EB2Du;
                h ^= h >> 16;
                return (h & 0xFFFFFFu) / 16777216f;
            }
        }
    }
}
