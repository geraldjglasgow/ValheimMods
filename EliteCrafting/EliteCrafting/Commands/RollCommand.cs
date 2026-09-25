using EliteCrafting.Affixes;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;
using UnityEngine;

namespace EliteCrafting.Commands
{
    /// <summary>
    /// <c>ecraft roll &lt;rarity&gt; &lt;prefab|slot&gt; [tier]</c> (console-commands.md section 3): creates a rolled magic
    /// item in the caller's own inventory (dropped at the feet when full). The item is built as a gear drop builds it
    /// (drops.md section 8: upgrade level 1, full durability, the world's world level, no crafter, a random variant)
    /// and rolled with <see cref="ItemRoller.RollFresh"/>, the drop's own procedure, so it is a true sample.
    /// <c>tier</c> (1-7) overrides the base's tier ceiling for this roll. <c>roll mythic</c> is allowed (CMD-3).
    /// Runs on the caller's machine; the item is client-owned like any other.
    /// </summary>
    internal static class RollCommand
    {
        public const string Grammar = "ecraft roll <rarity> <prefab|slot> [tier]";

        public static void Run(CommandCall call)
        {
            Player? player = Player.m_localPlayer;
            if (player == null)
            {
                call.Reply("needs a player in the world.");
                return;
            }
            RarityDef? rarity = ParseRarity(call);
            if (rarity == null || !Counts.TryParse(call, 2, 1, 7, 0, out int tier, Grammar))
            {
                return;
            }
            System.Random random = RollRandom.Create();
            GameObject? prefab = MagicBases.Resolve(call.Arg(1), random, out string? problem);
            if (prefab == null)
            {
                call.Fail(problem!, Grammar);
                return;
            }
            RollInto(call, player, prefab, rarity, tier, random);
        }

        private static RarityDef? ParseRarity(CommandCall call)
        {
            string id = call.Lower(0);
            EconomyRules economy = ActiveRules.Current.Economy;
            RarityDef? rarity = economy.Rarity(id);
            if (rarity == null)
            {
                call.Fail(id.Length == 0 ? "which rarity?" : $"unknown rarity '{id}'. Closest: {Closest.To(id, RarityIds(economy))}", Grammar);
                return null;
            }
            if (rarity.IsBase)
            {
                call.Fail($"{id} carries no affixes; roll a higher rarity.", Grammar);
                return null;
            }
            return rarity;
        }

        private static string[] RarityIds(EconomyRules economy)
        {
            string[] ids = new string[economy.Rarities.Count];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = economy.Rarities[i].Id;
            }
            return ids;
        }

        private static void RollInto(CommandCall call, Player player, GameObject prefab, RarityDef rarity, int tier, System.Random random)
        {
            ItemDrop.ItemData item = InventorySpawn.NewItem(prefab);
            int variants = item.m_shared.m_variants;
            item.m_variant = variants > 1 ? random.Next(variants) : 0;
            RollContext context = RollContext.For(item);
            context.Random = random;
            context.Ceiling = tier > 0 ? tier : context.Ceiling;
            ItemState? state = RollerCall.Roll(call, () => ItemRoller.RollFresh(ItemState.Empty, rarity, context));
            if (state == null || !RollerCall.Commit(call, item, state))
            {
                return;
            }
            item.m_durability = item.GetMaxDurability();
            bool inInventory = InventorySpawn.GiveItem(player, item);
            ItemReport.Write(call, item, inInventory ? "your inventory" : "the ground at your feet (inventory full)");
            call.Detail($"rolled with tier ceiling {context.Ceiling}" + (tier > 0 ? " (overridden)" : " (the base's own)"));
        }
    }
}
