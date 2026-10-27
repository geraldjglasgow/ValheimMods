using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A rudder's steering gear drawn on its rudder object (a placed rudder on the dock, or a launched ship's copy) on every
    /// peer with a screen, from the gear its ZDO or its blueprint holds (<see cref="KwRudderGear"/>): every shaft a part of
    /// its own, turned about its way by <see cref="KwGearTrain.Spin"/> of the rudder's angle (the quadrant on the stock turns
    /// with the blade itself), and the hangers and the bush under the wheel's deck, which stay put (<see cref="KwGearParts"/>).
    /// The angle is replicated state: on the dock the blade's, which follows the wheel's helm (<see cref="KwRudderBlade"/>);
    /// on a launched ship the ship's synced rudder value, the blade set to it here as the wheel is
    /// (<see cref="KwShipWheel"/>), so the game's sculling wiggle at slow speed never turns the gear without the wheel. A
    /// refusal is kept too, for the wheel's hover (<see cref="Words"/>). Nothing is drawn on a dedicated server.
    /// </summary>
    public sealed class KwRudderGearLook : MonoBehaviour
    {
        public const string Name = "gear";
        private const float Near = 0.6f;
        private static readonly List<KwRudderGearLook> All = new List<KwRudderGearLook>();

        private readonly List<(Transform Part, Vector3 Way)> runs = new List<(Transform Part, Vector3 Way)>();
        private Transform? blade;
        private Ship? ship;
        private float last = float.NaN;

        /// <summary>The gear it draws, or the refusal it keeps for the wheel's hover.</summary>
        public KwRudderGear Gear { get; private set; } = KwRudderGear.Refused(Vector3.zero, KwGearWhy.Route);

        /// <summary>A rudder's gear set (none: taken away), drawn when it is linked and there is a screen.</summary>
        public static void Set(GameObject rudder, KwRudderGear? gear)
        {
            KwRudderGearLook? look = rudder.GetComponent<KwRudderGearLook>();
            if (gear == null)
            {
                if (look != null)
                {
                    look.Clear();
                    Destroy(look);
                }
                return;
            }
            look ??= rudder.AddComponent<KwRudderGearLook>();
            look.Build(gear);
        }

        /// <summary>
        /// For a wheel's hover, the wheel at a world point: a line saying it steers the rudder whose gear names it, or why it
        /// does not (a rudder linked to it first, should another ship's rudder near have named it too); empty when no rudder
        /// near has measured it yet.
        /// </summary>
        public static string Words(Vector3 wheel)
        {
            KwRudderGear? refused = null;
            foreach (KwRudderGearLook look in All)
            {
                if (look == null || (look.transform.TransformPoint(KwMesh.ToUnity(look.Gear.Wheel)) - wheel).sqrMagnitude > Near * Near)
                    continue;
                if (look.Gear.Linked)
                    return look.Gear.Climbs ? "\nTurns the rudder through the steering shaft up the cabin's wall and under its roof"
                        : "\nTurns the rudder through the steering shaft under the deck";
                refused ??= look.Gear;
            }
            return refused == null ? "" : $"\n<color=#888888>Not linked to the rudder: {refused.Words} The rudder keeps its deck tiller.</color>";
        }

        private void Awake() => All.Add(this);

        private void OnDestroy() => All.Remove(this);

        private void Build(KwRudderGear gear)
        {
            Clear();
            Gear = gear;
            blade = transform.Find(KwRudder.BladeName);
            ship = GetComponentInParent<Ship>();
            last = float.NaN;
            if (!gear.Linked || !KwShipBody.Drawn)
                return;
            KwRudderTier tier = KwRudderTiers.Of(gameObject) ?? KwRudderTiers.All[0];
            Transform holder = new GameObject(Name) { layer = gameObject.layer }.transform;
            holder.SetParent(transform, false);
            KwRudderKit.Part(holder, "fixed", KwGearParts.Fixed(gear, tier), "kw_gear_fixed");
            List<KwGearRun> made = KwGearParts.Runs(gear, tier);
            for (int i = 0; i < made.Count; i++)
            {
                Transform run = KwRudderKit.Part(holder, "shaft" + i, made[i].Parts, "kw_gear_shaft" + i);
                run.localPosition = KwMesh.ToUnity(made[i].From);
                runs.Add((run, KwMesh.ToUnity(made[i].Way)));
            }
        }

        // The old drawing goes; renamed first, so a new one is found by name before the old is destroyed at the frame's end.
        private void Clear()
        {
            runs.Clear();
            Transform? old = transform.Find(Name);
            if (old == null)
                return;
            old.name = Name + "_old";
            old.gameObject.SetActive(false);
            Destroy(old.gameObject);
        }

        // Every shaft turned to the rudder's angle whenever it changes; on a ship the blade set to the helm first.
        private void LateUpdate()
        {
            if (runs.Count == 0 || blade == null)
                return;
            float angle = ship != null ? KwRudder.MaxAngle * ship.GetRudderValue() : Angle(blade.localRotation);
            if (ship != null)
                blade.localRotation = Quaternion.Euler(0f, -angle, 0f);
            if (angle == last)
                return;
            last = angle;
            float spin = KwGearTrain.Spin(angle);
            foreach ((Transform part, Vector3 way) in runs)
                part.localRotation = Quaternion.AngleAxis(-spin, way);
        }

        // The rudder's angle (kit, degrees) from the blade's turn about the up axis: the game's yaw, the other way round.
        private static float Angle(Quaternion q) => Mathf.DeltaAngle(0f, -2f * Mathf.Atan2(q.y, q.w) * Mathf.Rad2Deg);
    }
}
