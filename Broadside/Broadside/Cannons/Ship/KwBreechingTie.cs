using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship cannon's breeching made fast or not (asked for on 2026-10-26: "when the ship cannon is being actually moved to
    /// new locations, the anchoring ropes should be hidden"). On every peer that draws, its two ropes and the ringbolts on
    /// their deck plates (the piece's model) are hidden while someone hauls it (its ZDO names him,
    /// <see cref="KwArmLink.Tower"/>) and shown again once he lets go, made fast where it now stands, as they are when it is
    /// built: the ringbolts are its own, ahead of its brackets. As he lets go its owner looks for something solid at each
    /// ringbolt's plate to make it fast to (a deck, railing or side of the ship it rides), and when either has none writes it unmade (<see cref="KwArmLink.Untied"/>): the breeching then stays hidden
    /// until it is hauled where it can be made fast. A cannon never hauled keeps it as built.
    /// </summary>
    public static class KwBreechingTie
    {
        // How far from a ringbolt's plate something solid may be; how far the ring stands over its plate.
        private const float Reach = 0.12f, Ring = 0.045f;

        private static readonly Collider[] found = new Collider[16];
        private static int mask;

        /// <summary>Whether its breeching is shown: nobody hauls it, and it was made fast where it was last left.</summary>
        public static bool Shown(KwArm arm) => arm.Tower.IsNone() && (arm.Link == null || !arm.Link.Valid || arm.Link.Zdo.GetInt(arm.Link.Untied) == 0);

        /// <summary>On its owner as it is let go: whether both ringbolts have something to be made fast to, written when it changes.</summary>
        public static void Decide(KwArm arm)
        {
            if (arm.Link == null || !arm.Link.Valid)
                return;
            bool fast = Fast(arm, 1f) && Fast(arm, -1f);
            ZDO zdo = arm.Link.Zdo;
            if ((zdo.GetInt(arm.Link.Untied) == 0) != fast)
                zdo.Set(arm.Link.Untied, fast ? 0 : 1);
        }

        // Something solid within reach of a ringbolt's plate that is not the cannon itself (aboard a ship, of that ship).
        private static bool Fast(KwArm arm, float side)
        {
            Transform? ring = arm.transform.Find(KwBreeching.Named(KwBreeching.Anchor, side));
            if (ring == null)
                return true;
            mask = mask != 0 ? mask : LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
            Transform? ship = arm.Aboard ? arm.transform.parent : null;
            Vector3 plate = Physical(ship, ring.position - arm.transform.up * Ring);
            int count = Physics.OverlapSphereNonAlloc(plate, Reach, found, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Transform hit = found[i].transform;
                if (!hit.IsChildOf(arm.transform) && (ship == null || hit.IsChildOf(ship)))
                    return true;
            }
            return false;
        }

        // A point as drawn on a ship, where the ship's body's physics has it (its colliders are there; it is drawn a little behind).
        private static Vector3 Physical(Transform? ship, Vector3 drawn)
        {
            Rigidbody? body = ship != null ? ship.GetComponent<Rigidbody>() : null;
            return body == null ? drawn : body.position + body.rotation * ship!.InverseTransformPoint(drawn);
        }
    }
}
