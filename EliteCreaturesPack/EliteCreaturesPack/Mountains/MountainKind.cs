using UnityEngine;

namespace EliteCreaturesPack.Mountains
{
    /// <summary>Stable save identities and the five mountain encounter roles.</summary>
    public sealed class MountainKind
    {
        public static readonly MountainKind[] All =
        {
            new MountainKind("Frostfang", "Frostfang", "Wolf", "ecp_frostfang", 1400f, 1.65f, 1.4f, 3f, 1200f),
            new MountainKind("Rimeback", "Rimeback", "Lox", "ecp_rimeback", 650f, .65f, .65f, 7f, 600f),
            new MountainKind("ScreeWing", "Scree Wing", "Hatchling", "ecp_scree_wing", 240f, 1.1f, 1f, 10f, 480f),
            new MountainKind("CairnWight", "Cairn Wight", "Fenring", "ecp_cairn_wight", 450f, .9f, 1.1f, 8f, 600f),
            new MountainKind("IceCrawler", "Ice Crawler", "Neck", "ecp_ice_crawler", 180f, 2f, 5f, 15f, 360f),
        };
        public readonly string Id, Name, Base, Asset;
        public readonly float Health, Scale, Damage, Chance, Interval;
        public GameObject? Prefab;
        public string Creature => "ECP_" + Id;
        public string Word => "enemy_ecp_" + Id.ToLowerInvariant();
        public bool Boss => Id == "Frostfang";
        public bool Night => Id == "CairnWight";
        public bool Flying => Id == "ScreeWing";
        private MountainKind(string id, string name, string source, string asset, float health, float scale,
            float damage, float chance, float interval)
        {
            Id = id; Name = name; Base = source; Asset = asset;
            Health = health; Scale = scale; Damage = damage; Chance = chance; Interval = interval;
        }
        public static MountainKind? Find(string prefab)
        {
            foreach (MountainKind kind in All) if (kind.Creature == prefab) return kind;
            return null;
        }
    }
}
