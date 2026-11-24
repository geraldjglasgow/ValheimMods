using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

using SharedData = ItemDrop.ItemData.SharedData;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A spellbook (features/casters.md, "The spellbook"; one a tier: <see cref="CasterGear.Books"/>): a copy of the game's wood
    /// shield (a Shield item, always in the left hand beside a one-handed weapon: block and bash sounds, upgrades, its place
    /// on the back), wearing the workshop's book held as the Raven Fetish was (<see cref="HeldModel.OnHand"/>: the same
    /// contract, a <c>model</c> with its <c>grip</c> at the middle of its back); without the bundle it keeps the wood
    /// shield's look. While it is in the left hand it is drawn open on the palm (<see cref="BookOpen"/>, posed by
    /// <see cref="BookPose"/>); the shield hold stays where the pose yields and with the switch off. It blocks little (its own
    /// <see cref="GearPiece.Block"/>, +1 a level); the game sums its <see cref="GearPiece.EitrRegen"/> with the worn armour's
    /// (<c>Player.GetEquipmentEitrRegenModifier</c> counts the left hand), <see cref="HealPower"/> adds its heal power while
    /// it is held, and its pages are the spells a wand can cast (<see cref="BookPages"/>).
    /// </summary>
    public static class BookItem
    {
        public static GameObject Build(GearPiece book, GameObject shield)
        {
            GameObject item = PrefabBench.Copy(shield, book.Prefab);
            HeldModel.Wear(item, shield, CasterBundles.Prefab(book.Bundle, book.Asset), HeldModel.OnHand);
            BookOpen.Add(item, shield, book);
            SharedData shared = item.GetComponent<ItemDrop>().m_itemData.m_shared;
            CasterItemLook.Describe(shared, book);
            Stats(shared, book);
            return item;
        }

        private static void Stats(SharedData shared, GearPiece book)
        {
            shared.m_blockPower = book.Block;
            shared.m_blockPowerPerLevel = 1f;
            shared.m_deflectionForce = 5f;
            shared.m_timedBlockBonus = 1.5f;
            shared.m_eitrRegenModifier = book.EitrRegen;
            shared.m_movementModifier = 0f;
            shared.m_weight = 1f;
            shared.m_maxDurability = 100f;
            shared.m_durabilityPerLevel = 50f;
            shared.m_damageModifiers = new List<HitData.DamageModPair>();
        }
    }
}
