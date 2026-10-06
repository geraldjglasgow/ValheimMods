using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EliteCrafting.Display.Tooltips
{
    /// <summary>
    /// A tooltip too tall for the screen (a Rare item's inscriptions under a long vanilla block) scrolls instead of
    /// running off it (display.md section 3, "Long tooltips"). Added to every game tooltip as it is made; it does nothing
    /// until the text would take the tooltip past the screen. Then the text moves into a clipped viewport
    /// (<see cref="Name"/>) sized so the whole tooltip fits the screen less <see cref="ScreenMargin"/> above and below,
    /// a slim bar on its right (<see cref="BarName"/>) shows where the view is, and the mouse wheel (<see cref="WheelStep"/> a notch) or the right
    /// stick scrolls it while it shows. A new text starts at the top. Only the tooltip prefab the game uses for items
    /// (a <c>VerticalLayoutGroup</c> holding <c>Topic</c> and <c>Text</c>) is handled. Display only, on the viewing client.
    /// </summary>
    internal sealed class TooltipScroll : MonoBehaviour
    {
        public const string Name = "ecf_tooltip_scroll";

        /// <summary>The scroll bar's name, shown while the text scrolls: OpenKeep's wheel cycling of chests leaves the wheel alone then.</summary>
        public const string BarName = "ecf_tooltip_bar";

        private const float ScreenMargin = 24f;
        private const float MinimumView = 80f;
        private const float WheelStep = 60f;
        private const float StickSpeed = 700f;
        private const float BarWidth = 3f;
        private const float BarInset = 2f;

        // Until wrapped, the fit is measured only for a new text or screen height, on that frame and the next (the
        // layout may settle a frame late); a text that fits costs a reference and an int compare after that.
        private const int FitChecks = 2;
        private static readonly Color TrackColour = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color ThumbColour = new Color(1f, 1f, 1f, 0.55f);

        private RectTransform? _bkg;
        private RectTransform? _topic;
        private RectTransform? _text;
        private TMP_Text? _label;
        private VerticalLayoutGroup? _layout;
        private RectTransform? _viewport;
        private LayoutElement? _viewSize;
        private GameObject? _bar;
        private RectTransform? _thumb;
        private string? _shownText;
        private float _offset;
        private string? _fitText;
        private int _fitScreen;
        private int _fitChecks;

        [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.OnHoverStart))]
        private static class AttachPatch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                GameObject tooltip = UITooltip.m_tooltip;
                if (tooltip != null && tooltip.GetComponent<TooltipScroll>() == null)
                {
                    tooltip.AddComponent<TooltipScroll>();
                }
            }
        }

        private void Awake()
        {
            _bkg = transform.childCount > 0 ? transform.GetChild(0) as RectTransform : null;
            _layout = _bkg == null ? null : _bkg.GetComponent<VerticalLayoutGroup>();
            _topic = _bkg == null ? null : _bkg.Find("Topic") as RectTransform;
            _text = _bkg == null ? null : _bkg.Find("Text") as RectTransform;
            _label = _text == null ? null : _text.GetComponent<TMP_Text>();
            if (_layout == null || _label == null)
            {
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (_viewport == null && !FitMayHaveChanged())
            {
                return;
            }
            float needed = LayoutUtility.GetPreferredHeight(_text);
            float room = Room();
            if (_viewport == null)
            {
                if (needed <= room)
                {
                    return;
                }
                Wrap();
            }
            if (!ReferenceEquals(_label!.text, _shownText))
            {
                _shownText = _label.text;
                _offset = 0f;
            }
            Scroll(needed, room);
        }

        private bool FitMayHaveChanged()
        {
            string text = _label!.text;
            int screen = Screen.height;
            if (!ReferenceEquals(text, _fitText) || screen != _fitScreen)
            {
                _fitText = text;
                _fitScreen = screen;
                _fitChecks = FitChecks;
            }
            if (_fitChecks == 0)
            {
                return false;
            }
            _fitChecks--;
            return true;
        }

        /// <summary>The text's room: the screen in the tooltip's units, less the margins, the padding and the topic.</summary>
        private float Room()
        {
            float scale = _bkg!.lossyScale.y;
            if (scale <= 0f)
            {
                return float.MaxValue;
            }
            float topic = _topic != null && _topic.gameObject.activeSelf ? LayoutUtility.GetPreferredHeight(_topic) + _layout!.spacing : 0f;
            float room = Screen.height / scale - 2f * ScreenMargin - _layout!.padding.vertical - topic;
            return Mathf.Max(MinimumView, room);
        }

        /// <summary>The text moved into a clipped viewport the full width of the tooltip, in its place in the layout.</summary>
        private void Wrap()
        {
            GameObject view = new GameObject(Name, typeof(RectTransform), typeof(RectMask2D), typeof(LayoutElement));
            _viewport = (RectTransform)view.transform;
            _viewport.SetParent(_bkg, false);
            _viewport.SetSiblingIndex(_text!.GetSiblingIndex());
            _viewport.sizeDelta = new Vector2(_bkg!.rect.width, 0f);
            _viewSize = view.GetComponent<LayoutElement>();
            _text.SetParent(_viewport, false);
            _text.anchorMin = new Vector2(0.5f, 1f);
            _text.anchorMax = new Vector2(0.5f, 1f);
            _text.pivot = new Vector2(0.5f, 1f);
            BuildBar();
        }

        private void BuildBar()
        {
            _bar = Part(BarName, _viewport!, TrackColour);
            RectTransform track = (RectTransform)_bar.transform;
            track.anchorMin = new Vector2(1f, 0f);
            track.anchorMax = Vector2.one;
            track.pivot = new Vector2(1f, 0.5f);
            track.sizeDelta = new Vector2(BarWidth, 0f);
            track.anchoredPosition = new Vector2(-BarInset, 0f);
            _thumb = (RectTransform)Part("ecf_tooltip_thumb", track, ThumbColour).transform;
        }

        private static GameObject Part(string name, Transform parent, Color colour)
        {
            GameObject part = new GameObject(name, typeof(RectTransform), typeof(Image));
            part.transform.SetParent(parent, false);
            Image image = part.GetComponent<Image>();
            image.color = colour;
            image.raycastTarget = false;
            return part;
        }

        private void Scroll(float needed, float room)
        {
            float visible = Mathf.Min(needed, room);
            float range = Mathf.Max(0f, needed - visible);
            _offset = Mathf.Clamp(_offset + ScrollInput(), 0f, range);
            if (!Mathf.Approximately(_viewSize!.preferredHeight, visible))
            {
                _viewSize.preferredHeight = visible;
                _viewSize.minHeight = visible;
            }
            Vector2 at = new Vector2(0f, Mathf.Round(_offset));
            if (_text!.anchoredPosition != at)
            {
                _text.anchoredPosition = at;
            }
            ShowBar(range > 0.5f, visible, needed);
        }

        /// <summary>A wheel notch moves the view a few lines; the right stick moves it smoothly (up scrolls up).</summary>
        private static float ScrollInput()
        {
            float wheel = ZInput.GetMouseScrollWheel();
            float step = wheel > 0f ? -WheelStep : wheel < 0f ? WheelStep : 0f;
            return step + ZInput.GetJoyRightStickY() * StickSpeed * Time.unscaledDeltaTime;
        }

        private void ShowBar(bool show, float visible, float needed)
        {
            if (_bar!.activeSelf != show)
            {
                _bar.SetActive(show);
            }
            if (!show)
            {
                return;
            }
            float top = 1f - _offset / needed;
            Vector2 min = new Vector2(0f, top - visible / needed);
            Vector2 max = new Vector2(1f, top);
            if (_thumb!.anchorMin != min || _thumb.anchorMax != max)
            {
                _thumb.anchorMin = min;
                _thumb.anchorMax = max;
                _thumb.offsetMin = Vector2.zero;
                _thumb.offsetMax = Vector2.zero;
            }
        }
    }
}
