using UnityEngine;
using UnityEngine.EventSystems;

namespace PlateColumn
{
    /// <summary>
    /// A plate's tooltip. The game's <c>UITooltip</c> follows the mouse and cannot be pinned (its fixed position is only
    /// used with a gamepad or touch), so the plates carry this instead: half a second after the pointer enters the plate
    /// a <see cref="TipBox"/> appears right of it and stays put until the pointer leaves or the panel closes. A
    /// <c>UITooltip</c> an older copy of this library left on the plate is removed on hover, so a plate never shows two.
    /// </summary>
    internal sealed class PlateTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float ShowDelay = 0.5f;

        public GameObject? Prefab;
        public string Topic = "";
        public string Text = "";

        private float showAt = -1f;
        private GameObject? box;

        public void OnPointerEnter(PointerEventData eventData)
        {
            foreach (UITooltip old in GetComponents<UITooltip>())
            {
                Destroy(old);
            }
            UITooltip.HideTooltip();
            showAt = Time.unscaledTime + ShowDelay;
        }

        public void OnPointerExit(PointerEventData eventData) => Hide();

        private void Update()
        {
            if (showAt >= 0f && Time.unscaledTime >= showAt)
            {
                showAt = -1f;
                box = TipBox.Show(Prefab, (RectTransform)transform, Topic, Text);
            }
        }

        private void OnDisable() => Hide();

        private void Hide()
        {
            showAt = -1f;
            if (box != null)
            {
                Destroy(box);
                box = null;
            }
        }
    }
}
