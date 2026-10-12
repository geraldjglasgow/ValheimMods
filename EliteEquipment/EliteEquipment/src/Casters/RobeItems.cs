using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

using SharedData = ItemDrop.ItemData.SharedData;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A Seer's robe piece of any set (<see cref="RobeSets"/>): a copy of the game's Eitr-weave Hood or Robe or the leather
    /// trousers (their slots, sounds, upgrades, durability; recipes beside the game's armour of the set's biome), wearing
    /// the set's workshop robes (<see cref="RobeLook"/>), with its own words, icon, armour and movement, and the set: all
    /// three of one set worn give its bonus (<see cref="RobeSet.SetBonus"/>), shown by the game as any set is. Their eitr
    /// is added by <see cref="GearEitr"/>.
    /// </summary>
    public static class RobeItems
    {
        public static GameObject Build(GearPiece piece, GameObject game, GameObject helmet)
        {
            GameObject item = PrefabBench.Copy(game, piece.Prefab);
            RobeLook.Wear(piece, item, game, helmet, CasterBundles.Prefab(piece.Bundle, piece.Asset));
            SharedData shared = item.GetComponent<ItemDrop>().m_itemData.m_shared;
            CasterItemLook.Describe(shared, piece);
            Stats(shared, piece);
            return item;
        }

        /// <summary>A set's status effect exists once the database has woken; each of its pieces points at it then.</summary>
        public static void Join(RobeSet set)
        {
            foreach (GearPiece piece in set.Pieces)
            {
                if (piece.Item != null)
                    piece.Item.GetComponent<ItemDrop>().m_itemData.m_shared.m_setStatusEffect = set.SetBonus;
            }
        }

        private static void Stats(SharedData shared, GearPiece piece)
        {
            shared.m_armor = piece.Armour;
            shared.m_armorPerLevel = CasterGear.ArmourPerLevel;
            shared.m_weight = 1f;
            shared.m_movementModifier = piece.Movement;
            shared.m_eitrRegenModifier = 0f;
            shared.m_damageModifiers = new List<HitData.DamageModPair>();
            shared.m_equipStatusEffect = null;
            shared.m_setName = "ee_seer_" + piece.Set.Tier;
            shared.m_setSize = piece.Set.Pieces.Count;
            shared.m_setStatusEffect = piece.Set.SetBonus;
        }
    }
}
