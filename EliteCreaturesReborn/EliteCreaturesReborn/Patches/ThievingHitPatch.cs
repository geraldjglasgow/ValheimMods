using System.Collections.Generic;
using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// The Thieving steal, decided on the ONE machine whose Inventory is authoritative for it: the robbed player's
    /// own client. A player's Character is owned by that player's own machine, so `Character.RPC_Damage` for a
    /// player victim already runs there - the same fact DamageReactionPatch and DevourHitPatch already lean on for
    /// their own owner-side reactions, just for the opposite role (the victim here, not the attacker).
    /// <para>
    /// Check first, take second: the creature's ZNetView must be valid before anything is touched, and the pouch's
    /// resolved room is read from the creature's ZDO (any client can read a ZDO) before an item is ever removed. A
    /// hit that deals no damage, a creature already full, or a player carrying nothing unequipped are all ordinary
    /// hits - nothing here ever discards an item; not-taking it is always safe.
    /// </para>
    /// </summary>
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    public static class ThievingHitPatch
    {
        private static int _counter;

        private static void Postfix(Character __instance, HitData hit) =>
            Guard.Run("Character.RPC_Damage thieving", () => TrySteal(__instance, hit));

        private static void TrySteal(Character victim, HitData hit)
        {
            ZNetView victimView = victim.GetComponent<ZNetView>();
            if (hit == null || hit.GetTotalDamage() <= 0f || !victim.IsPlayer()
                || victimView == null || !victimView.IsValid() || !victimView.IsOwner())
            {
                return; // only the robbed player's own client decides this, and only on a landed, damaging hit
            }
            EliteController? thief = ReadyThief(hit.GetAttacker());
            if (thief == null)
            {
                return;
            }
            Steal(victim, thief);
        }

        // A resolved, Thieving, un-tamed creature; null otherwise. Tamed is checked here because vanilla taming is
        // live today independent of this mod's own (unbuilt) taming feature - the spec's "never steals, from its
        // owner or from anyone" applies regardless.
        private static EliteController? ReadyThief(Character? attacker)
        {
            if (attacker == null)
            {
                return null;
            }
            EliteController? controller = attacker.GetComponent<EliteController>();
            if (controller == null || !controller.Ready || !controller.Traits.Has(Mutation.Thieving))
            {
                return null;
            }
            Tameable tameable = attacker.GetComponent<Tameable>();
            return tameable != null && tameable.IsTamed() ? null : controller;
        }

        private static void Steal(Character victim, EliteController thief)
        {
            ZNetView creatureView = thief.View;
            if (creatureView == null || !creatureView.IsValid())
            {
                return; // no theft happens at all - the creature cannot be reached to bank it
            }
            int maxItems = PouchStore.ResolvedMaxItems(thief.Rules, thief.Traits);
            if (PouchStore.Count(creatureView.GetZDO()) >= maxItems)
            {
                return; // already full: an ordinary hit
            }
            ItemDrop.ItemData? item = PickItem(victim);
            if (item == null)
            {
                return; // nothing unequipped to take: an ordinary hit
            }
            Take(victim, thief, creatureView, item);
        }

        // Pool order: every unequipped item below the hotbar row (grid y != 0), then the hotbar row (y == 0) only if
        // the first pool is empty. Row 0 is confirmed as the hotbar row by Inventory.GetHotbar/GetBoundItems, both of
        // which key off m_gridPos.y == 0.
        private static ItemDrop.ItemData? PickItem(Character victim)
        {
            Humanoid? humanoid = victim as Humanoid;
            List<ItemDrop.ItemData>? all = humanoid != null ? humanoid.GetInventory()?.GetAllItems() : null;
            if (all == null)
            {
                return null;
            }
            List<ItemDrop.ItemData> pool = Filter(all, hotbar: false);
            if (pool.Count == 0)
            {
                pool = Filter(all, hotbar: true);
            }
            return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
        }

        private static List<ItemDrop.ItemData> Filter(List<ItemDrop.ItemData> all, bool hotbar)
        {
            List<ItemDrop.ItemData> pool = new List<ItemDrop.ItemData>();
            foreach (ItemDrop.ItemData item in all)
            {
                if (!item.m_equipped && (item.m_gridPos.y == 0) == hotbar)
                {
                    pool.Add(item);
                }
            }
            return pool;
        }

        private static void Take(Character victim, EliteController thief, ZNetView creatureView, ItemDrop.ItemData item)
        {
            Humanoid humanoid = (Humanoid)victim;
            string name = item.m_shared.m_name;
            int stack = item.m_stack;
            humanoid.GetInventory().RemoveItem(item); // takes the whole stack, whatever it was
            Announce(thief, name, stack);
            CreatureRpc.FireFlash(creatureView, victim.GetCenterPoint(), 2f, "steal");
            ThievingRpc.Send(creatureView, ++_counter, item);
        }

        private static void Announce(EliteController thief, string itemName, int stack)
        {
            MessageHud hud = MessageHud.instance;
            if (hud != null)
            {
                hud.ShowMessage(MessageHud.MessageType.TopLeft, $"{thief.Creature.GetHoverName()} stole {itemName} x{stack}");
            }
        }
    }
}
