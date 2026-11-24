using StatusTemplates;
using Translations;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Revive's wait (<see cref="Revive"/>): a status effect on the caster for <see cref="Seconds"/>, shown in the game's
    /// status list with its time left (the game's own look for an effect that runs out), while which Revive is refused.
    /// Made once into every item database with the casters' other effects (<see cref="CasterStatus"/>); the spell's icon,
    /// Mend's while the bundle lacks it.
    /// </summary>
    public static class ReviveCooldown
    {
        public const string EffectName = "EE_SE_ReviveCooldown";
        public const float Seconds = 300f;

        public static int Hash => EffectName.GetStableHashCode();

        public static StatusEffect Make()
        {
            SE_Stats effect = StatusTemplate.Make<SE_Stats>(EffectName);
            effect.m_name = Lang.Token("ee_se_revive_cooldown");
            effect.m_tooltip = Lang.Token("ee_se_revive_cooldown_tip");
            Sprite icon = CasterBundles.Sprite(CasterBundles.Spells, Spells.Revive.Icon);
            effect.m_icon = icon != null ? icon : CasterBundles.Sprite(CasterBundles.Spells, Spells.Mend.Icon);
            effect.m_ttl = Seconds;
            return effect;
        }
    }
}
