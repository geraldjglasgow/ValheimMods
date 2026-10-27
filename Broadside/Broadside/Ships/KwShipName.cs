using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's name, from its name boards (<see cref="KwNameBoard"/>): kept in the ship's own ZDO
    /// (<see cref="KwNameBoard.Key"/>), set by its owner on a call (<c>KW_ShipName</c>) from any player's E on one of its
    /// boards (<see cref="KwShipNameBoard"/>), where the ward lets them. Until it is set, the name the blueprint's first
    /// named board was saved with; the owner writes that into the ZDO once the ship is built. Every named ship's map pin
    /// shows it too: the owner copies it to the pin's name (<see cref="KwShipKeys.NameKey"/>). Every peer reads it once a
    /// second and draws it on every board, and the helm's hover starts with it (<see cref="KwShipNameHover"/>).
    /// </summary>
    public sealed class KwShipName : MonoBehaviour
    {
        private const string Call = "KW_ShipName";
        private readonly List<KwShipNameBoard> boards = new List<KwShipNameBoard>();
        private ZNetView? view;
        private string saved = "", shown = "\u0001";
        private bool adopted;

        /// <summary>The ship's name: the one set on it, else the blueprint's.</summary>
        public string Name => view != null && view.IsValid() && view.GetZDO().GetString(KwNameBoard.Hash, out string set) ? set : saved;

        /// <summary>The ship's name keeper, made with its first board, which gives it the blueprint's name if it has one.</summary>
        public static KwShipName Of(Transform ship, KwBpPiece board)
        {
            KwShipName keeper = ship.GetComponent<KwShipName>() ?? ship.gameObject.AddComponent<KwShipName>();
            if (keeper.view == null)
            {
                keeper.view = ship.GetComponent<ZNetView>();
                keeper.view.Register<string>(Call, (sender, text) => Guard.Run("Broadside ship name", () => keeper.Named(text)));
                keeper.InvokeRepeating(nameof(Refresh), 0.5f, 1f);
            }
            if (keeper.saved.Length == 0)
                keeper.saved = KwNameBoard.Clean(KwBpData.Text(board.Data, KwNameBoard.Hash));
            return keeper;
        }

        /// <summary>A board of this ship, to draw the name on.</summary>
        public void Add(KwShipNameBoard board) => boards.Add(board);

        /// <summary>Asks the ship's owner to name it.</summary>
        public void Ask(string text)
        {
            if (view != null && view.IsValid())
                view.InvokeRPC(Call, KwNameBoard.Clean(text));
        }

        // On the owner: the name kept, and given to the map pin when there is one.
        private void Named(string text)
        {
            if (view == null || !view.IsValid() || !view.IsOwner())
                return;
            string clean = KwNameBoard.Clean(text);
            view.GetZDO().Set(KwNameBoard.Hash, clean);
            if (clean.Length > 0)
                view.GetZDO().Set(KwShipKeys.NameHash, clean);
        }

        private void Refresh() => Guard.Run("Broadside ship name", () =>
        {
            Adopt();
            string now = Name;
            if (now == shown)
                return;
            shown = now;
            foreach (KwShipNameBoard board in boards)
            {
                if (board != null)
                    board.Show(now);
            }
        });

        // Once, on the owner: a new ship takes the blueprint's name into its ZDO.
        private void Adopt()
        {
            if (adopted || view == null || !view.IsValid() || !view.IsOwner())
                return;
            adopted = true;
            if (saved.Length > 0 && !view.GetZDO().GetString(KwNameBoard.Hash, out _))
                Named(saved);
        }

        private void OnDestroy()
        {
            if (view != null)
                view.Unregister(Call);
        }
    }

    /// <summary>The helm's hover on a named Broadside ship starts with its name.</summary>
    [HarmonyPatch(typeof(ShipControlls), nameof(ShipControlls.GetHoverText))]
    public static class KwShipNameHover
    {
        [HarmonyPostfix]
        private static void Postfix(ShipControlls __instance, ref string __result)
        {
            Ship? ship = __instance.m_ship;
            if (ship != null && ship.TryGetComponent(out KwShipName name) && name.Name.Length > 0)
                __result = $"<noparse>{name.Name}</noparse>\n" + __result;
        }
    }
}
