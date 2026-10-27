using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The blueprint table in the world: hovering shows the dock it serves and the Use key (<see cref="KwBpStationText"/>,
    /// worked out at most twice a second); the game's Use key opens the table's window for the local player
    /// (<see cref="KwBpShelf"/>: their ships and the shared ones, saving, building and clearing the dock), behind the
    /// game's ward check.
    /// </summary>
    public sealed class KwBpStation : MonoBehaviour, Hoverable, Interactable
    {
        private const float HoverEvery = 0.5f;
        private ZNetView? view;
        private string hover = "";
        private float hoverUntil;

        private void Awake() => view = GetComponent<ZNetView>();

        private bool Valid => view != null && view.IsValid();

        public string GetHoverText() => Guard.Run("Broadside blueprint table hover", Hover);

        public string GetHoverName() => KwBlueprintStation.Label;

        public float GetHoverOffset() => 0f;

        public bool Interact(Humanoid user, bool hold, bool alt) =>
            !hold && Guard.Run<Humanoid, bool>("Broadside blueprint table", Use, user);

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        private string Hover()
        {
            if (!Valid)
                return "";
            if (Time.time < hoverUntil)
                return hover;
            hoverUntil = Time.time + HoverEvery;
            hover = Localization.instance.Localize(KwBpStationText.Compose(transform.position));
            return hover;
        }

        private bool Use(Humanoid user)
        {
            if (!Valid || user == null || user != Player.m_localPlayer || !PrivateArea.CheckAccess(transform.position))
                return false;
            KwBpShelf.Shelf.Open(this);
            KwBpFiguresPanel.Opened();
            hoverUntil = 0f;
            return true;
        }
    }
}
