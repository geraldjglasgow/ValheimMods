using System.Text;
using PackPanel.Core;
using PackPanel.Layout;
using PackPanel.Slots;
using UnityEngine;

namespace PackPanel.Consume
{
    /// <summary>
    /// The Food and Mead bar (the user's request, 2026-09-28: the food and mead hotkeys shown in the bottom left of the
    /// screen). In the empty strip under the game's health panel, level with its left edge: a food square with the Food Key
    /// over it (<see cref="ConsumeBarCell"/>), then a square per Mead slot with its Mead Slot key and its mead
    /// (<see cref="ConsumeBarSlots"/>; since 2026-10-05, when the Mead Key and its square went). Food without slots or a
    /// key, or a slot without a key, is left out; nothing shows without any, while dead, or with the module off. A child of
    /// the HUD's root, so it hides with the HUD. Checked from the <c>Hud.Update</c> postfix (<see cref="HudTick"/>) ten times a second, planned again
    /// only when the layout or a key changes, built again only when a square comes or goes or a key changes. Per player (<c>5. Look / Food And Mead Bar</c>); local only,
    /// nothing is sent.
    /// </summary>
    public static class ConsumeBar
    {
        public const string Name = "PackPanel_consumebar";
        private const float Gap = 6f;
        private const float Every = 0.1f;

        /// <summary>The bar's bottom-left corner in HUD units: the health panel's left edge, under its bottom (58).</summary>
        private static readonly Vector2 Corner = new Vector2(50f, 6f);

        private static RectTransform bar;
        private static string built;
        private static float next;
        private static string planned = "";
        private static InventoryLayout plannedLayout;
        private static int plannedKeys = -1;

        public static void Tick(Hud hud)
        {
            if (Time.time < next)
                return;
            next = Time.time + Every;
            Player player = Player.m_localPlayer;
            string plan = Shown(player) ? Plan() : "";
            if (plan.Length > 0 && (bar == null || plan != built))
                Build(hud, plan);
            if (bar == null)
                return;
            bool show = plan.Length > 0;
            if (bar.gameObject.activeSelf != show)
                bar.gameObject.SetActive(show);
            if (show)
                ConsumeBarSlots.Refresh(player);
        }

        private static bool Shown(Player player) =>
            player != null && !player.IsDead() && InventoryState.Active && player == InventoryState.Player && ConsumeSettings.Bar.Value;

        private static string FoodKeyName() => ConsumeKeyNames.Food;

        private static bool FoodDrawn() => InventoryState.CellsOf(SlotKind.Food).Count > 0 && FoodKeyName().Length > 0;

        /// <summary>
        /// What the bar is built from: the food square and each drawn slot with its key; empty when nothing is drawn. Made
        /// again only when the layout (the slots) or a key changed, the same text otherwise.
        /// </summary>
        private static string Plan()
        {
            InventoryLayout layout = InventoryState.Layout;
            if (ReferenceEquals(layout, plannedLayout) && ConsumeKeyNames.Version == plannedKeys)
                return planned;
            plannedLayout = layout;
            plannedKeys = ConsumeKeyNames.Version;
            StringBuilder plan = new StringBuilder();
            if (FoodDrawn())
                plan.Append("food ").Append(FoodKeyName()).Append('|');
            ConsumeBarSlots.Plan(plan);
            planned = plan.ToString();
            return planned;
        }

        private static void Build(Hud hud, string plan)
        {
            if (bar != null)
                Object.Destroy(bar.gameObject);
            bar = null;
            GameObject template = Template(hud);
            if (template == null)
                return;
            bar = MakeBar(hud.m_healthPanel);
            float x = 0f;
            if (FoodDrawn())
            {
                ConsumeBarCell.Group(bar, template, SlotKind.Food, FoodKeyName(), x);
                x += ConsumeBarCell.Size + Gap;
            }
            ConsumeBarSlots.Build(bar, template, x, Gap);
            built = plan;
        }

        /// <summary>The game's first food square (<c>hudroot/healthpanel/food0</c>, the parent of <c>m_foodIcons[0]</c>); null before the HUD is up.</summary>
        private static GameObject Template(Hud hud)
        {
            if (hud == null || hud.m_healthPanel == null || hud.m_foodIcons == null || hud.m_foodIcons.Length == 0 || hud.m_foodIcons[0] == null)
                return null;
            Transform square = hud.m_foodIcons[0].transform.parent;
            return square != null ? square.gameObject : null;
        }

        /// <summary>The bar, a sibling of the health panel under the HUD's root, anchored to the screen's bottom-left corner.</summary>
        private static RectTransform MakeBar(RectTransform healthPanel)
        {
            GameObject go = new GameObject(Name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(healthPanel.parent, false);
            rect.SetSiblingIndex(healthPanel.GetSiblingIndex() + 1);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, ConsumeBarCell.Size);
            rect.anchoredPosition = Corner;
            return rect;
        }
    }
}
