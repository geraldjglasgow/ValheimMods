using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Wayfare.Targeting
{
    /// <summary>The TargetTeleport picker's window, built once per HUD from a copy of the game's text input window (the box
    /// signs and portal tags are typed in): its wood panel, title, Cancel and OK buttons with their gamepad keys, on the
    /// same canvas. The text field gives its place to the destination dropdown (<see cref="PickerDropdown"/>) and OK
    /// becomes Teleport. The copy is made under an inactive holder, so nothing in it wakes before the field is gone, and
    /// its buttons' inspector clicks, which still point at the game's own text input, are replaced. Its Update drives the
    /// picker while it is shown.</summary>
    internal sealed class PickerWindow : MonoBehaviour
    {
        private TMP_Text topic;
        private TMP_Text goText;

        public TMP_Dropdown Dropdown { get; private set; }
        public Button Go { get; private set; }

        public bool Visible => gameObject.activeSelf;

        /// <summary>The window, hidden; null when the game's text input window or settings dropdown is not there.</summary>
        public static PickerWindow Build()
        {
            TextInput input = TextInput.instance;
            TMP_Dropdown source = PickerDropdown.Source();
            if (input == null || input.m_panel == null || source == null)
                return null;
            GameObject holder = new GameObject("Wayfare_PickerHolder");
            holder.SetActive(false);
            GameObject copy = Instantiate(input.m_panel, holder.transform, false);
            copy.name = "Wayfare_PortalPicker";
            PickerWindow window = Assemble(copy, source);
            if (window != null)
            {
                copy.SetActive(false);
                copy.transform.SetParent(input.transform, false);
            }
            Destroy(holder);
            return window;
        }

        private static PickerWindow Assemble(GameObject copy, TMP_Dropdown source)
        {
            Transform field = copy.transform.Find("TextField");
            Transform title = copy.transform.Find("Topic");
            Button ok = Part<Button>(copy, "OK");
            Button cancel = Part<Button>(copy, "Cancel");
            if (field == null || title == null || ok == null || cancel == null)
                return null;
            PickerWindow window = copy.AddComponent<PickerWindow>();
            window.Dropdown = PickerDropdown.Make(source, (RectTransform)field);
            DestroyImmediate(field.gameObject);
            window.topic = title.GetComponent<TMP_Text>();
            window.Go = ok;
            window.goText = Part<TMP_Text>(ok.gameObject, "Text");
            Rewire(ok, PortalPicker.Go);
            Rewire(cancel, PortalPicker.Close);
            return window;
        }

        private static T Part<T>(GameObject parent, string name) where T : Component
        {
            Transform child = parent.transform.Find(name);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static void Rewire(Button button, UnityAction onClick)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(onClick);
        }

        /// <summary>Shown with its words localized now, so a language changed since the last time shows.</summary>
        public void Show(string topicToken, string goToken)
        {
            if (topic != null)
                topic.text = Localization.instance.Localize(topicToken);
            if (goText != null)
                goText.text = Localization.instance.Localize(goToken);
            gameObject.SetActive(true);
        }

        /// <summary>Hidden, an open list folded first so its click blocker goes with it.</summary>
        public void Hide()
        {
            if (Dropdown != null && Dropdown.IsExpanded)
                Dropdown.Hide();
            gameObject.SetActive(false);
        }

        private void Update() => PortalPicker.Tick();
    }
}
