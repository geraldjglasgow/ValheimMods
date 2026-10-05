using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// The breadcrumb: a strip across the top of the build menu's piece list while the Blueprints tab is shown,
    /// "Blueprints > houses > nordic", every part but the folder shown clickable to open it, a drop target and (but the
    /// top) renamed by a right click. The list is moved down by the strip's height meanwhile and put back on every other
    /// tab, so it never covers the game's search field or tabs. A chain too long for the strip loses its first parts
    /// ("... > houses > nordic", the dots standing for the folder above the first part shown); it is laid out again when
    /// the folder or the strip's width (the resolution) changes. The strip wears the list's own background.
    /// </summary>
    public static class Breadcrumb
    {
        public const float Height = 28f;
        private const float Gap = 2f;

        private static RectTransform strip;
        private static RectTransform list;
        private static Vector2 listTop;
        private static string built;

        /// <summary>BuildUi.Awake: the strip above the piece list, hidden until the tab is shown.</summary>
        public static void Install(BuildUi ui)
        {
            built = null;
            list = ui.m_pieceScrollRect != null ? (RectTransform)ui.m_pieceScrollRect.transform : null;
            if (list == null || list.parent == null)
                return;
            listTop = list.offsetMax;
            strip = TabLook.Child(list.parent, "OpenKeep Breadcrumb");
            strip.anchorMin = new Vector2(0f, 1f);
            strip.pivot = new Vector2(0.5f, 1f);
            strip.sizeDelta = new Vector2(0f, Height);
            Image back = strip.gameObject.AddComponent<Image>();
            Image source = list.GetComponent<Image>();
            back.sprite = source != null ? source.sprite : null;
            back.type = source != null ? source.type : Image.Type.Simple;
            back.color = source != null ? source.color : TabLook.Band;
            BreadcrumbParts.Install(strip);
            strip.gameObject.SetActive(false);
        }

        /// <summary>Per frame and whenever the game redraws the tag column: shown exactly while the tab is shown.</summary>
        public static void Sync()
        {
            if (strip == null)
                return;
            bool showing = BlueprintTab.Showing;
            if (strip.gameObject.activeSelf != showing)
            {
                strip.gameObject.SetActive(showing);
                list.offsetMax = showing ? listTop - new Vector2(0f, Height + Gap) : listTop;
                built = null;
            }
            float width = strip.rect.width;
            string key = BlueprintLibrary.CurrentFolder + "|" + Mathf.RoundToInt(width);
            if (!showing || width <= 0f || key == built)
                return;
            built = key;
            BreadcrumbParts.Build(Chain(BlueprintLibrary.CurrentFolder), width);
        }

        /// <summary>The folders from the top to <paramref name="current"/>: "", "houses", "houses/nordic".</summary>
        private static List<string> Chain(string current)
        {
            List<string> chain = new List<string> { "" };
            if (string.IsNullOrEmpty(current))
                return chain;
            string[] parts = current.Split('/');
            for (int i = 0; i < parts.Length; i++)
                chain.Add(string.Join("/", parts, 0, i + 1));
            return chain;
        }
    }
}
