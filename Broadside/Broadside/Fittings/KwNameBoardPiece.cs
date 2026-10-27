using System.Collections.Generic;
using PatchGuard;
using TMPro;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A name board on the dock (<see cref="KwNameBoard"/>): the ship's name in its own ZDO (<see cref="KwNameBoard.Key"/>),
    /// set as the game's sign sets its words: E opens the game's text box (<c>TextInput</c>), and the typing player's game
    /// claims the board and writes the name, where the ward lets them. Every peer reads the ZDO once a second and draws it
    /// on both faces (<see cref="KwNameText"/>, screens only); the hover says it. A blueprint saves the name with the board,
    /// and a ship built from it starts with that name (<see cref="KwShipName"/>). A placement ghost shows no name.
    /// </summary>
    public sealed class KwNameBoardPiece : MonoBehaviour, Hoverable, Interactable, TextReceiver
    {
        private ZNetView? view;
        private List<TMP_Text> texts = new List<TMP_Text>();
        private string shown = "\u0001";

        private string Name => view != null && view.IsValid() ? view.GetZDO().GetString(KwNameBoard.Hash) : "";

        private void Start()
        {
            view = GetComponent<ZNetView>();
            if (view == null || view.GetZDO() == null)
                return;
            texts = KwNameText.Add(transform);
            InvokeRepeating(nameof(Refresh), 0f, 1f);
        }

        private void Refresh() => Guard.Run("Broadside name board", () =>
        {
            string now = Name;
            if (now != shown)
                KwNameText.Show(texts, shown = now);
        });

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || view == null || !view.IsValid() || TextInput.instance == null || !PrivateArea.CheckAccess(transform.position))
                return false;
            TextInput.instance.RequestText(this, "The ship's name", KwNameBoard.Longest);
            return true;
        }

        public string GetText() => Name;

        public void SetText(string text)
        {
            if (view == null || !view.IsValid() || !PrivateArea.CheckAccess(transform.position))
                return;
            view.ClaimOwnership();
            view.GetZDO().Set(KwNameBoard.Hash, KwNameBoard.Clean(text));
            Refresh();
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public string GetHoverText() => Hover(Name);

        /// <summary>A name board's hover: the name (as typed, nothing in it read as markup) and what E does.</summary>
        public static string Hover(string name)
        {
            string use = Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] ");
            return name.Length == 0 ? "Name board\n" + use + "Name the ship" : $"Name board: <noparse>{name}</noparse>\n{use}Rename the ship";
        }

        public string GetHoverName() => "Name board";

        public float GetHoverOffset() => 0f;
    }
}
