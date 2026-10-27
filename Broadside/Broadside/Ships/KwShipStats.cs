using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How fast, handy and tough a launched ship is, from its build (formulas in FORMULAS.md):
    /// <list type="bullet">
    /// <item>top speed = size × beam × height × materials × rudder: size (1.5 t ÷ its weight)^0.1 (0.55 to 1.15); beam
    /// 1 − (beam ÷ length − 0.25) (0.6 to 1.05); height 1 − 0.06 × (hull depth − 2 m) (0.6 to 1); materials 1 + 0.025 ×
    /// the hull parts' average biome tier (Meadows 0 to Ashlands 6); rudder 1 + 0.01 × its biome tier. Set through the
    /// drag along the ship (the karve's ÷ speed²), so push and drag meet at that speed.</item>
    /// <item>sail push = the karve's × its sails' pull ÷ (length × depth), against the karve's own ratio, ¼ to 2 times; a
    /// sail's power is its area × cloth drive (raw hide 0.8, linen 1, scale hide 1.25), every sail on every mast and every
    /// jib on a bowsprit counted,
    /// added pointing forward and taken away pointing aft (<see cref="KwBpBow"/>); none when they cancel. A larger ship's
    /// larger sails do not make it faster. How much of it a wind gives is each kind of sail's own by the apparent wind
    /// (<see cref="Mix"/>, <see cref="KwSailPolar"/>): square sails with the wind from abaft the beam, fore-and-aft sails
    /// on every point outside their no-go zone (<see cref="KwShipWind"/>).</item>
    /// <item>steering = the rudder's (1.00 to 1.30 by tier): the turning push and how fast the helm swings; no rudder,
    /// a third of the turning push.</item>
    /// <item>health = 500 × (weight ÷ 38.6 t)^0.7 × (1 + 0.3 × the materials' tier), 150 to 30000: a ship the size of the
    /// game's longship (104 t) about its 1000, so a small iron cannon sinks one in as many hits as the game's own (10), and
    /// a big one in more, not quite in proportion to its weight (2026-10-26, the user: "With a small iron cannon, maybe
    /// about 20 shots in total should sink a fairly large ship": the user's 470 t bubba 2875, about 20 then, 28 since the cannons' second quarter off the same evening); a ship launched
    /// before keeps no more than that (<see cref="Apply"/>).</item>
    /// <item>perks by the materials' tier (<see cref="KwShipPerks"/>): the Ashlands sea's burning (<see cref="AshlandsBurn"/>)
    /// and the fire modifier (<see cref="Fire"/>).</item>
    /// </list>
    /// </summary>
    public sealed class KwShipStats
    {
        // internal: the blueprint table's figures work from the same numbers (KwBpHandling)
        internal const float KarveWeight = 1500f, KarveHealth = 500f, KarveDrag = 0.001f, KarveFactor = 0.03f, KarveRatio = 1.33f;
        internal const float HealthWeight = 38600f, HealthPower = 0.7f, MostHealth = 30000f;
        internal const float KarveSteer = 0.18f, KarveHelm = 1f;

        public float Speed { get; private set; } = 1f;
        public float SailFactor { get; private set; }
        public float Steering { get; private set; } = 1f;
        public float Health { get; private set; } = KarveHealth;
        public float Materials { get; private set; }
        public int Rudder { get; private set; } = -1;

        /// <summary>The ship's sails by kind, their areas and how each takes the wind (<see cref="KwSailMix"/>).</summary>
        public KwSailMix Mix { get; private set; } = KwSailMix.None;

        /// <summary>The share of the Ashlands sea's burning the hull takes by its materials' tier, 1 to 0 (<see cref="KwShipPerks"/>).</summary>
        public float AshlandsBurn => KwShipPerks.Burn(Materials);

        /// <summary>The hull's fire modifier by its materials' tier; null below Swamp, the karve's own kept (<see cref="KwShipPerks"/>).</summary>
        public HitData.DamageModifier? Fire => KwShipPerks.Fire(Materials);

        public static KwShipStats Of(KwBlueprint blueprint, KwBpHull hull, float weight)
        {
            KwShipStats stats = new KwShipStats { Materials = MaterialTier(blueprint), Rudder = RudderTier(blueprint) };
            float size = Mathf.Clamp(Mathf.Pow(KarveWeight / Mathf.Max(weight, 1f), 0.1f), 0.55f, 1.15f);
            float beam = Mathf.Clamp(1f - (hull.Beam / hull.Length - 0.25f), 0.6f, 1.05f);
            float height = Mathf.Clamp(1f - 0.06f * (hull.Depth - 2f), 0.6f, 1f);
            float rudder = 1f + 0.01f * Mathf.Max(0, stats.Rudder);
            stats.Speed = size * beam * height * (1f + 0.025f * stats.Materials) * rudder;
            stats.SailFactor = Sails(blueprint, hull);
            stats.Mix = KwSailMix.Of(blueprint);
            stats.Steering = stats.Rudder < 0 ? 1f / 3f : KwRudderTiers.All[stats.Rudder].Steering;
            stats.Health = Mathf.Clamp(KarveHealth * Mathf.Pow(Mathf.Max(0f, weight) / HealthWeight, HealthPower) * (1f + 0.3f * stats.Materials), 150f, MostHealth);
            return stats;
        }

        /// <summary>The numbers onto the game's ship and its wear, and its sails by kind onto its wind (<see cref="KwShipWind"/>).</summary>
        public void Apply(Ship ship, WearNTear? wear)
        {
            ship.m_sailForceFactor = SailFactor;
            (ship.GetComponent<KwShipWind>() ?? ship.gameObject.AddComponent<KwShipWind>()).Mix = Mix;
            ship.m_dampingForward = KarveDrag / (Speed * Speed);
            ship.m_stearVelForceFactor = KarveSteer * Steering;
            ship.m_stearForce *= Steering;
            ship.m_rudderSpeed = KarveHelm * Mathf.Max(Steering, 0.6f);
            if (wear != null)
                Cap(wear);
            KwShipPerks.Apply(ship, wear, this);
        }

        // The ship's full health; its owner brings a stored health above it down to it (ships launched before the health
        // was brought in line with the game's ships kept their old, larger health).
        private void Cap(WearNTear wear)
        {
            wear.m_health = Health;
            ZDO? zdo = wear.m_nview != null && wear.m_nview.IsValid() && wear.m_nview.IsOwner() ? wear.m_nview.GetZDO() : null;
            if (zdo != null && zdo.GetFloat(ZDOVars.s_health, Health) > Health)
                zdo.Set(ZDOVars.s_health, Health);
        }

        // The sails' push against the karve's: their pull along the bow (area × cloth drive, aft-pointing sails taking
        // away, KwBpBow.Pull) per length × depth, against the karve's ratio; none without sails or when they cancel.
        private static float Sails(KwBlueprint blueprint, KwBpHull hull)
        {
            float pull = Mathf.Max(0f, KwBpBow.Pull(blueprint));
            float ratio = pull / Mathf.Max(hull.Length * hull.Depth, 1f);
            return pull <= 0f ? 0f : KarveFactor * Mathf.Clamp(ratio / KarveRatio, 0.25f, 2f);
        }

        // The hull, keel and deck parts' average biome tier (0 Meadows to 6 Ashlands).
        private static float MaterialTier(KwBlueprint blueprint)
        {
            float sum = 0f;
            int count = 0;
            List<KwBiomeTier> tiers = new List<KwBiomeTier>(KwBiomeTiers.All);
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                string tool = piece.Prefab.StartsWith(KwToolPieces.PartPrefix) ? piece.Prefab.Substring(KwToolPieces.PartPrefix.Length) : "";
                if (KwBiomeTiers.PartOf(tool) == null)
                    continue;
                (sum, count) = (sum + Mathf.Max(0, tiers.IndexOf(KwBiomeTiers.Of(piece.Data))), count + 1);
            }
            return count > 0 ? sum / count : 0f;
        }

        // The ship's rudder's biome tier (0 Meadows to 6 Ashlands), or -1 with none.
        private static int RudderTier(KwBlueprint blueprint)
        {
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                if (!KwRudder.Is(piece.Prefab))
                    continue;
                for (int i = 0; i < KwRudderTiers.All.Count; i++)
                {
                    if (KwRudderTiers.All[i].Prefab == piece.Prefab)
                        return i;
                }
            }
            return -1;
        }
    }
}
