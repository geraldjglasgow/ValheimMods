using HarmonyLib;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// F2 renames a blueprint or a folder of the Blueprints tab in the game's text box, starting from its name: with the
    /// build menu open the blueprint under the mouse, with it closed the selected blueprint; a right click on a blueprint
    /// in the menu, or on a folder of the folder panel or the breadcrumb, does the same (<see cref="Tab.TabClicks"/>). A name with "/"
    /// moves it (see <see cref="BlueprintFiles"/>); the file's content is never touched. The box opens over the open menu
    /// (the game draws it above the HUD), which stays on its tab, folder and scroll and ignores the keys meanwhile
    /// (<see cref="BlueprintRightClickPatch"/>, <see cref="BlueprintTabKeysPatch"/>); Esc closes only the box, and after
    /// OK the menu is filled again in place. The game's own F2 (the connection panel) stays shut for a press that renames
    /// (<see cref="BlueprintRenameKeyPatch"/>).
    /// </summary>
    public static class BlueprintRename
    {
        private static int claimFrame = -1;
        private static Piece claimed;

        public static void Tick()
        {
            Piece target = Claimed();
            if (target != null)
                Start(target);
        }

        /// <summary>The entry an F2 press renames this frame, or null; worked out once a frame, so every reader agrees.</summary>
        public static Piece Claimed()
        {
            if (claimFrame == Time.frameCount)
                return claimed;
            claimFrame = Time.frameCount;
            claimed = Input.GetKeyDown(BlueprintRules.RenameKey) && !Keys.TextInputActive ? Target(Player.m_localPlayer) : null;
            return claimed;
        }

        /// <summary>The hovered entry with the menu open (a blueprint or a folder), else the selected blueprint, or null.</summary>
        private static Piece Target(Player player)
        {
            if (!BlueprintSettings.Enabled || player == null || !player.InPlaceMode() || Hud.instance == null)
                return null;
            if (Hud.IsPieceSelectionVisible())
            {
                Piece hovered = BlueprintTab.Hovered();
                return BlueprintMenu.Owns(hovered) ? hovered : null;
            }
            Piece selected = player.GetSelectedPiece();
            return BlueprintMenu.Owns(selected) ? selected : null;
        }

        /// <summary>Asks for a blueprint entry's new name (anything else is ignored).</summary>
        public static void Start(Piece target)
        {
            string path = BlueprintMenu.NameOf(target);
            if (path != null)
                StartPath(path, folder: false);
        }

        /// <summary>Asks for the new name of a blueprint or folder path (the top folder cannot be renamed).</summary>
        public static void StartPath(string path, bool folder)
        {
            if (!string.IsNullOrEmpty(path))
                NamePrompt.Ask(BlueprintWords.RenameTopic, BlueprintLibrary.Leaf(path), BlueprintFiles.NameLimit, typed => Done(path, folder, typed));
        }

        private static void Done(string path, bool folder, string typed)
        {
            if (BlueprintFiles.Rename(path, folder, typed, out string to, out string error))
            {
                BlueprintMenu.Moved(path, to);
                Messages.Center(BlueprintWords.Format(BlueprintWords.Renamed, path, to));
            }
            else if (error != null)
                Messages.Center(error);
            BlueprintMenu.Refresh();
        }
    }

    /// <summary>
    /// ConnectPanel.Update prefix and postfix (private): the game opens or shuts its connection panel on F2; a press that
    /// renames a blueprint puts the panel back as it was.
    /// </summary>
    [HarmonyPatch(typeof(ConnectPanel), nameof(ConnectPanel.Update))]
    public static class BlueprintRenameKeyPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ConnectPanel __instance, out bool __state)
        {
            __state = __instance.m_root != null && __instance.m_root.gameObject.activeSelf;
        }

        [HarmonyPostfix]
        public static void Postfix(ConnectPanel __instance, bool __state)
        {
            if (__instance.m_root != null && BlueprintSafe.Call("OpenKeep rename key", () => BlueprintRename.Claimed() != null || Bench.BenchWindow.IsOpen, false))
                __instance.m_root.gameObject.SetActive(__state);
        }
    }
}
