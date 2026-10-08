using System;
using System.Linq;
using EliteCrafting.Core;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// The code-side list of rune prefabs (prefabs.md): the five runes, the Dvergr Chisel and the eleven gems
    /// (sockets.md), whose prefab is <c>ECF_</c> + PascalCase id. Prefabs come from code only and exist on every peer
    /// whatever the YAML says; the YAML binds definitions to them (and can disable one, never add one).
    /// </summary>
    public static class StoneCatalog
    {
        public const string PrefabPrefix = "ECF_";

        /// <summary>The five runes, in the order a player meets them: Normal to Magic, Magic rerolled, Magic to Rare,
        /// back to Normal, sealed. The Shaping and Consecrated Runes were removed on 2026-10-07 (user decision): Magic
        /// holds two inscriptions, Rare three, the Serpent's gamble a fourth. The Rune Table holds these only.</summary>
        public static readonly string[] BuiltInIds =
        {
            "awakening", "recasting", "ascension", "cleansing", "serpent",
        };

        /// <summary>The Dvergr Chisel: one socket on an item that has none.</summary>
        public const string ChiselId = "dvergr_chisel";

        /// <summary>The gems (sockets.md section 3); what each gives per base is <c>Sockets.GemCatalog</c>.</summary>
        public static readonly string[] GemIds =
        {
            "gem_surtr", "gem_ymir", "gem_thor", "gem_nidhogg", "gem_hel", "gem_tyr", "gem_freyja", "gem_odin",
            "gem_skadi", "gem_heimdall", "gem_sleipnir",
        };

        /// <summary>Every built-in id: the runes, then the chisel and the gems.</summary>
        public static readonly string[] AllIds = BuiltInIds.Concat(new[] { ChiselId }).Concat(GemIds).ToArray();

        public static bool IsBuiltIn(string id) => Array.IndexOf(AllIds, id) >= 0;

        /// <summary>One of the five runes (not the chisel or a gem).</summary>
        public static bool IsRune(string id) => Array.IndexOf(BuiltInIds, id) >= 0;

        public static bool IsGem(string id) => Array.IndexOf(GemIds, id) >= 0;

        /// <summary>The chisel or a gem: the stones of the socket feature.</summary>
        public static bool IsSocketStone(string id) => id == ChiselId || IsGem(id);

        /// <summary><c>ascension</c> → <c>ECF_Ascension</c>.</summary>
        public static string PrefabFor(string builtInId) => PrefabPrefix + Ids.Pascal(builtInId);
    }
}
