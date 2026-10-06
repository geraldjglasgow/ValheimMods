namespace OpenKeep.Core
{
    /// <summary>
    /// Who may become a container's owner, and when (behind <see cref="ContainerScan.Claim(Container)"/>). The owner is
    /// the one client that writes a container. A container nobody owns is claimed at once, the way the game claims a free
    /// object. A container another client owns is never taken: that client is asked to hand it over
    /// (<see cref="HandOver"/>), which it does with its latest data, and the container is used on a later call, once it
    /// is ours; taking it locally could write our older copy over a change of theirs still on its way. Ship and cart
    /// storage is the vehicle's network object, so it is changed only while the local client owns the vehicle (the
    /// helmsman or the one pulling the cart, or whoever the game gave it to) and never claimed or asked for.
    /// </summary>
    internal static class ContainerClaim
    {
        public static bool Claim(Container container, float askEvery)
        {
            ZNetView view = container != null ? container.m_nview : null;
            if (view == null || !view.IsValid() || container.m_inventory == null || ContainerScan.InUseByAnother(container))
                return false;
            if (!view.IsOwner() && !TakeFree(container, view, askEvery))
                return false;
            if (SaveHolds.NeedsLoad(container))
                container.Load();
            return true;
        }

        /// <summary>Claims a container nobody owns; asks the owner of any other one (false now, ours on a later call).</summary>
        private static bool TakeFree(Container container, ZNetView view, float askEvery)
        {
            if (IsVehicle(container))
                return false;
            if (view.GetZDO().HasOwner())
            {
                HandOver.Ask(container, askEvery);
                return false;
            }
            view.ClaimOwnership();
            return view.IsOwner();
        }

        /// <summary>The local client owns the container, or may claim it this moment (nobody owns or uses it, no vehicle).</summary>
        public static bool CanClaimNow(Container container)
        {
            ZNetView view = container != null ? container.m_nview : null;
            if (view == null || !view.IsValid() || container.m_inventory == null)
                return false;
            if (view.IsOwner())
                return true;
            return !view.GetZDO().HasOwner() && !IsVehicle(container) && !ContainerScan.InUseByAnother(container);
        }

        /// <summary>Another client owns the container's ZDO.</summary>
        public static bool OwnedElsewhere(Container container)
        {
            ZNetView view = container != null ? container.m_nview : null;
            if (view == null || !view.IsValid())
                return false;
            ZDO zdo = view.GetZDO();
            return zdo.HasOwner() && !zdo.IsOwner();
        }

        /// <summary>Ship or cart storage of a vehicle the local client does not own.</summary>
        public static bool VehicleOfAnother(Container container)
        {
            ZNetView view = container != null ? container.m_nview : null;
            return view != null && view.IsValid() && !view.IsOwner() && IsVehicle(container);
        }

        private static bool IsVehicle(Container container) => ContainerScan.IsShip(container) || ContainerScan.IsCart(container);
    }
}
