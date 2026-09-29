using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesPack.Swamp
{
    /// <summary>Stable prefab identities and the five distinct swamp silhouettes and fighting roles.</summary>
    public sealed class SwampKind
    {
        public static readonly SwampKind[] All =
        {
            new SwampKind("MireJarl", "Mire Jarl", "Draugr_Elite", "ecp_mire_jarl", 900f, 1.35f, 1.35f, 4f, 900f),
            new SwampKind("ReedStalker", "Reed Stalker", "Draugr", "ecp_reed_stalker", 160f, 1.05f, 1f, 12f, 360f),
            new SwampKind("BogMaw", "Bog Maw", "Blob", "ecp_bog_maw", 200f, 1.25f, 1.2f, 10f, 420f),
            new SwampKind("FenCrawler", "Fen Crawler", "Neck", "ecp_fen_crawler", 100f, 1.5f, 3f, 18f, 300f),
            new SwampKind("DrownedShade", "Drowned Shade", "Wraith", "ecp_drowned_shade", 220f, 1.1f, 1.15f, 10f, 480f),
        };

        public readonly string Id, Name, Base, Asset;
        public readonly float Health, Scale, Damage, Chance, Interval;
        public GameObject? Prefab;
        public string Creature => "ECP_" + Id;
        public string Word => "enemy_ecp_" + Id.ToLowerInvariant();
        public bool Jarl => Id == "MireJarl";
        public bool Night => Id == "DrownedShade";

        private SwampKind(string id, string name, string source, string asset, float health, float scale,
            float damage, float chance, float interval)
        {
            Id = id; Name = name; Base = source; Asset = asset;
            Health = health; Scale = scale; Damage = damage; Chance = chance; Interval = interval;
        }

        public static SwampKind? Find(string prefab)
        {
            foreach (SwampKind kind in All)
                if (kind.Creature == prefab) return kind;
            return null;
        }
    }
}
