using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// The bench window's objects, made the first time it opens in a world (again after the game's GUI was rebuilt): a
    /// canvas of its own beside the game's screens (scaled by the game's own GuiScaler, sorted above the inventory and
    /// below the game's text box and pop-ups) holding the window - the crafting panel's shadow and wood background, its
    /// title in the game's font with the braid line under it, the two lists side by side (<see cref="BenchPaneView"/>), a
    /// status line and Close. Every part is a copy of the game's own (<see cref="BenchCopies"/>), so the window is the
    /// game's wood panel, or PackPanel's timber when PackPanel skins wood panels.
    /// </summary>
    public static class BenchUi
    {
        public const float Width = 1240f;
        public const float Height = 720f;
        private const int SortingOrder = 650;
        private const float Side = 34f;
        private const float Top = 84f;
        private const float Bottom = 70f;
        private const float Between = 24f;

        private static GameObject canvas;

        public static RectTransform Window { get; private set; }
        public static TMP_Text Title { get; private set; }
        public static TMP_Text Status { get; private set; }
        public static BenchPaneView Left { get; private set; }
        public static BenchPaneView Right { get; private set; }

        public static bool Ready => canvas != null;

        public static bool Shown => canvas != null && canvas.activeSelf;

        public static void Show(bool on)
        {
            if (canvas != null && canvas.activeSelf != on)
                canvas.SetActive(on);
        }

        /// <summary>Makes the window when it is missing; false while the game's inventory screen is not there to copy from.</summary>
        public static bool Ensure(IBenchPane library, IBenchPane pool)
        {
            if (canvas != null)
                return true;
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || gui.m_crafting == null || gui.m_recipeElementPrefab == null || gui.m_craftButton == null)
                return false;
            canvas = MakeCanvas(gui);
            BlueprintSafe.Run("OpenKeep blueprint bench window", () => Build(gui, library, pool));
            if (Left == null || Right == null)
            {
                Object.Destroy(canvas);
                canvas = null;
            }
            return canvas != null;
        }

        private static GameObject MakeCanvas(InventoryGui gui)
        {
            GameObject go = new GameObject("OpenKeep_BlueprintBench", typeof(RectTransform));
            go.SetActive(false);
            go.layer = gui.gameObject.layer;
            go.transform.SetParent(gui.transform.parent, false);
            Canvas own = go.AddComponent<Canvas>();
            Canvas source = gui.GetComponent<Canvas>();
            own.renderMode = RenderMode.ScreenSpaceOverlay;
            own.sortingOrder = SortingOrder;
            if (source != null)
                own.additionalShaderChannels = source.additionalShaderChannels;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            CanvasScaler scale = gui.GetComponent<CanvasScaler>();
            if (scale != null)
                scaler.uiScaleMode = scale.uiScaleMode;
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<GuiScaler>();
            return go;
        }

        private static void Build(InventoryGui gui, IBenchPane library, IBenchPane pool)
        {
            Left = Right = null;
            Window = BenchCopies.Rect("Window", canvas.transform);
            Window.anchorMin = Window.anchorMax = Window.pivot = new Vector2(0.5f, 0.5f);
            Window.sizeDelta = new Vector2(Width, Height);
            Backdrop(gui.m_crafting);
            Heading(gui.m_crafting);
            float paneWidth = (Width - 2f * Side - Between) / 2f;
            Rect area = new Rect(Side, -Top, paneWidth, Height - Top - Bottom);
            Left = BenchPaneView.Build(gui, Window, library, area);
            area.x += paneWidth + Between;
            Right = BenchPaneView.Build(gui, Window, pool, area);
            Footer(gui);
        }

        /// <summary>The crafting panel's soft shadow and its wood background, stretched over the window.</summary>
        private static void Backdrop(RectTransform crafting)
        {
            Transform darken = crafting.Find("Darken");
            if (darken != null)
                BenchCopies.Fill((RectTransform)BenchCopies.Copy(darken, Window, "Darken").transform, -50f);
            Transform background = crafting.Find("Bkg");
            if (background != null)
                BenchCopies.Fill((RectTransform)BenchCopies.Copy(background, Window, "Bkg").transform, 0f);
        }

        private static void Heading(RectTransform crafting)
        {
            Title = BenchCopies.Text(crafting.Find("topic").GetComponent<TMP_Text>(), Window, "Title", 32f, TextAlignmentOptions.Center);
            BenchCopies.Place(Title.rectTransform, 0f, -14f, Width, 46f);
            Title.text = Core.Language.Localize(BenchWords.Name);
            Transform braid = crafting.Find("BraidLineHorisontalMedium");
            if (braid == null)
                return;
            RectTransform line = (RectTransform)BenchCopies.Copy(braid, Window, "Braid").transform;
            BenchCopies.Place(line, (Width - 520f) / 2f, -58f, 520f, 14f);
        }

        private static void Footer(InventoryGui gui)
        {
            Status = BenchCopies.Text(Title, Window, "Status", 16f, TextAlignmentOptions.MidlineLeft);
            Status.color = new Color(0.86f, 0.8f, 0.68f);
            BenchCopies.Place(Status.rectTransform, Side, -(Height - Bottom + 14f), Width - 2f * Side - 160f, 40f);
            Button close = BenchCopies.Button(gui.m_craftButton, Window, "Close", BenchWords.Close, BenchWindow.Close);
            BenchCopies.Place((RectTransform)close.transform, Width - Side - 140f, -(Height - Bottom + 12f), 140f, 44f);
        }
    }
}
