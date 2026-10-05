using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// The ghost that follows the mouse while entries of the Blueprints tab are dragged: the pressed entry's icon, see-
    /// through, with the number of entries on a badge when there are several. It lives on the build menu (drawn over
    /// its list) and never catches the mouse, so the folder under it can still be found.
    /// </summary>
    public static class TabGhost
    {
        private const float Size = 52f;

        private static RectTransform ghost;
        private static Image icon;
        private static TMP_Text count;

        public static void Show(Sprite sprite, int entries, PointerEventData data)
        {
            if (ghost == null && !Make())
                return;
            icon.sprite = sprite;
            count.text = entries > 1 ? entries.ToString() : "";
            count.transform.parent.gameObject.SetActive(entries > 1);
            ghost.SetAsLastSibling();
            ghost.gameObject.SetActive(true);
            Move(data);
        }

        /// <summary>Puts the ghost under the mouse (its centre a little below and right of the pointer).</summary>
        public static void Move(PointerEventData data)
        {
            if (ghost == null)
                return;
            RectTransform parent = (RectTransform)ghost.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, data.position, data.pressEventCamera, out Vector2 local))
                ghost.localPosition = local + new Vector2(Size * 0.35f, -Size * 0.35f);
        }

        public static void Hide()
        {
            if (ghost != null)
                ghost.gameObject.SetActive(false);
        }

        /// <summary>The ghost on the build menu, made once per menu; false when there is no menu.</summary>
        private static bool Make()
        {
            BuildUi menu = BlueprintTab.Menu;
            if (menu == null)
                return false;
            ghost = TabLook.Child(menu.transform, "OpenKeep Drag Ghost");
            ghost.anchorMin = ghost.anchorMax = ghost.pivot = new Vector2(0.5f, 0.5f);
            ghost.sizeDelta = new Vector2(Size, Size);
            ghost.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            icon = TabLook.Image(ghost, "Icon", new Color(1f, 1f, 1f, 0.8f));
            icon.preserveAspect = true;
            RectTransform badge = TabLook.Image(ghost, "Badge", TabLook.Band).rectTransform;
            badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(1f, 1f);
            badge.sizeDelta = new Vector2(22f, 18f);
            count = TabLook.Text(badge, "Count", 13f);
            return true;
        }
    }
}
