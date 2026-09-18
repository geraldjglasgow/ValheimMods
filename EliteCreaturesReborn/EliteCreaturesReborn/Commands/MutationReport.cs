using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// One line of resolved numbers per mutation for <c>elite inspect</c>: what a creature actually ended up with after
    /// the additive star-power model and any large-star enhancement, not the configured values. Each field is read
    /// through the very same <see cref="Enhance"/> call the gameplay uses - <c>Stat</c> for a <c>1+bonus</c> multiplier,
    /// <c>Magnitude</c> for a raw amount, <c>PowerOf</c> for a cost or non-enhanced field - so the report can never drift
    /// from the behaviour. The gap between what a rule file says and what a creature became is where bugs live.
    /// </summary>
    public static class MutationReport
    {
        public static string Line(BiomeRules r, CreatureTraits t, Mutation m)
        {
            string tag = t.OnLargeStar(m) ? "  [large-star enhanced]" : "";
            return m switch
            {
                Mutation.Mad => $"Mad: move x{Enhance.Stat(r, t, m, Fields.Move):0.00}, attack speed x{Enhance.Stat(r, t, m, Fields.AttackSpeed):0.00}, health x{r.PowerOf(m, Fields.Health):0.00}{tag}",
                Mutation.Bloated => $"Bloated: health x{Enhance.Stat(r, t, m, Fields.Health):0.00}, blast {Enhance.Magnitude(r, t, m, Fields.Damage) * (1 + t.Stars):0} dmg r{Enhance.Magnitude(r, t, m, Fields.Radius):0.0}, delay {r.PowerOf(m, Fields.Delay):0.0}s{tag}",
                Mutation.Cloaked => $"Cloaked: reveal {Enhance.Magnitude(r, t, m, Fields.RevealDistance):0.0}m, fade {r.PowerOf(m, Fields.FadeTime):0.0}s, margin {r.PowerOf(m, Fields.FadeMargin):0.0}m{tag}",
                Mutation.Splintering => $"Splintering: damage x{r.PowerOf(m, Fields.Damage):0.00} (split table keys off stars, never enhanced)",
                Mutation.Leeching => $"Leeching: regen {r.PowerOf(m, Fields.Regen):0.0}%/s, never enhanced, capped {r.PowerOf(m, Fields.RegenCap):0} hp/s, paused {r.PowerOf(m, Fields.CombatCooldown):0}s after a hit; lifesteal {Enhance.Magnitude(r, t, m, Fields.Lifesteal):0.0}%{tag}",
                Mutation.Warding => $"Warding: reflect {Enhance.Magnitude(r, t, m, Fields.Reflect):0.0}%, knockback {Enhance.Magnitude(r, t, m, Fields.Knockback):0.0}{tag}",
                Mutation.Plated => $"Plated: {DamageMath.PlatedPercent(r, t):0}% damage cut at full hp (hard cap {r.PowerOf(m, Fields.MaxReduction):0}%), damage +{Enhance.Magnitude(r, t, m, Fields.Damage):0}% at empty{tag}",
                Mutation.Miasmic => $"Miasmic: poison str {Enhance.Magnitude(r, t, m, Fields.CloudDamage):0.0}, {Enhance.Magnitude(r, t, m, Fields.CloudsPerSecond):0.00} clouds/s, life {r.PowerOf(m, Fields.CloudLife):0.0}s, r{r.PowerOf(m, Fields.CloudRadius):0.0}{tag}",
                Mutation.Devouring => $"Devouring: absorb {Enhance.Magnitude(r, t, m, Fields.AbsorbHealth):0}% hp / {Enhance.Magnitude(r, t, m, Fields.AbsorbDamage):0}% dmg, slow {r.PowerOf(m, Fields.SlowPer100Health):0.0}%/100hp, hunts at {r.PowerOf(m, Fields.PlayerThreshold):0.00}x player hp, cooldown {r.PowerOf(m, Fields.DevourCooldown):0}s{tag}",
                Mutation.Thieving => $"Thieving: max items {PouchStore.ResolvedMaxItems(r, t)} (hard cap {PouchStore.HardCap}){tag}",
                _ => MutationCatalog.Word(m),
            };
        }
    }
}
