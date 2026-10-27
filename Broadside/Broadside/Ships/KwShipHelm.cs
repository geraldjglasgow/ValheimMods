using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a ship's own helm goes (the karve's controls, moved): standing behind the captain's wheel the rudder's steering
    /// gear is linked to (<see cref="KwShipWheel"/>), else on the deck at the rudder's grip, its tiller's or its deck
    /// tiller's (<see cref="KwShipTiller"/>), else sitting at the stern (no rudder, or one with nothing to stand at). The karve's chest is put away: a Broadside ship has no hold.
    /// </summary>
    public static class KwShipHelm
    {
        public static void Place(Transform ship, KwBpHull hull, GameObject? blade)
        {
            Transform? controls = ship.Find("interactive/controlls");
            ShipControlls? helm = controls != null ? controls.GetComponent<ShipControlls>() : null;
            if (helm == null || helm.m_attachPoint == null)
                return;
            bool wheel = KwShipWheel.TakesHelm(ship, helm, controls!, blade);
            // At a wheel the helm's own box (the karve's, floating half a metre over the hub) is off: the wheel's body takes E
            // for it (KwHoverForward). Left on, it stood unseen before a cabin's ladder behind the wheel, and E there took the
            // helm and set the player down at the wheel (the user, 2026-10-26).
            foreach (Collider own in controls!.GetComponents<Collider>())
                own.enabled = !wheel;
            if (wheel || (blade != null && KwShipTiller.Stand(ship, helm, controls!, blade.transform)))
                return;
            Vector3 stern = new Vector3(0f, hull.DeckTop + 0.4f, -hull.Length / 2f + 0.5f);
            controls!.localPosition = stern + new Vector3(0f, 0.4f, -0.3f);
            helm.m_attachPoint.position = ship.TransformPoint(stern);
            helm.m_attachPoint.rotation = ship.rotation;
            helm.m_attachAnimation = "attach_sitship";
            helm.m_detachOffset = new Vector3(0f, 0.5f, -0.6f);
            KwHelmsman.OnShip(helm, null);
        }
    }
}
