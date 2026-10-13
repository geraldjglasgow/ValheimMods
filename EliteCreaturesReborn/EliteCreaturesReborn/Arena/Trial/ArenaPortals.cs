using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The four archways' portals on this client (features/arena.md section 3, "Waves"), on the arena core's object, so
    /// they come and go with the core: every client near the hall has it, and a dedicated server (no screen) switches
    /// this off. Ten times a second it reads which archways still have creatures to send, one bit each in the core's ZDO
    /// (<see cref="ArenaKeys.Portals"/>, written by the core's owner), so every client opens and closes the same portals:
    /// a bit set opens its portal (<see cref="ArenaPortal"/>) at once, about half a second before the archway's first
    /// creature; a bit cleared closes it a second later, once the last creature has stepped out. With no trial running,
    /// or the hall not built here, every portal closes.
    /// </summary>
    internal sealed class ArenaPortals : MonoBehaviour
    {
        private const float ReadSeconds = 0.1f;

        /// <summary>Seconds a portal stays open after its archway's last creature, so it is seen stepping out.</summary>
        private const float HoldSeconds = 1f;

        private readonly ArenaPortal?[] _portals = new ArenaPortal?[ArenaTable.Archways];
        private readonly float[] _emptySince = new float[ArenaTable.Archways];
        private ZNetView _nview = null!;
        private float _next;

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            enabled = _nview != null && !ArenaPortalLook.NoScreen;
            for (int i = 0; i < _emptySince.Length; i++)
            {
                _emptySince[i] = -1f;
            }
        }

        private void Update()
        {
            float now = Time.time;
            if (now < _next)
            {
                return;
            }
            _next = now + ReadSeconds;
            Guard.Run("arena portals", static portals => portals.Refresh(), this);
        }

        private void Refresh()
        {
            ZDO? zdo = _nview.IsValid() ? _nview.GetZDO() : null;
            bool on = zdo != null && ArenaHall.Built && ArenaState.IsRunning(zdo);
            int mask = on ? zdo!.GetInt(ArenaKeys.PortalsHash) : 0;
            float now = Time.time;
            for (int lane = 0; lane < _portals.Length; lane++)
            {
                Show(lane, (mask >> lane & 1) != 0, now);
            }
        }

        private void Show(int lane, bool wanted, float now)
        {
            if (wanted)
            {
                Open(lane);
                return;
            }
            ArenaPortal? portal = _portals[lane];
            if (portal == null || !portal.IsOpen)
            {
                return;
            }
            if (_emptySince[lane] < 0f)
            {
                _emptySince[lane] = now;
            }
            else if (now - _emptySince[lane] >= HoldSeconds)
            {
                portal.Close();
            }
        }

        private void Open(int lane)
        {
            _emptySince[lane] = -1f;
            if (_portals[lane] == null)
            {
                _portals[lane] = Make(lane);
            }
            ArenaPortal? portal = _portals[lane];
            if (portal != null)
            {
                portal.Open();
            }
        }

        // The archway's portal, made the first time it opens; null when the hall has no such point here.
        private ArenaPortal? Make(int lane) =>
            ArenaSpots.PortalAnchor(lane, out ArenaSpots.Spot anchor) ? ArenaPortal.Make(transform, anchor) : null;
    }
}
