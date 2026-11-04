using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How fast, handy and tough a launched ship is, from its build (formulas in FORMULAS.md):
    /// <list type="bullet">
    /// <item>hull speed = size × beam × height × materials × rudder: size (1.5 t ÷ its weight)^0.005 (a 600 t ship 0.97);
    /// beam 1 − ½ × (beam ÷ length − 0.25) (0.6 to 1.05); height 1 − 0.005 × (hull depth − 2 m), at most 1; materials
    /// 1 + 0.025 × the hull parts' average biome tier (Meadows 0 to Ashlands 6); rudder 1 + 0.01 × its biome tier. Set
    /// through the drag along the ship (the karve's ÷ speed²), so push and drag meet at that speed: under oars, which push
    /// as a karve's, it is the ship's speed against a karve's.</item>
    /// <item>sail push = the karve's × 1.25 × its sails' pull ÷ (length × depth) ÷ a full rig's 0.665 (half the karve's own
    /// 1.33: a big ship's masts cannot carry the karve's sail for its size), ¼ to 1.25 times: a full rig or more pushes
    /// 1.25 times the karve's, less sail less in proportion. A sail's power is its area × cloth drive (raw hide 0.8, linen
    /// 1, scale hide 1.25), every sail on every mast and every jib on a bowsprit counted,
    /// added pointing forward and taken away pointing aft (<see cref="KwBpBow"/>); none when they cancel. A larger ship's
    /// larger sails do not make it faster. How much of it a wind gives is each kind of sail's own by the apparent wind
    /// (<see cref="Mix"/>, <see cref="KwSailPolar"/>): square sails with the wind from abaft the beam, fore-and-aft sails
    /// on every point outside their no-go zone (<see cref="KwShipWind"/>). Under sail the top speed against a karve's is
    /// about the hull speed × √(sail push ÷ the karve's) (<see cref="KwBpHandling"/> works it out): a big, well-rigged
    /// ship about the karve's, a small light one a little faster, one short of a full rig slower (2026-11-03, the user:
    /// "we need to be more generous with the formula for ship speed"; the size, depth and beam penalties and the karve's
    /// sail ratio before held a 26 m ship to 36% of a karve).</item>
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

        // A full rig: half the karve's sail power for its length × depth, pushing FullPush times the karve's sail. Size and
        // depth slow a hull gently (SizePower of its weight, DepthLoss a metre past 2 m), a broad one by BeamLoss of its
        // beam's share of its length past a quarter.
        private const float FullRig = KarveRatio / 2f, FullPush = 1.25f, SizePower = 0.005f, DepthLoss = 0.005f, BeamLoss = 0.5f;

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
            stats.Speed = HullSpeed(hull, weight, stats.Materials, stats.Rudder);
            stats.SailFactor = Sails(blueprint, hull);
            stats.Mix = KwSailMix.Of(blueprint);
            stats.Steering = stats.Rudder < 0 ? 1f / 3f : KwRudderTiers.All[stats.Rudder].Steering;
            stats.Health = Mathf.Clamp(KarveHealth * Mathf.Pow(Mathf.Max(0f, weight) / HealthWeight, HealthPower) * (1f + 0.3f * stats.Materials), 150f, MostHealth);
            return stats;
        }

        /// <summary>The numbers onto the ship's trim and its wear, and its sails by kind onto its wind (<see cref="KwShipWind"/>).</summary>
        public void Apply(Ship ship, WearNTear? wear)
        {
            Trim(ship);
            if (wear != null)
                Cap(wear, KwShipTrim.Of(ship));
            KwShipPerks.Apply(ship, wear, this);
        }

        /// <summary>
        /// The figures onto the ship's trim as shares of the karve's (<see cref="KwShipTrim"/>, the game's fields left as
        /// they are: the karve's, or what another mod set): sail push, drag along the ship (the karve's ÷ speed²), the
        /// turning pushes and the helm's swing (at least 0.6 of the karve's); its health; its sails by kind onto its wind.
        /// </summary>
        public void Trim(Ship ship)
        {
            KwShipTrim trim = KwShipTrim.Of(ship);
            trim.Sail = SailFactor / KarveFactor;
            trim.Drag = 1f / (Speed * Speed);
            trim.Steer = Steering;
            trim.Helm = Mathf.Max(Steering, 0.6f) / KarveHelm;
            trim.Health = Health;
            (ship.GetComponent<KwShipWind>() ?? ship.gameObject.AddComponent<KwShipWind>()).Mix = Mix;
        }

        // The ship's full health; its owner brings a stored health above it down to it (ships launched before the health
        // was brought in line with the game's ships kept their old, larger health).
        private static void Cap(WearNTear wear, KwShipTrim trim)
        {
            trim.Hold();
            float full = wear.m_health;
            ZDO? zdo = wear.m_nview != null && wear.m_nview.IsValid() && wear.m_nview.IsOwner() ? wear.m_nview.GetZDO() : null;
            if (zdo != null && zdo.GetFloat(ZDOVars.s_health, full) > full)
                zdo.Set(ZDOVars.s_health, full);
        }

        // The hull's speed against the karve's: size × beam × height × materials × rudder (no rudder counts as Meadows').
        private static float HullSpeed(KwBpHull hull, float weight, float materials, int rudderTier)
        {
            float size = Mathf.Pow(KarveWeight / Mathf.Max(weight, 1f), SizePower);
            float beam = Mathf.Clamp(1f - BeamLoss * (hull.Beam / hull.Length - 0.25f), 0.6f, 1.05f);
            float height = Mathf.Min(1f, 1f - DepthLoss * (hull.Depth - 2f));
            float rudder = 1f + 0.01f * Mathf.Max(0, rudderTier);
            return size * beam * height * (1f + 0.025f * materials) * rudder;
        }

        // The sails' push against the karve's: their pull along the bow (area × cloth drive, aft-pointing sails taking
        // away, KwBpBow.Pull) per length × depth, FullPush times the karve's at a full rig and in proportion below it, at
        // least a quarter of the karve's; none without sails or when they cancel.
        private static float Sails(KwBlueprint blueprint, KwBpHull hull)
        {
            float pull = Mathf.Max(0f, KwBpBow.Pull(blueprint));
            float ratio = pull / Mathf.Max(hull.Length * hull.Depth, 1f);
            return pull <= 0f ? 0f : KarveFactor * Mathf.Clamp(FullPush * ratio / FullRig, 0.25f, FullPush);
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
