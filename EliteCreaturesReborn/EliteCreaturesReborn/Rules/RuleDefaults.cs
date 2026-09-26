using System.Collections.Generic;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// The specification defaults expressed in code, used two ways: as the baseline a parsed file overlays onto, and
    /// as the value a single missing field falls back to. The written file (see <see cref="RuleText"/>) carries the
    /// same numbers with their comments; these are the safety net when a value is edited away or the file cannot load.
    /// </summary>
    internal static class RuleDefaults
    {
        /// <summary>A full defaults block: every field populated, ready for a file to overlay its own values onto.</summary>
        public static BiomeRules Baseline()
        {
            BiomeRules rules = new BiomeRules
            {
                // `star chances` has no global default - it is per-biome only. This is the fallback an unlisted biome
                // uses (the Meadows row); the parser overwrites it from the file's own Meadows entry when there is one.
                StarChances = new[] { 73f, 10f, 10f, 5f, 1f, 1f },
                Star = BaselineStarPower(),
                LargeStarPower = 1f,
                MutationChance = new[] { 2.5f, 3.5f, 5f, 6f, 7.5f, 10f },
            };
            rules.MutationChances[Mutation.Devouring] = new[] { 0.6f, 0.9f, 1.2f, 1.5f, 1.8f, 2.4f };
            foreach (Mutation mutation in MutationCatalog.InOrder)
            {
                rules.MutationPower[mutation] = new Dictionary<string, float>(Power(mutation));
            }
            return rules;
        }

        /// <summary>A whole rule set of nothing but defaults, for the rare case the file cannot be written or read.</summary>
        public static RuleSet BuildDefault() =>
            new RuleSet { Defaults = Baseline(), Boss = BaselineBoss(), Respawn = new RespawnRules() };

        /// <summary>
        /// The boss table's defaults. Conservative: nine bosses in ten stay plain, because a boss is a set-piece a
        /// group has prepared for and a surprise five-star Bonemass is a wasted evening. Health and damage climb
        /// harder per star than a creature's, size climbs less - a boss already fills its arena and one scaled much
        /// past that clips through the terrain it stands on.
        /// </summary>
        public static BossRules BaselineBoss()
        {
            return new BossRules
            {
                Enabled = true,
                StarChances = new[] { 90f, 6f, 3f, 1f },
                Star = new StarPower
                {
                    Growth = new[] { 0f, 0.05f, 0.10f, 0.15f, 0.20f, 0.20f },
                    Hp = new[] { 1f, 1.5f, 2.25f, 3.4f, 5.1f, 7.6f },
                    Attack = new[] { 1f, 1.25f, 1.55f, 1.9f, 2.3f, 2.75f },
                    SwingSpeed = new[] { 1f },
                    Speed = new[] { 1f },
                    Drops = new[] { 1f, 1.5f, 2f, 2.5f, 3f, 3.5f },
                },
                Aspects = AspectDefaults.Build(),
            };
        }

        public static float Power(Mutation mutation, string field)
        {
            return Power(mutation).TryGetValue(field, out float value) ? value : 1f;
        }

        /// <summary>The default vanilla effect prefab a mutation's named field falls back to when the file omits it.</summary>
        public static string Prefab(Mutation mutation, string field)
        {
            return PrefabTable.TryGetValue(mutation, out Dictionary<string, string> fields)
                && fields.TryGetValue(field, out string value) ? value : "";
        }

        private static StarPower BaselineStarPower()
        {
            return new StarPower
            {
                Growth = new[] { 0.06f, 0.10f, 0.15f, 0.20f, 0.25f, 0.30f },
                Hp = new[] { 1f, 1.4f, 1.95f, 2.6f, 3.3f, 4.0f },
                Attack = new[] { 1f, 1.2f, 1.45f, 1.75f, 2.1f, 2.5f },
                SwingSpeed = new[] { 1f, 1.02f, 1.05f, 1.08f, 1.12f, 1.16f },
                Speed = new[] { 1f, 1f, 1.03f, 1.06f, 1.1f, 1.15f },
                Drops = new[] { 1f, 1f, 1.5f, 2f, 2.5f, 3f },
            };
        }

        private static readonly Dictionary<Mutation, Dictionary<string, float>> Table = BuildTable();

        // The vanilla effect prefabs each visible mutation clones. These are best-effort names; the resolver
        // (EffectResolver) falls back to the nearest particle effect by keyword if a name is missing, so a wrong
        // string self-heals rather than leaving an invisible hazard. Every one is a rule-file field a server can edit.
        private static readonly Dictionary<Mutation, Dictionary<string, string>> PrefabTable =
            new Dictionary<Mutation, Dictionary<string, string>>
            {
                [Mutation.Bloated] = new Dictionary<string, string>
                    { [Fields.BlastEffect] = "fx_barrel_destroyed", [Fields.WarningEffect] = "fx_Smoke" },
                [Mutation.Miasmic] = new Dictionary<string, string>
                    { [Fields.CloudEffect] = "vfx_blob_death", [Fields.BodyEffect] = "vfx_blob_death" },
            };

        private static Dictionary<string, float> Power(Mutation mutation)
        {
            return Table.TryGetValue(mutation, out Dictionary<string, float> fields)
                ? fields : new Dictionary<string, float>();
        }

        private static Dictionary<Mutation, Dictionary<string, float>> BuildTable()
        {
            return new Dictionary<Mutation, Dictionary<string, float>>
            {
                [Mutation.Mad] = new Dictionary<string, float>
                    { [Fields.Move] = 1.6f, [Fields.AttackSpeed] = 1.5f, [Fields.Health] = 0.5f },
                [Mutation.Bloated] = new Dictionary<string, float>
                    { [Fields.Health] = 2.0f, [Fields.Delay] = 1.0f, [Fields.Damage] = 40f, [Fields.Radius] = 4f },
                [Mutation.Cloaked] = new Dictionary<string, float>
                    { [Fields.RevealDistance] = 6f, [Fields.FadeTime] = 0.5f, [Fields.FadeMargin] = 1f },
                [Mutation.Splintering] = new Dictionary<string, float>
                    { [Fields.Damage] = 0.6f, [Fields.MaxGenerations] = 0f, [Fields.MaxDescendants] = 0f },
                [Mutation.Leeching] = new Dictionary<string, float>
                    { [Fields.Regen] = 0.5f, [Fields.Lifesteal] = 10f, [Fields.RegenCap] = 20f,
                      [Fields.CombatCooldown] = 5f },
                [Mutation.Warding] = new Dictionary<string, float> { [Fields.Reflect] = 30f, [Fields.Knockback] = 4f },
                [Mutation.Plated] = new Dictionary<string, float>
                    { [Fields.Armour] = 40f, [Fields.Damage] = 60f, [Fields.MaxReduction] = 55f },
                [Mutation.Miasmic] = new Dictionary<string, float>
                    { [Fields.CloudLife] = 6f, [Fields.CloudDamage] = 5f, [Fields.CloudsPerSecond] = 1f,
                      [Fields.CloudRadius] = 4f },
                [Mutation.Devouring] = new Dictionary<string, float>
                    { [Fields.Move] = 1f, [Fields.AbsorbHealth] = 50f, [Fields.AbsorbDamage] = 25f,
                      [Fields.SlowPer100Health] = 2f, [Fields.PlayerThreshold] = 0.333f,
                      [Fields.DevourCooldown] = 60f },
                [Mutation.Thieving] = new Dictionary<string, float> { [Fields.MaxItems] = 1f },
            };
        }
    }
}
