using System;
using LocalEffects;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A heal flying from the wand to the friend it locked on to (user, 2026-10-09: "casting should like lock on hit them
    /// with heal"): a local object, on each machine near, that homes on the friend's middle at <see cref="Speed"/> carrying
    /// the workshop's gold mote (<c>ee_fx_mendbolt</c>), then calls its landing (the caster's machine heals there; every
    /// machine plays the heal's motes) and goes. Without the mote it still flies and lands, unseen. A friend that is gone
    /// before it lands ends the flight with a null landing; it never flies longer than <see cref="MaxSeconds"/>.
    /// </summary>
    public sealed class HealFlight : MonoBehaviour
    {
        private const float Speed = 25f;
        private const float Arrive = 0.3f;
        private const float MaxSeconds = 3f;

        private Character target;
        private Action<Character> land;
        private float started;

        public static void Launch(Vector3 from, Character target, Action<Character> land)
        {
            var go = new GameObject("EE_HealFlight");
            go.transform.position = from;
            HealFlight flight = go.AddComponent<HealFlight>();
            flight.target = target;
            flight.land = land;
            flight.started = Time.time;
            LocalEffect.Attach(CasterFx.Prepared(CasterFx.MendBolt), go.transform, from, endless: true);
        }

        private void Update()
        {
            if (target == null || target.IsDead() || Time.time - started > MaxSeconds)
            {
                Land(target != null && !target.IsDead() ? target : null);
                return;
            }
            Vector3 aim = target.GetCenterPoint();
            transform.position = Vector3.MoveTowards(transform.position, aim, Speed * Time.deltaTime);
            if ((aim - transform.position).sqrMagnitude <= Arrive * Arrive)
                Land(target);
        }

        private void Land(Character at)
        {
            Action<Character> landing = land;
            land = null;
            Object.Destroy(gameObject);
            if (landing != null)
                CasterSafe.Run("heal landing", () => landing(at));
        }
    }
}
