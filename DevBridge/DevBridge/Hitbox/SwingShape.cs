using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevBridge.Hitbox
{
    /// <summary>
    /// A melee swing's hit shape, worked out the way Attack.DoMeleeAttack casts it: from the origin joint (the
    /// character's root when the attack names none) raised by the attack height and moved sideways by the offset, one
    /// sphere of the ray width swept out to the attack range every 4 degrees across the attack angle (turned about the up
    /// axis for a horizontal swing, the side axis for a vertical one). Anything whose body crosses the drawn edge is hit.
    /// Drawn red at the swing's height and orange on the ground under it.
    /// </summary>
    internal static class SwingShape
    {
        private const float Step = 4f;
        private static readonly Color Edge = new Color(1f, 0.15f, 0.1f, 0.9f);
        private static readonly Color Ground = new Color(1f, 0.6f, 0.1f, 0.8f);

        internal static void Show(Attack attack)
        {
            attack.GetMeleeAttackDir(out Transform joint, out Vector3 aim);
            Transform body = attack.m_character.transform;
            Vector3 origin = joint.position + Vector3.up * attack.m_attackHeight + body.right * attack.m_attackOffset;
            List<Vector3> reach = Sweep(attack, aim).Select(direction => origin + direction * attack.m_attackRange).ToList();
            Lines.Draw(Outline(origin, reach, attack.m_attackAngle), Edge, HitboxView.Seconds);
            float floor = body.position.y + 0.05f;
            Lines.Draw(Outline(origin, reach, attack.m_attackAngle).Select(p => new Vector3(p.x, floor, p.z)).ToList(), Ground, HitboxView.Seconds);
            Report(attack);
        }

        private static IEnumerable<Vector3> Sweep(Attack attack, Vector3 aim)
        {
            Transform body = attack.m_character.transform;
            Vector3 local = body.InverseTransformDirection(aim);
            float half = attack.m_attackAngle / 2f;
            for (float angle = -half; angle <= half; angle += Step)
            {
                Quaternion turn = attack.m_attackType == Attack.AttackType.Horizontal ? Quaternion.Euler(0f, -angle, 0f)
                    : attack.m_attackType == Attack.AttackType.Vertical ? Quaternion.Euler(angle, 0f, 0f) : Quaternion.identity;
                yield return body.TransformDirection(turn * local);
            }
        }

        // A full circle closes on itself; anything narrower is a wedge from the origin out to the swept edge and back.
        private static List<Vector3> Outline(Vector3 origin, List<Vector3> reach, float angle)
        {
            if (angle >= 360f) return reach.Concat(new[] { reach[0] }).ToList();
            return new[] { origin }.Concat(reach).Concat(new[] { origin }).ToList();
        }

        private static void Report(Attack attack)
        {
            var entry = new Dictionary<string, object>
            {
                ["attacker"] = HitboxView.Name(attack.m_character),
                ["attack"] = attack.m_weapon?.m_shared?.m_name ?? "?",
                ["shape"] = $"{attack.m_attackType} {attack.m_attackAngle:0} deg",
                ["range"] = Fmt.R(attack.m_attackRange),
                ["width"] = Fmt.R(attack.m_attackRayWidth),
                ["height"] = Fmt.R(attack.m_attackHeight),
            };
            Player me = Player.m_localPlayer;
            if (me && me != attack.m_character) (entry["you_distance"], entry["you_gap"]) = Rounded(HitboxView.Apart(attack.m_character, me));
            HitboxView.Record("swing", entry);
        }

        internal static (float, float) Rounded((float a, float b) pair) => (Fmt.R(pair.a), Fmt.R(pair.b));
    }
}
