using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GrindstoneSkills
{
    /// <summary>
    /// The game's own star, as it draws it over a two-star creature: a black star (sprite craft_icon_32, 15 px) with a
    /// gold one (craft_icon, 12 px) on top, the child "star" of "level_2" in EnemyHud.m_baseHud. That template sits in
    /// the game scene, hidden by EnemyHud.Awake and never changed; its star is plain UI (Image and CanvasRenderer), so
    /// a copy scaled to any size looks exactly like the game's. Copies never catch the pointer and remember which of
    /// their images are the face (anything but the black outline), which <see cref="Tint"/> colours. The template is
    /// found again after a logout, when the scene is rebuilt.
    /// </summary>
    public static class StarGlyph
    {
        /// <summary>The size the game draws the template at; copies are scaled from it.</summary>
        private const float TemplateSize = 15f;

        private static GameObject template;

        /// <summary>The face images of one copy, found when it is made.</summary>
        private sealed class Faces : MonoBehaviour
        {
            public readonly List<Image> Images = new List<Image>();
        }

        /// <summary>A copy of the game's star under <paramref name="parent"/>, or null while the game scene is not loaded.</summary>
        public static RectTransform Create(Transform parent, float size)
        {
            GameObject source = Template();
            if (source == null)
                return null;
            GameObject copy = Object.Instantiate(source, parent, false);
            copy.name = "grindstone_star";
            Faces faces = copy.AddComponent<Faces>();
            foreach (Image image in copy.GetComponentsInChildren<Image>(true))
            {
                image.raycastTarget = false;
                if (image.color.maxColorComponent > 0.3f)
                    faces.Images.Add(image);
            }
            RectTransform rect = (RectTransform)copy.transform;
            rect.localScale = Vector3.one * (size / TemplateSize);
            return rect;
        }

        /// <summary>Colours the star's face, keeping each image's own alpha; the black outline stays.</summary>
        public static void Tint(RectTransform star, Color color)
        {
            if (star == null || !star.TryGetComponent(out Faces faces))
                return;
            foreach (Image image in faces.Images)
            {
                if (image != null)
                    image.color = new Color(color.r, color.g, color.b, image.color.a);
            }
        }

        private static GameObject Template()
        {
            if (template != null)
                return template;
            GameObject hud = EnemyHud.instance == null ? null : EnemyHud.instance.m_baseHud;
            Transform star = hud == null ? null : hud.transform.Find("level_2/star");
            template = star == null ? null : star.gameObject;
            return template;
        }
    }
}
