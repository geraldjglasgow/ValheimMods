using System;
using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Rules;
using UnityEngine;

namespace EliteCrafting.Items
{
    /// <summary>
    /// The tint of every rune (prefabs.md section 4b, DECISIONS.md PRF-3), resolved from the running rules: the
    /// entry's own <c>tint</c>, else for a promote rune the color of the rarity it produces (so a retheme of the
    /// rarities rethemes the runes that make them), else the default table below. Precomputed on every rules change;
    /// lookups do not allocate. Computed on every peer (cheap, one code path); only clients draw with it.
    /// </summary>
    internal static class StoneTints
    {
        // Judgement calls: Recasting is violet, Cleansing
        // pale silver.
        private static readonly Dictionary<string, string> Defaults = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["awakening"] = "#1EFF00", ["recasting"] = "#9B6BD6", ["ascension"] = "#0070DD",
            ["consecrated"] = "#E6C35C", ["cleansing"] = "#D8E4EE", ["serpent"] = "#3F7F2A",
            // The chisel brass; each gem the colour of what it gives (sockets.md section 3).
            ["dvergr_chisel"] = "#C9A045", ["gem_surtr"] = "#FF5A1F", ["gem_ymir"] = "#8FD8FF", ["gem_thor"] = "#5C8DFF",
            ["gem_nidhogg"] = "#7FD13B", ["gem_hel"] = "#B9C4D6", ["gem_tyr"] = "#C0392B", ["gem_freyja"] = "#FF7FAE",
            ["gem_odin"] = "#A06BFF", ["gem_skadi"] = "#E8F4FF", ["gem_heimdall"] = "#FFD84A", ["gem_sleipnir"] = "#3FE0C5",
        };

        private static Dictionary<string, Color?> _byId = new Dictionary<string, Color?>(StringComparer.Ordinal);
        private static Dictionary<string, Color?> _byPrefab = new Dictionary<string, Color?>(StringComparer.Ordinal);

        public static bool TryById(string? stoneId, out Color tint) => TryIn(_byId, stoneId, out tint);

        public static bool TryByPrefab(string? prefab, out Color tint) => TryIn(_byPrefab, prefab, out tint);

        /// <summary>Recomputes both tables from the rules (every defined rune plus every built-in id).</summary>
        public static void Rebuild(EconomyRules economy)
        {
            Dictionary<string, Color?> byId = new Dictionary<string, Color?>(StringComparer.Ordinal);
            Dictionary<string, Color?> byPrefab = new Dictionary<string, Color?>(StringComparer.Ordinal);
            foreach (StoneDef def in economy.Stones)
            {
                Color? tint = Resolve(def.Id, def, economy);
                byId[def.Id] = tint;
                byPrefab[def.Prefab] = tint;
            }
            foreach (string id in StoneCatalog.AllIds)
            {
                if (!byId.ContainsKey(id))
                {
                    Color? tint = Resolve(id, null, economy);
                    byId[id] = tint;
                    byPrefab[StoneCatalog.PrefabFor(id)] = tint;
                }
            }
            _byId = byId;
            _byPrefab = byPrefab;
        }

        private static Color? Resolve(string id, StoneDef? def, EconomyRules economy)
        {
            if (def?.Tint != null && Colors.TryParse(def.Tint, out Color32 own))
            {
                return own;
            }
            Color? produced = def != null && def.Verb == StoneVerb.Promote ? ProducedRarityColor(def, economy) : null;
            if (produced.HasValue)
            {
                return produced;
            }
            return Defaults.TryGetValue(id, out string hex) && Colors.TryParse(hex, out Color32 c) ? c : (Color?)null;
        }

        // The rarity a promote rune makes: one step above the highest rarity it accepts.
        private static Color? ProducedRarityColor(StoneDef def, EconomyRules economy)
        {
            RarityDef? highest = null;
            foreach (string id in def.AppliesTo)
            {
                RarityDef? rarity = economy.Rarity(id);
                if (rarity != null && (highest == null || rarity.Index > highest.Index))
                {
                    highest = rarity;
                }
            }
            RarityDef? next = highest != null ? economy.Next(highest) : null;
            return next != null ? next.Color32 : (Color?)null;
        }

        private static bool TryIn(Dictionary<string, Color?> table, string? key, out Color tint)
        {
            if (key != null && table.TryGetValue(key, out Color? found) && found.HasValue)
            {
                tint = found.Value;
                return true;
            }
            tint = Color.white;
            return false;
        }
    }
}
