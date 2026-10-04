using OpenKeep.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.BuildCamera
{
    /// <summary>
    /// The panel that says why camera pickup holds back, shown while items lie by the camera and the conditions are not
    /// met, gone a second after (or at once when the camera goes back). Built once under the game's HUD root (so the
    /// game's hide-HUD key hides it too) in the look of the build menu: the background of the piece selection window and
    /// the font, material and colour of the selected piece's name. Pickup Panel off: nothing is shown.
    /// </summary>
    public static class PickupPanel
    {
        private const float ShowSeconds = 1f;
        private const float Padding = 16f;
        private const float FontShare = 0.8f;

        private static RectTransform panel;
        private static TMP_Text label;
        private static float until;

        public static void Show(string missing)
        {
            if (!CameraPrefs.PickupPanel.Value || Hud.instance == null || Hud.instance.m_buildSelection == null)
                return;
            if (panel == null)
                Build(Hud.instance);
            label.text = Language.Localize(CameraModule.PickupNeeds) + ": " + missing;
            Vector2 size = label.GetPreferredValues(label.text);
            panel.sizeDelta = size + Vector2.one * (2f * Padding);
            panel.anchoredPosition = CameraPrefs.PickupPanelPosition.Value;
            panel.gameObject.SetActive(true);
            until = Time.time + ShowSeconds;
        }

        public static void Tick()
        {
            if (panel != null && panel.gameObject.activeSelf && (Time.time > until || !CameraState.Active || !CameraPrefs.PickupPanel.Value))
                panel.gameObject.SetActive(false);
        }

        private static void Build(Hud hud)
        {
            GameObject root = new GameObject("OpenKeep_CameraPickup", typeof(RectTransform));
            root.SetActive(false);
            panel = (RectTransform)root.transform;
            panel.SetParent(hud.m_rootObject.transform, worldPositionStays: false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 1f);
            PickupPanelLook.Background(root.AddComponent<Image>(), hud);
            label = Label(panel, hud.m_buildSelection);
        }

        private static TMP_Text Label(RectTransform parent, TMP_Text like)
        {
            GameObject holder = new GameObject("OpenKeep_CameraPickupText", typeof(RectTransform));
            holder.SetActive(false);   // the text wakes with its font set, so it never looks for TextMeshPro's missing default
            RectTransform rect = (RectTransform)holder.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = holder.AddComponent<TextMeshProUGUI>();
            PickupPanelLook.Text(text, like, FontShare);
            holder.SetActive(true);
            return text;
        }
    }
}
