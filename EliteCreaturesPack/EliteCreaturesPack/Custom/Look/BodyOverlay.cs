using EliteCreaturesPack.Core;
using LocalEffects;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// A creature's coloured smoke or flame overlay, worn for as long as it lives: on every peer that draws, as the
    /// creature starts, a local copy of its overlay template (<see cref="OverlaySource"/>) is put on its body the way the
    /// game puts a status effect's look on a burning or smoked character (at its centre, turned with it, as big as twice
    /// its radius, on the bone the game's entry names). The copy is made through LocalEffects: never networked, its network
    /// view, colliders and any area damage stripped, no timer, so it lasts until the creature goes and never hurts anything.
    /// Nothing is made on a dedicated server, and nothing runs after the start.
    /// </summary>
    public sealed class BodyOverlay : MonoBehaviour
    {
        /// <summary>The overlay's template, a part kept with the creature (set at build).</summary>
        public GameObject? m_template;

        /// <summary>Sized to the body as the game sizes the status effect it was taken from.</summary>
        public bool m_scaleToBody = true;

        /// <summary>The bone the game's entry puts it on; empty for the body's centre.</summary>
        public string m_child = "";

        private void Start() => SafeCall.Run("custom creature overlay", static me => me.Wear(), this);

        private void Wear()
        {
            if (Drawing.Headless || m_template == null)
            {
                return;
            }
            Character? character = GetComponent<Character>();
            Transform? bone = string.IsNullOrEmpty(m_child) ? null : Utils.FindChild(transform, m_child);
            Transform parent = bone != null ? bone : transform;
            Vector3 at = bone != null || character == null ? parent.position : character.GetCenterPoint();
            GameObject? worn = LocalEffect.Attach(m_template, parent, at, endless: true);
            if (worn == null)
            {
                return;
            }
            worn.transform.rotation = transform.rotation;
            if (m_scaleToBody && character != null)
            {
                Size(worn.transform, parent, character.GetRadius() * 2f);
            }
        }

        /// <summary>A world size of <paramref name="size"/> on every axis, whatever the parent's own scale.</summary>
        private static void Size(Transform worn, Transform parent, float size)
        {
            Vector3 lossy = parent.lossyScale;
            worn.localScale = new Vector3(size / NonZero(lossy.x), size / NonZero(lossy.y), size / NonZero(lossy.z));
        }

        private static float NonZero(float value) => Mathf.Abs(value) < 0.0001f ? 0.0001f : value;
    }
}
