using LocalEffects;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Cleanse (features/casters.md, spell 8; user, 2026-11-02: "removes all debuffs from ally or self"), a heal-kind
    /// spell aimed exactly like Mend: the friend locked on to as the cast started (<see cref="HealTarget"/>, within 30 m),
    /// or the caster when none is, or that friend died or went out of reach. It lands at once, at the motion's
    /// <c>Hit</c>, with no flight: the harmful status effects go (<see cref="CleanseEffect"/>, run by the game on the
    /// target's owner) and a small heal, <see cref="HealShare"/> of the wand's heal power (<c>Character.Heal</c>, also
    /// sent to the owner by the game). Its release (<c>ee_fx_cast_cleanse</c>) plays on the target at its feet for every
    /// player near (<see cref="CasterFx"/>'s message, drawn by <see cref="Show"/>).
    /// </summary>
    public static class Cleanse
    {
        public const float HealShare = 0.2f;

        public static void Cast(Player caster, ItemDrop.ItemData wand, Spell spell, Character locked)
        {
            Character target = FriendTarget.StillValid(locked, caster) ? locked : caster;
            target.GetSEMan().AddStatusEffect(CleanseEffect.Hash, resetTime: true);
            target.Heal(HealPower.Of(caster, wand) * HealShare, true);
            CasterFx.Send(SpellFx.CastCleanse, target.transform.position, target.GetZDOID(), spell);
        }

        /// <summary>
        /// The release on this machine: on the target at its feet, following it, or where it was cast when the target is
        /// not loaded here; the heal's gold motes when the bundle lacks it.
        /// </summary>
        public static void Show(Vector3 at, Character target)
        {
            GameObject prefab = CasterFx.Prepared(SpellFx.CastCleanse);
            if (prefab == null && target != null)
            {
                CasterFx.MendOn(target);
                return;
            }
            if (target == null)
            {
                CasterFx.Here(SpellFx.CastCleanse, at);
                return;
            }
            LocalEffect.Attach(prefab, target.transform, target.transform.position, endless: false);
            LocalEffect.Sound(ZNetScene.instance?.GetPrefab(CasterSounds.For(SpellFx.CastCleanse)), target.transform.position);
        }
    }
}
