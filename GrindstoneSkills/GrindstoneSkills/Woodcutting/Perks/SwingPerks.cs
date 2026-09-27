using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Stamina refund and less axe wear for a swing that hits wood, on the swinging player's own client, at their own
    /// Woodcutting level (linear from nothing at level 0, <see cref="SwingPerkSettings"/>).
    /// <para>The moment is the swing's Woodcutting raise (<see cref="SwingScope"/>). Attack.DoMeleeAttack sends every
    /// hit, then, when anything was hit, drains the weapon's durability once and raises each skill the hits used once,
    /// Woodcutting among them when any hit landed on wood. So this runs once per swing however many trees, logs or
    /// creatures it hit, always after the wear, and at level 100 too (Player.RaiseSkill always calls Skills.RaiseSkill;
    /// only Skill.Raise stops at 100). A swing that raises no Woodcutting gets nothing: the Thunderblood axes have no
    /// tree special hit and train Axes on wood.</para>
    /// <list type="bullet">
    /// <item>Stamina: the game spent Attack.GetAttackStamina (the attack's cost after equipment modifiers, status
    /// effects and the Axes skill's 33% at 100) times Game.m_staminaRate through Player.UseStamina, in Attack.Update on
    /// the swing's first frame. The same cost, read again at the hit, is given back in part with Player.AddStamina,
    /// which stops at max stamina.</item>
    /// <item>Wear: DoMeleeAttack took m_useDurabilityDrain times Game.m_durabilityRate off the weapon, for players and
    /// items that use durability. Part of it is put back, never above the item's max durability.</item>
    /// </list>
    /// Stamina and the inventory both live on the player's own client (it owns the player's ZDO and saves the durability
    /// with the character), so nothing here needs the server; the settings reach the client by config sync.
    /// </summary>
    public static class SwingPerks
    {
        /// <summary>
        /// Called by <see cref="SwingScope"/> on the swinging player's own client, once per swing that hit wood, after
        /// the game drained the weapon's durability for the swing.
        /// </summary>
        public static void OnSwingHitWood(Attack attack)
        {
            Humanoid character = attack != null ? attack.m_character : null;
            if (character == null || character != Player.m_localPlayer || !WoodSkill.Active)
                return;
            float level = WoodSkill.Local();
            RefundStamina(attack, character, WoodSkill.Share(SwingPerkSettings.StaminaRefund.Value, level));
            ReduceWear(attack.m_weapon, WoodSkill.Share(SwingPerkSettings.WearReduction.Value, level));
        }

        /// <summary>The stamina the game spent on this attack, as Player.UseStamina took it.</summary>
        private static float StaminaSpent(Attack attack) => Mathf.Max(0f, attack.GetAttackStamina()) * Game.m_staminaRate;

        /// <summary>The durability the game drains from this weapon per swing that hits anything; 0 for items without wear.</summary>
        private static float WearPerSwing(ItemDrop.ItemData weapon) =>
            weapon != null && weapon.m_shared.m_useDurability ? weapon.m_shared.m_useDurabilityDrain * Game.m_durabilityRate : 0f;

        private static void RefundStamina(Attack attack, Humanoid character, float share)
        {
            if (share <= 0f)
                return;
            float refund = StaminaSpent(attack) * share;
            if (refund > 0f)
                character.AddStamina(refund);
        }

        private static void ReduceWear(ItemDrop.ItemData weapon, float share)
        {
            float refund = WearPerSwing(weapon) * share;
            if (refund <= 0f)
                return;
            float restored = Mathf.Min(weapon.GetMaxDurability(), weapon.m_durability + refund);
            if (restored > weapon.m_durability)
                weapon.m_durability = restored;
        }
    }
}
