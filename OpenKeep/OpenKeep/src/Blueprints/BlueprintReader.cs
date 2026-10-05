using System;
using System.Globalization;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Reads a blueprint JSON file (JSON is YAML, so the merged YamlDotNet parses it): "name", "description",
    /// "pieces" as [prefab, x, y, z, yaw] rows, and "site" with "level_half" or "terrain" steps (level and paint
    /// squares), "water" {floor, depth} and OpenKeep's own "relief". Keys it does not know (DevBridge's "clear",
    /// "stand", "limits", "source") are ignored. Throws <see cref="FormatException"/> with the reason when it cannot read the file.
    /// </summary>
    public static class BlueprintReader
    {
        public static Blueprint Read(string path)
        {
            YamlStream stream = new YamlStream();
            using (StreamReader reader = new StreamReader(path))
                stream.Load(reader);
            if (stream.Documents.Count == 0 || !(stream.Documents[0].RootNode is YamlMappingNode root))
                throw new FormatException("the file is not a JSON object");
            Blueprint bp = new Blueprint { File = path, Name = Text(root, "name") ?? Path.GetFileNameWithoutExtension(path) };
            bp.Description = Text(root, "description") ?? "";
            ReadPieces(bp, Child(root, "pieces") as YamlSequenceNode);
            if (Child(root, "site") is YamlMappingNode site)
                ReadSite(bp, site);
            bp.Finish();
            return bp;
        }

        private static void ReadPieces(Blueprint bp, YamlSequenceNode rows)
        {
            if (rows == null)
                throw new FormatException("it has no \"pieces\" list");
            foreach (YamlNode node in rows.Children)
            {
                if (!(node is YamlSequenceNode row) || row.Children.Count < 5)
                    throw new FormatException("a piece is not a [prefab, x, y, z, yaw] row");
                bp.Pieces.Add(new BlueprintPiece
                {
                    Prefab = Scalar(row.Children[0]), X = Number(row.Children[1]), Y = Number(row.Children[2]),
                    Z = Number(row.Children[3]), Yaw = Number(row.Children[4]),
                });
            }
        }

        /// <summary>A saved relief replaces "level_half", which OpenKeep writes only for DevBridge's blueprint.py.</summary>
        private static void ReadSite(Blueprint bp, YamlMappingNode site)
        {
            if (Child(site, "relief") is YamlMappingNode relief)
                bp.Relief = ReadRelief(relief);
            if (Child(site, "terrain") is YamlSequenceNode steps)
            {
                foreach (YamlNode step in steps.Children)
                    ReadStep(bp, step as YamlMappingNode);
            }
            else if (Child(site, "level_half") is YamlScalarNode half && bp.Relief == null)
            {
                bp.Levels.Add(new LevelStep { Half = Number(half) });
            }
            if (Child(site, "water") is YamlMappingNode water)
                ReadWater(bp, water);
        }

        private static void ReadStep(Blueprint bp, YamlMappingNode step)
        {
            if (step == null || !(Child(step, "at") is YamlSequenceNode at) || at.Children.Count < 2)
                throw new FormatException("a terrain step has no \"at\": [x, z]");
            float x = Number(at.Children[0]), z = Number(at.Children[1]);
            float half = Number(Child(step, "half"));
            string op = Text(step, "op");
            if (op == "level")
                bp.Levels.Add(new LevelStep { X = x, Z = z, Half = half, Y = Child(step, "y") != null ? Number(Child(step, "y")) : 0f });
            else if (op == "paint")
                bp.Paints.Add(new PaintStep { X = x, Z = z, Half = half, Paint = Paint(Text(step, "paint")) });
            else
                throw new FormatException($"unknown terrain step \"{op}\" (level or paint)");
        }

        private static void ReadWater(Blueprint bp, YamlMappingNode water)
        {
            bp.HasWater = true;
            bp.WaterFloor = Number(Child(water, "floor"));
            bp.WaterDepth = Number(Child(water, "depth"));
        }

        private static Relief ReadRelief(YamlMappingNode node)
        {
            Relief relief = new Relief
            {
                X0 = Number(Child(node, "x")), Z0 = Number(Child(node, "z")),
                Width = (int)Number(Child(node, "width")), Depth = (int)Number(Child(node, "depth")),
            };
            if (!(Child(node, "offsets") is YamlSequenceNode offsets) || relief.Width < 2 || relief.Depth < 2 || offsets.Children.Count != relief.Width * relief.Depth)
                throw new FormatException("\"relief\" needs width and depth of at least 2 and width x depth offsets");
            relief.Offsets = new float[offsets.Children.Count];
            for (int i = 0; i < relief.Offsets.Length; i++)
                relief.Offsets[i] = Number(offsets.Children[i]);
            return relief;
        }

        private static GroundPaint Paint(string name)
        {
            if (name != null && Enum.TryParse(name, true, out GroundPaint paint) && Enum.IsDefined(typeof(GroundPaint), paint))
                return paint;
            throw new FormatException($"unknown paint \"{name}\" (dirt, paved, cultivated, grass, original, clearvegetation)");
        }

        private static YamlNode Child(YamlMappingNode map, string key)
        {
            return map.Children.TryGetValue(new YamlScalarNode(key), out YamlNode node) ? node : null;
        }

        private static string Text(YamlMappingNode map, string key) => Child(map, key) is YamlScalarNode s ? s.Value : null;

        private static string Scalar(YamlNode node)
        {
            if (node is YamlScalarNode s && !string.IsNullOrEmpty(s.Value))
                return s.Value;
            throw new FormatException("a piece has no prefab name");
        }

        private static float Number(YamlNode node)
        {
            if (node is YamlScalarNode s && float.TryParse(s.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                return value;
            throw new FormatException($"\"{(node as YamlScalarNode)?.Value}\" is not a number");
        }
    }
}
