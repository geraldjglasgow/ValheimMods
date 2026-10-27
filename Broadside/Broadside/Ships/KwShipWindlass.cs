using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// An anchor windlass on a launched ship (a drawn copy of the blueprint's piece, <see cref="KwAnchor"/>): E at it drops
    /// or weighs the ship's anchor (<see cref="KwShipAnchor"/>), with a word on the screen, and its hover says which. The
    /// first windlass shows the anchor and its cable, led as the blueprint's pieces lead it (<see cref="KwAnchorPlan"/>,
    /// the same as on the dock): weighed, the anchor hangs over the side at the fairlead, the cable from the drum along the
    /// deck through the fairlead and down to its ring; dropped, it lies tipped over on the sea floor where it was let go,
    /// the cable from the fairlead down to it. With no clear place found, the anchor is stowed on the deck while weighed,
    /// and the cable runs over the stem head when it is down. Only drawn: a dedicated server makes none
    /// (<see cref="KwShipBody.Drawn"/>); the anchor itself is the ship's.
    /// </summary>
    public sealed class KwShipWindlass : MonoBehaviour, Interactable, Hoverable
    {
        private const float Tipped = 80f;

        private KwShipAnchor? anchor;
        private KwAnchorLead? lead;
        private Transform? model;
        private LineRenderer? cable;
        private List<Vector3>? weighed, dropped;

        /// <summary>Makes a ship's copy of a windlass work; the one that shows (given its lead) keeps its anchor and gets the cable.</summary>
        public static void Make(Transform made, KwShipAnchor anchor, KwAnchorLead? lead)
        {
            KwShipWindlass windlass = made.gameObject.AddComponent<KwShipWindlass>();
            windlass.anchor = anchor;
            BoxCollider box = made.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.44f, 0f);
            box.size = new Vector3(1.3f, 0.88f, 0.7f);
            KwHoverForward.OnColliders(made);
            Transform? stowed = made.Find(KwAnchor.AnchorName);
            if (lead == null)
            {
                if (stowed != null)
                    stowed.gameObject.SetActive(false);
                return;
            }
            (windlass.lead, windlass.model, windlass.cable) = (lead, stowed, KwAnchorLook.Cable(made));
            if (!lead.Found)
                return;
            KwAnchorLook.Fairlead(made, lead);
            (windlass.weighed, windlass.dropped) = (lead.Cable(true), lead.Cable(false));
        }

        // The anchor where the ship's state puts it, and the cable to it.
        private void LateUpdate()
        {
            if (anchor == null || lead == null || model == null || cable == null)
                return;
            if (lead.Found)
                Led(anchor.Down);
            else
                Unled(anchor.Down);
        }

        // Led through its fairlead: hanging at the ring, or on the sea floor with the cable from the fairlead down to it.
        private void Led(bool down)
        {
            if (!down)
            {
                KwAnchorLook.Hang(model!, lead!);
                KwAnchorLook.Set(cable!, transform, weighed!);
                return;
            }
            Floor();
            KwAnchorLook.Set(cable!, transform, dropped!, model!.position);
        }

        // No clear place: stowed on the deck, no cable; down, the cable from the drum over the stem head to the anchor.
        private void Unled(bool down)
        {
            cable!.enabled = down;
            if (!down)
            {
                KwAnchorLook.Stow(model!);
                return;
            }
            Floor();
            cable.positionCount = 3;
            cable.SetPosition(0, transform.TransformPoint(KwMesh.ToUnity(KwAnchorRoute.DrumTop)));
            cable.SetPosition(1, anchor!.transform.TransformPoint(anchor.Hawse));
            cable.SetPosition(2, model!.position);
        }

        // The anchor tipped over on the sea floor where it was let go.
        private void Floor() =>
            model!.SetPositionAndRotation(anchor!.At + Vector3.up * 0.1f, Quaternion.Euler(0f, anchor.transform.eulerAngles.y, Tipped));

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || anchor == null)
                return false;
            bool down = anchor.Down;
            anchor.Ask();
            user.Message(MessageHud.MessageType.Center, down ? "You weigh the anchor" : "You let go the anchor");
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public string GetHoverText()
        {
            bool down = anchor != null && anchor.Down;
            string kept = lead != null && !lead.Found ? $"\nIts anchor is kept on the deck: {lead.Why}" : "";
            return Localization.instance.Localize(
                $"Anchor windlass{(down ? " (at anchor)" : "")}\n[<color=yellow><b>$KEY_Use</b></color>] {(down ? "Weigh anchor" : "Drop anchor")}{kept}");
        }

        public string GetHoverName() => "Anchor windlass";

        public float GetHoverOffset() => 0f;
    }
}
