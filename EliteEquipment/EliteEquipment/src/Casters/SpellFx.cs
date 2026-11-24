using LocalEffects;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The spells' own charge and release effects (features/casters.md, "Spell animations"; the workshop's
    /// <c>ee_casterfx_meadows</c> BRIEF, "Spell charges and releases"), drawn as local copies on each machine that draws:
    /// the charge from the cast's start to its <c>Hit</c>, at the wand's tip or on the ground under the caster as it is
    /// authored (<see cref="Charge"/>, from <see cref="SpellMotion"/>); the release where the cast lands, the ground ones at
    /// the caster's feet turned to the caster's facing and the ring scaled to the chant's reach (<see cref="Draw"/>, from
    /// <see cref="CasterFx"/>'s message). A spell with its own effects no longer shows the shared cast flash (its sound
    /// stays); the shared white charge, flash and chant ring stay as the fallback when the bundle lacks an effect.
    /// </summary>
    public static class SpellFx
    {
        public const string ChargeMend = "ee_fx_charge_mend";
        public const string ChargeVolley = "ee_fx_charge_volley";
        public const string ChargeHearth = "ee_fx_charge_hearth";
        public const string ChargeSwift = "ee_fx_charge_swift";
        public const string CastHearth = "ee_fx_cast_hearth";
        public const string CastSwift = "ee_fx_cast_swift";
        public const string CastVolley = "ee_fx_cast_volley";
        public const string ChargeCleanse = "ee_fx_charge_cleanse";
        public const string CastCleanse = "ee_fx_cast_cleanse";
        public const string ChargeWard = "ee_fx_charge_ward";
        public const string CastWard = "ee_fx_cast_ward";
        public const string CastSanctuary = "ee_fx_cast_sanctuary";
        public const string ChargeWar = "ee_fx_charge_war";
        public const string CastWarchant = "ee_fx_cast_warchant";
        public const string ChargeFeather = "ee_fx_charge_feather";
        public const string CastFeatherfall = "ee_fx_cast_featherfall";

        // The release rings are authored rolling out to this many metres.
        private const float AuthoredReach = 10f;

        /// <summary>The spell has its own effects here: its release (or, for Mend, whose release is its mend bolt, its charge).</summary>
        public static bool Own(Spell spell)
        {
            string own = spell == null ? null : spell.ReleaseEffect ?? spell.ChargeEffect;
            return own != null && CasterFx.Prepared(own) != null;
        }

        /// <summary>
        /// A caster effect message drawn the spell's own way: null when drawn here, else the effect to draw the shared way
        /// (itself, or the shared chant ring when a release is missing from the bundle).
        /// </summary>
        public static string Draw(string effect, Vector3 at, Character caster, Spell spell)
        {
            if (effect == CasterFx.Cast && Own(spell))
            {
                LocalEffect.Sound(ZNetScene.instance?.GetPrefab(CasterSounds.For(CasterFx.Cast)), at);
                return null;
            }
            if (ReleaseDraws.Draw(effect, at, caster, out string shared))
                return shared;
            if (PointRelease(effect, at, caster))
                return null;
            if (effect != CastHearth && effect != CastSwift)
                return effect;
            GameObject prefab = CasterFx.Prepared(effect);
            if (prefab == null)
                return CasterFx.Chant;
            Ground(prefab, at, caster);
            LocalEffect.Sound(ZNetScene.instance?.GetPrefab(CasterSounds.For(effect)), at);
            return null;
        }

        /// <summary>
        /// A release drawn as authored at one spot: a volley's burst at the tip, or Cleanse's splash on its target
        /// (<paramref name="character"/>, the message's character: for Cleanse the one cleansed).
        /// </summary>
        private static bool PointRelease(string effect, Vector3 at, Character character)
        {
            if (effect == CastVolley)
                CasterFx.Here(effect, at);
            else if (effect == CastCleanse)
                Cleanse.Show(at, character);
            else if (!HealReleases.Draw(effect, at, character))
                return false;
            return true;
        }

        /// <summary>A ground release where it was cast, its +Z the caster's facing, its ring rolling out to the chant's reach.</summary>
        public static void Ground(GameObject prefab, Vector3 at, Character caster)
        {
            GameObject copy = LocalEffect.AttachScaled(prefab, null, at, endless: false, Spell.ChantRange / AuthoredReach);
            if (copy == null || caster == null)
                return;
            Vector3 facing = Vector3.ProjectOnPlane(caster.transform.forward, Vector3.up);
            if (facing.sqrMagnitude > 0.0001f)
                copy.transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
        }

        /// <summary>
        /// The spell's charge on a caster, as long as it is kept: on the ground under the feet (following the caster) or at
        /// the wand's tip, drawn at its authored size; the shared white charge at the tip when the bundle lacks it.
        /// </summary>
        public static GameObject Charge(Player caster, Spell spell)
        {
            GameObject prefab = spell.ChargeEffect != null ? CasterFx.Prepared(spell.ChargeEffect) : null;
            if (prefab != null && AtFeet(spell.ChargeEffect))
                return LocalEffect.Attach(prefab, caster.transform, caster.transform.position, endless: true);
            Transform tip = TipSpawn.Tip(caster);
            if (tip == null)
                return null;
            GameObject copy = LocalEffect.Attach(prefab != null ? prefab : CasterFx.Prepared(CasterFx.Charge), tip, tip.position, endless: true);
            if (copy == null)
                return null;
            ChargeOrb.ScaleWithParent(copy);
            copy.transform.localScale = Vector3.one / Mathf.Max(0.001f, tip.lossyScale.x);
            return copy;
        }

        public static void Remove(GameObject charge)
        {
            if (charge != null)
                Object.Destroy(charge);
        }

        private static bool AtFeet(string effect) =>
            effect == ChargeHearth || effect == ChargeSwift || effect == ChargeWar || effect == ChargeFeather;
    }
}
