using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShipConfig
{
    /// <summary>
    /// The ship panel on the HUD, in the minimap's own look (<see cref="PanelLook"/>): under the ship HUD's wind
    /// indicator while that shows, else right under the minimap, as wide as the minimap. It sits under the HUD root, in
    /// the HUD's own layer, so it hides with the HUD and the inventory, crafting and other windows draw over it, as they
    /// do over the minimap. Lines: speed, speed multiplier, exploration radius, then, under a thin divider, one per
    /// Sailing ability GrindstoneSkills says the player has unlocked, with a cooldown bar.
    /// </summary>
    public sealed class ShipPanel
    {
        private const float StatHeight = 14f;
        private const float AbilityHeight = 15f;
        private const float Padding = 4f;
        private const float Side = 7f;
        private const float DividerSpace = 3f;
        private const float Gap = 6f;
        private const float VanillaExploreRadius = 100f;

        private static readonly Vector3[] corners = new Vector3[4];

        private readonly RectTransform root;
        private readonly CanvasGroup group;
        private readonly TMP_FontAsset font;
        private readonly GameObject tipPrefab;
        private readonly PanelRow speed;
        private readonly PanelRow multiplier;
        private readonly PanelRow explore;
        private readonly RectTransform divider;
        private readonly List<PanelRow> abilities = new List<PanelRow>();
        private int laidOut = -1;
        private bool interactive = true;

        public PanelHover Hover { get; }

        private ShipPanel(RectTransform root)
        {
            this.root = root;
            font = PanelLook.Font();
            tipPrefab = PanelLook.TipPrefab();
            group = root.gameObject.AddComponent<CanvasGroup>();
            PanelLook.Frame(root.gameObject.AddComponent<Image>());
            speed = new PanelRow(root, "ShipConfig_speed", StatHeight, font, tipPrefab);
            multiplier = new PanelRow(root, "ShipConfig_multiplier", StatHeight, font, tipPrefab);
            explore = new PanelRow(root, "ShipConfig_explore", StatHeight, font, tipPrefab);
            divider = PanelLook.Block("ShipConfig_divider", root, PanelLook.Line).rectTransform;
            Hover = new PanelHover(root);
        }

        /// <summary>
        /// A canvas of its own in the HUD's layer (no sorting of its own) with a raycaster, so <see cref="PanelHover"/>
        /// sees its lines.
        /// </summary>
        public static ShipPanel Create(Transform hudRoot)
        {
            RectTransform rect = PanelLook.Node("ShipConfig_ShipPanel", hudRoot);
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.gameObject.AddComponent<Canvas>();
            rect.gameObject.AddComponent<GraphicRaycaster>();
            return new ShipPanel(rect);
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

        /// <summary>Each line compares the numbers it shows, rounded as shown, and formats its text only when they moved.</summary>
        public void Refresh(Ship ship, float metresPerSecond)
        {
            int tenths = Mathf.RoundToInt(metresPerSecond * 36f);   // 0.1 km/h, as shown
            if (speed.Changed(tenths, 0))
                speed.Set("Speed", $"{tenths / 10f:0.0} km/h", "Speed", "How fast the ship moves over the water, in kilometres per hour.");
            RefreshMultiplier(ship);
            RefreshExplore();
            string[] ids = GrindstoneLink.Abilities();
            for (int i = 0; i < ids.Length; i++)
                RefreshAbility(Ability(i), ids[i]);
            for (int i = ids.Length; i < abilities.Count; i++)
                abilities[i].Hide();
            Layout(ids.Length);
        }

        private void RefreshMultiplier(Ship ship)
        {
            SpeedBonus.Parts(ship, out bool rowing, out float settings, out float skill);
            int settingsShown = Mathf.RoundToInt(settings * 100f), skillShown = Mathf.RoundToInt(skill * 100f);
            if (multiplier.Changed(settingsShown, skillShown * 2 + (rowing ? 1 : 0)))
                multiplier.Set("Speed multiplier", Multiplier(settings * skill), "Speed multiplier", SpeedBonus.Account(rowing, settings, skill));
        }

        /// <summary>"x1.32", orange above vanilla, red below.</summary>
        private static string Multiplier(float factor)
        {
            string text = $"x{factor:0.00}";
            if (factor > 1.005f)
                return $"<color={PanelLook.Orange}>{text}</color>";
            return factor < 0.995f ? $"<color={PanelLook.Red}>{text}</color>" : text;
        }

        private void RefreshExplore()
        {
            float factor = GrindstoneLink.ExploreFactor();
            float radius = Minimap.instance != null ? Minimap.instance.m_exploreRadius : VanillaExploreRadius;
            if (!explore.Changed(Mathf.RoundToInt(radius * factor), Mathf.RoundToInt(factor * 100f) * 2 + (GrindstoneLink.Present ? 1 : 0)))
                return;
            string account = $"How far around you the map uncovers as you sail. Vanilla {VanillaExploreRadius:0} m.";
            if (GrindstoneLink.Present)
                account += $"\nYour Sailing skill: x{factor:0.00}";
            explore.Set("Explore radius", $"{radius * factor:0} m", "Explore radius", account);
        }

        private static void RefreshAbility(PanelRow row, string id)
        {
            string name = GrindstoneLink.AbilityName(id);
            string key = GrindstoneLink.AbilityKey(id);
            float left = GrindstoneLink.AbilityCooldown(id);
            float length = GrindstoneLink.AbilityCooldownLength(id);
            row.SetBar(left > 0f && length > 0f ? 1f - left / length : 1f);
            if (!row.Changed(Mathf.Max(0, Mathf.CeilToInt(left)), 0, name, key))
                return;
            string label = key.Length > 0 ? $"[<color=yellow>{key}</color>] <color=#FFFFFF>{name}</color>" : name;
            row.Set(label, Cooldown(left), name, GrindstoneLink.AbilityDescription(id));
        }

        /// <summary>"Ready" in the game's orange, else the time left in grey: "42 s", "2:05".</summary>
        private static string Cooldown(float seconds)
        {
            int left = Mathf.CeilToInt(seconds);
            if (left <= 0)
                return $"<color={PanelLook.Orange}>Ready</color>";
            string time = left < 60 ? $"{left} s" : $"{left / 60}:{left % 60:00}";
            return $"<color=#B8B8B8>{time}</color>";
        }

        private PanelRow Ability(int index)
        {
            while (abilities.Count <= index)
            {
                PanelRow row = new PanelRow(root, "ShipConfig_ability" + abilities.Count, AbilityHeight, font, tipPrefab);
                row.AddBar();
                abilities.Add(row);
            }
            return abilities[index];
        }

        /// <summary>Lines placed top to bottom and the panel sized to hold them, when the number of abilities changed.</summary>
        private void Layout(int abilityCount)
        {
            if (abilityCount == laidOut)
                return;
            laidOut = abilityCount;
            float y = Padding;
            y = Stack(speed, y);
            y = Stack(multiplier, y);
            y = Stack(explore, y);
            divider.gameObject.SetActive(abilityCount > 0);
            if (abilityCount > 0)
                y = PlaceDivider(y);
            for (int i = 0; i < abilityCount; i++)
                y = Stack(abilities[i], y);
            root.sizeDelta = new Vector2(root.sizeDelta.x, y + Padding);
        }

        private static float Stack(PanelRow row, float y)
        {
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
            Minimap map = Minimap.instance;
            RectTransform small = map != null && map.m_smallRoot != null ? map.m_smallRoot.transform as RectTransform : null;
            if (small == null)
                return;
            small.GetWorldCorners(corners);
            float top = Mathf.Min(corners[0].y, WindIndicatorBottom()) - Gap * root.lossyScale.y;
            root.position = new Vector3((corners[0].x + corners[3].x) * 0.5f, top, root.position.z);
            root.sizeDelta = new Vector2((corners[3].x - corners[0].x) / root.lossyScale.x, root.sizeDelta.y);
        }

        /// <summary>The bottom of the round wind indicator (it turns with the ship, so from its centre), or no limit while it hides.</summary>
        private static float WindIndicatorBottom()
        {
            Hud hud = Hud.instance;
            RectTransform wind = hud != null && hud.m_shipHudRoot != null && hud.m_shipHudRoot.activeInHierarchy
                ? hud.m_shipWindIndicatorRoot
                : null;
            return wind != null ? wind.position.y - wind.rect.height * 0.5f * wind.lossyScale.y : float.MaxValue;
        }
    }
}
