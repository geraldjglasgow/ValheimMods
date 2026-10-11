using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// What the player looks at and presses on a Raiders Chest. It sits on the model's own object, which carries the
    /// chest's colliders, so the game's hover and use - which take the first Hoverable and Interactable found going up from
    /// the collider hit - find it before the Container on the root. That keeps the game's container hover and use, which
    /// every chest in the world runs, unpatched; this only hands both to the chest's <see cref="RaidChest"/>. Code that
    /// looks for the Container itself still finds it.
    /// </summary>
    internal sealed class ChestFace : MonoBehaviour, Hoverable, Interactable
    {
        private RaidChest? _chest;

        private void Awake() => _chest = GetComponentInParent<RaidChest>();

        public string GetHoverText() => _chest != null ? _chest.HoverText() : "";

        public string GetHoverName() => ChestPrefab.ContainerName;

        public float GetHoverOffset() => _chest != null && _chest.Container != null ? _chest.Container.m_hoverOffset : 0f;

        public bool Interact(Humanoid user, bool hold, bool alt) => _chest != null && _chest.Interact(user, hold, alt);

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }
}
