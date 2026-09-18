using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Util;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// The Thieving steal command, routed the same way CreatureRpc.Devour already is: from whichever machine decided
    /// the theft (the robbed player's own client - their Inventory is authoritative there, nowhere else) to the
    /// creature's own owner, over the creature's own ZNetView. `InvokeRPC` with no explicit target routes to the ZDO
    /// owner; the handler is registered on every machine but only the owner ever acts on it. Registered once per
    /// creature from EliteController.Setup, alongside CreatureRpc.Register.
    /// </summary>
    public static class ThievingRpc
    {
        public const string Steal = "ecr_steal";

        public static void Register(ZNetView nview, EliteController controller) =>
            nview.Register<int, ZPackage>(Steal, (sender, counter, pkg) => Bank(nview, controller, sender, counter, pkg));

        /// <summary>Player-client side, once the item has already left the player's inventory. `sender` on the far end
        /// is this machine's own id, which doubles as the stealer id for the ZDO-backed dedup - nothing extra to send.</summary>
        public static void Send(ZNetView creatureView, int counter, ItemDrop.ItemData item)
        {
            ZPackage pkg = new ZPackage();
            item.Save(pkg);
            creatureView.InvokeRPC(Steal, counter, pkg);
        }

        private static void Bank(ZNetView nview, EliteController controller, long sender, int counter, ZPackage pkg) =>
            Guard.Run("ThievingRpc.Steal", () => BankItem(nview, controller, sender, counter, pkg));

        private static void BankItem(ZNetView nview, EliteController controller, long sender, int counter, ZPackage pkg)
        {
            if (!nview.IsOwner())
            {
                return; // only the owner ever writes the pouch
            }
            var (prefabHash, item) = ItemDrop.ItemData.Load(pkg, Version.Item.ChunksNCheats);
            if (prefabHash == 0 || !PouchStore.TryResolvePrefab(prefabHash, item))
            {
                Log.Diag($"{nview.name}: steal packet named an unresolvable prefab ({prefabHash}), item lost");
                return;
            }
            ResolveOrDrop(nview, controller, sender, counter, item);
        }

        // The client already checked room before taking the item; this is the owner's own, authoritative re-check.
        // The two can disagree by a frame on a busy server - the item is never discarded, only dropped at the creature.
        private static void ResolveOrDrop(ZNetView nview, EliteController controller, long sender, int counter, ItemDrop.ItemData item)
        {
            ZDO zdo = nview.GetZDO();
            int maxItems = PouchStore.ResolvedMaxItems(controller.Rules, controller.Traits);
            PouchStore.Entry entry = new PouchStore.Entry { Item = item, StealerId = sender, StealCounter = counter };
            if (PouchStore.TryAdd(zdo, entry, maxItems))
            {
                return;
            }
            Log.Diag($"{nview.name}: pouch full at bank time, dropping '{item.m_shared?.m_name}' at the creature");
            ItemDrop.DropItem(item, item.m_stack, nview.transform.position, Quaternion.identity);
        }
    }
}
