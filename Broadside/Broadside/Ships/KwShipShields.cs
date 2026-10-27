using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's shield hooks: every railing's hooks of its blueprint (<see cref="KwShieldSlots"/>), numbered
    /// together in the blueprint's order, under the ship's fittings, their shields in the ship's own ZDO
    /// (<see cref="KwShieldRecord"/>, as the hatches keep theirs). The ship's owner runs its physics, so it is never handed
    /// over for a shield: the owner decides and the acting player's game moves the item. Hanging: the shield is offered to
    /// the owner (<c>KW_ShieldHang</c>, with a token), who hangs it if the hook is still empty and answers
    /// (<c>KW_ShieldHung</c>); only then does the player's game take the shield from its inventory, or, if the shield
    /// left it meanwhile, asks the owner to take that very one down again (<c>KW_ShieldUndo</c>). No answer in five seconds:
    /// the player's own copy of the ZDO decides (the shield hanging there: taken; not: kept). Taking: the owner empties the
    /// hook and sends the shield (<c>KW_ShieldGiven</c>) to the player's game, which puts it in the inventory or at their
    /// feet. A ship built from a blueprint starts with empty hooks. Destroyed, its owner drops the shields where they hung.
    /// </summary>
    public sealed class KwShipShields : KwShieldRack
    {
        private const string OfferCall = "KW_ShieldHang", HungCall = "KW_ShieldHung", UndoCall = "KW_ShieldUndo", AskCall = "KW_ShieldTake", GivenCall = "KW_ShieldGiven";
        private const float Wait = 5f;
        private (int Slot, int Token, string Saved, ItemDrop.ItemData Item, float Until)? pending;

        /// <summary>The ship's hooks, made with its first railing: the calls registered, the drop on destroy, the reading of the ZDO.</summary>
        public static KwShipShields Of(Transform ship)
        {
            KwShipShields? rack = ship.GetComponent<KwShipShields>();
            if (rack != null)
                return rack;
            rack = ship.gameObject.AddComponent<KwShipShields>();
            rack.Join(ship.GetComponent<ZNetView>());
            return rack;
        }

        /// <summary>A railing piece's hooks added under the fittings holder (the ship's frame), placed as the piece is.</summary>
        public void Rail(Transform holder, KwBpPiece piece)
        {
            foreach (KwShieldSlot slot in KwShieldSlots.Of(KwShipParts.Saved(piece)))
                AddSlot(holder, piece.Position + piece.Rotation * slot.At, piece.Rotation * slot.Turn);
            Refresh();
        }

        /// <summary>
        /// Every hook laid again in the blueprint's order after a sea edit changed its railings (<see cref="KwSeaRails"/>; the
        /// owner renumbered the hung shields in the ZDO to match, <see cref="KwSeaShields"/>), and the shields shown again.
        /// </summary>
        public void Relay(Transform holder, KwBlueprint blueprint)
        {
            ClearSlots();
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                if (KwRailStretches.IsRail(piece.Prefab))
                {
                    foreach (KwShieldSlot slot in KwShieldSlots.Of(KwShipParts.Saved(piece)))
                        AddSlot(holder, piece.Position + piece.Rotation * slot.At, piece.Rotation * slot.Turn);
                }
            }
            Refresh();
        }

        private void Join(ZNetView view)
        {
            View = view;
            enabled = false;
            view.Register<int, string, int>(OfferCall, (sender, slot, saved, token) => Guard.Run("Broadside ship shield", () => Offered(sender, slot, saved, token)));
            view.Register<int, int, bool>(HungCall, (sender, slot, token, ok) => Guard.Run("Broadside ship shield", () => Answered(slot, token, ok)));
            view.Register<int, string>(UndoCall, (sender, slot, saved) => Guard.Run("Broadside ship shield", () => Undone(slot, saved)));
            view.Register<int>(AskCall, (sender, slot) => Guard.Run("Broadside ship shield", () => Asked(sender, slot)));
            view.Register<int, string>(GivenCall, (sender, slot, saved) => Guard.Run("Broadside ship shield", () => Got(saved)));
            WearNTear? wear = GetComponent<WearNTear>();
            if (wear != null)
                wear.m_onDestroyed += () => KwShipBody.Step("Broadside ship shields dropped", DropAll);
            StartPolling();
        }

        private void OnDestroy()
        {
            if (View == null)
                return;
            foreach (string call in new[] { OfferCall, HungCall, UndoCall, AskCall, GivenCall })
                View.Unregister(call);
        }

        protected override void Hang(Player player, int slot, ItemDrop.ItemData shield)
        {
            if (pending != null)
                return;
            string saved = KwShieldRecord.Pack(shield);
            int token = Random.Range(1, int.MaxValue);
            pending = (slot, token, saved, shield, Time.time + Wait);
            enabled = true;
            View!.InvokeRPC(OfferCall, slot, saved, token);
        }

        protected override void Take(Player player, int slot)
        {
            if (KwShieldRecord.Unpack(Hung().TryGetValue(slot, out string saved) ? saved : "") == null)
                player.Message(MessageHud.MessageType.Center, "That shield is not known to this game");
            else
                View!.InvokeRPC(AskCall, slot);
        }

        // On the owner: the shield hung if the hook is still empty, and the player told.
        private void Offered(long sender, int slot, string saved, int token)
        {
            if (!View!.IsOwner())
                return;
            Dictionary<int, string> hung = Hung();
            bool ok = slot >= 0 && slot < Count && !hung.ContainsKey(slot) && saved.Length > 0;
            if (ok)
            {
                hung[slot] = saved;
                Save(hung);
            }
            View.InvokeRPC(sender, HungCall, slot, token, ok);
        }

        // On the owner: a shield that never left its player's inventory taken down again, if it is the one still hanging.
        private void Undone(int slot, string saved)
        {
            Dictionary<int, string> hung = Hung();
            if (View!.IsOwner() && hung.TryGetValue(slot, out string now) && now == saved)
            {
                hung.Remove(slot);
                Save(hung);
            }
        }

        // On the owner: the hook emptied and its shield sent to the player who asked.
        private void Asked(long sender, int slot)
        {
            Dictionary<int, string> hung = Hung();
            if (!View!.IsOwner() || !hung.TryGetValue(slot, out string saved))
                return;
            hung.Remove(slot);
            Save(hung);
            View.InvokeRPC(sender, GivenCall, slot, saved);
        }

        // On the player's game: the shield the owner took down, to the inventory or the feet.
        private void Got(string saved)
        {
            ItemDrop.ItemData? item = KwShieldRecord.Unpack(saved);
            if (item != null && Player.m_localPlayer != null)
                KwShieldRecord.Give(Player.m_localPlayer, item);
        }

        // On the player's game: the owner's answer to this game's offer.
        private void Answered(int slot, int token, bool ok)
        {
            if (pending == null || pending.Value.Slot != slot || pending.Value.Token != token)
                return;
            if (ok)
                Settle();
            else
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "A shield already hangs there");
            pending = null;
        }

        // The offered shield hangs: taken from the inventory, or, gone from it meanwhile, asked down again.
        private void Settle()
        {
            Player? player = Player.m_localPlayer;
            (int slot, _, string saved, ItemDrop.ItemData item, _) = pending!.Value;
            if (player != null && player.GetInventory().ContainsItem(item))
            {
                player.UnequipItem(item);
                player.GetInventory().RemoveOneItem(item);
            }
            else
                View!.InvokeRPC(UndoCall, slot, saved);
        }

        // No answer in time: the player's copy of the ZDO says whether the offer hung.
        private void Update()
        {
            if (pending == null)
            {
                enabled = false;
                return;
            }
            if (Time.time < pending.Value.Until)
                return;
            if (Hung().TryGetValue(pending.Value.Slot, out string now) && now == pending.Value.Saved)
                Guard.Run("Broadside ship shield", Settle);
            pending = null;
        }
    }
}
