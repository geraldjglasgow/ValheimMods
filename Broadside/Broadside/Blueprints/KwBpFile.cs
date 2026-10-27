using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A blueprint as JSON: <c>name</c>, <c>format</c> (1), a note on the <c>frame</c>, and <c>pieces</c>, one object per
    /// piece: <c>prefab</c>, <c>pos</c> [x, y, z] and <c>rot</c> [x, y, z, w] in the dock's frame, and its saved ZDO values
    /// by kind, <c>s</c> strings, <c>i</c> ints and <c>f</c> floats, each an object of key (a name, or "#hash") to value.
    /// A piece whose prefab the game does not have (one the kit no longer makes) is left out as the file is read, and the
    /// log says so once for each blueprint; a ship read stern first is turned round, bow at +z (<see cref="KwBpBow"/>).
    /// </summary>
    public static class KwBpFile
    {
        public const int Format = 1;
        private const string FrameNote = "Unity axes of the dock's aft section: x right, y up, z forward; metres from its origin on the ground at its aft end";
        private static readonly HashSet<string> Told = new HashSet<string>();

        public static string Write(KwBlueprint blueprint)
        {
            StringBuilder s = new StringBuilder("{\n  \"name\": ").Append(KwBpJson.Quote(blueprint.Name));
            s.Append(",\n  \"format\": ").Append(Format).Append(",\n  \"frame\": ").Append(KwBpJson.Quote(FrameNote));
            s.Append(",\n  \"pieces\": [");
            for (int i = 0; i < blueprint.Pieces.Count; i++)
                s.Append(i == 0 ? "\n    " : ",\n    ").Append(Row(blueprint.Pieces[i]));
            return s.Append("\n  ]\n}\n").ToString();
        }

        public static KwBlueprint Read(string name, string text)
        {
            KwBlueprint blueprint = Exact(name, text);
            LeaveOutUnknown(blueprint);
            KwBpBow.Fix(blueprint);
            return blueprint;
        }

        /// <summary>
        /// Every piece as written, in its order, none left out and none turned: a launched ship's own blueprint
        /// (<see cref="KwShipData"/>), whose pieces' indices its marks and sea edits key on (a removed piece's tombstone,
        /// <see cref="KwSeaApply.Gone"/>, is no prefab the game has) and which was turned bow forward once, at launch.
        /// </summary>
        public static KwBlueprint Exact(string name, string text)
        {
            if (!(KwBpJson.Parse(text) is Dictionary<string, object?> root))
                throw new FormatException("the file is not a JSON object");
            if (!(Get(root, "pieces") is List<object?> rows))
                throw new FormatException("it has no \"pieces\" list");
            KwBlueprint blueprint = new KwBlueprint(name);
            foreach (object? row in rows)
                blueprint.Pieces.Add(Piece(row as Dictionary<string, object?> ?? throw new FormatException("a piece is not an object")));
            return blueprint;
        }

        // The pieces whose prefab the game does not have go, with one line in the log (the first time this blueprint has
        // them); before the network scene is up nothing is known, so every piece stays.
        private static void LeaveOutUnknown(KwBlueprint blueprint)
        {
            SortedDictionary<string, int> unknown = Unknown(blueprint);
            if (unknown.Count == 0)
                return;
            int left = blueprint.Pieces.RemoveAll(p => unknown.ContainsKey(p.Prefab));
            List<string> names = new List<string>();
            foreach (KeyValuePair<string, int> pair in unknown)
                names.Add(pair.Value > 1 ? $"{pair.Key} x{pair.Value}" : pair.Key);
            string told = $"Broadside blueprint {blueprint.Name}: left out {left} piece{(left == 1 ? "" : "s")} the game has no prefab for: {string.Join(", ", names)}";
            if (Told.Add(told))
                Log.Warn(told);
        }

        // Each prefab of the blueprint's that the game does not have, and how many pieces are of it.
        private static SortedDictionary<string, int> Unknown(KwBlueprint blueprint)
        {
            SortedDictionary<string, int> unknown = new SortedDictionary<string, int>(StringComparer.Ordinal);
            ZNetScene? scene = ZNetScene.instance;
            if (scene == null)
                return unknown;
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                if (scene.GetPrefab(piece.Prefab) == null)
                    unknown[piece.Prefab] = unknown.TryGetValue(piece.Prefab, out int count) ? count + 1 : 1;
            }
            return unknown;
        }

        private static string Row(KwBpPiece piece)
        {
            Vector3 p = piece.Position;
            Quaternion q = piece.Rotation;
            StringBuilder s = new StringBuilder("{ \"prefab\": ").Append(KwBpJson.Quote(piece.Prefab));
            s.Append(", \"pos\": [").Append(N(p.x, 4)).Append(", ").Append(N(p.y, 4)).Append(", ").Append(N(p.z, 4)).Append(']');
            s.Append(", \"rot\": [").Append(N(q.x, 6)).Append(", ").Append(N(q.y, 6)).Append(", ").Append(N(q.z, 6)).Append(", ").Append(N(q.w, 6)).Append(']');
            Section(s, piece.Data, "s", v => v is string text ? KwBpJson.Quote(text) : null);
            Section(s, piece.Data, "i", v => v is int number ? number.ToString(CultureInfo.InvariantCulture) : null);
            Section(s, piece.Data, "f", v => v is float real ? real.ToString("R", CultureInfo.InvariantCulture) : null);
            return s.Append(" }").ToString();
        }

        private static void Section(StringBuilder s, List<KwBpValue> data, string kind, Func<object, string?> format)
        {
            List<string> items = new List<string>();
            foreach (KwBpValue value in data)
            {
                string? text = format(value.Value);
                if (text != null)
                    items.Add(KwBpJson.Quote(KwBpData.KeyName(value.Hash)) + ": " + text);
            }
            if (items.Count > 0)
                s.Append(", \"").Append(kind).Append("\": { ").Append(string.Join(", ", items)).Append(" }");
        }

        private static string N(float value, int decimals) => value.ToString("0." + new string('#', decimals), CultureInfo.InvariantCulture);

        private static KwBpPiece Piece(Dictionary<string, object?> row)
        {
            string prefab = Get(row, "prefab") as string ?? throw new FormatException("a piece has no \"prefab\"");
            List<float> p = Numbers(row, "pos", 3), q = Numbers(row, "rot", 4);
            List<KwBpValue> data = new List<KwBpValue>();
            Values(row, "s", data, v => v as string);
            Values(row, "i", data, v => v is double d ? (object)(int)Math.Round(d) : null);
            Values(row, "f", data, v => v is double d ? (object)(float)d : null);
            Quaternion turn = Quaternion.Normalize(new Quaternion(q[0], q[1], q[2], q[3]));
            return new KwBpPiece(prefab, new Vector3(p[0], p[1], p[2]), turn, data);
        }

        private static List<float> Numbers(Dictionary<string, object?> row, string key, int count)
        {
            List<float> numbers = new List<float>();
            if (Get(row, key) is List<object?> list)
            {
                foreach (object? item in list)
                {
                    if (item is double d)
                        numbers.Add((float)d);
                }
            }
            if (numbers.Count != count)
                throw new FormatException($"a piece's \"{key}\" is not {count} numbers");
            return numbers;
        }

        private static void Values(Dictionary<string, object?> row, string kind, List<KwBpValue> data, Func<object?, object?> convert)
        {
            if (!(Get(row, kind) is Dictionary<string, object?> map))
                return;
            foreach (KeyValuePair<string, object?> pair in map)
            {
                object? value = convert(pair.Value);
                if (value != null)
                    data.Add(new KwBpValue(KwBpData.KeyHash(pair.Key), value));
            }
        }

        private static object? Get(Dictionary<string, object?> map, string key) => map.TryGetValue(key, out object? value) ? value : null;
    }
}
