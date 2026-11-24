using LocalEffects;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Eir's Spring (features/casters.md, "Nine more spells"): a circle of <see cref="Radius"/> on the ground where the
    /// caster aims (<see cref="TipSpawn.AimPoint"/>, within <see cref="Reach"/>; at the caster's feet when the aim is
    /// farther or on nothing), healing every friend inside (players and tames, <see cref="FriendTarget.Friendly"/>) by
    /// <see cref="TickShare"/> of the wand's heal power every second for <see cref="Seconds"/>. The ticks are decided on the
    /// caster's machine (<see cref="SpringPool"/>) and delivered with <c>Character.Heal</c>, which the game sends to each
    /// healed one's owner. The ring (<c>ee_fx_cast_spring</c>, looping) is drawn on every machine near for the same
    /// seconds from one <see cref="CasterFx"/> message: the duration is the spell's own, so the message carries none.
    /// </summary>
    public static class Spring
    {
        public const float Radius = 4f;
        public const float Reach = 20f;
        public const float Seconds = 8f;
        public const float TickShare = 0.1f;

        // The shared chant ring (authored to 10 m) drawn to the spring's 4 m when the bundle lacks the spring's own.
        private const float FallbackFlash = 1.6f;
        private const string Sound = "sfx_Potion_health_Start";

        public static void Cast(Player caster, ItemDrop.ItemData wand, Spell spell)
        {
            Vector3 center = Center(caster);
            SpringPool.Open(caster, center, HealPower.Of(caster, wand) * TickShare);
            CasterFx.Send(HealReleases.Spring, center, ZDOID.None, spell);
        }

        private static Vector3 Center(Player caster)
        {
            Vector3 aim = TipSpawn.AimPoint(caster);
            return Vector3.Distance(aim, caster.transform.position) > Reach ? caster.transform.position : GroundPoint.Below(aim);
        }

        /// <summary>The ring on this machine for the spring's seconds, as authored; the gold chant ring at 4 m without it.</summary>
        public static void Show(Vector3 at)
        {
            GameObject prefab = CasterFx.Prepared(HealReleases.Spring);
            if (prefab == null)
                LocalEffect.Flash(CasterFx.Prepared(CasterFx.Chant), at, FallbackFlash);
            else
            {
                GameObject copy = LocalEffect.Attach(prefab, null, at, endless: true);
                if (copy != null)
                    Object.Destroy(copy, Seconds);
            }
            LocalEffect.Sound(ZNetScene.instance?.GetPrefab(Sound), at);
        }
    }
}
