using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The "Blueprints" tab of the game's build menu (BuildUi: By Usage, By Material, Recent, Favorites, then this one).
    /// Made once per menu when the menu wakes: a copy of the last tab under the same parent (its horizontal layout
    /// places it), labelled, with its own piece list (<see cref="BlueprintPieceList"/>) and a place in the menu's tab
    /// handler so the tab keys reach it. It shows only while blueprints are on and the hammer is the build tool; hidden,
    /// the tab keys skip it and a menu that showed it goes back to the first tab. The other tabs never list the
    /// entries (<see cref="BlueprintListFilterPatch"/>).
    /// </summary>
    public static class BlueprintTab
    {
        private static BuildUi menu;
        private static Button button;
        private static int index = -1;

        /// <summary>The tab was made for the build menu that exists now.</summary>
        public static bool Installed => menu != null && button != null;

        /// <summary>The build menu the tab was made for, or null.</summary>
        public static BuildUi Menu => Installed ? menu : null;

        /// <summary>The build menu is open on this tab.</summary>
        public static bool Showing => Installed && menu.gameObject.activeSelf && menu.m_currentPieceList == index;

        /// <summary>The menu's button under the mouse (or chosen with a gamepad) while it shows a piece, or null.</summary>
        public static BuildUiPieceButton HoveredButton
        {
            get
            {
                BuildUiPieceButton hovered = Installed ? menu.m_currentHoveredPieceButton : null;
                return hovered != null && hovered.gameObject.activeInHierarchy ? hovered : null;
            }
        }

        /// <summary>BuildUi.Awake postfix: adds the tab, unless the menu is not shaped as expected (then the entries stay in "All").</summary>
        public static void Install(BuildUi ui)
        {
            int lists = ui.m_pieceLists.Count;
            if (ui.m_tabButtons.Count != lists || ui.m_tabHandler == null || ui.m_tabHandler.m_tabs.Count != lists || ui.m_tabContainer.childCount == 0)
            {
                Plugin.Log.LogWarning("OpenKeep: the build menu's tabs are not as expected; the blueprints stay in the hammer's own lists");
                return;
            }
            menu = ui;
            index = lists;
            ui.m_pieceLists.Add(new BlueprintPieceList());
            button = MakeButton(ui.m_tabContainer);
            ui.m_tabButtons.Add(button);
            ui.m_tabHandler.m_tabs.Add(new TabHandler.Tab { m_button = button, m_onClick = new UnityEvent() });
            Tab.TabLook.Capture(ui);
            Tab.TabBox.Install(ui);
            Tab.FolderPanel.Install(ui);
            Tab.Breadcrumb.Install(ui);
            Show(false);
        }

        /// <summary>A copy of the last tab with no click of its own but ours, and our label on every text in it.</summary>
        private static Button MakeButton(RectTransform container)
        {
            GameObject copy = Object.Instantiate(container.GetChild(container.childCount - 1).gameObject, container, false);
            copy.name = "OpenKeep Blueprints";
            Button made = copy.GetComponent<Button>();
            made.onClick = new Button.ButtonClickedEvent();
            int tab = index;
            made.onClick.AddListener(() => BlueprintSafe.Run("OpenKeep blueprints tab", () => menu.SelectPieceList(tab)));
            foreach (TMP_Text text in copy.GetComponentsInChildren<TMP_Text>(true))
                text.text = Core.Language.Localize(BlueprintWords.Tab);
            return made;
        }

        /// <summary>Per frame: the tab shows exactly while blueprints are on and the menu is the hammer's.</summary>
        public static void Tick()
        {
            if (!Installed)
                return;
            bool wanted = BlueprintSettings.Enabled && HammerTable.Is(menu.m_currentBuildTool);
            if (button.gameObject.activeSelf != wanted)
                Show(wanted);
            if (wanted && menu.gameObject.activeSelf)
                Tab.TabMemory.OnTab = menu.m_currentPieceList == index;
            Tab.FolderPanel.Sync();
            Tab.Breadcrumb.Sync();
        }

        /// <summary>
        /// BuildUi.OpenBuildMenu postfix: the game keeps its tab only while the build tool stays the same and starts a new
        /// menu on its first tab, so a menu opened with the hammer goes back to the Blueprints tab when that was the last
        /// tab shown with it (remembered across restarts, <see cref="Tab.TabMemory"/>).
        /// </summary>
        public static void Opened()
        {
            if (!Installed)
                return;
            bool wanted = BlueprintSettings.Enabled && HammerTable.Is(menu.m_currentBuildTool);
            if (button.gameObject.activeSelf != wanted)
                Show(wanted);
            if (wanted && Tab.TabMemory.OnTab && menu.gameObject.activeSelf && menu.m_currentPieceList != index)
                menu.SelectPieceList(index);
        }

        /// <summary>Shows or hides the tab; hidden, the tab keys skip it (a tab without a button) and the menu leaves it.</summary>
        private static void Show(bool on)
        {
            button.gameObject.SetActive(on);
            menu.m_tabHandler.m_tabs[index].m_button = on ? button : null;
            if (on || menu.m_currentPieceList != index)
                return;
            if (menu.gameObject.activeSelf && menu.m_currentBuildTool != null)
                menu.SelectPieceList(0);
            else
                menu.m_currentPieceList = 0;
        }

        /// <summary>
        /// The entries changed: an open menu showing the tab draws its buttons again (a new folder, a renamed file), and the
        /// piece info follows the button under the mouse, which may now show another entry.
        /// </summary>
        public static void Redraw()
        {
            if (!Installed || !menu.gameObject.activeSelf || menu.m_currentPieceList != index || menu.m_currentBuildTool == null)
                return;
            menu.UpdatePieceButtons();
            Tab.FolderPanel.Sync();
            Tab.Breadcrumb.Sync();
            Piece hovered = Hovered();
            if (hovered != null && Hud.instance != null)
                Hud.instance.OnHoverPiece(hovered);
        }

        /// <summary>The entry under the mouse in the open menu: the hovered button's piece (buttons are reused when the list is drawn again), else the game's.</summary>
        public static Piece Hovered()
        {
            BuildUiPieceButton hovered = HoveredButton;
            if (hovered != null)
                return hovered.Piece;
            return Hud.instance != null ? Hud.instance.m_hoveredPiece : null;
        }
    }
}
