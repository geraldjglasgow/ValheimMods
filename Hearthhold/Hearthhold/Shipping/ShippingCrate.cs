using System;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// The selling part of a built Shipping Crate, on world instances only (the prefab copy sits under an inactive holder,
    /// so this Awake never runs there). Every few seconds the crate's ZDO owner compares the dawn day (a day starts at
    /// the game's morning, 15% into it) with the day stored in the ZDO; when a dawn has passed it sells once, however many
    /// days were missed, and stores the day. A new crate stores the day when first seen. Never sells while open or
    /// before the container has loaded its latest items; it simply tries again on the next check. While the switch is
    /// off it only keeps the day current, so turning it on sells at the next dawn, not at once.
    /// </summary>
    public sealed class ShippingCrate : MonoBehaviour
    {
        private const float CheckInterval = 5f;
        private const float MessageRange = 30f;
        private const double DawnFraction = 0.15;

        private ZNetView nview;
        private Container container;

        private void Awake()
        {
            nview = GetComponent<ZNetView>();
            container = GetComponent<Container>();
            if (nview == null || container == null || nview.GetZDO() == null)
                return;
            ZNetView view = nview;
            nview.Register<int, int>(ShippingKeys.RpcSold, (sender, items, coins) => ShowSold(view, items, coins));
            InvokeRepeating(nameof(Check), UnityEngine.Random.Range(2f, CheckInterval), CheckInterval);
        }

        private void Check() => HookGuard.Run("shipping crate", CheckNow);

        private void CheckNow()
        {
            if (nview == null || !nview.IsValid() || !nview.IsOwner() || EnvMan.instance == null || ZNet.instance == null)
                return;
            ZDO zdo = nview.GetZDO();
            int today = DawnDay();
            int stored = zdo.GetInt(ShippingKeys.Day, -1);
            if (stored >= today)
                return;
            if (stored < 0 || !ShippingPiece.On)
            {
                zdo.Set(ShippingKeys.Day, today);
                return;
            }
            if (!Ready(zdo))
                return;
            zdo.Set(ShippingKeys.Day, today);
            Sell(zdo);
        }

        /// <summary>The day number, counted from dawn rather than midnight.</summary>
        public static int DawnDay()
        {
            double length = Math.Max(1L, EnvMan.instance.m_dayLengthSec);
            return (int)Math.Floor(ZNet.instance.GetTimeSeconds() / length - DawnFraction);
        }

        /// <summary>Not open on this owner and holding the ZDO's latest items.</summary>
        private bool Ready(ZDO zdo)
        {
            // Only this owner's own flag: the ZDO's shared in-use value can stay set after an opener disconnects.
            if (container.IsInUse())
                return false;
            return container.GetInventory() != null && container.m_lastRevision == zdo.DataRevision;
        }

        private void Sell(ZDO zdo)
        {
            (int items, int coins) = ShippingSale.Sell(container.GetInventory());
            if (items <= 0)
                return;
            zdo.Set(ShippingKeys.SoldItems, items);
            zdo.Set(ShippingKeys.SoldCoins, coins);
            nview.InvokeRPC(ZNetView.Everybody, ShippingKeys.RpcSold, items, coins);
        }

        private static void ShowSold(ZNetView view, int items, int coins)
        {
            Player player = Player.m_localPlayer;
            if (view == null || player == null || MessageHud.instance == null)
                return;
            if (Vector3.Distance(player.transform.position, view.transform.position) > MessageRange)
                return;
            MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, $"The Shipping Crate sold {items} items for {coins} coins");
        }
    }
}
