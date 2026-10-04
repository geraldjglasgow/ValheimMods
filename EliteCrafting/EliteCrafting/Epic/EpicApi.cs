using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using EliteCrafting.Core;

namespace EliteCrafting.Epic
{
    /// <summary>
    /// Epic Loot's published API (<c>EpicLoot.API</c>), found by reflection on first use: EliteCrafting neither references
    /// nor needs Epic Loot. While Epic Loot is loaded (<see cref="Installed"/>, the user's decision 2026-10-03) the runes
    /// work on Epic Loot's own magic items and EliteCrafting drops no magic gear of its own. A missing method or a call
    /// that throws turns the link off with one warning; the runes then refuse rather than half-work.
    /// </summary>
    internal static class EpicApi
    {
        public const string Guid = "randyknapp.mods.epicloot";

        /// <summary>The API methods the runes use, by name and parameter count.</summary>
        private static readonly (string Name, int Arity)[] Needed =
        {
            ("IsUnidentified", 1), ("CanBeMagicItem", 1), ("GetMagicItemJson", 1), ("RollMagicItemJson", 3),
            ("ApplyMagicItemJson", 2), ("GetAvailableEffectTypes", 3), ("GetMagicItemEffectDefinition", 1),
        };

        private static readonly Dictionary<string, MethodInfo> Methods = new Dictionary<string, MethodInfo>();
        private static bool looked;
        private static bool ready;

        /// <summary>Epic Loot is loaded: EliteCrafting's own magic gear stays off and the runes work on Epic Loot's magic.</summary>
        public static bool Installed => Chainloader.PluginInfos.TryGetValue(Guid, out PluginInfo info) && info.Instance != null;

        /// <summary>Installed, and every API method the runes use was found.</summary>
        public static bool Ready
        {
            get
            {
                Bind();
                return ready;
            }
        }

        /// <summary>Epic Loot's own assembly, for the few calls outside its API (<see cref="EpicExtras"/>).</summary>
        public static Assembly? Assembly { get; private set; }

        public static bool IsUnidentified(ItemDrop.ItemData item) => Call("IsUnidentified", item) is bool b && b;

        public static bool CanBeMagic(ItemDrop.ItemData item) => Call("CanBeMagicItem", item) is bool b && b;

        /// <summary>The item's magic as Epic Loot's JSON; null for an item that is not magic.</summary>
        public static string? MagicJson(ItemDrop.ItemData item) => Call("GetMagicItemJson", item) as string;

        /// <summary>A fresh magic item of the rarity rolled for this item, as JSON; empty when Epic Loot rolls none.</summary>
        public static string RollJson(int rarity, ItemDrop.ItemData item) => Call("RollMagicItemJson", rarity, item, 0f) as string ?? "";

        /// <summary>Writes the magic onto the item through Epic Loot (it refreshes an equipped item's effects itself).</summary>
        public static bool Apply(ItemDrop.ItemData item, string json) => Call("ApplyMagicItemJson", item, json) is bool b && b;

        /// <summary>The effect types Epic Loot allows on this item as it would stand with this magic.</summary>
        public static List<string> AvailableEffects(ItemDrop.ItemData item, string json, int rarity) =>
            Call("GetAvailableEffectTypes", item, json, rarity) as List<string> ?? new List<string>();

        /// <summary>An effect's definition (weight, values per rarity, words) as JSON; empty when unknown.</summary>
        public static string EffectDefinition(string type) => Call("GetMagicItemEffectDefinition", type) as string ?? "";

        private static object? Call(string name, params object?[] args)
        {
            if (!Ready)
            {
                return null;
            }
            try
            {
                return Methods[name].Invoke(null, args);
            }
            catch (Exception e)
            {
                ready = false;
                Log.Warn($"Epic Loot's {name} failed, runes no longer work on Epic Loot items: {e.InnerException?.Message ?? e.Message}");
                return null;
            }
        }

        private static void Bind()
        {
            if (looked)
            {
                return;
            }
            looked = true;
            if (!Chainloader.PluginInfos.TryGetValue(Guid, out PluginInfo info) || info.Instance == null)
            {
                return;
            }
            Assembly = info.Instance.GetType().Assembly;
            ready = FindAll(Assembly.GetType("EpicLoot.API"));
            if (ready)
            {
                Log.Info("Epic Loot found: runes work on Epic Loot's magic items; EliteCrafting drops no magic gear of its own");
            }
        }

        private static bool FindAll(Type? api)
        {
            foreach ((string name, int arity) in Needed)
            {
                MethodInfo? method = Find(api, name, arity);
                if (method == null)
                {
                    Log.Warn($"Epic Loot is installed but its API has no {name}; runes refuse Epic Loot items");
                    return false;
                }
                Methods[name] = method;
            }
            return true;
        }

        private static MethodInfo? Find(Type? api, string name, int arity)
        {
            if (api == null)
            {
                return null;
            }
            foreach (MethodInfo method in api.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name == name && method.GetParameters().Length == arity)
                {
                    return method;
                }
            }
            return null;
        }
    }
}
