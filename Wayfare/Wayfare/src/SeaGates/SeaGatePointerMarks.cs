using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wayfare.Targeting;

namespace Wayfare.SeaGates
{
    /// <summary>The crew's pointers on the large map (<see cref="SeaGatePointers"/>): an arrow with the player's name
    /// beside it, on Wayfare's map layer above every icon. Each player keeps one colour, picked from their player id;
    /// the helmsman's is gold, like the gate the ship is in. Placed the way the sea gate icons are
    /// (<see cref="SeaGateMapIcons.Place"/>), hidden while the point is off the visible map.</summary>
    internal static class SeaGatePointerMarks
    {
        private const float ArrowSize = 22f;
        private static readonly Color HelmGold = new Color(1f, 0.82f, 0.3f, 1f);

        private sealed class Mark
        {
            public RectTransform Root;
            public Image Arrow;
            public Text Label;
        }

        private static readonly Dictionary<long, Mark> marks = new Dictionary<long, Mark>();
        private static readonly List<long> gone = new List<long>();

        internal static void Draw(Dictionary<long, CrewPointer> pointers, long helmsmanId)
        {
            RectTransform layer = MapIconLayer.Root;
            if (layer == null || Minimap.instance == null)
            {
                Clear();
                return;
            }
            foreach (KeyValuePair<long, CrewPointer> pair in pointers)
                Show(layer, pair.Key, pair.Value, helmsmanId);
            RemoveGone(pointers);
        }

        private static void Show(RectTransform layer, long peer, CrewPointer pointer, long helmsmanId)
        {
            if (!marks.TryGetValue(peer, out Mark mark) || mark.Root == null)
                marks[peer] = mark = Build(layer);
            bool visible = SeaGateMapIcons.Place(mark.Root, pointer.World);
            if (mark.Root.gameObject.activeSelf != visible)
                mark.Root.gameObject.SetActive(visible);
            if (!visible)
                return;
            Color color = pointer.PlayerId == helmsmanId ? HelmGold : ColorOf(pointer.PlayerId);
            mark.Arrow.color = color;
            mark.Label.color = color;
            mark.Label.text = pointer.Name ?? "";
            mark.Root.SetAsLastSibling();
        }

        /// <summary>A bright colour of the player's own, the same on every machine and every stop.</summary>
        private static Color ColorOf(long playerId)
        {
            float hue = (uint)(playerId ^ (playerId >> 32)) % 360 / 360f;
            return Color.HSVToRGB(hue, 0.55f, 1f);
        }

        private static void RemoveGone(Dictionary<long, CrewPointer> pointers)
        {
            gone.Clear();
            foreach (long peer in marks.Keys)
            {
                if (!pointers.ContainsKey(peer))
                    gone.Add(peer);
            }
            foreach (long peer in gone)
                Remove(peer);
        }

        private static void Remove(long peer)
        {
            if (marks[peer].Root != null)
                Object.Destroy(marks[peer].Root.gameObject);
            marks.Remove(peer);
        }

        internal static void Clear()
        {
            if (marks.Count == 0)
                return;
            gone.Clear();
            gone.AddRange(marks.Keys);
            foreach (long peer in gone)
                Remove(peer);
        }

        /// <summary>A root whose pivot is the arrow's tip, so placing the root puts the tip on the point.</summary>
        private static Mark Build(RectTransform layer)
        {
            GameObject go = new GameObject("Wayfare.CrewPointer", typeof(RectTransform), typeof(Image));
            RectTransform root = (RectTransform)go.transform;
            root.SetParent(layer, worldPositionStays: false);
            Sprite arrow = SeaGateMapSprites.Pointer;
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(arrow.pivot.x / arrow.rect.width, arrow.pivot.y / arrow.rect.height);
            root.sizeDelta = new Vector2(ArrowSize, ArrowSize);
            Image image = go.GetComponent<Image>();
            image.sprite = arrow;
            image.raycastTarget = false;
            return new Mark { Root = root, Arrow = image, Label = BuildLabel(root) };
        }

        private static Text BuildLabel(RectTransform parent)
        {
            RectTransform rect = (RectTransform)new GameObject("Name", typeof(RectTransform)).transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(ArrowSize * 0.75f, -ArrowSize * 0.55f);
            rect.sizeDelta = new Vector2(160f, 18f);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            rect.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
            return text;
        }
    }
}
