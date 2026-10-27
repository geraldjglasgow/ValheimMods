using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a launched ship broken up with the hammer leaves (<see cref="KwShipBreak"/>), made on its owner just before the
    /// game destroys it: everything it cost (<see cref="KwBpCost"/>, in full, as the game gives back a removed piece's
    /// requirements; nothing with the world's free build key, as the game) and the shields hung on its railings
    /// (<see cref="KwShipShields"/>, whose hooks are emptied so its drop on destroy finds none), in the game's floating
    /// cargo crates (<see cref="Crate"/>, the crate a ship's chest leaves when it sinks), each filled to its own
    /// inventory's size, laid along the hull (<see cref="KwShipBreakPlan"/>) at the water's surface, or where the hull lay
    /// on land. The crates are networked objects made on the owner, as the game makes its own, so every player sees the
    /// same ones; the game empties and removes a crate when it is looted.
    /// </summary>
    public static class KwShipBreakSpill
    {
        public const string Crate = "CargoCrate";
        private const float Lift = 0.3f;

        /// <summary>On the ship's owner: its cost and hung shields packed into crates along it; how many crates were made.</summary>
        public static int Spill(KwShipBody body)
        {
            ZDO zdo = body.GetComponent<ZNetView>().GetZDO();
            KwBlueprint? blueprint = body.Blueprint ?? KwShipData.Unpack(zdo.GetString(KwShipBody.Key));
            GameObject? crate = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Crate) : null;
            if (blueprint == null || crate == null)
                return 0;
            List<ItemDrop.ItemData> items = Shields(zdo);
            items.AddRange(Materials(body.GetComponent<Piece>(), blueprint));
            items.AddRange(KwShipRiderBreak.Gather(body)); // the pieces built on it, their resources and contents (KwShipRider)
            zdo.Set(KwShieldRecord.Hash, "");
            return Fill(body.transform, body.Hull ?? KwBpHull.Measure(blueprint), crate, items, zdo.IsCheated());
        }

        // The shields hung on the railings, in hook order; one that does not read in this game is left out, as its drop would be.
        private static List<ItemDrop.ItemData> Shields(ZDO zdo)
        {
            List<ItemDrop.ItemData> shields = new List<ItemDrop.ItemData>();
            Dictionary<int, string> hung = KwShieldRecord.Read(zdo.GetString(KwShieldRecord.Hash));
            foreach (int slot in hung.Keys.OrderBy(s => s))
            {
                if (KwShieldRecord.Unpack(hung[slot]) is ItemDrop.ItemData shield)
                    shields.Add(shield);
            }
            return shields;
        }

        // Everything the ship cost, item by item in stacks.
        private static List<ItemDrop.ItemData> Materials(Piece? piece, KwBlueprint blueprint)
        {
            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>();
            if (piece == null || ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()))
                return items;
            foreach ((string item, int count) in KwShipBreakPlan.Stacks(KwBpCost.Of(blueprint), StackSize))
            {
                GameObject prefab = ObjectDB.instance.GetItemPrefab(item);
                ItemDrop.ItemData data = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
                (data.m_dropPrefab, data.m_stack) = (prefab, count);
                items.Add(data);
            }
            return items;
        }

        // An item's stack size; 0 for one this game lacks.
        private static int StackSize(string item)
        {
            GameObject? prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(item) : null;
            ItemDrop? drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            return drop != null ? Mathf.Max(drop.m_itemData.m_shared.m_maxStackSize, 1) : 0;
        }

        // The items into crates, one stack a slot, each crate filled to its inventory's size, laid along the hull.
        private static int Fill(Transform ship, KwBpHull hull, GameObject crate, List<ItemDrop.ItemData> items, bool cheated)
        {
            Container box = crate.GetComponent<Container>();
            int slots = Mathf.Max(box.m_width * box.m_height, 1);
            List<Vector2> places = KwShipBreakPlan.Places(KwShipBreakPlan.Crates(items.Count, slots), hull.Aft, hull.Fore);
            Quaternion turn = Quaternion.Euler(0f, ship.eulerAngles.y, 0f);
            for (int i = 0; i < places.Count; i++)
            {
                Inventory inventory = Object.Instantiate(crate, At(ship, hull, places[i]), turn).GetComponent<Container>().GetInventory();
                for (int k = i * slots; k < Mathf.Min(items.Count, (i + 1) * slots); k++)
                {
                    items[k].m_cheated |= cheated && !PlayerProfile.s_bypassCheatChecks;
                    inventory.AddItem(items[k]);
                }
            }
            return places.Count;
        }

        // A crate's place: over its spot on the keel line, at the water's surface, or at the keel's height when that is
        // higher (a ship on land), a little above it.
        private static Vector3 At(Transform ship, KwBpHull hull, Vector2 spot)
        {
            Vector3 at = ship.TransformPoint(new Vector3(spot.x, hull.Keel, spot.y));
            at.y = Mathf.Max(Floating.GetLiquidLevel(at), at.y) + Lift;
            return at;
        }
    }
}
