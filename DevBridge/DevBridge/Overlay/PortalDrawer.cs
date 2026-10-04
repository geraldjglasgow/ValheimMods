using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>
    /// portals: each portal's (TeleportWorld's) trigger collider in cyan, the range it looks for a player to light up in
    /// faint violet, and the game's own pairing: a violet line to the portal its ZDO is connected to and a ring where a
    /// player would come out. Only the connection the game holds is drawn (its tag pairing, kept by the server): Wayfare
    /// leaves that in place but never uses it, so with Wayfare the line is not where its portals send anyone. A target
    /// this machine has no ZDO for is counted, not drawn (and not requested).
    /// </summary>
    internal static class PortalDrawer
    {
        private static readonly Color Trigger = new Color(0.3f, 0.9f, 1f, 0.9f);
        private static readonly Color Link = new Color(0.75f, 0.45f, 1f, 0.95f);
        private static readonly Color Range = new Color(0.75f, 0.45f, 1f, 0.35f);

        internal static void Draw(OverlayArea area, Category into)
        {
            foreach (ZNetView view in area.Views)
            {
                TeleportWorld portal = view.GetComponent<TeleportWorld>();
                if (!portal) continue;
                into.Count("portals");
                foreach (TeleportWorldTrigger trigger in portal.GetComponentsInChildren<TeleportWorldTrigger>())
                {
                    foreach (Collider collider in trigger.GetComponents<Collider>()) into.Lines.Add(ColliderWire.Of(collider), Trigger, 0.04f);
                }
                if (portal.m_proximityRoot) into.Lines.Add(Shapes.Ring(portal.m_proximityRoot.position, portal.m_activationRange), Range, 0.03f);
                Connection(portal, view.GetZDO(), into);
            }
        }

        private static void Connection(TeleportWorld portal, ZDO zdo, Category into)
        {
            ZDOID target = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
            if (target == ZDOID.None)
            {
                into.Count("unconnected");
                return;
            }
            ZDO other = ZDOMan.instance.GetZDO(target);
            if (other == null)
            {
                into.Count("target_not_loaded");
                return;
            }
            into.Count("connected");
            Vector3 exit = other.GetPosition() + other.GetRotation() * Vector3.forward * portal.m_exitDistance + Vector3.up;
            into.Lines.Add(new[] { portal.transform.position + Vector3.up, other.GetPosition() + Vector3.up }, Link, 0.08f);
            into.Lines.Add(Shapes.Ring(exit, 0.5f), Link, 0.05f);
        }
    }
}
