using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// Hair and beard items for a human's look. The game's own are the ones its character creator offers: the
    /// Customization items named Hair... and Beard..., without the helmet variants (names with an underscore), the
    /// creator's "none" items (HairNone, BeardNone) among them. Read once per ObjectDB, which every world load makes anew.
    /// A definition may name any Customization item of the kind, or "none" (no item at all); the human step checks the
    /// names as the creature is built (<see cref="Unknown"/>), so the roll never meets one the game lacks.
    /// </summary>
    internal static class HumanHairs
    {
        public const string HairKind = "Hair", BeardKind = "Beard";
        private const string None = "none";

        private static ObjectDB? readFrom;
        private static int[] hairs = new int[0], beards = new int[0];

        /// <summary>
        /// The item hash of a random entry of `wanted`; "none" is no item (0). An empty list, or an entry naming no item
        /// the game has, takes a random one of the game's own of that kind.
        /// </summary>
        public static int Pick(string[] wanted, string kind)
        {
            ObjectDB? db = ObjectDB.instance;
            if (db == null)
            {
                return 0;
            }
            if (wanted.Length > 0)
            {
                string name = wanted[Random.Range(0, wanted.Length)];
                if (IsNone(name))
                {
                    return 0;
                }
                if (db.GetItemPrefab(name) != null)
                {
                    return name.GetStableHashCode();
                }
            }
            int[] offered = Offered(db, kind);
            return offered.Length == 0 ? 0 : offered[Random.Range(0, offered.Length)];
        }

        /// <summary>The names in `wanted` that are neither "none" nor a hair (or beard) item of the game or a mod; none
        /// without an ObjectDB to ask.</summary>
        public static List<string> Unknown(IEnumerable<string> wanted, string kind)
        {
            ObjectDB? db = ObjectDB.instance;
            return db == null ? new List<string>() : wanted.Where(name => !IsNone(name) && !IsStyle(db, name, kind)).ToList();
        }

        /// <summary>The game's own of the kind by name, and "none", for a message.</summary>
        public static string Choices(string kind)
        {
            ObjectDB? db = ObjectDB.instance;
            IEnumerable<string> names = db == null ? Enumerable.Empty<string>() : Names(db, kind);
            return string.Join(", ", names.Concat(new[] { None }));
        }

        private static bool IsNone(string name) => string.Equals(name, None, StringComparison.OrdinalIgnoreCase);

        private static bool IsStyle(ObjectDB db, string name, string kind)
        {
            GameObject? prefab = db.GetItemPrefab(name);
            ItemDrop? item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            return item != null && item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Customization
                && name.StartsWith(kind, StringComparison.Ordinal);
        }

        private static int[] Offered(ObjectDB db, string kind)
        {
            if (readFrom != db)
            {
                (readFrom, hairs, beards) = (db, Read(db, HairKind), Read(db, BeardKind));
            }
            return kind == BeardKind ? beards : hairs;
        }

        private static int[] Read(ObjectDB db, string kind) => Names(db, kind).Select(name => name.GetStableHashCode()).ToArray();

        private static IEnumerable<string> Names(ObjectDB db, string kind) =>
            db.GetAllItems(ItemDrop.ItemData.ItemType.Customization, kind)
                .Select(item => item.gameObject.name)
                .Where(name => !name.Contains("_"));
    }
}
