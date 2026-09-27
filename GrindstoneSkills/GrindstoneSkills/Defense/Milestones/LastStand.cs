using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Last Stand, the level 100 milestone, on the local player's own client, in <see cref="DamageIntake"/>: when
    /// damage (from anything) would take the last of the player's health and Last Stand is ready, the damage is scaled
    /// down to leave exactly 1 health, the player takes no damage at all for Last Stand Invulnerability seconds, and
    /// Last Stand rests for Last Stand Cooldown seconds. "Last Stand!" shows in the middle of the screen, and icons show
    /// the invulnerability and the cooldown (<see cref="DefenseEffects"/>). A player already at 1 health or less is
    /// kept at their health. God mode is left to the game. The cooldown is in memory: a relog resets it.
    /// </summary>
    public static class LastStand
    {
        private static float invulnerableUntil = float.NegativeInfinity;
        private static float readyAt = float.NegativeInfinity;

        public static bool Invulnerable => Time.time < invulnerableUntil;

        public static float InvulnerableFor => Mathf.Max(0f, invulnerableUntil - Time.time);

        /// <summary>Seconds until Last Stand is ready again, 0 when it is.</summary>
        public static float CooldownLeft => Mathf.Max(0f, readyAt - Time.time);

        public static bool Unlocked => DefenseSkill.LocalReached(DefenseMilestoneSettings.LastStandLevel.Value);

        /// <summary>Called with the hit already reduced; keeps it from killing the player when Last Stand is ready.</summary>
        public static void Check(Player player, HitData hit)
        {
            float health = player.GetHealth();
            float damage = DamageIntake.Final(hit);
            if (health <= 0f || damage < health || !Unlocked || CooldownLeft > 0f || player.InGodMode())
                return;
            DamageIntake.Scale(hit, Mathf.Max(0f, health - 1f) / damage);
            Trigger(player);
        }

        private static void Trigger(Player player)
        {
            invulnerableUntil = Time.time + Mathf.Max(0f, DefenseMilestoneSettings.LastStandInvulnerability.Value);
            readyAt = Time.time + DefenseMilestoneSettings.LastStandCooldown.Value;
            player.Message(MessageHud.MessageType.Center, "Last Stand!");
            DefenseEffects.Refresh();
        }
    }
}
