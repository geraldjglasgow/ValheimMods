using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EliteCreaturesReborn.Recap.Window
{
    /// <summary>
    /// The pieces of the game's compendium window (its "Texts" dialog) the recap is built from, found in a fresh copy:
    /// the framed panel, its title, the list on the left with its entry template, the scrolling area on the right, its
    /// text, and the two close buttons (the panel's own and the dark backdrop's).
    /// </summary>
    internal sealed class WindowParts
    {
        public GameObject Root = null!;
        public RectTransform Frame = null!;
        public TMP_Text Topic = null!;
        public RectTransform List = null!;
        public ScrollRect ListScroll = null!;
        public RectTransform ListRoot = null!;
        public GameObject Element = null!;
        public RectTransform Area = null!;
        public ScrollRect AreaScroll = null!;
        public RectTransform AreaContent = null!;
        public TMP_Text Text = null!;
        public Button Close = null!;
        public Button Backdrop = null!;
        public Image Inset = null!;

        /// <summary>Read from the copy's own dialog component, before that component is removed.</summary>
        public static WindowParts? From(GameObject root, TextsDialog dialog)
        {
            Transform? frame = Utils.FindChild(dialog.transform, "Texts_frame");
            Transform? close = frame != null ? frame.Find("Closebutton") : null;
            Transform? backdrop = dialog.transform.Find("Closebutton");
            Transform? topic = frame != null ? frame.Find("topic") : null;
            if (frame == null || close == null || backdrop == null || topic == null || dialog.m_textArea == null)
            {
                return null;
            }
            WindowParts? parts = Lists(dialog);
            if (parts == null)
            {
                return null;
            }
            parts.Root = root;
            parts.Frame = (RectTransform)frame;
            parts.Topic = topic.GetComponent<TMP_Text>();
            parts.Close = close.GetComponent<Button>();
            parts.Backdrop = backdrop.GetComponent<Button>();
            return parts;
        }

        // The list is the scroll view holding the entries' root, inside its framed inset. Found from the entries, not from
        // the dialog's m_leftScrollRect: the game now points that at the text area's scroll view, so the list was never
        // placed and the text area was placed (and, in the altar window, hidden) in its stead.
        private static WindowParts? Lists(TextsDialog dialog)
        {
            ScrollRect? list = dialog.m_listRoot != null ? dialog.m_listRoot.GetComponentInParent<ScrollRect>(true) : null;
            if (list == null || dialog.m_rightScrollbar == null)
            {
                return null;
            }
            RectTransform inset = (RectTransform)list.transform.parent;
            return new WindowParts
            {
                List = inset,
                ListScroll = list,
                ListRoot = dialog.m_listRoot!,
                Element = dialog.m_elementPrefab,
                Area = (RectTransform)dialog.m_rightScrollbar.transform.parent,
                AreaScroll = dialog.m_rightScrollbar.transform.parent.GetComponentInChildren<ScrollRect>(true),
                AreaContent = (RectTransform)dialog.m_textArea.transform.parent,
                Text = dialog.m_textArea,
                Inset = inset.GetComponent<Image>(),
            };
        }
    }
}
