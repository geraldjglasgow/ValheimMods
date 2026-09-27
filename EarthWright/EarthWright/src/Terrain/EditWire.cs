using System;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// The network form of a <see cref="TerrainEdit"/>: a version byte, the header, then the stroke or the vertex set.
    /// A package of an unknown version is refused rather than misread.
    /// </summary>
    public static class EditWire
    {
        public const byte Version = 1;

        public static ZPackage Write(TerrainEdit edit)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(Version);
            pkg.Write((byte)edit.Kind);
            pkg.Write((int)edit.Flags);
            pkg.Write(edit.SenderPlayer);
            pkg.Write(edit.SenderPeer);
            pkg.Write(edit.Source ?? "");
            if (edit.Kind == EditKind.Stroke)
                StrokeWire.Write(pkg, edit.Stroke);
            else
                VertexWire.Write(pkg, edit.Vertices);
            return pkg;
        }

        /// <summary>Reads an edit, or returns null (and logs) when the package is not one this version understands.</summary>
        public static TerrainEdit Read(ZPackage pkg)
        {
            try
            {
                byte version = pkg.ReadByte();
                if (version != Version)
                {
                    Plugin.Log.LogWarning($"Terrain edit of wire version {version} ignored (this is {Version}); is every player on the same EarthWright?");
                    return null;
                }
                return ReadBody(pkg);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Unreadable terrain edit ignored: " + e.Message);
                return null;
            }
        }

        private static TerrainEdit ReadBody(ZPackage pkg)
        {
            TerrainEdit edit = new TerrainEdit
            {
                Kind = (EditKind)pkg.ReadByte(),
                Flags = (EditFlags)pkg.ReadInt(),
                SenderPlayer = pkg.ReadLong(),
                SenderPeer = pkg.ReadLong(),
                Source = pkg.ReadString(),
            };
            if (edit.Kind == EditKind.Stroke)
                edit.Stroke = StrokeWire.Read(pkg);
            else
                edit.Vertices = VertexWire.Read(pkg);
            return edit;
        }

        /// <summary>A copy of the edit (through the wire form), for sending a trimmed variant without touching the original.</summary>
        public static TerrainEdit Clone(TerrainEdit edit)
        {
            ZPackage pkg = Write(edit);
            pkg.SetPos(0);
            return Read(new ZPackage(pkg.GetArray()));
        }
    }

    /// <summary>Wire form of a <see cref="BrushStroke"/>.</summary>
    public static class StrokeWire
    {
        public static void Write(ZPackage pkg, BrushStroke s)
        {
            pkg.Write(s.Center);
            pkg.Write((byte)s.Shape);
            pkg.Write(s.Radius);
            pkg.Write(s.Radius2);
            pkg.Write(s.Rotation);
            pkg.Write(s.Hardness);
            pkg.Write((byte)s.Height);
            pkg.Write((byte)s.Style);
            pkg.Write(s.Target);
            pkg.Write(s.Amount);
            pkg.Write(s.MaxStep);
            pkg.Write(s.Strength);
            WritePaint(pkg, s);
        }

        private static void WritePaint(ZPackage pkg, BrushStroke s)
        {
            pkg.Write((byte)s.Paint);
            pkg.Write(s.PaintRadius);
            pkg.Write(s.PaintStrength);
            pkg.Write(s.Density);
            pkg.Write(s.BandMin);
            pkg.Write(s.BandMax);
            pkg.Write(s.RandomShare);
            pkg.Write(s.Seed);
        }

        public static BrushStroke Read(ZPackage pkg)
        {
            BrushStroke s = new BrushStroke
            {
                Center = pkg.ReadVector3(),
                Shape = (BrushShape)pkg.ReadByte(),
                Radius = pkg.ReadSingle(),
                Radius2 = pkg.ReadSingle(),
                Rotation = pkg.ReadSingle(),
                Hardness = pkg.ReadSingle(),
                Height = (HeightOp)pkg.ReadByte(),
                Style = (LevelStyle)pkg.ReadByte(),
                Target = pkg.ReadSingle(),
                Amount = pkg.ReadSingle(),
                MaxStep = pkg.ReadSingle(),
                Strength = pkg.ReadSingle(),
            };
            ReadPaint(pkg, s);
            return s;
        }

        private static void ReadPaint(ZPackage pkg, BrushStroke s)
        {
            s.Paint = (PaintOp)pkg.ReadByte();
            s.PaintRadius = pkg.ReadSingle();
            s.PaintStrength = pkg.ReadSingle();
            s.Density = pkg.ReadSingle();
            s.BandMin = pkg.ReadSingle();
            s.BandMax = pkg.ReadSingle();
            s.RandomShare = pkg.ReadSingle();
            s.Seed = pkg.ReadInt();
        }
    }
}
