using EliteCrafting.Text;
using UnityEngine;

namespace EliteCrafting.Tables.Window
{
    /// <summary>
    /// The Rune Table's window on one inventory screen (rune-table.md section 6): made from the game's crafting panel the
    /// first time it opens (<see cref="PanelBuilder"/>, <see cref="PanelLayout"/>), shown in the crafting panel's place
    /// (its anchors, position and size copied on every open, so a UI mod's layout is followed) while the crafting panel
    /// hides, and filled by the chosen tab. Local player only.
    /// </summary>
    internal sealed class TablePanel
    {
        private readonly InventoryGui _gui;
        private readonly TableTab[] _tabs = { new InscribeTab(), new SacrificeTab(), new SocketTab() };
        private int _tab;

        public TablePanel(InventoryGui gui)
        {
            _gui = gui;
            Parts = PanelParts.From(PanelBuilder.Build(gui.m_crafting));
            PanelLayout.Arrange(Parts, gui, PickTab, i => Current.PickRune(i), i => Current.PickEssence(i));
            Parts.Action?.onClick.AddListener(() => TableWindow.Run(view => Current.Act(view, TableWindow.ShiftHeld)));
            Parts.Extra?.onClick.AddListener(() => TableWindow.Run(view => Current.Extra(view)));
        }

        public PanelParts Parts { get; }

        /// <summary>False once the inventory screen it was made on is gone (a logout).</summary>
        public bool Alive => _gui != null && Parts.Root != null;

        private TableTab Current => _tabs[_tab];

        public void Show()
        {
            Follow();
            Parts.Rect.SetSiblingIndex(_gui.m_crafting.GetSiblingIndex() + 1);
            _gui.m_crafting.gameObject.SetActive(false);
            Parts.Root.SetActive(true);
        }

        /// <summary>Takes the crafting panel's place again when a UI mod moved or resized it (only what changed is set).</summary>
        public void Follow()
        {
            RectTransform crafting = _gui.m_crafting;
            RectTransform rect = Parts.Rect;
            if (rect.anchorMin != crafting.anchorMin || rect.anchorMax != crafting.anchorMax || rect.pivot != crafting.pivot)
            {
                rect.anchorMin = crafting.anchorMin;
                rect.anchorMax = crafting.anchorMax;
                rect.pivot = crafting.pivot;
            }
            if (rect.anchoredPosition != crafting.anchoredPosition || rect.sizeDelta != crafting.sizeDelta)
            {
                rect.anchoredPosition = crafting.anchoredPosition;
                rect.sizeDelta = crafting.sizeDelta;
            }
        }

        public void Hide()
        {
            if (Parts.Root != null)
            {
                Parts.Root.SetActive(false);
            }
            if (_gui != null && _gui.m_crafting != null)
            {
                _gui.m_crafting.gameObject.SetActive(true);
            }
        }

        public void Render(TableView view)
        {
            if (Parts.Title != null)
            {
                Parts.Title.text = Words.Localize(TableWords.Name);
            }
            _tab = _tabs[_tab].Shown ? _tab : 0;
            for (int i = 0; i < Parts.Tabs.Length; i++)
            {
                Parts.Tabs[i].gameObject.SetActive(_tabs[i].Shown);
                Parts.Tabs[i].interactable = i != _tab;
                TMPro.TMP_Text? label = Parts.LabelOf(Parts.Tabs[i]);
                if (label != null)
                {
                    label.text = Words.Localize(_tabs[i].Label);
                }
            }
            Current.Fill(view);
        }

        private void PickTab(int index)
        {
            _tab = Mathf.Clamp(index, 0, _tabs.Length - 1);
            TableWindow.MarkDirty();
        }
    }
}
