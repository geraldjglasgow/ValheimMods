using System.Collections.Generic;
using PatchGuard;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// What every peer that draws shows of a ship cannon rolling (<see cref="KwShipSpec.Wheels"/>: its four trucks), from
    /// where it is drawn, measured in what carries it (the launched ship it rides, so the trucks stand still as the ship
    /// sails; else the world): each truck rolled by how far its own hub went along the deck in its own plane since the last
    /// frame (its level travel along the way it rolls, as far again as it climbs, up to a steep slope's), so they roll
    /// forward and back as it is hauled and turn against each other as it swings round, the inner ones barely and the
    /// outer ones far (they hang on its carriage, so they roll as it recoils and runs out too, <see cref="KwShipRecoil"/>);
    /// and held still on the deck as the carriage pitches, its pitch taken back off their turn. Then the hauler's hands
    /// on his grips (<see cref="KwTowHands"/>). Nothing on a dedicated server.
    /// </summary>
    public sealed class KwWheels : MonoBehaviour
    {
        // A jump, a teleport, a first sync or the cannon coming aboard: left out. The steepest climb a wheel rolls up.
        private const float Jump = 2f, Steepest = 40f;

        private sealed class Wheel
        {
            public Transform Node = null!;
            public float Radius, Turn;
            public Vector3 Last;
        }

        private readonly List<Wheel> wheels = new List<Wheel>();
        private readonly KwTowHands hands = new KwTowHands();
        private Transform? gripLeft, gripRight;
        private KwArm? arm;
        private bool broken;

        private void Start()
        {
            arm = GetComponent<KwArm>();
            KwShipSpec? carriage = arm != null ? KwArmory.Arm(arm.Id)?.Ship : null;
            (gripLeft, gripRight) = (transform.Find(KwCannonBody.GripLeft), transform.Find(KwCannonBody.GripRight));
            ZNetView? view = GetComponent<ZNetView>();
            bool placed = view != null && view.GetZDO() != null;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || !placed || carriage == null || !Found(carriage))
                enabled = false;
        }

        // Each wheel's node by its path; false when one is missing.
        private bool Found(KwShipSpec carriage)
        {
            foreach ((string path, float radius) in carriage.Wheels)
            {
                Transform? node = transform.Find(path);
                if (node == null)
                    return false;
                wheels.Add(new Wheel { Node = node, Radius = radius, Last = node.position });
            }
            return wheels.Count > 0;
        }

        private void Update()
        {
            Transform? carrier = transform.parent;
            foreach (Wheel wheel in wheels)
                Roll(wheel, carrier);
        }

        // After the animator: the hauler's fists on his grips.
        private void LateUpdate()
        {
            if (broken || arm == null)
                return;
            broken = true;
            Guard.Run("Broadside cannon hauler's hands", static w => w.hands.Hold(w.arm!, w.gripLeft, w.gripRight), this);
            broken = false;
        }

        // A truck turned by its hub's travel along the deck in its own plane, in its carrier's frame, less the pitch of
        // what it hangs on about its axle (which turns it with its carriage, not on the deck).
        private static void Roll(Wheel wheel, Transform? carrier)
        {
            Transform on = wheel.Node.parent;
            // up is the world's, or the carrier's own (its deck's) in its frame: Vector3.up either way
            Vector3 at = Into(carrier, wheel.Node.position, true), axle = Into(carrier, on.right, false), up = Vector3.up;
            Vector3 level = Vector3.Cross(axle, up).normalized, forward = Into(carrier, on.forward, false);
            Vector3 travel = at - wheel.Last;
            wheel.Last = at;
            float along = Along(travel, level, up);
            if (Mathf.Abs(along) <= Jump && Mathf.Abs(along) >= 1e-5f)
                wheel.Turn = Mathf.Repeat(wheel.Turn + along / wheel.Radius * Mathf.Rad2Deg, 360f);
            float pitch = Mathf.Atan2(-Vector3.Dot(forward, Vector3.Cross(level, axle)), Vector3.Dot(forward, level)) * Mathf.Rad2Deg;
            wheel.Node.localRotation = Quaternion.AngleAxis(wheel.Turn - pitch, Vector3.right);
        }

        // How far a hub rolled: its level travel along the way it rolls, as far again as it climbed or fell with it (up to
        // the steepest climb); straight up or down alone, none.
        private static float Along(Vector3 travel, Vector3 level, Vector3 up)
        {
            float flat = Vector3.Dot(travel, level), rise = Mathf.Abs(Vector3.Dot(travel, up));
            float most = Mathf.Abs(flat) * Mathf.Tan(Steepest * Mathf.Deg2Rad);
            return Mathf.Sign(flat) * Mathf.Sqrt(flat * flat + Mathf.Min(rise, most) * Mathf.Min(rise, most));
        }

        // A point or a way in the carrier's frame (the world's when nothing carries it).
        private static Vector3 Into(Transform? carrier, Vector3 v, bool point) =>
            carrier == null ? v : point ? carrier.InverseTransformPoint(v) : carrier.InverseTransformDirection(v);
    }
}
