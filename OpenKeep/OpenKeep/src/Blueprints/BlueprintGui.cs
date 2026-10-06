using System;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The blueprint feature's IMGUI drawing (its HUD lines and the Site planner's panel), on the plugin's own object.
    /// Unity calls an enabled OnGUI several times every frame whether it draws or not, so the component is switched on
    /// by <see cref="BlueprintRunner"/> only while there is something to draw, and stays on one frame longer, so a
    /// callback sees its own "nothing to show" once and lets go of what it drew.
    /// </summary>
    public sealed class BlueprintGui : MonoBehaviour
    {
        private static readonly Action DrawHud = BlueprintHud.Draw;

        private bool wantedBefore;

        /// <summary>Per frame from the runner: drawing is wanted now.</summary>
        public void Want(bool wanted)
        {
            bool on = wanted || wantedBefore;
            if (enabled != on)
                enabled = on;
            wantedBefore = wanted;
        }

        private void OnGUI()
        {
            BlueprintSafe.Run("OpenKeep blueprint HUD", DrawHud);
            Sites.SiteHooks.Gui();
        }
    }
}
