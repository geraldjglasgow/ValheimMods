using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The drag of the nets a launched ship tows, on the ship's object (added by the first winch riding it, on every peer):
    /// each net in the water adds <see cref="KwTrawlTune.DragPerNet"/> of the ship's own drag along it, by how much of its
    /// rope is out (<see cref="KwTrawlWinch.InWater"/>), taken off the ship's speed in each physics step on the ship's owner
    /// only, where its physics run, as the ship's own drag is (<see cref="KwShipDrive"/>: the speed along the ship squared
    /// times its drag along the ship (<see cref="KwShipTrim.ForwardDrag"/>) and its share, at most 1 m/s a step), while it floats. Nothing is changed on the
    /// ship and nothing is kept: with the nets in, it sails as before. One net: the drag x1.4, its top speed under sail or
    /// oars about 15% lower (the speed goes as the drag's square root).
    /// </summary>
    public sealed class KwTrawlDrag : MonoBehaviour
    {
        private readonly List<KwTrawlWinch> winches = new List<KwTrawlWinch>();
        private Ship? ship;
        private KwShipHydro? hydro;

        /// <summary>A winch riding this ship counted (the component added to the ship when it has none).</summary>
        public static void Add(KwShipRiders on, KwTrawlWinch winch)
        {
            KwTrawlDrag drag = on.TryGetComponent(out KwTrawlDrag had) ? had : on.gameObject.AddComponent<KwTrawlDrag>();
            if (!drag.winches.Contains(winch))
                drag.winches.Add(winch);
        }

        /// <summary>A winch no longer counted on that ship.</summary>
        public static void Remove(KwShipRiders? on, KwTrawlWinch winch)
        {
            if (on != null && on.TryGetComponent(out KwTrawlDrag drag))
                drag.winches.Remove(winch);
        }

        /// <summary>The share of the ship's own drag the nets add, by how much of each one's rope is out.</summary>
        public static float Extra(float nets) => KwTrawlTune.DragPerNet * Mathf.Max(0f, nets);

        private void Awake() => (ship, hydro) = (GetComponent<Ship>(), GetComponent<KwShipHydro>());

        private void FixedUpdate()
        {
            if (winches.Count == 0 || ship == null || ship.m_body == null || ship.m_nview == null || !ship.m_nview.IsOwner() || hydro == null || !hydro.Afloat)
                return;
            float nets = 0f;
            foreach (KwTrawlWinch winch in winches)
                nets += winch != null ? winch.InWater : 0f;
            if (nets <= 0f)
                return;
            Rigidbody body = ship.m_body;
            Vector3 forward = ship.transform.forward;
            float along = Vector3.Dot(body.linearVelocity, forward);
            float slow = along * along * Mathf.Sign(along) * KwShipTrim.Of(ship).ForwardDrag(ship) * KwShipDrive.DragShare * Extra(nets);
            body.linearVelocity -= forward * Mathf.Clamp(slow, -1f, 1f);
        }
    }
}
