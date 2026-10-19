using System;
using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Loot
{
    /// <summary>
    /// What a boss pays beyond its own table (`rewards`): its biome's material, so a boss is still worth fighting once its
    /// trophy and key item are spent (a player's suggestion on GitHub, 2026-10-10). One roll of the amount for the plain
    /// fight and one more per star, for every player near the boss (each earns a solo share, as with the trophies; at
    /// least one), times its aspects' loot (a Bountiful boss pays every aspect's) and, while the loot
    /// rules are on, the global and boss multipliers. Paid in every loot mode, Vanilla included, as a boss's trophies and
    /// aspect are, and added after the engine's own multiply so nothing scales it twice. Each Twin pays its own; of a
    /// Tethered pair only the last to fall (the first drops nothing); a Phantom copy never. A boss with no row here
    /// (another mod's) pays nothing extra. The table is fixed: a server that wants other rewards turns `rewards` off and
    /// writes `extra drops` rows in the boss's `creatures:` entry.
    /// </summary>
    internal static class BossRewards
    {
        private readonly struct Reward
        {
            public readonly string Item;
            public readonly int Min;
            public readonly int Max;

            public Reward(string item, int min, int max)
            {
                Item = item;
                Min = min;
                Max = max;
            }
        }

        private static readonly Dictionary<string, Reward> Table =
            new Dictionary<string, Reward>(StringComparer.OrdinalIgnoreCase)
            {
                ["Eikthyr"] = new Reward("Flint", 2, 4), // the Meadows has no metal: flint, its scarce early material
                ["gd_king"] = new Reward("CopperOre", 5, 10),
                ["Bonemass"] = new Reward("IronScrap", 5, 10),
                ["Dragon"] = new Reward("SilverOre", 5, 10),
                ["GoblinKing"] = new Reward("BlackMetalScrap", 5, 10),
                ["SeekerQueen"] = new Reward("Carapace", 5, 10),
                ["Fader"] = new Reward("FlametalOreNew", 5, 10),
            };

        /// <summary>Adds the boss's reward to its drop list; <paramref name="rulesOn"/> is false in Vanilla loot mode.</summary>
        public static void Pay(EliteController controller, List<KeyValuePair<GameObject, int>> result, bool rulesOn)
        {
            if (!Pays(controller, out Reward reward) || !(DropRoller.FindPrefab(reward.Item) is GameObject prefab))
            {
                return;
            }
            int rolls = (controller.Traits.Stars + 1) * PlayersNear(controller);
            int amount = 0;
            for (int roll = 0; roll < rolls; roll++)
            {
                amount += UnityEngine.Random.Range(reward.Min, reward.Max + 1);
            }
            DropRoller.Add(result, prefab, DropRoller.Scaled(amount, AspectLoot.Factor(controller) * Multiplier(rulesOn)));
        }

        /// <summary>The altar's line for the fight on the bowl, "Reward: 10-20 Iron Scrap per player", or "" when it pays
        /// none. In the player's language: a hover that keeps it builds it again when <c>Lang.Version</c> changes.</summary>
        public static string AltarLine(string bossPrefab, int stars, float aspectLoot)
        {
            if (!RuleState.Active.Boss.Rewards || !Table.TryGetValue(bossPrefab, out Reward reward)
                || !(DropRoller.FindPrefab(reward.Item) is GameObject prefab))
            {
                return "";
            }
            float factor = aspectLoot * Multiplier(RuleState.Active.Loot.Mode != LootMode.Vanilla);
            int rolls = stars + 1;
            int min = DropRoller.Scaled(reward.Min * rolls, factor);
            int max = DropRoller.Scaled(reward.Max * rolls, factor);
            return $"\n<color=orange>{Lang.Format("ecr_loot_altar_reward", min, max, ItemName(prefab))}</color>";
        }

        private static bool Pays(EliteController controller, out Reward reward)
        {
            reward = default;
            Character? boss = controller.Creature;
            return RuleState.Active.Boss.Rewards && boss != null && boss.IsBoss() && !controller.Traits.PhantomCopy
                && Table.TryGetValue(Utils.GetPrefabName(controller.gameObject), out reward);
        }

        /// <summary>The players sharing the fight: those within a trophy's range of the boss, at least one.</summary>
        private static int PlayersNear(EliteController controller) =>
            Mathf.Max(1, Player.GetPlayersInRangeXZ(controller.transform.position, BossTrophies.PlayerRange));

        private static float Multiplier(bool rulesOn)
        {
            LootRules loot = RuleState.Active.Loot;
            return rulesOn ? loot.GlobalMultiplier * loot.BossMultiplier : 1f;
        }

        private static string ItemName(GameObject prefab)
        {
            ItemDrop? item = prefab.GetComponent<ItemDrop>();
            string name = item != null ? item.m_itemData.m_shared.m_name : prefab.name;
            return Localization.instance != null ? Localization.instance.Localize(name) : name;
        }
    }
}
