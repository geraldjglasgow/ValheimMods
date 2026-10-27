using System.Collections.Generic;
using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The captain loads every empty cannon aboard, on his own game, while he steers (the user, 2026-10-26: "guns are
    /// loading on port side but not the starboard side": cannons never fired were never loaded, so U skipped them). Every
    /// <see cref="Every"/> seconds, bow to stern, the first cannon that wants a load (empty and not loading, nobody mans or
    /// hauls it, not tipped, no reload of the broadside's queued for it) has its load begun from his pack by the
    /// broadside's own reload step (<see cref="KwBroadsideReload.Begin"/>: a gunner's reload in every way, the same rest
    /// after a shot, the items taken and the step written in one frame), one a count, so the loads roll down the ship. One
    /// his game does not own is asked of its owner (<see cref="KwArmLink.Load"/>, at most once every <see cref="Again"/>
    /// seconds a cannon), who hands it over as the called shot does when it is still empty and free (<see cref="Asked"/>).
    /// Short of a load it stays empty (the reload lines say so) and is tried again every <see cref="Again"/> seconds. While
    /// a reload of the broadside's is queued, nothing new is begun, so the reloads its message promised come first.
    /// </summary>
    public static class KwBroadsideLoad
    {
        /// <summary>Seconds between two looks for a cannon to load (a new load begun at most this often).</summary>
        public const float Every = 0.2f;

        /// <summary>Seconds before a cannon is asked of its owner again, or tried again when the pack was short.</summary>
        public const float Again = 2f;

        private static readonly Dictionary<KwArm, float> asked = new Dictionary<KwArm, float>(), shortAt = new Dictionary<KwArm, float>();
        private static float next;

        /// <summary>Each frame on the captain's game: the next empty cannon loaded or asked for when due; off the helm, forgotten.</summary>
        public static void Tick(Player? captain, List<KwArm>? aboard)
        {
            if (captain == null || aboard == null || aboard.Count == 0)
            {
                Forget();
                return;
            }
            if (Time.time < next || KwBroadsideReload.Pending)
                return;
            next = Time.time + Every;
            foreach (KwArm arm in aboard)
            {
                if (Wants(arm) && Try(arm, captain))
                    return;
            }
        }

        /// <summary>Empty and not loading, nobody mans or hauls it, upright, no reload of the broadside's queued for it.</summary>
        public static bool Wants(KwArm arm)
        {
            if (arm == null || arm.Frame == null || !arm.Link.Valid || arm.Manned || !arm.Tower.IsNone())
                return false;
            WeaponState s = arm.State;
            return !s.Loaded && !s.Busy && s.Step == 0 && !KwBroadsideReload.Queued(arm);
        }

        // One cannon: asked of its owner, or its load begun from his pack; whether a load was begun (the count's one).
        private static bool Try(KwArm arm, Player captain)
        {
            if (!arm.Net.IsOwner())
            {
                if (Due(asked, arm))
                    arm.Net.InvokeRPC(KwArmLink.Load, captain.GetPlayerID());
                return false;
            }
            if (shortAt.TryGetValue(arm, out float at) && Time.time < at + Again)
                return false;
            KwReload made = KwBroadsideReload.Begin(arm, captain, arm.Link.Zdo.GetInt(arm.Link.LoadKind));
            if (made == KwReload.Short)
                shortAt[arm] = Time.time;
            else
                shortAt.Remove(arm);
            return made == KwReload.Begun;
        }

        // Whether the cannon may be asked for now (not within Again of the last ask), noting the ask.
        private static bool Due(Dictionary<KwArm, float> last, KwArm arm)
        {
            if (last.TryGetValue(arm, out float at) && Time.time < at + Again)
                return false;
            last[arm] = Time.time;
            return true;
        }

        /// <summary>
        /// On the cannon's owner (any peer, a dedicated server too): the captain's game asks for the cannon to load it; handed
        /// over (the ZDO sent first) when it is still empty and not loading and nobody mans or hauls it. Nothing is answered:
        /// his game begins the load once the cannon is its own.
        /// </summary>
        public static void Asked(KwArm arm, long sender)
        {
            if (!arm.Net.IsValid() || !arm.Net.IsOwner() || arm.Frame == null || arm.Manned || !arm.Tower.IsNone())
                return;
            WeaponState s = arm.State;
            ZDO zdo = arm.Net.GetZDO();
            if (s.Loaded || s.Busy || s.Step != 0 || zdo.GetOwner() == sender)
                return;
            ZDOMan.instance.ForceSendZDO(sender, zdo.m_uid);
            zdo.SetOwner(sender);
        }

        // Off the helm: the asks and shortfalls noted are dropped (a cannon gone since is never kept).
        private static void Forget()
        {
            if (asked.Count > 0)
                asked.Clear();
            if (shortAt.Count > 0)
                shortAt.Clear();
        }
    }
}
