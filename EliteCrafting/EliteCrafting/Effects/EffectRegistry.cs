using System;
using System.Collections.Generic;
using EliteCrafting.Core;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// Effect id → registration record. The table is filled from <see cref="EffectCatalog"/> the first time anything
    /// touches the registry, so it is complete before the affix YAML is validated (rules load in plugin Awake).
    /// <para>
    /// To add an effect: add a line to <see cref="EffectCatalog"/> (the effects area owns that file) and implement it
    /// with a Harmony patch or an aggregate channel. <see cref="Register"/> after the rules have loaded throws: an id
    /// the YAML could not see at validation time would make the same file valid or invalid depending on timing.
    /// </para>
    /// </summary>
    public static class EffectRegistry
    {
        private static readonly Dictionary<string, EffectDef> Table = new Dictionary<string, EffectDef>(StringComparer.Ordinal);
        private static bool _frozen;

        static EffectRegistry()
        {
            EffectCatalog.RegisterAll();
        }

        /// <summary>Every registered effect id, for validation and listings.</summary>
        public static IEnumerable<string> AllIds => Table.Keys;

        public static IEnumerable<EffectDef> All => Table.Values;

        public static int Count => Table.Count;

        public static bool IsRegistered(string? id) => id != null && Table.ContainsKey(id);

        public static bool TryGet(string? id, out EffectDef def)
        {
            def = null!;
            return id != null && Table.TryGetValue(id, out def);
        }

        public static EffectDef? Get(string? id) => id != null && Table.TryGetValue(id, out EffectDef def) ? def : null;

        /// <summary>Adds an effect. Only from <see cref="EffectCatalog"/>, before the rules first load.</summary>
        public static void Register(EffectDef def)
        {
            if (_frozen)
            {
                throw new InvalidOperationException($"effect '{def.Id}' registered after the rules loaded; add it to EffectCatalog");
            }
            if (!Core.Ids.IsValid(def.Id) || Table.ContainsKey(def.Id))
            {
                throw new ArgumentException($"effect id '{def.Id}' is invalid or registered twice");
            }
            Table.Add(def.Id, def);
        }

        /// <summary>Called by the rules loader; later registrations throw.</summary>
        internal static void Freeze() => _frozen = true;
    }
}
