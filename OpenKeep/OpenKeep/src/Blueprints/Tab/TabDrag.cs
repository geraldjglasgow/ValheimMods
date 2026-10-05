using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// The drag handle on a piece button that shows a blueprint of the Blueprints tab (enabled only then, so any other
    /// button's drag still reaches the list's scrolling). Past the event system's drag threshold a press on the entry
    /// becomes a drag of that blueprint, or of every pick when it is one of them, and no click follows; a ghost with the
    /// count follows the mouse, the folder under it lights up (a row of the folder panel - its first row for the folder
    /// above - or a part of the breadcrumb), and letting go there moves the blueprints in (<see cref="TabMoves"/>).
    /// Anywhere else nothing happens. The wheel still scrolls the list (the handle takes drags only). No drag starts
    /// while a name box is up.
    /// </summary>
    public sealed class TabDrag : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static List<Piece> dragged;
        private static FolderTarget zone;
        private static int releasedFrame = -1;

        public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = true;

        public void OnBeginDrag(PointerEventData data)
        {
            Piece piece = GetComponent<BuildUiPieceButton>()?.Piece;
            if (data.button != PointerEventData.InputButton.Left || !TabPicks.Pickable(piece) || !BlueprintTab.Showing || NamePrompt.Showing)
                return;
            data.eligibleForClick = false;
            dragged = TabPicks.Contains(piece) ? TabPicks.Ordered() : new List<Piece> { piece };
            releasedFrame = -1;
            TabGhost.Show(piece.m_icon, dragged.Count, data);
        }

        public void OnDrag(PointerEventData data)
        {
            if (dragged == null)
                return;
            TabGhost.Move(data);
            SetZone(ZoneUnder(data));
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (dragged == null)
                return;
            FolderTarget into = ZoneUnder(data);
            List<Piece> moving = dragged;
            Cancel();
            if (into != null)
                BlueprintSafe.Run("OpenKeep blueprint drop", () => TabMoves.Into(moving, into.Folder));
        }

        /// <summary>The panel row or breadcrumb part under the mouse that can take the drop, or null.</summary>
        private static FolderTarget ZoneUnder(PointerEventData data)
        {
            GameObject over = data.pointerCurrentRaycast.gameObject;
            FolderTarget found = over != null ? over.GetComponentInParent<FolderTarget>() : null;
            return found != null && found.Takes() ? found : null;
        }

        /// <summary>Lights the folder the drop would go into, and only that one.</summary>
        private static void SetZone(FolderTarget area)
        {
            if (area == zone)
                return;
            if (zone != null)
                zone.SetLit(false);
            zone = area;
            if (zone != null)
                zone.SetLit(true);
        }

        /// <summary>Ends a drag without a drop: the ghost goes, the target mark goes.</summary>
        private static void Cancel()
        {
            dragged = null;
            TabGhost.Hide();
            SetZone(null);
        }

        /// <summary>Per frame: a drag the event system lost (the menu closed, the button released elsewhere) ends.</summary>
        public static void Tick()
        {
            if (dragged == null)
                return;
            if (!BlueprintTab.Showing)
            {
                Cancel();
                return;
            }
            if (ZInput.GetMouseButton(0))
                releasedFrame = -1;
            else if (releasedFrame < 0)
                releasedFrame = Time.frameCount;
            else if (Time.frameCount > releasedFrame + 2)
                Cancel();
        }
    }
}
