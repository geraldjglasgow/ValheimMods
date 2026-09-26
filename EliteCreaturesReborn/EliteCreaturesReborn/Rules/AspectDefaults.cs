using System.Collections.Generic;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// The boss-aspect defaults expressed in code: the baseline a parsed `aspects:` block overlays onto, and the value a
    /// single missing field falls back to. The written rule file carries the same numbers with their comments. Every
    /// number here that did not come with the request is a judgement call and says so in `features/boss-aspects.md`.
    /// </summary>
    internal static class AspectDefaults
    {
        public static AspectRules Build()
        {
            AspectRules rules = new AspectRules { Enabled = true, ShiftHours = 1f };
            rules.Chances[Aspect.None] = 20f; // one fight in five is the boss as the game ships it
            foreach (Aspect aspect in AspectCatalog.InOrder)
            {
                rules.Chances[aspect] = 10f;
                rules.Power[aspect] = new Dictionary<string, float>(FieldsOf(aspect));
            }
            foreach (KeyValuePair<Aspect, float> pair in LootTable)
            {
                rules.Loot[pair.Key] = pair.Value;
            }
            foreach (KeyValuePair<string, string[]> pair in SummonTable)
            {
                rules.Bosses[pair.Key] = new BossAspectRule { Summons = new List<string>(pair.Value) };
            }
            return rules;
        }

        public static float Power(Aspect aspect, string field) =>
            FieldsOf(aspect).TryGetValue(field, out float value) ? value : 0f;

        private static Dictionary<string, float> FieldsOf(Aspect aspect) =>
            PowerTable.TryGetValue(aspect, out Dictionary<string, float> fields) ? fields : new Dictionary<string, float>();

        // Gentlest first. Twin pays 1.0 per boss because both twins drop full loot: the fight already pays double.
        private static readonly Dictionary<Aspect, float> LootTable = new Dictionary<Aspect, float>
        {
            [Aspect.None] = 1f, [Aspect.Twin] = 1f, [Aspect.Shielded] = 1.1f, [Aspect.Enraged] = 1.2f,
            [Aspect.Elementalist] = 1.2f, [Aspect.Mending] = 1.3f, [Aspect.Phantom] = 1.3f,
            [Aspect.Reflective] = 1.4f, [Aspect.Summoner] = 1.5f,
        };

        private static readonly Dictionary<Aspect, Dictionary<string, float>> PowerTable =
            new Dictionary<Aspect, Dictionary<string, float>>
            {
                [Aspect.Reflective] = new Dictionary<string, float> { [Fields.Reflect] = 15f },
                [Aspect.Shielded] = new Dictionary<string, float> { [Fields.ArrowReduction] = 30f },
                [Aspect.Mending] = new Dictionary<string, float> { [Fields.Regen] = 0.3f },
                [Aspect.Summoner] = new Dictionary<string, float>
                    { [Fields.Every] = 33f, [Fields.Count] = 2f, [Fields.Stars] = 2f },
                [Aspect.Elementalist] = new Dictionary<string, float> { [Fields.ElementalBonus] = 20f },
                [Aspect.Enraged] = new Dictionary<string, float> { [Fields.PhysicalBonus] = 20f },
                [Aspect.Twin] = new Dictionary<string, float>
                    { [Fields.LessHealth] = 25f, [Fields.LessDamage] = 25f },
                [Aspect.Phantom] = new Dictionary<string, float>
                    { [Fields.Copies] = 4f, [Fields.Health] = 100f, [Fields.LessDamage] = 50f },
            };

        // Each vanilla boss calls creatures of its own biome, by prefab name (checked against the game's prefabs).
        private static readonly Dictionary<string, string[]> SummonTable = new Dictionary<string, string[]>
        {
            ["Eikthyr"] = new[] { "Boar", "Neck" },
            ["gd_king"] = new[] { "Greydwarf_Elite", "Greydwarf_Shaman" },
            ["Bonemass"] = new[] { "Draugr_Elite", "BlobElite" },
            ["Dragon"] = new[] { "Hatchling" },
            ["GoblinKing"] = new[] { "GoblinBrute", "GoblinShaman" },
            ["SeekerQueen"] = new[] { "SeekerBrute", "Seeker" },
            ["Fader"] = new[] { "Charred_Melee", "Charred_Archer" },
        };
    }
}
