using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// A click with the Copy entry (<see cref="Sites.SiteHooks.CopyClick"/>, routed by <see cref="BlueprintTool"/>, which
    /// already kept the game from placing anything): the building under the crosshair is selected, or let go when every
    /// piece of it was (Shift + click); a plain click the piece alone, with G its joined pieces of the same type, with Shift + G
    /// every piece of that type in its building. The same pieces the hover
    /// showed (<see cref="CopyHover.Take"/>), its building looked up now when the hover had not found it yet.
    /// </summary>
    public static class CopyClicks
    {
        public static void OnClick(Player player)
        {
            if (!BlueprintSettings.Enabled)
            {
                Messages.Center(BlueprintWords.Disabled);
                return;
            }
            Piece aimed = CopyAim.Pick();
            CopyMode mode = CopyKeys.Mode;
            List<FixPiece> building = aimed != null && mode != CopyMode.Piece ? CopyBuildings.Of(aimed, now: true) : null;
            List<FixPiece> group = CopyHover.Take(aimed, mode, building);
            if (group.Count == 0)
            {
                Messages.Center(CopyWords.AimHint);
                return;
            }
            int changed = CopySelection.Toggle(group);
            Messages.Center(BlueprintWords.Format(changed > 0 ? CopyWords.Added : CopyWords.Removed, Mathf.Abs(changed), CopySelection.Count));
        }
    }
}
