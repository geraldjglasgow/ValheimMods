using System.Collections.Generic;

namespace EliteCreaturesReborn.Recap
{
    /// <summary>
    /// A hit in plain facts: how it came and its damage by type, each as an id ("burning", "slash", "edge_of_world") that
    /// the window turns into words when shown (<see cref="Window.RecapText.Word"/>); who dealt it is an
    /// <see cref="AttackerName"/>, read the moment it hit, since the creature may be gone when the recap is watched.
    /// </summary>
    internal static class HitDescribe
    {
        /// <summary>A creature's stars after its name, " 2★"; empty for none.</summary>
        public static string Stars(int stars) => stars > 0 ? " " + stars + "★" : "";

        /// <summary>How the hit came, in the game's own causes; a creature's or player's hit is "ranged" or "hit".</summary>
        public static string Source(HitData hit) => hit.m_hitType switch
        {
            HitData.HitType.Burning => "burning",
            HitData.HitType.Poisoned => "poison",
            HitData.HitType.Freezing => "freezing",
            HitData.HitType.Fall => "fall",
            HitData.HitType.Drowning => "drowning",
            HitData.HitType.EdgeOfWorld => "edge_of_world",
            HitData.HitType.Tree => "falling_tree",
            HitData.HitType.Structural => "falling_building",
            HitData.HitType.Boat => "ship",
            HitData.HitType.AshlandsOcean => "boiling_sea",
            HitData.HitType.AshlandsLava => "lava",
            HitData.HitType.CinderFire => "cinder_fire",
            HitData.HitType.Undefined or HitData.HitType.EnemyHit or HitData.HitType.PlayerHit => hit.m_ranged ? "ranged" : "hit",
            _ => hit.m_hitType.ToString().ToLowerInvariant(),
        };

        /// <summary>
        /// The damage by type, largest first. The game's untyped damage (a fall, drowning) is named after its cause.
        /// </summary>
        public static List<KeyValuePair<string, float>> Parts(HitData hit, string source)
        {
            HitData.DamageTypes d = hit.m_damage;
            List<KeyValuePair<string, float>> parts = new List<KeyValuePair<string, float>>();
            Add(parts, source == "hit" || source == "ranged" ? "true" : source, d.m_damage);
            Add(parts, "blunt", d.m_blunt);
            Add(parts, "slash", d.m_slash);
            Add(parts, "pierce", d.m_pierce);
            Add(parts, "chop", d.m_chop);
            Add(parts, "pickaxe", d.m_pickaxe);
            Add(parts, "fire", d.m_fire);
            Add(parts, "frost", d.m_frost);
            Add(parts, "lightning", d.m_lightning);
            Add(parts, "poison", d.m_poison);
            Add(parts, "spirit", d.m_spirit);
            parts.Sort((a, b) => b.Value.CompareTo(a.Value));
            return parts;
        }

        private static void Add(List<KeyValuePair<string, float>> parts, string name, float value)
        {
            if (value > 0.1f)
            {
                parts.Add(new KeyValuePair<string, float>(name, value));
            }
        }
    }
}
