using System;
using PatchGuard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Stow
{
    /// <summary>
    /// The trash can on its own wood plate between the armour and the weight readouts (<see cref="StatPlates"/>): a
    /// copy of the armour plate that keeps only its wood and its icon, already enlarged and centred, which shows the
    /// bin instead of the shield. Clicking it with a dragged stack trashes the stack. The copy sits right after the
    /// armour plate among the panel's children, so the panel's background covers its inner edge as it covers the
    /// game's plates. Returns false, with nothing created, when the two plates are too close for a third one between
    /// them or the copy has no icon, so the button row keeps the can.
    /// </summary>
    public static class TrashPlate
    {
        public static bool TryCreate(StatPlates plates, Action onClick)
        {
            float room = Mathf.Abs(plates.Armor.anchoredPosition.y - plates.Weight.anchoredPosition.y);
            if (room < 2f * plates.Armor.rect.height)
            {
                Plugin.Log.LogInfo($"trash can stays in the button row: the armour and weight plates are {room:0} px apart");
                return false;
            }
            GameObject go = UnityEngine.Object.Instantiate(plates.Armor.gameObject, plates.Armor.parent);
            Image bin = Strip(go);
            if (bin == null)
            {
                UnityEngine.Object.Destroy(go);
                return false;
            }
            go.name = "OpenKeep_trash";
            go.transform.SetSiblingIndex(plates.Armor.GetSiblingIndex() + 1);
            ((RectTransform)go.transform).anchoredPosition = plates.Middle;
            bin.sprite = StowSprites.Bin;
            MakeButton(go, bin, onClick);
            go.SetActive(true);
            Plugin.Log.LogInfo($"trash can on its own plate at {plates.Middle} from the player panel's top-right");
            return true;
        }

        /// <summary>Turns <paramref name="go"/> into the trash button: the bin lights up on hover and press.</summary>
        public static void MakeButton(GameObject go, Image bin, Action onClick)
        {
            Button button = go.GetComponent<Button>();
            if (button == null)
                button = go.AddComponent<Button>();
            button.targetGraphic = bin;
            ColorBlock colours = button.colors;
            colours.highlightedColor = new Color(1f, 0.55f, 0.45f, 1f);
            colours.selectedColor = colours.highlightedColor;
            colours.pressedColor = new Color(1f, 0.3f, 0.2f, 1f);
            button.colors = colours;
            button.onClick.AddListener(() => Guard.Run("trash can", onClick));
        }

        /// <summary>Keeps the copy's wood and icon and removes everything else: the text, any other child and every
        /// behaviour on the root, so the copy carries nothing that updates it. Returns the icon.</summary>
        private static Image Strip(GameObject go)
        {
            Transform plate = go.transform;
            Transform textChild = StatPlates.ChildHolding(plate, go.GetComponentInChildren<TMP_Text>(true));
            Image background = StatPlates.BackgroundOf(plate);
            Image icon = StatPlates.IconOf(plate, textChild);
            for (int i = plate.childCount - 1; i >= 0; i--)
            {
                Transform child = plate.GetChild(i);
                bool keep = (background != null && child == background.transform) || (icon != null && child == icon.transform);
                if (!keep)
                    UnityEngine.Object.Destroy(child.gameObject);
            }
            foreach (Behaviour behaviour in go.GetComponents<Behaviour>())
                UnityEngine.Object.Destroy(behaviour);
            return icon;
        }
    }
}
