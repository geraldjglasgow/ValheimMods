using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>Wire form of a <see cref="VertexSet"/>.</summary>
    public static class VertexWire
    {
        public static void Write(ZPackage pkg, VertexSet set)
        {
            pkg.Write((byte)set.Mode);
            pkg.Write(set.CompPosition);
            if (set.Mode == VertexMode.Restore)
                WriteRaw(pkg, set);
            else
                WriteTargets(pkg, set);
        }

        public static VertexSet Read(ZPackage pkg)
        {
            VertexSet set = new VertexSet { Mode = (VertexMode)pkg.ReadByte(), CompPosition = pkg.ReadVector3() };
            if (set.Mode == VertexMode.Restore)
                ReadRaw(pkg, set);
            else
                ReadTargets(pkg, set);
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
        }

        private static void ReadRaw(ZPackage pkg, VertexSet set)
        {
            int count = pkg.ReadInt();
            for (int i = 0; i < count; i++)
            {
                set.Raw.Add(new RawVertex
                {
                    Index = pkg.ReadInt(),
                    HeightModified = pkg.ReadBool(),
                    LevelDelta = pkg.ReadSingle(),
                    SmoothDelta = pkg.ReadSingle(),
                    PaintModified = pkg.ReadBool(),
                    Paint = new Color(pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle()),
                });
            }
        }
    }
}
