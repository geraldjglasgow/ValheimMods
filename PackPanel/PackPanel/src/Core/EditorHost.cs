using UnityEngine;
using YamlConfig;

namespace PackPanel.Core
{
    /// <summary>
    /// Draws the YAML editor window (Edit backpacks, Edit tackleboxes). Unity runs its IMGUI pass every frame for every
    /// enabled behaviour that has an <c>OnGUI</c>, so the drawing lives on this component of its own, switched on by
    /// <see cref="Follow"/> only while the window is open, and the plugin has no <c>OnGUI</c>.
    /// </summary>
    public sealed class EditorHost : MonoBehaviour
    {
        private YamlEditorWindow window;

        public static EditorHost Add(GameObject on, YamlEditorWindow window)
        {
            EditorHost host = on.AddComponent<EditorHost>();
            host.window = window;
            host.enabled = false;
            return host;
        }

        /// <summary>From the plugin's Update: on while the window is open, off otherwise.</summary>
        public void Follow()
        {
            bool open = window != null && window.IsOpen;
            if (enabled != open)
                enabled = open;
        }

        private void OnGUI() => window?.OnGUI();
    }
}
