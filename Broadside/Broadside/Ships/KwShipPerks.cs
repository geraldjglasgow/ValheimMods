using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a launched ship's materials earn it besides its health (<see cref="KwShipStats"/>: more for a higher tier), by
    /// the hull's average biome tier (Meadows 0 to Ashlands 6):
    /// <list type="bullet">
    /// <item>the Ashlands sea burns it less (<see cref="Burn"/>): all of the game's burning for a Meadows or Black Forest
    /// hull, 90% Swamp, 80% Mountains, 60% Plains, 35% Mistlands, none for an Ashlands hull (which is then Ashlands-ready,
    /// as the game's own drakkar: no burning, no flames), in between for a mixed one. The game burns a ship once a second,
    /// 1 to 30 by how far into the Ashlands sea it is (<c>Ship.TakeAshlandsDamage</c>); the ship's step passes it its time
    /// scaled by the share (<see cref="ShareOf"/>), so it burns that much less often.</item>
    /// <item>fire hurts it less (<see cref="Fire"/>, its wear's fire modifier): the karve's own for Meadows and Black Forest,
    /// slightly resistant for Swamp and Mountains, resistant for Plains and Mistlands, very resistant for Ashlands.</item>
    /// </list>
    /// Set from the stats on every peer (the burning and the hits land on the ship's owner); the helm's hover lists them
    /// with the cargo and the anchor (<see cref="KwShipCard"/>).
    /// </summary>
    public sealed class KwShipPerks : MonoBehaviour
    {
        private static readonly float[] Burns = { 1f, 1f, 0.9f, 0.8f, 0.6f, 0.35f, 0f };

        private static readonly HitData.DamageModifier?[] Fires =
        {
            null, null, HitData.DamageModifier.SlightlyResistant, HitData.DamageModifier.SlightlyResistant,
            HitData.DamageModifier.Resistant, HitData.DamageModifier.Resistant, HitData.DamageModifier.VeryResistant,
        };

        /// <summary>The share of the Ashlands sea's burning this ship takes.</summary>
        public float Share { get; private set; } = 1f;

        /// <summary>The hull's average tier, its health and its fire modifier, as set.</summary>
        public float Tier { get; private set; }
        public float Health { get; private set; }
        public HitData.DamageModifier FireModifier { get; private set; }

        /// <summary>The share of the Ashlands sea's burning a hull of this average tier takes (between the tiers' shares).</summary>
        public static float Burn(float tier)
        {
            float t = Mathf.Clamp(tier, 0f, Burns.Length - 1);
            int low = Mathf.Min((int)t, Burns.Length - 2);
            return Mathf.Lerp(Burns[low], Burns[low + 1], t - low);
        }

        /// <summary>The fire modifier of a hull of this average tier (its nearest tier's); null keeps the ship's own.</summary>
        public static HitData.DamageModifier? Fire(float tier) => Fires[Mathf.Clamp(Mathf.RoundToInt(tier), 0, Fires.Length - 1)];

        /// <summary>The perks onto the game's ship and its wear (from <see cref="KwShipStats.Apply"/>).</summary>
        public static void Apply(Ship ship, WearNTear? wear, KwShipStats stats)
        {
            if (!ship.TryGetComponent(out KwShipPerks perks))
                perks = ship.gameObject.AddComponent<KwShipPerks>();
            (perks.Share, perks.Tier, perks.Health) = (stats.AshlandsBurn, stats.Materials, stats.Health);
            ship.m_ashlandsReady = perks.Share <= 0.001f;
            if (wear != null && stats.Fire is HitData.DamageModifier fire)
                wear.m_damages.m_fire = fire;
            perks.FireModifier = wear != null ? wear.m_damages.m_fire : HitData.DamageModifier.Normal;
        }

        /// <summary>The share of the Ashlands sea's burning a ship takes: its perks', or all of it.</summary>
        public static float ShareOf(Ship ship) => ship.TryGetComponent(out KwShipPerks perks) ? perks.Share : 1f;
    }
}
