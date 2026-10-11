using System.Collections.Generic;
using EliteCreaturesReborn.Loot;
using EliteCreaturesReborn.Patches;
using EliteCreaturesReborn.Rules;
using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// A raider's own loot, times the raid's drops (features/raids.md section 3): its drop list as the mod's loot rules
    /// left it (stars, mutations, aspects, the world-wide multiplier), scaled once more by the multiplier the raider pays
    /// at (<see cref="RaiderPurse.DropsFor"/>) - x5 in a deadly raid, a tenth to a half in a trivial one, x1 once the
    /// raid was lost. Below 1 each stack is rounded up or down at random by its fraction, so on average the share is
    /// right and a raid that pays a tenth mostly drops nothing. It touches the rows the loot engine treats as the
    /// creature's own (its table and the rows its <c>creatures:</c> entry names), trophies only when the trophy switch
    /// says so, and never a row another mod put in the list. A raider that forfeits (a stop) drops nothing at all, every row cleared. The coins it carried are
    /// not here: they are no loot and never multiplied (<see cref="RaidCoins"/>). Called last by the drop list's patch
    /// (<see cref="LootPatch"/>), on the dying raider's owner, while its ZDO still answers; a failure is reported and
    /// swallowed, leaving the list as the loot rules made it.
    /// </summary>
    internal static class RaidLoot
    {
        /// <summary>The creature's own drop prefabs for the kill being reworked; filled and emptied in one call.</summary>
        private static readonly HashSet<GameObject> Own = new HashSet<GameObject>();

        /// <summary>Every kill's drop list passes here: a creature that is no raider costs three field reads and one ZDO
        /// lookup.</summary>
        public static void Apply(CharacterDrop drop, List<KeyValuePair<GameObject, int>> result)
        {
            Character character = drop.m_character;
            ZNetView? nview = character != null ? character.m_nview : null;
            ZDO? zdo = nview != null ? nview.GetZDO() : null;
            if (result != null && RaiderTag.IsRaider(zdo))
            {
                SafeCall.Run("CharacterDrop.GenerateDropList raid loot", static (d, r) => Rework(d, r), drop, result);
            }
        }

        private static void Rework(CharacterDrop drop, List<KeyValuePair<GameObject, int>> result)
        {
            ZDO zdo = drop.m_character.m_nview.GetZDO();
            if (RaiderPurse.Forfeit(zdo))
            {
                result.Clear(); // a stop: nothing at all, whoever put it in the list
                return;
            }
            float factor = Mathf.Max(0f, RaiderPurse.DropsFor(zdo));
            if (!Mathf.Approximately(factor, 1f))
            {
                Scale(drop, result, factor);
            }
        }

        private static void Scale(CharacterDrop drop, List<KeyValuePair<GameObject, int>> result, float factor)
        {
            CreatureLootRule? rule = RuleFor(drop);
            bool trophies = rule?.MultiplyTrophies ?? RuleState.Active.Loot.MultiplyTrophies;
            ClaimOwn(drop, rule);
            for (int i = result.Count - 1; i >= 0; i--)
            {
                GameObject item = result[i].Key;
                if (item == null || !Own.Contains(item) || (!trophies && DropRoller.IsTrophy(item)))
                {
                    continue;
                }
                int amount = RandomRound(result[i].Value * factor);
                if (amount > 0)
                {
                    result[i] = new KeyValuePair<GameObject, int>(item, amount);
                }
                else
                {
                    result.RemoveAt(i);
                }
            }
            Own.Clear();
        }

        // The rows the loot engine counts as this creature's own: its table, and every item its rule file entry names.
        private static void ClaimOwn(CharacterDrop drop, CreatureLootRule? rule)
        {
            Own.Clear();
            foreach (CharacterDrop.Drop row in drop.m_drops)
            {
                if (row.m_prefab != null)
                {
                    Own.Add(row.m_prefab);
                }
            }
            if (rule != null)
            {
                ClaimNamed(rule.Overrides);
                ClaimNamed(rule.Extras);
            }
        }

        private static void ClaimNamed(List<DropRule> rows)
        {
            foreach (DropRule row in rows)
            {
                GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(row.Item) : null;
                if (prefab != null)
                {
                    Own.Add(prefab);
                }
            }
        }

        private static CreatureLootRule? RuleFor(CharacterDrop drop) =>
            RuleState.Active.CreatureLoot.TryGetValue(Utils.GetPrefabName(drop.gameObject), out CreatureLootRule rule)
                ? rule : null;

        /// <summary>A scaled amount rounded up by the chance of its fraction (2.3 is 3 three times in ten, else 2), held
        /// to the game's per-row cap.</summary>
        private static int RandomRound(float amount)
        {
            int whole = Mathf.FloorToInt(amount);
            if (Random.value < amount - whole)
            {
                whole++;
            }
            return Mathf.Min(whole, DropRoller.AmountCap);
        }
    }
}
