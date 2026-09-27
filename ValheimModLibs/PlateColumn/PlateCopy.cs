using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlateColumn
{
    /// <summary>
    /// A mod's plate is a copy of the game's armour plate - the same wood, the same enlarged icon, the same font - that
    /// keeps its wood, its icon (showing the mod's sprite) and, when asked, its text, and loses everything else: other
    /// children and every behaviour on its root, so nothing the game or another mod put there comes along. It sits right
    /// after the armour plate among the panel's children, so the panel's wood covers its inner edge as it covers the
    /// game's plates. The stripping is immediate, so a tooltip added in the same frame is the only one on it.
    /// </summary>
    internal static class PlateCopy
    {
        public static Plate? Make(RectTransform source, PlateSpec spec, string name)
        {
            GameObject go = Object.Instantiate(source.gameObject, source.parent, false);
            go.name = name;
            Transform plate = go.transform;
            TMP_Text? text = go.GetComponentInChildren<TMP_Text>(true);
            Transform? textChild = PlateStyle.ChildHolding(plate, text != null ? text.transform : null);
            Image? wood = PlateStyle.BackgroundOf(plate);
            Image? icon = PlateStyle.IconOf(plate, textChild);
            if (icon == null)
            {
                Object.Destroy(go);
                return null;
            }
            Strip(go, wood, icon, spec.WithText ? textChild : null);
            if (spec.Icon != null)
            {
                icon.sprite = spec.Icon;
            }
            plate.SetSiblingIndex(source.GetSiblingIndex() + 1);
            go.SetActive(true);
            return new Plate((RectTransform)plate, icon, spec.WithText ? text : null);
        }

        /// <summary>A plate this panel already has, found again by its parts.</summary>
        public static Plate? Wrap(RectTransform plate)
        {
            TMP_Text? text = plate.GetComponentInChildren<TMP_Text>(true);
            Image? icon = PlateStyle.IconOf(plate, PlateStyle.ChildHolding(plate, text != null ? text.transform : null));
            return icon != null ? new Plate(plate, icon, text) : null;
        }

        private static void Strip(GameObject go, Image? wood, Image icon, Transform? keptText)
        {
            Transform plate = go.transform;
            for (int i = plate.childCount - 1; i >= 0; i--)
            {
                Transform child = plate.GetChild(i);
                bool keep = (wood != null && child == wood.transform) || child == icon.transform || child == keptText;
                if (!keep)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
            foreach (Behaviour behaviour in go.GetComponents<Behaviour>())
            {
                Object.DestroyImmediate(behaviour);
            }
        }
    }
}
