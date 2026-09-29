using System.Collections.Generic;
using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using HarmonyLib;
using PatchGuard;
using PlayerGrid;
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
    /// resolved room (one item per star, at least `max items`) is read from the creature's ZDO (any client can read a
    /// ZDO) before an item is ever removed. A landed melee hit takes at most one item however much room is left, so a
    /// 3-star thief needs three hits to fill its pouch. A ranged or area hit, a hit that deals no damage, a hit dodged or
    /// parried, a creature already full, or a player carrying nothing unequipped in their own grid (PackPanel's slots
    /// never count) are all ordinary hits - nothing here ever discards an item; not-taking it is always safe.
    /// </para>
    /// </summary>
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    public static class ThievingHitPatch
    {
        private const int HotbarWidth = 8;

        private static int _counter;

        // Read before the game touches the hit: RPC_Damage drops a dodgeable hit that meets a dodge roll's i-frames
        // without applying anything, but this postfix still runs after that early return, with the damage untouched.
        private static void Prefix(Character __instance, HitData hit, out bool __state) =>
            __state = Guard.Run("Character.RPC_Damage thieving dodge",
                () => hit != null && hit.m_dodgeable && __instance.IsDodgeInvincible());

        private static void Postfix(Character __instance, HitData hit, bool __state) =>
            Guard.Run("Character.RPC_Damage thieving", () => TrySteal(__instance, hit, __state));

        private static void TrySteal(Character victim, HitData hit, bool dodged)
        {
            ZNetView victimView = victim.GetComponent<ZNetView>();
            if (!Landed(hit, dodged) || !victim.IsPlayer() || victimView == null || !victimView.IsValid() || !victimView.IsOwner())
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

        // Only a melee strike steals: the game marks every projectile and area-of-effect hit m_ranged (a Greydwarf's
        // thrown stone included) and sends the flag with the hit. A parry still lets a sliver of damage through (see
        // ThievingParryPatch), so it is ruled out by name, not by the damage left over.
        private static bool Landed(HitData hit, bool dodged) =>
            hit != null && !hit.m_ranged && !dodged && !ThievingParryPatch.Parried(hit) && hit.GetTotalDamage() > 0f;

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

        // Pool order: every unequipped item of the player's own grid outside the hotbar, then the hotbar's only if the
        // first pool is empty. PackPanel's slots (worn gear, the backpack, food, mead, ammo, the purse, the key ring, the
        // tacklebox) are cells of the same inventory below its main rows and are in neither pool; while PackPanel lays
        // the inventory out but its grid cannot be read, nothing is taken at all rather than a slot risked.
        private static ItemDrop.ItemData? PickItem(Character victim)
        {
            if (!(victim is Player player) || !PackPanelGrid.TryMainRows(player, out int mainRows))
            {
                return null;
            }
            List<ItemDrop.ItemData>? all = player.GetInventory()?.GetAllItems();
            if (all == null)
            {
                return null;
            }
            List<ItemDrop.ItemData> pool = Filter(all, mainRows, hotbar: false);
            if (pool.Count == 0)
            {
                pool = Filter(all, mainRows, hotbar: true);
            }
            return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
        }

        private static List<ItemDrop.ItemData> Filter(List<ItemDrop.ItemData> all, int mainRows, bool hotbar)
        {
            List<ItemDrop.ItemData> pool = new List<ItemDrop.ItemData>();
            foreach (ItemDrop.ItemData item in all)
            {
                if (!item.m_equipped && item.m_gridPos.y < mainRows && InHotbar(item.m_gridPos) == hotbar)
                {
                    pool.Add(item);
                }
            }
            return pool;
        }

        // The hotbar is row 0's first eight cells, the ones the 1-8 keys use (Inventory.GetHotbar counts to 8); a grid
        // wider than 8 (PackPanel's) has ordinary cells right of it, robbed before the hotbar like any other.
        private static bool InHotbar(Vector2i cell) => cell.y == 0 && cell.x < HotbarWidth;

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
