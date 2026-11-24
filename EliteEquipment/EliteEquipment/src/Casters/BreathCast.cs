using System.Collections;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Moder's Breath (features/casters.md, "Nine more spells"), a strike, on the caster's machine. When the cast lands
    /// (<see cref="SpellCast"/>, the eitr already paid) <see cref="Bursts"/> bursts of Moder's frost leave the wand's tip,
    /// each toward what the crosshair is on at that moment (<see cref="TipSpawn.AimPoint"/>): its look a networked
    /// <see cref="BreathLook"/> spawned and owned here, so every player sees it, and its hit <see cref="BreathCone"/>'s, decided
    /// here. Moder's breath sound plays once, with the first burst (the game's own networked
    /// <see cref="BreathLook.GameSound"/>). When the spell's motion plays (ee_spell_thrust) its six <c>Hit</c>s pace the bursts
    /// (the first here, the next five from <see cref="SpellHits"/> through <see cref="Burst"/>), else one every
    /// <see cref="Every"/> s. A caster who can no longer cast (dead, the wand or the book put away, swimming, teleporting,
    /// the switch off) breathes no more.
    /// </summary>
    public static class BreathCast
    {
        public const int Bursts = 6;
        public const float Every = 0.1f;
        public const string Charge = "ee_fx_charge_frost";

        private static readonly WaitForSeconds wait = new WaitForSeconds(Every);

        public static void Fire(Player caster, ItemDrop.ItemData wand, Spell spell)
        {
            if (BreathLook.Breath == null)
            {
                Plugin.Log.LogWarning($"EliteEquipment: {BreathLook.Prefab} was not made, the breath does nothing");
                return;
            }
            Sound(caster);
            if (SpellMotion.Drives(spell))
                Burst(caster, wand, spell);
            else
                caster.StartCoroutine(Breathe(caster, wand));
        }

        /// <summary>One burst now, while the caster can still cast (a <c>Hit</c> of the spell's own motion).</summary>
        public static void Burst(Player caster, ItemDrop.ItemData wand, Spell spell)
        {
            if (BreathLook.Breath != null && VolleyCast.CanCast(caster, wand))
                Breath(caster, wand);
        }

        private static IEnumerator Breathe(Player caster, ItemDrop.ItemData wand)
        {
            for (int burst = 0; burst < Bursts; burst++)
            {
                if (burst > 0)
                    yield return wait;
                if (!VolleyCast.CanCast(caster, wand))
                    yield break;
                CasterSafe.Run("breath burst", () => Breath(caster, wand));
            }
        }

        private static void Breath(Player caster, ItemDrop.ItemData wand)
        {
            Vector3 from = From(caster);
            Vector3 toAim = TipSpawn.AimPoint(caster) - from;
            Vector3 way = toAim.sqrMagnitude > 0.0001f ? toAim.normalized : caster.GetLookDir().normalized;
            Object.Instantiate(BreathLook.Breath, from, Quaternion.LookRotation(way));
            BreathCone.Burst(caster, wand, from, way);
        }

        private static void Sound(Player caster)
        {
            GameObject sound = ZNetScene.instance?.GetPrefab(BreathLook.GameSound);
            if (sound != null)
                Object.Instantiate(sound, From(caster), Quaternion.identity);
        }

        private static Vector3 From(Player caster)
        {
            Transform tip = TipSpawn.Tip(caster);
            return tip != null ? tip.position : caster.GetEyePoint();
        }
    }
}
