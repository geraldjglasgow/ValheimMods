using System.Collections;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Elder's Volley (features/casters.md, spell 7; user, 2026-11-02: "the elder's attack where things shoot fast out of
    /// his hand"), the strike, on the caster's machine. When the cast lands (<see cref="SpellCast"/>, the eitr already paid)
    /// <see cref="Shots"/> of the Elder's seeds (<see cref="VolleySeeds"/>) leave the wand's tip, the first at once and one
    /// every <see cref="Every"/> s after, each toward what the crosshair is on at that moment (<see cref="TipSpawn.AimPoint"/>)
    /// turned at random by up to <see cref="Spread"/> degrees each way, up and across, at the Elder's shot's speed, with
    /// <see cref="VolleyHit"/>'s hit. The caster's machine owns the seeds and decides their hits by the game's projectile
    /// rules; every player sees them by the game's own sync. A caster who can no longer cast (dead, the wand or the book put
    /// away, swimming, teleporting, the switch off) fires no more of them. The volley's release effect
    /// (<see cref="Spell.ReleaseEffect"/>, <c>ee_fx_cast_volley</c>) is shown at the tip with every seed. When the volley's
    /// own motion plays (<see cref="SpellMotion"/>, ee_spell_thrust) its six <c>Hit</c>s pace the seeds instead of the timer:
    /// the first here, the next five from <see cref="SpellHits"/> through <see cref="Seed"/>, one per recoil.
    /// </summary>
    public static class VolleyCast
    {
        public const int Shots = 6;
        public const float Every = 0.1f;
        public const float Spread = 1.5f;

        private static readonly WaitForSeconds wait = new WaitForSeconds(Every);

        public static void Fire(Player caster, ItemDrop.ItemData wand, Spell spell)
        {
            if (VolleySeeds.Seed == null)
            {
                Plugin.Log.LogWarning($"EliteEquipment: {VolleySeeds.Prefab} was not made, the volley fires nothing");
                return;
            }
            if (SpellMotion.Drives(spell))
                Seed(caster, wand, spell);
            else
                caster.StartCoroutine(Volley(caster, wand, spell));
        }

        /// <summary>One seed now, while the caster can still cast (a <c>Hit</c> of the volley's own motion).</summary>
        public static void Seed(Player caster, ItemDrop.ItemData wand, Spell spell)
        {
            if (VolleySeeds.Seed != null && CanCast(caster, wand))
                Shoot(caster, wand, spell);
        }

        private static IEnumerator Volley(Player caster, ItemDrop.ItemData wand, Spell spell)
        {
            for (int shot = 0; shot < Shots; shot++)
            {
                if (shot > 0)
                    yield return wait;
                if (!CanCast(caster, wand))
                    yield break;
                CasterSafe.Run("volley seed", () => Shoot(caster, wand, spell));
            }
        }

        public static bool CanCast(Player caster, ItemDrop.ItemData wand) =>
            caster != null && caster == Player.m_localPlayer && CastersSettings.On && !caster.IsDead()
            && !caster.IsSwimming() && !caster.IsTeleporting() && CasterGearWorn.Wand(caster) == wand
            && CasterGearWorn.Book(caster) != null;

        private static void Shoot(Player caster, ItemDrop.ItemData wand, Spell spell)
        {
            Transform tip = TipSpawn.Tip(caster);
            Vector3 from = tip != null ? tip.position : caster.GetEyePoint();
            Vector3 way = Turned(TipSpawn.AimPoint(caster) - from, caster.GetLookDir());
            GameObject seed = Object.Instantiate(VolleySeeds.Seed, from, Quaternion.LookRotation(way));
            Projectile projectile = seed.GetComponent<Projectile>();
            projectile.Setup(caster, way * VolleySeeds.Speed, -1f, VolleyHit.For(caster, wand), wand, null);
            if (spell.ReleaseEffect != null)
                CasterFx.Send(spell.ReleaseEffect, from, ZDOID.None, spell);
        }

        /// <summary>The way to the aim, turned at random by up to <see cref="Spread"/> degrees across and up.</summary>
        private static Vector3 Turned(Vector3 toAim, Vector3 fallback)
        {
            Vector3 aim = toAim.sqrMagnitude > 0.0001f ? toAim.normalized : fallback.normalized;
            Vector3 side = Vector3.Cross(aim, Vector3.up);
            Quaternion up = side.sqrMagnitude > 0.0001f
                ? Quaternion.AngleAxis(Random.Range(-Spread, Spread), side.normalized)
                : Quaternion.identity;
            return Quaternion.AngleAxis(Random.Range(-Spread, Spread), Vector3.up) * (up * aim);
        }
    }
}
