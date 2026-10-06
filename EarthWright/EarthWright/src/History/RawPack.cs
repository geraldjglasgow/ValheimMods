using System.Collections.Generic;
using EarthWright.Terrain;

namespace EarthWright.History
{
    /// <summary>
    /// One heightmap's raw compiler values packed into compressed bytes (the restore wire layout, <see cref="VertexWire"/>)
    /// and back, so a snapshot of a large area keeps a fraction of the memory a dictionary of values would take.
    /// </summary>
    internal static class RawPack
    {
        /// <summary>The values of these indices as this machine holds them now, packed.</summary>
        public static byte[] Pack(Heightmap map, TerrainComp comp, List<int> indices)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(indices.Count);
            foreach (int index in indices)
                VertexWire.WriteRaw(pkg, RawAccess.Read(map, comp, index));
            return pkg.GetCompressed();
        }

        /// <summary>Adds the packed values to <paramref name="into"/>, by index.</summary>
        public static void Unpack(byte[] data, Dictionary<int, RawVertex> into)
        {
            ZPackage pkg = new ZPackage(Utils.Decompress(data));
            int count = pkg.ReadInt();
            for (int i = 0; i < count; i++)
            {
                RawVertex value = VertexWire.ReadRaw(pkg);
                into[value.Index] = value;
            }
        }
    }
}
