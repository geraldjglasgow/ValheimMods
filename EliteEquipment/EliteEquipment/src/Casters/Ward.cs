namespace EliteEquipment.Casters
{
    /// <summary>
    /// Ward (features/casters.md, "Nine more spells"), a ward-kind spell aimed exactly like Mend: the friend locked on to
    /// as the cast started (<see cref="HealTarget"/>, within 30 m), or the caster when none is, or that friend died or went
    /// out of reach. It lands at once, at the motion's <c>Hit</c>: the game's own shield (<see cref="WardEffect"/>, a copy
    /// of the Staff of Protection's <c>SE_Shield</c>) absorbing <see cref="AbsorbShare"/> times the wand's heal power for
    /// <see cref="Seconds"/>, put on by the target's owner (the game's <c>SEMan.AddStatusEffect</c> sends it there, the
    /// amount riding as its skill level). Its bubble (<c>ee_fx_cast_ward</c>) is drawn on the target for every player near
    /// while it lasts (<see cref="WardBubble"/>).
    /// </summary>
    public static class Ward
    {
        public const float AbsorbShare = 2f;
        public const float Seconds = 20f;

        public static void Cast(Player caster, ItemDrop.ItemData wand, Spell spell, Character locked)
        {
            Character target = FriendTarget.StillValid(locked, caster) ? locked : caster;
            float absorb = HealPower.Of(caster, wand) * AbsorbShare;
            target.GetSEMan().AddStatusEffect(WardEffect.Hash, resetTime: true, itemLevel: 0, skillLevel: absorb);
            CasterFx.Send(SpellFx.CastWard, target.transform.position, target.GetZDOID(), spell);
        }
    }
}
