using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A row of shield hooks kept in one ZDO (<see cref="KwShieldRecord"/>): a railing part's on the dock
    /// (<see cref="KwRailShields"/>), or every railing's on a launched ship (<see cref="KwShipShields"/>). Each hook is an
    /// unseen box over the rail (the "item" layer: the player's look and E find it, building and removing rays pass it),
    /// whose hover and use go to the rack (<see cref="KwShieldHook"/>, through <see cref="KwHoverForward"/>). With a shield
    /// in hand (or a shield used from the hotbar) E on an empty hook hangs it, E on a hung one takes it back; how the item
    /// leaves or reaches the inventory is the rack's own (<see cref="Hang"/>, <see cref="Take"/>), so it is never made twice
    /// or lost. Every peer reads the ZDO twice a second and shows the hung shields where there is a screen
    /// (<see cref="KwShieldShow"/>). When the railing or the ship is destroyed, its owner drops every hung shield where it hung.
    /// </summary>
    public abstract class KwShieldRack : MonoBehaviour
    {
        private const float Poll = 0.5f;
        private readonly List<Transform> slots = new List<Transform>();
        private readonly Dictionary<int, GameObject> shown = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, string> names = new Dictionary<int, string>();
        private string seen = "\u0001";

        /// <summary>The network view whose ZDO holds the hooks' shields.</summary>
        protected ZNetView? View { get; set; }

        /// <summary>How many hooks the rack has.</summary>
        public int Count => slots.Count;

        /// <summary>A hook added under a parent at a place, its +z out of the hull; returns its number.</summary>
        protected int AddSlot(Transform parent, Vector3 at, Quaternion turn)
        {
            GameObject slot = new GameObject("shield_hook" + slots.Count) { layer = LayerMask.NameToLayer("item") };
            slot.transform.SetParent(parent, false);
            slot.transform.SetLocalPositionAndRotation(at, turn);
            GameObject hit = new GameObject("hit") { layer = slot.layer };
            hit.transform.SetParent(slot.transform, false);
            BoxCollider box = hit.AddComponent<BoxCollider>();
            // across the rail from a little inboard of it to the shield's face, from the rail's foot to its top
            (box.center, box.size) = (new Vector3(0f, 0.04f, -0.01f), new Vector3(0.9f, 0.68f, 0.46f));
            KwShieldHook hook = slot.AddComponent<KwShieldHook>();
            (hook.Rack, hook.Slot) = (this, slots.Count);
            KwHoverForward.OnColliders(slot.transform);
            slots.Add(slot.transform);
            seen = "\u0001";
            return slots.Count - 1;
        }

        /// <summary>Every hook taken away (its shields' looks with it), before the hooks are laid again.</summary>
        protected void ClearSlots()
        {
            foreach (Transform slot in slots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            slots.Clear();
            shown.Clear();
            names.Clear();
            seen = "\u0001";
        }

        /// <summary>Starts reading the ZDO twice a second.</summary>
        protected void StartPolling() => InvokeRepeating(nameof(Refresh), Poll, Poll);

        /// <summary>The hooks' shields as the ZDO holds them now.</summary>
        protected Dictionary<int, string> Hung() => KwShieldRecord.Read(View != null && View.IsValid() ? View.GetZDO().GetString(KwShieldRecord.Hash) : "");

        /// <summary>The hooks' shields written to the ZDO (only ever on its owner) and shown at once.</summary>
        protected void Save(Dictionary<int, string> hung)
        {
            View!.GetZDO().Set(KwShieldRecord.Hash, KwShieldRecord.Write(hung));
            Refresh();
        }

        /// <summary>Where a hook is in the world.</summary>
        protected Vector3 SlotAt(int slot) => slot >= 0 && slot < slots.Count && slots[slot] != null ? slots[slot].position : transform.position;

        /// <summary>The hung shields drawn again when the ZDO's text changed since they were last drawn (or the hooks were laid again).</summary>
        protected void Refresh() => Guard.Run("Broadside shield hooks", Redraw);

        private void Redraw()
        {
            string text = View != null && View.IsValid() ? View.GetZDO().GetString(KwShieldRecord.Hash) : "";
            if (text == seen)
                return;
            seen = text;
            foreach (GameObject old in shown.Values)
            {
                if (old != null)
                    Destroy(old);
            }
            shown.Clear();
            names.Clear();
            foreach (KeyValuePair<int, string> entry in KwShieldRecord.Read(text))
                Show(entry.Key, entry.Value);
        }

        // One hook's shield: its name for the hover, its model where there is a screen.
        private void Show(int slot, string saved)
        {
            ItemDrop.ItemData? item = KwShieldRecord.Unpack(saved);
            if (item == null || slot < 0 || slot >= slots.Count)
                return;
            names[slot] = item.m_shared.m_name;
            if (KwShipBody.Drawn && KwShieldShow.Hang(slots[slot], item) is GameObject model)
                shown[slot] = model;
        }

        /// <summary>A hook's hover: what hangs there and what E does.</summary>
        public string HoverOf(int slot, Humanoid? user)
        {
            if (names.TryGetValue(slot, out string name))
                return Localization.instance.Localize($"Shield hook ({name})\n[<color=yellow><b>$KEY_Use</b></color>] Take the shield");
            return Localization.instance.Localize(KwShieldRecord.Hangs(user?.LeftItem)
                ? "Shield hook\n[<color=yellow><b>$KEY_Use</b></color>] Hang your shield here" : "Shield hook\nHold a shield to hang it here");
        }

        /// <summary>E on a hook (item null: the shield in hand), or a shield used on it from the hotbar.</summary>
        public bool Use(Humanoid user, int slot, ItemDrop.ItemData? item)
        {
            if (user is not Player player || player != Player.m_localPlayer || View == null || !View.IsValid())
                return false;
            if (!PrivateArea.CheckAccess(SlotAt(slot)))
                return true;
            if (Hung().ContainsKey(slot))
            {
                if (item == null)
                    Take(player, slot);
                return item == null;
            }
            ItemDrop.ItemData? shield = item ?? player.LeftItem;
            if (KwShieldRecord.Hangs(shield))
                Hang(player, slot, shield!);
            else
                player.Message(MessageHud.MessageType.Center, item == null ? "Hold a shield to hang it here" : "Only a shield hangs here");
            return true;
        }

        /// <summary>Hangs the player's shield on an empty hook, taking it from the inventory only once it hangs.</summary>
        protected abstract void Hang(Player player, int slot, ItemDrop.ItemData shield);

        /// <summary>Takes the shield from a hook back to the player.</summary>
        protected abstract void Take(Player player, int slot);

        /// <summary>On the owner, the railing or ship destroyed: every hung shield dropped where it hung, and the hooks emptied.</summary>
        public void DropAll()
        {
            if (View == null || !View.IsValid() || !View.IsOwner())
                return;
            foreach (KeyValuePair<int, string> entry in Hung())
                KwShieldRecord.Drop(entry.Value, SlotAt(entry.Key) + Vector3.up * 0.3f, Quaternion.identity);
            View.GetZDO().Set(KwShieldRecord.Hash, "");
        }
    }

    /// <summary>One shield hook's hover and use, handed to its rack (<see cref="KwShieldRack"/>).</summary>
    public sealed class KwShieldHook : MonoBehaviour, Hoverable, Interactable
    {
        public KwShieldRack? Rack;
        public int Slot;

        public bool Interact(Humanoid user, bool hold, bool alt) => !hold && Rack != null && Rack.Use(user, Slot, null);

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => Rack != null && Rack.Use(user, Slot, item);

        public string GetHoverText() => Rack != null ? Rack.HoverOf(Slot, Player.m_localPlayer) : "";

        public string GetHoverName() => "Shield hook";

        public float GetHoverOffset() => 0f;
    }
}
