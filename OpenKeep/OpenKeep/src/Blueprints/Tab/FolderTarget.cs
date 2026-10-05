using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// A folder on the folder panel or the breadcrumb: what a click there opens, whether a drag can drop into it and
    /// whether a right click renames it. It knows when the mouse is over it (for the right click), lights up green
    /// while a drag would drop into it, and on a breadcrumb part underlines its text while the mouse is over it.
    /// </summary>
    public sealed class FolderTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const string LightName = "OpenKeep Drop";

        /// <summary>The folder under the mouse, or null.</summary>
        public static FolderTarget Hovered { get; private set; }

        /// <summary>The folder's path ("" for the top).</summary>
        public string Folder;

        /// <summary>A click opens <see cref="Folder"/> (false for the folder shown now).</summary>
        public bool Opens;

        /// <summary>A right click renames <see cref="Folder"/> (never the top).</summary>
        public bool Renames;

        /// <summary>The breadcrumb text to underline while the mouse is over it, or null on a panel row (the game's button shows its own hover).</summary>
        public TMP_Text Tinted;

        /// <summary>Sets what this target stands for (the parts are reused for other folders).</summary>
        public void Set(string folder, bool opens, bool renames)
        {
            Folder = folder;
            Opens = opens;
            Renames = renames && !string.IsNullOrEmpty(folder);
            SetLit(false);
        }

        /// <summary>A click (the button's own, so a gamepad works too): opens the folder unless a name box is up.</summary>
        public void Activate()
        {
            if (Opens && !NamePrompt.Showing)
                BlueprintSafe.Run("OpenKeep blueprint folder", () => BlueprintFolders.Open(Folder));
        }

        /// <summary>The folder can take dragged blueprints: any folder but the one shown (where they already are).</summary>
        public bool Takes() => Opens && gameObject.activeInHierarchy;

        /// <summary>The green frame of a drop target.</summary>
        public void SetLit(bool lit)
        {
            Transform light = transform.Find(LightName);
            if (!lit)
            {
                if (light != null)
                    light.gameObject.SetActive(false);
                return;
            }
            if (light == null)
                light = TabLook.Frame(transform, LightName);
            TabLook.Paint(light, TabLook.Target);
            light.SetAsLastSibling();
            light.gameObject.SetActive(true);
        }

        public void OnPointerEnter(PointerEventData data)
        {
            Hovered = this;
            if (Tinted != null && Opens)
                Tinted.fontStyle |= FontStyles.Underline;
        }

        public void OnPointerExit(PointerEventData data) => Leave();

        private void OnDisable() => Leave();

        private void Leave()
        {
            if (Hovered == this)
                Hovered = null;
            if (Tinted != null)
                Tinted.fontStyle &= ~FontStyles.Underline;
        }
    }
}
