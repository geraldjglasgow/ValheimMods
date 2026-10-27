using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.Hitbox
{
    /// <summary>
    /// An area attack's hit shape, worked out the way Attack.DoAreaAttack casts it: a sphere of the ray width round the
    /// origin joint (the character's root when the attack names none), raised by the attack height, moved forward by the
    /// range and sideways by the offset. Anything whose body reaches into the sphere is hit. Drawn red as three rings
    /// round the centre, orange as its shadow on the ground under it, like a swing.
    /// </summary>
    internal static class AreaShape
    {
        private static readonly Color Edge = new Color(1f, 0.15f, 0.1f, 0.9f);
        private static readonly Color Ground = new Color(1f, 0.6f, 0.1f, 0.8f);

        internal static void Show(Attack attack)
        {
            Transform body = attack.m_character.transform;
            Vector3 centre = attack.GetAttackOrigin().position + Vector3.up * attack.m_attackHeight
                + body.forward * attack.m_attackRange + body.right * attack.m_attackOffset;
            float radius = attack.m_attackRayWidth;
            foreach (Vector3[] ring in Rings(centre, radius, body))
            {
                Lines.Draw(ring, Edge, HitboxView.Seconds);
            }
            Lines.Draw(Lines.Ring(new Vector3(centre.x, body.position.y + 0.05f, centre.z), radius), Ground, HitboxView.Seconds);
            Report(attack, centre);
        }

        /// <summary>The level ring and two upright ones, along and across the attacker's facing.</summary>
        private static IEnumerable<Vector3[]> Rings(Vector3 centre, float radius, Transform body)
        {
            yield return Lines.Ring(centre, radius);
            foreach (Vector3 axis in new[] { body.forward, body.right })
            {
                var ring = new Vector3[25];
                for (int i = 0; i <= 24; i++)
                {
                    float angle = i * Mathf.PI * 2f / 24f;
                    ring[i] = centre + (Mathf.Cos(angle) * axis + Mathf.Sin(angle) * Vector3.up) * radius;
                }
                yield return ring;
            }
        }

        private static void Report(Attack attack, Vector3 centre)
        {
            var entry = new Dictionary<string, object>
            {
                ["attacker"] = HitboxView.Name(attack.m_character),
                ["attack"] = attack.m_weapon?.m_shared?.m_name ?? "?",
                ["shape"] = "Area" + (attack.m_attackOriginJoint.Length > 0 ? " at " + attack.m_attackOriginJoint : ""),
                ["radius"] = Fmt.R(attack.m_attackRayWidth),
                ["centre"] = new[] { Fmt.R(centre.x), Fmt.R(centre.y), Fmt.R(centre.z) },
            };
            Player me = Player.m_localPlayer;
            if (me && me != attack.m_character) entry["you_from_centre"] = Fmt.R(Vector3.Distance(me.GetCenterPoint(), centre));
            HitboxView.Record("area", entry);
        }
    }
}
