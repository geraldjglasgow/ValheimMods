using System;
using PatchGuard;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Stow
{
    /// <summary>
    /// The trash can on the player panel's right column, in the gap between the weight and the armour readouts, on
    /// a copy of their plate. The game exposes only the two texts (<c>m_weight</c>, <c>m_armor</c>); each plate is
    /// the nearest ancestor of its text below <c>m_player</c> that draws a sprite. The weight plate is cloned, every
    /// child (icon, text) removed so only the background stays, the bin drawn on it, and the copy set at the
    /// midpoint of the two plates, scaled down when the gap is narrower than a plate. Fails quietly (returns false)
    /// when the plates cannot be found, so the button row keeps the can as before.
    /// </summary>
    public static class TrashPlate
    {
        private const float Inset = 0.2f;
        private const float MinScale = 0.5f;

        public static bool TryCreate(InventoryGui gui, Action onClick)
        {
            RectTransform weight = PlateOf(gui, gui.m_weight);
            RectTransform armor = PlateOf(gui, gui.m_armor);
            if (weight == null || armor == null || weight == armor)
                return false;
            GameObject go = UnityEngine.Object.Instantiate(weight.gameObject, weight.parent);
            go.name = "OpenKeep_trash";
            go.SetActive(true);
            Strip(go);
            RectTransform rect = (RectTransform)go.transform;
            Fit(rect, weight, armor);
            Image bin = AddBin(go);
            AddButton(go, bin, onClick);
            Plugin.Log.LogInfo($"trash can on a copy of {Path(gui, weight)} ({weight.rect.width:0}x{weight.rect.height:0}, scale {rect.localScale.x:0.00})");
            return true;
        }

        /// <summary>The nearest ancestor of the text, below the player panel, whose Image draws a sprite.</summary>
        private static RectTransform PlateOf(InventoryGui gui, Component text)
        {
            if (text == null)
                return null;
            for (Transform t = text.transform.parent; t != null && t != gui.m_player; t = t.parent)
            {
                Image image = t.GetComponent<Image>();
                if (image != null && image.sprite != null)
                    return t as RectTransform;
            }
            return null;
        }

        private static void Strip(GameObject go)
        {
            for (int i = go.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(go.transform.GetChild(i).gameObject);
            foreach (Behaviour behaviour in go.GetComponents<Behaviour>())
            {
                if (!(behaviour is Image) && !(behaviour is LayoutElement))
                    UnityEngine.Object.Destroy(behaviour);
            }
            LayoutElement layout = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
        }

        /// <summary>Centres the copy between the two plates (world space, so differing anchors do not matter) and
        /// shrinks it uniformly when the gap between them is narrower than a plate.</summary>
        private static void Fit(RectTransform rect, RectTransform weight, RectTransform armor)
        {
            Vector3 weightCentre = Centre(weight);
            Vector3 armorCentre = Centre(armor);
            Vector3 middle = (weightCentre + armorCentre) / 2f;
            rect.position = middle + (weight.position - weightCentre);
            float height = weight.rect.height * weight.lossyScale.y;
            float gap = Vector3.Distance(weightCentre, armorCentre) - height;
            float scale = height > 0f && gap < height ? Mathf.Max(MinScale, gap / height) : 1f;
            rect.localScale = weight.localScale * scale;
        }

        private static Vector3 Centre(RectTransform rect) => rect.TransformPoint(rect.rect.center);

        private static Image AddBin(GameObject plate)
        {
            GameObject go = new GameObject("bin", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(plate.transform, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(Inset, Inset);
            rect.anchorMax = new Vector2(1f - Inset, 1f - Inset);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.sprite = StowSprites.Bin;
            image.color = new Color(0.85f, 0.75f, 0.6f, 0.95f);
            image.preserveAspect = true;
            return image;
        }

        private static void AddButton(GameObject go, Image bin, Action onClick)
        {
            go.GetComponent<Image>().raycastTarget = true;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = bin;
            ColorBlock colours = button.colors;
            colours.highlightedColor = new Color(1f, 0.55f, 0.45f, 1f);
            colours.selectedColor = colours.highlightedColor;
            colours.pressedColor = new Color(1f, 0.3f, 0.2f, 1f);
            button.colors = colours;
            button.onClick.AddListener(() => Guard.Run("trash can", onClick));
        }

        private static string Path(InventoryGui gui, Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null && p != gui.m_player; p = p.parent)
                path = p.name + "/" + path;
            return path;
        }
    }
}
