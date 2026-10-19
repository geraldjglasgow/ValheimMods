using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Draws short words in the world with the game's own floating damage text (DamageText.AddInworldText, local
    /// only), for the callouts of every module (<see cref="WoodCallout"/>, <see cref="MineCallout"/>). Runs on the client
    /// that shows the text: nothing is drawn without a camera (a dedicated server), while the HUD is hidden, or when
    /// the spot is farther than <see cref="Range"/> from the camera. Each caller checks its own "Show Callouts" first.
    /// A callout sent from another machine travels as a <see cref="LangWire"/> message; one that arrives here unread is
    /// read into this machine's language before it is drawn.
    /// </summary>
    public static class FloatingText
    {
        /// <summary>How far away a callout can be seen, in metres.</summary>
        public const float Range = 40f;

        /// <summary>Shows <paramref name="text"/> at <paramref name="position"/> on this client, if a camera is near it.</summary>
        public static void Show(Vector3 position, string text)
        {
            Camera camera = Utils.GetMainCamera();
            if (camera == null || DamageText.instance == null || Hud.IsUserHidden() || string.IsNullOrEmpty(text))
                return;
            float distance = Vector3.Distance(camera.transform.position, position);
            if (distance > Range)
                return;
            string shown = LangWire.IsMessage(text) ? LangWire.Read(text) : text;
            DamageText.instance.AddInworldText(DamageText.TextType.Bonus, position, distance, shown, false);
        }
    }
}
