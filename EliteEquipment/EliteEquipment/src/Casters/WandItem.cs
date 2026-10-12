using BundlePrefabs;
using UnityEngine;

using SharedData = ItemDrop.ItemData.SharedData;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A wand (<see cref="CasterGear.Wands"/>): a copy of the game's Club (a one-handed weapon: its hold, block, sounds, upgrades and place on the
    /// back), wearing the workshop wand, with two attacks built on the game's Staff of Embers' cast (<c>staff_fireball</c>,
    /// the one cast motion that plays on a one-handed weapon, tried in game 2026-10-09). Primary (left mouse): the bolt
    /// (<see cref="WandBolt"/>), pierce and spirit, paid in eitr (<see cref="SpellCost"/>), charged by holding
    /// (<see cref="CasterControls"/>, <see cref="WindUp"/>) and thrown with the charged staff's release (<see cref="Release"/>). Secondary (right mouse through
    /// <see cref="CasterControls"/>, and middle mouse): a cast that hits nothing (<c>AttackType.None</c>):
    /// <see cref="SpellCast"/> casts the selected spell when it lands, <see cref="SpellCost"/> prices it. The skill is
    /// Elemental Magic (the bolt's); heals and chants raise Blood Magic themselves.
    /// </summary>
    public static class WandItem
    {
        private const string Cast = "staff_fireball";
        /// <summary>
        /// The bolt's motion: the game's charged staff letting go (<c>staff_charge_attack</c>, its upper body only, the
        /// state of the workshop's casts, <see cref="WandMoves"/>), after <see cref="WindUp"/>'s hold (<c>staff_charging</c>).
        /// Chained three levels deep like a sword; the game's numbered triggers are held back (<see cref="WandAnimations"/>).
        /// </summary>
        private const string Release = "staff_charge_attack";

        public static GameObject Build(GearPiece wand, GameObject club, GameObject staff)
        {
            GameObject item = PrefabBench.Copy(club, wand.Prefab);
            HeldModel.Wear(item, club, CasterBundles.Prefab(wand.Bundle, wand.Asset), HeldModel.InFist);
            SharedData shared = item.GetComponent<ItemDrop>().m_itemData.m_shared;
            CasterItemLook.Describe(shared, wand);
            Stats(shared, wand.Bolt);
            Attacks(shared, staff.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack);
            return item;
        }

        /// <summary>
        /// The bolt is built with the scene's prefabs, after the wand: the wand's primary attack fires it. Until then the
        /// wand has no primary attack (no animation: the game offers none), so it never fires the staff's own fireball.
        /// </summary>
        public static void Arm(GameObject wand, GameObject bolt)
        {
            if (wand == null || bolt == null)
                return;
            Attack attack = wand.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack;
            attack.m_attackProjectile = bolt;
            attack.m_attackAnimation = Release;
        }

        private static void Stats(SharedData shared, float bolt)
        {
            shared.m_skillType = Skills.SkillType.ElementalMagic;
            shared.m_damages = new HitData.DamageTypes { m_pierce = bolt, m_spirit = bolt };
            shared.m_damagesPerLevel = new HitData.DamageTypes { m_pierce = 1f, m_spirit = 1f };
            shared.m_weight = 1f;
            shared.m_maxDurability = 100f;
            shared.m_durabilityPerLevel = 50f;
            shared.m_useDurability = true;
            shared.m_useDurabilityDrain = 1f;
            shared.m_attackForce = 10f;
            shared.m_eitrRegenModifier = 0f;
        }

        private static void Attacks(SharedData shared, Attack cast)
        {
            Attack spell = Clean(cast, Attack.AttackType.None, 0f);
            spell.m_speedFactorRotation = 0f;
            shared.m_secondaryAttack = spell;
            Attack bolt = Clean(cast, Attack.AttackType.Projectile, 0f);
            bolt.m_projectiles = 1;
            // The staff's cast tilts its fireball 5 degrees up and spreads it a degree: the bolt goes where it is aimed.
            bolt.m_launchAngle = 0f;
            bolt.m_projectileAccuracy = 0f;
            bolt.m_projectileAccuracyMin = 0f;
            // Three moves chained as a sword's swings are (WandMoves); the game names their triggers staff_charge_attack0..2,
            // which WandAnimations holds back.
            bolt.m_attackChainLevels = 3;
            bolt.m_attackRandomAnimations = 0;
            bolt.m_attackAnimation = "";
            shared.m_attack = bolt;
        }

        /// <summary>
        /// A copy of the staff's cast as one of the wand's attacks: no projectile (the bolt is armed later), no eitr (priced
        /// by <see cref="SpellCost"/>), no health; the staff's fire sounds and flames go, the cast's own sound and flash
        /// come from <see cref="CasterFx"/>.
        /// </summary>
        private static Attack Clean(Attack cast, Attack.AttackType type, float stamina)
        {
            Attack attack = cast.Clone();
            attack.m_attackType = type;
            attack.m_attackProjectile = null;
            attack.m_attackEitr = 0f;
            attack.m_attackStamina = stamina;
            attack.m_attackHealth = 0f;
            attack.m_attackHealthPercentage = 0f;
            attack.m_startEffect = new EffectList();
            attack.m_triggerEffect = new EffectList();
            attack.m_trailStartEffect = new EffectList();
            attack.m_hitEffect = new EffectList();
            attack.m_burstEffect = new EffectList();
            return attack;
        }
    }
}
