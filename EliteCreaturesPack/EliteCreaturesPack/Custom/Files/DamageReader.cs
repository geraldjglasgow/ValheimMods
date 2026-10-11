using System.Collections.Generic;
using EliteCreaturesPack.Custom.Definitions;
using YamlConfig;

namespace EliteCreaturesPack.Custom.Files
{
    /// <summary>
    /// Reads a <see cref="DamageBlock"/>, the shape `damage:` has on a creature and on a new attack: a map of damage type
    /// to number, or <c>total:</c> alone. Also turns a damage type's word into a <see cref="DamageKind"/>.
    /// </summary>
    internal static class DamageReader
    {
        private const float MaxDamage = 1000000f;
        private const string TotalKey = "total";

        public static DamageBlock? Read(YamlNode map, string key, FieldReader fields)
        {
            YamlNode? block = fields.Block(map, key);
            if (block == null)
            {
                return null;
            }
            DamageBlock damage = new DamageBlock();
            foreach (KeyValuePair<string, YamlNode> entry in block.Entries)
            {
                ReadEntry(entry.Key, entry.Value, fields, damage);
            }
            if (damage.Total != null && damage.PerType.Count > 0)
            {
                block.Error("give either damage types or a total, not both");
            }
            return damage;
        }

        private static void ReadEntry(string key, YamlNode value, FieldReader fields, DamageBlock damage)
        {
            fields.Note(value);
            float? amount = fields.NumberOf(value, 0f, MaxDamage);
            if (string.Equals(key.Trim(), TotalKey, System.StringComparison.OrdinalIgnoreCase))
            {
                damage.Total = amount;
            }
            else if (Kind(key, value) is DamageKind kind && amount != null)
            {
                damage.PerType[kind] = amount.Value;
            }
        }

        /// <summary>A damage type's word (blunt, slash, pierce, chop, pickaxe, fire, frost, lightning, poison, spirit), or null and an error at the value.</summary>
        public static DamageKind? Kind(string word, YamlNode at)
        {
            if (Words.TryParse(word, out DamageKind kind))
            {
                return kind;
            }
            at.Error($"'{word}' is not a damage type: {Words.Choices<DamageKind>()}");
            return null;
        }
    }
}
