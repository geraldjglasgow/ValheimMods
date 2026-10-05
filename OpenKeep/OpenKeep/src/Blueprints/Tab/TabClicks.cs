using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// Clicks on the Blueprints tab's entries. Left (the game's own click, a BuildUi.OnSelectPiece prefix): with Ctrl or
    /// Shift held on a blueprint it changes the picks (<see cref="TabPicks"/>) and the menu stays open; a plain click
    /// clears the picks and keeps the game's behaviour (the entry is selected for building and the menu closes), except
    /// on the Construction ghosts switch, which flips (<see cref="GhostSwitch"/>) with the menu staying open. Right (a
    /// BuildUi.Update prefix, read from the mouse, since the game's buttons have no right click): on a blueprint of the
    /// grid, a folder row of the panel or a breadcrumb part (not the top) it renames it like F2 in a name box over the
    /// open menu, and that frame of
    /// the menu is skipped, so the game's own use of the right button there (its Build Menu key, which closes the menu)
    /// does not fire on top.
    /// </summary>
    public static class TabClicks
    {
        /// <summary>False when the click was taken here (the game does not select the entry or close the menu).</summary>
        public static bool BeforeSelect(Piece piece)
        {
            if (BlueprintMenu.IsGhosts(piece))
            {
                GhostSwitch.Toggle();
                return false;
            }
            bool ctrl = BlueprintKeys.Ctrl, shift = Shift;
            if ((ctrl || shift) && TabPicks.Pickable(piece) && BlueprintTab.Showing)
            {
                TabPicks.Click(piece, ctrl, shift);
                return false;
            }
            TabPicks.Clear();
            return true;
        }

        /// <summary>True when the right button went down on a blueprint of the shown tab, a panel folder or a breadcrumb part and its rename began.</summary>
        public static bool RightClicked()
        {
            if (!BlueprintTab.Showing || ZInput.IsGamepadActive() || !ZInput.GetMouseButtonDown(1))
                return false;
            Piece hovered = BlueprintTab.HoveredButton != null ? BlueprintTab.HoveredButton.Piece : null;
            if (TabPicks.Pickable(hovered))
            {
                BlueprintRename.Start(hovered);
                return true;
            }
            FolderTarget folder = FolderTarget.Hovered;
            if (folder == null || !folder.Renames || !folder.gameObject.activeInHierarchy)
                return false;
            BlueprintRename.StartPath(folder.Folder, folder: true);
            return true;
        }

        private static bool Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    /// <summary>
    /// Player.SetControls prefix: Ctrl is the game's sneak key, so a Ctrl + click that picks entries of the shown
    /// Blueprints tab (the game still reads the player's keys while the build menu is open) does not make the player
    /// sneak too.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    public static class TabCtrlPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance, ref bool crouch)
        {
            if (crouch && __instance == Player.m_localPlayer && BlueprintKeys.Ctrl && BlueprintTab.Showing)
                crouch = false;
        }
    }
}
