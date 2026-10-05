using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// A blueprint as bytes for a construction site's ZDO, so every client draws and builds it without the file: the
    /// prefab names once, the pieces as rows of name index and four floats, the levelled and painted squares, the
    /// water rule and the saved relief, compressed with the game's own <c>Utils.Compress</c>.
    /// </summary>
    public static class SiteCodec
    {
        private const int Version = 1;

        public static byte[] Encode(Blueprint bp)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(Version);
            pkg.Write(bp.Name ?? "");
            pkg.Write(bp.Description ?? "");
            WritePieces(pkg, bp.Pieces);
            WriteSite(pkg, bp);
            return Utils.Compress(pkg.GetArray());
        }

        /// <summary>The blueprint, or null when the bytes are not a site blueprint of this version.</summary>
        public static Blueprint Decode(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;
            ZPackage pkg = new ZPackage(Utils.Decompress(data));
            if (pkg.ReadInt() != Version)
                return null;
            Blueprint bp = new Blueprint { Name = pkg.ReadString(), Description = pkg.ReadString() };
            ReadPieces(pkg, bp.Pieces);
            ReadSite(pkg, bp);
            bp.Finish();
            return bp;
        }

        private static void WritePieces(ZPackage pkg, List<BlueprintPiece> pieces)
        {
            List<string> names = new List<string>();
            Dictionary<string, int> index = new Dictionary<string, int>();
            foreach (BlueprintPiece p in pieces)
            {
                if (!index.ContainsKey(p.Prefab))
                {
                    index[p.Prefab] = names.Count;
                    names.Add(p.Prefab);
                }
            }
            pkg.Write(names.Count);
            foreach (string name in names)
                pkg.Write(name);
            pkg.Write(pieces.Count);
            foreach (BlueprintPiece p in pieces)
                WriteRow(pkg, index[p.Prefab], p);
        }

        private static void WriteRow(ZPackage pkg, int name, BlueprintPiece p)
        {
            pkg.Write(name);
            pkg.Write(p.X);
            pkg.Write(p.Y);
            pkg.Write(p.Z);
            pkg.Write(p.Yaw);
        }

        private static void ReadPieces(ZPackage pkg, List<BlueprintPiece> pieces)
        {
            string[] names = new string[pkg.ReadInt()];
            for (int i = 0; i < names.Length; i++)
                names[i] = pkg.ReadString();
            int count = pkg.ReadInt();
            for (int i = 0; i < count; i++)
            {
                pieces.Add(new BlueprintPiece
                {
                    Prefab = names[pkg.ReadInt()], X = pkg.ReadSingle(), Y = pkg.ReadSingle(), Z = pkg.ReadSingle(), Yaw = pkg.ReadSingle(),
                });
            }
        }

        private static void WriteSite(ZPackage pkg, Blueprint bp)
        {
            pkg.Write(bp.Levels.Count);
            foreach (LevelStep s in bp.Levels)
                WriteFloats(pkg, s.X, s.Z, s.Half, s.Y);
            pkg.Write(bp.Paints.Count);
            foreach (PaintStep s in bp.Paints)
            {
                WriteFloats(pkg, s.X, s.Z, s.Half);
                pkg.Write((byte)s.Paint);
            }
            pkg.Write(bp.HasWater);
            WriteFloats(pkg, bp.WaterFloor, bp.WaterDepth);
            WriteRelief(pkg, bp.Relief);
        }

        private static void ReadSite(ZPackage pkg, Blueprint bp)
        {
            int levels = pkg.ReadInt();
            for (int i = 0; i < levels; i++)
                bp.Levels.Add(new LevelStep { X = pkg.ReadSingle(), Z = pkg.ReadSingle(), Half = pkg.ReadSingle(), Y = pkg.ReadSingle() });
            int paints = pkg.ReadInt();
            for (int i = 0; i < paints; i++)
                bp.Paints.Add(new PaintStep { X = pkg.ReadSingle(), Z = pkg.ReadSingle(), Half = pkg.ReadSingle(), Paint = (GroundPaint)pkg.ReadByte() });
            bp.HasWater = pkg.ReadBool();
            bp.WaterFloor = pkg.ReadSingle();
            bp.WaterDepth = pkg.ReadSingle();
            bp.Relief = ReadRelief(pkg);
        }

        private static void WriteRelief(ZPackage pkg, Relief r)
        {
            pkg.Write(r != null);
            if (r == null)
                return;
            WriteFloats(pkg, r.X0, r.Z0);
            pkg.Write(r.Width);
            pkg.Write(r.Depth);
            foreach (float offset in r.Offsets)
                pkg.Write(offset);
        }

        private static Relief ReadRelief(ZPackage pkg)
        {
            if (!pkg.ReadBool())
                return null;
            Relief r = new Relief { X0 = pkg.ReadSingle(), Z0 = pkg.ReadSingle(), Width = pkg.ReadInt(), Depth = pkg.ReadInt() };
            r.Offsets = new float[Mathf.Max(0, r.Width * r.Depth)];
            for (int i = 0; i < r.Offsets.Length; i++)
                r.Offsets[i] = pkg.ReadSingle();
            return r;
        }

        private static void WriteFloats(ZPackage pkg, params float[] values)
        {
            foreach (float v in values)
                pkg.Write(v);
        }
    }
}
