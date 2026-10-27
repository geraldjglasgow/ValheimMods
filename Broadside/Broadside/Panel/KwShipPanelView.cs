using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Broadside
{
    /// <summary>
    /// The ship panel on the HUD, in the minimap's own look (<see cref="KwPanelLook"/>), the place and look of ShipConfig's
    /// panel: under the ship HUD's wind indicator while that shows, else right under the minimap, as wide as the minimap.
    /// It sits under the HUD root, in the HUD's own layer, so it hides with the HUD and the inventory, crafting and other
    /// windows draw over it, as they do over the minimap. Lines: speed, speed multiplier, explore radius, ship health,
    /// other Broadside code's lines (<see cref="KwShipPanel"/>), then, under a thin divider, one per Sailing ability
    /// GrindstoneSkills says the player has unlocked, with a cooldown bar. What they say is <see cref="KwShipPanelLines"/>'.
    /// </summary>
    internal sealed class KwShipPanelView
    {
        public const string Name = "Broadside_ShipPanel";
        private const float StatHeight = 14f, AbilityHeight = 15f, Padding = 4f, Side = 7f, DividerSpace = 3f, Gap = 6f;

        private static readonly Vector3[] corners = new Vector3[4];

        private readonly RectTransform root;
        private readonly CanvasGroup group;
        private readonly TMP_FontAsset? font;
        private readonly GameObject? tipPrefab;
        private readonly RectTransform divider;
        private readonly List<KwPanelRow> others = new List<KwPanelRow>();
        private readonly List<KwPanelRow> abilities = new List<KwPanelRow>();
        private long laidOut = -1;
        private bool interactive = true;

        public KwPanelRow Speed { get; }
        public KwPanelRow Multiplier { get; }
        public KwPanelRow Explore { get; }
        public KwPanelRow Health { get; }
        public KwPanelHover Hover { get; }

        private KwShipPanelView(RectTransform root)
        {
            this.root = root;
            font = KwPanelLook.Font();
            tipPrefab = KwPanelLook.TipPrefab();
            group = root.gameObject.AddComponent<CanvasGroup>();
            KwPanelLook.Frame(root.gameObject.AddComponent<Image>());
            Speed = new KwPanelRow(root, "speed", StatHeight, font, tipPrefab);
            Multiplier = new KwPanelRow(root, "multiplier", StatHeight, font, tipPrefab);
            Explore = new KwPanelRow(root, "explore", StatHeight, font, tipPrefab);
            Health = new KwPanelRow(root, "health", StatHeight, font, tipPrefab);
            divider = KwPanelLook.Block("divider", root, KwPanelLook.Line).rectTransform;
            Hover = new KwPanelHover(root);
        }

        /// <summary>A canvas of its own in the HUD's layer (no sorting of its own) with a raycaster, so <see cref="KwPanelHover"/> sees its lines.</summary>
        public static KwShipPanelView Create(Transform hudRoot)
        {
            RectTransform rect = KwPanelLook.Node(Name, hudRoot);
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.gameObject.AddComponent<Canvas>();
            rect.gameObject.AddComponent<GraphicRaycaster>();
            return new KwShipPanelView(rect);
        }

        public void SetVisible(bool visible)
        {
            if (root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
        }

        /// <summary>The lines are hit-tested only while the cursor is free (inventory, map or a menu open).</summary>
        public void Interactive(bool free)
        {
            if (interactive == free)
                return;
            interactive = free;
            group.blocksRaycasts = free;
        }

        /// <summary>The row drawing other code's line <paramref name="index"/> (<see cref="KwPanelLine.Index"/>), made on first use.</summary>
        public KwPanelRow Other(int index)
        {
            while (others.Count <= index)
            {
                KwPanelRow row = new KwPanelRow(root, "line" + others.Count, StatHeight, font, tipPrefab);
                row.AddBar();
                row.Hide();
                others.Add(row);
            }
            return others[index];
        }

        /// <summary>The row of ability <paramref name="index"/>, with its cooldown bar, made on first use.</summary>
        public KwPanelRow Ability(int index)
        {
            while (abilities.Count <= index)
            {
                KwPanelRow row = new KwPanelRow(root, "ability" + abilities.Count, AbilityHeight, font, tipPrefab);
                row.AddBar();
                abilities.Add(row);
            }
            return abilities[index];
        }

        /// <summary>Abilities from <paramref name="count"/> on hidden (fewer unlocked than rows made).</summary>
        public void HideAbilitiesFrom(int count)
        {
            for (int i = count; i < abilities.Count; i++)
                abilities[i].Hide();
        }

        /// <summary>
        /// Lines placed top to bottom and the panel sized to hold them, when <paramref name="key"/> (which other lines show
        /// and how many abilities, <see cref="KwShipPanelLines"/>) changed since the last layout.
        /// </summary>
        public void Layout(long key, int abilityCount)
        {
            if (key == laidOut)
                return;
            laidOut = key;
            float y = Stack(Health, Stack(Explore, Stack(Multiplier, Stack(Speed, Padding))));
            for (int i = 0; i < others.Count; i++)
                y = Stack(others[i], y, KwShipPanel.Lines[i].Shown);
            divider.gameObject.SetActive(abilityCount > 0);
            if (abilityCount > 0)
                y = PlaceDivider(y);
            for (int i = 0; i < abilityCount; i++)
                y = Stack(abilities[i], y);
            root.sizeDelta = new Vector2(root.sizeDelta.x, y + Padding);
        }

        private static float Stack(KwPanelRow row, float y, bool shown = true)
        {
            if (!shown)
            {
                row.Hide();
                return y;
            }
            row.Place(y, Side);
            return y + row.Height;
        }

        private float PlaceDivider(float y)
        {
            divider.anchorMin = new Vector2(0f, 1f);
            divider.anchorMax = new Vector2(1f, 1f);
            divider.pivot = new Vector2(0.5f, 1f);
            divider.anchoredPosition = new Vector2(0f, -(y + DividerSpace));
            divider.sizeDelta = new Vector2(-2f * Side, 1f);
            return y + 2f * DividerSpace + 1f;
        }

        /// <summary>Under the wind indicator while the ship HUD shows, else under the minimap; as wide as the minimap.</summary>
        public void Place()
        {
            Minimap? map = Minimap.instance;
            RectTransform? small = map != null && map.m_smallRoot != null ? map.m_smallRoot.transform as RectTransform : null;
            if (small == null)
                return;
            small.GetWorldCorners(corners);
            float top = Mathf.Min(corners[0].y, WindIndicatorBottom()) - Gap * root.lossyScale.y;
            Vector3 at = new Vector3((corners[0].x + corners[3].x) * 0.5f, top, root.position.z);
            if (root.position != at)
                root.position = at;
            float width = (corners[3].x - corners[0].x) / root.lossyScale.x;
            if (!Mathf.Approximately(root.sizeDelta.x, width))
                root.sizeDelta = new Vector2(width, root.sizeDelta.y);
        }

        /// <summary>The bottom of the round wind indicator (it turns with the ship, so from its centre), or no limit while it hides.</summary>
        private static float WindIndicatorBottom()
        {
            Hud? hud = Hud.instance;
            RectTransform? wind = hud != null && hud.m_shipHudRoot != null && hud.m_shipHudRoot.activeInHierarchy
                ? hud.m_shipWindIndicatorRoot
                : null;
            return wind != null ? wind.position.y - wind.rect.height * 0.5f * wind.lossyScale.y : float.MaxValue;
        }
    }
}
