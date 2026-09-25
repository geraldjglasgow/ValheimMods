using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Items;
using EliteCrafting.Rules;
using UnityEngine;

namespace EliteCrafting.Commands
{
    /// <summary>
    /// <c>ecraft give &lt;stone_id&gt;|&lt;shard_id&gt;|all [count]</c> (console-commands.md section 3): stones, essences
    /// and salvage shards into the caller's own
    /// inventory, the rest dropped at their feet. <c>count</c> 1-9999, default 1. <c>all</c> gives <c>count</c> of every
    /// enabled stone. A single id may name a disabled stone or a built-in stone missing from the YAML (its prefab is
    /// still registered, RC-3); the reply says so. The prefab comes from the Items area's registry. Local player only.
    /// </summary>
    internal static class GiveCommand
    {
        public const string Grammar = "ecraft give <stone_id>|<shard_id>|all [count]";

        public static void Run(CommandCall call)
        {
            Player? player = Player.m_localPlayer;
            string id = call.Lower(0);
            if (player == null || id.Length == 0)
            {
                call.Fail(player == null ? "needs a player in the world." : "which stone?", Grammar);
                return;
            }
            if (!Counts.TryParse(call, 1, 1, 9999, 1, out int count, Grammar))
            {
                return;
            }
            if (id == "all")
            {
                GiveAll(call, player, count);
            }
            else
            {
                GiveOne(call, player, id, count);
            }
        }

        private static void GiveOne(CommandCall call, Player player, string id, int count)
        {
            GameObject? prefab = StonePrefab(id, out string? problem);
            if (prefab == null)
            {
                call.Fail(problem!, Grammar);
                return;
            }
            StoneDef? def = ActiveRules.Current.Stone(id);
            call.Reply(Placed(prefab, count, InventorySpawn.GiveStack(player, prefab, count)));
            if (StoneCatalog.IsShard(id))
            {
                ShardNote(call, id);
            }
            else if (def == null || !def.Enabled)
            {
                call.Detail($"{id} is {(def == null ? "not in the configuration" : "disabled")}: applying it refuses with stone_disabled.");
            }
        }

        private static void GiveAll(CommandCall call, Player player, int count)
        {
            List<string> lines = new List<string>();
            int given = 0;
            foreach (StoneDef stone in ActiveRules.Current.Economy.Stones)
            {
                if (!stone.Enabled)
                {
                    continue;
                }
                GameObject? prefab = StonePrefab(stone.Id, out string? problem);
                lines.Add(prefab == null ? problem! : Placed(prefab, count, InventorySpawn.GiveStack(player, prefab, count)));
                given += prefab == null ? 0 : 1;
            }
            call.Reply($"{count} of each of {given} enabled stones");
            lines.ForEach(call.Detail);
        }

        private static string Placed(GameObject prefab, int count, int added)
        {
            string name = ItemText.Name(prefab.GetComponent<ItemDrop>().m_itemData);
            string dropped = added < count ? $", {count - added} dropped at your feet (inventory full)" : "";
            return $"{added} {name} ({prefab.name}) into the inventory{dropped}";
        }

        /// <summary>
        /// The prefab of a stone id from the Items area's registry (<see cref="StonePrefabs.Get"/>: the configured
        /// entry's prefab, else a built-in id's own). It must also be in the object database, or the stones would be
        /// dropped from the inventory on the next load (game notes pitfall 6).
        /// </summary>
        public static GameObject? StonePrefab(string id, out string? problem)
        {
            bool shard = StoneCatalog.IsShard(id);
            GameObject? prefab = shard ? StonePrefabs.GetShard(id) : StonePrefabs.Get(id);
            bool known = shard || ActiveRules.Current.Stone(id) != null || StoneCatalog.IsBuiltIn(id);
            bool inDatabase = prefab != null && ObjectDB.instance != null && ObjectDB.instance.GetItemPrefab(prefab.name) == prefab;
            problem = !known ? $"unknown stone '{id}'. Closest: {Closest.To(id, StoneIds())}"
                : prefab == null ? $"{id}: its stone prefab has not been built (see the log)."
                : !inDatabase ? $"{id}: its prefab {prefab.name} is not registered in the object database."
                : null;
            return problem == null ? prefab : null;
        }

        private static IEnumerable<string> StoneIds()
        {
            foreach (StoneDef stone in ActiveRules.Current.Economy.Stones)
            {
                yield return stone.Id;
            }
            foreach (string id in StoneCatalog.BuiltInIds)
            {
                yield return id;
            }
            foreach (string id in StoneCatalog.ShardIds)
            {
                yield return id;
            }
        }

        // What the shards fuse into under the running rules (salvage.md section 6), or why they do not.
        private static void ShardNote(CommandCall call, string id)
        {
            FragmentDef? fragment = ActiveRules.Current.Economy.Salvage.Fragment(id);
            StoneDef? stone = fragment == null ? null : ActiveRules.Current.Stone(fragment.Stone);
            call.Detail(fragment == null ? $"{id} is not in the configuration (salvage.fragments): right-clicking it refuses."
                : $"{fragment.Fuse} fuse into {fragment.Stone}" + (stone == null || !stone.Enabled ? " (disabled: fusing refuses)" : ""));
        }
    }

    /// <summary>Numeric argument parsing with the error reply.</summary>
    internal static class Counts
    {
        /// <summary>Argument <paramref name="index"/> as an integer in [min, max]; absent = <paramref name="fallback"/>.</summary>
        public static bool TryParse(CommandCall call, int index, int min, int max, int fallback, out int value, string grammar)
        {
            value = fallback;
            string text = call.Arg(index);
            if (text.Length == 0)
            {
                return true;
            }
            if (Numbers.TryInt(text, out value) && value >= min && value <= max)
            {
                return true;
            }
            call.Fail($"'{text}' should be a whole number from {min} to {max}.", grammar);
            return false;
        }
    }
}
