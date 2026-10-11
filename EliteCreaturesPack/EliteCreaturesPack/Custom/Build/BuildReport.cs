using EliteCreaturesPack.Core;
using EliteCreaturesPack.Custom.Definitions;

namespace EliteCreaturesPack.Custom.Build
{
    /// <summary>
    /// Where a step reports what is wrong with a definition. Every message names the creature, the file and the line of
    /// the field (the nearest value read on the field's path, or the entry's line), and, when the definition being
    /// applied is one of its bases', that base: <c>Custom creature 'ECP_X' (from its base 'ECP_Y') (Creatures.yml, line
    /// 40): attacks[0].projectile: unknown projectile 'Foo'</c>.
    /// <list type="bullet">
    /// <item><see cref="Warn"/>: logged; the creature is still built.</item>
    /// <item><see cref="Fail"/>: logged; the creature is left out (its shell destroyed, its creatures in the world kept,
    /// hidden), and so is every creature that needs it. The remaining steps of a failed creature are skipped.</item>
    /// </list>
    /// </summary>
    public sealed class BuildReport
    {
        private readonly ShellRecord record;
        private readonly CreatureDefinition definition;

        internal BuildReport(ShellRecord record, CreatureDefinition definition)
        {
            this.record = record;
            this.definition = definition;
        }

        /// <summary>Whether the creature has failed (in this pass or an earlier one).</summary>
        public bool Failed => record.Failed;

        /// <param name="field">The field's path inside the entry, like <c>drops.items[1].item</c>, or null for the whole entry.</param>
        public void Warn(string message, string? field = null) => Log.Warn(Format(message, field));

        /// <param name="field">The field's path inside the entry, like <c>attacks[0].from</c>, or null for the whole entry.</param>
        public void Fail(string message, string? field = null)
        {
            record.Failed = true;
            Log.Error(Format(message, field) + " The creature is left out.");
        }

        /// <summary>The start of a message about a definition: <c>Custom creature 'X' (file, line n)</c>.</summary>
        public static string Describe(CreatureDefinition definition, string? field = null) =>
            $"Custom creature '{definition.Name}' ({definition.File}, line {definition.LineOf(field)})";

        private string Format(string message, string? field)
        {
            CreatureDefinition creature = record.Chain.Creature;
            string via = ReferenceEquals(definition, creature) ? "" : $" (from its base '{definition.Name}')";
            string what = string.IsNullOrEmpty(field) ? "" : field + ": ";
            return $"Custom creature '{creature.Name}'{via} ({definition.File}, line {definition.LineOf(field)}): {what}{message}";
        }
    }
}
