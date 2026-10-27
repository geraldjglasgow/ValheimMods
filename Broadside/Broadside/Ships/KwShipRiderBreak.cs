using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What becomes of a launched ship's riders (<see cref="KwShipRider"/>) when the ship goes, on the ship's owner, before
    /// the game destroys it. Broken up with the hammer (<see cref="KwShipBreakSpill"/>): every rider is taken over by this
    /// game and destroyed the game's own way, so it drops what the game drops for it (its resources, a chest's contents,
    /// an item stand's item, an armour stand's gear), and every item it dropped is taken back at once for the ship's
    /// floating crates (<see cref="Gather"/>). Wrecked (sunk, or destroyed by damage; <see cref="Wreck"/>): each is removed
    /// by its owner the game's way, its resources and contents dropped where it was, floating on the water. A ship removed
    /// any other way (<c>kwship clear</c>) leaves its riders as ordinary pieces (<see cref="KwShipRiderScene"/>).
    /// </summary>
    public static class KwShipRiderBreak
    {
        private static readonly List<ItemDrop> made = new List<ItemDrop>();

        /// <summary>On the ship's owner, breaking it up: its riders destroyed and what they dropped, as items for the crates.</summary>
        public static List<ItemDrop.ItemData> Gather(KwShipBody body)
        {
            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>();
            KwShipRiders? ship = body.GetComponent<KwShipRiders>();
            if (ship == null)
                return items;
            foreach (KwShipRider rider in new List<KwShipRider>(ship.Riders))
                KwShipBody.Step("Broadside ship rider broken up", () => Take(ship, rider, items));
            return items;
        }

        // One rider owned here and destroyed the game's way; every item it dropped taken back.
        private static void Take(KwShipRiders ship, KwShipRider rider, List<ItemDrop.ItemData> items)
        {
            ZNetView? view = rider != null ? rider.GetComponent<ZNetView>() : null;
            if (view == null || !view.IsValid())
                return;
            ship.Forget(rider!);
            view.ClaimOwnership();
            int before = ItemDrop.s_instances.Count;
            Break(rider!.gameObject);
            Collect(before, items);
        }

        // Destroyed as the game destroys a piece that is removed: its wear's way, or for one without wear the hammer's.
        private static void Break(GameObject piece)
        {
            if (piece.TryGetComponent(out WearNTear wear))
            {
                wear.Destroy(null, false);
                return;
            }
            piece.GetComponent<Piece>()?.DropResources();
            ZNetScene.instance.Destroy(piece);
        }

        // The items dropped since the list had so many (Unity destroys nothing before the frame's end, so the new ones are
        // the list's last), each taken back as an item and its object destroyed.
        private static void Collect(int from, List<ItemDrop.ItemData> items)
        {
            made.Clear();
            for (int i = from; i < ItemDrop.s_instances.Count; i++)
                made.Add(ItemDrop.s_instances[i]);
            foreach (ItemDrop drop in made)
            {
                if (drop == null || drop.m_nview == null || !drop.m_nview.IsValid() || !drop.m_nview.IsOwner())
                    continue;
                ItemDrop.ItemData item = drop.m_itemData.Clone();
                item.m_dropPrefab ??= ObjectDB.instance.GetItemPrefab(Utils.GetPrefabName(drop.gameObject));
                items.Add(item);
                ZNetScene.instance.Destroy(drop.gameObject);
            }
            made.Clear();
        }

        /// <summary>On the ship's owner as its wear destroys it (sunk, wrecked): each rider removed by its owner, its drops where it was.</summary>
        public static void Wreck(KwShipRiders ship)
        {
            foreach (KwShipRider rider in new List<KwShipRider>(ship.Riders))
                KwShipBody.Step("Broadside ship rider wrecked", () => Sink(ship, rider));
        }

        private static void Sink(KwShipRiders ship, KwShipRider rider)
        {
            ZNetView? view = rider != null ? rider.GetComponent<ZNetView>() : null;
            if (view == null || !view.IsValid())
                return;
            if (rider!.TryGetComponent(out WearNTear wear))
            {
                if (view.IsOwner())
                    ship.Forget(rider);
                wear.Remove();
                return;
            }
            ship.Forget(rider);
            view.ClaimOwnership();
            Break(rider.gameObject);
        }
    }
}
