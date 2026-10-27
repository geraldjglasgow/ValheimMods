using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Broadside
{
    /// <summary>One saved ZDO value of a piece: its key's hash and a string, an int or a float.</summary>
    public readonly struct KwBpValue
    {
        public KwBpValue(int hash, object value)
        {
            Hash = hash;
            Value = value;
        }

        public int Hash { get; }
        public object Value { get; }
    }

    /// <summary>
    /// What a ship piece keeps in its ZDO besides the game's own state, saved with it in a blueprint and given back to the
    /// piece built from it: every string, int and float whose key is not one of the game's (<see cref="ZDOVars"/>: health,
    /// support, creator, owner, fuel, items, text and the rest stay the game's business) and not the ship id. So a
    /// generated part's span (<see cref="KwGenerated.Key"/>) comes back, and so does any key a fitting adds later without
    /// a change here. Keys are read by hash; the ones named in <see cref="Known"/> are written by name, any other as
    /// "#hash". A railing's hung shields are saved as data only (<see cref="DataOnly"/>): a piece built again gets none.
    /// </summary>
    public static class KwBpData
    {
        // Custom keys the kit's pieces are known to keep, for readable files (unknown ones still travel, as "#hash"): a
        // generated part's span and biome tier, a rudder's linkage to its helm, its steering gear and the waterline on it, a name board's name and a railing's hung shields.
        private static readonly string[] Known = { KwGenerated.Key, KwBiomeTiers.Key, KwRudderLay.Key, KwRudderGear.Key, KwRudderWater.Key, KwNameBoard.Key, KwShieldRecord.Key };

        // Keys a blueprint keeps as data only, never given back to a piece built from it: a railing's hung shields are
        // real items, which building a blueprint must not make again.
        private static readonly HashSet<int> DataOnly = new HashSet<int> { KwShieldRecord.Hash };

        private static Dictionary<int, string>? names;
        private static HashSet<int>? game;

        public static List<KwBpValue> Capture(ZDO zdo)
        {
            ZDOExtraData.GetData(zdo.m_uid, out List<KeyValuePair<int, float>> floats, out _, out _, out List<KeyValuePair<int, int>> ints, out _,
                out List<KeyValuePair<int, string>> strings, out _, out _);
            List<KwBpValue> values = new List<KwBpValue>();
            foreach (KeyValuePair<int, string> pair in strings)
                Keep(values, pair.Key, pair.Value);
            foreach (KeyValuePair<int, int> pair in ints)
                Keep(values, pair.Key, pair.Value);
            foreach (KeyValuePair<int, float> pair in floats)
                Keep(values, pair.Key, pair.Value);
            return values;
        }

        public static void Apply(ZDO zdo, List<KwBpValue> values)
        {
            foreach (KwBpValue value in values)
            {
                if (DataOnly.Contains(value.Hash))
                    continue;
                if (value.Value is string text)
                    zdo.Set(value.Hash, text);
                else if (value.Value is int number)
                    zdo.Set(value.Hash, number);
                else if (value.Value is float real)
                    zdo.Set(value.Hash, real);
            }
        }

        /// <summary>The first string saved under this key, or null.</summary>
        public static string? Text(List<KwBpValue> values, int hash)
        {
            foreach (KwBpValue value in values)
            {
                if (value.Hash == hash && value.Value is string text)
                    return text;
            }
            return null;
        }

        /// <summary>How a key is written in a file: its name when known, else "#" and its hash.</summary>
        public static string KeyName(int hash) => Names().TryGetValue(hash, out string name) ? name : "#" + hash.ToString(CultureInfo.InvariantCulture);

        public static int KeyHash(string key) =>
            key.StartsWith("#") && int.TryParse(key.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int hash) ? hash : key.GetStableHashCode();

        private static void Keep(List<KwBpValue> values, int hash, object value)
        {
            if (hash != KwBlueprintStation.ShipHash && !Game().Contains(hash))
                values.Add(new KwBpValue(hash, value));
        }

        private static Dictionary<int, string> Names()
        {
            if (names != null)
                return names;
            names = new Dictionary<int, string>();
            foreach (string name in Known)
                names[name.GetStableHashCode()] = name;
            return names;
        }

        // Every key hash the game itself declares.
        private static HashSet<int> Game()
        {
            if (game != null)
                return game;
            game = new HashSet<int>();
            foreach (FieldInfo field in typeof(ZDOVars).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (field.FieldType == typeof(int) && field.GetValue(null) is int hash)
                    game.Add(hash);
            }
            return game;
        }
    }
}
