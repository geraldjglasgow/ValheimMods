using System;
using EliteCrafting.Core;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// The code-side list of rune prefabs (prefabs.md): the seven runes, whose prefab is <c>ECF_</c> + PascalCase id.
    /// Prefabs come from code only and exist on every peer whatever the YAML says; the YAML binds definitions to them
    /// (and can disable a rune, never add one).
    /// </summary>
    public static class StoneCatalog
    {
        public const string PrefabPrefix = "ECF_";

        /// <summary>In the order a player meets them: Normal to Magic, more on Magic, Magic rerolled, Magic to Rare,
        /// more on Rare, back to Normal, sealed.</summary>
        public static readonly string[] BuiltInIds =
        {
            "awakening", "shaping", "recasting", "ascension", "consecrated", "cleansing", "serpent",
        };

        public static bool IsBuiltIn(string id) => Array.IndexOf(BuiltInIds, id) >= 0;

        /// <summary><c>consecrated</c> → <c>ECF_Consecrated</c>.</summary>
        public static string PrefabFor(string builtInId) => PrefabPrefix + Ids.Pascal(builtInId);
    }
}
