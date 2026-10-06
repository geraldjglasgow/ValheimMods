using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PackPanel.Panels
{
    /// <summary>
    /// A cheap fingerprint of what the Gear tab's stat sheet is worked out from (<see cref="SheetLines"/>), so the sheet
    /// is filled again only when it changed (<see cref="GearStats"/>): the pools and each food eaten (to the half unit, as
    /// the sheet shows them rounded and they shrink as food runs out), armour, weight and carry weight, every carried
    /// item (which one, its stack and quality, whether it is equipped; equipped ones with their durability to the half
    /// percent and their custom data, where Epic Loot and EliteCrafting keep their effects), the held and sheathed items
    /// and their skills, the health while a held weapon's numbers follow it, the status effects and the world's carry
    /// modifier. Nothing is allocated. What it cannot see (another mod's effect changing its own numbers) waits for the
    /// sheet's slower recheck.
    /// </summary>
    public static class SheetInputs
    {
        private static long hash;

        public static long Key(Player player)
        {
            hash = 17;
            if (player == null)
                return hash;
            Pools(player);
            Items(player.GetInventory().GetAllItems());
            Held(player);
            Effects(player.GetSEMan().GetStatusEffects());
            return hash;
        }

        private static void Pools(Player player)
        {
            Half(player.GetMaxHealth());
            Half(player.GetMaxStamina());
            Half(player.GetMaxEitr());
            Mix(player.GetBodyArmor());
            Mix(player.GetInventory().GetTotalWeight());
            Mix(player.GetMaxCarryWeight());
            Mix(Game.m_carryWeightRate);
            List<Player.Food> foods = player.m_foods;
            Mix(foods.Count);
            for (int i = 0; i < foods.Count; i++)
            {
                Player.Food food = foods[i];
                Mix(food.m_item);
                Half(food.m_health);
                Half(food.m_stamina);
                Half(food.m_eitr);
            }
        }

        private static void Items(List<ItemDrop.ItemData> items)
        {
            Mix(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                ItemDrop.ItemData item = items[i];
                Mix(item);
                Mix(item.m_stack);
                Mix(item.m_quality);
                if (item.m_equipped)
                    Equipped(item);
            }
        }

        private static void Equipped(ItemDrop.ItemData item)
        {
            Mix(1L);
            if (item.m_shared.m_useDurability)
                Mix((long)Mathf.FloorToInt(item.GetDurabilityPercentage() * 200f));
            Mix(item.m_crafterName != null ? item.m_crafterName.GetHashCode() : 0);
            if (item.m_customData == null)
                return;
            long data = 0;
            foreach (KeyValuePair<string, string> pair in item.m_customData)
                data += (pair.Key != null ? pair.Key.GetHashCode() : 0) * 31L + (pair.Value != null ? pair.Value.GetHashCode() : 0);
            Mix(data);
        }

        private static void Held(Player player)
        {
            Mix(player.m_rightItem);
            Mix(player.m_leftItem);
            Mix(player.m_hiddenRightItem);
            Mix(player.m_hiddenLeftItem);
            Mix(player.GetAmmoItem());
            Weapon(player, HeldItems.Right(player));
            Weapon(player, HeldItems.Left(player));
            Mix(player.GetSkillLevel(Skills.SkillType.Blocking));
        }

        private static void Weapon(Player player, ItemDrop.ItemData item)
        {
            if (item == null)
                return;
            Mix(player.GetSkillLevel(item.m_shared.m_skillType));
            Attack attack = item.m_shared.m_attack;
            if (attack != null && (attack.m_staminaReturnPerMissingHP != 0f || attack.m_attackHealthPercentage != 0f
                || attack.m_damageMultiplierPerMissingHP != 0f || attack.m_damageMultiplierByTotalHealthMissing != 0f))
                Mix(player.GetHealth());
        }

        private static void Effects(List<StatusEffect> effects)
        {
            Mix(effects.Count);
            for (int i = 0; i < effects.Count; i++)
                Mix(effects[i]);
        }

        private static void Mix(long value) => hash = unchecked((hash ^ value) * 1099511628211L);

        private static void Mix(float value) => Mix((long)value.GetHashCode());

        /// <summary>A value that changes all the time, to the half unit: it changes whenever its rounded text can.</summary>
        private static void Half(float value) => Mix((long)Mathf.FloorToInt(value * 2f));

        /// <summary>Which object, not its contents (no boxing: only reference types come here).</summary>
        private static void Mix(object value) => Mix((long)(value != null ? RuntimeHelpers.GetHashCode(value) : 0));
    }
}
