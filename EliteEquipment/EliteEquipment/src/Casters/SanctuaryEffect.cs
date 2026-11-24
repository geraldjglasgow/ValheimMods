using StatusTemplates;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Sanctuary's shelter (<see cref="PrefabName"/>): a status effect that leaves <see cref="Taken"/> of every hit a friend
    /// inside the dome takes (the game's <c>StatusEffect.OnDamaged</c>, which the game runs on the character's owner as a
    /// hit lands, before it is applied), <see cref="Linger"/> seconds after its last refresh (<see cref="Sanctuary"/>). Also
    /// the caster's cooldown (<see cref="CooldownName"/>, <see cref="MakeCooldown"/>): only a mark with the spell's icon and
    /// the game's countdown. Both in every item database with the casters' other effects (<see cref="CasterStatus"/>).
    /// </summary>
    public sealed class SanctuaryEffect : StatusEffect
    {
        public const string PrefabName = "EE_SE_Sanctuary";
        public const string CooldownName = "EE_SE_SanctuaryCooldown";
        public const float Taken = 0.7f;
        public const float Linger = 1f;

        public static int Hash => PrefabName.GetStableHashCode();
        public static int CooldownHash => CooldownName.GetStableHashCode();

        public static SanctuaryEffect Make(Spell spell)
        {
            SanctuaryEffect effect = StatusTemplate.Make<SanctuaryEffect>(PrefabName);
            effect.m_name = spell.Token;
            effect.m_tooltip = "$ee_se_sanctuary_tip";
            effect.m_icon = CasterBundles.Sprite(CasterBundles.Spells, spell.Icon);
            effect.m_ttl = Linger;
            return effect;
        }

        public static SE_Stats MakeCooldown(Spell spell)
        {
            SE_Stats effect = StatusTemplate.Make<SE_Stats>(CooldownName);
            effect.m_name = "$ee_se_sanctuary_cooldown";
            effect.m_tooltip = "";
            effect.m_icon = CasterBundles.Sprite(CasterBundles.Spells, spell.Icon);
            effect.m_ttl = Sanctuary.Cooldown;
            return effect;
        }

        public override void OnDamaged(HitData hit, Character attacker) => hit.ApplyModifier(Taken);
    }
}
