using System.Collections.Generic;
using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
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
    /// never count) are all ordinary hits - nothing here ever discards an item; not-taking it is always safe. Both halves
    /// are steps of <see cref="HitPatch"/>. The item is held, not destroyed, until the creature's owner has banked it
    /// (<see cref="StealEscrow"/>): a refused or unanswered steal gives it back.
    /// </para>
    /// </summary>
    public static class ThievingHitPatch
    {
        private const int HotbarWidth = 8;

        // Never stolen, equipped or not: losing the wishbone to a thief that runs off is a lost boss reward.
        private static readonly HashSet<string> Unstealable = new HashSet<string> { "Wishbone" };

        // Read before the game touches the hit: RPC_Damage drops a dodgeable hit that meets a dodge roll's i-frames
        // without applying anything, but the steps after the hit still run after that early return, damage untouched.
        internal static bool Dodged(Struck struck) =>
            struck.Hit != null && struck.Hit.m_dodgeable && struck.Victim.IsDodgeInvincible();

        internal static void TrySteal(Struck struck, bool dodged)
        {
            Character victim = struck.Victim;
            HitData? hit = struck.Hit;
            if (hit == null || !Landed(hit, dodged) || !victim.IsPlayer() || !struck.Owned)
            {
                return; // only the robbed player's own client decides this, and only on a landed, damaging hit
            }
            EliteController? thief = ReadyThief(struck.Attacker, struck.AttackerElite);
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
        private static EliteController? ReadyThief(Character? attacker, EliteController? controller)
        {
            if (attacker == null || controller == null || !controller.Ready || !controller.Traits.Has(Mutation.Thieving))
            {
                return null;
            }
            Tameable tameable = attacker.GetComponent<Tameable>();
            return tameable != null && tameable.IsTamed() ? null : controller;
        }

        private static void Steal(Character victim, EliteController thief)
        {
            ZNetView creatureView = thief.View;
            if (creatureView == null || !creatureView.IsValid() || creatureView.GetZDO().GetOwner() == 0L)
            {
                return; // no theft happens at all - the creature has no owner to bank it
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
                if (Stealable(item) && item.m_gridPos.y < mainRows && InHotbar(item.m_gridPos) == hotbar)
                {
                    pool.Add(item);
                }
            }
            return pool;
        }

        private static bool Stealable(ItemDrop.ItemData item) =>
            !item.m_equipped && (item.m_dropPrefab == null || !Unstealable.Contains(item.m_dropPrefab.name));

        // The hotbar is row 0's first eight cells, the ones the 1-8 keys use (Inventory.GetHotbar counts to 8); a grid
        // wider than 8 (PackPanel's) has ordinary cells right of it, robbed before the hotbar like any other.
        private static bool InHotbar(Vector2i cell) => cell.y == 0 && cell.x < HotbarWidth;

        // The stack leaves the inventory now and is held until the creature's owner answers: banked, it is gone; refused
        // or unanswered, it comes back (StealEscrow). Nothing is taken at all when it cannot be held.
        private static void Take(Character victim, EliteController thief, ZNetView creatureView, ItemDrop.ItemData item)
        {
            string name = item.m_shared.m_name;
            int stack = item.m_stack;
            int id = StealEscrow.Hold(((Humanoid)victim).GetInventory(), item);
            if (id == 0)
            {
                return;
            }
            Announce(thief, name, stack);
            CreatureRpc.FireFlash(creatureView, victim.GetCenterPoint(), 2f, "steal");
            ThievingRpc.Send(creatureView, id, item);
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
