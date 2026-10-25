using StatusTemplates;


namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// The Gravewind Blast's frost. The gust does no frost damage (the game would freeze for a share of the victim's
    /// health, 2 s on one player and 5 on another); its attack item carries this status effect instead, which the game
    /// adds on the victim's owner (<c>Character.RPC_Damage</c>), and which puts the game's own Frost on them for
    /// <see cref="BatFiendNumbers.GravewindFrostSeconds"/>, shortened by frost resistance as the game's frost damage would
    /// be (a Frost resistance mead halves it, immunity cancels it). It never shortens a frost already lasting longer.
    /// It has no icon, so the HUD shows only the game's Frost.
    /// </summary>
    public sealed class BatFiendChill : StatusEffect
    {
        /// <summary>The effect the gravewind item carries.</summary>
        public static readonly StatusTemplate<BatFiendChill> Template = new StatusTemplate<BatFiendChill>(
            "ecr_batfiend_chill", se => (se.m_name, se.m_ttl) = ("$enemy_ecr_batfiend", 0.1f));

        public override void Setup(Character character)
        {
            base.Setup(character);
            float seconds = BatFiendNumbers.GravewindFrostSeconds * Share(character.GetDamageModifiers().GetModifier(HitData.DamageType.Frost));
            SEMan seman = character.GetSEMan();
            if (seconds <= 0f || seman == null)
            {
                return;
            }
            var frost = (seman.GetStatusEffect(SEMan.s_statusEffectFrost) ?? seman.AddStatusEffect(SEMan.s_statusEffectFrost)) as SE_Frost;
            if (frost != null && frost.m_ttl - frost.m_time < seconds)
            {
                frost.m_ttl = seconds;
                frost.ResetTime();
                frost.TriggerStartEffects();
            }
        }

        /// <summary>How much of the frost a damage modifier lets through, as it would scale frost damage.</summary>
        private static float Share(HitData.DamageModifier modifier) => modifier switch
        {
            HitData.DamageModifier.Immune => 0f,
            HitData.DamageModifier.VeryResistant => 0.25f,
            HitData.DamageModifier.Resistant => 0.5f,
            HitData.DamageModifier.SlightlyResistant => 0.75f,
            HitData.DamageModifier.SlightlyWeak => 1.25f,
            HitData.DamageModifier.Weak => 1.5f,
            HitData.DamageModifier.VeryWeak => 2f,
            _ => 1f,
        };
    }
}
