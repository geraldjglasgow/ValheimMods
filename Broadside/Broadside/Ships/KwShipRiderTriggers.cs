using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The trigger volumes on a piece built aboard (a hammock's, a copy of the game's bed: its player base, 20 m round; with
    /// any piece allowed, a fire's warmth and base, a workbench's or a portal's base) each given a body of its own, kinematic,
    /// as the piece is made the ship's child (<see cref="KwShipRiders.Take"/>). Unity sends a trigger's enters and exits to
    /// its body's object too; as part of the ship's body every one reached <c>Ship.OnTriggerEnter</c>, so everyone within
    /// 20 m of a hammock counted as aboard (the ship panel up, the ship not to be broken up), and stayed aboard once the
    /// trigger went with him inside (no exit is sent for a trigger destroyed; <see cref="KwShipAboard"/> tidies what is left).
    /// The game's effect areas find their triggers by overlap and keep their own trigger messages, so they work as before. A
    /// trigger with a solid collider on its object or under it is left as it was: the solid one must stay the ship's.
    /// </summary>
    public static class KwShipRiderTriggers
    {
        private static readonly List<Collider> under = new List<Collider>();

        /// <summary>Every trigger under the piece, but its own object's, on a kinematic body of its own.</summary>
        public static void Own(GameObject rider)
        {
            foreach (Collider collider in rider.GetComponentsInChildren<Collider>(true))
            {
                GameObject holder = collider.gameObject;
                if (!collider.isTrigger || holder == rider || holder.TryGetComponent(out Rigidbody _) || !TriggersOnly(holder))
                    continue;
                Rigidbody body = holder.AddComponent<Rigidbody>();
                (body.isKinematic, body.useGravity) = (true, false);
            }
        }

        // No solid collider on the object or under it, so its own body takes triggers only.
        private static bool TriggersOnly(GameObject holder)
        {
            holder.GetComponentsInChildren(true, under);
            bool only = under.TrueForAll(c => c.isTrigger);
            under.Clear();
            return only;
        }
    }
}
