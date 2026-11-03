using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Broadside
{
    /// <summary>
    /// The crane's keys in a box at the bottom of the screen while the local player works a crane (the user, 2026-10-25:
    /// "a box at the bottom of the screen that just shows the crane operation instructions"). The game's own boxed
    /// tooltip (the inventory items' <c>InventoryTooltip</c>: a frame, a topic and a text), so it keeps the game's look;
    /// under the HUD's root, so it hides with the HUD; centred across, its bottom <see cref="Bottom"/> of the screen's
    /// height up, placed again when the screen changes size. Local only.
    /// </summary>
    public static class KwCraneKeys
    {
        private const float Bottom = 0.1f;
        private static readonly Vector3[] Corners = new Vector3[4];
        private static GameObject? box;
        private static Vector2Int placedFor;

        public static void Show()
        {
            Hide();
            Transform? hud = Hud.instance != null && Hud.instance.m_rootObject != null ? Hud.instance.m_rootObject.transform : null;
            GameObject? prefab = BoxPrefab();
            if (hud == null || prefab == null)
                return;
            box = Object.Instantiate(prefab, hud);
            box.name = "KWC_CraneKeys";
            box.transform.SetAsLastSibling();
            Text(box.transform, "Topic", KwCrane.Label);
            Text(box.transform, "Text", Localization.instance.Localize(KwCraneText.KeyLines));
            Place();
        }

        public static void Hide()
        {
            if (box != null)
                Object.Destroy(box);
            box = null;
        }

        /// <summary>Each frame while working: placed again when the screen changed size.</summary>
        public static void Keep()
        {
            if (box != null && placedFor != new Vector2Int(Screen.width, Screen.height))
                Place();
        }

        // The inventory items' boxed tooltip prefab, or null before the inventory exists.
        private static GameObject? BoxPrefab()
        {
            InventoryGrid? grid = InventoryGui.instance != null ? InventoryGui.instance.m_playerGrid : null;
            UITooltip? tip = grid != null && grid.m_elementPrefab != null ? grid.m_elementPrefab.GetComponent<UITooltip>() : null;
            return tip != null ? tip.m_tooltipPrefab : null;
        }

        private static void Text(Transform root, string name, string text)
        {
            Transform? part = Utils.FindChild(root, name);
            TMP_Text? label = part != null ? part.GetComponent<TMP_Text>() : null;
            if (label != null)
                label.text = text;
        }

        // The box's frame (the prefab's first child) moved so it is centred across, its bottom edge Bottom up the screen.
        private static void Place()
        {
            var frame = (RectTransform)box!.transform.GetChild(0);
            LayoutRebuilder.ForceRebuildLayoutImmediate(frame);
            frame.GetWorldCorners(Corners);
            float width = Corners[2].x - Corners[0].x;
            Vector3 goal = new Vector3((Screen.width - width) / 2f, Screen.height * Bottom, Corners[0].z);
            frame.position += goal - Corners[0];
            placedFor = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
