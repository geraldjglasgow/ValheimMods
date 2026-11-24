using StatusTemplates;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Ward's shield: the game's own <c>SE_Shield</c> (the Staff of Protection's, which takes all of every hit until the
    /// damage it took passes what it absorbs), made as our own with our numbers: no fixed amount, the caster's (sent as the
    /// skill level, <see cref="Ward"/>); <see cref="Ward.Seconds"/> whatever the item level; no world-level amount and no
    /// skill raised as it breaks. The game shield's hit and break effects and, until ours is in the bundle, its icon are
    /// borrowed (<see cref="GameBorrow"/>). Its bubble is not the game's networked start effect but our own, drawn by every
    /// machine near (<see cref="WardBubble"/>), so as the ward ends on the owner (broken, run out, removed, its wearer
    /// dead) it tells them. Cast again on a warded friend it starts whole. In every item database with the casters' other
    /// effects (<see cref="CasterStatus"/>).
    /// </summary>
    public sealed class WardEffect : SE_Shield
    {
        public const string PrefabName = "EE_SE_Ward";

        public static int Hash => PrefabName.GetStableHashCode();

        /// <summary>The one made, put in every item database (null before the first).</summary>
        public static WardEffect Template { get; private set; }

        public static WardEffect Make(Spell spell)
        {
            WardEffect effect = StatusTemplate.Make<WardEffect>(PrefabName);
            effect.m_name = spell.Token;
            effect.m_tooltip = "";
            effect.m_icon = CasterBundles.Sprite(CasterBundles.Spells, spell.Icon);
            effect.m_ttl = Ward.Seconds;
            effect.m_ttlPerItemLevel = 0;
            effect.m_absorbDamage = 0f;
            effect.m_absorbDamageWorldLevel = 0f;
            effect.m_absorbDamagePerSkillLevel = 1f;
            effect.m_levelUpSkillOnBreak = Skills.SkillType.None;
            Template = effect;
            return effect;
        }

        /// <summary>The game shield's hit and break effects, and its icon while ours is missing.</summary>
        public void Borrow(SE_Shield game)
        {
            m_hitEffects = game.m_hitEffects;
            m_breakEffects = game.m_breakEffects;
            if (m_icon == null)
                m_icon = game.m_icon;
        }

        /// <summary>On the owner as the ward goes on, or goes on again: the amount, and a fresh shield.</summary>
        public override void SetLevel(int itemLevel, float skillLevel)
        {
            m_damage = 0f;
            base.SetLevel(itemLevel, skillLevel);
        }

        public override void Stop()
        {
            base.Stop();
            Character warded = m_character;
            if (warded != null)
                CasterSafe.Run("ward end", () => WardBubble.SendEnd(warded));
        }
    }
}
