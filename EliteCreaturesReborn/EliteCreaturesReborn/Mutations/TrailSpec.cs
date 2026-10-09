using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// One creature's ground trail in numbers, read from its rules once: how long a patch lasts, how wide it is, how
    /// far apart they fall, how much a patch slows a player and (ice only) how much grip it leaves them; for fire, how
    /// much it burns, and for roots, how long it holds. Ice and mud read theirs from the rule file; fire and roots have
    /// fixed ones (<see cref="TrailKind"/>), the fire scaled by the creature's star `attack` line. The slow, the burn and
    /// the hold are the trail's gain, so a large star enhances them; life, radius, spacing and grip are its shape and
    /// never are. The same numbers on every machine, since every machine holds the same rules, so a patch acts on a
    /// player exactly as much wherever it is drawn. <see cref="Describe"/> is the `elite inspect` line, read through the
    /// very same calls.
    /// </summary>
    internal readonly struct TrailSpec
    {
        /// <summary>
        /// The most patches one creature may have on the ground at once: a long life spaces drops out in time.
        /// </summary>
        public const int MaxLive = 40;

        /// <summary>The least time between two drops, however fast the creature runs.</summary>
        private const float MinGap = 0.25f;

        /// <summary>
        /// The most a patch may slow a player, however its slow is set or enhanced: never a standstill.
        /// </summary>
        private const float MaxSlow = 90f;

        /// <summary>The longest a root patch may hold a player, however enhanced.</summary>
        private const float MaxHold = 3f;

        public readonly float Life;
        public readonly float Radius;
        public readonly float Spacing;

        /// <summary>The share of a player's speed a patch takes away, 0 to 0.9.</summary>
        public readonly float Slow;

        /// <summary>
        /// The share of their normal grip a player keeps on the patch: 1 off ice, as little as 0.01 on it.
        /// </summary>
        public readonly float Grip;

        /// <summary>Fire: the fire a second a patch burns a player with. Roots: the seconds it holds them. 0 otherwise.</summary>
        public readonly float Strength;

        /// <summary>The vanilla effect whose ground decal the patches are drawn with.</summary>
        public readonly string Effect;

        private TrailSpec(float life, float radius, float spacing, float slow, float grip, float strength, string effect)
        {
            Life = life;
            Radius = radius;
            Spacing = spacing;
            Slow = slow;
            Grip = grip;
            Strength = strength;
            Effect = effect;
        }

        /// <summary>False when `trail life` is 0: the creature lays no trail at all.</summary>
        public bool Lays => Life > 0f;

        /// <summary>
        /// Seconds between two drops at the least, so no creature has more than <see cref="MaxLive"/> patches.
        /// </summary>
        public float Gap => Mathf.Max(MinGap, Life / MaxLive);

        public static TrailSpec Of(BiomeRules r, CreatureTraits t, TrailKind kind) =>
            kind.Ruled ? Ruled(r, t, kind) : Fixed(r, t, kind);

        private static TrailSpec Ruled(BiomeRules r, CreatureTraits t, TrailKind kind)
        {
            Mutation m = kind.Mutation;
            float slow = Mathf.Clamp(Enhance.Magnitude(r, t, m, Fields.Slow), 0f, MaxSlow) / 100f;
            float grip = kind.Slips ? Mathf.Clamp(r.PowerOf(m, Fields.Grip), 1f, 100f) / 100f : 1f;
            float life = Mathf.Max(0f, r.PowerOf(m, Fields.TrailLife));
            float radius = Mathf.Clamp(r.PowerOf(m, Fields.PatchRadius), 0.25f, 8f);
            float spacing = Mathf.Max(0.25f, r.PowerOf(m, Fields.PatchSpacing));
            return new TrailSpec(life, radius, spacing, slow, grip, 0f, r.PrefabOf(m, Fields.TrailEffect));
        }

        // Fire burns as much harder as the creature's blows hit with its stars; a hold never passes MaxHold.
        private static TrailSpec Fixed(BiomeRules r, CreatureTraits t, TrailKind kind)
        {
            float gain = t.OnLargeStar(kind.Mutation) ? r.LargeStarPower : 1f;
            float strength = kind.Feel == TrailFeel.Burn
                ? kind.Strength * r.Star.AttackAt(t.Stars) * gain
                : Mathf.Min(kind.Strength * gain, MaxHold);
            return new TrailSpec(kind.Life, kind.Radius, kind.Spacing, 0f, 1f, strength, kind.DecalEffect);
        }

        /// <summary>
        /// The trail part of a mutation's `elite inspect` line (Frostbound's shares its line with the aura).
        /// </summary>
        public static string Describe(BiomeRules r, CreatureTraits t, Mutation m)
        {
            TrailKind kind = TrailKind.Of(m);
            TrailSpec spec = Of(r, t, kind);
            if (!spec.Lays)
            {
                return $"no {kind.Name} trail (trail life 0)";
            }
            return $"{kind.Name} patch r{spec.Radius:0.0}m every {spec.Spacing:0.0}m walked, lasting {spec.Life:0}s; "
                + $"{Feeling(kind, spec)}; none when tamed; drawn from {spec.Effect}";
        }

        private static string Feeling(TrailKind kind, TrailSpec spec) => kind.Feel switch
        {
            TrailFeel.Burn => $"players in it catch fire, {spec.Strength:0.0} fire a second",
            TrailFeel.Root => $"a player stepping in is held {spec.Strength:0.0}s, then free for {RootFooting.Free:0}s",
            _ => kind.Slips
                ? $"players on it slide (grip {spec.Grip * 100f:0}%) and are {spec.Slow * 100f:0}% slower"
                : $"players in it {spec.Slow * 100f:0}% slower, for {kind.Linger:0.0}s after stepping out",
        };
    }
}
