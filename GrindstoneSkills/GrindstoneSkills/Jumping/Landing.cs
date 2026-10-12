using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The one SEMan.ModifyFallDamage postfix behind Soft Landing and Roll. The game calls it only from
    /// Character.UpdateGroundContact, for a player whose fall was higher than 4 m: baseDamage is the game's
    /// Clamp01((h - 4) / 16) x 100, damage is that after every status effect (each adds a share of the base, such as a
    /// feather cape's -100%), and the hit is made only when the result is above 0. m_maxAirAltitude and the position are
    /// still those of the landing during the call (the game resets them after), so the real fall height is read there.
    /// <para>The postfix recomputes the damage from that height with <see cref="SoftLanding"/>, asks
    /// <see cref="LandingRoll"/> to roll when the fall would still hurt, and scales the game's result by new base over old
    /// base: the status effects keep their share, and another mod's change to the same number still applies. A fall
    /// that is now safe ends at 0 and the game makes no hit.</para>
    /// <para>Runs on the player's own client: UpdateGroundContact runs on a character's owner, and the fall hit is applied
    /// there. Creatures, other players and a dedicated server return at the first test. Off, the damage is the game's.</para>
    /// </summary>
    public static class Landing
    {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyFallDamage))]
        private static class FallDamage
        {
            [HarmonyPostfix]
            private static void Postfix(SEMan __instance, float baseDamage, ref float damage)
            {
                Player player = Player.m_localPlayer;
                if (player == null || baseDamage <= 0f || !ReferenceEquals(__instance.m_character, player) || !GameSkillSettings.Jumping.Value)
                    return;
                damage = HookGuard.Run("soft landing", static fall => Land(fall.player, fall.baseDamage, fall.damage), (player, baseDamage, damage), damage);
            }
        }

        /// <summary>The fall damage after Soft Landing and Roll, from the game's base and its result after status effects.</summary>
        private static float Land(Player player, float baseDamage, float damage)
        {
            float height = Mathf.Max(0f, player.m_maxAirAltitude - player.transform.position.y);
            float factor = LocalSkill.Factor(Skills.SkillType.Jump);
            float soft = SoftLanding.Damage(height, factor);
            float scale = soft / baseDamage;
            if (damage * scale > 0f && LandingRoll.TryRoll(player))
                scale = LandingRoll.Damage(soft, height, factor) / baseDamage;
            return damage * scale;
        }
    }
}
