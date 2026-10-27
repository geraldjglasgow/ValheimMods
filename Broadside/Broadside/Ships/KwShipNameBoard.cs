using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A name board on a launched ship: its look is in the ship's (<see cref="KwShipLook"/>); here an unseen box over it to
    /// aim at (the "item" layer, its hover and use relayed by <see cref="KwHoverForward"/>), the name drawn on both its faces
    /// (<see cref="KwNameText"/>, screens only), and E opening the game's text box to name the ship, which goes to the
    /// ship's owner (<see cref="KwShipName"/>). Made on every peer, as the ship's other fittings.
    /// </summary>
    public sealed class KwShipNameBoard : MonoBehaviour, Hoverable, Interactable, TextReceiver
    {
        private KwShipName? ship;
        private List<TMP_Text> texts = new List<TMP_Text>();

        /// <summary>The board of a blueprint piece under the fittings holder (the ship's frame), placed as the piece is.</summary>
        public static void Make(Transform ship, Transform holder, KwBpPiece piece)
        {
            GameObject made = new GameObject("name_board") { layer = LayerMask.NameToLayer("item") };
            made.transform.SetParent(holder, false);
            made.transform.SetLocalPositionAndRotation(piece.Position, piece.Rotation);
            GameObject hit = new GameObject("hit") { layer = made.layer };
            hit.transform.SetParent(made.transform, false);
            BoxCollider box = hit.AddComponent<BoxCollider>();
            (box.center, box.size) = (new Vector3(0f, 0f, KwNameBoard.Face / 2f), new Vector3(1.7f, 0.4f, KwNameBoard.Face + 0.06f));
            KwShipNameBoard board = made.AddComponent<KwShipNameBoard>();
            board.ship = KwShipName.Of(ship, piece);
            board.texts = KwNameText.Add(made.transform);
            board.ship.Add(board);
            KwHoverForward.OnColliders(made.transform);
        }

        /// <summary>The name drawn on the board's faces.</summary>
        public void Show(string name) => KwNameText.Show(texts, name);

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || ship == null || TextInput.instance == null || !PrivateArea.CheckAccess(transform.position))
                return false;
            TextInput.instance.RequestText(this, "The ship's name", KwNameBoard.Longest);
            return true;
        }

        public string GetText() => ship != null ? ship.Name : "";

        public void SetText(string text)
        {
            if (ship != null && PrivateArea.CheckAccess(transform.position))
                ship.Ask(text);
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public string GetHoverText() => KwNameBoardPiece.Hover(GetText());

        public string GetHoverName() => "Name board";

        public float GetHoverOffset() => 0f;
    }
}
