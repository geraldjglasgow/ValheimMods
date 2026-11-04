using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Hovering a crate riding the game's cart (<see cref="KwCartRider"/>): its colliders are part of the cart's body, which
    /// the game's hover (<c>Player.FindHoverObject</c>) takes as the cart itself, and the cart's storage box over the bed (the
    /// "item" layer, hovered as its Container) wraps the crates, so a crate there could never be opened or unloaded. When the
    /// game has picked a cart carrying crates (or its storage), the same ray is cast again and the nearest hit on a crate
    /// riding that cart is taken, so long as nothing but the cart itself stands in front of it and it is within the player's
    /// reach. Asked from <see cref="KwShipRiderHover"/>'s postfix (one patch on the hover), for the local player only, and
    /// only while some cart carries a crate: one ray then, none otherwise.
    /// </summary>
    public static class KwCartHover
    {
        private static readonly RaycastHit[] hits = new RaycastHit[32];

        /// <summary>The crate the player looks at on a cart the game hovers, as the hover. Whether it was one.</summary>
        public static bool Pick(Player player, ref GameObject hover)
        {
            KwCartBed? bed = Bed(hover);
            if (bed == null || bed.Riders.Count == 0 || GameCamera.instance == null)
                return false;
            Collider? crate = Nearest(player, bed);
            if (crate == null)
                return false;
            hover = crate.gameObject;
            return true;
        }

        // The cart the hover is: the cart itself, or its storage.
        private static KwCartBed? Bed(GameObject hover)
        {
            if (hover.TryGetComponent(out KwCartBed bed))
                return bed;
            return hover.TryGetComponent(out Container box) && box.m_wagon != null && box.m_wagon.TryGetComponent(out KwCartBed its) ? its : null;
        }

        // The nearest hit on a crate riding this cart, nearer than anything that is not the cart, within reach of the eye.
        private static Collider? Nearest(Player player, KwCartBed bed)
        {
            Transform eye = GameCamera.instance.transform;
            int count = Physics.RaycastNonAlloc(eye.position, eye.forward, hits, 50f, player.m_interactMask);
            (Collider? best, float nearest, float blocked) = (null, float.MaxValue, float.MaxValue);
            Vector3 point = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                Collider found = hits[i].collider;
                Rigidbody? body = found != null ? found.attachedRigidbody : null;
                if (found == null || body != null && body.gameObject == player.gameObject)
                    continue;
                KwCartRider? rider = found.GetComponentInParent<KwCartRider>();
                if (rider != null && rider.On == bed && hits[i].distance < nearest)
                    (best, nearest, point) = (found, hits[i].distance, hits[i].point);
                else if (rider == null && (body == null || body.gameObject != bed.gameObject))
                    blocked = Mathf.Min(blocked, hits[i].distance);
            }
            return best != null && nearest < blocked && Vector3.Distance(player.m_eye.position, point) < player.m_maxInteractDistance ? best : null;
        }
    }
}
