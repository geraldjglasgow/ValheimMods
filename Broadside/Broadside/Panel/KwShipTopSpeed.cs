using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How fast the ship would go right now (the user, 2026-10-26: "What my top speed will be depending on how the wind is
    /// and how my sails are down or up ... I want to know how fast I could go"): the speed at which the push meets the drag,
    /// as the launched ship's own step has them (<see cref="KwShipDrive"/>), steering straight on its present heading.
    /// Under sail, each kind of sail's push by the apparent wind at that speed (<see cref="KwRigDamageDrive.Push"/>: the
    /// game's wind now, its holed or fallen sails giving less) times the wind's strength, the ship's sail force and the
    /// sail set (half sail half of it, its upper sails furled); under oars the oars' push. Both times the helmsman's
    /// Sailing speed factor (GrindstoneSkills, which raises the push inside the step). Pure arithmetic, local to the screen.
    /// </summary>
    internal static class KwShipTopSpeed
    {
        private const int Steps = 30;

        /// <summary>The top speed, m/s, at a speed setting: full or half sail, rowing ahead or astern, 0 stopped.</summary>
        public static float At(Ship ship, Ship.Speed setting, float skill)
        {
            if (setting == Ship.Speed.Slow || setting == Ship.Speed.Back)
                return Rowing(ship) * skill;
            if (setting != Ship.Speed.Full && setting != Ship.Speed.Half)
                return 0f;
            KwShipWind? wind = ship.GetComponent<KwShipWind>();
            return wind == null || EnvMan.instance == null ? 0f : Sailing(ship, wind.Mix, setting == Ship.Speed.Half) * skill;
        }

        // Under oars: the oars' push a physics step (their force by the step) against the drag's share of the speed squared.
        private static float Rowing(Ship ship)
        {
            float drag = Mathf.Max(ship.m_dampingForward * KwShipDrive.DragShare, 1e-9f);
            return Mathf.Sqrt(Mathf.Max(0f, ship.m_backwardForce) * Time.fixedDeltaTime / drag);
        }

        // Under sail: the speed where the sails' push ahead (by the apparent wind at that speed) meets the drag, halved to it.
        private static float Sailing(Ship ship, KwSailMix mix, bool half)
        {
            float drag = Mathf.Max(ship.m_dampingForward * KwShipDrive.DragShare, 1e-9f), lo = 0f, hi = 45f;
            if (Excess(ship, mix, half, drag, 0.01f) <= 0f)
                return 0f;
            for (int i = 0; i < Steps; i++)
            {
                float mid = (lo + hi) / 2f;
                if (Excess(ship, mix, half, drag, mid) > 0f)
                    lo = mid;
                else
                    hi = mid;
            }
            return lo;
        }

        // The sails' push ahead less the drag at a speed straight ahead: the speed change a physics step.
        private static float Excess(Ship ship, KwSailMix mix, bool half, float drag, float speed)
        {
            Vector3 forward = Vector3.ProjectOnPlane(ship.transform.forward, Vector3.up).normalized;
            KwApparentWind air = KwShipWind.Air(forward * speed, forward);
            float ahead = Vector3.Dot(KwRigDamageDrive.Push(ship, mix, air, half), forward);
            return ahead * air.Strength * ship.m_sailForceFactor * (half ? 0.5f : 1f) - drag * speed * speed;
        }
    }
}
