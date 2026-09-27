using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Menu;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// The quick ramp key: a ramp from the player's feet to the aimed point, with the brush width and the current
    /// profile, built at once while any terrain entry is selected. It is the ramp entry's work: charged, checked and
    /// named as that entry, and only available while the server allows the quick ramp and shows the ramp entry. With
    /// the ramp entry selected and no point set, the preview shows it faintly.
    /// </summary>
    public static class QuickRamp
    {
        /// <summary>The ramp the key would build now, or null without a cursor.</summary>
        public static PathDraft Draft()
        {
            Player player = Player.m_localPlayer;
            if (player == null || !PathSelection.TryCursor(out Vector3 cursor))
                return null;
            Vector3 feet = player.transform.position;
            Vector3 end = PathSelection.WithHeight(cursor);
            return DraftFactory.Ramp(feet, end, 0, PathSettings.QuickRampBlendEnds.Value, PathSelection.BrushWidth());
        }

        /// <summary>The server allows the quick ramp and the ramp entry is on for this player.</summary>
        public static bool Allowed
        {
            get
            {
                ToolAction ramp = RampAction();
                return PathSettings.QuickRampAllowed.Value && ramp != null && EntryVisibility.Visible(ramp.Id);
            }
        }

        public static void Build()
        {
            if (!Allowed)
            {
                Messages.Center(PathWords.Text("quick_off"));
                return;
            }
            PathDraft draft = Draft();
            if (draft != null && PathCommit.Commit(draft, RampAction(), PathSelection.RampKey))
                Messages.TopLeft(PathWords.Text("built"));
        }

        /// <summary>The ramp entry's action (its costs, cooldown and protection rules apply), or null when there is none.</summary>
        private static ToolAction RampAction()
        {
            foreach (ToolAction action in ActionCatalog.All)
            {
                if (PathSelection.IsRamp(action))
                    return action;
            }
            return null;
        }
    }
}
