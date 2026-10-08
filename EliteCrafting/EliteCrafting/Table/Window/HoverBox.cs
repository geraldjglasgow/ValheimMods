using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EliteCrafting.Tables.Window
{
    /// <summary>
    /// The Rune Table's hover text (user request 2026-10-07: "anytime we have hover text it should be in a box and appear
    /// off to the right or left of where you're hovering"): the game's own boxed tooltip (the inventory items'
    /// <c>InventoryTooltip</c>: a framed box with a topic and a text; the requirement slots' own prefab is a bare caption
    /// with no box, which drew the text straight over the window), placed beside the hovered element, top edges level: to its right, or to its left when the right
    /// has no room, kept on screen. The game's <c>UITooltip</c> follows the pointer instead; this never uses it. One box
    /// at a time, destroyed when the pointer leaves, the element is disabled or the window closes.
    /// </summary>
    internal static class HoverBox
    {
        private const float Gap = 8f;
        private static readonly Vector3[] Corners = new Vector3[4];

        private static GameObject? _prefab;
        private static GameObject? _box;
        private static TipHover? _owner;

        /// <summary>The game's tooltip prefab, taken from the first slot whose own tooltip is replaced.</summary>
        public static void Remember(GameObject? prefab)
        {
            if (_prefab == null && prefab != null)
            {
                _prefab = prefab;
            }
        }

        public static void Show(TipHover owner)
        {
            Hide();
            Canvas? canvas = owner.GetComponentInParent<Canvas>();
            GameObject? prefab = BoxPrefab();
            if (prefab == null || canvas == null)
            {
                return;
            }
            _box = Object.Instantiate(prefab, canvas.rootCanvas.transform);
            _box.transform.SetAsLastSibling();
            _owner = owner;
            Write(owner);
            Place((RectTransform)owner.transform, canvas.rootCanvas.scaleFactor);
        }

        // The inventory items' boxed tooltip; the remembered slot prefab only when the inventory grid has none.
        private static GameObject? BoxPrefab()
        {
            InventoryGrid? grid = InventoryGui.instance != null ? InventoryGui.instance.m_playerGrid : null;
            UITooltip? tip = grid != null && grid.m_elementPrefab != null ? grid.m_elementPrefab.GetComponent<UITooltip>() : null;
            return tip != null && tip.m_tooltipPrefab != null ? tip.m_tooltipPrefab : _prefab;
        }

        /// <summary>The owner's texts changed while its box shows: written again (an emptied text hides it).</summary>
        public static void Refresh(TipHover owner)
        {
            if (_owner != owner || _box == null)
            {
                return;
            }
            if (owner.Empty)
            {
                Hide();
                return;
            }
            Write(owner);
            Place((RectTransform)owner.transform, _box.GetComponentInParent<Canvas>().scaleFactor);
        }

        /// <summary>Hides the box; with an owner, only that owner's.</summary>
        public static void Hide(TipHover? owner = null)
        {
            if (owner != null && owner != _owner)
            {
                return;
            }
            if (_box != null)
            {
                Object.Destroy(_box);
            }
            _box = null;
            _owner = null;
        }

        private static void Write(TipHover owner)
        {
            Text(_box!.transform, "Topic", owner.Topic);
            Text(_box.transform, "Text", owner.Text);
        }

        private static void Text(Transform root, string name, string text)
        {
            Transform? part = Utils.FindChild(root, name);
            TMP_Text? label = part != null ? part.GetComponent<TMP_Text>() : null;
            if (label != null)
            {
                label.text = text;
                label.gameObject.SetActive(text.Length > 0);
            }
        }

        // The box's frame (the prefab's first child) moved so its top left sits right of the target, or its top right
        // left of it when the right side of the screen is too narrow; then kept on screen as the game keeps its own.
        private static void Place(RectTransform target, float scale)
        {
            var frame = (RectTransform)_box!.transform.GetChild(0);
            LayoutRebuilder.ForceRebuildLayoutImmediate(frame);
            target.GetWorldCorners(Corners);
            Vector3 targetLeft = Corners[1];
            Vector3 targetRight = Corners[2];
            frame.GetWorldCorners(Corners);
            float width = Corners[2].x - Corners[1].x;
            float x = targetRight.x + Gap * scale;
            if (x + width > Screen.width)
            {
                x = targetLeft.x - Gap * scale - width;
            }
            _box.transform.position += new Vector3(x - Corners[1].x, targetLeft.y - Corners[1].y, 0f);
            Utils.ClampUIToScreen(frame);
        }
    }

    /// <summary>A hover text on a Rune Table element, shown in the <see cref="HoverBox"/>.</summary>
    internal sealed class TipHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string Topic { get; private set; } = "";
        public string Text { get; private set; } = "";
        public bool Empty => Topic.Length == 0 && Text.Length == 0;

        public void Set(string topic, string text)
        {
            if (topic == Topic && text == Text)
            {
                return;
            }
            Topic = topic;
            Text = text;
            HoverBox.Refresh(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!Empty)
            {
                HoverBox.Show(this);
            }
        }

        public void OnPointerExit(PointerEventData eventData) => HoverBox.Hide(this);

        private void OnDisable() => HoverBox.Hide(this);
    }
}
