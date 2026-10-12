using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

using SharedData = ItemDrop.ItemData.SharedData;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The Raven Fetish: a copy of the game's wood shield (a Shield item, always in the left hand: block and bash sounds,
    /// upgrades, its place on the back), wearing the workshop's hand-ward (a cut wooden board, a little bigger than a hand,
    /// the raven fetish lashed to its face; user, 2026-10-10) held like the game's bucklers (<see cref="HeldModel.OnHand"/>).
    /// It blocks little; the game sums its eitr regen with the worn armour's (<c>Player.GetEquipmentEitrRegenModifier</c>
    /// counts the left hand), and <see cref="HealPower"/> adds its heal power while it is held.
    /// </summary>
    public static class FocusItem
    {
        public static GameObject Build(GameObject shield)
        {
            GearPiece focus = CasterGear.Focus;
            GameObject item = PrefabBench.Copy(shield, focus.Prefab);
            HeldModel.Wear(item, shield, CasterBundles.Prefab(focus.Bundle, focus.Asset), HeldModel.OnHand);
            SharedData shared = item.GetComponent<ItemDrop>().m_itemData.m_shared;
            CasterItemLook.Describe(shared, focus);
            Stats(shared);
            return item;
        }

        private static void Stats(SharedData shared)
        {
            shared.m_blockPower = 2f;
            shared.m_blockPowerPerLevel = 1f;
            shared.m_deflectionForce = 5f;
            shared.m_timedBlockBonus = 1.5f;
            shared.m_eitrRegenModifier = CasterGear.FocusEitrRegen;
            shared.m_movementModifier = 0f;
            shared.m_weight = 0.5f;
            shared.m_maxDurability = 100f;
            shared.m_durabilityPerLevel = 50f;
            shared.m_damageModifiers = new List<HitData.DamageModPair>();
        }
    }
}
