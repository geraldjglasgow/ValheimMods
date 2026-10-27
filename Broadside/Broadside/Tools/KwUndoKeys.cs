using HarmonyLib;
using Hotkeys;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Ctrl + Z undoes and Ctrl + Y redoes the local player's building on the dock (<see cref="KwUndo"/>) while a Broadside
    /// piece is selected in the hammer, the build camera's flight included. Read by the Hotkeys library's rules for a
    /// modified key: either Ctrl held as the game reads it, no Shift or Alt, nothing being typed (the sizes window counts);
    /// and only while the game takes the player's keys (no menu, map or inventory open). One hidden object on the player's
    /// game, made the first time a build tool is taken out (<see cref="KwUndoHammer"/>), so never on a dedicated server.
    /// </summary>
    public sealed class KwUndoKeys : MonoBehaviour
    {
        private static KwUndoKeys? instance;

        public static void Ensure()
        {
            if (instance != null)
                return;
            GameObject holder = new GameObject("KwUndoKeys");
            DontDestroyOnLoad(holder);
            instance = holder.AddComponent<KwUndoKeys>();
        }

        private void Update()
        {
            bool undo = Input.GetKeyDown(KeyCode.Z), redo = !undo && Input.GetKeyDown(KeyCode.Y);
            Player? me = undo || redo ? Player.m_localPlayer : null;
            if (me == null || !Chord() || !InKit(me) || !me.TakeInput())
                return;
            if (undo)
                Guard.Run("Broadside undo", KwUndo.Undo, me);
            else
                Guard.Run("Broadside redo", KwUndo.Redo, me);
        }

        /// <summary>Ctrl + Z went down this frame with a Broadside piece in this player's hammer.</summary>
        public static bool UndoDown(Player player) => Input.GetKeyDown(KeyCode.Z) && Chord() && InKit(player);

        // Either Ctrl held, no Shift or Alt, nothing being typed.
        private static bool Chord() =>
            Held(KeyCode.LeftControl, KeyCode.RightControl) && !Held(KeyCode.LeftShift, KeyCode.RightShift)
            && !Held(KeyCode.LeftAlt, KeyCode.RightAlt) && !Typing.Active;

        private static bool Held(KeyCode left, KeyCode right) => Hotkey.KeyHeld(left) || Hotkey.KeyHeld(right);

        // A Broadside piece selected in the hammer: a tool, a fitting, a dock piece, the table or a saved ship.
        private static bool InKit(Player player)
        {
            Piece? selected = player.InPlaceMode() ? player.GetSelectedPiece() : null;
            return selected != null && selected.name.StartsWith("KW_", System.StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// In the game's debug mode (devcommands, debugmode) Z is its fly key in <c>Player.Update</c>: on the frame Ctrl + Z
    /// undoes, debug mode is off for that one update (it is read nowhere else there) and back on right after. Costs one
    /// static read a frame outside debug mode.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static class KwUndoDebugGuard
    {
        private static bool suppressed;

        [HarmonyPrefix]
        private static void Prefix(Player __instance)
        {
            if (!Player.m_debugMode || __instance != Player.m_localPlayer || !KwUndoKeys.UndoDown(__instance))
                return;
            Player.m_debugMode = false;
            suppressed = true;
        }

        [HarmonyFinalizer]
        private static void Finalizer()
        {
            if (!suppressed)
                return;
            suppressed = false;
            Player.m_debugMode = true;
        }
    }
}
