using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// A torch a player keeps lit day and night: <see cref="KeepKey"/> in the torch's ZDO, so every client sees it and it
    /// survives restarts, owner changes and the setting being turned off and on. The player's client never writes the
    /// ZDO: as the game's own fire switch does (<c>Fireplace.Interact</c> claims an unowned fire, then invokes
    /// <c>RPC_ToggleOn</c> on the owner), it claims only an unowned fire and sends <see cref="RpcName"/> with the wanted
    /// state to the ZDO owner, which writes the flag and applies it at once: kept lit, the fire is lit and leaves the
    /// schedule; back on the schedule, the phase is forgotten so <see cref="TorchSwitch.Tick"/> puts it out by day at once.
    /// The wanted state rather than a toggle is sent, so two players pressing together agree.
    /// </summary>
    public static class TorchKeep
    {
        /// <summary>ZDO bool: a player keeps this torch lit day and night. Written as false, never removed (the game's
        /// <c>RemoveInt</c> does not raise the data revision, so a removal would not reach the other clients).</summary>
        public const string KeepKey = "OpenKeep.torchKeepLit";

        /// <summary>RPC to the torch's ZDO owner, one bool: keep lit (true) or back on the schedule (false).</summary>
        public const string RpcName = "OpenKeep_TorchKeepLit";

        private static readonly int KeepHash = KeepKey.GetStableHashCode();
        private static readonly int RpcHash = RpcName.GetStableHashCode();

        public static bool IsKept(ZDO zdo) => zdo.GetBool(KeepHash, false);

        /// <summary>The key and the hover apply: a torch a player built, while the schedule switches its prefab. Any client.</summary>
        public static bool Switchable(Fireplace fire)
        {
            ZNetView view = fire != null ? fire.m_nview : null;
            if (view == null || !view.IsValid())
                return false;
            Piece piece = fire.m_piece;
            return piece != null && piece.IsPlacedByPlayer() && TorchSwitch.Switches(view.GetZDO());
        }

        /// <summary>Once per fire's net view, from <c>Fireplace.Awake</c>; the game registers nothing without a ZDO either.</summary>
        public static void Register(Fireplace fire)
        {
            ZNetView view = fire.m_nview;
            if (view == null || view.GetZDO() == null || view.m_functions.ContainsKey(RpcHash))
                return;
            view.Register<bool>(RpcName, (sender, keep) => Receive(fire, sender, keep));
        }

        /// <summary>The local player's key on a hovered torch: the ward check of the game's own interactions (the ward
        /// flashes when refused), then the wanted state to the owner.</summary>
        public static void Request(Fireplace fire)
        {
            if (!Switchable(fire) || !PrivateArea.CheckAccess(fire.transform.position))
                return;
            ZNetView view = fire.m_nview;
            bool keep = !IsKept(view.GetZDO());
            if (!view.HasOwner())
                view.ClaimOwnership();
            view.InvokeRPC(RpcName, keep);
            Messages.Center(fire.m_name + ": " + (keep ? TorchFeature.KeptWord : TorchFeature.ScheduledWord));
        }

        /// <summary>On the owner (a stale request after an owner change is dropped, as the game's fire RPCs do).</summary>
        private static void Receive(Fireplace fire, long sender, bool keep)
        {
            if (fire == null || !FuelFires.OwnedPlayerFire(fire))
                return;
            ZDO zdo = fire.m_nview.GetZDO();
            zdo.Set(KeepHash, keep);
            if (keep)
                TorchSwitch.SetOn(fire, zdo, true);
            else
                TorchSwitch.Forget(zdo);
            TorchSwitch.Tick(fire);
            Vector3 at = fire.transform.position;
            string state = keep ? "kept lit day and night" : "back on the night schedule";
            Plugin.Log.LogInfo($"OpenKeep: {Utils.GetPrefabName(fire.gameObject)} at {at.x:0}:{at.y:0}:{at.z:0} {state} (asked by peer {sender})");
        }
    }
}
