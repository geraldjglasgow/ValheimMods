using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// The component on a construction site's post (prefab <c>OpenKeep_Site</c>): its <see cref="SiteState"/>, this
    /// machine's working state for it (<see cref="Run"/>), and the list of loaded sites every site class looks through.
    /// Hovering it shows the site (<see cref="SiteHover"/>); E hands over materials (<see cref="SiteDelivery"/>),
    /// Shift + E asks to take it down (<see cref="SiteTakeDown"/>). Its RPCs are registered by the modules through
    /// <see cref="SiteHooks.MarkerCreated"/>; its ghost goes with it.
    /// </summary>
    public sealed class SiteMarker : MonoBehaviour, Hoverable, Interactable
    {
        private static readonly List<SiteMarker> loaded = new List<SiteMarker>();

        /// <summary>Every site marker loaded on this machine with a valid ZDO.</summary>
        public static IReadOnlyList<SiteMarker> Loaded => loaded;

        public ZNetView View { get; private set; }

        public SiteState State { get; private set; }

        /// <summary>This machine's working state for the site (the builder's timer and ground plan, the cached bill); never networked.</summary>
        internal SiteRun Run { get; } = new SiteRun();

        private void Awake()
        {
            View = GetComponent<ZNetView>();
            if (View == null || !View.IsValid())
                return;
            State = new SiteState(View.GetZDO());
            loaded.Add(this);
            SiteHooks.RaiseMarkerCreated(this);
        }

        private void OnDestroy()
        {
            loaded.Remove(this);
            SiteGhost.Forget(this);
        }

        public string GetHoverText() => SiteHover.Cached(this) ?? BlueprintSafe.Call("OpenKeep site hover", () => SiteHover.Text(this), "");

        public string GetHoverName() => State?.Name ?? "";

        public float GetHoverOffset() => 0f;

        /// <summary>E hands over materials, Shift + E asks to take the site down; only the local player, never on hold.</summary>
        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            Player player = user as Player;
            if (hold || player == null || player != Player.m_localPlayer || State == null || !View.IsValid())
                return false;
            if (alt)
                BlueprintSafe.Run("OpenKeep site take down", () => SiteTakeDown.Ask(this, player));
            else
                BlueprintSafe.Run("OpenKeep site delivery", () => SiteDelivery.Deliver(this, player));
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        /// <summary>The loaded site nearest a point within a radius (flat distance), or null.</summary>
        public static SiteMarker Nearest(Vector3 point, float radius)
        {
            SiteMarker best = null;
            float bestDistance = radius;
            foreach (SiteMarker site in loaded)
            {
                if (site == null || site.State == null)
                    continue;
                Vector3 d = site.transform.position - point;
                float distance = Mathf.Sqrt(d.x * d.x + d.z * d.z);
                if (distance <= bestDistance)
                {
                    best = site;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}
