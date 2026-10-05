using UnityEngine;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// The highlight on a piece button of the Blueprints tab: a gold frame with a faint gold fill on a picked blueprint
    /// (the multi-selection). A child of the game's button, drawn above the icon and the name band, set again whenever
    /// the menu sets the button up or the picks change.
    /// </summary>
    public static class TabMark
    {
        public enum State
        {
            None,
            Picked,
        }

        private const string Name = "OpenKeep Mark";

        public static void Set(BuildUiPieceButton button, State state)
        {
            Transform mark = button.transform.Find(Name);
            if (state == State.None)
            {
                if (mark != null)
                    mark.gameObject.SetActive(false);
                return;
            }
            if (mark == null)
                mark = TabLook.Frame(button.transform, Name);
            TabLook.Paint(mark, TabLook.Picked);
            mark.SetAsLastSibling();
            mark.gameObject.SetActive(true);
        }
    }
}
