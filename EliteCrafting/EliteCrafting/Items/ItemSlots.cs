using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EliteCrafting.Core;
using EliteCrafting.Rules;
using ItemType = ItemDrop.ItemData.ItemType;
using SkillType = Skills.SkillType;

namespace EliteCrafting.Items
{
    /// <summary>
    /// Classifies an item into the slot taxonomy (item-data.md section 2, classification by skill from the game notes
    /// Q12) and answers the magic-base question. The result is cached per <c>SharedData</c>, which the game shares
    /// between every copy of an item type, so a lookup is one table hit. Torches, trinkets, hands, ammo, materials and
    /// everything else outside the table are never magic bases.
    /// </summary>
    public static class ItemSlots
    {
        private static readonly ConditionalWeakTable<ItemDrop.ItemData.SharedData, SlotInfo> Cache =
            new ConditionalWeakTable<ItemDrop.ItemData.SharedData, SlotInfo>();

        /// <summary>Prefix of every stone prefab name (<c>ECF_Awakening</c>, <c>ECF_Custom01</c>).</summary>
        public const string StonePrefabPrefix = "ECF_";

        /// <summary>Prefix of every stone's shared name token (<c>$ecf_stone_awakening</c>).</summary>
        public const string StoneNamePrefix = "$ecf_stone_";

        public static SlotInfo Classify(ItemDrop.ItemData? item)
        {
            if (item?.m_shared == null)
            {
                return SlotInfo.NotEligible;
            }
            return Cache.GetValue(item.m_shared, SlotClassifier.Classify);
        }

        public static ItemSlot SlotOf(ItemDrop.ItemData? item) => Classify(item).Slot;

        /// <summary>One of our stones: by prefab name or by shared name token. Stones never carry item state.</summary>
        public static bool IsStone(ItemDrop.ItemData? item)
        {
            if (item?.m_shared == null)
            {
                return false;
            }
            if (item.m_dropPrefab != null && item.m_dropPrefab.name.StartsWith(StonePrefabPrefix, System.StringComparison.Ordinal))
            {
                return true;
            }
            string? name = item.m_shared.m_name;
            return name != null && name.StartsWith(StoneNamePrefix, System.StringComparison.Ordinal);
        }

        /// <summary>
        /// May carry affixes (item-data.md section 2): not stackable, maps to a slot, not a stone. Read live, since
        /// another mod may change stack sizes at runtime.
        /// </summary>
        public static bool IsMagicBase(ItemDrop.ItemData? item)
        {
            return item?.m_shared != null
                && item.m_shared.m_maxStackSize == 1
                && Classify(item).Slot != ItemSlot.None
                && !IsStone(item);
        }

        /// <summary>Whether an affix's <c>requires</c> block accepts this item type.</summary>
        public static bool Satisfies(SlotInfo info, AffixRequirements requires)
        {
            if (requires.Hands != Hands.None && info.Hands != requires.Hands)
            {
                return false;
            }
            if (!info.HasTraits(requires.Traits))
            {
                return false;
            }
            return requires.Skills.Count == 0 || AnyGoverned(info, requires.Skills);
        }

