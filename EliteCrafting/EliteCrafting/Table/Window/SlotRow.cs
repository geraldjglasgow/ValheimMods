using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCrafting.Tables.Window
{
    /// <summary>
    /// A row of small clickable requirement slots under the description (the runes, the essences), each a copy of the
    /// game's slot scaled down whole, so its frame, icon and text keep the game's proportions.
    /// </summary>
    internal sealed class SlotRow
    {
        private readonly List<Slot> _slots = new List<Slot>();
        private readonly GameObject _row;

        public SlotRow(RectTransform parent, GameObject template, int count, Rect area, Action<int> clicked)
        {
            _row = new GameObject("ECF_SlotRow", typeof(RectTransform));
            _row.transform.SetParent(parent, false);
            Rects.Place((RectTransform)_row.transform, area.x, area.y, area.width, area.height);
            var size = ((RectTransform)template.transform).rect.size;
            float scale = area.height / Mathf.Max(1f, size.y);
            float step = Mathf.Min(area.height + 4f, area.width / Mathf.Max(1, count));
            for (int i = 0; i < count; i++)
            {
                _slots.Add(Make(template, i, i * step, size, scale, clicked));
            }
        }

        public int Count => _slots.Count;

        public Slot this[int index] => _slots[index];

        public void Show(bool on)
        {
            if (_row.activeSelf != on)
            {
                _row.SetActive(on);
            }
        }

        private Slot Make(GameObject template, int index, float x, Vector2 size, float scale, Action<int> clicked)
        {
            GameObject copy = Object.Instantiate(template, _row.transform, false);
            copy.name = "ECF_Slot" + index;
            var rect = (RectTransform)copy.transform;
            Rects.Place(rect, x, 0f, size.x, size.y);
            rect.localScale = new Vector3(scale, scale, 1f);
            var slot = new Slot(copy);
            slot.Clickable().onClick.AddListener(() => clicked(index));
            slot.Name?.gameObject.SetActive(false);
            // No hover text on the rows (user, 2026-10-07): the copy's tooltip still holds the crafting panel's last material.
            slot.Tooltip("", "");
            return slot;
        }
    }
}
