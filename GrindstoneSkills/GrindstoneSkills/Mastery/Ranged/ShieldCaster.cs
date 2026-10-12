using System.Runtime.CompilerServices;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Who cast a shield, for <see cref="ShieldXp"/>. Runs on the shielded character's owner (a player's own client, or
    /// whichever machine owns a creature, a dedicated server included). The game keeps no caster on a shield. Read from the
    /// game code 2026-10-11: a staff's shield spell reaches each character as a hit carrying the shield's status effect
    /// (Aoe.OnHit, with the caster as the hit's attacker); Character.RPC_Damage, on that character's owner, adds the effect
    /// (or renews it) and then hands it the attacker through StatusEffect.SetAttacker, which SE_Shield leaves empty. A
    /// postfix there notes the caster's character (its ZDOID) when the caster is a player, beside that shield in a weak table,
    /// so the note goes with the effect. A shield given any other way (a parry's, a creature's spell, one a mod adds
    /// directly) has no player caster and trains nobody; recasting on a shielded character hands the shield to the newest
    /// caster. With the switch off nothing is noted.
    /// </summary>
    public static class ShieldCaster
    {
        private sealed class Caster
        {
            public ZDOID Character;
        }

        private static readonly ConditionalWeakTable<SE_Shield, Caster> casters = new ConditionalWeakTable<SE_Shield, Caster>();

        /// <summary>The character of the player who cast this shield; false when no player cast it.</summary>
        public static bool TryGet(SE_Shield shield, out ZDOID caster)
        {
            caster = casters.TryGetValue(shield, out Caster found) ? found.Character : ZDOID.None;
            return !caster.IsNone();
        }

        [HarmonyPatch(typeof(StatusEffect), nameof(StatusEffect.SetAttacker))]
        private static class Handed
        {
            [HarmonyPostfix]
            private static void Postfix(StatusEffect __instance, Character attacker)
            {
                if (__instance is SE_Shield shield && attacker is Player && GameSkillSettings.WeaponMastery.Value)
                    HookGuard.Run("shield caster", static cast => Note(cast.shield, cast.attacker), (shield, attacker));
            }
        }

        private static void Note(SE_Shield shield, Character caster) =>
            casters.GetOrCreateValue(shield).Character = caster.GetZDOID();
    }
}
