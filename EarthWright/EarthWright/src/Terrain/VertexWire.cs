using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Wire form of a <see cref="VertexSet"/>: the mode and the compiler position, then the entries as one compressed
    /// package (the game's own compression). An undo restore carries raw values for up to a whole heightmap, about 30
    /// bytes a vertex before compression, and a ramp or road thousands of targets.
    /// </summary>
    public static class VertexWire
    {
        public static void Write(ZPackage pkg, VertexSet set)
        {
            pkg.Write((byte)set.Mode);
            pkg.Write(set.CompPosition);
            ZPackage body = new ZPackage();
            if (set.Mode == VertexMode.Restore)
                WriteRaw(body, set);
            else
                WriteTargets(body, set);
            pkg.WriteCompressed(body);
        }

        public static VertexSet Read(ZPackage pkg)
        {
            VertexSet set = new VertexSet { Mode = (VertexMode)pkg.ReadByte(), CompPosition = pkg.ReadVector3() };
            ZPackage body = pkg.ReadCompressedPackage();
            if (set.Mode == VertexMode.Restore)
                ReadRaw(body, set);
            else
                ReadTargets(body, set);
            return set;
        }

        private static void WriteTargets(ZPackage pkg, VertexSet set)
        {
            pkg.Write(set.Targets.Count);
            foreach (TargetVertex v in set.Targets)
            {
                pkg.Write(v.X);
                pkg.Write(v.Z);
                pkg.Write(v.Height);
                pkg.Write(v.Weight);
                pkg.Write((byte)v.Paint);
                pkg.Write(v.PaintStrength);
            }
        }

        private static void ReadTargets(ZPackage pkg, VertexSet set)
        {
            int count = pkg.ReadInt();
            for (int i = 0; i < count; i++)
            {
                set.Targets.Add(new TargetVertex
                {
                    X = pkg.ReadInt(),
                    Z = pkg.ReadInt(),
                    Height = pkg.ReadSingle(),
                    Weight = pkg.ReadSingle(),
                    Paint = (PaintOp)pkg.ReadByte(),
                    PaintStrength = pkg.ReadSingle(),
                });
            }
        }

        private static void WriteRaw(ZPackage pkg, VertexSet set)
        {
            pkg.Write(set.Raw.Count);
            foreach (RawVertex v in set.Raw)
                WriteRaw(pkg, v);
        }

        private static void ReadRaw(ZPackage pkg, VertexSet set)
        {
            int count = pkg.ReadInt();
            for (int i = 0; i < count; i++)
                set.Raw.Add(ReadRaw(pkg));
        }

        /// <summary>One raw value as it travels (and as snapshots keep it).</summary>
        public static void WriteRaw(ZPackage pkg, RawVertex v)
        {
            pkg.Write(v.Index);
            pkg.Write(v.HeightModified);
            pkg.Write(v.LevelDelta);
            pkg.Write(v.SmoothDelta);
            pkg.Write(v.PaintModified);
            pkg.Write(v.Paint.r);
            pkg.Write(v.Paint.g);
            pkg.Write(v.Paint.b);
            pkg.Write(v.Paint.a);
        }

        public static RawVertex ReadRaw(ZPackage pkg)
        {
            return new RawVertex
            {
                Index = pkg.ReadInt(),
                HeightModified = pkg.ReadBool(),
                LevelDelta = pkg.ReadSingle(),
                SmoothDelta = pkg.ReadSingle(),
                PaintModified = pkg.ReadBool(),
                Paint = new Color(pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle()),
            };
        }
    }
}
