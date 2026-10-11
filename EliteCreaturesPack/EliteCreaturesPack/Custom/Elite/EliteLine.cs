using System.Collections.Generic;
using EliteCreaturesPack.Custom.Build;

namespace EliteCreaturesPack.Custom.Elite
{
    /// <summary>
    /// One list of a creature's `elite:` block on its way to Elite Creatures Reborn: its values, the field it was written
    /// in, and the report of the definition that wrote it - the creature's own, or the base of its chain that set it last.
    /// Every message about the list, from the checks and from ECR's answers after the build, goes through that report, so
    /// it names the creature, that definition's file and the field's line.
    /// </summary>
    internal sealed class EliteLine<T>
    {
        public EliteLine(IReadOnlyList<T> values, string field, BuildReport report)
        {
            Values = values;
            Field = field;
            Report = report;
        }

        public IReadOnlyList<T> Values { get; }

        /// <summary>The field's path inside the entry, like <c>elite.mutations</c>.</summary>
        public string Field { get; }

        public BuildReport Report { get; }

        /// <summary>The same line with the values that passed a check; null when none did, so nothing is registered for it.</summary>
        public EliteLine<T>? With(List<T> kept) => kept.Count == 0 ? null : new EliteLine<T>(kept, Field, Report);

        /// <summary>A warning at this line (or at <paramref name="field"/>, a path inside it): the creature still loads.</summary>
        public void Warn(string message, string? field = null) => Report.Warn(message, field ?? Field);
    }
}
