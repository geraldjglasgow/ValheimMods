using System;
using System.Collections.Generic;
using System.Text;

namespace EliteCrafting.Core
{
    /// <summary>
    /// Maps our PascalCase enums to the snake_case ids the YAML and item data use (<c>RerollAffixes</c> ↔
    /// <c>reroll_affixes</c>, <c>UtilityItem</c> ↔ <c>utility_item</c>). The table is built once per enum type.
    /// </summary>
    public static class EnumIds<T> where T : struct, Enum
    {
        private static readonly Dictionary<string, T> ByIdTable = new Dictionary<string, T>(StringComparer.Ordinal);
        private static readonly Dictionary<T, string> IdByValue = new Dictionary<T, string>();

        static EnumIds()
        {
            foreach (T value in (T[])Enum.GetValues(typeof(T)))
            {
                string id = ToSnake(value.ToString());
                ByIdTable[id] = value;
                IdByValue[value] = id;
            }
        }

        /// <summary>The snake_case id of a value.</summary>
        public static string Id(T value) => IdByValue.TryGetValue(value, out string id) ? id : ToSnake(value.ToString());

        /// <summary>Parses a snake_case id (exact, lowercase).</summary>
        public static bool TryParse(string? id, out T value)
        {
            value = default;
            return id != null && ByIdTable.TryGetValue(id, out value);
        }

        /// <summary>All ids, in declaration order, for error messages.</summary>
        public static string Joined => string.Join(", ", IdByValue.Values);

        private static string ToSnake(string name)
        {
            StringBuilder sb = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c) && i > 0)
                {
                    sb.Append('_');
                }
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
    }
}
