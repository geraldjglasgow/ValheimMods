using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// On a launched ship every collider belongs to the ship's body, and the game takes what the player aims at to be the
    /// collider's own object only when that object can itself be hovered; otherwise it takes the whole ship. This sits on
    /// each collider of a ship's working fitting (a hatch's leaves) and hands the hover text and the use on to the fitting
    /// above it.
    /// </summary>
    public sealed class KwHoverForward : MonoBehaviour, Interactable, Hoverable
    {
        private Interactable? use;
        private Hoverable? hover;

        /// <summary>Puts one on every collider under a fitting that cannot be hovered itself.</summary>
        public static void OnColliders(Transform fitting)
        {
            foreach (Collider collider in fitting.GetComponentsInChildren<Collider>(true))
            {
                if (collider.GetComponent<Hoverable>() == null)
                    collider.gameObject.AddComponent<KwHoverForward>();
            }
        }

        private Interactable? Use => use ??= Up<Interactable>();
        private Hoverable? Hover => hover ??= Up<Hoverable>();

        // The nearest of its parents that is one.
        private T? Up<T>() where T : class
        {
            for (Transform? t = transform.parent; t != null; t = t.parent)
            {
                T? found = t.GetComponent<T>();
                if (found != null)
                    return found;
            }
            return null;
        }

        public bool Interact(Humanoid user, bool hold, bool alt) => Use != null && Use.Interact(user, hold, alt);

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => Use != null && Use.UseItem(user, item);

        public string GetHoverText() => Hover != null ? Hover.GetHoverText() : "";

        public string GetHoverName() => Hover != null ? Hover.GetHoverName() : "";

        public float GetHoverOffset() => Hover != null ? Hover.GetHoverOffset() : 0f;
    }
}
