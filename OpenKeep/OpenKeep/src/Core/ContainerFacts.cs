using System;
using UnityEngine;

namespace OpenKeep.Core
{
    /// <summary>
    /// What never changes about a container once it woke: its net object's prefab name (the ship or cart for their
    /// storage, without "(Clone)"), whether it is a ship's or a cart's storage and whether it is the private chest.
    /// <see cref="ContainerScan"/> keeps one per tracked container, found on first use, so the reach list and every
    /// count read them without walking the hierarchy or building strings.
    /// </summary>
    internal sealed class ContainerFacts
    {
        private const string PrivateChestPrefab = "piece_chest_private";

        public ContainerFacts(Container container)
        {
            GameObject root = container.m_nview != null ? container.m_nview.gameObject : container.gameObject;
            PrefabName = Utils.GetPrefabName(root);
            IsShip = container.GetComponentInParent<Ship>() != null;
            IsCart = container.m_wagon != null || container.GetComponentInParent<Vagon>() != null;
            IsPrivateChest = string.Equals(PrefabName, PrivateChestPrefab, StringComparison.OrdinalIgnoreCase);
        }

        public string PrefabName { get; }

        public bool IsShip { get; }

        public bool IsCart { get; }

        public bool IsPrivateChest { get; }
    }
}
