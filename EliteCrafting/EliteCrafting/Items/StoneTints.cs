using System;
using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Rules;
using UnityEngine;

namespace EliteCrafting.Items
{
    /// <summary>
    /// The tint of every stone (prefabs.md section 4b, DECISIONS.md PRF-3), resolved from the running rules:
    /// the entry's own <c>tint</c>, else for a promote stone the color of the rarity it produces (so a retheme of the
    /// rarities rethemes the stones that make them), else the default table below, else none (the base's own look:
    /// Honing, Tempering, an unbound reserved prefab). Precomputed on every rules change; lookups do not allocate.
    /// Computed on every peer (cheap, one code path); only clients draw with it.
    /// </summary>
    internal static class StoneTints
    {
        // Judgement calls from prefabs.md section 4b. Keyed by stone id, or by family for graded stones.
        private static readonly Dictionary<string, string> Defaults = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["awakening"] = "#1EFF00", ["ascension"] = "#0070DD", ["exaltation"] = "#A335EE",
            ["transcendence"] = "#FF8000", ["apotheosis"] = "#E6262E",
            ["growth"] = "#2EC4B6", ["turmoil"] = "#FFB000", ["upheaval"] = "#D1307A", ["perfection"] = "#FFD700",
            ["severing"] = "#9FB4C7", ["unmaking"] = "#5A5A5A",
            ["serpent"] = "#3F7F2A", ["binding"] = "#C0C0C0", ["chance"] = "#FF69B4", ["reflection"] = "#E0FFFF",
            ["sigil_war"] = "#C0392B", ["sigil_warding"] = "#2E86DE", ["sigil_fortune"] = "#27AE60",
            ["sigil_preservation"] = "#8E44AD", ["sigil_culling"] = "#7F8C8D",
            // essences.md section 9: one tint per family, both grades
            ["essence_storm"] = "#FFE45C", ["essence_grove"] = "#6B8E23", ["essence_venom"] = "#9ACD32",
            ["essence_frost"] = "#9FE7FF", ["essence_battle"] = "#B0413E", ["essence_seidr"] = "#6A5ACD",
            ["essence_ember"] = "#FF5A1F", ["essence_tide"] = "#1F8FA8",
        };

        private static Dictionary<string, Color?> _byId = new Dictionary<string, Color?>(StringComparer.Ordinal);
        private static Dictionary<string, Color?> _byPrefab = new Dictionary<string, Color?>(StringComparer.Ordinal);

        public static bool TryById(string? stoneId, out Color tint) => TryIn(_byId, stoneId, out tint);

        public static bool TryByPrefab(string? prefab, out Color tint) => TryIn(_byPrefab, prefab, out tint);

        /// <summary>Recomputes both tables from the rules (every defined stone plus every built-in id).</summary>
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
            foreach (string id in StoneCatalog.BuiltInIds)
            {
                if (!byId.ContainsKey(id))
                {
                    Color? tint = Resolve(id, null, economy);
                    byId[id] = tint;
                    byPrefab[StoneCatalog.PrefabFor(id)] = tint;
                }
            }
            AddShards(byId, byPrefab, economy);
            _byId = byId;
            _byPrefab = byPrefab;
        }

        // A shard is tinted like the stone it fuses into (SAL-15), unless its fragment entry names a tint.
        private static void AddShards(Dictionary<string, Color?> byId, Dictionary<string, Color?> byPrefab, EconomyRules economy)
        {
            foreach (string id in StoneCatalog.ShardIds)
            {
                FragmentDef? def = economy.Salvage.Fragment(id);
                Color? tint = def?.Tint != null && Colors.TryParse(def.Tint, out Color32 own) ? own : (Color?)null;
                string stone = def?.Stone ?? id.Substring("shard_".Length);
                if (!tint.HasValue && byId.TryGetValue(stone, out Color? stoneTint))
                {
                    tint = stoneTint;
                }
                byId[id] = tint;
                byPrefab[StoneCatalog.PrefabFor(id)] = tint;
            }
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
            return Defaults.TryGetValue(Family(id), out string hex) && Colors.TryParse(hex, out Color32 c) ? c : (Color?)null;
        }

        // The rarity a promote stone makes: one step above the highest rarity it accepts.
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

        /// <summary><c>growth_lesser</c> → <c>growth</c>; other ids unchanged.</summary>
        public static string Family(string id)
        {
            if (id.EndsWith("_lesser", StringComparison.Ordinal)) return id.Substring(0, id.Length - 7);
            if (id.EndsWith("_greater", StringComparison.Ordinal)) return id.Substring(0, id.Length - 8);
            return id;
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
