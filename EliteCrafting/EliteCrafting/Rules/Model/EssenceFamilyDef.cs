using System;
using System.Collections.Generic;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// One <c>essence_families</c> entry (essences.md section 11): the affix ids an <c>imbue</c> stone's guaranteed
    /// draw is restricted to. Members are ids of the affix family; unknown, disabled or Mythic-only ones are skipped at
    /// roll time (ESS-12), never an error here, because the two YAML families load separately. Immutable.
    /// </summary>
    public sealed class EssenceFamilyDef
    {
        public string Id { get; internal set; } = "";

        /// <summary>A <c>$key</c> (default <c>$ecf_family_&lt;id&gt;</c>) or literal text.</summary>
        public string Name { get; internal set; } = "";

        public IReadOnlyList<string> Affixes { get; internal set; } = Array.Empty<string>();
    }
}
