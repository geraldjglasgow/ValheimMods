using UnityEngine;
using UnityEngine.Rendering;

namespace Wayfare.SeaGates
{
    /// <summary>What the placement preview draws, client only: one world-space line from the ghost to the pillar it
    /// would pair with, green or red, and the reason as the game's own floating NPC text over the line's middle. The
    /// text is set only when it changes (the NPC text fades in each time it is set), and the whole preview hides
    /// itself when nothing has refreshed it for a frame, so it never outlives the ghost or the local player.</summary>
    internal sealed class SeaGatePreviewLine : MonoBehaviour
    {
        private const float Lift = 1.5f;
        private const float Width = 0.08f;
        private const float LabelLift = 1.5f;
        private const float LabelCullDistance = 80f;
        private const float LabelRetrySeconds = 1f;
        private static readonly Color OkColour = new Color(0.35f, 1f, 0.4f, 0.9f);
        private static readonly Color BadColour = new Color(1f, 0.3f, 0.25f, 0.9f);

        private static SeaGatePreviewLine instance;

        private LineRenderer line;
        private GameObject label;
        private string shownText;
        private string colouredFrom;   // the reason the coloured text was made from, compared by reference
        private bool colouredOk;
        private string coloured;
        private float labelSetAt;
        private int refreshedFrame = -10;

        /// <summary>Draws the line from <paramref name="from"/> to <paramref name="to"/> with the reason (already
        /// localized); call every frame while it should stay.</summary>
        public static void Show(Vector3 from, Vector3 to, bool ok, string reason)
        {
            SeaGatePreviewLine preview = Ensure();
            preview.refreshedFrame = Time.frameCount;
            preview.DrawLine(from + Vector3.up * Lift, to + Vector3.up * Lift, ok ? OkColour : BadColour);
            preview.label.transform.position = (from + to) * 0.5f + Vector3.up * (Lift + LabelLift);
            preview.SetLabel(preview.Text(ok, reason));
        }

        public static void Hide()
        {
            if (instance != null)
                instance.HideAll();
        }

        /// <summary>The coloured reason, made again only when the reason or the verdict changes (the reason is a
        /// string kept until it changes, <see cref="SeaGatePreview.Reason"/>).</summary>
        private string Text(bool ok, string reason)
        {
            if (coloured != null && ok == colouredOk && ReferenceEquals(reason, colouredFrom))
                return coloured;
            colouredFrom = reason;
            colouredOk = ok;
            coloured = (ok ? "<color=#66FF77>" : "<color=#FF6655>") + reason + "</color>";
            return coloured;
        }

        private static SeaGatePreviewLine Ensure()
        {
            if (instance != null)
                return instance;
            GameObject holder = new GameObject("Wayfare.SeaGatePreview");
            instance = holder.AddComponent<SeaGatePreviewLine>();
            instance.line = BuildLine(holder);
            instance.label = new GameObject("Wayfare.SeaGatePreview.Label");
            instance.label.transform.SetParent(holder.transform, false);
            return instance;
        }

        private static LineRenderer BuildLine(GameObject holder)
        {
            LineRenderer line = holder.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = Width;
            line.endWidth = Width;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = SeaGatePreviewMaterial.Get();
            line.enabled = false;
            return line;
        }

        private void DrawLine(Vector3 from, Vector3 to, Color colour)
        {
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startColor = colour;
            line.endColor = colour;
            line.enabled = line.sharedMaterial != null;
        }

        /// <summary>Sets the floating text when it changed, or again after a pause when the game dropped it (the HUD
        /// was hidden, or it was culled), never every frame.</summary>
        private void SetLabel(string text)
        {
            Chat chat = Chat.instance;
            if (chat == null)
                return;
            bool missing = chat.FindNpcText(label) == null;
            if (text == shownText && !(missing && Time.time - labelSetAt > LabelRetrySeconds))
                return;
            chat.SetNpcText(label, Vector3.zero, LabelCullDistance, 0f, "", text, false);
            shownText = text;
            labelSetAt = Time.time;
        }

        private void HideAll()
        {
            line.enabled = false;
            if (shownText == null)
                return;
            if (Chat.instance != null)
                Chat.instance.ClearNpcText(label);
            shownText = null;
        }

        private void LateUpdate()
        {
            if (Time.frameCount - refreshedFrame > 1)
                HideAll();
        }

        private void OnDestroy()
        {
            if (Chat.instance != null && label != null)
                Chat.instance.ClearNpcText(label);
            if (instance == this)
                instance = null;
        }
    }

    /// <summary>The preview line's material: an unlit vertex-colour shader that ships with the game (Sprites/Default,
    /// else the internal coloured shader), else the cart's own rope line material. Null when none exists, and the
    /// line is then not drawn; the reason text still shows.</summary>
    internal static class SeaGatePreviewMaterial
    {
        private static readonly string[] Shaders = { "Sprites/Default", "Hidden/Internal-Colored" };
        private static Material material;

        public static Material Get()
        {
            if (material != null)
                return material;
            foreach (string name in Shaders)
            {
                Shader shader = Shader.Find(name);
                if (shader != null)
                    return material = new Material(shader) { name = "Wayfare sea gate preview", hideFlags = HideFlags.HideAndDontSave };
            }
            return material = CartLineMaterial();
        }

        private static Material CartLineMaterial()
        {
            GameObject cart = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("Cart") : null;
            LineRenderer rope = cart != null ? cart.GetComponentInChildren<LineRenderer>(true) : null;
            return rope != null ? rope.sharedMaterial : null;
        }
    }
}
