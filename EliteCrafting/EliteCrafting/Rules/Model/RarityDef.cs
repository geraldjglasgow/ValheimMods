using UnityEngine;

namespace EliteCrafting.Rules
{
    /// <summary>One rung of the ladder (rarity.md, economy-yaml.md section 2). Ladder order = <see cref="Index"/>.</summary>
    public sealed class RarityDef
    {
        public string Id { get; internal set; } = "";

        /// <summary>0 for the base rarity (Common).</summary>
        public int Index { get; internal set; }

        /// <summary>A <c>$key</c> (default <c>$ecf_rarity_&lt;id&gt;</c>) or literal text.</summary>
        public string Name { get; internal set; } = "";

        /// <summary><c>#RRGGBB</c>, the single source for every rarity-colored surface.</summary>
        public string Color { get; internal set; } = "#FFFFFF";

        public Color32 Color32 { get; internal set; } = new Color32(255, 255, 255, 255);

        /// <summary>Ground glow; always false on the base rarity.</summary>
        public bool Glow { get; internal set; }

        public int MinAffixes { get; internal set; }
        public int MaxAffixes { get; internal set; }

        /// <summary>How many of the count come from the Mythic-only pool.</summary>
        public int MythicAffixes { get; internal set; }

        /// <summary>Multiplier on this rarity's drop weights; 0 = never drops.</summary>
        public float DropWeight { get; internal set; } = 1f;

        public bool IsBase => Index == 0;
    }

    /// <summary>The <c>rolling:</c> section (rarity.md section 4).</summary>
    public sealed class RollingSettings
    {
        public int TierWindow { get; internal set; } = 3;
        public int PromoteAddsAtLeast { get; internal set; } = 1;

        /// <summary>rarity id → (affix count → weight). A rarity absent here draws its count uniformly.</summary>
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyDictionary<int, float>> CountWeights
        { get; internal set; } = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IReadOnlyDictionary<int, float>>();
    }

    /// <summary>The <c>sigils:</c> section.</summary>
    public sealed class SigilSettings
    {
        public bool ConsumeOnUnsteered { get; internal set; }
    }
}
