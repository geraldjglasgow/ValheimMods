using System.Collections;
using System.Collections.Generic;
using LocalEffects;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Sanctuary (features/casters.md, "Nine more spells"; The Queen's page, <see cref="BossPages"/>): a dome of
    /// <see cref="Radius"/> metres at the caster's feet for <see cref="Seconds"/>. The caster's machine runs it (a coroutine
    /// on the caster, so it ends early should the caster die or leave): every <see cref="Every"/> seconds each friend inside
    /// (players and tames, <see cref="FriendTarget.Friendly"/>) gets <see cref="SanctuaryEffect"/>, 30% less damage taken,
    /// through the game's <c>SEMan.AddStatusEffect</c> (sent to each one's owner); it lasts
    /// <see cref="SanctuaryEffect.Linger"/> past its last refresh, so it ends shortly after they step out or the dome ends.
    /// The cast puts <see cref="SanctuaryEffect.CooldownName"/> on the caster for <see cref="Cooldown"/> seconds, and no
    /// Sanctuary starts while it is on (<see cref="Waits"/>, before any eitr is paid). The dome (<c>ee_fx_cast_sanctuary</c>)
    /// is drawn where it stands for every player near and ended at <see cref="Seconds"/> (<see cref="Show"/>).
    /// </summary>
    public static class Sanctuary
    {
        public const float Radius = 6f;
        public const float Seconds = 10f;
        public const float Every = 0.5f;
        public const float Cooldown = 120f;

        private static readonly List<Character> near = new List<Character>();

        /// <summary>The caster's next Sanctuary is not ready yet.</summary>
        public static bool Waits(Player caster, Spell spell) =>
            spell == Spells.Sanctuary && caster.GetSEMan().HaveStatusEffect(SanctuaryEffect.CooldownHash);

        public static void Cast(Player caster, Spell spell)
        {
            Vector3 center = caster.transform.position;
            caster.GetSEMan().AddStatusEffect(SanctuaryEffect.CooldownHash, resetTime: true);
            caster.StartCoroutine(Dome(caster, center));
            CasterFx.Send(SpellFx.CastSanctuary, center, caster.GetZDOID(), spell);
        }

        private static IEnumerator Dome(Player caster, Vector3 center)
        {
            float end = Time.time + Seconds;
            var wait = new WaitForSeconds(Every);
            while (Time.time < end && caster != null && !caster.IsDead())
            {
                CasterSafe.Run("sanctuary", () => Shelter(caster, center));
                yield return wait;
            }
        }

        private static void Shelter(Player caster, Vector3 center)
        {
            near.Clear();
            Character.GetCharactersInRange(center, Radius, near);
            foreach (Character character in near)
            {
                if (FriendTarget.Friendly(character, caster))
                    character.GetSEMan().AddStatusEffect(SanctuaryEffect.Hash, resetTime: true);
            }
            near.Clear();
        }

        /// <summary>The dome on this machine where it was cast, for its seconds: null when drawn, else the shared ring to draw.</summary>
        public static string Show(Vector3 at)
        {
            GameObject prefab = CasterFx.Prepared(SpellFx.CastSanctuary);
            if (prefab == null)
                return CasterFx.Chant;
            GameObject copy = LocalEffect.Attach(prefab, null, at, endless: true);
            if (copy != null)
                Object.Destroy(copy, Seconds);
            ReleaseDraws.Sound(SpellFx.CastSanctuary, at);
            return null;
        }
    }
}
