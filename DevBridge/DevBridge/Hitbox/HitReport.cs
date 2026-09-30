using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.Hitbox
{
    /// <summary>
    /// A hit arriving at the local player, whatever made it (a melee swing, a projectile, a mod's own area damage): a
    /// yellow line along the ground from the attacker's centre to the player's with the player's body outlined, and a log
    /// entry with the damage, the attack the attacker was in, and how far apart the two stood.
    /// </summary>
    internal static class HitReport
    {
        private static readonly Color Line = new Color(1f, 0.9f, 0.1f, 0.95f);

        internal static void Record(Character player, HitData hit)
        {
            Character attacker = hit.GetAttacker();
            var entry = new Dictionary<string, object>
            {
                ["attacker"] = attacker ? HitboxView.Name(attacker) : "(none)",
                ["attack"] = AttackName(attacker),
                ["damage"] = Fmt.R(hit.GetTotalDamage()),
                ["dodged"] = player.IsDodgeInvincible(),
            };
            if (attacker) Measure(entry, attacker, player);
            HitboxView.Record("hit", entry);
        }

        private static void Measure(Dictionary<string, object> entry, Character attacker, Character player)
        {
            (entry["distance"], entry["gap"]) = SwingShape.Rounded(HitboxView.Apart(attacker, player));
            entry["above"] = Fmt.R(player.transform.position.y - attacker.transform.position.y);
            Vector3 from = attacker.transform.position + Vector3.up * 0.1f, to = player.transform.position + Vector3.up * 0.1f;
            float seconds = HitboxView.Seconds * 2f;
            Lines.Draw(new[] { from, to }, Line, seconds, 0.06f);
            Lines.Draw(Lines.Ring(to, player.GetRadius()), Line, seconds);
        }

        // The attack the attacker is in, or the one it just finished (a projectile or a lingering area lands after it).
        private static string AttackName(Character attacker)
        {
            Humanoid humanoid = attacker as Humanoid;
            if (humanoid == null) return "(not a humanoid)";
            Attack attack = humanoid.m_currentAttack ?? humanoid.m_previousAttack;
            return attack?.m_weapon?.m_shared?.m_name ?? "(none)";
        }
    }
}
