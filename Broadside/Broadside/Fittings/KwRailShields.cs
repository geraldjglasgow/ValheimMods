using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A railing part's shield hooks on the dock (<see cref="KwShieldSlots"/>, <see cref="KwShieldRack"/>), its hung shields
    /// in the part's own ZDO. Hanging and taking go the way the game's item stand goes: the acting player's game first owns
    /// the railing (claiming it when nobody does, else asking its owner by RPC, <see cref="AskOwner"/>, who hands it
    /// over), then does the whole change itself in one step: the ZDO written and the shield taken from or put into its own
    /// inventory together, so a shield is never made twice or lost. Two seconds without the railing handed over and
    /// nothing changes. The hooks are laid again each time the part builds (<see cref="KwGenerated"/>). Broken or removed
    /// (on its owner), the railing drops its shields where they hung.
    /// </summary>
    public sealed class KwRailShields : KwShieldRack
    {
        private const string AskOwner = "KW_ShieldOwn";
        private const float Wait = 2f;
        private (bool Hang, int Slot, ItemDrop.ItemData? Item, float Until)? pending;
        private bool joined;

        /// <summary>A railing part's hooks laid (again) from its saved text, on every peer.</summary>
        public static void Fit(GameObject part, string saved)
        {
            KwRailShields rack = part.GetComponent<KwRailShields>() ?? part.AddComponent<KwRailShields>();
            rack.Lay(saved);
        }

        private void Lay(string saved)
        {
            View ??= GetComponent<ZNetView>();
            if (View == null || View.GetZDO() == null)
                return;
            Join();
            ClearSlots();
            foreach (KwShieldSlot slot in KwShieldSlots.Of(saved))
                AddSlot(transform, slot.At, slot.Turn);
            Refresh();
        }

        // Once: the owner's hand-over call, the drop when the railing is destroyed, and the reading of the ZDO.
        private void Join()
        {
            if (joined)
                return;
            joined = true;
            enabled = false;
            View!.Register(AskOwner, sender => Guard.Run("Broadside shield hook owner", () => HandOver(sender)));
            WearNTear? wear = GetComponent<WearNTear>();
            if (wear != null)
                wear.m_onDestroyed += () => KwShipBody.Step("Broadside shields dropped", DropAll);
            StartPolling();
        }

        // On the owner: the railing handed to the player who asked, as the game's item stand does.
        private void HandOver(long sender)
        {
            if (View != null && View.IsValid() && View.IsOwner())
                View.GetZDO().SetOwner(sender);
        }

        protected override void Hang(Player player, int slot, ItemDrop.ItemData shield) => Begin(true, slot, shield);

        protected override void Take(Player player, int slot) => Begin(false, slot, null);

        // The change waits until this game owns the railing.
        private void Begin(bool hang, int slot, ItemDrop.ItemData? item)
        {
            pending = (hang, slot, item, Time.time + Wait);
            if (!View!.IsOwner())
            {
                if (!View.HasOwner())
                    View.ClaimOwnership();
                else
                    View.InvokeRPC(AskOwner);
            }
            enabled = true;
        }

        private void Update()
        {
            if (pending == null || View == null || !View.IsValid())
            {
                enabled = false;
                return;
            }
            if (View.IsOwner())
                Guard.Run("Broadside shield hook", Finish);
            else if (Time.time > pending.Value.Until)
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "The railing is busy: try again");
            else
                return;
            pending = null;
            enabled = false;
        }

        // Owning the railing: the shield moved between the hook and the inventory, both at once.
        private void Finish()
        {
            Player? player = Player.m_localPlayer;
            if (pending == null || player == null)
                return;
            (bool hang, int slot, ItemDrop.ItemData? item, _) = pending.Value;
            System.Collections.Generic.Dictionary<int, string> hung = Hung();
            if (hang && item != null && !hung.ContainsKey(slot) && player.GetInventory().ContainsItem(item))
            {
                hung[slot] = KwShieldRecord.Pack(item);
                Save(hung);
                player.UnequipItem(item);
                player.GetInventory().RemoveOneItem(item);
            }
            else if (!hang && hung.TryGetValue(slot, out string saved) && KwShieldRecord.Unpack(saved) is ItemDrop.ItemData back)
            {
                hung.Remove(slot);
                Save(hung);
                KwShieldRecord.Give(player, back);
            }
        }
    }
}
