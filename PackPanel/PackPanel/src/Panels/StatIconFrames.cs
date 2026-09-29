using System.Collections.Generic;
using PlateColumn;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackPanel.Panels
{
    /// <summary>Vanilla gray squares behind compact status icons, with small centered value overlays.</summary>
    public static class StatIconFrames
    {
        private const string Name = "PackPanel_stat_icon_frame";
        private static readonly Dictionary<Image, Color> colours = new Dictionary<Image, Color>();
        private static Transform owner;

        public static void Apply(InventoryGui gui, RectTransform boxes, bool on)
        {
            if (owner != boxes) { colours.Clear(); StatIconLayout.Restore(); owner = boxes; }
            if (!on) StatIconLayout.Restore();
            foreach (Transform box in boxes)
            {
                TMP_Text value = box.GetComponentInChildren<TMP_Text>(true);
                bool target = value == gui.m_armor || value == gui.m_weight || box.name.EndsWith("_world_tier");
                Dress(gui, (RectTransform)box, on && target);
            }
        }

        /// <summary>The same frame and centered value are used by the smaller minimap copies.</summary>
        public static void Dress(InventoryGui gui, RectTransform box, bool on)
        {
            Image background = Background(box);
            if (background == null) return;
            Transform frame = background.transform.Find(Name);
            if (on && frame == null) frame = Create(background.transform, gui);
            if (frame == null) return;
            // A new plate can be copied from the already styled armor plate.
            if (!colours.ContainsKey(background))
                colours[background] = background.color.a == 0f ? Color.white : background.color;
            background.color = on ? Color.clear : colours[background];
            frame.gameObject.SetActive(on);
            TMP_Text value = box.GetComponentInChildren<TMP_Text>(true);
            bool weight = value == gui.m_weight || box.name.EndsWith("_packpanel_weight");
            if (on) StatIconLayout.Apply(box, background, value, weight);
        }

        private static Image Background(Transform box)
        {
            foreach (Transform child in box)
            {
                Image image = child.GetComponent<Image>();
                if (image != null && image.sprite != null && image.sprite.name == Skin.Box.name) return image;
            }
            return null;
        }

        private static Transform Create(Transform parent, InventoryGui gui)
        {
            GameObject go = new GameObject(Name, typeof(RectTransform), typeof(Image));
            go.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            Image vanilla = gui.m_takeAllButton.GetComponent<Image>();
            image.sprite = vanilla.sprite;
            image.material = vanilla.material;
            image.color = vanilla.color;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return rect;
        }
    }
}