        private static bool AnyGoverned(SlotInfo info, IReadOnlyList<SkillType> skills)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                if (info.IsGovernedBy(skills[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The YAML id of a slot (<c>melee_weapon</c>).</summary>
        public static string Id(ItemSlot slot) => EnumIds<ItemSlot>.Id(slot);

        /// <summary>Parses a slot id; <c>none</c> is not a slot.</summary>
        public static bool TryParse(string? id, out ItemSlot slot) =>
            EnumIds<ItemSlot>.TryParse(id, out slot) && slot != ItemSlot.None;
    }

    /// <summary>The classification rules themselves, run once per item type.</summary>
    internal static class SlotClassifier
    {
        public static SlotInfo Classify(ItemDrop.ItemData.SharedData shared)
        {
            ItemSlot slot = SlotFor(shared);
            if (slot == ItemSlot.None)
            {
                return SlotInfo.NotEligible;
            }
            return new SlotInfo(slot, HandsOf(shared.m_itemType), TraitsOf(shared), SkillsOf(shared));
        }

        private static ItemSlot SlotFor(ItemDrop.ItemData.SharedData shared)
        {
            ItemSlot armor = ArmorSlot(shared.m_itemType);
            if (armor != ItemSlot.None)
            {
                return armor;
            }
            if (IsTool(shared))
            {
                return ItemSlot.Tool;
            }
            return WeaponSlot(shared);
        }

        private static ItemSlot ArmorSlot(ItemType type)
        {
            switch (type)
            {
                case ItemType.Shield: return ItemSlot.Shield;
                case ItemType.Helmet: return ItemSlot.Head;
                case ItemType.Chest: return ItemSlot.Chest;
                case ItemType.Legs: return ItemSlot.Legs;
                case ItemType.Shoulder: return ItemSlot.Cape;
                case ItemType.Utility: return ItemSlot.UtilityItem;
                default: return ItemSlot.None;
            }
        }

        // Tools: the Tool type, anything with a build-piece table (hammer, hoe, cultivator), pickaxes and fishing rods.
        private static bool IsTool(ItemDrop.ItemData.SharedData shared)
        {
            if (!IsHeldType(shared.m_itemType))
            {
                return false;
            }
            return shared.m_itemType == ItemType.Tool
                || shared.m_buildPieces != null
                || shared.m_skillType == SkillType.Pickaxes
                || shared.m_skillType == SkillType.Fishing;
        }

        private static ItemSlot WeaponSlot(ItemDrop.ItemData.SharedData shared)
        {
            if (!IsHeldType(shared.m_itemType))
            {
                return ItemSlot.None;
            }
            switch (shared.m_skillType)
            {
                case SkillType.Bows:
                case SkillType.Crossbows: return ItemSlot.RangedWeapon;
                case SkillType.ElementalMagic:
                case SkillType.BloodMagic: return ItemSlot.MagicWeapon;
                case SkillType.Swords:
                case SkillType.Knives:
                case SkillType.Clubs:
                case SkillType.Polearms:
                case SkillType.Spears:
                case SkillType.Axes:
                case SkillType.Unarmed: return shared.m_itemType == ItemType.Bow ? ItemSlot.RangedWeapon : ItemSlot.MeleeWeapon;
                default: return shared.m_itemType == ItemType.Bow ? ItemSlot.RangedWeapon : ItemSlot.None;
            }
        }

        private static bool IsHeldType(ItemType type) =>
            type == ItemType.OneHandedWeapon || type == ItemType.TwoHandedWeapon || type == ItemType.TwoHandedWeaponLeft
            || type == ItemType.Attach_Atgeir || type == ItemType.Bow || type == ItemType.Tool;

        private static Hands HandsOf(ItemType type)
        {
            switch (type)
            {
                case ItemType.OneHandedWeapon: return Hands.One;
                case ItemType.TwoHandedWeapon:
                case ItemType.TwoHandedWeaponLeft:
                case ItemType.Attach_Atgeir:
                case ItemType.Bow: return Hands.Two;
                default: return Hands.None;
            }
        }

        private static ItemTraits TraitsOf(ItemDrop.ItemData.SharedData shared)
        {
            ItemTraits traits = ItemTraits.None;
            if (shared.m_useDurability) traits |= ItemTraits.WearsOut;
            if (shared.m_movementModifier < 0f) traits |= ItemTraits.MovementPenalty;
            if (shared.m_buildPieces != null) traits |= ItemTraits.Builds;
            if (FiresProjectiles(shared.m_attack)) traits |= ItemTraits.Projectile;
            if (!string.IsNullOrEmpty(shared.m_ammoType)) traits |= ItemTraits.Ammo;
            if (shared.m_itemType == ItemType.Shield && shared.m_timedBlockBonus > 1f) traits |= ItemTraits.CanParry;
            return traits;
        }

        private static bool FiresProjectiles(Attack? attack) =>
            attack != null && (attack.m_attackType == Attack.AttackType.Projectile
                || attack.m_attackType == Attack.AttackType.TriggerProjectile);

        private static IReadOnlyList<SkillType> SkillsOf(ItemDrop.ItemData.SharedData shared)
        {
            List<SkillType> skills = new List<SkillType> { shared.m_skillType };
            if (shared.m_damages.m_chop > 0f && shared.m_skillType != SkillType.WoodCutting)
            {
                skills.Add(SkillType.WoodCutting);
            }
            if (shared.m_damages.m_pickaxe > 0f && shared.m_skillType != SkillType.Pickaxes)
            {
                skills.Add(SkillType.Pickaxes);
            }
            return skills;
        }
    }
}
