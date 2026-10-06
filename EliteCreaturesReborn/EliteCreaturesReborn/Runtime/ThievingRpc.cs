using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using PatchGuard;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// The Thieving steal request, from the machine that decided the theft (the robbed player's own client - their
    /// Inventory is authoritative there, nowhere else) to the creature's owner, over the creature's own ZNetView:
    /// `InvokeRPC` with no explicit target routes to the ZDO owner, and to this machine directly when it is the owner.
    /// The owner banks the item or refuses it, and always answers (<see cref="StealEscrow"/>), so the player keeps the
    /// item whenever it was not banked. Registered only on Thieving creatures, once they have resolved, on every
    /// machine, so the request reaches whichever machine owns the creature.
    /// </summary>
    public static class ThievingRpc
    {
        // Renamed with the answer it now expects: an owner running a build that never answers does not know this name,
        // so it drops the request and the player gets the item back on the timeout instead of losing it.
        public const string Steal = "ecr_steal_ask";

        public static void Register(ZNetView nview, EliteController controller) =>
            nview.Register<int, ZPackage>(Steal, (sender, id, pkg) => Bank(nview, controller, sender, id, pkg));

        /// <summary>Player-client side, once <see cref="StealEscrow.Hold"/> holds the item. `sender` on the far end is
        /// this machine's own id, which doubles as the stealer id for the ZDO-backed dedup - nothing extra to send.</summary>
        public static void Send(ZNetView creatureView, int id, ItemDrop.ItemData item)
        {
            ZPackage pkg = new ZPackage();
            item.Save(pkg);
            creatureView.InvokeRPC(Steal, id, pkg);
        }

        // The answer goes out whatever happens here, a throw included: an unanswered request is only given back late.
        private static void Bank(ZNetView nview, EliteController controller, long sender, int id, ZPackage pkg)
        {
            bool banked = false;
            try
            {
                banked = Guard.Run("ThievingRpc.Steal", static ask => BankItem(ask.nview, ask.controller, ask.sender, ask.id, ask.pkg),
                    (nview, controller, sender, id, pkg));
            }
            finally
            {
                StealEscrow.Answer(sender, id, banked);
            }
        }

        // Only the owner writes the pouch, and it re-checks what the player saw: a live Thieving creature with room.
        private static bool BankItem(ZNetView nview, EliteController controller, long sender, int id, ZPackage pkg)
        {
            if (!nview.IsValid() || !nview.IsOwner() || !controller.Ready || controller.Creature.IsDead()
                || !controller.Traits.Has(Mutation.Thieving))
            {
                return false; // ownership moved in flight, or the thief is gone: refused, the player keeps it
            }
            var (prefabHash, item) = ItemDrop.ItemData.Load(pkg, Version.Item.ChunksNCheats);
            if (prefabHash == 0 || !PouchStore.TryResolvePrefab(prefabHash, item))
            {
                if (Log.Diagnostics)
                {
                    Log.Diag($"{nview.name}: steal request named an unresolvable prefab ({prefabHash}), refused");
                }
                return false;
            }
            int maxItems = PouchStore.ResolvedMaxItems(controller.Rules, controller.Traits);
            PouchStore.Entry entry = new PouchStore.Entry { Item = item, StealerId = sender, StealCounter = id };
            return PouchStore.TryAdd(nview.GetZDO(), entry, maxItems); // false: full since the player looked, refused
        }
    }
}
