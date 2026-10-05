using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// The New folder button at the right end of the folder panel's first row (the up row, or "Blueprints" at the top):
    /// a copy of the build menu's own key badge (the "Q" beside its tabs: its sprite and colour) holding the New folder
    /// icon, brighter under the mouse, with the game's tooltip naming the folder it makes the new one in. Its click is its
    /// own (the row under it does not go up): it asks for the name over the open menu and makes the folder inside the
    /// folder shown. The row's text keeps clear of it.
    /// </summary>
    public static class NewFolderBadge
    {
        private const float Size = 24f;
        private const float Inset = 4f;
        private const string KeyBadge = "InputHelp/MK hints/Left";

        private static UITooltip tooltip;

        /// <summary>Puts the button on the panel's first row (made once per menu with that row).</summary>
        public static void Attach(BuildUiTagButton row, BuildUi menu)
        {
            RectTransform badge = Copy(menu, row.transform);
            badge.name = "OpenKeep New Folder";
            badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(1f, 0.5f);
            badge.anchoredPosition = new Vector2(-Inset, 0f);
            badge.sizeDelta = new Vector2(Size, Size);
            Image icon = TabLook.Image(badge, "Icon", Color.white);
            icon.sprite = BlueprintIcons.Get(BlueprintIcons.FolderNew);
            icon.preserveAspect = true;
            icon.rectTransform.offsetMin = new Vector2(2f, 2f);
            icon.rectTransform.offsetMax = new Vector2(-2f, -2f);
            MakeButton(badge.GetComponent<Image>());
            tooltip = MakeTooltip(badge.gameObject);
            foreach (TMPro.TextMeshProUGUI text in row.m_textMeshes)
                text.rectTransform.offsetMax = new Vector2(-(Size + 2f * Inset), text.rectTransform.offsetMax.y);
        }

        /// <summary>The panel shows another folder: the tooltip names it.</summary>
        public static void Describe(string folder)
        {
            if (tooltip != null)
                tooltip.m_text = BlueprintWords.Format(BlueprintWords.NewFolderDescription, BlueprintEntries.Shown(folder));
        }

        /// <summary>The menu's key badge copied with nothing inside and no layout of its own; a plain dark square without it.</summary>
        private static RectTransform Copy(BuildUi menu, Transform row)
        {
            Transform source = menu.m_tabContainer != null && menu.m_tabContainer.parent != null ? menu.m_tabContainer.parent.Find(KeyBadge) : null;
            if (source == null)
                return TabLook.Image(row, "Badge", TabLook.Band).rectTransform;
            RectTransform badge = (RectTransform)Object.Instantiate(source.gameObject, row, false).transform;
            foreach (Transform child in badge)
                child.gameObject.SetActive(false);
            foreach (Behaviour layout in badge.GetComponents<LayoutGroup>())
                layout.enabled = false;
            foreach (Behaviour fitter in badge.GetComponents<ContentSizeFitter>())
                fitter.enabled = false;
            return badge;
        }

        /// <summary>The click and the hover: the badge brightens under the mouse and darkens while pressed.</summary>
        private static void MakeButton(Image badge)
        {
            badge.raycastTarget = true;
            Button button = badge.gameObject.AddComponent<Button>();
            button.targetGraphic = badge;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.625f, 0.625f, 0.625f, 1f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = colors.normalColor;
            colors.pressedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
            colors.colorMultiplier = 1.6f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(Clicked);
        }

        /// <summary>The game's tooltip (taken from its piece icons), when the game has one to give.</summary>
        private static UITooltip MakeTooltip(GameObject badge)
        {
            UITooltip source = Hud.instance != null && Hud.instance.m_pieceIconPrefab != null ? Hud.instance.m_pieceIconPrefab.GetComponent<UITooltip>() : null;
            if (source == null || source.m_tooltipPrefab == null)
                return null;
            UITooltip made = badge.AddComponent<UITooltip>();
            made.m_tooltipPrefab = source.m_tooltipPrefab;
            made.m_topic = BlueprintWords.NewFolderName;
            made.m_text = BlueprintWords.Format(BlueprintWords.NewFolderDescription, BlueprintEntries.Shown(BlueprintLibrary.CurrentFolder));
            return made;
        }

        private static void Clicked()
        {
            if (NamePrompt.Showing)
                return;
            UITooltip.HideTooltip();
            BlueprintSafe.Run("OpenKeep new folder", BlueprintFolders.AskNewFolder);
        }
    }
}
